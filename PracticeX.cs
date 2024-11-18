using Rewired.UI.ControlMapper;
using TMPro;
using UnityEngine;
using ZeepSDK.LevelEditor;
using ZeepSDK.Racing;

namespace PracticeMode
{
    public enum Scene { Other, Editor, TestMode };

    public static class PracticeX
    {
        //The actual recorder
        public static SoapboxRecorder mRecorder = new SoapboxRecorder();

        //Should we record right now?
        public static bool mRecord = false;       

        // Selected frame for test mode
        public static SoapboxRecorderFrame mSelectedFrame = null;

        //Timeline selection slider.
        private static float mPreviousSliderValue = 0.0f;
        private static float mSliderValue = 0.0f;
        private static float mMinTime = 0.0f;
        private static float mMaxTime = 0.0f;
        private static bool mCheckpointFlag = false;        
        
        //The scene we are currently in.
        public static Scene mCurrentScene = Scene.Other;

        //The window rect for UIConfigurator.
        public static RectTransform mPmWindow;

        //Mouse in rect
        private static bool mouseInRect;
        private static object mLock = new object();

        public static void Initialize()
        {
            LevelEditorApi.EnteredTestMode += () => 
            { 
                SetCurrentScene(Scene.TestMode); 
            };

            LevelEditorApi.EnteredLevelEditor += () =>
            {
                SetCurrentScene(Scene.Editor);
            };

            LevelEditorApi.LevelLoaded += OnNewLevelLoaded;
            RacingApi.PassedCheckpoint += OnPassedCheckpoint;
        }
        
        #region Visualizer
        //The soapbox in the level editor for position visualization, will be created once. Doesnt reflect any cosmetic changes.
        public static GameObject mSoapboxVisualizer = null;
        private static void HideVisualizer()
        {
            if(mSoapboxVisualizer != null)
            {
                mSoapboxVisualizer.SetActive(false);
            }
        }

        private static void ShowVisualizer()
        {
            if (mSoapboxVisualizer != null)
            {
                mSoapboxVisualizer.SetActive(true);
            }
        }

        private static void SetVisualizerTransform(SoapboxRecorderFrame frame)
        {   
            if(mSoapboxVisualizer != null)
            {
                mSoapboxVisualizer.transform.position = frame.position;
                mSoapboxVisualizer.transform.rotation = frame.rotation;
            }            
        }

