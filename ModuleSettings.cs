using Celeste.Mod.viddiesToolbox.Menu;
using Celeste.Mod.viddiesToolbox.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.viddiesToolbox {

    [SettingName("VIDDIES_TOOLBOX_NAME")]    
    public class ModuleSettings : EverestModuleSettings {

        [SettingIgnore]
        private ViddiesToolboxModule Mod => ViddiesToolboxModule.Instance;

        #region Map Timer
        [SettingIgnore]
        public bool EnableCampaignTimer { get; set; } = false;
        [SettingIgnore]
        public bool EnableMapTimer { get; set; } = false;

        public bool Timer { get; set; } = false;
        public void CreateTimerEntry(TextMenu menu, bool inGame) {
            TextMenu.OnOff campaignEntry = new TextMenu.OnOff(Dialog.Clean("VIDDIES_TOOLBOX_CAMPAIGN_TIMER"), EnableCampaignTimer);
            TextMenu.OnOff mapEntry = new TextMenu.OnOff(Dialog.Clean("VIDDIES_TOOLBOX_MAP_TIMER"), EnableMapTimer);
            campaignEntry.OnValueChange = v => {
                EnableCampaignTimer = v;
                if (!v || !EnableMapTimer) return;
                EnableMapTimer = false;
                mapEntry.Index = 0;
                mapEntry.SelectWiggler.Start();
                mapEntry.ValueWiggler.Start();
            };
            mapEntry.OnValueChange = v => {
                EnableMapTimer = v;
                if (!v || !EnableCampaignTimer) return;
                EnableCampaignTimer = false;
                campaignEntry.Index = 0;
                campaignEntry.SelectWiggler.Start();
                campaignEntry.ValueWiggler.Start();
            };
            menu.Add(campaignEntry);
            menu.Add(mapEntry);
        }

        [SettingName("VIDDIES_TOOLBOX_ROOM_TIMER")]
        public bool EnableRoomTimer { get; set; } = false;
        
        #endregion

        #region Analog Direction Fixer
        public bool AnalogUseDashDirectionsForMovement { get; set; } = false;
        public void CreateAnalogUseDashDirectionsForMovementEntry(TextMenu menu, bool inGame) {
            menu.Add(new TextMenu.OnOff(Dialog.Clean("VIDDIES_TOOLBOX_ANALOG_DASH_MOVEMENT"), AnalogUseDashDirectionsForMovement) {
                OnValueChange = v => {
                    AnalogUseDashDirectionsForMovement = v;
                    ViddiesToolboxModule.Instance.SetAnalogMoveDirectionsEnabled(v);
                }
            });
        }

        [SettingIgnore]
        public bool AnalogUseMoveDirectionsForDashing { get; set; } = false;
        #endregion

        #region Hotkeys
        [SettingName("VIDDIES_TOOLBOX_HOTKEYS_ENABLED")]
        public bool HotkeysEnabled { get; set; } = true;
        [SettingName("VIDDIES_TOOLBOX_TOGGLE_HOTKEYS")]
        public ButtonBinding ToggleHotkeys { get; set; }
        [SettingName("VIDDIES_TOOLBOX_TOGGLE_ANALOG_FIX")]
        public ButtonBinding ToggleAnalogFix { get; set; }
        #endregion

        #region Lineup Helper
        [SettingIgnore]
        public bool DemoLineupEnabled { get; set; } = false;
        [SettingIgnore]
        public string DemoLineupSelectedTech { get; set; } = "Full Jump";

        [SettingName("VIDDIES_TOOLBOX_DEMO_LINEUP_NEXT_TECH")]
        public ButtonBinding ButtonDemoLineupNextTech { get; set; }
        #endregion

        #region Move Player Keybinds
        public float MovePlayerModifiedStep { get; set; } = 0.1f;

        [SettingSubHeader("VIDDIES_TOOLBOX_MOVE_PLAYER")]
        [SettingName("VIDDIES_TOOLBOX_MOVE_PLAYER_UP")]
        public ButtonBinding ButtonMovePlayerUp { get; set; }
        [SettingName("VIDDIES_TOOLBOX_MOVE_PLAYER_DOWN")]
        public ButtonBinding ButtonMovePlayerDown { get; set; }
        [SettingName("VIDDIES_TOOLBOX_MOVE_PLAYER_LEFT")]
        public ButtonBinding ButtonMovePlayerLeft { get; set; }
        [SettingName("VIDDIES_TOOLBOX_MOVE_PLAYER_RIGHT")]
        public ButtonBinding ButtonMovePlayerRight { get; set; }

        [SettingName("VIDDIES_TOOLBOX_MOVE_PLAYER_MODIFIER")]
        public ButtonBinding ButtonMovePlayerModifier { get; set; }
        [SettingName("VIDDIES_TOOLBOX_SET_SUBPIXEL_MODIFIER")]
        public ButtonBinding ButtonSetSubpixelModifier { get; set; }
        [SettingName("VIDDIES_TOOLBOX_TARGET_GOLDEN_MODIFIER")]
        public ButtonBinding ButtonTargetGoldenModifier { get; set; }

        public bool MovePlayerMenu { get; set; }

        public void CreateMovePlayerMenuEntry(TextMenu menu, bool inGame) {
            List<float> MoveSteps = new List<float>() {
                0.2f, 0.1f, 0.05f, 0.01f, 0.005f, 0.001f, 0.0005f, 0.0001f, 0.00005f, 0.00001f, 0.000005f, 0.000001f
            };
            menu.Add(new CustomEnumerableSlider<float>(Dialog.Clean("VIDDIES_TOOLBOX_MOVE_PLAYER_MODIFIED_DISTANCE"), MoveSteps, (v) => v.ToString(), MovePlayerModifiedStep) {
                OnValueChange = (v) => {
                    MovePlayerModifiedStep = v;
                },
            });
        }
        #endregion

        #region Freeze Engine Keybinds
        [SettingRange(1, 99999)]
        [SettingName("VIDDIES_TOOLBOX_FREEZE_ENGINE_MULTIPLE_FRAMES")]
        public int FreezeEngineMultipleFrames { get; set; } = 1;
        
        [SettingSubHeader("VIDDIES_TOOLBOX_FREEZE_ENGINE")]
        [SettingName("VIDDIES_TOOLBOX_FREEZE_ENGINE_TOGGLE")]
        public ButtonBinding ButtonToggleFreezeEngine { get; set; }
        [SettingName("VIDDIES_TOOLBOX_FREEZE_ENGINE_ADVANCE_FRAME")]
        public ButtonBinding ButtonAdvanceFrame { get; set; }
        [SettingName("VIDDIES_TOOLBOX_FREEZE_ENGINE_ADVANCE_MULTIPLE_FRAMES")]
        public ButtonBinding ButtonAdvanceMultipleFrames { get; set; }
        #endregion

        #region Teleport Points
        [SettingIgnore]
        public int TeleportPointsMax { get; set; } = 5;
        public List<TeleportPoints.PositionData> TeleportPoints = new List<TeleportPoints.PositionData>() {
            null, null, null, null, null
        };
        [SettingSubHeader("VIDDIES_TOOLBOX_TELEPORT_POINTS")]
        [SettingName("VIDDIES_TOOLBOX_TELEPORT_POINTS")]
        public List<ButtonBinding> ButtonsTeleportPoint { get; set; } = new List<ButtonBinding>() {
            new ButtonBinding(),
            new ButtonBinding(),
            new ButtonBinding(),
            new ButtonBinding(),
            new ButtonBinding(),
        };
        [SettingName("VIDDIES_TOOLBOX_TELEPORT_POINTS_BUTTONS_RESPAWN_MODIFIER")]
        public ButtonBinding TeleportPointSetRespawnModifier { get; set; }
        [SettingName("VIDDIES_TOOLBOX_TELEPORT_POINTS_BUTTONS_CLEAR_MODIFIER")]
        public ButtonBinding TeleportPointClearModifier { get; set; }

        public bool TeleportPointsMenu { get; set; }
        public void CreateTeleportPointsMenuEntry(TextMenu menu, bool inGame) {
            menu.Add(new TextMenu.Slider(Dialog.Clean("VIDDIES_TOOLBOX_TELEPORT_POINTS"), i => i.ToString(), 1, 100, TeleportPointsMax) {
                OnValueChange = (v) => {
                    TeleportPointsMax = v;
                    while (TeleportPoints.Count > TeleportPointsMax) {
                        int lastIndex = TeleportPoints.Count - 1;
                        TeleportPoints.RemoveAt(lastIndex);
                        //Button Bindings
                        ViddiesToolboxModule.DeregisterButtonBinding(ButtonsTeleportPoint[lastIndex]);
                        ButtonsTeleportPoint.RemoveAt(lastIndex);
                    } 
                    while (TeleportPoints.Count < TeleportPointsMax) {
                        TeleportPoints.Add(null);
                        //Button Bindings
                        ButtonBinding binding = new ButtonBinding();
                        ButtonsTeleportPoint.Add(binding);
                        ViddiesToolboxModule.InitializeButtonBinding(binding);
                    }
                }
            });
        }
        #endregion

        #region Arbitrary Console Commands
        [SettingSubHeader("VIDDIES_TOOLBOX_CONSOLE_COMMANDS")]
        public Dictionary<string, ButtonBinding> ButtonsConsoleCommands { get; set; } = new Dictionary<string, ButtonBinding>() {
            ["Button 1"] = new ButtonBinding(),
        };
        public Dictionary<string, string> ConsoleCommands { get; set; } = new Dictionary<string, string>() {
            ["Button 1"] = "Invoke Player.MoveV -100",
        };

        [SettingIgnore]
        public string ConsoleCommandSelected { get; set; } = "Button 1";
        public bool ConsoleCommandMenu { get; set; }
        public void CreateConsoleCommandMenuEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu(Dialog.Clean("VIDDIES_TOOLBOX_BINDABLE_CONSOLE_COMMANDS"), false);

            TextMenuExt.EnumerableSlider<string> sliderSelectedCommand = new TextMenuExt.EnumerableSlider<string>(Dialog.Clean("VIDDIES_TOOLBOX_BINDABLE_CONSOLE_COMMANDS_SELECTED_COMMAND"), ButtonsConsoleCommands.Keys, ConsoleCommandSelected);
            TextMenu.SubHeader headerButtonCommand = new TextMenu.SubHeader(String.Format(Dialog.Get("VIDDIES_TOOLBOX_BINDABLE_CONSOLE_COMMANDS_CURRENT_COMMAND"), ConsoleCommands[ConsoleCommandSelected]), topPadding: false);

            TextMenu.Button buttonAddCommand = new TextMenu.Button(Dialog.Clean("VIDDIES_TOOLBOX_BINDABLE_CONSOLE_COMMANDS_ADD_NEW_COMMAND"));
            TextMenu.Button buttonDeleteCommand = new TextMenu.Button(Dialog.Clean("VIDDIES_TOOLBOX_BINDABLE_CONSOLE_COMMANDS_DELETE_SELECTED_COMMAND"));
            TextMenu.Button buttonImportButtonName = new TextMenu.Button(Dialog.Clean("VIDDIES_TOOLBOX_BINDABLE_CONSOLE_COMMANDS_IMPORT_COMMAND_NAME_FROM_CLIPBOARD"));
            TextMenu.Button buttonImportCommand = new TextMenu.Button(Dialog.Clean("VIDDIES_TOOLBOX_BINDABLE_CONSOLE_COMMANDS_IMPORT_CONSOLE_COMMAND_FROM_CLIPBOARD"));

            sliderSelectedCommand.OnValueChange = (v) => {
                ConsoleCommandSelected = v;
                headerButtonCommand.Title = $"Command: {ConsoleCommands[ConsoleCommandSelected]}";
            };

            buttonAddCommand.OnPressed = () => {
                string newButtonName = "Button " + (ButtonsConsoleCommands.Count + 1);
                ButtonsConsoleCommands.Add(newButtonName, new ButtonBinding());
                ConsoleCommands.Add(newButtonName, "");

                sliderSelectedCommand.Values.Add(Tuple.Create(newButtonName, newButtonName));
                sliderSelectedCommand.SelectWiggler.Start();

                ViddiesToolboxModule.InitializeButtonBinding(ButtonsConsoleCommands[newButtonName]);
            };

            buttonDeleteCommand.OnPressed = () => {
                if (ButtonsConsoleCommands.Count <= 1) return;

                ButtonsConsoleCommands.Remove(ConsoleCommandSelected);
                ConsoleCommands.Remove(ConsoleCommandSelected);

                sliderSelectedCommand.Values.RemoveAt(sliderSelectedCommand.Index);
                sliderSelectedCommand.Index = 0;
                sliderSelectedCommand.SelectWiggler.Start();

                ConsoleCommandSelected = sliderSelectedCommand.Values[0].Item1;
                headerButtonCommand.Title = String.Format(Dialog.Get("VIDDIES_TOOLBOX_BINDABLE_CONSOLE_COMMANDS_CURRENT_COMMAND"), ConsoleCommands[ConsoleCommandSelected]);
            };

            buttonImportButtonName.OnPressed = () => {
                string text = TextInput.GetClipboardText();
                if (string.IsNullOrEmpty(text)) return;

                //Replace key with new key
                ButtonsConsoleCommands.Add(text, ButtonsConsoleCommands[ConsoleCommandSelected]);
                ButtonsConsoleCommands.Remove(ConsoleCommandSelected);

                //Replace commands key with new key
                ConsoleCommands.Add(text, ConsoleCommands[ConsoleCommandSelected]);
                ConsoleCommands.Remove(ConsoleCommandSelected);

                //Set new key
                ConsoleCommandSelected = text;

                //Modify slider
                sliderSelectedCommand.Values.Insert(sliderSelectedCommand.Index + 1, Tuple.Create(text, text));
                sliderSelectedCommand.Values.RemoveAt(sliderSelectedCommand.Index);
                sliderSelectedCommand.SelectWiggler.Start();
            };

            buttonImportCommand.OnPressed = () => {
                string text = TextInput.GetClipboardText();
                if (string.IsNullOrEmpty(text)) return;

                ConsoleCommands[ConsoleCommandSelected] = text;
                headerButtonCommand.Title = String.Format(Dialog.Get("VIDDIES_TOOLBOX_BINDABLE_CONSOLE_COMMANDS_CURRENT_COMMAND"), text);
            };


            subMenu.Add(sliderSelectedCommand);
            subMenu.Add(headerButtonCommand);

            subMenu.Add(buttonAddCommand);
            subMenu.Add(buttonDeleteCommand);
            subMenu.Add(buttonImportButtonName);
            subMenu.Add(buttonImportCommand);

            menu.Add(subMenu);
        }
        #endregion
        
        #region Other
        public bool OtherOptions { get; set; }
        public void CreateOtherOptionsEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu(Dialog.Clean("VIDDIES_TOOLBOX_OTHER"), false);

            //Analog directions
            subMenu.Add(new TextMenu.OnOff(Dialog.Clean("VIDDIES_TOOLBOX_ANALOG_MOVEMENT_DASH"), AnalogUseMoveDirectionsForDashing) {
                OnValueChange = v => {
                    AnalogUseMoveDirectionsForDashing = v;
                }
            });

            //Demo lineup helper
            subMenu.Add(new TextMenu.OnOff(Dialog.Clean("VIDDIES_TOOLBOX_OTHER_DEMO_LINEUP_ENABLED"), DemoLineupEnabled) {
                OnValueChange = v => {
                    DemoLineupEnabled = v;
                }
            });
            List<string> techList = new List<string>() {
                "Full Jump",
                "Up Dash Buffer", "Up-Diagonal Dash Buffer", "Down Dash Buffer", "Down-Diagonal Dash Buffer", "Horizontal Dash Buffer",
                "Max Height Hyper",
            };
            subMenu.Add(new TextMenuExt.EnumerableSlider<string>(Dialog.Clean("VIDDIES_TOOLBOX_OTHER_DEMO_LINEUP_TECH"), techList, DemoLineupSelectedTech) {
                OnValueChange = (v) => {
                    DemoLineupSelectedTech = v;
                },
            });

            menu.Add(subMenu);
        }
        #endregion
    }
}
