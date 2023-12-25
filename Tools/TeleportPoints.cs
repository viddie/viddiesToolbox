using FMOD;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.viddiesToolbox.Tools {
    public class TeleportPoints {

        private static ViddiesToolboxModule Mod => ViddiesToolboxModule.Instance;
        private static ModuleSettings Settings => Mod.ModSettings;

        private Vector2 _ApplyRemainder = Vector2.Zero;

        
        public TeleportPoints() {}

        public void Hook() {
            On.Celeste.Level.Update += Level_Update;
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
        }

        public void UnHook() {
            On.Celeste.Level.Update -= Level_Update;
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
        }

        private void Level_Update(On.Celeste.Level.orig_Update orig, Level self) {
            orig(self);

            for (int i = 0; i < Settings.ButtonsTeleportPoint.Count; i++) {
                if (!Settings.ButtonsTeleportPoint[i].Pressed) continue;
                if (Settings.TeleportPointsPositions.Count <= i) continue;

                Mod.Log($"Pressed teleport button {i}", LogLevel.Info);

                bool holdingModifier = Settings.TeleportPointClearModifier.Check; //See if modifier is being held
                bool pointIsEmpty = Settings.TeleportPointsPositions[i] == Vector2.Zero && Settings.TeleportPointsRemainders[i] == Vector2.Zero;

                if (holdingModifier) {
                    ClearTeleportPoint(i);
                    continue;
                }

                Player player = self.Tracker.GetEntity<Player>();
                if (player == null || player.Dead) continue;

                if (pointIsEmpty) {
                    SetTeleportPoint(player, self.Session, i);
                } else {
                    UseTeleportPoint(player, self.Session, i);
                }
            }
        }

        private void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            orig(self, playerIntro, isFromLoader);

            if (_ApplyRemainder != Vector2.Zero) {
                Player player = self.Tracker.GetEntity<Player>();
                ApplyRemainder(player, _ApplyRemainder);
                _ApplyRemainder = Vector2.Zero;
            }
        }

        public void SetTeleportPoint(Player player, Session session, int index) {
            Vector2 position = player.Position;
            Vector2 remainder = player.PositionRemainder;
            Settings.TeleportPointsPositions[index] = position;
            Settings.TeleportPointsRemainders[index] = remainder;
            Settings.TeleportPointsLevelNames[index] = session.Level;
            Mod.Log($"Set teleport point {index} to position: {position} remainder {remainder}", LogLevel.Info);
        }
        public void ClearTeleportPoint(int index) {
            Settings.TeleportPointsPositions[index] = Vector2.Zero;
            Settings.TeleportPointsRemainders[index] = Vector2.Zero;
            Mod.Log($"Cleared teleport point {index}", LogLevel.Info);
        }
        public void UseTeleportPoint(Player player, Session session, int index) {
            Vector2 position = Settings.TeleportPointsPositions[index];
            Vector2 remainder = Settings.TeleportPointsRemainders[index];
            string levelName = Settings.TeleportPointsLevelNames[index];
            string currentLevel = session.Level;
            bool isSameLevel = levelName == currentLevel;

            if (session.MapData.Levels.FirstOrDefault((LevelData lvl) => lvl.Name == levelName) == null) {
                Mod.Log($"Teleport Point {index}: Level '{levelName}' wasn't found in the current chapter", LogLevel.Info);
                return;
            }
 
            session.RespawnPoint = position;
            player.Position = position;

            if (!isSameLevel) {
                session.Level = levelName;
                Engine.Scene.Paused = true;
                Engine.Scene = new LevelLoader(session, position);
                _ApplyRemainder = remainder;
            } else {
                ApplyRemainder(player, remainder);
            }

            Mod.Log($"Teleport Point {index}: Teleported player to position: {position} remainder {remainder}", LogLevel.Info);
        }

        public void ApplyRemainder(Player player, Vector2 remainder) {
            if (player == null || player.Dead) return;

            player.ZeroRemainderX();
            player.ZeroRemainderY();
            player.MoveH(remainder.X);
            player.MoveV(remainder.Y);
        }
    }
}
