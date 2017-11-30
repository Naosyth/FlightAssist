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
        public class GyroController
        {
            const double minGyroRpmScale = 0.001;
            const double gyroVelocityScale = 0.2;

            private readonly List<IMyGyro> gyros;
            private readonly IMyShipController cockpit;
            public bool gyroOverride;
            private Vector3D reference;
            private Vector3D target;
            public double angle;

            public GyroController(List<IMyGyro> gyros, IMyShipController cockpit)
            {
                this.gyros = gyros;
                this.cockpit = cockpit;
            }

            public void Tick()
            {
                UpdateGyroRpm();
            }

            public void SetGyroOverride(bool state)
            {
                gyroOverride = state;
                for (int i = 0; i < gyros.Count; i++)
                    gyros[i].GyroOverride = gyroOverride;
            }

            public void SetTargetOrientation(Vector3D setReference, Vector3D setTarget)
            {
                reference = setReference;
                target = setTarget;
                UpdateGyroRpm();
            }

            private void UpdateGyroRpm()
            {
                if (!gyroOverride) return;

                for (int i = 0; i < gyros.Count; i++)
                {
                    var g = gyros[i];

                    Matrix localOrientation;
                    g.Orientation.GetMatrix(out localOrientation);
                    var localReference = Vector3D.Transform(reference, MatrixD.Transpose(localOrientation));
                    var localTarget = Vector3D.Transform(target, MatrixD.Transpose(g.WorldMatrix.GetOrientation()));

                    var axis = Vector3D.Cross(localReference, localTarget);
                    angle = axis.Length();
                    angle = Math.Atan2(angle, Math.Sqrt(Math.Max(0.0, 1.0 - angle * angle)));
                    if (Vector3D.Dot(localReference, localTarget) < 0)
                        angle = Math.PI;
                    axis.Normalize();
                    axis *= Math.Max(minGyroRpmScale, g.GetMaximum<float>("Roll") * (angle / Math.PI) * gyroVelocityScale);

                    g.Pitch = (float)-axis.X;
                    g.Yaw = (float)-axis.Y;
                    g.Roll = (float)-axis.Z;
                }
            }
        }
    }
}
