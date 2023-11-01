using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.viddiesToolbox.Entities {
    public class LineupIndicatorEntity : Entity {

        private static ViddiesToolboxModule Mod => ViddiesToolboxModule.Instance;
        
        private int IndicatorWidth = 300;
        private int MaxHeightFrames = 2;
        private string SelectedTech = "Full Jump";
        private WorldTextEntity Label;

        private Dictionary<string, Tuple<float, bool>> TechList = new Dictionary<string, Tuple<float, bool>>() {
            ["Full Jump"] = Tuple.Create(-26.75f, false),
            ["Up Dash Buffer"] = Tuple.Create(-42f, true),
            ["Up-Diagonal Dash Buffer"] = Tuple.Create(-29.6985f, true),
            ["Down Dash Buffer"] = Tuple.Create(44f, true),
            ["Down-Diagonal Dash Buffer"] = Tuple.Create(31.1128f, true),
            ["Horizontal Dash Buffer"] = Tuple.Create(0f, true),
            ["Max Height Hyper"] = Tuple.Create(-12.375f, false),
        };

        public LineupIndicatorEntity() : base() {
            Depth = Depths.Top;
            Tag = Tags.TransitionUpdate;

            Label = new WorldTextEntity(Position);
        }

        public override void Added(Scene scene) {
            scene.Add(Label);
            base.Added(scene);
        }

        public override void Update() {
            Player player = Scene.Tracker.GetEntity<Player>();
            if (player == null) {
                Visible = false;
                Label.Visible = Visible;
                return;
            }

            if (Mod.ModSettings.ButtonDemoLineupNextTech.Pressed) {
                if (SelectedTech == null) {
                    Mod.ModSettings.DemoLineupSelectedTech = TechList.Keys.First();
                } else {
                    int index = TechList.Keys.ToList().IndexOf(SelectedTech);
                    index++;
                    if (index >= TechList.Keys.Count) {
                        index = 0;
                    }
                    Mod.ModSettings.DemoLineupSelectedTech = TechList.Keys.ToList()[index];
                }
            }
            
            SelectedTech = Mod.ModSettings.DemoLineupSelectedTech ?? TechList.Keys.First();
            if (!TechList.Keys.Contains(SelectedTech)) {
                SelectedTech = TechList.Keys.First();
            }

            Tuple<float, bool> details = TechList[SelectedTech];
            float distance = details.Item1;
            bool isBuffered = details.Item2;

            Position = player.Position + new Vector2(0f, -3 + distance + player.PositionRemainder.Y);
            

            if (isBuffered) {
                MaxHeightFrames = 5;
            } else {
                float subpixel = distance + player.PositionRemainder.Y;
                subpixel -= (float)Math.Floor(subpixel);
                subpixel++;
                subpixel -= (float)Math.Floor(subpixel);
                
                if (subpixel <= 0.25) {
                    MaxHeightFrames = 4;
                } else if (subpixel <= 0.5) {
                    MaxHeightFrames = 2;
                } else if (subpixel <= 0.75) {
                    MaxHeightFrames = 8;
                } else {
                    MaxHeightFrames = 6;
                }
            }

            Label.Position = Position + new Vector2(0f, -5f);
            Label.Position.Y = (float)Math.Round(Label.Position.Y);
            Label.Text = $"{SelectedTech} - {MaxHeightFrames}f demo";
            Label.Outline = true;
            Label.Scale = 0.75f;
            Label.Update();
            
            Visible = Mod.ModSettings.DemoLineupEnabled;
            Label.Visible = Visible;
        }

        public override void Render() {
            Draw.HollowRect(Position.X - (IndicatorWidth / 2), (float)Math.Round(Position.Y), IndicatorWidth, 1f, Color.Green);

            Label.Render();
        }
    }
}
