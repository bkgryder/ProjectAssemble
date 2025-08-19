using System;
using Microsoft.Xna.Framework;

namespace ProjectAssemble.World
{
    /// <summary>
    /// Simple grid-based world used to track cell occupancy.
    /// </summary>
    public class GridWorld
    {
        /// <summary>
        /// Width and height of the grid in cells.
        /// </summary>
        public readonly int W, H;
        bool[,] _occ;

        /// <summary>
        /// Initializes a new instance of the <see cref="GridWorld"/> class.
        /// </summary>
        /// <param name="w">Width in cells.</param>
        /// <param name="h">Height in cells.</param>
        public GridWorld(int w, int h) { W = w; H = h; _occ = new bool[w, h]; }

        /// <summary>
        /// Clears the occupancy map prior to rebuilding.
        /// </summary>
        public void BeginOccupancy() { Array.Clear(_occ, 0, _occ.Length); }

        /// <summary>
        /// Completes occupancy rebuilding. Currently a no-op.
        /// </summary>
        public void EndOccupancy() { }

        /// <summary>
        /// Determines whether the given point is within bounds.
        /// </summary>
        public bool InBounds(Point p) => p.X >= 0 && p.Y >= 0 && p.X < W && p.Y < H;

        /// <summary>
        /// Determines whether the specified cell is occupied.
        /// </summary>
        public bool IsOccupied(Point p) => InBounds(p) && _occ[p.X, p.Y];

        /// <summary>
        /// Marks the specified cell as occupied if in bounds.
        /// </summary>
        public void MarkOccupied(Point p) { if (InBounds(p)) _occ[p.X, p.Y] = true; }
    }
}
