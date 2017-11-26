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
        // TODO: Read from custom data?
        const string remoteBlockName = "FA Remote";
        const string textPanelName = "FA Screen";
        const string gyroGroupName = "FA Gyros";
        const string spaceMainThrust = "backward";

        const double halfPi = Math.PI / 2;
        const double radToDeg = 180 / Math.PI;
        const double degToRad = Math.PI / 180;

        const int gyroResponsiveness = 8;
        const double minGyroRpmScale = 0.001;
        const double gyroVelocityScale = 0.2;

        public IMyRemoteControl remote;
        public IMyTextPanel textPanel;
        public List<IMyGyro> gyros;

        private GyroController gyroController;

        private HoverModule hoverModule;
        private VectorModule vectorModule;
        private Module activeModule;

        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            GetBlocks();
            gyroController = new GyroController(gyros, remote);
            hoverModule = new HoverModule(gyroController);
            vectorModule = new VectorModule(gyroController);
        }

        public void Main(string argument, UpdateType updateType) {
            
            if ((updateType & UpdateType.Update1) == 0)
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
                        break;
                }

                if (activeModule != null)
                    activeModule.ProcessCommand(args);

                return;
            } else
            {
                if (activeModule != null)
                {
                    activeModule.Tick();
                    if (textPanel != null)
                    {
                        textPanel.WritePublicText(activeModule.GetPrintString(), false);
                        textPanel.ShowPublicTextOnScreen();
                    }
                }
            }

            gyroController.Update();
        }

        // TODO try/catch
        private void GetBlocks() {
            // Remote Control Block
            remote = GridTerminalSystem.GetBlockWithName(remoteBlockName) as IMyRemoteControl;
            if (remote == null)
                Helpers.PrintException("Unable to find Remote Control block with name: " + remoteBlockName); 
            else
                Echo("Detected remote control block");

            // Text Panel (optional)
            textPanel = GridTerminalSystem.GetBlockWithName(textPanelName) as IMyTextPanel;
            if (textPanel != null)
                Echo("Detected text panel");

            // Gyroscopes
            IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(name: gyroGroupName);
            if (group == null)
                Helpers.PrintException("Error: Unable to find group with name: " + gyroGroupName);
            else {
                var list = new List<IMyTerminalBlock>();
                group.GetBlocks(list);
                gyros = list.ConvertAll(x => (IMyGyro)x);
                if (list.Count > 0)
                    Echo("Detected " + gyros.Count + " gyroscopes");
                else
                    Echo("Warning: Group " + gyroGroupName + " contains no gyroscopes");
            }
        }
    }
}