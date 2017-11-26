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
        public class Helpers
        {
            public const double halfPi = Math.PI / 2;
            public const double radToDeg = 180 / Math.PI;
            public const double degToRad = Math.PI / 180;

            public static double NotNan(double val)
            {
                if (double.IsNaN(val)) return 0;
                return val;
            }

            public static bool EqualWithMargin(double value, double target, double margin)
            {
                return value > target - margin && value < target + margin;
            }

            public static string GetCommandFromArgs(string[] args)
            {
                if (args[1] != null)
                {
                    return args[1].ToLower();
                } else
                {
                    return "";
                }
            }

            public static void PrintException(string error)
            {
                throw new Exception("Error: " + error);
            }
        }
    }
}