        public static void CreateVisualizer()
        {
            NetworkedGhostSpawner networkedGhostSpawner = GameObject.FindObjectOfType<NetworkedGhostSpawner>();
            if (networkedGhostSpawner == null) { return; }

            Shpleeble shpleeble = new GameObject("Shpleeble").AddComponent<Shpleeble>();
            GameObject.DontDestroyOnLoad(shpleeble);

            //SOAPBOX
            SetupModelCar soapbox = GameObject.Instantiate(networkedGhostSpawner.zeepkistGhostPrefab.ghostModel.transform, shpleeble.transform).GetComponent<SetupModelCar>();
            //Remove ghost wheel scripts
            Ghost_AnimateWheel[] animateWheelScripts = soapbox.transform.GetComponentsInChildren<Ghost_AnimateWheel>();
            foreach (Ghost_AnimateWheel gaw in animateWheelScripts)
            {
                GameObject.Destroy(gaw);
            }
            //Attach the left and right arm to the top of the armature
            Transform armatureTopSX = soapbox.transform.Find("Character/Armature/Top");
            Transform leftArmSX = soapbox.transform.Find("Character/Left Arm");
            Transform rightArmSX = soapbox.transform.Find("Character/Right Arm");
            leftArmSX.parent = armatureTopSX;
            leftArmSX.localPosition = new Vector3(-0.25f, 0, 1.25f);
            leftArmSX.localEulerAngles = new Vector3(0, 240, 0);
            rightArmSX.parent = armatureTopSX;
            rightArmSX.localPosition = new Vector3(-0.25f, 0, -1.25f);
            rightArmSX.localEulerAngles = new Vector3(0, 120, 0);

            //CAMERA MAN
            SetupModelCar cameraMan = GameObject.Instantiate(networkedGhostSpawner.zeepkistGhostPrefab.cameraManModel.transform, shpleeble.transform).GetComponent<SetupModelCar>();
            GameObject camera = cameraMan.transform.Find("Character/Right Arm/Camera").gameObject;
            camera.SetActive(false);

            //Attach the left and right arm to the top of the armature
            Transform armatureTop = cameraMan.transform.Find("Character/Armature/Top");
            Transform leftArm = cameraMan.transform.Find("Character/Left Arm");
            Transform rightArm = cameraMan.transform.Find("Character/Right Arm");
            leftArm.parent = armatureTop;
            leftArm.localPosition = new Vector3(-0.25f, 0, 1.25f);
            leftArm.localEulerAngles = new Vector3(0, 240, 0);
            rightArm.parent = armatureTop;
            rightArm.localPosition = new Vector3(-0.25f, 0, -1.25f);
            rightArm.localEulerAngles = new Vector3(0, 120, 0);

            //DISPLAY NAME
            TextMeshPro displayName = GameObject.Instantiate(networkedGhostSpawner.zeepkistGhostPrefab.nameDisplay.transform, shpleeble.transform).GetComponent<TextMeshPro>();
            GameObject.Destroy(displayName.transform.GetComponent<DisplayPlayerName>());
            GameObject.Destroy(displayName.transform.Find("hoethouder").gameObject);
            displayName.transform.localScale = new Vector3(-1, 1, 1);

            //OTHER
            GameObject hornModel = soapbox.transform.Find("Visible Horn").gameObject;
            hornModel.SetActive(false);

            GameObject paragliderModel = soapbox.transform.Find("Glider").gameObject;
            foreach (Transform t in paragliderModel.transform)
            {
                t.gameObject.SetActive(true);
            }
            paragliderModel.SetActive(false);

            shpleeble.SetObjects(soapbox, cameraMan, displayName, hornModel, paragliderModel, camera, armatureTop);
            shpleeble.SetCosmetics(GetLocalPlayerData().ToCosmeticsV16());

            shpleeble.gameObject.SetActive(false);

            mSoapboxVisualizer = shpleeble.gameObject;
            HideVisualizer();
        }

        private static void CreateVisualizer2()
        {
            if(mSoapboxVisualizer != null)
            {
                return;
            }

            NetworkedGhostSpawner networkedGhostSpawner = GameObject.FindObjectOfType<NetworkedGhostSpawner>();
            NetworkedZeepkistGhost networkedZeepkistGhost = networkedGhostSpawner.zeepkistGhostPrefab;
            Transform soapboxOriginal = networkedZeepkistGhost.ghostModel.transform;

            mSoapboxVisualizer = GameObject.Instantiate(soapboxOriginal.gameObject);
            mSoapboxVisualizer.transform.Find("Glider")?.gameObject.SetActive(false);
            mSoapboxVisualizer.transform.Find("Visible Horn")?.gameObject.SetActive(false);       
            GameObject.DontDestroyOnLoad(mSoapboxVisualizer);
            HideVisualizer();
        }

