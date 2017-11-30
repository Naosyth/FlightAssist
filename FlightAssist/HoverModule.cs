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

            private readonly CustomDataConfig config;
            private readonly GyroController gyroController;
            private readonly IMyShipController cockpit;
            private int smartDelayTimer;
            private float setSpeed;
            private double worldSpeedForward, worldSpeedRight, worldSpeedUp;
            private double pitch, roll;
            private double desiredPitch, desiredRoll;

            public HoverModule(CustomDataConfig config, GyroController gyroController, IMyShipController cockpit)
            {
                this.config = config;
                this.gyroController = gyroController;
                this.cockpit = cockpit;

                gyroResponsiveness = config.Get<int>("gyroResponsiveness");
                maxPitch = config.Get<double>("maxPitch");
                maxRoll = config.Get<double>("maxRoll");

                AddAction("disabled", (args) => { gyroController.SetGyroOverride(false); }, null);

                AddAction("smart", (string[] args) =>
                {
                    smartDelayTimer = 0;
                    setSpeed = (args.Length > 0 && args[0] != null) ? Int32.Parse(args[0]) : 0;
                }, () =>
                {
                    if (cockpit.MoveIndicator.Length() > 0.0f || cockpit.RotationIndicator.Length() > 0.0f)
                    {
                        desiredPitch = -(pitch - 90);
                        desiredRoll = (roll - 90);
                        gyroController.SetGyroOverride(false);
                        smartDelayTimer = 0;
                    } else if (smartDelayTimer > config.Get<int>("smartDelayTime"))
                    {
                        gyroController.SetGyroOverride(true);
                        desiredPitch = Math.Atan((worldSpeedForward - setSpeed) / gyroResponsiveness) / Helpers.halfPi * maxPitch;
                        desiredRoll = Math.Atan(worldSpeedRight / gyroResponsiveness) / Helpers.halfPi * maxRoll;
                    } else
                        smartDelayTimer++;
                });

                AddAction("stop", null, () =>
                {
                    desiredPitch = Math.Atan(worldSpeedForward / gyroResponsiveness) / Helpers.halfPi * maxPitch;
                    desiredRoll = Math.Atan(worldSpeedRight / gyroResponsiveness) / Helpers.halfPi * maxRoll;
                });

                AddAction("glide", null, () =>
                {
                    desiredPitch = 0;
                    desiredRoll = Math.Atan(worldSpeedRight / gyroResponsiveness) / Helpers.halfPi * maxRoll;
                });

                AddAction("freeglide", null, () =>
                {
                    desiredPitch = 0;
                    desiredRoll = 0;
                });
            }

            protected override void OnSetAction()
            {
                gyroController.SetGyroOverride(action?.execute != null);
                if (action?.execute != null)
                    cockpit.DampenersOverride = true;
            }

            public override void Tick()
            {
                base.Tick();

                if (cockpit.GetNaturalGravity().Length() == 0)
                    SetAction("disabled");

                CalcWorldSpeed();
                CalcPitchAndRoll();
                PrintStatus();
                if (cockpit.GetNaturalGravity().Length() > 0)
                {
                    PrintVelocity();
                    PrintOrientation();
                } else
                    PrintLine("\n\n   No Planetary Gravity");

                if (action?.execute != null)
                    action?.execute();
                if (gyroController.gyroOverride)
                    ExecuteManeuver();
            }

            private void PrintStatus()
            {
                PrintLine("    HOVER MODULE ACTIVE");
                PrintLine("    MODE: " + action?.name.ToUpper());
                if (setSpeed > 0)
                    PrintLine("    SET SPEED: " + setSpeed + "m/s");
                else
                    PrintLine("");
            }

            private void PrintVelocity()
            {
                string velocityString = " X:" + worldSpeedForward.ToString("+000;\u2013000");
                velocityString += " Y:" + worldSpeedRight.ToString("+000;\u2013000");
                velocityString += " Z:" + worldSpeedUp.ToString("+000;\u2013000");
                PrintLine("\n Velocity (m/s)+\n" + velocityString);
            }

            private void PrintOrientation()
            {
                PrintLine("\n Orientation");
                PrintLine(" Pitch: " + (90-pitch).ToString("+00;\u201300") + "° | Roll: " + ((90-roll)*-1).ToString("+00;\u201300") + "°");
            }

            private void CalcWorldSpeed()
            {
                Vector3D linearVelocity = Vector3D.Normalize(cockpit.GetShipVelocities().LinearVelocity);
                Vector3D gravity = -Vector3D.Normalize(cockpit.GetNaturalGravity());
                worldSpeedForward = Helpers.NotNan(Vector3D.Dot(linearVelocity, Vector3D.Cross(gravity, cockpit.WorldMatrix.Right)) * cockpit.GetShipSpeed());
                worldSpeedRight = Helpers.NotNan(Vector3D.Dot(linearVelocity, Vector3D.Cross(gravity, cockpit.WorldMatrix.Forward)) * cockpit.GetShipSpeed());
                worldSpeedUp = Helpers.NotNan(Vector3D.Dot(linearVelocity, gravity) * cockpit.GetShipSpeed());
            }

            private void CalcPitchAndRoll()
            {
                Vector3D gravity = -Vector3D.Normalize(cockpit.GetNaturalGravity());
                pitch = Helpers.NotNan(Math.Acos(Vector3D.Dot(cockpit.WorldMatrix.Forward, gravity)) * Helpers.radToDeg);
                roll = Helpers.NotNan(Math.Acos(Vector3D.Dot(cockpit.WorldMatrix.Right, gravity)) * Helpers.radToDeg);
            }

            private void ExecuteManeuver()
            {
                Matrix cockpitOrientation;
                cockpit.Orientation.GetMatrix(out cockpitOrientation);
                var quatPitch = Quaternion.CreateFromAxisAngle(cockpitOrientation.Left, (float)(desiredPitch * Helpers.degToRad));
                var quatRoll = Quaternion.CreateFromAxisAngle(cockpitOrientation.Backward, (float)(desiredRoll * Helpers.degToRad));
                var reference = Vector3D.Transform(cockpitOrientation.Down, quatPitch * quatRoll);
                gyroController.SetTargetOrientation(reference, cockpit.GetNaturalGravity());
            }
        }
    }
}
