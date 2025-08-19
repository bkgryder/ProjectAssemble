using Microsoft.Xna.Framework;
using ProjectAssemble.Core;

namespace ProjectAssemble.Entities.Shapes
{
    /// <summary>
    /// Defines a source that spawns shapes in the world.
    /// </summary>
    public class ShapeSource
    {
        static int _nextId = 1;

        /// <summary>
        /// Gets the unique identifier of the source.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Base grid position of the source.
        /// </summary>
        public Point BasePos;

        /// <summary>
        /// Type of shape produced.
        /// </summary>
        public ShapeType Type;

        /// <summary>
        /// Facing direction of the source.
        /// </summary>
        public Direction Facing;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShapeSource"/> class.
        /// </summary>
        /// <param name="basePos">Base position of the source.</param>
        /// <param name="type">Type of shape.</param>
        /// <param name="facing">Initial facing direction.</param>
        public ShapeSource(Point basePos, ShapeType type, Direction facing)
        { Id = _nextId++; BasePos = basePos; Type = type; Facing = facing; }
    }
}
