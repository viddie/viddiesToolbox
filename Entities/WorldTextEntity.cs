using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.viddiesToolbox.Entities {
    public class WorldTextEntity : Entity {

        private float alpha = 1.0f;
        public float Scale = 1.0f;
        public string Text = "";
        public bool Outline = false;
        public bool DebugPosition = false;

        public WorldTextEntity(Vector2 position) : base(position) {
            base.Tag = Tags.HUD;
        }
        
        public override void Render() {
            Camera cam = ((Level)base.Scene).Camera;
            
            Vector2 camToScreen = cam.CameraToScreen(Position); //320x180
            Vector2 position2 = camToScreen * 6;
            if (SaveData.Instance != null && SaveData.Instance.Assists.MirrorMode) {
                position2.X = 1920f - position2.X;
            }
            
            if (DebugPosition) {
                Draw.Circle(position2, 1, Color.Red, 10);
                Draw.Circle(position2, 3, Color.Red, 10);
            }

            if (Outline) {
                ActiveFont.DrawOutline(Text, position2, new Vector2(0.5f, 0.5f), Vector2.One * Scale, Color.White * alpha, 2f, Color.Black * alpha);
            } else {
                ActiveFont.Draw(Text, position2, new Vector2(0.5f, 0.5f), Vector2.One * Scale, Color.White * alpha);
            }
        }
    }
}
