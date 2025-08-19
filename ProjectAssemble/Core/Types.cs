using Microsoft.Xna.Framework;

namespace ProjectAssemble.Core
{
    public enum MachineType { Arm }
    public enum ShapeType { L, Rect2x2 }
    public enum Direction { Up, Right, Down, Left }

    public static class Dir
    {
        public static Point ToDelta(Direction d) => d switch
        {
            Direction.Up => new Point(0, -1),
            Direction.Right => new Point(1, 0),
            Direction.Down => new Point(0, 1),
            Direction.Left => new Point(-1, 0),
            _ => Point.Zero
        };

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
