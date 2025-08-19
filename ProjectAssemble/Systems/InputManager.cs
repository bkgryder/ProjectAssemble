using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ProjectAssemble.Systems
{
    /// <summary>
    /// Handles polling of mouse and keyboard state and exposes simple events
    /// for high level interactions like drag start, rotation and selection.
    /// </summary>
    public class InputManager
    {
        public event Action<Point> DragStarted;
        public event Action Rotate;
        public event Action Select;

        public MouseState CurrentMouse { get; private set; }
        public MouseState PreviousMouse { get; private set; }
        public KeyboardState CurrentKeyboard { get; private set; }
        public KeyboardState PreviousKeyboard { get; private set; }

        public Point MousePosition => new Point(CurrentMouse.X, CurrentMouse.Y);

        public void Update()
        {
            PreviousMouse = CurrentMouse;
            PreviousKeyboard = CurrentKeyboard;

            CurrentMouse = Mouse.GetState();
            CurrentKeyboard = Keyboard.GetState();

            if (JustPressed(CurrentMouse.LeftButton, PreviousMouse.LeftButton))
                DragStarted?.Invoke(MousePosition);
            if (JustPressedKey(Keys.Q) || JustPressedKey(Keys.E))
                Rotate?.Invoke();
            if (JustPressedKey(Keys.Enter))
                Select?.Invoke();
        }

        public bool JustPressed(ButtonState cur, ButtonState prev) => cur == ButtonState.Pressed && prev == ButtonState.Released;
        public bool JustReleased(ButtonState cur, ButtonState prev) => cur == ButtonState.Released && prev == ButtonState.Pressed;
        public bool JustPressedKey(Keys key) => CurrentKeyboard.IsKeyDown(key) && !PreviousKeyboard.IsKeyDown(key);
    }
}
