using System;
using Microsoft.Xna.Framework;

namespace ProjectAssemble.World
{
    public class GridWorld
    {
        public readonly int W, H;
        bool[,] _occ;
        public GridWorld(int w, int h) { W = w; H = h; _occ = new bool[w, h]; }
        public void BeginOccupancy() { Array.Clear(_occ, 0, _occ.Length); }
        public void EndOccupancy() { }
        public bool InBounds(Point p) => p.X >= 0 && p.Y >= 0 && p.X < W && p.Y < H;
        public bool IsOccupied(Point p) => InBounds(p) && _occ[p.X, p.Y];
        public void MarkOccupied(Point p) { if (InBounds(p)) _occ[p.X, p.Y] = true; }
    }
}
