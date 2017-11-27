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

                AddAction("disabled", (args) => { gyroController.OverrideGyros(false); }, null);

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

                AddAction("pitch", null, () =>
                {
                    desiredPitch = Math.Atan(worldSpeedForward / gyroResponsiveness) / Helpers.halfPi * maxPitch;
                    desiredRoll = (roll - 90);
                });

                AddAction("roll", null, () =>
                {
                    desiredPitch = -(pitch - 90);
                    desiredRoll = Math.Atan(worldSpeedRight / gyroResponsiveness) / Helpers.halfPi * maxRoll;
                });

                AddAction("cruise", (string[] args) =>
                {
                    setSpeed = args[0] != null ? Int32.Parse(args[0]) : 0;
                }, () =>
                {
                    desiredPitch = Math.Atan((worldSpeedForward - setSpeed) / gyroResponsiveness) / Helpers.halfPi * maxPitch;
                    desiredRoll = Math.Atan(worldSpeedRight / gyroResponsiveness) / Helpers.halfPi * maxRoll;
                });
                
            }

            protected override void OnSetAction()
            {
                gyroController.OverrideGyros(action?.execute != null);
                if (action?.execute != null)
                    gyroController.remote.DampenersOverride = true;
            }

            // TODO Auto toggle on when entering gravity
            public override void Tick()
            {
                base.Tick();

                if (!gyroController.inGravity)
                    SetAction("disabled");

                CalcWorldSpeed();
                CalcPitchAndRoll();
                PrintStatus();
                PrintVelocity();
                PrintOrientation();

                if (gyroController.gyrosEnabled)
                    ExecuteManeuver();
            }

            private void PrintStatus()
            {
                PrintLine("    HOVER MODULE ACTIVE");
                PrintLine("    MODE: " + action?.name.ToUpper());
            }

            private void PrintVelocity()
            {
                PrintLine("\n Velocity (M/S)");
                string velocityString = " X:" + worldSpeedForward.ToString("+000;\u2013000");
                velocityString += " Y:" + worldSpeedRight.ToString("+000;\u2013000");
                velocityString += " Z:" + worldSpeedUp.ToString("+000;\u2013000");
                PrintLine(velocityString);
            }

            private void PrintOrientation()
            {
                PrintLine("\n Orientation");
                PrintLine(" Pitch: " + (90-pitch).ToString("+00;\u201300") + "° | Roll: " + ((90-roll)*-1).ToString("+00;\u201300") + "°");
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
