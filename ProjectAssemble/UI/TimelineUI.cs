using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProjectAssemble.Entities.Machines;
using ProjectAssemble.Systems;

namespace ProjectAssemble.UI
{
    public class TimelineUI
    {
        const int TIMESTEPS = 21;
        Rectangle _rect;
        int _hoveredStep = -1;
        int _hoveredRow = -1;
        bool _dragging = false;
        int _currentStep = 0;

        public Rectangle Rect => _rect;
        public int CurrentStep => _currentStep;
        public int StepCount => TIMESTEPS;
        public bool IsDragging => _dragging;

        public event Action<int> StepChanged;

        public void Update(InputManager input, Rectangle gridRect, List<ArmMachine> arms)
        {
            var ms = input.CurrentMouse;
            var mouse = new Point(ms.X, ms.Y);
            int lanes = Math.Max(1, arms.Count);
            int laneH = 22; int pad = 8;
            int innerHeight = lanes * laneH;
            _rect = new Rectangle(gridRect.X, gridRect.Bottom + 12, gridRect.Width, pad * 2 + innerHeight + 18);

            _hoveredStep = StepAt(mouse, arms);
            _hoveredRow = RowAt(mouse, arms);

            if (!_dragging && input.JustPressed(ms.LeftButton, input.PreviousMouse.LeftButton) && _rect.Contains(mouse))
            {
                _dragging = true;
                if (_hoveredStep >= 0) SetStep(_hoveredStep);
            }
            if (_dragging && ms.LeftButton == ButtonState.Pressed)
            {
                if (_hoveredStep >= 0) SetStep(_hoveredStep);
            }
            if (_dragging && input.JustReleased(ms.LeftButton, input.PreviousMouse.LeftButton))
            {
                _dragging = false;
            }
        }

        void SetStep(int step)
        {
            if (_currentStep != step)
            {
                _currentStep = step;
                StepChanged?.Invoke(_currentStep);
            }
        }

        int StepAt(Point mouse, List<ArmMachine> arms)
        {
            if (!_rect.Contains(mouse)) return -1;
            int pad = 8; int gap = 4; int laneH = 22; int labelColW = 48;
            int lanes = Math.Max(1, arms.Count);
            var inner = new Rectangle(_rect.X + pad, _rect.Y + pad, _rect.Width - pad * 2, lanes * laneH);
            int slotsW = Math.Max(40, inner.Width - labelColW);
            int slotW = Math.Max(14, (slotsW - gap * (TIMESTEPS - 1)) / TIMESTEPS);
            int totalW = TIMESTEPS * slotW + (TIMESTEPS - 1) * gap;
            int relX = mouse.X - (inner.X + labelColW);
            if (relX < 0 || relX >= totalW) return -1;
            int step = relX / (slotW + gap);
            int insideX = relX % (slotW + gap);
            if (insideX >= slotW) return -1;
            return Math.Clamp(step, 0, TIMESTEPS - 1);
        }

        int RowAt(Point mouse, List<ArmMachine> arms)
        {
            if (!_rect.Contains(mouse)) return -1;
            int pad = 8; int laneH = 22;
            int lanes = Math.Max(1, arms.Count);
            var inner = new Rectangle(_rect.X + pad, _rect.Y + pad, _rect.Width - pad * 2, lanes * laneH);
            if (mouse.Y < inner.Y || mouse.Y >= inner.Bottom) return -1;
            int relY = mouse.Y - inner.Y;
            int row = relY / laneH;
            return Math.Clamp(row, 0, lanes - 1);
        }

        public void Draw(SpriteBatch sb, Texture2D px, SpriteFont font, List<ArmMachine> arms)
        {
            FillRect(sb, px, _rect, new Color(30, 32, 38));
            DrawRect(sb, px, _rect, new Color(80, 85, 98), 2);

            int lanes = Math.Max(1, arms.Count);
            int pad = 8; int gap = 4; int laneH = 22; int labelColW = 48;
            var inner = new Rectangle(_rect.X + pad, _rect.Y + pad, _rect.Width - pad * 2, lanes * laneH);
            int slotsW = Math.Max(40, inner.Width - labelColW);
            int slotW = Math.Max(14, (slotsW - gap * (TIMESTEPS - 1)) / TIMESTEPS);
            int slotH = laneH - 2;
            int slotsX = inner.X + labelColW;

            for (int row = 0; row < lanes; row++)
            {
                int laneY = inner.Y + row * laneH;
                var laneRect = new Rectangle(inner.X, laneY, inner.Width, laneH);
                var stripe = (row % 2 == 0) ? new Color(255, 255, 255, 12) : new Color(255, 255, 255, 6);
                FillRect(sb, px, laneRect, stripe);

                var labelRect = new Rectangle(inner.X, laneY, labelColW - 6, laneH);
                DrawRect(sb, px, labelRect, new Color(60, 65, 78), 1);
                if (font != null && row < arms.Count)
                {
                    string labelText = "Arm " + arms[row].Label;
                    var size = font.MeasureString(labelText);
                    sb.DrawString(font, labelText, new Vector2(labelRect.X + 6, labelRect.Y + (laneH - size.Y) / 2f), Color.White);
                }

                for (int i = 0; i < TIMESTEPS; i++)
                {
                    int x = slotsX + i * (slotW + gap);
                    var r = new Rectangle(x, laneY + 1, slotW, slotH);
                    bool isCurrent = (i == _currentStep);
                    bool isHoverStep = (i == _hoveredStep);
                    bool isHoverRow = (row == _hoveredRow);
                    var fill = new Color(255, 255, 255, 10);
                    if (isCurrent) fill = new Color(120, 200, 255, 90);
                    else if (isHoverStep && isHoverRow) fill = new Color(200, 220, 255, 40);
                    FillRect(sb, px, r, fill);
                    DrawRect(sb, px, r, isCurrent ? new Color(120, 200, 255) : new Color(160, 170, 190), 1);
                }
            }

            if (font != null)
            {
                for (int i = 0; i < TIMESTEPS; i += 5)
                {
                    int x = slotsX + i * (slotW + gap);
                    sb.DrawString(font, i.ToString(), new Vector2(x + 2, inner.Bottom + 2), new Color(200, 210, 230));
                }
                sb.DrawString(font, $"Step: {_currentStep} / {TIMESTEPS - 1}", new Vector2(_rect.X + 6, _rect.Y - 18), Color.White);
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
