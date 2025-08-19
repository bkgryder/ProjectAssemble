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
        /// <summary>
        /// Occurs when a drag is initiated.
        /// </summary>
        public event Action<Point> DragStarted;

        /// <summary>
        /// Occurs when a rotate command is issued.
        /// </summary>
        public event Action Rotate;

        /// <summary>
        /// Occurs when a select command is issued.
        /// </summary>
        public event Action Select;

        /// <summary>
        /// Gets the current mouse state.
        /// </summary>
        public MouseState CurrentMouse { get; private set; }

        /// <summary>
        /// Gets the previous mouse state.
        /// </summary>
        public MouseState PreviousMouse { get; private set; }

        /// <summary>
        /// Gets the current keyboard state.
        /// </summary>
        public KeyboardState CurrentKeyboard { get; private set; }

        /// <summary>
        /// Gets the previous keyboard state.
        /// </summary>
        public KeyboardState PreviousKeyboard { get; private set; }

        /// <summary>
        /// Gets the current mouse position as a point.
        /// </summary>
        public Point MousePosition => new Point(CurrentMouse.X, CurrentMouse.Y);

        /// <summary>
        /// Polls input devices and fires interaction events.
        /// </summary>
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

        /// <summary>
        /// Determines whether the button transitioned from released to pressed.
        /// </summary>
        public bool JustPressed(ButtonState cur, ButtonState prev) => cur == ButtonState.Pressed && prev == ButtonState.Released;

        /// <summary>
        /// Determines whether the button transitioned from pressed to released.
        /// </summary>
        public bool JustReleased(ButtonState cur, ButtonState prev) => cur == ButtonState.Released && prev == ButtonState.Pressed;

        /// <summary>
        /// Determines whether the specified key was just pressed.
        /// </summary>
        public bool JustPressedKey(Keys key) => CurrentKeyboard.IsKeyDown(key) && !PreviousKeyboard.IsKeyDown(key);
    }
}