        public static PlayerData GetLocalPlayerData()
        {
            PlayerData playerData = new PlayerData();
            playerData.playerID = -1;
            playerData.state = 255;

            try
            {
                ZeepkistNetworking.CosmeticIDs cosmeticIDs = ProgressionManager.Instance.GetAdventureCosmetics();

                playerData.name = PlayerManager.Instance.steamAchiever.GetPlayerName(false);

                playerData.zeepkist = cosmeticIDs.zeepkist;
                playerData.frontWheels = cosmeticIDs.frontWheels;
                playerData.rearWheels = cosmeticIDs.rearWheels;
                playerData.paraglider = cosmeticIDs.paraglider;
                playerData.horn = cosmeticIDs.horn;
                playerData.hat = cosmeticIDs.hat;
                playerData.glasses = cosmeticIDs.glasses;
                playerData.color_body = cosmeticIDs.color_body;
                playerData.color_leftArm = cosmeticIDs.color_leftArm;
                playerData.color_rightArm = cosmeticIDs.color_rightArm;
                playerData.color_leftLeg = cosmeticIDs.color_leftLeg;
                playerData.color_rightLeg = cosmeticIDs.color_rightLeg;
                playerData.color = cosmeticIDs.color;
            }
            catch
            {
                playerData.name = "Sphleeble";
                playerData.hat = 23000;
                playerData.color = 1000;
                playerData.zeepkist = 1000;

                playerData.zeepkist = 1000;
                playerData.frontWheels = 1000;
                playerData.rearWheels = 1000;
                playerData.paraglider = 1000;
                playerData.horn = 1000;
                playerData.hat = 23000;
                playerData.glasses = 1000;
                playerData.color_body = 1000;
                playerData.color_leftArm = 1000;
                playerData.color_rightArm = 1000;
                playerData.color_leftLeg = 1000;
                playerData.color_rightLeg = 1000;
                playerData.color = 1000;
            }

            return playerData;
        }

        public struct PlayerData
        {
            public int playerID;
            public string name;
            public byte state;
            public int zeepkist;
            public int frontWheels;
            public int rearWheels;
            public int paraglider;
            public int horn;
            public int hat;
            public int glasses;
            public int color_body;
            public int color_leftArm;
            public int color_rightArm;
            public int color_leftLeg;
            public int color_rightLeg;
            public int color;

            public CosmeticsV16 ToCosmeticsV16()
            {
                CosmeticsV16 cosmetics = new CosmeticsV16();
                ZeepkistNetworking.CosmeticIDs cosmeticIDs = new ZeepkistNetworking.CosmeticIDs();
                cosmeticIDs.zeepkist = zeepkist;
                cosmeticIDs.frontWheels = frontWheels;
                cosmeticIDs.rearWheels = rearWheels;
                cosmeticIDs.paraglider = paraglider;
                cosmeticIDs.horn = horn;
                cosmeticIDs.hat = hat;
                cosmeticIDs.glasses = glasses;
                cosmeticIDs.color_body = color_body;
                cosmeticIDs.color_leftArm = color_leftArm;
                cosmeticIDs.color_rightArm = color_rightArm;
                cosmeticIDs.color_leftLeg = color_leftLeg;
                cosmeticIDs.color_rightLeg = color_rightLeg;
                cosmeticIDs.color = color;
                cosmetics.IDsToCosmetics(cosmeticIDs);
                return cosmetics;
            }
        }
        #endregion

        #region Scenes
        public static void SetCurrentScene(Scene currentScene)
        {
            mCurrentScene = currentScene;

            //Always reset on scene change.
            mouseInRect = false;
            DoMouseBlock(false);

            switch (currentScene)
            {
                case Scene.Editor:

                    Plugin.Instance.Log("SetCurrentScene: Editor");
                    //No need for recording in the editor.
                    mRecord = false;

                    //If the recorder contains frames:
                    if (mRecorder.frames.Count > 0)
                    {
                        //Update the slider range.
                        mMinTime = mRecorder.GetFirstFrame().time;
                        mMaxTime = mRecorder.GetLastFrame().time;

                        //If the slider value has an invalid time, set the slider to the minimum
                        if (mSliderValue < mMinTime || mSliderValue > mMaxTime)
                        {
                            mSliderValue = mMinTime;
                        }

                        //Show the visualizer.
                        ShowVisualizer();

                        //Get the frame based on the slider value and set the visualizer at that frame.
                        int frameIndex = mRecorder.GetFrameIndexByTime(mSliderValue);
                        if (frameIndex != -1)
                        {
                            mRecorder.SetCurrentFrameIndex(frameIndex);
                            SoapboxRecorderFrame frame = mRecorder.GetCurrentFrame();
                            if (frame != null)
                            {
                                SetVisualizerTransform(frame);
                            }
                        }
                    }
                    break;
                case Scene.TestMode:
                    Plugin.Instance.Log("SetCurrentScene: TestMode");
                    HideVisualizer();
                    break;
                case Scene.Other:
                    mRecord = false;
                    HideVisualizer();
                    break;
            }
        }

