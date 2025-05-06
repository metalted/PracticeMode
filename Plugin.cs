using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using UnityEngine;

namespace PracticeMode
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string pluginGUID = "com.metalted.zeepkist.practicemode";
        public const string pluginName = "Practice Mode";
        public const string pluginVersion = "1.6";
        public static Plugin Instance;

        public ConfigEntry<KeyCode> enableModKey;
        public ConfigEntry<bool> modEnabled;

        private void Awake()
        {
            Harmony harmony = new Harmony(pluginGUID);
            harmony.PatchAll();

            // Plugin startup logic
            Logger.LogInfo($"Plugin {pluginName} is loaded!");

            Instance = this;

            PracticeX.Initialize();
            GUIStyleX.Initialize();

            enableModKey = Config.Bind("Settings", "Toggle Plugin Functionality", KeyCode.None, "Toggle the plugin on or off.");
            modEnabled = Config.Bind("Settings", "Plugin Enabled", true, "Is the plugin currently enabled?");
        }     
        
        private void OnGUI()
        {
            if (modEnabled.Value)
            {
                PracticeX.DoGUI();
            }
        }

        private void Update()
        {
            if(Input.GetKeyDown(enableModKey.Value))
            {
                modEnabled.Value = !modEnabled.Value;
                PlayerManager.Instance.messenger.Log("Practice Mode: " + (modEnabled.Value ? "On" : "Off"), 2f);
            }

            if (modEnabled.Value)
            {
                PracticeX.DoUpdate();
            }
        }

        public void Log(string message, int level = 0)
        {
            switch (level)
            {
                default:
                case 0:
                    Logger.LogInfo(message);
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(GameMaster), "SpawnPlayers")]
    public class SetupGameSpawnPlayers
    {
        public static bool Prefix(GameMaster __instance)
        {
            if (!Plugin.Instance.modEnabled.Value)
            {
                return true;
            }

            return PracticeX.OnSpawnPlayers(__instance);
        }
    }

    [HarmonyPatch(typeof(GameMaster), "RestartLevel")]
    public class GameMasterRestartLevel
    {
        public static void Prefix(GameMaster __instance)
        {
            PracticeX.OnRestartLevel(__instance);
        }
    }

    [HarmonyPatch(typeof(GameMaster), "ReleaseTheZeepkists")]
    public static class SetupGameReleaseTheZeepkistsPatch
    {
        public static void Postfix(GameMaster __instance)
        {
            if (!Plugin.Instance.modEnabled.Value)
            {
                return;
            }

            PracticeX.OnReleaseTheZeepkists(__instance);
        }
    }

    [HarmonyPatch(typeof(MainMenuUI), "Awake")]
    public static class MainMenuUIAwake
    {
        public static void Postfix(MainMenuUI __instance)
        {
            PracticeX.OnMainMenu(__instance);
        }
    }

    [HarmonyPatch(typeof(LEV_SaveLoad), "AreYouSure")]
    public class LEV_SaveLoadAreYouSure
    {
        public static void Postfix(LEV_SaveLoad __instance)
        {
            PracticeX.OnSaveLoad(__instance);
        }
    }

    [HarmonyPatch(typeof(LEV_LevelEditorCentral), "Awake")]
    public class LEV_CentralAwakePatch
    {
        public static void Postfix(LEV_LevelEditorCentral __instance)
        {
            PracticeX.OnLevelEditor(__instance);
        }
    }
}
