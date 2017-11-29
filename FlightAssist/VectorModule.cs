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
            private Vector3D thrustVector;

            private double startSpeed;

            public VectorModule(CustomDataConfig config, GyroController gyroController)
            {
                this.gyroController = gyroController;

                thrustVector = GetThrustVector(config.Get<string>("spaceMainThrust"));

                AddAction("disabled", (args) => { gyroController.OverrideGyros(false); }, null);
                AddAction("brake", (args) => {
                    startSpeed = gyroController.speed;
                    gyroController.remote.DampenersOverride = false;
                }, SpaceBrake);
                AddAction("prograde", null, () => { TargetOrientation(-gyroController.deltaPosition); });
                AddAction("retrograde", null, () => { TargetOrientation(gyroController.deltaPosition); });
            }

            protected override void OnSetAction()
            {
                gyroController.OverrideGyros(action?.execute != null);
            }

            public override void Tick()
            {
                base.Tick();

                PrintStatus();

                if (gyroController.gyrosEnabled)
                    action?.execute();
            }

            private void PrintStatus()
            {
                PrintLine("  VECTOR MODULE ACTIVE");
                PrintLine("  MODE: " + action?.name.ToUpper() + "\n");

                string output = "";
                if (action?.name == "brake")
                {
                    var percent = Math.Abs(gyroController.speed / startSpeed);
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
                    output = " Speed: " + Math.Abs(gyroController.speed).ToString("000") + " m/s";

                PrintLine(output);
            }

            private void TargetOrientation(Vector3D target)
            {
                gyroController.SetTargetOrientation(thrustVector, target);
            }

            private void SpaceBrake()
            {
                TargetOrientation(gyroController.deltaPosition);

                if (Helpers.EqualWithMargin(gyroController.angle, 0, angleThreshold))
                    gyroController.remote.DampenersOverride = true;

                if (gyroController.speed < speedThreshold)
                    SetAction("disabled");
            }

            private Vector3D GetThrustVector(string direction)
            {
                switch (direction.ToLower())
                {
                    case "down": return gyroController.shipOrientation.Down;
                    case "up": return gyroController.shipOrientation.Up;
                    case "forward": return gyroController.shipOrientation.Forward;
                    case "backward": return gyroController.shipOrientation.Backward;
                    case "right": return gyroController.shipOrientation.Right;
                    case "left": return gyroController.shipOrientation.Left;
                    default: throw new Exception("Unidentified thrust direction '" + direction.ToLower() + "'");
                }
            }
        }
    }
}
