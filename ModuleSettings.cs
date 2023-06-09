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
        [SettingName("Move Player Up")]
        public ButtonBinding ButtonMovePlayerUp { get; set; }
        [SettingName("Move Player Down")]
        public ButtonBinding ButtonMovePlayerDown { get; set; }
        [SettingName("Move Player Left")]
        public ButtonBinding ButtonMovePlayerLeft { get; set; }
        [SettingName("Move Player Right")]
        public ButtonBinding ButtonMovePlayerRight { get; set; }

        [SettingName("Move 0.1 Subpixel Modifier")]
        public ButtonBinding ButtonMoveOneTenthPixelModifier { get; set; }

        [SettingName("Set Subpixel Modifier")]
        public ButtonBinding ButtonSetSubpixelModifier { get; set; }
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
            TextMenu.SubHeader headerButtonCommand = new TextMenu.SubHeader($"Command: {ConsoleCommands[ConsoleCommandSelected]}", topPadding:false);
            
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
                sliderSelectedCommand.Values.Insert(sliderSelectedCommand.Index+1, Tuple.Create(text, text));
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

        #region Freeze Engine Keybinds
        public ButtonBinding ButtonToggleFreezeEngine { get; set; }
        public ButtonBinding ButtonAdvanceFrame { get; set; }
        public bool IgnoreOtherFreezeFramesWhileFrameAdvancing { get; set; }
        #endregion
    }
}
