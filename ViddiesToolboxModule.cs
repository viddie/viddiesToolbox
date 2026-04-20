using Celeste.Mod.ConsistencyTracker;
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
using Util = Celeste.Mod.viddiesToolbox.Tools.Util;

namespace Celeste.Mod.viddiesToolbox {
    public class ViddiesToolboxModule : EverestModule {
        
        public static ViddiesToolboxModule Instance;
        private const string FREEZE_SOUND   = SFX.ui_game_pause;
        private const string UNFREEZE_SOUND = SFX.ui_game_unpause;
        private const string FRAME_ADVANCE_SOUND = SFX.ui_main_button_select;

        private const float FREEZE_TIME = 0.01666666f * 100;
        public override Type SettingsType => typeof(ModuleSettings);
        public ModuleSettings ModSettings => (ModuleSettings)this._Settings;

        private bool ConsistencyTrackerLoaded { get; set; } = false;
        private bool RoomTimerEnabled => ModSettings.EnableRoomTimer && ConsistencyTrackerLoaded;

        private TimerType FileTimer {
            get {
                if (ModSettings.EnableCampaignTimer) return TimerType.Campaign;
                if (ModSettings.EnableMapTimer) return TimerType.Map;
                if (RoomTimerEnabled) return TimerType.Room;
                return TimerType.Default;
            }
        }
        private TimerType RunTimer {
            get {
                if (RoomTimerEnabled && (ModSettings.EnableCampaignTimer || ModSettings.EnableMapTimer)) return TimerType.Room;
                return TimerType.Default;
            }
        }
        

        private FreezeState engineFrozenState = FreezeState.Normal;
        private float savedFreezeTimer = float.NaN;
        private int advanceFrames = -1;

        private readonly TeleportPoints teleports = new TeleportPoints();

        public ViddiesToolboxModule() {
            Instance = this;

            Logger.SetLogLevel("viddiesToolbox/", LogLevel.Info);
        }

        public override void Load() {
            On.Monocle.Engine.Update += Engine_Update;
            On.Celeste.SpeedrunTimerDisplay.DrawTime += SpeedrunTimerDisplay_DrawTime;
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
            On.Celeste.Input.GetAimVector += Input_GetAimVector;

            teleports.Hook();
        }

        public override void Unload() {
            On.Monocle.Engine.Update -= Engine_Update;
            On.Celeste.SpeedrunTimerDisplay.DrawTime -= SpeedrunTimerDisplay_DrawTime;
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
            On.Celeste.Input.GetAimVector -= Input_GetAimVector;

            teleports.UnHook();
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
            foreach (var entry in ModSettings.ButtonsConsoleCommands) {
                InitializeButtonBinding(entry.Value);
            }
            foreach (ButtonBinding entry in ModSettings.ButtonsTeleportPoint) {
                InitializeButtonBinding(entry);
            }
        }

        #region Hooks

        private FreezeState? targetFreezeState;
        private void EnginePreUpdate() {
            FreezeState newState = engineFrozenState;
            
            if (ModSettings.ButtonToggleFreezeEngine.Pressed && ModSettings.HotkeysEnabled) {
                if (engineFrozenState == FreezeState.Normal) {
                    targetFreezeState = FreezeState.Frozen;
                    //Log($"Freezing engine | FreezeTimer: {Engine.FreezeTimer}, SavedFreezeTimer: {_SavedFreezeTimer} | DeltaTime: {Engine.DeltaTime}, RawDeltaTime: {Engine.RawDeltaTime}");
                } else {
                    targetFreezeState = FreezeState.Normal;
                    //Log("Unfreezing engine");
                }
                //Log($"Freeze state check: Current: {EngineFrozenState}, New: {_TargetFreezeState}");
                //ResetLogOnce();
            }

            if (targetFreezeState != null) { 
                newState = targetFreezeState.Value;
                targetFreezeState = null;
            }

            bool doFrameAdvance = (ModSettings.ButtonAdvanceFrame.Pressed || ModSettings.ButtonAdvanceMultipleFrames.Pressed) && ModSettings.HotkeysEnabled;
            switch (engineFrozenState) {
                case FreezeState.Normal when newState == FreezeState.Frozen: //Previously normal, now frozen
                    savedFreezeTimer = Engine.FreezeTimer;
                    Engine.FreezeTimer = FREEZE_TIME;
                    Audio.Play(FREEZE_SOUND);
                    break;
                case FreezeState.Frozen when newState == FreezeState.Normal: //Previously frozen, now normal
                    Engine.FreezeTimer = savedFreezeTimer;
                    savedFreezeTimer = float.NaN;
                    Audio.Play(UNFREEZE_SOUND);
                    break;
                case FreezeState.Frozen when newState == FreezeState.Frozen: {
                    if (doFrameAdvance && advanceFrames == -1) {
                        advanceFrames = ModSettings.ButtonAdvanceMultipleFrames.Pressed ? ModSettings.FreezeEngineMultipleFrames : 1;
                        Engine.FreezeTimer = savedFreezeTimer;
                        Audio.Play(FRAME_ADVANCE_SOUND);
                        ResetLogOnce();
                    }

                    if (advanceFrames > 0) {
                        ResetLogOnce();
                        advanceFrames--;
                    } else if (advanceFrames == 0) {
                        advanceFrames = -1;
                        savedFreezeTimer = Engine.FreezeTimer;
                        Engine.FreezeTimer = FREEZE_TIME;
                    } else {
                        Engine.FreezeTimer = FREEZE_TIME;
                    }
                    
                    break;
                }
            }

            LogOnce($"Engine.FreezeTime: {Engine.FreezeTimer}, _SavedFreezeTimer: {savedFreezeTimer}");
            engineFrozenState = newState;
        }

