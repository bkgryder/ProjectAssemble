using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectAssemble.Core;

namespace ProjectAssemble.Entities.Machines
{
    /// <summary>
    /// Represents a machine that can be placed in the world.
    /// </summary>
    public interface IMachine
    {
        /// <summary>
        /// Gets the type of machine.
        /// </summary>
        MachineType Type { get; }

        /// <summary>
        /// Gets the base grid position of the machine.
        /// </summary>
        Point BasePos { get; }

        /// <summary>
        /// Draws the machine using the provided sprite batch and textures.
        /// </summary>
        /// <param name="sb">The sprite batch to draw with.</param>
        /// <param name="tiles">The tilesheet texture.</param>
        /// <param name="px">A 1x1 pixel texture.</param>
        /// <param name="origin">Origin offset of the grid.</param>
        /// <param name="tilesPerRow">Number of tiles per row in the tilesheet.</param>
        void Draw(SpriteBatch sb, Texture2D tiles, Texture2D px, Point origin, int tilesPerRow);
    }
}
