using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectAssemble.Core;
using ProjectAssemble.World;
using ProjectAssemble.Entities.Machines;
using ProjectAssemble.Entities.Shapes;

namespace ProjectAssemble.Systems
{
    /// <summary>
    /// Centralized management of machines, shape sources and instances as well as
    /// occupancy and auto-replenishment logic.
    /// </summary>
    public class WorldManager
    {
        public GridWorld World { get; }
        public InputManager Input { get; }
        public List<IMachine> Machines { get; } = new();
        public List<ShapeSource> ShapeSources { get; } = new();
        public List<ShapeInstance> ShapeInstances { get; } = new();

        public WorldManager(int width, int height, InputManager input = null)
        {
            Input = input;
            World = new GridWorld(width, height);
        }

        public void RebuildOccupancy()
        {
            World.BeginOccupancy();
            foreach (var m in Machines) World.MarkOccupied(m.BasePos);
            foreach (var s in ShapeSources) World.MarkOccupied(s.BasePos);
            foreach (var inst in ShapeInstances)
                foreach (var c in inst.Cells) World.MarkOccupied(c);
            World.EndOccupancy();
        }

        public void ReplenishShapes()
        {
            for (int i = 0; i < ShapeSources.Count; i++)
            {
                var src = ShapeSources[i];
                bool has = ShapeInstances.Exists(si => si.SourceId == src.Id);
                if (!has)
                {
                    var cells = GetFootprint(src.Type, src.BasePos, src.Facing);
                    if (AreCellsFree(cells))
                        ShapeInstances.Add(new ShapeInstance(src.Id, cells));
                }
            }
        }

        public bool AreCellsFree(List<Point> cells)
        {
            foreach (var p in cells)
                if (!World.InBounds(p) || World.IsOccupied(p)) return false;
            return true;
        }

        public static List<Point> GetFootprint(ShapeType type, Point basePos, Direction facing)
        {
            var rel = GetShapeCells(type);
            var outCells = new List<Point>(rel.Count);
            foreach (var rp in rel)
            {
                var rot = RotateOffset(rp, facing);
                outCells.Add(new Point(basePos.X + rot.X, basePos.Y + rot.Y));
            }
            return outCells;
        }

        static Point RotateOffset(Point p, Direction facing)
        {
            int k = facing switch { Direction.Right => 0, Direction.Down => 1, Direction.Left => 2, Direction.Up => 3, _ => 0 };
            int x = p.X, y = p.Y;
            for (int i = 0; i < k; i++) { int nx = y; y = -x; x = nx; }
            return new Point(x, y);
        }

        static List<Point> GetShapeCells(ShapeType t)
        {
            switch (t)
            {
                case ShapeType.L:
                    return new List<Point> { new Point(0, 0), new Point(1, 0), new Point(0, 1) };
                case ShapeType.Rect2x2:
                    return new List<Point> { new Point(0, 0), new Point(1, 0), new Point(0, 1), new Point(1, 1) };
                default:
                    return new List<Point> { new Point(0, 0) };
            }
        }
    }
}
