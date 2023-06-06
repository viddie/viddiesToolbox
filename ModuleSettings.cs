using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.viddiesToolbox {
    public class ModuleSettings : EverestModuleSettings {

        [SettingIgnore]
        private ViddiesToolboxModule Mod => ViddiesToolboxModule.Instance;

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

    }
}
