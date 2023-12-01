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
            float currentTime = frames[currentFrameIndex].time;
            while (currentFrameIndex < frames.Count - 1 && frames[currentFrameIndex + 1].time <= currentTime + 1.0f)
            {
                currentFrameIndex++;
            }
        }

        public void AdvanceMinute()
        {
            float currentTime = frames[currentFrameIndex].time;
            while (currentFrameIndex < frames.Count - 1 && frames[currentFrameIndex + 1].time <= currentTime + 60.0f)
            {
                currentFrameIndex++;
            }
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
            float currentTime = frames[currentFrameIndex].time;
            while (currentFrameIndex > 0 && frames[currentFrameIndex - 1].time >= currentTime - 1.0f)
            {
                currentFrameIndex--;
            }
        }

        public void RewindMinute()
        {
            float currentTime = frames[currentFrameIndex].time;
            while (currentFrameIndex > 0 && frames[currentFrameIndex - 1].time >= currentTime - 60.0f)
            {
                currentFrameIndex--;
            }
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
                // Handle the case where the currentFrameIndex is out of bounds.
                return null; // Or throw an exception, return a default frame, etc., depending on your needs.
            }
        }

        public void RemoveFramesAfterCurrent()
        {
            if (currentFrameIndex >= 0 && currentFrameIndex < frames.Count)
            {
                frames.RemoveRange(currentFrameIndex + 1, frames.Count - currentFrameIndex - 1);
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
        public List<SoapboxState> recording = new List<SoapboxState>();
        public int currentRecordingIndex = -1;
        public int selectedRecordingFrame = -1;
        public bool record = false;

        public ConfigEntry<KeyCode> toggleModelKey;
        public ConfigEntry<KeyCode> backwardKey;
        public ConfigEntry<KeyCode> forwardKey;
        public ConfigEntry<KeyCode> timeScaleCycleKey;
        public GameObject soapboxStateIndicator;

        public enum GameState { Other, Editor, TestMode};
        public GameState gameState = GameState.Other;

        public enum TimeScale { Frame, Second, Minute};
        public TimeScale timeScale = TimeScale.Frame;
        private void Awake()
        {
            Harmony harmony = new Harmony(pluginGUID);
            harmony.PatchAll();

            // Plugin startup logic
            Logger.LogInfo($"Plugin {pluginName} is loaded!");

            Instance = this;

            LevelEditorApi.EnteredTestMode += () => { SetGameState(GameState.TestMode); };
            LevelEditorApi.EnteredLevelEditor += () => { SetGameState(GameState.Editor); };

            toggleModelKey = Config.Bind("Controls", "Toggle Model", KeyCode.Keypad8, "");
            backwardKey = Config.Bind("Controls", "Rewind", KeyCode.Keypad4, "");
            forwardKey = Config.Bind("Controls", "Fast Forward", KeyCode.Keypad6, "");
            timeScaleCycleKey = Config.Bind("Controls", "Time Cycle", KeyCode.Keypad5, "");

            soapboxStateIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject.DontDestroyOnLoad(soapboxStateIndicator);
            soapboxStateIndicator.SetActive(false);
        }

        public void SetGameState(GameState gameState)
        {
            this.gameState = gameState;
            Debug.LogWarning(gameState);

            if(gameState == GameState.TestMode)
            {
                soapboxStateIndicator.SetActive(false);
            }
        }

        public SoapboxState GetCurrentState()
        {
            if(selectedRecordingFrame == -1)
            {
                if (currentRecordingIndex != -1)
                {
                    selectedRecordingFrame = 0;
                }
                else
                {
                    return null;
                }
            }

            //Check validity of state.
            if (recording.Count > selectedRecordingFrame)
            {
                if (recording[selectedRecordingFrame] != null)
                {
                    return recording[selectedRecordingFrame];
                }
            }

            return null;
        }

        public void ManageRecordingFrame()
        {
            //currentRecordingIndex contains the index of the last frame stored.
            if (currentRecordingIndex == -1)
            {
                //No recording present.
                return;
            }

            //There is a recording, but there is no frame selected. If possible set it to the first frame.
            if (selectedRecordingFrame == -1)
            {
                if(recording.Count > 0)
                {
                    selectedRecordingFrame = 0;
                }               
            }          

            Debug.LogWarning("Current recording count:" + recording.Count);
            Debug.LogWarning("Current recording index:" + currentRecordingIndex);

            //Remove everything after the selected index.
            recording = recording.Take(selectedRecordingFrame + 1).ToList();
        }

        private void Update()
        {
            switch(gameState)
            {
                case GameState.TestMode:
                    if (record)
                    {
                        int frame;
                        if (currentRecordingIndex == -1)
                        {
                            frame = 0;
                        }
                        else
                        {
                            frame = currentRecordingIndex + 1;
                        }

                        SetupCar sc = PlayerManager.Instance.currentMaster.carSetups[0];
                        if(sc.reseter.finished)
                        {
                            Debug.Log("Finished");
                            EnableRecording(false);
                        }
                        
                        SoapboxState soapboxState = new SoapboxState()
                        {
                            position = sc.transform.position,
                            rotation = sc.transform.rotation,
                            velocity = sc.cc.GetRB().velocity,
                            angularVelocity = sc.cc.GetRB().velocity,
                            time = PlayerManager.Instance.currentMaster.currentLevelPhysicsTime,
                            frameID = frame
                        };

                        currentRecordingIndex = frame;
                        recording.Add(soapboxState);
                        soapboxState.Log();
                    }
                    break;
                case GameState.Editor:
                    if(Input.GetKeyDown((KeyCode)timeScaleCycleKey.BoxedValue))
                    {
                        switch(timeScale)
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

                    if(Input.GetKeyDown((KeyCode)toggleModelKey.BoxedValue))
                    {
                        SoapboxState indexState = GetCurrentState();
                        if(indexState == null)
                        {
                            soapboxStateIndicator.SetActive(false);
                        }
                        else
                        {
                            soapboxStateIndicator.SetActive(!soapboxStateIndicator.activeSelf);
                            soapboxStateIndicator.transform.position = indexState.position;
                            soapboxStateIndicator.transform.rotation = indexState.rotation;
                        }          
                    }

                    if (Input.GetKeyDown((KeyCode)backwardKey.BoxedValue))
                    {
                        AdvanceTimeline(false, timeScale);
                    }

                    if (Input.GetKeyDown((KeyCode)forwardKey.BoxedValue))
                    {
                        AdvanceTimeline(true, timeScale);
                    }
                    break;
                case GameState.Other:
                    break;
            }
        }

        public void AdvanceTimeline(bool forward, TimeScale tScale)
        {
            SoapboxState indexState = GetCurrentState();

            if(indexState == null) { return; }

            int selectedFrame = -1;

            if (forward)
            {
                switch (timeScale)
                {
                    case TimeScale.Frame:
                        for(int i = 0; i < recording.Count; i++)
                        {
                            if (recording[i].time > indexState.time)
                            {
                                selectedFrame = i;
                                break;
                            }
                        }
                        break;
                    case TimeScale.Second:
                        for (int i = 0; i < recording.Count; i++)
                        {
                            if (recording[i].time - indexState.time > 1)
                            {
                                selectedFrame = i;
                                break;
                            }
                        }
                        break;
                    case TimeScale.Minute:
                        for (int i = 0; i < recording.Count; i++)
                        {
                            if (recording[i].time - indexState.time > 60)
                            {
                                selectedFrame = i;
                                break;
                            }
                        }
                        break;
                }

                if (selectedFrame == -1)
                {
                    //Out of range, so select the last one.
                    selectedFrame = recording.Count - 1;
                }
            }
            else
            {
                switch (timeScale)
                {
                    case TimeScale.Frame:
                        for (int i = recording.Count - 1; i >= 0; i--)
                        {
                            if (recording[i].time < indexState.time)
                            {
                                selectedFrame = i;
                                break;
                            }
                        }
                        break;
                    case TimeScale.Second:
                        for (int i = recording.Count - 1; i >= 0; i--)
                        {
                            if (indexState.time - recording[i].time > 1)
                            {
                                selectedFrame = i;
                                break;
                            }
                        }
                        break;
                    case TimeScale.Minute:
                        for (int i = recording.Count - 1; i >= 0; i--)
                        {
                            if (indexState.time - recording[i].time > 60)
                            {
                                selectedFrame = i;
                                break;
                            }
                        }
                        break;
                }

                if(selectedFrame == -1)
                {
                    //Out of range, so select the first one.
                    selectedFrame = 0;
                }               
            }

            selectedRecordingFrame = selectedFrame;

            SoapboxState selectedState = GetCurrentState();
            if(selectedState != null)
            {
                currentRecordingIndex = selectedFrame;
                soapboxStateIndicator.transform.position = indexState.position;
                soapboxStateIndicator.transform.rotation = indexState.rotation;
                PlayerManager.Instance.messenger.Log("State time: " + indexState.time, 1f);
            }
            else
            {
                Debug.LogError("State is null for some reason!");
            }           
        }

        public void SpawnPlayer(GameMaster gameMaster, SoapboxState indexState)
        {
            SetupCar setupCar = GameObject.Instantiate<SetupCar>(gameMaster.soapboxPrefab);
            setupCar.transform.position = indexState.position;
            setupCar.transform.rotation = indexState.rotation;
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

        public void SetPlayerVelocitiesOnRelease(GameMaster gameMaster, SoapboxState indexState)
        {
            gameMaster.carSetups[0].cc.GetRB().velocity = indexState.velocity;
            gameMaster.carSetups[0].cc.GetRB().angularVelocity = indexState.angularVelocity;
        }

        public void EnableRecording(bool state = true)
        {
            record = state;
        }
    }

    public class SoapboxState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public float time;
        public int frameID;

        public void Log()
        {
            Debug.Log("----------");
            Debug.Log(position);
            Debug.Log(rotation);
            Debug.Log(velocity);
            Debug.Log(angularVelocity);
            Debug.Log(time);
            Debug.Log(frameID);
        }
    }

    [HarmonyPatch(typeof(GameMaster), "SpawnPlayers")]
    public class SetupGameSpawnPlayers
    {
        public static bool Prefix(GameMaster __instance)
        {
            if(Plugin.Instance.gameState == Plugin.GameState.Other)
            {
                return true;
            }

            SoapboxState indexState = Plugin.Instance.GetCurrentState();

            if(indexState != null)
            {
                Plugin.Instance.SpawnPlayer(__instance, indexState);
                Plugin.Instance.ManageRecordingFrame();
                return false;
            }
           
            return true;
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

            SoapboxState indexState = Plugin.Instance.GetCurrentState();

            if (indexState != null)
            {
                Plugin.Instance.SetPlayerVelocitiesOnRelease(__instance, indexState);
            }

            Plugin.Instance.EnableRecording();
        }
    }


    [HarmonyPatch(typeof(MainMenuUI), "Awake")]
    public static class MainMenuUIAwake
    {
        public static void Postfix()
        {
            Plugin.Instance.SetGameState(Plugin.GameState.Other);
        }
    }
}
