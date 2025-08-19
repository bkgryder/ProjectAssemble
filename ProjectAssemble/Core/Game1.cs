using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProjectAssemble.World;
using ProjectAssemble.Entities.Machines;
using ProjectAssemble.Entities.Shapes;
using ProjectAssemble.Systems;
using ProjectAssemble.UI;

namespace ProjectAssemble.Core
{
    // Step 1g: Arm labels (A,B,C,...) + multi-row timeline lanes by Arm
    // - Arms auto-assign next available label when placed; labels persist when moved
    // - Timeline shows one row per Arm with the label at left; scrubbing still sets the global current step
    // - Prep for future draggable action modules per-lane
    // Existing features retained: hover/select outlines, hotkeys, drag/edit, shapes with auto-replenish
    // Tilesheet: Content/Factory_16 (16x16)
    /// <summary>
    /// Main game entry point.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager _graphics;
        SpriteBatch _sb;

        Texture2D _px;      // 1x1
        Texture2D _tiles;   // tilesheet: Factory_16
        SpriteFont _font;   // optional

        // Grid
        const int TILE = 16;
        const int GRID_W = 32;
        const int GRID_H = 28;
        readonly Point _gridOrigin = new Point(200, 40);
        Rectangle GridRect => new Rectangle(_gridOrigin.X, _gridOrigin.Y, GRID_W * TILE, GRID_H * TILE);
        GridWorld _world;

        // Per-cell tile indices (background floor etc.)
        int[,] _tileIds;

        // Palettes and timeline UI
        MachinePaletteUI _machinePaletteUI;
        ShapePaletteUI _shapePaletteUI;
        ActionPaletteUI _actionPaletteUI;
        TimelineUI _timelineUI;
        ArmParameterUI _armParamUI;
        ArmAction _pendingArmAction = ArmAction.None;
        bool _draggingAction = false;
        int _currentStep = 0;

        // Drag state - machines
        bool _dragging = false;
        bool _draggingFromPalette = false;
        bool _draggingExisting = false;
        MachineType? _dragType = null;
        // Drag state - shapes
        bool _draggingShape = false;
        bool _draggingShapeExisting = false;
        ShapeType? _dragShapeType = null;

        Point _mouse;
        // Ghost edit props while dragging
        Direction _ghostFacing = Direction.Right;
        int _ghostExt = 0;
        IMachine _pickedMachine = null; // when dragging existing
        Point _pickedOriginCell;

        ShapeSource _pickedSource = null; // when dragging existing shape source
        Direction _ghostShapeFacing = Direction.Right;

        InputManager _input;
        WorldManager _worldManager;

        // Placed machines (managed by WorldManager)
        List<IMachine> Machines => _worldManager.Machines;

        // Shapes (managed by WorldManager)
        List<ShapeSource> ShapeSources => _worldManager.ShapeSources;
        List<ShapeInstance> ShapeInstances => _worldManager.ShapeInstances;

        // Hover/Select
        Point _hoverCell;
        bool _hoverInGrid = false;
        IMachine _hoverMachine = null;
        ShapeSource _hoverSource = null;

