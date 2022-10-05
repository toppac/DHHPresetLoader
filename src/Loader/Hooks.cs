using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace DHHPresetLoader
{
    public static class Hooks
    {
        //public static Action<string> LoadPreset { get; set; }
        //public static Action<string> SavePreset { get; set; }
        public static bool PanelOn = false;


        public static void GetDHHScript(ref object ___dhhRuntimeScript)
        { 
            if (___dhhRuntimeScript != null)
            {
                DHHPresetLoader.Instance.DhhRuntimeScript = ___dhhRuntimeScript;
                DHHPresetLoader.Instance.InitMethod();
            }
            else
            {
                DHHPresetLoader.Instance.LogError("Get DHHRunScript failed!");
            }
        }

        public static void GetMenuOn(ref bool ___menuOn)
        {
            PanelOn = ___menuOn;
        }

        public static void GetAI4MenuOn(ref bool ___MenuOn)
        {
            PanelOn = ___MenuOn;
        }

        internal static void Clear()
        {
            //LoadPreset = null;
            //SavePreset = null;
            PanelOn = false;
        }
    }
}
