using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        public const string Version = "2.0";

        private ConfigReader configReader;

        public IMyRemoteControl remote;
        public IMyTextPanel textPanel;
        public List<IMyGyro> gyros;

        private GyroController gyroController;
        private HoverModule hoverModule;
        private VectorModule vectorModule;
        private Module activeModule;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            configReader = new ConfigReader(Me);
            GetBlocks();
            gyroController = new GyroController(gyros, remote);
            hoverModule = new HoverModule(configReader, gyroController);
            vectorModule = new VectorModule(configReader, gyroController);

            string startCommand = configReader.Get<string>("startCommand");
            if (startCommand != null)
                ProcessCommand(startCommand);
            else
                gyroController.OverrideGyros(false);
        }

        public void Main(string argument, UpdateType updateType)
        {
            if ((updateType & UpdateType.Update1) == 0)
            {
                ProcessCommand(argument);
                return;
            }
            gyroController.Update();
            activeModule?.Tick();
            if (textPanel != null)
            {
                textPanel.WritePublicText(GetTextPanelHeader());
                if (activeModule != null)
                    textPanel.WritePublicText(activeModule?.GetPrintString(), true);
            }
        }

        private void ProcessCommand(string argument)
        {
            string[] args = argument.Split(' ');
            switch (args[0].ToLower())
            {
                case "hover":
                    activeModule = hoverModule;
                    break;
                case "vector":
                    activeModule = vectorModule;
                    break;
                case "stop":
                    activeModule = null;
                    gyroController.OverrideGyros(false);
                    break;
                case "reset":
                    configReader.SetDefaults();
                    break;
            }

            if (activeModule != null)
                activeModule.ProcessCommand(args);
        }

        private string GetTextPanelHeader()
        {
            string header = "       FLIGHT ASSIST V" + Version;
            header += "\n----------------------------------------\n\n";
            return header;
        }

        private void GetBlocks()
        {
            // Remote Control Block
            remote = GetBlockByName(configReader.Get<string>("remoteBlockName")) as IMyRemoteControl;

            // Text Panel (optional)
            textPanel = GetBlockByName(configReader.Get<string>("textPanelName"), true) as IMyTextPanel;
            if (textPanel != null)
            {
                textPanel.FontSize = 1.5f;
                textPanel.ShowPublicTextOnScreen();
            }

            // Gyroscopes
            IMyBlockGroup group = GetBlockGroupByName(configReader.Get<string>("gyroGroupName"));
            var list = new List<IMyTerminalBlock>();
            group.GetBlocks(list);
            gyros = list.ConvertAll(x => (IMyGyro)x);
            if (list.Count < 0)
                Echo("Warning: no gyroscopes were found in the BlockGroup");
        }

        private IMyTerminalBlock GetBlockByName(string name, bool optional = false)
        {
            IMyTerminalBlock block = GridTerminalSystem.GetBlockWithName(name);
            if (!optional && block == null)
                Helpers.PrintException("Error: Unable to find block with name: " + name);
            return block;
        }

        private IMyBlockGroup GetBlockGroupByName(string name, bool optional = false)
        {
            IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(name);
            if (!optional && group == null)
                Helpers.PrintException("Error: Unable to find group with name: " + name);
            return group;
        }
    }
}