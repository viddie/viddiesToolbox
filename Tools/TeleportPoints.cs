using Celeste.Mod.viddiesToolbox.Entities;
using FMOD;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.viddiesToolbox.Tools {
    public class TeleportPoints {

        public class PositionData {
            public Vector2 Position { get; set; } = Vector2.Zero;
            public Vector2 Remainder { get; set; } = Vector2.Zero;
            public Facings Facing { get; set; } = Facings.Left;
            public string LevelName { get; set; } = null;
        }

        private static ViddiesToolboxModule Mod => ViddiesToolboxModule.Instance;
        private static ModuleSettings Settings => Mod.ModSettings;

        private Vector2 _ApplyRemainder = Vector2.Zero;
        private int _ApplyRemainderIndex = -1;
        private Facings _ApplyFacing = Facings.Left;
        private bool _SetRespawn = false;
        
        
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

            if (_ApplyRemainderIndex != -1) {
                Tooltip.Show($"Teleported to point {_ApplyRemainderIndex + 1}", 1.3f);

                if (!_SetRespawn) {
                    self.Session.RespawnPoint = null; //Set respawn to closest respawn point in the room
                }
                _ApplyRemainderIndex = -1;
            }

            for (int i = 0; i < Settings.ButtonsTeleportPoint.Count; i++) {
                if (!Settings.ButtonsTeleportPoint[i].Pressed) continue;
                if (Settings.TeleportPoints.Count <= i) continue;

                bool holdingModifier = Settings.TeleportPointClearModifier.Check;
                bool pointIsEmpty = Settings.TeleportPoints[i] == null;
                bool setRespawn = Settings.TeleportPointSetRespawnModifier.Check;

                if (holdingModifier) {
                    ClearTeleportPoint(i);
                    continue;
                }

                Player player = self.Tracker.GetEntity<Player>();
                if (player == null || player.Dead) continue;

                if (pointIsEmpty) {
                    SetTeleportPoint(player, self.Session, i);
                } else {
                    UseTeleportPoint(player, self.Session, i, setRespawn);
                }
            }
        }

        private void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            orig(self, playerIntro, isFromLoader);

            if (_ApplyRemainder != Vector2.Zero) {
                Player player = self.Tracker.GetEntity<Player>();
                ApplyRemainder(player, _ApplyRemainder);
                if (player != null) player.Facing = _ApplyFacing;
                _ApplyRemainder = Vector2.Zero;
            }
        }

        public void SetTeleportPoint(Player player, Session session, int index) {
            PositionData data = new PositionData() {
                Position = player.Position,
                Remainder = player.PositionRemainder,
                Facing = player.Facing,
                LevelName = session.Level
            };
            
            Settings.TeleportPoints[index] = data;
            Mod.SaveSettings();
            Mod.Log($"Set teleport point {index} to position: {data.Position} remainder {data.Remainder}", LogLevel.Info);
            Tooltip.Show($"Saved teleport ({index + 1})");
        }
        public void ClearTeleportPoint(int index) {
            Settings.TeleportPoints[index] = null;
            Mod.SaveSettings();
            Mod.Log($"Cleared teleport point {index}", LogLevel.Info);
            Tooltip.Show($"Cleared teleport ({index + 1})");
        }
        public void UseTeleportPoint(Player player, Session session, int index, bool setRespawn) {
            PositionData data = Settings.TeleportPoints[index];
            string currentLevel = session.Level;

            Vector2 position = data.Position;
            Vector2 remainder = data.Remainder;
            Facings facing = data.Facing;
            string levelName = data.LevelName;
            
            bool isSameLevel = levelName == currentLevel;

            if (session.MapData.Levels.FirstOrDefault((LevelData lvl) => lvl.Name == levelName) == null) {
                Mod.Log($"Teleport Point {index}: Level '{levelName}' wasn't found in the current chapter", LogLevel.Info);
                Tooltip.Show($"Level '{levelName}' wasn't found");
                return;
            }

            //Set respawn point either if the player wants to OR if its a different screen that has to be loaded first
            if (setRespawn || !isSameLevel) { 
                session.RespawnPoint = position;
            }
            
            player.Position = position;
            player.Facing = facing;

            if (!isSameLevel) {
                _ApplyRemainder = remainder;
                _ApplyRemainderIndex = index;
                _ApplyFacing = facing;
                _SetRespawn = setRespawn;

                session.Level = levelName;
                Engine.Scene.Paused = true;
                Engine.Scene = new LevelLoader(session, position);
            } else {
                ApplyRemainder(player, remainder);
            }

            Tooltip.Show($"Teleported to point ({index + 1})");
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
