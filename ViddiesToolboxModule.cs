using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System;

namespace Celeste.Mod.viddiesToolbox {
    public class ViddiesToolboxModule : EverestModule {
        
        public static ViddiesToolboxModule Instance;


        public override Type SettingsType => typeof(ModuleSettings);
        public ModuleSettings ModSettings => (ModuleSettings)this._Settings;

        public ViddiesToolboxModule() {
            Instance = this;
        }

        public override void Load() {
            On.Monocle.Engine.Update += Engine_Update;
        }

        private void Engine_Update(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gameTime) {
            orig(self, gameTime);

            if (!(Engine.Scene is Level)) return;
            Level level = Engine.Scene as Level;
            Player player = level.Tracker.GetEntity<Player>();

            if (player == null) return;

            UpdateHotkeyPresses(player);
        }

        public void UpdateHotkeyPresses(Player player) {
            if (ModSettings == null) {
                Log($"'ModSettings' was null!");
                return;
            }
            if (ModSettings.ButtonMovePlayerUp == null) {
                Log($"'ButtonMovePlayer1PixelUp' was null!");
                return;
            }

            if (!ModSettings.ButtonSetSubpixelModifier.Check) {
                float distance = ModSettings.ButtonMoveOneTenthPixelModifier.Check ? 0.1f : 1f;

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

        public override void Unload() {
            On.Monocle.Engine.Update -= Engine_Update;
        }


        public void Log(string message) {
            Logger.Log("viddiesToolbox", message);
        }
    }
}
