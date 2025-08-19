using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProjectAssemble.Core;
using ProjectAssemble.Systems;

namespace ProjectAssemble.UI
{
    /// <summary>
    /// UI component for selecting machine types.
    /// </summary>
    public class MachinePaletteUI
    {
        readonly Rectangle _rect;

        /// <summary>
        /// Gets the bounds of the palette.
        /// </summary>
        public Rectangle Rect => _rect;

        /// <summary>
        /// Occurs when a machine type is picked.
        /// </summary>
        public event Action<MachineType> MachinePicked;

        /// <summary>
        /// Initializes a new instance of the <see cref="MachinePaletteUI"/> class.
        /// </summary>
        /// <param name="rect">Display rectangle.</param>
        public MachinePaletteUI(Rectangle rect)
        {
            _rect = rect;
        }

        /// <summary>
        /// Updates the palette state based on input.
        /// </summary>
        /// <param name="input">The input manager.</param>
        public void Update(InputManager input)
        {
            var ms = input.CurrentMouse;
            var pos = new Point(ms.X, ms.Y);
            if (input.JustPressed(ms.LeftButton, input.PreviousMouse.LeftButton) && _rect.Contains(pos))
            {
                MachinePicked?.Invoke(MachineType.Arm);
            }
        }

        /// <summary>
        /// Draws the palette.
        /// </summary>
        public void Draw(SpriteBatch sb, Texture2D tiles, Texture2D px, SpriteFont font)
        {
            FillRect(sb, px, _rect, new Color(30, 32, 38));
            DrawRect(sb, px, _rect, new Color(80, 85, 98), 2);

            var inner = new Rectangle(_rect.X + 8, _rect.Y + 8, _rect.Width - 16, 56);
            FillRect(sb, px, inner, new Color(255, 255, 255, 8));
            DrawRect(sb, px, inner, Color.White, 1);

            if (tiles != null)
            {
                var dest = new Rectangle(inner.X + 12, inner.Y + 12, 32, 32);
                sb.Draw(tiles, dest, SrcCR(tiles, 1, 2), Color.White);
                if (font != null) sb.DrawString(font, "Arm", new Vector2(dest.Right + 8, dest.Y + 8), Color.Black);
            }
            else if (font != null)
            {
                sb.DrawString(font, "Arm", new Vector2(inner.X + 8, inner.Y + 8), Color.Black);
            }
        }

        static Rectangle SrcCR(Texture2D tiles, int col1, int row1)
        {
            const int TILE = 16;
            int tilesPerRow = Math.Max(1, tiles.Width / TILE);
            int idx = (row1 - 1) * tilesPerRow + (col1 - 1);
            int tx = idx % tilesPerRow;
            int ty = idx / tilesPerRow;
            return new Rectangle(tx * TILE, ty * TILE, TILE, TILE);
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
