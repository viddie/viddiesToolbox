using Celeste.Mod.viddiesToolbox.Menu;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.viddiesToolbox {
    public class ModuleSettings : EverestModuleSettings {

        [SettingIgnore]
        private ViddiesToolboxModule Mod => ViddiesToolboxModule.Instance;

        #region Move Player Keybinds
        public float MovePlayerModifiedStep { get; set; } = 0.1f;

        public ButtonBinding ButtonMovePlayerUp { get; set; }
        public ButtonBinding ButtonMovePlayerDown { get; set; }
        public ButtonBinding ButtonMovePlayerLeft { get; set; }
        public ButtonBinding ButtonMovePlayerRight { get; set; }

        public ButtonBinding ButtonMovePlayerModifier { get; set; }
        public ButtonBinding ButtonSetSubpixelModifier { get; set; }

        public bool MovePlayerMenu { get; set; }

        public void CreateMovePlayerMenuEntry(TextMenu menu, bool inGame) {
            List<float> MoveSteps = new List<float>() {
                0.2f, 0.1f, 0.05f, 0.01f, 0.005f, 0.001f,
            };
            menu.Add(new CustomEnumerableSlider<float>("Move Player Modified Distance", MoveSteps, (v) => v.ToString(), MovePlayerModifiedStep) {
                OnValueChange = (v) => {
                    MovePlayerModifiedStep = v;
                },
            });
        }
        #endregion

        #region Freeze Engine Keybinds
        public ButtonBinding ButtonToggleFreezeEngine { get; set; }
        public ButtonBinding ButtonAdvanceFrame { get; set; }
        #endregion

        #region Map Timer
        public bool EnableMapTimer { get; set; } = false;
        #endregion

        #region Lineup Helper
        [SettingIgnore]
        public bool DemoLineupEnabled { get; set; } = false;
        [SettingIgnore]
        public string DemoLineupSelectedTech { get; set; } = "Full Jump";
        public ButtonBinding ButtonDemoLineupNextTech { get; set; }
        #endregion

        #region Analog Direction Fixer
        public bool AnalogUseDashDirectionsForMovement { get; set; } = false;
        public void CreateAnalogUseDashDirectionsForMovementEntry(TextMenu menu, bool inGame) {
            menu.Add(new TextMenu.OnOff("Analog: Use Dash Directions For Moving", AnalogUseDashDirectionsForMovement) {
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
        public bool HotkeysEnabled { get; set; } = true;
        public ButtonBinding ToggleHotkeys { get; set; }
        #endregion

        #region Arbitrary Console Commands
        public Dictionary<string, ButtonBinding> ButtonsConsoleCommands { get; set; } = new Dictionary<string, ButtonBinding>() {
            ["Button 1"] = new ButtonBinding(),
        };
        public Dictionary<string, string> ConsoleCommands { get; set; } = new Dictionary<string, string>() {
            ["Button 1"] = "Invoke Player.MoveV -100",
        };

        public string ConsoleCommandSelected { get; set; } = "Button 1";
        public bool ConsoleCommandMenu { get; set; }
        public void CreateConsoleCommandMenuEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("Bindable Console Commands", false);

            TextMenuExt.EnumerableSlider<string> sliderSelectedCommand = new TextMenuExt.EnumerableSlider<string>("Selected Command", ButtonsConsoleCommands.Keys, ConsoleCommandSelected);
            TextMenu.SubHeader headerButtonCommand = new TextMenu.SubHeader($"Command: {ConsoleCommands[ConsoleCommandSelected]}", topPadding: false);

            TextMenu.Button buttonAddCommand = new TextMenu.Button("Add New Command");
            TextMenu.Button buttonDeleteCommand = new TextMenu.Button("Delete Selected Command");
            TextMenu.Button buttonImportButtonName = new TextMenu.Button("Import Command Name from Clipboard");
            TextMenu.Button buttonImportCommand = new TextMenu.Button("Import Console Command from Clipboard");

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

                Mod.InitializeButtonBinding(ButtonsConsoleCommands[newButtonName]);
            };

            buttonDeleteCommand.OnPressed = () => {
                if (ButtonsConsoleCommands.Count <= 1) return;

                ButtonsConsoleCommands.Remove(ConsoleCommandSelected);
                ConsoleCommands.Remove(ConsoleCommandSelected);

                sliderSelectedCommand.Values.RemoveAt(sliderSelectedCommand.Index);
                sliderSelectedCommand.Index = 0;
                sliderSelectedCommand.SelectWiggler.Start();

                ConsoleCommandSelected = sliderSelectedCommand.Values[0].Item1;
                headerButtonCommand.Title = $"Command: {ConsoleCommands[ConsoleCommandSelected]}";
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
                headerButtonCommand.Title = $"Command: {text}";
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
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("Other", false);

            //Analog directions
            subMenu.Add(new TextMenu.OnOff("Analog: Use Move Directions For Dashing", AnalogUseMoveDirectionsForDashing) {
                OnValueChange = v => {
                    AnalogUseMoveDirectionsForDashing = v;
                }
            });

            //Demo lineup helper
            subMenu.Add(new TextMenu.OnOff("Demo Lineup Enabled", DemoLineupEnabled) {
                OnValueChange = v => {
                    DemoLineupEnabled = v;
                }
            });
            List<string> techList = new List<string>() {
                "Full Jump",
                "Up Dash Buffer", "Up-Diagonal Dash Buffer", "Down Dash Buffer", "Down-Diagonal Dash Buffer", "Horizontal Dash Buffer",
                "Max Height Hyper",
            };
            subMenu.Add(new TextMenuExt.EnumerableSlider<string>("Demo Lineup Tech", techList, DemoLineupSelectedTech) {
                OnValueChange = (v) => {
                    DemoLineupSelectedTech = v;
                },
            });

            menu.Add(subMenu);
        }
        #endregion
    }
}
