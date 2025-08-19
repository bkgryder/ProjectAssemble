using Microsoft.Xna.Framework;
using ProjectAssemble.Core;

namespace ProjectAssemble.Entities.Shapes
{
    public class ShapeSource
    {
        static int _nextId = 1;
        public int Id { get; private set; }
        public Point BasePos;
        public ShapeType Type;
        public Direction Facing;
        public ShapeSource(Point basePos, ShapeType type, Direction facing)
        { Id = _nextId++; BasePos = basePos; Type = type; Facing = facing; }
    }
}
