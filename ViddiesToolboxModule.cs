using Celeste.Mod.viddiesToolbox.Enums;
using Celeste.Mod.viddiesToolbox.Menu;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using MonoMod;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.viddiesToolbox {
    public class ViddiesToolboxModule : EverestModule {
        
        public static ViddiesToolboxModule Instance;
        public static string FreezeSound   = SFX.ui_game_pause;
        public static string UnfreezeSound = SFX.ui_game_unpause;
        public static string FrameAdvanceSound = SFX.ui_main_button_select;

        public override Type SettingsType => typeof(ModuleSettings);
        public ModuleSettings ModSettings => (ModuleSettings)this._Settings;

        private FreezeState EngineFrozenState = FreezeState.Normal;
        private float _SavedFreezeTimer = float.NaN;
        private bool _DidFrameAdvance = false;

        public ViddiesToolboxModule() {
            Instance = this;

            Logger.SetLogLevel("viddiesToolbox/", LogLevel.Info);
        }

        public override void Load() {
            On.Monocle.Engine.Update += Engine_Update;
        }

        public override void OnInputInitialize() {
            base.OnInputInitialize();

            //Register custom bindings
            foreach (KeyValuePair<string, ButtonBinding> entry in ModSettings.ButtonsConsoleCommands) {
                InitializeButtonBinding(entry.Value);
            }
        }

        public void EnginePreUpdate() {
            FreezeState newState = EngineFrozenState;
            
            if (ModSettings.ButtonToggleFreezeEngine.Pressed && ModSettings.HotkeysEnabled) {
                if (EngineFrozenState == FreezeState.Normal) {
                    newState = FreezeState.Frozen;
                    Log($"Freezing engine | FreezeTimer: {Engine.FreezeTimer}, SavedFreezeTimer: {_SavedFreezeTimer} | DeltaTime: {Engine.DeltaTime}, RawDeltaTime: {Engine.RawDeltaTime}");
                } else {
                    newState = FreezeState.Normal;
                    Log("Unfreezing engine");
                }
                Log($"Freeze state check: Current: {EngineFrozenState}, New: {newState}");
                ResetLogOnce();
            }


            bool doFrameAdvance = ModSettings.ButtonAdvanceFrame.Pressed && ModSettings.HotkeysEnabled;
            if (EngineFrozenState == FreezeState.Normal && newState == FreezeState.Frozen) { //Previously normal, now frozen
                _SavedFreezeTimer = Engine.FreezeTimer;
                Engine.FreezeTimer = 0.01666666f * 1000;
                Audio.Play(FreezeSound);
                
            } else if (EngineFrozenState == FreezeState.Frozen && newState == FreezeState.Normal) { //Previously frozen, now normal
                Engine.FreezeTimer = _SavedFreezeTimer;
                _SavedFreezeTimer = float.NaN;
                Audio.Play(UnfreezeSound);

            } else if (EngineFrozenState == FreezeState.Frozen && newState == FreezeState.Frozen) {
                if (_DidFrameAdvance) {
                    _SavedFreezeTimer = Engine.FreezeTimer;
                    _DidFrameAdvance = false;
                }
                
                if (doFrameAdvance) {
                    Engine.FreezeTimer = _SavedFreezeTimer;
                    ResetLogOnce();
                    _DidFrameAdvance = true;
                    Audio.Play(SFX.ui_main_button_select);
                } else {
                    Engine.FreezeTimer = 0.01666666f * 1000;
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

        public override void Unload() {
            On.Monocle.Engine.Update -= Engine_Update;
        }

        public void Log(string message, LogLevel level = LogLevel.Debug) {
            Logger.Log(level, "viddiesToolbox/all", message);
        }

        private bool _LoggedOnce = false;
        public void LogOnce(string message) {
            if (_LoggedOnce) return;
            _LoggedOnce = true;
            Log(message);
        }
        public void ResetLogOnce() {
            _LoggedOnce = false;
        }

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
        }
    }
}
