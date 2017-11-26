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
        public class HoverModule : Module
        {
            private int gyroResponsiveness;
            private double maxPitch;
            private double maxRoll;

            private readonly ConfigReader config;
            private readonly GyroController gyroController;
            private float setSpeed;
            private double worldSpeedForward, worldSpeedRight, worldSpeedUp;
            private double pitch, roll;
            private double desiredPitch, desiredRoll;

            public HoverModule(ConfigReader config, GyroController gyroController)
            {
                this.config = config;
                this.gyroController = gyroController;

                gyroResponsiveness = config.Get<int>("gyroResponsiveness");
                maxPitch = config.Get<double>("maxPitch");
                maxRoll = config.Get<double>("maxRoll");

                actions.Add("stop", new ModuleAction((args) => { gyroController.OverrideGyros(false); }, null));

                actions.Add("hover", new ModuleAction(null, () =>
                {
                    desiredPitch = Math.Atan(worldSpeedForward / gyroResponsiveness) / Helpers.halfPi * maxPitch;
                    desiredRoll = Math.Atan(worldSpeedRight / gyroResponsiveness) / Helpers.halfPi * maxRoll;
                }));

                actions.Add("glide", new ModuleAction(null, () =>
                {
                    desiredPitch = 0;
                    desiredRoll = Math.Atan(worldSpeedRight / gyroResponsiveness) / Helpers.halfPi * maxRoll;
                }));

                actions.Add("freeglide", new ModuleAction(null, () =>
                {
                    desiredPitch = 0;
                    desiredRoll = 0;
                }));

                actions.Add("pitch", new ModuleAction(null, () =>
                {
                    desiredPitch = Math.Atan(worldSpeedForward / gyroResponsiveness) / Helpers.halfPi * maxPitch;
                    desiredRoll = (roll - 90);
                }));

                actions.Add("roll", new ModuleAction(null, () =>
                {
                    desiredPitch = -(pitch - 90);
                    desiredRoll = Math.Atan(worldSpeedRight / gyroResponsiveness) / Helpers.halfPi * maxRoll;
                }));
                
                actions.Add("cruise", new ModuleAction((string[] args) =>
                {
                    setSpeed = args[0] != null ? Int32.Parse(args[0]) : 0;
                }, () =>
                {
                    desiredPitch = Math.Atan((worldSpeedForward - setSpeed) / gyroResponsiveness) / Helpers.halfPi * maxPitch;
                    desiredRoll = Math.Atan(worldSpeedRight / gyroResponsiveness) / Helpers.halfPi * maxRoll;
                }));
                
            }

            protected override void OnSetAction()
            {
                gyroController.OverrideGyros(action.execute != null);
            }

            // TODO Auto toggle on when entering gravity
            public override void Tick()
            {
                base.Tick();

                if (!gyroController.inGravity)
                    SetAction("stop");

                if (gyroController.gyrosEnabled)
                {
                    CalcWorldSpeed();
                    CalcPitchAndRoll();
                    ExecuteManeuver();
                    PrintStatus();
                    PrintVelocity();
                    PrintOrientation();
                }
            }

            // TODO make current mode work again
            private void PrintStatus()
            {
                PrintLine("----- Status -------------------------------------------");
                PrintLine("Hover State: " + (gyroController.gyrosEnabled ? "ENABLED" : "DISABLED"));
                //PrintLine("Hover Mode: " + mode.ToUpper());
            }

            private void PrintVelocity()
            {
                PrintLine("\n----- Velocity ----------------------------------------");
                PrintLine("  F/B: " + worldSpeedForward.ToString("+000;\u2013000"));
                PrintLine("  R/L: " + worldSpeedRight.ToString("+000;\u2013000"));
                PrintLine("  U/D: " + worldSpeedUp.ToString("+000;\u2013000"));
            }

            private void PrintOrientation()
            {
                PrintLine("\n----- Orientation ----------------------------------------");
                PrintLine("Pitch: " + pitch.ToString("+00;\u201300") + "° | Roll: " + roll.ToString("+00;\u201300") + "°");
            }

            private void CalcWorldSpeed()
            {
                worldSpeedForward = Helpers.NotNan(Vector3D.Dot(gyroController.deltaPosition, Vector3D.Cross(gyroController.gravity, gyroController.worldOrientation.Right)) * gyroController.speed);
                worldSpeedRight = Helpers.NotNan(Vector3D.Dot(gyroController.deltaPosition, Vector3D.Cross(gyroController.gravity, gyroController.worldOrientation.Forward)) * gyroController.speed);
                worldSpeedUp = Helpers.NotNan(Vector3D.Dot(gyroController.deltaPosition, gyroController.gravity) * gyroController.speed);
            }

            private void CalcPitchAndRoll()
            {
                pitch = Helpers.NotNan(Math.Acos(Vector3D.Dot(gyroController.worldOrientation.Forward, gyroController.gravity)) * Helpers.radToDeg);
                roll = Helpers.NotNan(Math.Acos(Vector3D.Dot(gyroController.worldOrientation.Right, gyroController.gravity)) * Helpers.radToDeg);
            }

            private void ExecuteManeuver()
            {
                action?.execute();
                var quatPitch = Quaternion.CreateFromAxisAngle(gyroController.shipOrientation.Left, (float)(desiredPitch * Helpers.degToRad));
                var quatRoll = Quaternion.CreateFromAxisAngle(gyroController.shipOrientation.Backward, (float)(desiredRoll * Helpers.degToRad));
                var reference = Vector3D.Transform(gyroController.shipOrientation.Down, quatPitch * quatRoll);
                gyroController.SetTargetOrientation(reference, gyroController.remote.GetNaturalGravity());
            }
        }
    }
}
