using Microsoft.Xna.Framework;

namespace ProjectAssemble.Core
{
    /// <summary>
    /// Available machine types.
    /// </summary>
    public enum MachineType { Arm }

    /// <summary>
    /// Supported shape types.
    /// </summary>
    public enum ShapeType { L, Rect2x2 }

    /// <summary>
    /// Actions that an arm can perform in the timeline.
    /// </summary>
    public enum ArmAction { None, Move }

    /// <summary>
    /// Timeline-related constants.
    /// </summary>
    public static class Timeline
    {
        /// <summary>
        /// Total number of available steps in the timeline.
        /// </summary>
        public const int Steps = 21;
    }

    /// <summary>
    /// Cardinal directions used for machine orientation.
    /// </summary>
    public enum Direction { Up, Right, Down, Left }

    /// <summary>
    /// Helper methods for working with <see cref="Direction"/> values.
    /// </summary>
    public static class Dir
    {
        /// <summary>
        /// Converts a direction to its unit <see cref="Point"/> delta.
        /// </summary>
        /// <param name="d">The direction to convert.</param>
        /// <returns>The delta representing the direction.</returns>
        public static Point ToDelta(Direction d) => d switch
        {
            Direction.Up => new Point(0, -1),
            Direction.Right => new Point(1, 0),
            Direction.Down => new Point(0, 1),
            Direction.Left => new Point(-1, 0),
            _ => Point.Zero
        };

        /// <summary>
        /// Gets the rotation angle in radians for the given direction.
        /// </summary>
        /// <param name="d">The direction.</param>
        /// <returns>The angle in radians.</returns>
        public static float Angle(Direction d) => d switch
        {
            Direction.Right => 0f,
            Direction.Down => MathHelper.PiOver2,
            Direction.Left => MathHelper.Pi,
            Direction.Up => -MathHelper.PiOver2,
            _ => 0f
        };
    }
}
