using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectAssemble.Core;

namespace ProjectAssemble.Entities.Machines
{
    public interface IMachine
    {
        MachineType Type { get; }
        Point BasePos { get; }
        void Draw(SpriteBatch sb, Texture2D tiles, Texture2D px, Point origin, int tilesPerRow);
    }
}
