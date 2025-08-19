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
        /// <summary>
        /// Gets the grid world managed by this instance.
        /// </summary>
        public GridWorld World { get; }

        /// <summary>
        /// Gets the input manager associated with the world.
        /// </summary>
        public InputManager Input { get; }

        /// <summary>
        /// Gets the collection of machines in the world.
        /// </summary>
        public List<IMachine> Machines { get; } = new();

        /// <summary>
        /// Gets the collection of shape sources in the world.
        /// </summary>
        public List<ShapeSource> ShapeSources { get; } = new();

        /// <summary>
        /// Gets the collection of active shape instances.
        /// </summary>
        public List<ShapeInstance> ShapeInstances { get; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="WorldManager"/> class.
        /// </summary>
        /// <param name="width">World width in cells.</param>
        /// <param name="height">World height in cells.</param>
        /// <param name="input">Optional input manager.</param>
        public WorldManager(int width, int height, InputManager input = null)
        {
            Input = input;
            World = new GridWorld(width, height);
        }

        /// <summary>
        /// Rebuilds the occupancy map based on machines and shapes.
        /// </summary>
        public void RebuildOccupancy()
        {
            World.BeginOccupancy();
            foreach (var m in Machines) World.MarkOccupied(m.BasePos);
            foreach (var s in ShapeSources) World.MarkOccupied(s.BasePos);
            foreach (var inst in ShapeInstances)
                foreach (var c in inst.Cells) World.MarkOccupied(c);
            World.EndOccupancy();
        }

        /// <summary>
        /// Ensures each shape source has a corresponding shape instance if space is available.
        /// </summary>
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

        /// <summary>
        /// Determines whether all specified cells are within bounds and unoccupied.
        /// </summary>
        /// <param name="cells">Cells to check.</param>
        /// <returns><c>true</c> if all cells are free; otherwise, <c>false</c>.</returns>
        public bool AreCellsFree(List<Point> cells)
        {
            foreach (var p in cells)
                if (!World.InBounds(p) || World.IsOccupied(p)) return false;
            return true;
        }

        /// <summary>
        /// Gets the absolute footprint cells for a shape placed at the specified position and orientation.
        /// </summary>
        /// <param name="type">Shape type.</param>
        /// <param name="basePos">Base position.</param>
        /// <param name="facing">Facing direction.</param>
        /// <returns>List of absolute grid cells.</returns>
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
