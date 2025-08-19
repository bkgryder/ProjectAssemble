using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectAssemble.Core;

namespace ProjectAssemble.Entities.Machines
{
    public class ArmMachine : IMachine
    {
        public const int MaxExtension = 3; // debug clamp
        public MachineType Type => MachineType.Arm;
        public Point BasePos { get; private set; }
        public Direction Facing { get; set; }
        public int Extension { get; set; } = 0; // tiles beyond base along Facing
        public char Label { get; set; } = ' '; // 'A','B',...

        public ArmMachine(Point basePos, Direction facing)
        {
            BasePos = basePos;
            Facing = facing;
        }

        public void Draw(SpriteBatch sb, Texture2D tiles, Texture2D px, Point origin, int tilesPerRow)
        {
            var center = new Vector2(origin.X + BasePos.X * 16 + 8, origin.Y + BasePos.Y * 16 + 8);
            Vector2 originPx = new Vector2(8, 8);
            float rot = Dir.Angle(Facing);

            if (tiles == null)
            {
                sb.Draw(px, new Rectangle((int)center.X - 8, (int)center.Y - 8, 16, 16), new Color(180, 180, 200));
                return;
            }

            Rectangle SRC(int c, int r)
            {
                int idx = (r - 1) * tilesPerRow + (c - 1);
                int tx = idx % tilesPerRow;
                int ty = idx / tilesPerRow;
                return new Rectangle(tx * 16, ty * 16, 16, 16);
            }

            var baseSrc = (Extension > 0) ? SRC(2, 2) : SRC(1, 2);
            sb.Draw(tiles, center, baseSrc, Color.White, rot, originPx, 1f, SpriteEffects.None, 0f);

            if (Extension <= 0) return;

            var d = Dir.ToDelta(Facing);
            for (int i = 1; i < Extension; i++)
            {
                var segCenter = new Vector2(origin.X + (BasePos.X + d.X * i) * 16 + 8,
                                             origin.Y + (BasePos.Y + d.Y * i) * 16 + 8);
                sb.Draw(tiles, segCenter, SRC(3, 1), Color.White, rot, originPx, 1f, SpriteEffects.None, 0f);
            }
            var headCenter = new Vector2(origin.X + (BasePos.X + d.X * Extension) * 16 + 8,
                                          origin.Y + (BasePos.Y + d.Y * Extension) * 16 + 8);
            sb.Draw(tiles, headCenter, SRC(2, 1), Color.White, rot, originPx, 1f, SpriteEffects.None, 0f);
        }
    }
}
