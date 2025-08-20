using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectAssemble.Core;

namespace ProjectAssemble.Entities.Machines
{
    /// <summary>
    /// A machine that can extend and rotate to manipulate shapes.
    /// </summary>
    public class ArmMachine : IMachine
    {
        /// <summary>
        /// Maximum extension length of the arm.
        /// </summary>
        public const int MaxExtension = 3; // debug clamp

        /// <inheritdoc/>
        public MachineType Type => MachineType.Arm;

        /// <inheritdoc/>
        public Point BasePos { get; private set; }

        /// <summary>
        /// Gets or sets the facing direction of the arm.
        /// </summary>
        public Direction Facing { get; set; }

        /// <summary>
        /// Gets or sets the extension length beyond the base along the facing direction.
        /// </summary>
        public int Extension { get; set; } = 0; // tiles beyond base along Facing

        /// <summary>
        /// Gets or sets the label assigned to this arm.
        /// </summary>
        public char Label { get; set; } = ' '; // 'A','B',...

        /// <summary>
        /// Gets or sets how far the arm moves when commanded.
        /// </summary>
        public int MoveAmount { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value indicating whether the arm is currently grabbing.
        /// </summary>
        public bool Grabbed { get; set; } = false;

        /// <summary>
        /// Programmed commands for each step of the timeline.
        /// </summary>
        public ArmCommand[] Program { get; } = new ArmCommand[Timeline.Steps];

        /// <summary>
        /// Initializes a new instance of the <see cref="ArmMachine"/> class.
        /// </summary>
        /// <param name="basePos">Base position of the arm.</param>
        /// <param name="facing">Initial facing direction.</param>
        public ArmMachine(Point basePos, Direction facing)
        {
            BasePos = basePos;
            Facing = facing;
            for (int i = 0; i < Program.Length; i++)
            {
                Program[i] = new ArmCommand { Action = ArmAction.None, Amount = 0 };
            }
        }

        /// <summary>
        /// Executes the command assigned to the specified timeline step.
        /// </summary>
        /// <param name="step">The timeline step to execute.</param>
        public void ExecuteStep(int step)
        {
            if (step < 0 || step >= Program.Length) return;

            var cmd = Program[step];
            if (cmd.Action == ArmAction.Move)
            {
                Extension = Math.Clamp(Extension + cmd.Amount, 0, MaxExtension);
            }
        }

        /// <inheritdoc/>
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
