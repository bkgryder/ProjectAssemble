using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProjectAssemble.Entities.Machines;
using ProjectAssemble.Systems;

namespace ProjectAssemble.UI
{
    /// <summary>
    /// Simple parameter panel for editing properties on an <see cref="ArmMachine"/>.
    /// </summary>
    public class ArmParameterUI
    {
        ArmMachine _target;
        Rectangle _rect;
        bool _visible;

        Rectangle LabelRect => new Rectangle(_rect.X + 8, _rect.Y + 8, _rect.Width - 16, 20);
        Rectangle MoveRect => new Rectangle(_rect.X + 8, _rect.Y + 36, _rect.Width - 16, 20);
        Rectangle GrabRect => new Rectangle(_rect.X + 8, _rect.Y + 64, _rect.Width - 16, 20);

        /// <summary>
        /// Gets a value indicating whether the panel is currently visible.
        /// </summary>
        public bool Visible => _visible;

        /// <summary>
        /// Shows the panel for the specified arm near the supplied rectangle.
        /// </summary>
        public void Show(ArmMachine arm, Rectangle anchor)
        {
            _target = arm;
            int w = 160; int h = 92;
            _rect = new Rectangle(anchor.Right + 8, anchor.Y, w, h);
            _visible = true;
        }

        /// <summary>
        /// Hides the panel.
        /// </summary>
        public void Hide()
        {
            _visible = false;
            _target = null;
        }

        /// <summary>
        /// Updates the panel based on input.
        /// </summary>
        public void Update(InputManager input)
        {
            if (!_visible || _target == null) return;
            var ms = input.CurrentMouse;
            var pos = new Point(ms.X, ms.Y);

            if (input.JustPressed(ms.LeftButton, input.PreviousMouse.LeftButton))
            {
                if (!_rect.Contains(pos))
                {
                    Hide();
                    return;
                }

                if (LabelRect.Contains(pos))
                {
                    char c = _target.Label;
                    if (c < 'Z') c++; else c = 'A';
                    _target.Label = c;
                }
                else if (MoveRect.Contains(pos))
                {
                    int mid = MoveRect.X + MoveRect.Width / 2;
                    if (pos.X < mid) _target.MoveAmount = System.Math.Max(0, _target.MoveAmount - 1);
                    else _target.MoveAmount++;
                }
                else if (GrabRect.Contains(pos))
                {
                    _target.Grabbed = !_target.Grabbed;
                }
            }
        }

        /// <summary>
        /// Draws the panel.
        /// </summary>
        public void Draw(SpriteBatch sb, Texture2D px, SpriteFont font)
        {
            if (!_visible || _target == null) return;

            FillRect(sb, px, _rect, new Color(30, 32, 38));
            DrawRect(sb, px, _rect, new Color(80, 85, 98), 2);

            if (font != null)
            {
                FillRect(sb, px, LabelRect, new Color(255, 255, 255, 8));
                DrawRect(sb, px, LabelRect, Color.Black, 1);
                sb.DrawString(font, $"Label: {_target.Label}", new Vector2(LabelRect.X + 4, LabelRect.Y + 2), Color.White);

                FillRect(sb, px, MoveRect, new Color(255, 255, 255, 8));
                DrawRect(sb, px, MoveRect, Color.Black, 1);
                sb.DrawString(font, $"Move: {_target.MoveAmount}", new Vector2(MoveRect.X + 4, MoveRect.Y + 2), Color.White);

                FillRect(sb, px, GrabRect, new Color(255, 255, 255, 8));
                DrawRect(sb, px, GrabRect, Color.Black, 1);
                sb.DrawString(font, $"Grabbed: {_target.Grabbed}", new Vector2(GrabRect.X + 4, GrabRect.Y + 2), Color.White);
            }
        }

        static void FillRect(SpriteBatch sb, Texture2D px, Rectangle r, Color c) => sb.Draw(px, r, c);
        static void DrawRect(SpriteBatch sb, Texture2D px, Rectangle r, Color c, int t = 1)
        {
            sb.Draw(px, new Rectangle(r.X, r.Y, r.Width, t), c);
            sb.Draw(px, new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
            sb.Draw(px, new Rectangle(r.X, r.Y, t, r.Height), c);
            sb.Draw(px, new Rectangle(r.Right - t, r.Y, t, r.Height), c);
        }
    }
}

