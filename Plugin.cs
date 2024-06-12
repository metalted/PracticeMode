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
        public const string pluginVersion = "1.4";

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

        public RectTransform pmWindow;

        private void Awake()
        {
            Harmony harmony = new Harmony(pluginGUID);
            harmony.PatchAll();

            // Plugin startup logic
            Logger.LogInfo($"Plugin {pluginName} is loaded!");

            Instance = this;

            LevelEditorApi.EnteredTestMode += () => { SetGameState(GameState.TestMode); };
            LevelEditorApi.EnteredLevelEditor += () => 
            { 
                SetGameState(GameState.Editor); 
            };
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
                // Calculate the actual Rect in screen space
                Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(null, pmWindow.TransformPoint(pmWindow.rect.min));
                Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(null, pmWindow.TransformPoint(pmWindow.rect.max));

                // Convert screen space to GUI space
                float guiYMin = Screen.height - screenMax.y;
                float guiYMax = Screen.height - screenMin.y;
                Rect pmWindowRect = new Rect(screenMin.x, guiYMin, screenMax.x - screenMin.x, guiYMax - guiYMin);

                // Draw the main box with title
                GUI.Box(pmWindowRect, "Practice Mode");

                // Calculate positions within the box relative to its RectTransform
                float buttonWidth = 60f;
                float buttonHeight = 25f;
                float sliderHeight = 20f;
                float padding = 5f;

                // Calculate the available width for the buttons and the label
                float totalButtonWidth = 5 * (buttonWidth + padding);
                float availableLabelWidth = pmWindowRect.width - totalButtonWidth - 2 * padding;

                // Starting x and y positions for elements within the box
                float startX = pmWindowRect.x + padding;
                float startY = pmWindowRect.y + padding + buttonHeight; // Move down for title

                // Title position (spanning full width)
                Rect titleRect = new Rect(pmWindowRect.x, pmWindowRect.y, pmWindowRect.width, buttonHeight);
                GUI.Label(titleRect, "Practice Mode", GUI.skin.box);

                // Move down to the next row
                startY += buttonHeight + padding;

                // Row elements positioning
                // Previous frame button <
                Rect previousFrameRect = new Rect(startX, startY, buttonWidth, buttonHeight);
                if (GUI.Button(previousFrameRect, "<"))
                {
                    recorder.PreviousFrame();
                    sliderValue = recorder.GetCurrentFrame().time;
                }

                // Previous checkpoint button <C
                Rect previousCheckpointRect = new Rect(previousFrameRect.x + buttonWidth + padding, startY, buttonWidth, buttonHeight);
                if (GUI.Button(previousCheckpointRect, "<C"))
                {
                    recorder.PreviousCheckpointFrame();
                    sliderValue = recorder.GetCurrentFrame().time;
                }

                // Time label
                Rect timeLabelRect = new Rect(previousCheckpointRect.x + buttonWidth + padding, startY, availableLabelWidth, buttonHeight);
                GUI.Label(timeLabelRect, "Time: " + sliderValue.ToString("F2"));

                // Next checkpoint button C>
                Rect nextCheckpointRect = new Rect(timeLabelRect.x + availableLabelWidth + padding, startY, buttonWidth, buttonHeight);
                if (GUI.Button(nextCheckpointRect, "C>"))
                {
                    recorder.NextCheckpointFrame();
                    sliderValue = recorder.GetCurrentFrame().time;
                }

                // Next frame button >
                Rect nextFrameRect = new Rect(nextCheckpointRect.x + buttonWidth + padding, startY, buttonWidth, buttonHeight);
                if (GUI.Button(nextFrameRect, ">"))
                {
                    recorder.NextFrame();
                    sliderValue = recorder.GetCurrentFrame().time;
                }

                // Reset button to clear the recording
                Rect resetButtonRect = new Rect(nextFrameRect.x + buttonWidth + padding, startY, buttonWidth, buttonHeight);
                if (GUI.Button(resetButtonRect, "Reset"))
                {
                    recorder.Clear();
                    soapboxVisualizer.SetActive(false);
                    sliderValue = 0;
                }

                // Move down to the next row
                startY += buttonHeight + padding;

                // Slider for selecting the frame (spanning full width)
                Rect sliderRect = new Rect(pmWindowRect.x + padding, startY, pmWindowRect.width - 2 * padding, sliderHeight);
                float newSliderValue = GUI.HorizontalSlider(sliderRect, sliderValue, minTime, maxTime);

                // Update the frame based on the slider value if changed
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

    [HarmonyPatch(typeof(GameMaster), "RestartLevel")]
    public class GameMasterRestartLevel
    {
        public static void Prefix()
        {
            if (Plugin.Instance.gameState == Plugin.GameState.Other)
            {
                return;
            }

            Plugin.Instance.record = false;
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
            Plugin.Instance.recorder.Clear();

            if (Plugin.Instance.soapboxVisualizer != null) { return; }

            NetworkedGhostSpawner networkedGhostSpawner = GameObject.FindObjectOfType<NetworkedGhostSpawner>();
            NetworkedZeepkistGhost networkedZeepkistGhost = networkedGhostSpawner.zeepkistGhostPrefab;
            Transform soapboxOriginal = networkedZeepkistGhost.ghostModel.transform;            

            Plugin.Instance.soapboxVisualizer = GameObject.Instantiate(soapboxOriginal.gameObject);

            Transform glider = Plugin.Instance.soapboxVisualizer.transform.Find("Glider");
            if(glider != null)
            {
                glider.transform.gameObject.SetActive(false);
            }

            Transform visibleHorn = Plugin.Instance.soapboxVisualizer.transform.Find("Visible Horn");
            if(visibleHorn != null)
            {
                visibleHorn.transform.gameObject.SetActive(false);
            }

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

    [HarmonyPatch(typeof(LEV_LevelEditorCentral), "Awake")]
    public class LEV_CentralAwakePatch
    {
        public static void Postfix(LEV_LevelEditorCentral __instance)
        {
            // Find the Canvas in the hierarchy of LEV_LevelEditorCentral
            RectTransform canvasRectTransform = __instance.transform.Find("Canvas").GetComponent<RectTransform>();

            // Create a new GameObject that will act as the window you want to position
            GameObject newWindow = new GameObject("PMWindow", typeof(RectTransform));

            // Set the new GameObject's parent to be the canvas
            newWindow.transform.SetParent(canvasRectTransform, false);
            newWindow.transform.SetAsFirstSibling();

            // Get the RectTransform component of the new GameObject
            RectTransform pmWindowRectTransform = newWindow.GetComponent<RectTransform>();

            // Optionally, set the RectTransform's properties to desired values
            pmWindowRectTransform.anchorMin = new Vector2(0.5f, 0.5f); // Center anchor
            pmWindowRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            pmWindowRectTransform.pivot = new Vector2(0.5f, 0.5f);
            pmWindowRectTransform.anchoredPosition = Vector2.zero; // Center position
            pmWindowRectTransform.sizeDelta = new Vector2(200, 100); // Width and height

            // Save the RectTransform to Plugin.Instance.pmWindow
            Plugin.Instance.pmWindow = pmWindowRectTransform;

            ZeepSDK.UI.UIApi.AddToConfigurator(Plugin.Instance.pmWindow);
        }
    }
}
