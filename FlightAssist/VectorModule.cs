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
    partial class Program
    {
        public class VectorModule : Module
        {
            private double angleThreshold = 0.01;
            private double speedThreshold = 0.3;

            private readonly GyroController gyroController;
            private readonly IMyShipController cockpit;
            private Vector3D thrustVector;

            private double startSpeed;

            public VectorModule(CustomDataConfig config, GyroController gyroController, IMyShipController cockpit)
            {
                this.gyroController = gyroController;
                this.cockpit = cockpit;

                thrustVector = GetThrustVector(config.Get<string>("spaceMainThrust"));

                AddAction("disabled", (args) => { gyroController.SetGyroOverride(false); }, null);
                AddAction("brake", (args) => {
                    startSpeed = cockpit.GetShipSpeed();
                    cockpit.DampenersOverride = false;
                }, SpaceBrake);
                AddAction("prograde", null, () => { TargetOrientation(-Vector3D.Normalize(cockpit.GetShipVelocities().LinearVelocity)); });
                AddAction("retrograde", null, () => { TargetOrientation(Vector3D.Normalize(cockpit.GetShipVelocities().LinearVelocity)); });
            }

            protected override void OnSetAction()
            {
                gyroController.SetGyroOverride(action?.execute != null);
            }

            public override void Tick()
            {
                base.Tick();

                PrintStatus();

                if (gyroController.gyroOverride)
                    action?.execute();
            }

            private void PrintStatus()
            {
                PrintLine("  VECTOR MODULE ACTIVE");
                PrintLine("  MODE: " + action?.name.ToUpper() + "\n");

                string output = "";
                if (action?.name == "brake")
                {
                    var percent = Math.Abs(cockpit.GetShipSpeed() / startSpeed);
                    string progressBar;
                    progressBar = "|";
                    int width = 24;
                    var height = 3;
                    output = " PROGRESS\n";
                    for (var i = 0; i < width; i++)
                        progressBar += (i < width * (1-percent)) ? "#" : " ";
                    progressBar += "|\n";
                    for (var i = 0; i < height; i++)
                        output += progressBar;
                }
                else
                    output = " Speed: " + Math.Abs(cockpit.GetShipSpeed()).ToString("000") + " m/s";

                PrintLine(output);
            }

            private void TargetOrientation(Vector3D target)
            {
                gyroController.SetTargetOrientation(thrustVector, target);
            }

            private void SpaceBrake()
            {
                TargetOrientation(Vector3D.Normalize(cockpit.GetShipVelocities().LinearVelocity));

                if (Helpers.EqualWithMargin(gyroController.angle, 0, angleThreshold))
                    cockpit.DampenersOverride = true;

                if (cockpit.GetShipSpeed() < speedThreshold)
                    SetAction("disabled");
            }

            private Vector3D GetThrustVector(string direction)
            {
                Matrix cockpitOrientation;
                cockpit.Orientation.GetMatrix(out cockpitOrientation);
                switch (direction.ToLower())
                {
                    case "down": return cockpitOrientation.Down;
                    case "up": return cockpitOrientation.Up;
                    case "forward": return cockpitOrientation.Forward;
                    case "backward": return cockpitOrientation.Backward;
                    case "right": return cockpitOrientation.Right;
                    case "left": return cockpitOrientation.Left;
                    default: throw new Exception("Unidentified thrust direction '" + direction.ToLower() + "'");
                }
            }
        }
    }
}
