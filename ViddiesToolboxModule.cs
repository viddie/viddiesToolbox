﻿using Celeste.Mod.ConsistencyTracker;
using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.viddiesToolbox.Analog;
using Celeste.Mod.viddiesToolbox.Entities;
using Celeste.Mod.viddiesToolbox.Enums;
using Celeste.Mod.viddiesToolbox.Menu;
using Celeste.Mod.viddiesToolbox.ThirdParty;
using Celeste.Mod.viddiesToolbox.Tools;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.viddiesToolbox {
    public class ViddiesToolboxModule : EverestModule {
        
        public static ViddiesToolboxModule Instance;
        public static string FreezeSound   = SFX.ui_game_pause;
        public static string UnfreezeSound = SFX.ui_game_unpause;
        public static string FrameAdvanceSound = SFX.ui_main_button_select;

        public static float FreezeTime = 0.01666666f * 1000;
        public override Type SettingsType => typeof(ModuleSettings);
        public ModuleSettings ModSettings => (ModuleSettings)this._Settings;

        private bool ConsistencyTrackerLoaded { get; set; } = false;
        public bool RoomTimerEnabled => ModSettings.EnableRoomTimer && ConsistencyTrackerLoaded;

        private FreezeState EngineFrozenState = FreezeState.Normal;
        private float _SavedFreezeTimer = float.NaN;
        private bool _DidFrameAdvance = false;
        private int _AdvanceFrames = -1;

        public TeleportPoints Teleports = new TeleportPoints();

        public ViddiesToolboxModule() {
            Instance = this;

            Logger.SetLogLevel("viddiesToolbox/", LogLevel.Info);
        }

        public override void Load() {
            On.Monocle.Engine.Update += Engine_Update;
            On.Celeste.SpeedrunTimerDisplay.DrawTime += SpeedrunTimerDisplay_DrawTime;
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
            On.Celeste.Input.GetAimVector += Input_GetAimVector;

            Teleports.Hook();
        }

        public override void Unload() {
            On.Monocle.Engine.Update -= Engine_Update;
            On.Celeste.SpeedrunTimerDisplay.DrawTime -= SpeedrunTimerDisplay_DrawTime;
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
            On.Celeste.Input.GetAimVector -= Input_GetAimVector;

            Teleports.UnHook();
        }

        public override void Initialize() {
            base.Initialize();

            if (ModSettings.AnalogUseDashDirectionsForMovement) {
                LoadAnalogMoveDirections();
            }

            // load SpeedrunTool if it exists
            SpeedrunToolSupport.Load();

            if (Everest.Modules.Any(m => m.Metadata.Name == "ConsistencyTracker")) {
                ConsistencyTrackerLoaded = true;
            }
        }

        public override void OnInputInitialize() {
            base.OnInputInitialize();

            //Register custom bindings
            foreach (KeyValuePair<string, ButtonBinding> entry in ModSettings.ButtonsConsoleCommands) {
                InitializeButtonBinding(entry.Value);
            }
            foreach (ButtonBinding entry in ModSettings.ButtonsTeleportPoint) {
                InitializeButtonBinding(entry);
            }
        }

        #region Hooks

        private FreezeState? _TargetFreezeState;
        public void EnginePreUpdate() {
            FreezeState newState = EngineFrozenState;
            
            if (ModSettings.ButtonToggleFreezeEngine.Pressed && ModSettings.HotkeysEnabled) {
                if (EngineFrozenState == FreezeState.Normal) {
                    _TargetFreezeState = FreezeState.Frozen;
                    //Log($"Freezing engine | FreezeTimer: {Engine.FreezeTimer}, SavedFreezeTimer: {_SavedFreezeTimer} | DeltaTime: {Engine.DeltaTime}, RawDeltaTime: {Engine.RawDeltaTime}");
                } else {
                    _TargetFreezeState = FreezeState.Normal;
                    //Log("Unfreezing engine");
                }
                //Log($"Freeze state check: Current: {EngineFrozenState}, New: {_TargetFreezeState}");
                //ResetLogOnce();
            }

            if (_TargetFreezeState != null) { 
                newState = _TargetFreezeState.Value;
                _TargetFreezeState = null;
            }

            bool doFrameAdvance = (ModSettings.ButtonAdvanceFrame.Pressed || ModSettings.ButtonAdvanceMultipleFrames.Pressed) && ModSettings.HotkeysEnabled;
            switch (EngineFrozenState) {
                case FreezeState.Normal when newState == FreezeState.Frozen: //Previously normal, now frozen
                    _SavedFreezeTimer = Engine.FreezeTimer;
                    Engine.FreezeTimer = FreezeTime;
                    Audio.Play(FreezeSound);
                    break;
                case FreezeState.Frozen when newState == FreezeState.Normal: //Previously frozen, now normal
                    Engine.FreezeTimer = _SavedFreezeTimer;
                    _SavedFreezeTimer = float.NaN;
                    Audio.Play(UnfreezeSound);
                    break;
                case FreezeState.Frozen when newState == FreezeState.Frozen: {
                    if (doFrameAdvance && _AdvanceFrames == -1) {
                        _AdvanceFrames = ModSettings.ButtonAdvanceMultipleFrames.Pressed ? ModSettings.FreezeEngineMultipleFrames : 1;
                        Engine.FreezeTimer = _SavedFreezeTimer;
                        Audio.Play(FrameAdvanceSound);
                        ResetLogOnce();
                    }

                    if (_AdvanceFrames > 0) {
                        ResetLogOnce();
                        _AdvanceFrames--;
                    } else if (_AdvanceFrames == 0) {
                        _AdvanceFrames = -1;
                        _SavedFreezeTimer = Engine.FreezeTimer;
                        Engine.FreezeTimer = FreezeTime;
                    } else {
                        Engine.FreezeTimer = FreezeTime;
                    }
                    
                    break;
                }
            }

            LogOnce($"Engine.FreezeTime: {Engine.FreezeTimer}, _SavedFreezeTimer: {_SavedFreezeTimer}");
            EngineFrozenState = newState;
        }

        private void Engine_Update(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gameTime) {
            orig(self, gameTime);

            if (ModSettings.ToggleHotkeys.Pressed) {
                ModSettings.HotkeysEnabled = !ModSettings.HotkeysEnabled;
                Log($"Hotkeys enabled: {ModSettings.HotkeysEnabled}", LogLevel.Info);
            }
            
            if (!(Engine.Scene is Level)) return;
            Level level = Engine.Scene as Level;
            if (!level.Paused) EnginePreUpdate();

            Player player = level.Tracker.GetEntity<Player>();
            if (player == null) return;

            UpdateHotkeyPresses(player);
        }

        public void UpdateHotkeyPresses(Player player) {
            if (ModSettings == null) {
                Log($"'ModSettings' was null!", LogLevel.Warn);
                return;
            }
            if (ModSettings.ButtonMovePlayerUp == null) {
                Log($"'ButtonMovePlayer1PixelUp' was null!", LogLevel.Warn);
                return;
            }
            if (!ModSettings.HotkeysEnabled) return;

            Follower follower = player.Leader.Followers.Find((f) => f.Entity is Strawberry);
            if (follower != null && ModSettings.ButtonTargetGoldenModifier.Check) {
                follower.MoveTowardsLeader = false;
                Strawberry berry = (Strawberry)follower.Entity;
                if (!ModSettings.ButtonSetSubpixelModifier.Check) {
                    float distance = 1f;

                    if (ModSettings.ButtonMovePlayerUp.Pressed) {
                        berry.Position.Y -= distance;
                    }
                    if (ModSettings.ButtonMovePlayerDown.Pressed) {
                        berry.Position.Y += distance;
                    }
                    if (ModSettings.ButtonMovePlayerLeft.Pressed) {
                        berry.Position.X -= distance;
                    }
                    if (ModSettings.ButtonMovePlayerRight.Pressed) {
                        berry.Position.X += distance;
                    }
                }
            } else {
                if (!ModSettings.ButtonSetSubpixelModifier.Check) {
                    float distance = ModSettings.ButtonMovePlayerModifier.Check ? ModSettings.MovePlayerModifiedStep : 1f;

                    if (ModSettings.ButtonMovePlayerUp.Pressed) {
                        player.MoveV(-distance);
                    }
                    if (ModSettings.ButtonMovePlayerDown.Pressed) {
                        player.MoveV(distance);
                    }
                    if (ModSettings.ButtonMovePlayerLeft.Pressed) {
                        player.MoveH(-distance);
                    }
                    if (ModSettings.ButtonMovePlayerRight.Pressed) {
                        player.MoveH(distance);
                    }
                }

                if (ModSettings.ButtonSetSubpixelModifier.Check) {
                    if (ModSettings.ButtonMovePlayerUp.Pressed) {
                        float moveV = -player.PositionRemainder.Y;
                        player.MoveV(moveV - 0.5f);
                    }
                    if (ModSettings.ButtonMovePlayerDown.Pressed) {
                        float moveV = 1 - player.PositionRemainder.Y;
                        player.MoveV(moveV - 0.5f);
                    }
                    if (ModSettings.ButtonMovePlayerLeft.Pressed) {
                        float moveH = -player.PositionRemainder.X;
                        player.MoveH(moveH - 0.5f);
                    }
                    if (ModSettings.ButtonMovePlayerRight.Pressed) {
                        float moveH = 1 - player.PositionRemainder.X;
                        player.MoveH(moveH - 0.5f);
                    }
                }
            }

            
            foreach (KeyValuePair<string, ButtonBinding> entry in ModSettings.ButtonsConsoleCommands) {
                if (entry.Value.Pressed) {
                    string buttonName = entry.Key;
                    string consoleCommand = ModSettings.ConsoleCommands[buttonName];

                    if (string.IsNullOrEmpty(consoleCommand)) {
                        Log($"Console command for button '{buttonName}' was null or empty!", LogLevel.Warn);
                        continue;
                    }
                    
                    try {
                        //Split first word from the rest of the string
                        string[] split = consoleCommand.Split(new char[] { ' ' }, 2);
                        string command = split[0].ToLower();
                        string[] args = split.Length > 1 ? split[1].Split(' ') : new string[0];

                        Log($"Executing button '{buttonName}' with command '{consoleCommand}' -> '{command}' with args '{string.Join("', '", args)}'");
                        Engine.Commands.ExecuteCommand(command, args);
                    } catch (Exception ex) {
                        Log($"Exception while executing button '{buttonName}' with command '{consoleCommand}': {ex}", LogLevel.Warn);
                    }
                }
            }
        }


        int logCounter = 0;
        private void SpeedrunTimerDisplay_DrawTime(On.Celeste.SpeedrunTimerDisplay.orig_DrawTime orig, Vector2 position, string timeString, float scale, bool valid, bool finished, bool bestTime, float alpha) {
            if (!ModSettings.EnableMapTimer && !RoomTimerEnabled) {
                orig(position, timeString, scale, valid, finished, bestTime, alpha);
                return;
            }
            TimeSpan timeSpan = TimeSpan.FromTicks(SaveData.Instance.Time);
            int num = (int)timeSpan.TotalHours;
            string fileString = num + timeSpan.ToString("\\:mm\\:ss\\.fff");

            TimeSpan timeSpanRun = TimeSpan.FromTicks(SaveData.Instance.CurrentSession.Time);
            string runString = ((!(timeSpanRun.TotalHours >= 1.0)) ? timeSpanRun.ToString("mm\\:ss") : ((int)timeSpanRun.TotalHours + ":" + timeSpanRun.ToString("mm\\:ss")));

            string mapTimeString = "";
            try {
                AreaKey area = SaveData.Instance.CurrentSession.Area;
                AreaStats areaStats = SaveData.Instance.Areas_Safe[area.ID];
                long time = areaStats.Modes[(int)area.Mode].TimePlayed;
                TimeSpan timeSpan2 = TimeSpan.FromTicks(time);
                int num2 = (int)timeSpan2.TotalHours;
                mapTimeString = num2 + timeSpan2.ToString("\\:mm\\:ss\\.fff");

            } catch (Exception ex) {
                logCounter++;
                if (logCounter > 300) {
                    logCounter = 0;
                    Logger.LogDetailed(ex, "viddiesToolbox");
                }
            }

            string roomTimeString = "";
            if (ConsistencyTrackerLoaded) {
                long timeSpent = GetTimeSpentInCurrentRoom();
                TimeSpan timeSpanRoom = TimeSpan.FromTicks(timeSpent);
                roomTimeString = ((!(timeSpanRoom.TotalHours >= 1.0)) ? timeSpanRoom.ToString("mm\\:ss") : ((int)timeSpanRoom.TotalHours + ":" + timeSpanRoom.ToString("mm\\:ss")));
            }

            if (timeString == fileString) {
                if (ModSettings.EnableMapTimer) {
                    timeString = mapTimeString;
                } else if (RoomTimerEnabled) {
                    timeString = roomTimeString;
                }
            }
            if (timeString == runString && RoomTimerEnabled && ModSettings.EnableMapTimer) {
                timeString = roomTimeString;
            }

            orig(position, timeString, scale, valid, finished, bestTime, alpha);
        }

        private void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            orig(self, playerIntro, isFromLoader);
            self.Add(new LineupIndicatorEntity());
        }

        private Vector2 Input_GetAimVector(On.Celeste.Input.orig_GetAimVector orig, Facings defaultFacing) {
            if (!ModSettings.AnalogUseMoveDirectionsForDashing) {
                return orig(defaultFacing);
            }
            
            Vector2 value = Input.Aim.Value;
            if (value == Vector2.Zero) {
                if (SaveData.Instance != null && SaveData.Instance.Assists.DashAssist) {
                    return Input.LastAim;
                }

                Input.LastAim = Vector2.UnitX * (float)defaultFacing;
            } else if (SaveData.Instance != null && SaveData.Instance.Assists.ThreeSixtyDashing) {
                Input.LastAim = value.SafeNormalize();
            } else {
                int x = Math.Abs(value.X) > 0.3 ? Math.Sign(value.X) : 0;
                int y = Math.Abs(value.Y) > 0.7 ? Math.Sign(value.Y) : 0;
                Input.LastAim = new Vector2(x, y).SafeNormalize();
            }
       
            return Input.LastAim;
        }
        #endregion

        #region Analog Direction Stuff
        public void SetAnalogMoveDirectionsEnabled(bool enabled) {
            if (enabled) {
                LoadAnalogMoveDirections();
            } else {
                UnloadAnalogMoveDirections();
            }
        }
        
        private void LoadAnalogMoveDirections() {
            Log($"Injecting analog move direction override...");

            VirtualIntegerJointAxis moveX = new VirtualIntegerJointAxis(VirtualIntegerJointAxis.AxisType.X, Settings.Instance.Left, Settings.Instance.LeftMoveOnly, Settings.Instance.Right, Settings.Instance.RightMoveOnly, Input.Gamepad, 0.3f);
            VirtualIntegerJointAxis moveY = new VirtualIntegerJointAxis(VirtualIntegerJointAxis.AxisType.Y, Settings.Instance.Up, Settings.Instance.UpMoveOnly, Settings.Instance.Down, Settings.Instance.DownMoveOnly, Input.Gamepad, 0.3f);
            moveX.Other = moveY;
            moveY.Other = moveX;

            Input.MoveX = moveX;
            Input.MoveY = moveY;
            Input.GliderMoveY = moveY;
        }

        private void UnloadAnalogMoveDirections() {
            Log($"Removing analog move direction override...");
            
            Input.MoveX = new VirtualIntegerAxis(Settings.Instance.Left, Settings.Instance.LeftMoveOnly, Settings.Instance.Right, Settings.Instance.RightMoveOnly, Input.Gamepad, 0.3f);
            Input.MoveY = new VirtualIntegerAxis(Settings.Instance.Up, Settings.Instance.UpMoveOnly, Settings.Instance.Down, Settings.Instance.DownMoveOnly, Input.Gamepad, 0.7f);
            Input.GliderMoveY = new VirtualIntegerAxis(Settings.Instance.Up, Settings.Instance.UpMoveOnly, Settings.Instance.Down, Settings.Instance.DownMoveOnly, Input.Gamepad, 0.3f);
        }
        #endregion

        #region Log Stuff
        public void Log(string message, LogLevel level = LogLevel.Debug) {
            Logger.Log(level, "viddiesToolbox", message);
        }

        private bool _LoggedOnce = false;
        public void LogOnce(string message) {
            if (_LoggedOnce) return;
            _LoggedOnce = true;
            Log(message, LogLevel.Info);
        }
        public void ResetLogOnce() {
            _LoggedOnce = false;
        }
        #endregion

        #region Mod Menu Section Stuff
        protected override void CreateModMenuSectionKeyBindings(TextMenu menu, bool inGame, EventInstance snapshot) {
            //base.CreateModMenuSectionKeyBindings(menu, inGame, snapshot);
            menu.Add(new TextMenu.Button(Dialog.Clean("options_keyconfig")).Pressed(delegate {
                menu.Focused = false;
                Engine.Scene.Add(CreateCustomKeyboardConfigUI(menu));
                Engine.Scene.OnEndOfFrame += delegate {
                    Engine.Scene.Entities.UpdateLists();
                };
            }));
            menu.Add(new TextMenu.Button(Dialog.Clean("options_btnconfig")).Pressed(delegate {
                menu.Focused = false;
                Engine.Scene.Add(CreateCustomButtonConfigUI(menu));
                Engine.Scene.OnEndOfFrame += delegate {
                    Engine.Scene.Entities.UpdateLists();
                };
            }));
        }
        private Entity CreateCustomKeyboardConfigUI(TextMenu menu) {
            return new CustomModuleSettingsKeyboardConfigUI(this) {
                OnClose = () => menu.Focused = true
            };
        }
        private Entity CreateCustomButtonConfigUI(TextMenu menu) {
            return new CustomModuleSettingsButtonConfigUI(this) {
                OnClose = () => menu.Focused = true
            };
        }
        #endregion

        #region Button Binding Stuff
        public void InitializeButtonBinding(ButtonBinding buttonBinding) {
            if (buttonBinding == null) return;
            if (buttonBinding.Button != null) return;
            if (buttonBinding.Binding == null) return;
            
            buttonBinding.Button = new VirtualButton(buttonBinding.Binding, Input.Gamepad, 0.08f, 0.2f);
            buttonBinding.Button.AutoConsumeBuffer = true;
        }
        public void DeregisterButtonBinding(ButtonBinding buttonBinding) {
            if (buttonBinding == null) return;
            buttonBinding.Button?.Deregister();
        }

        public override void OnInputDeregister() {
            base.OnInputDeregister();

            //Deregister custom bindings
            foreach (KeyValuePair<string, ButtonBinding> entry in ModSettings.ButtonsConsoleCommands) {
                DeregisterButtonBinding(entry.Value);
            }
            foreach (ButtonBinding entry in ModSettings.ButtonsTeleportPoint) {
                DeregisterButtonBinding(entry);
            }
        }
        #endregion

        #region Speedrun Tool Support
        public void SpeedrunToolSaveState(Dictionary<Type, Dictionary<string, object>> savedvalues, Level level) {
            Log($"SaveState set", LogLevel.Debug);
            _TargetFreezeState = FreezeState.Normal;
        }

        public void SpeedrunToolLoadState(Dictionary<Type, Dictionary<string, object>> savedvalues, Level level) {
            Log($"SaveState loaded", LogLevel.Debug);
            _TargetFreezeState = FreezeState.Normal;
        }

        public void SpeedrunToolClearState() {}
        #endregion

        #region CCT Support
        private static long GetTimeSpentInCurrentRoom() {
            return ConsistencyTrackerModule.Instance.CurrentChapterStats.CurrentRoom.TimeSpentInRoom;
        }
        #endregion
    }
}