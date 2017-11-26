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
        public class ModuleAction
        {
            public string name;
            public Action<string[]> initialize;
            public Action execute;

            public ModuleAction(string name, Action<string[]> initialize, Action execute)
            {
                this.name = name;
                this.initialize = initialize;
                this.execute = execute;
            }
        }
    }
}
