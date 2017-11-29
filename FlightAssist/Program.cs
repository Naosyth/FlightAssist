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
        public const string Version = "3.0.1";

        private List<ConfigOption> defaultConfig = new List<ConfigOption>()
        {
            new ConfigOption("remoteBlockName", "FA Remote", "Name of the remote control block.", true),
            new ConfigOption("textPanelName", "FA Screen", "Name of the text panel.", false),
            new ConfigOption("gyroGroupName", "FA Gyros", "Name of the group containing the gyroscopes.", true),
            new ConfigOption("spaceMainThrust", "backward", "Direction of your main thrust used by the vector module.", true),
            new ConfigOption("gyroResponsiveness", "8", "Tuning variable. Higher = faster but may over-shoot.", true),
            new ConfigOption("maxPitch", "45", "Max pitch used by hover module.", true),
            new ConfigOption("maxRoll", "45", "Max roll used by hover module.", true),
            new ConfigOption("gyroVelocityScale", "0.2", "Tuning variable used to adjust gyroscope response.", true),
            new ConfigOption("startCommand", "hover stop", "Command ran automatically upon successful compilation.", false),
        };
        private CustomDataConfig configReader;

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
            configReader = new CustomDataConfig(Me, defaultConfig);
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

            if (args.Length < 1)
                return;

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
                    configReader.InitializeConfig();
                    break;
            }

            if (activeModule != null)
                activeModule.ProcessCommand(args);
        }

        private string GetTextPanelHeader()
        {
            string header = "    FLIGHT ASSIST V" + Version;
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
                textPanel.Font = "Monospace";
                textPanel.FontSize = 1.0f;
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