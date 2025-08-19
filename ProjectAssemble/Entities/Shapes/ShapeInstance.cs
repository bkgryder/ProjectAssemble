using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ProjectAssemble.Entities.Shapes
{
    public class ShapeInstance
    {
        public int SourceId;
        public List<Point> Cells; // absolute cells
        public ShapeInstance(int sourceId, List<Point> cells)
        { SourceId = sourceId; Cells = cells; }
    }
}
