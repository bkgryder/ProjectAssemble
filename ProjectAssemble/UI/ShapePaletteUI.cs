using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectAssemble.Core;
using ProjectAssemble.Systems;

namespace ProjectAssemble.UI
{
    /// <summary>
    /// UI component for selecting shape types.
    /// </summary>
    public class ShapePaletteUI
    {
        readonly Rectangle _rect;

        /// <summary>
        /// Gets the bounds of the palette.
        /// </summary>
        public Rectangle Rect => _rect;

        /// <summary>
        /// Occurs when a shape type is picked.
        /// </summary>
        public event Action<ShapeType> ShapePicked;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShapePaletteUI"/> class.
        /// </summary>
        /// <param name="rect">Display rectangle.</param>
        public ShapePaletteUI(Rectangle rect)
        {
            _rect = rect;
        }

        /// <summary>
        /// Updates the palette state based on input.
        /// </summary>
        public void Update(InputManager input)
        {
            var ms = input.CurrentMouse;
            var pos = new Point(ms.X, ms.Y);
            if (input.JustPressed(ms.LeftButton, input.PreviousMouse.LeftButton))
            {
                var r1 = new Rectangle(_rect.X + 8, _rect.Y + 32, _rect.Width - 16, 44);
                var r2 = new Rectangle(_rect.X + 8, _rect.Y + 84, _rect.Width - 16, 44);
                if (r1.Contains(pos)) ShapePicked?.Invoke(ShapeType.L);
                else if (r2.Contains(pos)) ShapePicked?.Invoke(ShapeType.Rect2x2);
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
                sb.DrawString(font, "Shapes", new Vector2(_rect.X + 8, _rect.Y + 8), Color.White);

            var cellY = _rect.Y + 32;
            DrawEntry(sb, px, font, new Rectangle(_rect.X + 8, cellY, _rect.Width - 16, 44), ShapeType.L);
            cellY += 52;
            DrawEntry(sb, px, font, new Rectangle(_rect.X + 8, cellY, _rect.Width - 16, 44), ShapeType.Rect2x2);
        }

        void DrawEntry(SpriteBatch sb, Texture2D px, SpriteFont font, Rectangle r, ShapeType t)
        {
            FillRect(sb, px, r, new Color(255, 255, 255, 8));
            DrawRect(sb, px, r, Color.White, 1);
            var center = new Point(r.X + r.Width / 2, r.Y + r.Height / 2);
            foreach (var p in GetShapeCells(t))
            {
                var pr = new Rectangle(center.X - 16 + p.X * 8, center.Y - 8 + p.Y * 8, 8, 8);
                FillRect(sb, px, pr, new Color(160, 255, 180, 180));
                DrawRect(sb, px, pr, new Color(90, 200, 120), 1);
            }
            if (font != null) sb.DrawString(font, t.ToString(), new Vector2(r.X + 8, r.Bottom - 18), Color.White);
        }

        static IEnumerable<Point> GetShapeCells(ShapeType t)
        {
            switch (t)
            {
                case ShapeType.L:
                    return new[] { new Point(0, 0), new Point(1, 0), new Point(0, 1) };
                case ShapeType.Rect2x2:
                    return new[] { new Point(0, 0), new Point(1, 0), new Point(0, 1), new Point(1, 1) };
                default:
                    return new[] { new Point(0, 0) };
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
