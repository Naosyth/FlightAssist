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
        public const string Version = "3.0.2";

        private List<ConfigOption> defaultConfig = new List<ConfigOption>()
        {
            new ConfigOption("blockGroupName", "Flight Assist", "Block group that contains all required blocks.", true),
            new ConfigOption("smartDelayTime", "20", "Duration to wait in ticks before overriding gyros in smart mode.", true),
            new ConfigOption("spaceMainThrust", "backward", "Direction of your main thrust used by the vector module.", true),
            new ConfigOption("gyroResponsiveness", "8", "Tuning variable. Higher = faster but may over-shoot more.", true),
            new ConfigOption("maxPitch", "45", "Max pitch used by hover module.", true),
            new ConfigOption("maxRoll", "45", "Max roll used by hover module.", true),
            new ConfigOption("gyroVelocityScale", "0.2", "Tuning variable used to adjust gyroscope response.", true),
            new ConfigOption("startCommand", "hover smart", "Command ran automatically upon successful compilation.", false),
        };
        private CustomDataConfig configReader;

        public IMyShipController cockpit;
        public IMyTextPanel textPanel;
        public List<IMyGyro> gyros = new List<IMyGyro>();

        private GyroController gyroController;
        private HoverModule hoverModule;
        private VectorModule vectorModule;
        private Module activeModule;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            configReader = new CustomDataConfig(Me, defaultConfig);
            GetBlocks();
            gyroController = new GyroController(gyros, cockpit);
            hoverModule = new HoverModule(configReader, gyroController, cockpit);
            vectorModule = new VectorModule(configReader, gyroController, cockpit);

            string startCommand = configReader.Get<string>("startCommand");
            if (startCommand != null)
                ProcessCommand(startCommand);
            else
                gyroController.SetGyroOverride(false);
        }

        public void Main(string argument, UpdateType updateType)
        {
            if ((updateType & UpdateType.Update1) == 0)
            {
                ProcessCommand(argument);
                return;
            }
            gyroController.Tick();
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
                    gyroController.SetGyroOverride(false);
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
            string blockGroupName = configReader.Get<string>("blockGroupName");
            IMyBlockGroup blockGroup = GridTerminalSystem.GetBlockGroupWithName(blockGroupName);

            List<IMyShipController> controllers = new List<IMyShipController>();
            blockGroup.GetBlocksOfType<IMyShipController>(controllers);
            if (controllers.Count == 0)
                throw new Exception("Error: " + blockGroupName + " does not contain a cockpit or remote control block.");
            cockpit = controllers[0];

            List<IMyTextPanel> textPanels = new List<IMyTextPanel>();
            blockGroup.GetBlocksOfType<IMyTextPanel>(textPanels);
            if (textPanels.Count > 0)
            {
                textPanel = textPanels[0];
                textPanel.Font = "Monospace";
                textPanel.FontSize = 1.0f;
                textPanel.ShowPublicTextOnScreen();
            }

            blockGroup.GetBlocksOfType<IMyGyro>(gyros);
            if (gyros.Count == 0)
                throw new Exception("Error: " + blockGroupName + " does not contain any gyroscopes.");
        }
    }
}