        public static void OnMainMenu(MainMenuUI instance)
        {
            //Set the current scene
            SetCurrentScene(Scene.Other);
            //Clear the recording on main menu, otherwise the recording sticks to an empty level when we return to the level editor.
            mRecorder.Clear();
            //Create the visualizer if not already done.
            CreateVisualizer();

            Plugin.Instance.Log("OnMainMenu: Clearing recordings and creating visualizer.");
        }

        public static void OnLevelEditor(LEV_LevelEditorCentral instance)
        {
            // Find the Canvas in the hierarchy of LEV_LevelEditorCentral
            RectTransform canvasRectTransform = instance.transform.Find("Canvas").GetComponent<RectTransform>();

            // Create a new GameObject that will act as the window you want to position
            GameObject newWindow = new GameObject("PMWindow", typeof(RectTransform));

            // Set the new GameObject's parent to be the canvas.
            newWindow.transform.SetParent(canvasRectTransform, false);
            //Make it appear on top.
            newWindow.transform.SetAsFirstSibling();

            // Get the RectTransform component of the new GameObject
            RectTransform pmWindowRectTransform = newWindow.GetComponent<RectTransform>();

            // Optionally, set the RectTransform's properties to desired values
            pmWindowRectTransform.anchorMin = new Vector2(0.5f, 0.5f); // Center anchor
            pmWindowRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            pmWindowRectTransform.pivot = new Vector2(0.5f, 0.5f);
            pmWindowRectTransform.anchoredPosition = Vector2.zero; // Center position
            pmWindowRectTransform.sizeDelta = new Vector2(200, 100); // Width and height

            // Save the RectTransform.
            mPmWindow = pmWindowRectTransform;
            //Add it to the configurator.
            ZeepSDK.UI.UIApi.AddToConfigurator(mPmWindow);

            Plugin.Instance.Log("OnLevelEditor: Setting up window.");
        }
        #endregion

        #region Main Loop
        public static Rect mPmWindowRect;