        private void Engine_Update(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gameTime) {
            orig(self, gameTime);

            if (ModSettings.ToggleHotkeys.Pressed) {
                ModSettings.HotkeysEnabled = !ModSettings.HotkeysEnabled;
                Log($"Hotkeys enabled: {ModSettings.HotkeysEnabled}", LogLevel.Info);
            }

            if (ModSettings.ToggleAnalogFix.Pressed) {
                ModSettings.AnalogUseDashDirectionsForMovement = !ModSettings.AnalogUseDashDirectionsForMovement;
                SetAnalogMoveDirectionsEnabled(ModSettings.AnalogUseDashDirectionsForMovement);
                Log($"Analog fix (Dash for Movement) enabled: {ModSettings.AnalogUseDashDirectionsForMovement}", LogLevel.Info);
            }
            
            if (!(Engine.Scene is Level level)) return;
            if (!level.Paused) EnginePreUpdate();

            Player player = level.Tracker.GetEntity<Player>();
            if (player == null) return;

            UpdateHotkeyPresses(player);
        }

        private void UpdateHotkeyPresses(Player player) {
            if (ModSettings == null) {
                Log($"'ModSettings' was null!", LogLevel.Warn);
                return;
            }
            if (ModSettings.ButtonMovePlayerUp == null) {
                Log($"'ButtonMovePlayer1PixelUp' was null!", LogLevel.Warn);
                return;
            }
            if (!ModSettings.HotkeysEnabled) return;

            Follower follower = player.Leader.Followers.Find(f => f.Entity is Strawberry);
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
                        
                        // Engine.Commands.commandHistory is private, but the command needs to be added to it for other special commands to work. Its a simple list of strings containing the full text (command and parameters) to execute.
                        var commandHistory = Util.GetPrivateProperty<List<string>>(Engine.Commands, "commandHistory");
                        commandHistory?.Insert(0, consoleCommand);
                        Engine.Commands.ExecuteCommand(command, args);
                    } catch (Exception ex) {
                        Log($"Exception while executing button '{buttonName}' with command '{consoleCommand}': {ex}", LogLevel.Warn);
                    }
                }
            }
        }


        private int logCounter;
        private void SpeedrunTimerDisplay_DrawTime(On.Celeste.SpeedrunTimerDisplay.orig_DrawTime orig, Vector2 position, string timeString, float scale, bool valid, bool finished, bool bestTime, float alpha) {
            if (!ModSettings.EnableMapTimer && !RoomTimerEnabled && !ModSettings.EnableCampaignTimer) {
                orig(position, timeString, scale, valid, finished, bestTime, alpha);
                return;
            }
            TimeSpan timeSpan = TimeSpan.FromTicks(SaveData.Instance.Time);
            int num = (int)timeSpan.TotalHours;
            string fileString = num + timeSpan.ToString(@"\:mm\:ss\.fff");

            TimeSpan timeSpanRun = TimeSpan.FromTicks(SaveData.Instance.CurrentSession.Time);
            string runString = !(timeSpanRun.TotalHours >= 1.0) ? timeSpanRun.ToString("mm\\:ss") : (int)timeSpanRun.TotalHours + ":" + timeSpanRun.ToString("mm\\:ss");

            string newTimeString = null;
            if (timeString == fileString) {
                switch (FileTimer) {
                    case TimerType.Campaign:
                        newTimeString = CalculateTimeString(TimerType.Campaign);
                        break;
                    case TimerType.Map:
                        newTimeString = CalculateTimeString(TimerType.Map);
                        break;
                    case TimerType.Room:
                        newTimeString = CalculateTimeString(TimerType.Room);
                        break;
                    case TimerType.Default:
                    default:
                        break;
                }
            }
            if (timeString == runString && RunTimer == TimerType.Room) {
                newTimeString = CalculateTimeString(TimerType.Room);
            }
            
            if (!string.IsNullOrEmpty(newTimeString)) {
                timeString = newTimeString;
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

        private bool loggedOnce;

        private void LogOnce(string message) {
            if (loggedOnce) return;
            loggedOnce = true;
            Log(message, LogLevel.Info);
        }

        private void ResetLogOnce() {
            loggedOnce = false;
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
        public static void InitializeButtonBinding(ButtonBinding buttonBinding) {
            if (buttonBinding == null) return;
            if (buttonBinding.Button != null) return;
            if (buttonBinding.Binding == null) return;
            
            buttonBinding.Button = new VirtualButton(buttonBinding.Binding, Input.Gamepad, 0.08f, 0.2f) {
                AutoConsumeBuffer = true
            };
        }
        public static void DeregisterButtonBinding(ButtonBinding buttonBinding) {
            buttonBinding?.Button?.Deregister();
        }

        public override void OnInputDeregister() {
            base.OnInputDeregister();

            //Deregister custom bindings
            foreach (var entry in ModSettings.ButtonsConsoleCommands) {
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
            targetFreezeState = FreezeState.Normal;
        }

        public void SpeedrunToolLoadState(Dictionary<Type, Dictionary<string, object>> savedvalues, Level level) {
            Log($"SaveState loaded", LogLevel.Debug);
            targetFreezeState = FreezeState.Normal;
        }

        public void SpeedrunToolClearState() {}
        #endregion

        #region CCT Support
        private static long GetTimeSpentInCurrentRoom() {
            return ConsistencyTrackerModule.Instance.CurrentChapterStats.CurrentRoom.TimeSpentInRoom;
        }
        #endregion

        #region Timer Stuff
        private string CalculateTimeString(TimerType type) {
            logCounter++;
            if (type == TimerType.Campaign) {
                try {
                    AreaKey areaKey = SaveData.Instance.CurrentSession.Area;
                    LevelSetStats lss = SaveData.Instance.GetLevelSetStatsFor(areaKey.LevelSet);
                    long totalTime = 0;
                    if (lss != null) {
                        totalTime += lss.AreasIncludingCeleste.Sum(area => area.TotalTimePlayed);
                    }
                    TimeSpan timeSpan2 = TimeSpan.FromTicks(totalTime);
                    int num2 = (int)timeSpan2.TotalHours;
                    return num2 + timeSpan2.ToString(@"\:mm\:ss\.fff");
                } catch (Exception ex) {
                    if (logCounter % 3600 == 0) {
                        logCounter = 0;
                        Logger.LogDetailed(ex, "viddiesToolbox");
                    }
                }
            } else if (type == TimerType.Map) {
                try {
                    AreaKey area = SaveData.Instance.CurrentSession.Area;
                    AreaStats areaStats = SaveData.Instance.Areas_Safe[area.ID];
                    long time = areaStats.Modes[(int)area.Mode].TimePlayed;
                    TimeSpan timeSpan2 = TimeSpan.FromTicks(time);
                    int num2 = (int)timeSpan2.TotalHours;
                    return num2 + timeSpan2.ToString(@"\:mm\:ss\.fff");
                } catch (Exception ex) {
                    if (logCounter % 3600 == 0) {
                        Logger.LogDetailed(ex, "viddiesToolbox");
                    }
                }
            } else if (type == TimerType.Room) {
                if (!ConsistencyTrackerLoaded) return null;
                long timeSpent = GetTimeSpentInCurrentRoom();
                TimeSpan timeSpanRoom = TimeSpan.FromTicks(timeSpent);
                if (!(timeSpanRoom.TotalHours >= 1.0)) {
                    return timeSpanRoom.ToString(@"mm\:ss");
                }
                return (int)timeSpanRoom.TotalHours + ":" + timeSpanRoom.ToString(@"mm\:ss");
            }
            return null;
        }

        private enum TimerType {
            Campaign,
            Map,
            Room,
            Default,
        }
        #endregion
    }
}