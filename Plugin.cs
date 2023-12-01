using BepInEx;
using UnityEngine;
using HarmonyLib;
using ZeepSDK.LevelEditor;
using System.Collections.Generic;
using BepInEx.Configuration;
using System.Linq;

namespace PracticeMode
{
    public class SoapboxRecorderFrame
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public float time;
    }

    public class SoapboxRecorder
    {
        public List<SoapboxRecorderFrame> frames;
        public int currentFrameIndex = 0;

        public SoapboxRecorder()
        {
            frames = new List<SoapboxRecorderFrame>();
        }

        public void AddFrame(SoapboxRecorderFrame frame)
        {
            frames.Add(frame);
        }

        public void Clear()
        {
            frames.Clear();
            currentFrameIndex = 0;
        }

        public void AdvanceFrame()
        {
            if (currentFrameIndex < frames.Count - 1)
            {
                currentFrameIndex++;
            }
        }

        public void AdvanceSecond()
        {
            try
            {
                float currentTime = frames[currentFrameIndex].time;
                while (currentFrameIndex < frames.Count - 1 && frames[currentFrameIndex + 1].time <= currentTime + 1.0f)
                {
                    currentFrameIndex++;
                }
            }
            catch { }
        }

        public void AdvanceMinute()
        {
            try { 
                float currentTime = frames[currentFrameIndex].time;
                while (currentFrameIndex < frames.Count - 1 && frames[currentFrameIndex + 1].time <= currentTime + 60.0f)
                {
                    currentFrameIndex++;
                }
            }
            catch { }
        }

        public void RewindFrame()
        {
            if (currentFrameIndex > 0)
            {
                currentFrameIndex--;
            }
        }

        public void RewindSecond()
        {
            try
            {
                float currentTime = frames[currentFrameIndex].time;
                while (currentFrameIndex > 0 && frames[currentFrameIndex - 1].time >= currentTime - 1.0f)
                {
                    currentFrameIndex--;
                }
            }
            catch { }
        }

        public void RewindMinute()
        {
            try
            {
                float currentTime = frames[currentFrameIndex].time;
                while (currentFrameIndex > 0 && frames[currentFrameIndex - 1].time >= currentTime - 60.0f)
                {
                    currentFrameIndex--;
                }
            }
            catch { }
        }

        public void AdvanceToEnd()
        {
            currentFrameIndex = frames.Count - 1;
        }

        public void RewindToStart()
        {
            currentFrameIndex = 0;
        }

        public SoapboxRecorderFrame GetCurrentFrame()
        {
            if (currentFrameIndex >= 0 && currentFrameIndex < frames.Count)
            {
                return frames[currentFrameIndex];
            }
            else
            {
                return null;
            }
        }

        public void RemoveFramesAfterCurrent()
        {
            if (currentFrameIndex >= 0 && currentFrameIndex < frames.Count)
            {
                frames.RemoveRange(currentFrameIndex + 1, frames.Count - currentFrameIndex - 1);
            }
        }

        public SoapboxRecorderFrame GetLastFrame()
        {
            if (frames.Count > 0)
            {
                return frames[frames.Count - 1];
            }
            else
            {
                return null;
            }
        }
    }

    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string pluginGUID = "com.metalted.zeepkist.practicemode";
        public const string pluginName = "Practice Mode";
        public const string pluginVersion = "1.0";
        
        public static Plugin Instance;
        public SoapboxRecorder recorder = new SoapboxRecorder();
        public bool record = false;

        //Settings
        public ConfigEntry<KeyCode> toggleModelKey;
        public ConfigEntry<KeyCode> backwardKey;
        public ConfigEntry<KeyCode> forwardKey;
        public ConfigEntry<KeyCode> timeScaleCycleKey;
        public ConfigEntry<KeyCode> clearKey;

        //Gameobject to show the soapbox in the level editor.
        public GameObject soapboxStateIndicator = null;

        //Where in the game we are.
        public enum GameState { Other, Editor, TestMode};
        public GameState gameState = GameState.Other;

        //The select time scale for moving the timeline.
        public enum TimeScale { Frame, Second, Minute};
        public TimeScale timeScale = TimeScale.Frame;

        //The starter frame
        public SoapboxRecorderFrame selectedFrame = null;
        
        private void Awake()
        {
            Harmony harmony = new Harmony(pluginGUID);
            harmony.PatchAll();

            // Plugin startup logic
            Logger.LogInfo($"Plugin {pluginName} is loaded!");

            Instance = this;

            LevelEditorApi.EnteredTestMode += () => { SetGameState(GameState.TestMode); };
            LevelEditorApi.EnteredLevelEditor += () => { SetGameState(GameState.Editor); };
            LevelEditorApi.LevelLoaded += () => { recorder.Clear(); soapboxStateIndicator.SetActive(false); };

            toggleModelKey = Config.Bind("Controls", "Toggle Model", KeyCode.Keypad8, "");
            backwardKey = Config.Bind("Controls", "Rewind", KeyCode.Keypad4, "");
            forwardKey = Config.Bind("Controls", "Fast Forward", KeyCode.Keypad6, "");
            timeScaleCycleKey = Config.Bind("Controls", "Time Cycle", KeyCode.Keypad5, "");
            clearKey = Config.Bind("Controls", "Clear Recording", KeyCode.Keypad2, "");
        }

        public void SetGameState(GameState gameState)
        {
            this.gameState = gameState;

            switch (gameState)
            {
                case GameState.Editor:                   
                    break;
                case GameState.TestMode:
                    //Always make sure the model is hidden when going into test mode.
                    soapboxStateIndicator.SetActive(false);
                    break;
                case GameState.Other:
                    //Always make sure the model is hidden when leaving the level editor.
                    if (soapboxStateIndicator != null)
                    {
                        soapboxStateIndicator.SetActive(false);
                    }
                    break;
            }
        }     

        private void Update()
        {
            switch(gameState)
            {
                case GameState.TestMode:
                    TestModeUpdate();
                    break;
                case GameState.Editor:
                    record = false;
                    EditorUpdate();
                    break;
            }
        }

        private void EditorUpdate()
        {
            //Timescale selection input.
            if (Input.GetKeyDown((KeyCode)timeScaleCycleKey.BoxedValue))
            {
                switch (timeScale)
                {
                    case TimeScale.Frame:
                        timeScale = TimeScale.Second;
                        break;
                    case TimeScale.Second:
                        timeScale = TimeScale.Minute;
                        break;
                    case TimeScale.Minute:
                        timeScale = TimeScale.Frame;
                        break;
                }

                PlayerManager.Instance.messenger.Log("Time Scale: " + timeScale.ToString(), 1f);
            }

            //Hide/show model
            if (Input.GetKeyDown((KeyCode)toggleModelKey.BoxedValue))
            {
                SoapboxRecorderFrame frame = recorder.GetCurrentFrame();

                //No frame available.
                if(frame == null)
                {
                    soapboxStateIndicator.SetActive(false);
                }
                else
                {
                    bool show = !soapboxStateIndicator.activeSelf;
                    soapboxStateIndicator.SetActive(!soapboxStateIndicator.activeSelf);

                    if(show)
                    {
                        SetIndicator(frame);
                    }                    
                }
            }

            //Move timeline backward.
            if (Input.GetKeyDown((KeyCode)backwardKey.BoxedValue))
            {
                switch (timeScale)
                {
                    case TimeScale.Frame:
                        recorder.RewindFrame();
                        break;
                    case TimeScale.Second:
                        recorder.RewindSecond();
                        break;
                    case TimeScale.Minute:
                        recorder.RewindMinute();
                        break;
                }

                SoapboxRecorderFrame frame = recorder.GetCurrentFrame();
                if (frame == null)
                {
                    soapboxStateIndicator.SetActive(false);
                }
                else
                {
                    SetIndicator(frame);
                }
            }

            //Move timeline forward.
            if (Input.GetKeyDown((KeyCode)forwardKey.BoxedValue))
            {
                switch (timeScale)
                {
                    case TimeScale.Frame:
                        recorder.AdvanceFrame();
                        break;
                    case TimeScale.Second:
                        recorder.AdvanceSecond();
                        break;
                    case TimeScale.Minute:
                        recorder.AdvanceMinute();
                        break;
                }

                SoapboxRecorderFrame frame = recorder.GetCurrentFrame();
                if (frame == null)
                {
                    soapboxStateIndicator.SetActive(false);
                }
                else
                {
                    SetIndicator(frame);
                }
            }

            //Clear key
            if(Input.GetKeyDown((KeyCode) clearKey.BoxedValue))
            {
                recorder.Clear();
                PlayerManager.Instance.messenger.Log("Clear Recorder", 1f);
            }
        }

        private void TestModeUpdate()
        {
            if (record)
            {
                SetupCar sc = PlayerManager.Instance.currentMaster.carSetups[0];
                if (sc.reseter.finished)
                {
                    record = false;
                    return;
                }

                float addedTime = selectedFrame != null ? selectedFrame.time : 0;

                SoapboxRecorderFrame lastFrame = recorder.GetLastFrame();
                if(lastFrame != null)
                {
                    if(PlayerManager.Instance.currentMaster.currentLevelPhysicsTime + addedTime == lastFrame.time)
                    {
                        return;
                    }
                }

                SoapboxRecorderFrame newFrame = new SoapboxRecorderFrame()
                {
                    position = sc.transform.position,
                    rotation = sc.transform.rotation,
                    velocity = sc.cc.GetRB().velocity,
                    angularVelocity = sc.cc.GetRB().angularVelocity,
                    time = PlayerManager.Instance.currentMaster.currentLevelPhysicsTime + addedTime
                };

                recorder.AddFrame(newFrame);
            }
        }

        private void SetIndicator(SoapboxRecorderFrame frame)
        {
            soapboxStateIndicator.transform.position = frame.position;
            soapboxStateIndicator.transform.rotation = frame.rotation;
            PlayerManager.Instance.messenger.Log("Current Time: " + frame.time, 1f);
        }      

        public void SpawnPlayer(GameMaster gameMaster, SoapboxRecorderFrame frame)
        {
            SetupCar setupCar = GameObject.Instantiate<SetupCar>(gameMaster.soapboxPrefab);
            setupCar.transform.position = frame.position;
            setupCar.transform.rotation = frame.rotation;
            setupCar.DoCarSetupSingleplayer();

            gameMaster.PlayersReady.Add(setupCar.GetComponent<ReadyToReset>());
            gameMaster.PlayersReady[0].GiveMaster(gameMaster, 0);
            gameMaster.PlayersReady[0].screenPointer = gameMaster.PlayerScreensUI.GetScreen(0);
            gameMaster.PlayersReady[0].WakeScreenPointer();
            gameMaster.playerResults.Add(new WinCompare.Result(0, 0.0f, 0));
            gameMaster.carSetups.Add(setupCar);
            setupCar.cc.GetRB().collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            setupCar.cc.GetRB().isKinematic = true;
        }

        public void SetPlayerVelocitiesOnRelease(GameMaster gameMaster, SoapboxRecorderFrame frame)
        {
            gameMaster.carSetups[0].cc.GetRB().velocity = frame.velocity;
            gameMaster.carSetups[0].cc.GetRB().angularVelocity = frame.angularVelocity;
        }
    }    

    [HarmonyPatch(typeof(GameMaster), "SpawnPlayers")]
    public class SetupGameSpawnPlayers
    {
        public static bool Prefix(GameMaster __instance)
        {
            //Spawn normally if not in test mode.
            if(Plugin.Instance.gameState == Plugin.GameState.Other)
            {
                return true;
            }

            //If a frame is selected / available, make sure to remove all the next frames and then set the player at the right position.
            SoapboxRecorderFrame frame = Plugin.Instance.recorder.GetCurrentFrame();
            if(frame != null)
            {
                Plugin.Instance.recorder.RemoveFramesAfterCurrent();
                Plugin.Instance.SpawnPlayer(__instance, frame);
                Plugin.Instance.selectedFrame = frame;
                return false;
            }
            else
            {
                Plugin.Instance.selectedFrame = null;
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(GameMaster), "ReleaseTheZeepkists")]
    public static class SetupGameReleaseTheZeepkistsPatch
    {
        public static void Postfix(GameMaster __instance)
        {
            if (Plugin.Instance.gameState == Plugin.GameState.Other)
            {
                return;
            }

            if(Plugin.Instance.selectedFrame != null)
            {
                Plugin.Instance.SetPlayerVelocitiesOnRelease(__instance, Plugin.Instance.selectedFrame);
            }          

            Plugin.Instance.record = true;
        }
    }


    [HarmonyPatch(typeof(MainMenuUI), "Awake")]
    public static class MainMenuUIAwake
    {
        public static void Postfix()
        {
            Plugin.Instance.SetGameState(Plugin.GameState.Other);
            Plugin.Instance.recorder.RewindToStart();
            if (Plugin.Instance.soapboxStateIndicator != null) { return; }

            NetworkedGhostSpawner networkedGhostSpawner = GameObject.FindObjectOfType<NetworkedGhostSpawner>();
            NetworkedZeepkistGhost networkedZeepkistGhost = networkedGhostSpawner.zeepkistGhostPrefab;
            Transform soapboxOriginal = networkedZeepkistGhost.ghostModel.transform;
            Plugin.Instance.soapboxStateIndicator = GameObject.Instantiate(soapboxOriginal.gameObject);
            GameObject.DontDestroyOnLoad(Plugin.Instance.soapboxStateIndicator);
            Plugin.Instance.soapboxStateIndicator.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(LEV_SaveLoad), "AreYouSure")]
    public class LEV_SaveLoadAreYouSure
    {
        public static void Postfix()
        {
            Plugin.Instance.recorder.RewindToStart();
        }
    }
}
