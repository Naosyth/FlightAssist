﻿using Sandbox.Game.EntityComponents;
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

            public VectorModule(ConfigReader config, GyroController gyroController)
            {
                this.gyroController = gyroController;

                thrustVector = GetThrustVector(config.Get<string>("spaceMainThrust"));

                AddAction("stop", (args) => { gyroController.OverrideGyros(false); }, null);
                AddAction("brake", (args) => { gyroController.remote.DampenersOverride = false; }, SpaceBrake);
                AddAction("prograde", null, () => { TargetOrientation(gyroController.deltaPosition); });
                AddAction("retrograde", null, () => { TargetOrientation(-gyroController.deltaPosition); });
            }

            protected override void OnSetAction()
            {
                gyroController.OverrideGyros(action.execute != null);
            }

            public override void Tick()
            {
                base.Tick();

                if (gyroController.gyrosEnabled)
                    action?.execute();
            }

            private void TargetOrientation(Vector3D target)
            {
                gyroController.SetTargetOrientation(thrustVector, target);
            }

            private void SpaceBrake()
            {
                if (gyroController.inGravity)
                {
                    SetAction("stop");
                    return;
                }

                TargetOrientation(gyroController.deltaPosition);

                if (Helpers.EqualWithMargin(gyroController.angle, 0, angleThreshold))
                    gyroController.remote.DampenersOverride = true;

                // Stop when velocity is nearly 0
                if (gyroController.speed < speedThreshold)
                    SetAction("stop");
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
