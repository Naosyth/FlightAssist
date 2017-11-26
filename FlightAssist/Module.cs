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
        public abstract class Module
        {
            protected ModuleAction action;
            protected Dictionary<string, ModuleAction> actions = new Dictionary<string, ModuleAction>();
            private string printBuffer;

            public virtual void Tick() { printBuffer = ""; }

            public virtual void ProcessCommand(string[] args) {
                SetAction(Helpers.GetCommandFromArgs(args), args.Skip(2).ToArray());
            }

            protected void AddAction(string name, Action<string[]> initialize, Action execute)
            {
                actions.Add(name, new ModuleAction(name, initialize, execute));
            }
      
            protected bool SetAction(string actionName)
            {
                return SetAction(actionName, null);
            }

            protected bool SetAction(string actionName, string[] args)
            {
                if (!actions.Keys.Contains<string>(actionName))
                    return false;

                if (actions.TryGetValue(actionName, out action))
                {
                    action.initialize?.Invoke(args);
                    OnSetAction();
                    return true;
                }
                return false;
            }

            protected virtual void OnSetAction() { }

            public string GetPrintString() { return printBuffer; }
            protected void PrintLine(string line) { printBuffer += line + "\n"; }
        }
    }
}