        IMachine _selectedMachine = null;
        ShapeSource _selectedSource = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Game1"/> class.
        /// </summary>
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 800;
            _input = new InputManager();
            _worldManager = new WorldManager(GRID_W, GRID_H, _input);
            _world = _worldManager.World;
        }

        protected override void Initialize()
        {
            _tileIds = new int[GRID_W, GRID_H];
            for (int x = 0; x < GRID_W; x++)
                for (int y = 0; y < GRID_H; y++)
                    _tileIds[x, y] = 0; // default floor index

            // UI components
            int rightX = _graphics.PreferredBackBufferWidth - 8 - 160;
            _machinePaletteUI = new MachinePaletteUI(new Rectangle(8, 8, 160, 200));
            _shapePaletteUI = new ShapePaletteUI(new Rectangle(rightX, 8, 160, 200));
            _actionPaletteUI = new ActionPaletteUI(new Rectangle(8, 220, 160, 80));
            _timelineUI = new TimelineUI();
            _armParamUI = new ArmParameterUI();
            _machinePaletteUI.MachinePicked += OnMachinePicked;
            _shapePaletteUI.ShapePicked += OnShapePicked;
            _actionPaletteUI.ActionPicked += OnActionPicked;
            _timelineUI.StepChanged += s => _currentStep = s;
            _timelineUI.SlotClicked += OnTimelineSlotClicked;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _sb = new SpriteBatch(GraphicsDevice);
            _px = new Texture2D(GraphicsDevice, 1, 1);
            _px.SetData(new[] { Color.White });

            try { _font = Content.Load<SpriteFont>("DefaultFont"); } catch { _font = null; }
            try { _tiles = Content.Load<Texture2D>("Factory_16"); } catch { _tiles = null; }
        }

        protected override void Update(GameTime gameTime)
        {
            _input.Update();
            var ms = _input.CurrentMouse;
            var kb = _input.CurrentKeyboard;
            if (kb.IsKeyDown(Keys.Escape)) Exit();

            _mouse = new Point(ms.X, ms.Y);

            var armsList = GetArmsSorted();
            _timelineUI.Update(_input, GridRect, armsList, _pendingArmAction != ArmAction.None);
            _machinePaletteUI.Update(_input);
            _shapePaletteUI.Update(_input);
            _actionPaletteUI.Update(_input);
            _armParamUI.Update(_input);

            _hoverInGrid = GridRect.Contains(_mouse);
            _hoverCell = ScreenToCell(_mouse);
            _hoverMachine = _hoverInGrid ? MachineAtCell(_hoverCell) : null;
            _hoverSource = _hoverInGrid ? SourceAtCell(_hoverCell) : null;

            // Rebuild occupancy from placed machines & shapes
            _worldManager.RebuildOccupancy();

            // ===== Drag start by picking an existing machine =====
            if (!_timelineUI.IsDragging && !_dragging && !_draggingShape && JustPressed(ms.LeftButton, _input.PreviousMouse.LeftButton) && _hoverMachine != null)
            {
                _dragging = true; _draggingFromPalette = false; _draggingExisting = true;
                _pickedMachine = _hoverMachine; _pickedOriginCell = _pickedMachine.BasePos;
                _dragType = _pickedMachine.Type;
                switch (_pickedMachine)
                {
                    case ArmMachine arm:
                        _ghostFacing = arm.Facing; _ghostExt = arm.Extension; break;
                }
                Machines.Remove(_pickedMachine); // remove while dragging so the cell is free
                _selectedMachine = null; _selectedSource = null;
            }

            // ===== Drag start by picking an existing shape source =====
            if (!_timelineUI.IsDragging && !_dragging && !_draggingShape && JustPressed(ms.LeftButton, _input.PreviousMouse.LeftButton) && _hoverSource != null)
            {
                _draggingShape = true; _draggingShapeExisting = true;
                _pickedSource = _hoverSource;
                _dragShapeType = _pickedSource.Type;
                _ghostShapeFacing = _pickedSource.Facing;
                ShapeSources.Remove(_pickedSource);
                _selectedMachine = null; _selectedSource = null;
            }

            // ===== While dragging: rotate & extend debug =====
            if (_dragging)
            {
                if (JustPressedKey(kb, _input.PreviousKeyboard, Keys.Q)) _ghostFacing = RotCCW(_ghostFacing);
                if (JustPressedKey(kb, _input.PreviousKeyboard, Keys.E)) _ghostFacing = RotCW(_ghostFacing);

                if (JustPressedKey(kb, _input.PreviousKeyboard, Keys.OemPlus) || JustPressedKey(kb, _input.PreviousKeyboard, Keys.Add))
                    _ghostExt = Math.Min(ArmMachine.MaxExtension, _ghostExt + 1);
                if (JustPressedKey(kb, _input.PreviousKeyboard, Keys.OemMinus) || JustPressedKey(kb, _input.PreviousKeyboard, Keys.Subtract))
                    _ghostExt = Math.Max(0, _ghostExt - 1);
            }

            if (_draggingShape)
            {
                if (JustPressedKey(kb, _input.PreviousKeyboard, Keys.Q)) _ghostShapeFacing = RotCCW(_ghostShapeFacing);
                if (JustPressedKey(kb, _input.PreviousKeyboard, Keys.E)) _ghostShapeFacing = RotCW(_ghostShapeFacing);
            }

            // ===== Drop machine =====
            if (_dragging && JustReleased(ms.LeftButton, _input.PreviousMouse.LeftButton))
            {
                bool placed = false;
                if (_hoverInGrid)
                {
                    var cell = _hoverCell;
                    if (_world.InBounds(cell) && !_world.IsOccupied(cell))
                    {
                        switch (_dragType)
                        {
                            case MachineType.Arm:
                                char label;
                                if (_draggingExisting && _pickedMachine is ArmMachine existing)
                                    label = existing.Label;
                                else
                                    label = NextArmLabel();
                                var arm = new ArmMachine(cell, _ghostFacing) { Extension = _ghostExt, Label = label };
                                Machines.Add(arm); placed = true; break;
                        }
                    }
                }

                if (!placed && _draggingExisting && _pickedMachine != null)
                {
                    switch (_pickedMachine)
                    {
                        case ArmMachine arm:
                            var back = new ArmMachine(_pickedOriginCell, _ghostFacing) { Extension = _ghostExt, Label = arm.Label };
                            Machines.Add(back);
                            break;
                    }
                }

                _dragging = false; _draggingFromPalette = _draggingExisting = false;
                _dragType = null; _pickedMachine = null;
            }

            // ===== Drop shape source =====
            if (_draggingShape && JustReleased(ms.LeftButton, _input.PreviousMouse.LeftButton))
            {
                bool placed = false;
                if (_hoverInGrid)
                {
                    var cell = _hoverCell;
                    if (_world.InBounds(cell) && !_world.IsOccupied(cell))
                    {
                        var src = new ShapeSource(cell, _dragShapeType.Value, _ghostShapeFacing);
                        ShapeSources.Add(src); placed = true;
                    }
                }

                if (!placed && _draggingShapeExisting && _pickedSource != null)
                {
                    // put back
                    ShapeSources.Add(new ShapeSource(_pickedSource.BasePos, _pickedSource.Type, _ghostShapeFacing));
                }

                _draggingShape = false; _draggingShapeExisting = false; _dragShapeType = null; _pickedSource = null;
            }

            // ===== Drop action onto timeline =====
            if (_draggingAction && JustReleased(ms.LeftButton, _input.PreviousMouse.LeftButton))
            {
                if (_pendingArmAction != ArmAction.None && _selectedMachine is ArmMachine selected)
                {
                    int row = _timelineUI.HoveredRow;
                    int step = _timelineUI.HoveredStep;
                    var arms = GetArmsSorted();
                    int idx = arms.IndexOf(selected);
                    if (idx == row && step >= 0 && step < selected.Program.Length)
                    {
                        if (selected.Program[step].Action == _pendingArmAction)
                            selected.Program[step] = default;
                        else
                            selected.Program[step] = new ArmCommand { Action = _pendingArmAction, Amount = 0 };
                    }
                }
                _draggingAction = false;
                _pendingArmAction = ArmAction.None;
            }

            // ===== Selection (Enter to select hovered; Esc clears) =====
            if (!_dragging && !_draggingShape)
            {
                if (JustPressedKey(kb, _input.PreviousKeyboard, Keys.Enter))
                {
                    _selectedMachine = _hoverMachine;
                    _selectedSource = _hoverSource;
                }
                if (JustPressedKey(kb, _input.PreviousKeyboard, Keys.Back) || JustPressedKey(kb, _input.PreviousKeyboard, Keys.Delete))
                {
                    // Delete hovered: instance > source > machine
                    if (_hoverInGrid)
                    {
                        var inst = InstanceAtCell(_hoverCell);
                        if (inst != null) ShapeInstances.Remove(inst);
                        else if (_hoverSource != null) ShapeSources.Remove(_hoverSource);
                        else if (_hoverMachine != null) Machines.Remove(_hoverMachine);
                    }
                }
                if (JustPressedKey(kb, _input.PreviousKeyboard, Keys.Escape))
                { _selectedMachine = null; _selectedSource = null; }
            }

            // ===== Hotkeys WITHOUT picking up =====
            if (!_dragging && !_draggingShape)
            {
                // Target is selected if any, else hovered
                IMachine targetM = _selectedMachine ?? _hoverMachine;
                if (targetM is ArmMachine ta)
                {
                    if (JustPressedKey(kb, _input.PreviousKeyboard, Keys.Q)) ta.Facing = RotCCW(ta.Facing);
                    if (JustPressedKey(kb, _input.PreviousKeyboard, Keys.E)) ta.Facing = RotCW(ta.Facing);
                    if (JustPressedKey(kb, _input.PreviousKeyboard, Keys.OemPlus) || JustPressedKey(kb, _input.PreviousKeyboard, Keys.Add))
                        ta.Extension = Math.Min(ArmMachine.MaxExtension, ta.Extension + 1);
                    if (JustPressedKey(kb, _input.PreviousKeyboard, Keys.OemMinus) || JustPressedKey(kb, _input.PreviousKeyboard, Keys.Subtract))
                        ta.Extension = Math.Max(0, ta.Extension - 1);
                }
            }

            // ===== Right-click to delete or configure arm =====
            if (!_armParamUI.Visible && JustPressed(ms.RightButton, _input.PreviousMouse.RightButton) && _hoverInGrid)
            {
                if (_hoverMachine is ArmMachine arm)
                {
                    _armParamUI.Show(arm, CellRect(arm.BasePos));
                }
                else
                {
                    var inst = InstanceAtCell(_hoverCell);
                    if (inst != null) ShapeInstances.Remove(inst);
                    else if (_hoverSource != null) ShapeSources.Remove(_hoverSource);
                    else if (_hoverMachine != null) Machines.Remove(_hoverMachine);
                }
            }

            // Auto-replenish shapes from sources
            _worldManager.ReplenishShapes();
            base.Update(gameTime);
        }

        void OnMachinePicked(MachineType type)
        {
            if (_timelineUI.IsDragging || _dragging || _draggingShape) return;
            _dragging = true; _draggingFromPalette = true; _draggingExisting = false;
            _dragType = type;
            _ghostFacing = Direction.Right;
            _ghostExt = 0;
            _pickedMachine = null;
            _selectedMachine = null; _selectedSource = null;
        }

        void OnShapePicked(ShapeType type)
        {
            if (_timelineUI.IsDragging || _dragging || _draggingShape) return;
            _draggingShape = true; _draggingShapeExisting = false;
            _dragShapeType = type;
            _ghostShapeFacing = Direction.Right;
            _pickedSource = null;
            _selectedMachine = null; _selectedSource = null;
        }

        void OnActionPicked(ArmAction action)
        {
            _pendingArmAction = action;
            _draggingAction = true;
        }

        void OnTimelineSlotClicked(int row, int step)
        {
            if (_pendingArmAction == ArmAction.None) return;
            if (_selectedMachine is ArmMachine selected)
            {
                var arms = GetArmsSorted();
                int idx = arms.IndexOf(selected);
                if (idx == row && step >= 0 && step < selected.Program.Length)
                {
                    if (selected.Program[step].Action == _pendingArmAction)
                        selected.Program[step] = default;
                    else
                        selected.Program[step] = new ArmCommand { Action = _pendingArmAction, Amount = 0 };
                }
            }
        }


        bool JustPressed(ButtonState cur, ButtonState prev) => cur == ButtonState.Pressed && prev == ButtonState.Released;
        bool JustReleased(ButtonState cur, ButtonState prev) => cur == ButtonState.Released && prev == ButtonState.Pressed;
        bool JustPressedKey(KeyboardState kb, KeyboardState prev, Keys k) => kb.IsKeyDown(k) && !prev.IsKeyDown(k);

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(18, 20, 24));
            _sb.Begin(samplerState: SamplerState.PointClamp);

            // Palettes
            _machinePaletteUI.Draw(_sb, _tiles, _px, _font);
            _shapePaletteUI.Draw(_sb, _px, _font);
            _actionPaletteUI.Draw(_sb, _px, _font);

            // Grid tiles
            DrawTiles();

            // Shape instances (items) below machines
            DrawShapeInstances();

            // Grid lines overlay
            DrawGridLines();

            // Machines
            foreach (var m in Machines)
                m.Draw(_sb, _tiles, _px, _gridOrigin, TilesPerRow);

            // Arm labels on grid
            DrawArmLabelsOnGrid();

            // Hover outline
            if (_hoverInGrid)
            {
                var cellR = CellRect(_hoverCell);
                var hoverColor = (_hoverMachine != null || _hoverSource != null) ? new Color(140, 210, 255) : new Color(200, 200, 200, 150);
                DrawRect(cellR, hoverColor, 2);
            }

            // Selected outline
            if (_selectedMachine != null)
            {
                var r = CellRect(_selectedMachine.BasePos);
                DrawRect(r, new Color(255, 240, 120), 3);
            }
            else if (_selectedSource != null)
            {
                var r = CellRect(_selectedSource.BasePos);
                DrawRect(r, new Color(255, 240, 120), 3);
            }

            // Drag ghosts (machines)
            if (_dragging && _dragType.HasValue)
            {
                if (_hoverInGrid)
                {
                    var cell = _hoverCell;
                    var valid = _world.InBounds(cell) && !_world.IsOccupied(cell);
                    var r = CellRect(cell);
                    FillRect(r, valid ? new Color(120, 200, 255, 40) : new Color(255, 120, 120, 40));
                    DrawRect(r, valid ? new Color(120, 200, 255) : new Color(200, 80, 80), 2);

                    if (_tiles != null)
                    {
                        switch (_dragType)
                        {
                            case MachineType.Arm:
                                var temp = new ArmMachine(cell, _ghostFacing) { Extension = _ghostExt };
                                temp.Draw(_sb, _tiles, _px, _gridOrigin, TilesPerRow);
                                break;
                        }
                    }
                }
            }

            // Drag ghosts (shapes)
            if (_draggingShape && _dragShapeType.HasValue)
            {
                if (_hoverInGrid)
                {
                    var cell = _hoverCell;
                    var cells = GetFootprint(_dragShapeType.Value, cell, _ghostShapeFacing);
                    bool valid = _worldManager.AreCellsFree(cells);
                    foreach (var p in cells)
                    {
                        var r = CellRect(p);
                        FillRect(r, valid ? new Color(160, 255, 180, 50) : new Color(255, 120, 120, 40));
                        DrawRect(r, valid ? new Color(120, 240, 140) : new Color(200, 80, 80), 2);
                    }
                }
            }

            // Timeline
            _timelineUI.Draw(_sb, _px, _font, GetArmsSorted());
            _armParamUI.Draw(_sb, _px, _font);

            // Help text
            if (_font != null)
            {
                string help = $"Hover/select + hotkeys. Arms auto-label A-D. Move/rotate/extend. Shapes auto-replenish. Drag timeline lanes to scrub step 0-{_timelineUI.StepCount - 1}.";
                _sb.DrawString(_font, help, new Vector2(12, _timelineUI.Rect.Bottom + 10), Color.White);
            }

            _sb.End();
            base.Draw(gameTime);
        }

        // ======= Label helpers =======
        char NextArmLabel()
        {
            var used = new HashSet<char>();
            foreach (var m in Machines)
                if (m is ArmMachine a && a.Label != ' ') used.Add(a.Label);
            for (char c = 'A'; c <= 'D'; c++) if (!used.Contains(c)) return c;
            return '?';
        }

        List<ArmMachine> GetArmsSorted()
        {
            var list = new List<ArmMachine>();
            foreach (var m in Machines) if (m is ArmMachine a) list.Add(a);
            list.Sort((a, b) => a.Label.CompareTo(b.Label));
            return list;
        }

        void DrawArmLabelsOnGrid()
        {
            if (_font == null) return;
            foreach (var a in GetArmsSorted())
            {
                var r = CellRect(a.BasePos);
                var s = a.Label == ' ' ? "?" : a.Label.ToString();
                _sb.DrawString(_font, s, new Vector2(r.X + 2, r.Y + 1), new Color(255, 240, 140));
            }
        }

        // ======= Tile helpers =======
        int TilesPerRow => _tiles == null ? 1 : Math.Max(1, _tiles.Width / TILE);
        Rectangle SrcRect(int tileIndex)
        {
            if (_tiles == null) return new Rectangle(0, 0, 1, 1);
            int tx = tileIndex % TilesPerRow;
            int ty = tileIndex / TilesPerRow;
            return new Rectangle(tx * TILE, ty * TILE, TILE, TILE);
        }
        Rectangle SrcCR(int col1, int row1)
        {
            if (_tiles == null) return new Rectangle(0, 0, 1, 1);
            int idx = (row1 - 1) * TilesPerRow + (col1 - 1);
            return SrcRect(idx);
        }

        void DrawTiles()
        {
            if (_tiles == null)
            {
                FillRect(GridRect, new Color(28, 30, 36));
                return;
            }

            for (int y = 0; y < GRID_H; y++)
            {
                for (int x = 0; x < GRID_W; x++)
                {
                    var dest = new Rectangle(_gridOrigin.X + x * TILE, _gridOrigin.Y + y * TILE, TILE, TILE);
                    _sb.Draw(_tiles, dest, SrcRect(_tileIds[x, y]), Color.White);
                }
            }
        }

        void DrawGridLines()
        {
            var line = new Color(38, 42, 52, 140);
            for (int y = 0; y <= GRID_H; y++)
            {
                int ypix = _gridOrigin.Y + y * TILE;
                _sb.Draw(_px, new Rectangle(_gridOrigin.X, ypix, GRID_W * TILE, 1), line);
            }
            for (int x = 0; x <= GRID_W; x++)
            {
                int xpix = _gridOrigin.X + x * TILE;
                _sb.Draw(_px, new Rectangle(xpix, _gridOrigin.Y, 1, GRID_H * TILE), line);
            }
        }

        Rectangle CellRect(Point cell)
        {
            return new Rectangle(_gridOrigin.X + cell.X * TILE, _gridOrigin.Y + cell.Y * TILE, TILE, TILE);
        }

        void FillRect(Rectangle r, Color c) => _sb.Draw(_px, r, c);
        void DrawRect(Rectangle r, Color c, int t = 1)
        {
            _sb.Draw(_px, new Rectangle(r.X, r.Y, r.Width, t), c);
            _sb.Draw(_px, new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
            _sb.Draw(_px, new Rectangle(r.X, r.Y, t, r.Height), c);
            _sb.Draw(_px, new Rectangle(r.Right - t, r.Y, t, r.Height), c);
        }

        IMachine MachineAtCell(Point cell)
        {
            foreach (var m in Machines) if (m.BasePos == cell) return m; return null;
        }

        ShapeSource SourceAtCell(Point cell)
        {
            foreach (var s in ShapeSources) if (s.BasePos == cell) return s; return null;
        }

        ShapeInstance InstanceAtCell(Point cell)
        {
            foreach (var si in ShapeInstances)
                foreach (var c in si.Cells)
                    if (c == cell) return si;
            return null;
        }

        Point ScreenToCell(Point screen)
        {
            int cx = (screen.X - _gridOrigin.X) / TILE;
            int cy = (screen.Y - _gridOrigin.Y) / TILE;
            return new Point(cx, cy);
        }

        // Facing helpers
        static Direction RotCW(Direction d) => (Direction)(((int)d + 1) & 3);
        static Direction RotCCW(Direction d) => (Direction)(((int)d + 3) & 3);

        // === Shapes ===
        List<Point> GetFootprint(ShapeType type, Point basePos, Direction facing)
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
            // rotate (x,y) around (0,0) by 90* k where k depends on facing (Right=0, Down=1, Left=2, Up=3)
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

        void DrawShapeInstances()
        {
            foreach (var inst in ShapeInstances)
            {
                foreach (var c in inst.Cells)
                {
                    var r = CellRect(c);
                    FillRect(r, new Color(160, 255, 180, 80));
                    DrawRect(r, new Color(90, 200, 120), 2);
                }
            }

            // Draw sources (pins)
            foreach (var src in ShapeSources)
            {
                var r = CellRect(src.BasePos);
                FillRect(r, new Color(120, 170, 255, 25));
                DrawRect(r, new Color(120, 170, 255), 2);
                if (_font != null) _sb.DrawString(_font, src.Type.ToString(), new Vector2(r.X + 2, r.Y + 1), new Color(200, 220, 255));
            }
        }
    }
}
