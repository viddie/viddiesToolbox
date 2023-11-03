using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.viddiesToolbox.Analog {
    public class VirtualIntegerJointAxis : VirtualIntegerAxis {

        public enum AxisType {
            X,
            Y
        }

        public VirtualIntegerJointAxis Other { get; set; }
        public AxisType Type { get; private set; }


        public VirtualIntegerJointAxis(AxisType type, Binding negative, Binding negativeAlt, Binding positive, Binding positiveAlt, int gamepadIndex, float threshold, OverlapBehaviors overlapBehavior = OverlapBehaviors.TakeNewer) : base(negative, negativeAlt, positive, positiveAlt, gamepadIndex, threshold, overlapBehavior) {
            Type = type;
        }

        public override void Update() {
            foreach (VirtualAxis.Node node in Nodes) {
                node.Update();
            }

            orig_Update();
            if (MInput.Disabled) {
                return;
            }

            foreach (VirtualAxis.Node node2 in Nodes) {
                float value = node2.Value;
                if (value != 0f) {
                    Value = Math.Sign(value);
                    if (Inverted) {
                        Value *= -1;
                    }

                    break;
                }
            }
        }

        public new void orig_Update() {
            PreviousValue = Value;
            if (MInput.Disabled) {
                return;
            }

            //Use the biggest value of Positive/PositiveAlt and Negative/NegativeAlt, except negative is inverted
            float thisValue = GetAxisValue();
            float otherValue = Other.GetAxisValue();

            float x, y;
            if (Type == AxisType.X) {
                x = thisValue;
                y = otherValue;
            } else {
                x = otherValue;
                y = thisValue;
            }

            float num = new Vector2(x, y).Angle();
            int num2 = ((num < 0f) ? 1 : 0);
            float num3 = (float)Math.PI / 8f - (float)num2 * 0.08726646f;
            Vector2 result;
            if (x != 0 || y != 0) {
                if (Calc.AbsAngleDiff(num, 0f) < num3) {
                    result = new Vector2(1f, 0f);
                } else if (Calc.AbsAngleDiff(num, (float)Math.PI) < num3) {
                    result = new Vector2(-1f, 0f);
                } else if (Calc.AbsAngleDiff(num, -(float)Math.PI / 2f) < num3) {
                    result = new Vector2(0f, -1f);
                } else if (Calc.AbsAngleDiff(num, (float)Math.PI / 2f) < num3) {
                    result = new Vector2(0f, 1f);
                } else {
                    result = new Vector2(Math.Sign(x), Math.Sign(y));
                }
            } else {
                result = Vector2.Zero;
            }

            if (Type == AxisType.X) {
                Value = (int)result.X;
            } else {
                Value = (int)result.Y;
            }

            if (Inverted) {
                Value = -Value;
            }
        }

        public float GetAxisValue() {
            float positive = Math.Max(Positive.Axis(GamepadIndex, Threshold), PositiveAlt != null ? PositiveAlt.Axis(GamepadIndex, Threshold) : 0);
            float negative = Math.Max(Negative.Axis(GamepadIndex, Threshold), NegativeAlt != null ? NegativeAlt.Axis(GamepadIndex, Threshold) : 0);
            float value = Math.Min(1, Math.Max(positive, negative));
            if (negative > positive) {
                value = -value;
            }

            return value;
        }
    }
}
