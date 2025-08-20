using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
        /// Occurs when an action is picked along with an optional amount.
        /// </summary>
        public event Action<ArmAction, int> ActionPicked;

        bool _capturingMoveAmount = false;
        int _moveAmount = 0;

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
            if (_capturingMoveAmount)
            {
                var kb = input.CurrentKeyboard;
                var prev = input.PreviousKeyboard;
                for (int i = 0; i <= 9; i++)
                {
                    var key = Keys.D0 + i;
                    var numPad = Keys.NumPad0 + i;
                    if (kb.IsKeyDown(key) && !prev.IsKeyDown(key)) _moveAmount = _moveAmount * 10 + i;
                    if (kb.IsKeyDown(numPad) && !prev.IsKeyDown(numPad)) _moveAmount = _moveAmount * 10 + i;
                }
                if (kb.IsKeyDown(Keys.Back) && !prev.IsKeyDown(Keys.Back))
                    _moveAmount /= 10;
                if (kb.IsKeyDown(Keys.Enter) && !prev.IsKeyDown(Keys.Enter))
                {
                    _capturingMoveAmount = false;
                    ActionPicked?.Invoke(ArmAction.Move, _moveAmount);
                    _moveAmount = 0;
                }
                if (kb.IsKeyDown(Keys.Escape) && !prev.IsKeyDown(Keys.Escape))
                {
                    _capturingMoveAmount = false;
                    _moveAmount = 0;
                }
                return;
            }

            var ms = input.CurrentMouse;
            var pos = new Point(ms.X, ms.Y);
            if (input.JustPressed(ms.LeftButton, input.PreviousMouse.LeftButton))
            {
                var moveRect = new Rectangle(_rect.X + 8, _rect.Y + 32, _rect.Width - 16, 20);
                if (moveRect.Contains(pos))
                {
                    _capturingMoveAmount = true;
                    _moveAmount = 0;
                }
            }
        }

        /// <summary>
        /// Draws the palette.
        /// </summary>
        /// <param name="actionPending">If true, shows help text for placing an action.</param>
        public void Draw(SpriteBatch sb, Texture2D px, SpriteFont font, bool actionPending)
        {
            FillRect(sb, px, _rect, new Color(30, 32, 38));
            DrawRect(sb, px, _rect, new Color(80, 85, 98), 2);

            if (font != null)
                sb.DrawString(font, "Actions", new Vector2(_rect.X + 8, _rect.Y + 8), Color.White);

            var r = new Rectangle(_rect.X + 8, _rect.Y + 32, _rect.Width - 16, 20);
            FillRect(sb, px, r, new Color(255, 255, 255, 8));
            DrawRect(sb, px, r, Color.White, 1);
            if (font != null)
            {
                string label = _capturingMoveAmount ? $"Move: {_moveAmount}" : "Move";
                sb.DrawString(font, label, new Vector2(r.X + 4, r.Y + 2), Color.Black);
                if (actionPending && !_capturingMoveAmount)
                {
                    const string help = "Select an action, then click a timeline slot";
                    sb.DrawString(font, help, new Vector2(_rect.X + 8, _rect.Bottom - 20), new Color(200, 200, 200));
                }
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