        public static void DoGUI()
        {
            if (mCurrentScene != Scene.Editor)
            {
                return;
            }

            if(mRecorder.frames.Count <= 0)
            {
                return;
            }

            // Calculate the actual Rect in screen space
            Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(null, mPmWindow.TransformPoint(mPmWindow.rect.min));
            Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(null, mPmWindow.TransformPoint(mPmWindow.rect.max));

            // Convert screen space to GUI space
            float guiYMin = Screen.height - screenMax.y;
            float guiYMax = Screen.height - screenMin.y;
            mPmWindowRect = new Rect(screenMin.x, guiYMin, screenMax.x - screenMin.x, guiYMax - guiYMin);

            // Draw the main box
            GUI.Box(mPmWindowRect, "", GUIStyleX.windowBody);

            // Calculate positions within the box relative to its RectTransform
            float buttonWidth = 60f;
            float buttonHeight = 25f;
            float sliderHeight = 20f;
            float padding = 5f;

            // Calculate the available width for the buttons and the label
            float totalButtonWidth = 5 * (buttonWidth + padding);
            float availableLabelWidth = mPmWindowRect.width - totalButtonWidth - 2 * padding;

            // Starting x and y positions for elements within the box
            float startX = mPmWindowRect.x;
            float startY = mPmWindowRect.y; // Move down for title

            // Title position (spanning full width)
            Rect titleRect = new Rect(mPmWindowRect.x, mPmWindowRect.y, mPmWindowRect.width, buttonHeight);
            GUI.Box(titleRect, "Practice Mode", GUIStyleX.windowHeader);

            // Move down to the next row
            startY += buttonHeight + padding;
            startX += padding;

            // Row elements positioning
            // Previous frame button <
            Rect previousFrameRect = new Rect(startX, startY, buttonWidth, buttonHeight);
            if (GUI.Button(previousFrameRect, "<", GUIStyleX.windowButton))
            {
                mRecorder.PreviousFrame();
                mSliderValue = mRecorder.GetCurrentFrame().time;
            }

            // Previous checkpoint button <C
            Rect previousCheckpointRect = new Rect(previousFrameRect.x + buttonWidth + padding, startY, buttonWidth, buttonHeight);
            if (GUI.Button(previousCheckpointRect, "<C", GUIStyleX.windowButton))
            {
                mRecorder.PreviousCheckpointFrame();
                mSliderValue = mRecorder.GetCurrentFrame().time;
            }

            // Time label
            Rect timeLabelRect = new Rect(previousCheckpointRect.x + buttonWidth + padding, startY, availableLabelWidth, buttonHeight);
            GUI.Box(timeLabelRect, "Time: " + mSliderValue.ToString("F2"), GUIStyleX.windowTimeLabel);

            // Next checkpoint button C>
            Rect nextCheckpointRect = new Rect(timeLabelRect.x + availableLabelWidth + padding, startY, buttonWidth, buttonHeight);
            if (GUI.Button(nextCheckpointRect, "C>", GUIStyleX.windowButton))
            {
                mRecorder.NextCheckpointFrame();
                mSliderValue = mRecorder.GetCurrentFrame().time;
            }

            // Next frame button >
            Rect nextFrameRect = new Rect(nextCheckpointRect.x + buttonWidth + padding, startY, buttonWidth, buttonHeight);
            if (GUI.Button(nextFrameRect, ">", GUIStyleX.windowButton))
            {
                mRecorder.NextFrame();
                mSliderValue = mRecorder.GetCurrentFrame().time;
            }

            // Reset button to clear the recording
            Rect resetButtonRect = new Rect(nextFrameRect.x + buttonWidth + padding, startY, buttonWidth, buttonHeight);
            if (GUI.Button(resetButtonRect, "Reset", GUIStyleX.windowButton))
            {
                mRecorder.Clear();
                HideVisualizer();
                mSliderValue = 0;
            }

            // Move down to the next row
            startY += buttonHeight + padding;

            // Slider for selecting the frame (spanning full width)
            Rect sliderRect = new Rect(mPmWindowRect.x + padding, startY, mPmWindowRect.width - 2 * padding, sliderHeight);
            float newSliderValue = GUI.HorizontalSlider(sliderRect, mSliderValue, mMinTime, mMaxTime, GUIStyleX.windowSlider, GUIStyleX.windowSliderThumb);

            // Update the frame based on the slider value if changed
            if (Mathf.Abs(newSliderValue - mPreviousSliderValue) > Mathf.Epsilon)
            {
                mSliderValue = newSliderValue;
                int frameIndex = mRecorder.GetFrameIndexByTime(mSliderValue);
                if (frameIndex != -1)
                {
                    mRecorder.SetCurrentFrameIndex(frameIndex);
                    SoapboxRecorderFrame frame = mRecorder.GetCurrentFrame();
                    if (frame != null)
                    {
                        SetVisualizerTransform(frame);
                    }
                }

                mPreviousSliderValue = newSliderValue;
            }            
        }

        public static void DoMouseBlock(bool state)
        {
            if (state)
            {
                LevelEditorApi.BlockMouseInput(mLock);
            }
            else
            {
                LevelEditorApi.UnblockMouseInput(mLock);
            }
        }

