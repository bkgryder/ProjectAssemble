using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ProjectAssemble.Entities.Shapes
{
    /// <summary>
    /// Represents a placed shape in the world.
    /// </summary>
    public class ShapeInstance
    {
        /// <summary>
        /// Identifier of the source that produced this instance.
        /// </summary>
        public int SourceId;

        /// <summary>
        /// Absolute grid cells occupied by the shape.
        /// </summary>
        public List<Point> Cells; // absolute cells

        /// <summary>
        /// Initializes a new instance of the <see cref="ShapeInstance"/> class.
        /// </summary>
        /// <param name="sourceId">Source identifier.</param>
        /// <param name="cells">Cells occupied by the instance.</param>
        public ShapeInstance(int sourceId, List<Point> cells)
        { SourceId = sourceId; Cells = cells; }
    }
}
