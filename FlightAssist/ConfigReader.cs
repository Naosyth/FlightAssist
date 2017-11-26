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
        public class ConfigReader
        {
            private Dictionary<string, string> config = new Dictionary<string, string>();
            private IMyProgrammableBlock pb;

            public ConfigReader(IMyProgrammableBlock pb)
            {
                this.pb = pb;
                if (pb.CustomData == "")
                    SetDefaults();
                else
                    ParseConfig();
            }

            public T Get<T>(string key)
            {
                string value;
                if (config.TryGetValue(key, out value))
                    return (T)Convert.ChangeType(value, typeof(T));
                return default(T);
            }

            private void ParseConfig()
            {
                string[] lines = pb.CustomData.Split('\n');

                foreach (string line in lines)
                {
                    var words = line.Split('=');
                    if (words.Length == 2)
                    {
                        string key = words[0].Trim();
                        string value = words[1].Trim();
                        config[key] = value;
                    }
                }
            }

            public void SetDefaults()
            {
                config.Clear();
                config.Add("remoteBlockName", "FA Remote");
                config.Add("textPanelName", "FA Screen");
                config.Add("gyroGroupName", "FA Gyros");
                config.Add("spaceMainThrust", "backward");
                config.Add("gyroResponsiveness", "8");
                config.Add("maxPitch", "45");
                config.Add("maxRoll", "45");
                config.Add("gyroVelocityScale", "0.2");
                config.Add("startCommand", "hover hover");
                SaveConfig();
            }

            private void SaveConfig()
            {
                StringBuilder configString = new StringBuilder();
                foreach (KeyValuePair<string, string> entry in config)
                    configString.Append(entry.Key + "=" + entry.Value + "\n");
                pb.CustomData = configString.ToString();
            }
        }
    }
}
