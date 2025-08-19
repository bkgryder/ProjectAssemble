using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectAssemble.Core;
using ProjectAssemble.Systems;

namespace ProjectAssemble.UI
{
    /// <summary>
    /// UI component for selecting arm actions.
    /// </summary>
    public class ActionPaletteUI
    {
        readonly Rectangle _rect;

        /// <summary>
        /// Gets the bounds of the palette.
        /// </summary>
        public Rectangle Rect => _rect;

        /// <summary>
        /// Occurs when an action is picked.
        /// </summary>
        public event Action<ArmAction> ActionPicked;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionPaletteUI"/> class.
        /// </summary>
        public ActionPaletteUI(Rectangle rect)
        {
            _rect = rect;
        }

        /// <summary>
        /// Updates the palette based on input.
        /// </summary>
        public void Update(InputManager input)
        {
            var ms = input.CurrentMouse;
            var pos = new Point(ms.X, ms.Y);
            if (input.JustPressed(ms.LeftButton, input.PreviousMouse.LeftButton))
            {
                var moveRect = new Rectangle(_rect.X + 8, _rect.Y + 32, _rect.Width - 16, 20);
                if (moveRect.Contains(pos))
                    ActionPicked?.Invoke(ArmAction.Move);
            }
        }

        /// <summary>
        /// Draws the palette.
        /// </summary>
        public void Draw(SpriteBatch sb, Texture2D px, SpriteFont font)
        {
            FillRect(sb, px, _rect, new Color(30, 32, 38));
            DrawRect(sb, px, _rect, new Color(80, 85, 98), 2);

            if (font != null)
                sb.DrawString(font, "Actions", new Vector2(_rect.X + 8, _rect.Y + 8), Color.White);

            var r = new Rectangle(_rect.X + 8, _rect.Y + 32, _rect.Width - 16, 20);
            FillRect(sb, px, r, new Color(255, 255, 255, 8));
            DrawRect(sb, px, r, Color.White, 1);
            if (font != null)
                sb.DrawString(font, "Move", new Vector2(r.X + 4, r.Y + 2), Color.Black);
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