        public static void DoUpdate()
        {
            if(mCurrentScene == Scene.Editor)
            {
                if (mPmWindowRect.Contains(Event.current.mousePosition))
                {
                    if(!mouseInRect)
                    {
                        mouseInRect = true;
                        DoMouseBlock(true);
                    }                    
                }
                else
                {
                    if (mouseInRect)
                    {
                        mouseInRect = false;
                        DoMouseBlock(false);
                    }
                }
            }

            //Dont need update if we are not in testing mode.
            if (mCurrentScene != Scene.TestMode)
            {
                return;
            }

            //If we are recording right now.
            if (mRecord)
            {
                //Get the first car setup.
                SetupCar sc = PlayerManager.Instance.currentMaster.carSetups[0];

                //If we have finished, stop recording.
                if (sc.reseter.finished)
                {
                    mRecord = false;
                    return;
                }

                //Calculate the time difference between last and current frame.
                float addedTime = mSelectedFrame != null ? mSelectedFrame.time : 0;
                SoapboxRecorderFrame lastFrame = mRecorder.GetLastFrame();
                if (lastFrame != null)
                {
                    //Nothing has happened, for instance when pausing, so no need to save frame.
                    if (PlayerManager.Instance.currentMaster.currentLevelPhysicsTime + addedTime == lastFrame.time)
                    {
                        return;
                    }
                }

                //Create a new frame with all info.
                SoapboxRecorderFrame newFrame = new SoapboxRecorderFrame()
                {
                    position = sc.transform.position,
                    rotation = sc.transform.rotation,
                    velocity = sc.cc.GetRB().velocity,
                    angularVelocity = sc.cc.GetRB().angularVelocity,
                    time = PlayerManager.Instance.currentMaster.currentLevelPhysicsTime + addedTime,
                    isCheckpoint = mCheckpointFlag
                };

                //Reset the checkpoint flag for next frame.
                mCheckpointFlag = false;
                //Add the frame to the recorder.
                mRecorder.AddFrame(newFrame);
            }
        }
        #endregion

        #region Events
        public static void OnNewLevelLoaded()
        {
            //When a new level loads, clear the recorder and hide the visualizer.
            mRecorder.Clear();
            HideVisualizer();

            Plugin.Instance.Log("OnNewLevelLoaded. Clearing recordings and hiding visualizer.");
        }

        public static void OnPassedCheckpoint(float time)
        {
            if (mRecord)
            {
                mCheckpointFlag = true;
            }
            else
            {
                mCheckpointFlag = false;
            }
        }

        public static bool OnSpawnPlayers(GameMaster instance)
        {
            if (mCurrentScene != Scene.TestMode)
            {
                return true;
            }

            Plugin.Instance.Log("OnSpawnPlayers: In Test Mode, using custom spawning if frames are available.");

            SoapboxRecorderFrame frame = mRecorder.GetCurrentFrame();
            if (frame != null)
            {
                //Remove frames so we can record over them.
                mRecorder.RemoveFramesAfterCurrent();
                //Spawn the player at the frames location.
                SpawnPlayer(instance, frame);
                mSelectedFrame = frame;
                return false;
            }
            else
            {
                mSelectedFrame = null;
                return true;
            }
        }

        public static void OnRestartLevel(GameMaster instance)
        {
            if (mCurrentScene != Scene.TestMode)
            {
                return;
            }

            mRecord = false;
        }

        public static void OnReleaseTheZeepkists(GameMaster instance)
        {
            if (mCurrentScene != Scene.TestMode)
            {
                return;
            }

            Plugin.Instance.Log("OnReleaseTheZeepkists: TestMode, using frame data if available.");

            if (mSelectedFrame != null)
            {
                SetPlayerVelocitiesOnRelease(instance, mSelectedFrame);
            }

            mRecord = true;
        }

        public static void OnSaveLoad(LEV_SaveLoad instance)
        {
            mRecorder.RewindToStart();

            Plugin.Instance.Log("OnSaveLoad: New level loaded, rewinding recorder.");
        }
        #endregion

        public static void SpawnPlayer(GameMaster gameMaster, SoapboxRecorderFrame frame)
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

            Plugin.Instance.Log("SpawnPlayer: Using frame data.");
        }

        public static void SetPlayerVelocitiesOnRelease(GameMaster gameMaster, SoapboxRecorderFrame frame)
        {
            gameMaster.carSetups[0].cc.GetRB().velocity = frame.velocity;
            gameMaster.carSetups[0].cc.GetRB().angularVelocity = frame.angularVelocity;

            Plugin.Instance.Log("SetPlayerVelocitiesOnRelease: Using frame data.");
        }
    }
}
