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
            public readonly IMyRemoteControl remote;
            public Matrix shipOrientation;
            public MatrixD worldOrientation;
            private Vector3D position;
            public Vector3D deltaPosition;
            private Vector3D oldPosition;
            public double speed;
            public double localSpeedUp;
            public double localSpeedRight;
            public double localSpeedForward;
            public Vector3D gravity;
            public bool inGravity;
            private bool switchingGravity;
            public bool gyrosEnabled;
            private Vector3D reference;
            private Vector3D target;
            public double angle;
            private double dt = 1000 / 60;

            public GyroController(List<IMyGyro> gyros, IMyRemoteControl remote)
            {
                this.gyros = gyros;
                this.remote = remote;

                remote.Orientation.GetMatrix(out shipOrientation);
            }

            public void Update()
            {
                CalcVelocity();
                CalcGravity();
                UpdateGyroRpm();
            }

            private void CalcVelocity()
            {
                worldOrientation = remote.WorldMatrix;
                position = remote.GetPosition();
                deltaPosition = position - oldPosition;
                oldPosition = position;
                speed = deltaPosition.Length() / dt * 1000;
                deltaPosition.Normalize();

                localSpeedUp = Helpers.NotNan(Vector3D.Dot(deltaPosition, worldOrientation.Up) * speed);
                localSpeedRight = Helpers.NotNan(Vector3D.Dot(deltaPosition, worldOrientation.Right) * speed);
                localSpeedForward = Helpers.NotNan(Vector3D.Dot(deltaPosition, worldOrientation.Forward) * speed);
            }

            private void CalcGravity()
            {
                gravity = -Vector3D.Normalize(remote.GetNaturalGravity());
                switchingGravity = inGravity;
                inGravity = !double.IsNaN(gravity.X);
                switchingGravity = (inGravity != switchingGravity);
            }

            public void OverrideGyros(bool state)
            {
                gyrosEnabled = state;
                for (int i = 0; i < gyros.Count; i++)
                    gyros[i].SetValueBool("Override", gyrosEnabled);
            }

            public void SetTargetOrientation(Vector3D setReference, Vector3D setTarget)
            {
                reference = setReference;
                target = setTarget;
                UpdateGyroRpm();
            }

            private void UpdateGyroRpm()
            {
                if (!gyrosEnabled) return;

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

                    g.SetValueFloat("Pitch", (float)axis.X);
                    g.SetValueFloat("Yaw", (float)-axis.Y);
                    g.SetValueFloat("Roll", (float)-axis.Z);
                }
            }
        }
    }
}
