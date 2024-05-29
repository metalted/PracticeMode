using BepInEx;
using UnityEngine;
using HarmonyLib;
using ZeepSDK.LevelEditor;
using System.Collections.Generic;
using BepInEx.Configuration;
using System.Linq;
using ZeepSDK.Racing;

namespace PracticeMode
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string pluginGUID = "com.metalted.zeepkist.practicemode";
        public const string pluginName = "Practice Mode";
        public const string pluginVersion = "1.3";

        public static Plugin Instance;
        public SoapboxRecorder recorder = new SoapboxRecorder();

        //Should we record right now?
        public bool record = false;

        //The soapbox in the level editor for position visualization.
        public GameObject soapboxVisualizer = null;

        // Selected frame for test mode
        public SoapboxRecorderFrame selectedFrame = null;

        //Timeline selection slider.
        private float previousSliderValue = 0.0f;
        private float sliderValue = 0.0f;
        private float minTime = 0.0f;
        private float maxTime = 0.0f;
        private bool checkpointFlag = false;

        //Where in the game we are.
        public enum GameState { Other, Editor, TestMode };
        public GameState gameState = GameState.Other;

        private void Awake()
        {
            Harmony harmony = new Harmony(pluginGUID);
            harmony.PatchAll();

            // Plugin startup logic
            Logger.LogInfo($"Plugin {pluginName} is loaded!");

            Instance = this;

            LevelEditorApi.EnteredTestMode += () => { SetGameState(GameState.TestMode); };
            LevelEditorApi.EnteredLevelEditor += () => { SetGameState(GameState.Editor); };
            LevelEditorApi.LevelLoaded += () => { recorder.Clear(); soapboxVisualizer.SetActive(false); };

            RacingApi.PassedCheckpoint += (time) => { if (record) { checkpointFlag = true; } else { checkpointFlag = false; } };
        }

        public void SetGameState(GameState gameState)
        {
            this.gameState = gameState;

            switch (gameState)
            {
                case GameState.Editor:

                    //No need for recording in the editor.
                    record = false;

                    if (recorder.frames.Count > 0)
                    {
                        //Update the slider range.
                        minTime = recorder.GetFirstFrame().time;
                        maxTime = recorder.GetLastFrame().time;

                        //Set to min if out of range.
                        if (sliderValue < minTime || sliderValue > maxTime)
                        {
                            sliderValue = minTime;
                        }

                        //Show the visualizer.
                        Plugin.Instance.soapboxVisualizer.SetActive(true);

                        //Get the frame based on the slider value.
                        int frameIndex = recorder.GetFrameIndexByTime(sliderValue);
                        if (frameIndex != -1)
                        {
                            recorder.SetCurrentFrameIndex(frameIndex);
                            SoapboxRecorderFrame frame = recorder.GetCurrentFrame();
                            if (frame != null)
                            {
                                SetIndicator(frame);
                            }
                        }
                    }
                    break;

                case GameState.TestMode:
                    soapboxVisualizer.SetActive(false);
                    break;
                case GameState.Other:
                    if (soapboxVisualizer != null)
                    {
                        soapboxVisualizer.SetActive(false);
                    }
                    break;
            }
        }

        private void Update()
        {
            if (gameState == GameState.TestMode)
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
                    if (lastFrame != null)
                    {
                        if (PlayerManager.Instance.currentMaster.currentLevelPhysicsTime + addedTime == lastFrame.time)
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
                        time = PlayerManager.Instance.currentMaster.currentLevelPhysicsTime + addedTime,
                        isCheckpoint = checkpointFlag
                    };

                    checkpointFlag = false;

                    recorder.AddFrame(newFrame);
                }
            }
        }

        private void SetIndicator(SoapboxRecorderFrame frame)
        {
            soapboxVisualizer.transform.position = frame.position;
            soapboxVisualizer.transform.rotation = frame.rotation;
        }

        private void OnGUI()
        {
            if (gameState == GameState.Editor && recorder.frames.Count > 0)
            {
                if (GUI.Button(new Rect(10, Screen.height * 0.15f + 10, 30, 30), "<C"))
                {
                    recorder.PreviousCheckpointFrame();
                    sliderValue = recorder.GetCurrentFrame().time;
                }

                if (GUI.Button(new Rect(50, Screen.height * 0.15f + 10, 30, 30), "<"))
                {
                    recorder.PreviousFrame();
                    sliderValue = recorder.GetCurrentFrame().time;
                }

                float newSliderValue = GUI.HorizontalSlider(new Rect(90, Screen.height * 0.15f + 10, Screen.width * 0.375f, 30), sliderValue, minTime, maxTime);
                GUI.Label(new Rect(100 + Screen.width * 0.375f, Screen.height * 0.15f + 10, 100, 30), "Time: " + newSliderValue.ToString("F2"));

                if (GUI.Button(new Rect(210 + Screen.width * 0.375f, Screen.height * 0.15f + 10, 30, 30), ">"))
                {
                    recorder.NextFrame();
                    sliderValue = recorder.GetCurrentFrame().time;
                }

                if (GUI.Button(new Rect(250 + Screen.width * 0.375f, Screen.height * 0.15f + 10, 30, 30), "C>"))
                {
                    recorder.NextCheckpointFrame();
                    sliderValue = recorder.GetCurrentFrame().time;
                }

                if(GUI.Button(new Rect(290 + Screen.width * 0.375f, Screen.height * 0.15f + 10, 60, 30), "Reset"))
                {
                    recorder.Clear(); 
                    soapboxVisualizer.SetActive(false);
                    sliderValue = 0;
                }

                if (Mathf.Abs(newSliderValue - previousSliderValue) > Mathf.Epsilon)
                {
                    sliderValue = newSliderValue;
                    int frameIndex = recorder.GetFrameIndexByTime(sliderValue);
                    if (frameIndex != -1)
                    {
                        recorder.SetCurrentFrameIndex(frameIndex);
                        SoapboxRecorderFrame frame = recorder.GetCurrentFrame();
                        if (frame != null)
                        {
                            SetIndicator(frame);
                        }
                    }
                    previousSliderValue = newSliderValue;
                }
            }
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
            if (Plugin.Instance.gameState == Plugin.GameState.Other)
            {
                return true;
            }

            SoapboxRecorderFrame frame = Plugin.Instance.recorder.GetCurrentFrame();
            if (frame != null)
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

            if (Plugin.Instance.selectedFrame != null)
            {
                Plugin.Instance.SetPlayerVelocitiesOnRelease(__instance, Plugin.Instance.selectedFrame);
            }

            Plugin.Instance.record = true;
        }
    }

    [HarmonyPatch(typeof(MainMenuUI), "Awake")]
    public static class MainMenuUIAwake
    {
        public static void Postfix(MainMenuUI __instance)
        {
            Plugin.Instance.SetGameState(Plugin.GameState.Other);
            Plugin.Instance.recorder.RewindToStart();
            if (Plugin.Instance.soapboxVisualizer != null) { return; }

            NetworkedGhostSpawner networkedGhostSpawner = GameObject.FindObjectOfType<NetworkedGhostSpawner>();
            NetworkedZeepkistGhost networkedZeepkistGhost = networkedGhostSpawner.zeepkistGhostPrefab;
            Transform soapboxOriginal = networkedZeepkistGhost.ghostModel.transform;
            Plugin.Instance.soapboxVisualizer = GameObject.Instantiate(soapboxOriginal.gameObject);
            GameObject.DontDestroyOnLoad(Plugin.Instance.soapboxVisualizer);
            Plugin.Instance.soapboxVisualizer.SetActive(false);
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
