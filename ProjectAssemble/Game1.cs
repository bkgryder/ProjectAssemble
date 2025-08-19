using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProjectAssemble.Core;
using ProjectAssemble.World;
using ProjectAssemble.Entities.Machines;
using ProjectAssemble.Entities.Shapes;

namespace ProjectAssemble
{
    // Step 1g: Arm labels (A,B,C,...) + multi-row timeline lanes by Arm
    // - Arms auto-assign next available label when placed; labels persist when moved
    // - Timeline shows one row per Arm with the label at left; scrubbing still sets the global current step
    // - Prep for future draggable action modules per-lane
    // Existing features retained: hover/select outlines, hotkeys, drag/edit, shapes with auto-replenish
    // Tilesheet: Content/Factory_16 (16x16)
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

        // Palettes
        Rectangle _machinePaletteRect = new Rectangle(8, 8, 160, 200);
        Rectangle _shapePaletteRect; // computed in Initialize based on backbuffer

        // Timeline
        const int TIMESTEPS = 21; // 0..20 inclusive
        Rectangle _timelineRect;  // recomputed each Update to fit lane count
        int _currentStep = 0;
        int _hoveredStep = -1;
        int _hoveredRow = -1; // index into sorted arms list
        bool _timelineDragging = false;

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

        // Placed machines
        List<IMachine> _machines = new List<IMachine>();

        // Shapes
        List<ShapeSource> _shapeSources = new List<ShapeSource>();
        List<ShapeInstance> _shapeInstances = new List<ShapeInstance>();

        // Hover/Select
        Point _hoverCell;
        bool _hoverInGrid = false;
        IMachine _hoverMachine = null;
        ShapeSource _hoverSource = null;

        IMachine _selectedMachine = null;
        ShapeSource _selectedSource = null;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 800;
        }

        protected override void Initialize()
        {
            _world = new GridWorld(GRID_W, GRID_H);
            _tileIds = new int[GRID_W, GRID_H];
            for (int x = 0; x < GRID_W; x++)
                for (int y = 0; y < GRID_H; y++)
                    _tileIds[x, y] = 0; // default floor index

            // Right-side palette placement
            int rightX = _graphics.PreferredBackBufferWidth - 8 - 160;
            _shapePaletteRect = new Rectangle(rightX, 8, 160, 200);

            // Initial timeline rect; height will adjust each frame based on lane count
            _timelineRect = new Rectangle(_gridOrigin.X, GridRect.Bottom + 12, GRID_W * TILE, 60);

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

        MouseState _prevMS;
        KeyboardState _prevKB;
        protected override void Update(GameTime gameTime)
        {
            var ms = Mouse.GetState();
            var kb = Keyboard.GetState();
            if (kb.IsKeyDown(Keys.Escape)) Exit();

            _mouse = new Point(ms.X, ms.Y);

            // Recompute timeline height to fit the number of Arm lanes
            var armsList = GetArmsSorted();
            int lanes = Math.Max(1, armsList.Count);
            int laneH = 22; int pad = 8; // vertical padding inside timeline panel
            int labelColW = 48; // left label column width
            int gap = 4; // horizontal gap between steps
            int innerHeight = lanes * laneH;
            _timelineRect = new Rectangle(_gridOrigin.X, GridRect.Bottom + 12, GRID_W * TILE, pad * 2 + innerHeight + 18); // extra for tick labels

            // Hover
            _hoverInGrid = GridRect.Contains(_mouse);
            _hoverCell = ScreenToCell(_mouse);
            _hoverMachine = _hoverInGrid ? MachineAtCell(_hoverCell) : null;
            _hoverSource = _hoverInGrid ? SourceAtCell(_hoverCell) : null;

            // Hover step/row calc
            _hoveredStep = TimelineStepAt(_mouse);
            _hoveredRow = TimelineRowAt(_mouse);

            // ===== Timeline drag/scrub handling =====
            if (!_dragging && !_draggingShape)
            {
                if (!_timelineDragging && JustPressed(ms.LeftButton, _prevMS.LeftButton) && _timelineRect.Contains(_mouse))
                {
                    _timelineDragging = true;
                    if (_hoveredStep >= 0) _currentStep = _hoveredStep;
                }
                if (_timelineDragging && ms.LeftButton == ButtonState.Pressed)
                {
                    if (_hoveredStep >= 0) _currentStep = _hoveredStep;
                }
                if (_timelineDragging && JustReleased(ms.LeftButton, _prevMS.LeftButton))
                {
                    _timelineDragging = false;
                }
            }

            // Rebuild occupancy from placed machines & shapes
            RebuildOccupancy();

            // ===== Drag start from machine palette =====
            if (!_timelineDragging && !_dragging && !_draggingShape && JustPressed(ms.LeftButton, _prevMS.LeftButton) && _machinePaletteRect.Contains(_mouse))
            {
                var picked = PaletteMachineAt(_mouse);
                if (picked.HasValue)
                {
                    _dragging = true; _draggingFromPalette = true; _draggingExisting = false;
                    _dragType = picked.Value;
                    _ghostFacing = Direction.Right;
                    _ghostExt = 0;
                    _pickedMachine = null;
                    _selectedMachine = null; _selectedSource = null;
                }
            }

            // ===== Drag start by picking an existing machine =====
            if (!_timelineDragging && !_dragging && !_draggingShape && JustPressed(ms.LeftButton, _prevMS.LeftButton) && _hoverMachine != null)
            {
                _dragging = true; _draggingFromPalette = false; _draggingExisting = true;
                _pickedMachine = _hoverMachine; _pickedOriginCell = _pickedMachine.BasePos;
                _dragType = _pickedMachine.Type;
                switch (_pickedMachine)
                {
                    case ArmMachine arm:
                        _ghostFacing = arm.Facing; _ghostExt = arm.Extension; break;
                }
                _machines.Remove(_pickedMachine); // remove while dragging so the cell is free
                _selectedMachine = null; _selectedSource = null;
            }

            // ===== Drag start from shape palette =====
            if (!_timelineDragging && !_dragging && !_draggingShape && JustPressed(ms.LeftButton, _prevMS.LeftButton) && _shapePaletteRect.Contains(_mouse))
            {
                var picked = PaletteShapeAt(_mouse);
                if (picked.HasValue)
                {
                    _draggingShape = true; _draggingShapeExisting = false;
                    _dragShapeType = picked.Value;
                    _ghostShapeFacing = Direction.Right;
                    _pickedSource = null;
                    _selectedMachine = null; _selectedSource = null;
                }
            }

            // ===== Drag start by picking an existing shape source =====
            if (!_timelineDragging && !_dragging && !_draggingShape && JustPressed(ms.LeftButton, _prevMS.LeftButton) && _hoverSource != null)
            {
                _draggingShape = true; _draggingShapeExisting = true;
                _pickedSource = _hoverSource;
                _dragShapeType = _pickedSource.Type;
                _ghostShapeFacing = _pickedSource.Facing;
                _shapeSources.Remove(_pickedSource);
                _selectedMachine = null; _selectedSource = null;
            }

            // ===== While dragging: rotate & extend debug =====
            if (_dragging)
            {
                if (JustPressedKey(kb, _prevKB, Keys.Q)) _ghostFacing = RotCCW(_ghostFacing);
                if (JustPressedKey(kb, _prevKB, Keys.E)) _ghostFacing = RotCW(_ghostFacing);

                if (JustPressedKey(kb, _prevKB, Keys.OemPlus) || JustPressedKey(kb, _prevKB, Keys.Add))
                    _ghostExt = Math.Min(ArmMachine.MaxExtension, _ghostExt + 1);
                if (JustPressedKey(kb, _prevKB, Keys.OemMinus) || JustPressedKey(kb, _prevKB, Keys.Subtract))
                    _ghostExt = Math.Max(0, _ghostExt - 1);
            }

            if (_draggingShape)
            {
                if (JustPressedKey(kb, _prevKB, Keys.Q)) _ghostShapeFacing = RotCCW(_ghostShapeFacing);
                if (JustPressedKey(kb, _prevKB, Keys.E)) _ghostShapeFacing = RotCW(_ghostShapeFacing);
            }

            // ===== Drop machine =====
            if (_dragging && JustReleased(ms.LeftButton, _prevMS.LeftButton))
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
                                var label = NextArmLabel();
                                var arm = new ArmMachine(cell, _ghostFacing) { Extension = _ghostExt, Label = label };
                                _machines.Add(arm); placed = true; break;
                        }
                    }
                }

                if (!placed && _draggingExisting && _pickedMachine != null)
                {
                    switch (_pickedMachine)
                    {
                        case ArmMachine arm:
                            var back = new ArmMachine(_pickedOriginCell, _ghostFacing) { Extension = _ghostExt, Label = arm.Label };
                            _machines.Add(back);
                            break;
                    }
                }

                _dragging = false; _draggingFromPalette = _draggingExisting = false;
                _dragType = null; _pickedMachine = null;
            }

            // ===== Drop shape source =====
            if (_draggingShape && JustReleased(ms.LeftButton, _prevMS.LeftButton))
            {
                bool placed = false;
                if (_hoverInGrid)
                {
                    var cell = _hoverCell;
                    if (_world.InBounds(cell) && !_world.IsOccupied(cell))
                    {
                        var src = new ShapeSource(cell, _dragShapeType.Value, _ghostShapeFacing);
                        _shapeSources.Add(src); placed = true;
                    }
                }

                if (!placed && _draggingShapeExisting && _pickedSource != null)
                {
                    // put back
                    _shapeSources.Add(new ShapeSource(_pickedSource.BasePos, _pickedSource.Type, _ghostShapeFacing));
                }

                _draggingShape = false; _draggingShapeExisting = false; _dragShapeType = null; _pickedSource = null;
            }

            // ===== Selection (Enter to select hovered; Esc clears) =====
            if (!_dragging && !_draggingShape)
            {
                if (JustPressedKey(kb, _prevKB, Keys.Enter))
                {
                    _selectedMachine = _hoverMachine;
                    _selectedSource = _hoverSource;
                }
                if (JustPressedKey(kb, _prevKB, Keys.Back) || JustPressedKey(kb, _prevKB, Keys.Delete))
                {
                    // Delete hovered: instance > source > machine
                    if (_hoverInGrid)
                    {
                        var inst = InstanceAtCell(_hoverCell);
                        if (inst != null) _shapeInstances.Remove(inst);
                        else if (_hoverSource != null) _shapeSources.Remove(_hoverSource);
                        else if (_hoverMachine != null) _machines.Remove(_hoverMachine);
                    }
                }
                if (JustPressedKey(kb, _prevKB, Keys.Escape))
                { _selectedMachine = null; _selectedSource = null; }
            }

            // ===== Hotkeys WITHOUT picking up =====
            if (!_dragging && !_draggingShape)
            {
                // Target is selected if any, else hovered
                IMachine targetM = _selectedMachine ?? _hoverMachine;
                if (targetM is ArmMachine ta)
                {
                    if (JustPressedKey(kb, _prevKB, Keys.Q)) ta.Facing = RotCCW(ta.Facing);
                    if (JustPressedKey(kb, _prevKB, Keys.E)) ta.Facing = RotCW(ta.Facing);
                    if (JustPressedKey(kb, _prevKB, Keys.OemPlus) || JustPressedKey(kb, _prevKB, Keys.Add))
                        ta.Extension = Math.Min(ArmMachine.MaxExtension, ta.Extension + 1);
                    if (JustPressedKey(kb, _prevKB, Keys.OemMinus) || JustPressedKey(kb, _prevKB, Keys.Subtract))
                        ta.Extension = Math.Max(0, ta.Extension - 1);
                }
            }

            // ===== Right-click to delete =====
            if (JustPressed(ms.RightButton, _prevMS.RightButton) && _hoverInGrid)
            {
                var inst = InstanceAtCell(_hoverCell);
                if (inst != null) _shapeInstances.Remove(inst);
                else if (_hoverSource != null) _shapeSources.Remove(_hoverSource);
                else if (_hoverMachine != null) _machines.Remove(_hoverMachine);
            }

            // Auto-replenish shapes from sources
            ReplenishShapes();

            _prevMS = ms; _prevKB = kb;
            base.Update(gameTime);
        }

        void RebuildOccupancy()
        {
            _world.BeginOccupancy();
            foreach (var m in _machines) _world.MarkOccupied(m.BasePos);
            foreach (var s in _shapeSources) _world.MarkOccupied(s.BasePos);
            foreach (var inst in _shapeInstances)
                foreach (var c in inst.Cells) _world.MarkOccupied(c);
            _world.EndOccupancy();
        }

        void ReplenishShapes()
        {
            // Ensure each source has exactly one instance at its start spot when footprint is clear
            for (int i = 0; i < _shapeSources.Count; i++)
            {
                var src = _shapeSources[i];
                bool has = _shapeInstances.Exists(si => si.SourceId == src.Id);
                if (!has)
                {
                    var cells = GetFootprint(src.Type, src.BasePos, src.Facing);
                    if (AreCellsFree(cells))
                        _shapeInstances.Add(new ShapeInstance(src.Id, cells));
                }
            }
        }

        bool AreCellsFree(List<Point> cells)
        {
            foreach (var p in cells)
                if (!_world.InBounds(p) || _world.IsOccupied(p)) return false;
            return true;
        }

        bool JustPressed(ButtonState cur, ButtonState prev) => cur == ButtonState.Pressed && prev == ButtonState.Released;
        bool JustReleased(ButtonState cur, ButtonState prev) => cur == ButtonState.Released && prev == ButtonState.Pressed;
        bool JustPressedKey(KeyboardState kb, KeyboardState prev, Keys k) => kb.IsKeyDown(k) && !prev.IsKeyDown(k);

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(18, 20, 24));
            _sb.Begin(samplerState: SamplerState.PointClamp);

            // Palettes
            DrawMachinePalette();
            DrawShapePalette();

            // Grid tiles
            DrawTiles();

            // Shape instances (items) below machines
            DrawShapeInstances();

            // Grid lines overlay
            DrawGridLines();

            // Machines
            foreach (var m in _machines)
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
                    bool valid = AreCellsFree(cells);
                    foreach (var p in cells)
                    {
                        var r = CellRect(p);
                        FillRect(r, valid ? new Color(160, 255, 180, 50) : new Color(255, 120, 120, 40));
                        DrawRect(r, valid ? new Color(120, 240, 140) : new Color(200, 80, 80), 2);
                    }
                }
            }

            // Timeline
            DrawTimeline();

            // Help text
            if (_font != null)
            {
                string help = "Hover/select + hotkeys. Arms auto-label A,B,C... Move/rotate/extend. Shapes auto-replenish. Drag timeline lanes to scrub step 0-20.";
                _sb.DrawString(_font, help, new Vector2(12, _timelineRect.Bottom + 10), Color.White);
            }

            _sb.End();
            base.Draw(gameTime);
        }

        // ======= Label helpers =======
        char NextArmLabel()
        {
            var used = new HashSet<char>();
            foreach (var m in _machines)
                if (m is ArmMachine a && a.Label != ' ') used.Add(a.Label);
            for (char c = 'A'; c <= 'Z'; c++) if (!used.Contains(c)) return c;
            return '?';
        }

        List<ArmMachine> GetArmsSorted()
        {
            var list = new List<ArmMachine>();
            foreach (var m in _machines) if (m is ArmMachine a) list.Add(a);
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

        // ======= Timeline helpers =======
        void DrawTimeline()
        {
            // Collect lanes (arms sorted by label)
            var arms = GetArmsSorted();
            int lanes = Math.Max(1, arms.Count);

            // Panel
            FillRect(_timelineRect, new Color(30, 32, 38));
            DrawRect(_timelineRect, new Color(80, 85, 98), 2);

            // Geometry (must match TimelineStepAt/TimelineRowAt)
            int pad = 8; int gap = 4; int laneH = 22; int labelColW = 48;
            var inner = new Rectangle(_timelineRect.X + pad, _timelineRect.Y + pad, _timelineRect.Width - pad * 2, lanes * laneH);
            int slotsW = Math.Max(40, inner.Width - labelColW);
            int slotW = Math.Max(14, (slotsW - gap * (TIMESTEPS - 1)) / TIMESTEPS);
            int slotH = laneH - 2;
            int slotsX = inner.X + labelColW;

            // Lanes
            for (int row = 0; row < lanes; row++)
            {
                var laneY = inner.Y + row * laneH;
                var laneRect = new Rectangle(inner.X, laneY, inner.Width, laneH);
                // stripe background
                var stripe = (row % 2 == 0) ? new Color(255, 255, 255, 12) : new Color(255, 255, 255, 6);
                FillRect(laneRect, stripe);

                // Label cell
                var labelRect = new Rectangle(inner.X, laneY, labelColW - 6, laneH);
                DrawRect(labelRect, new Color(60, 65, 78), 1);
                if (_font != null)
                {
                    string labelText = (row < arms.Count) ? ("Arm " + arms[row].Label) : "";
                    var size = _font.MeasureString(labelText);
                    _sb.DrawString(_font, labelText, new Vector2(labelRect.X + 6, labelRect.Y + (laneH - size.Y) / 2f), Color.White);
                }

                // Slots
                for (int i = 0; i < TIMESTEPS; i++)
                {
                    int x = slotsX + i * (slotW + gap);
                    var r = new Rectangle(x, laneY + 1, slotW, slotH);
                    bool isCurrent = (i == _currentStep);
                    bool isHoverStep = (i == _hoveredStep);
                    bool isHoverRow = (row == _hoveredRow);
                    var fill = new Color(255, 255, 255, 10);
                    if (isCurrent) fill = new Color(120, 200, 255, 90);
                    else if (isHoverStep && isHoverRow) fill = new Color(200, 220, 255, 40);
                    FillRect(r, fill);
                    DrawRect(r, isCurrent ? new Color(120, 200, 255) : new Color(160, 170, 190), 1);
                }
            }

            // Tick labels along the bottom + current step header
            if (_font != null)
            {
                for (int i = 0; i < TIMESTEPS; i += 5)
                {
                    int x = slotsX + i * (slotW + gap);
                    var s = i.ToString();
                    _sb.DrawString(_font, s, new Vector2(x + 2, inner.Bottom + 2), new Color(200, 210, 230));
                }
                _sb.DrawString(_font, $"Step: {_currentStep} / {TIMESTEPS - 1}", new Vector2(_timelineRect.X + 6, _timelineRect.Y - 18), Color.White);
            }
        }

        int TimelineStepAt(Point mouse)
        {
            if (!_timelineRect.Contains(mouse)) return -1;
            // match layout from DrawTimeline
            int pad = 8; int gap = 4; int laneH = 22; int labelColW = 48;
            var arms = GetArmsSorted(); int lanes = Math.Max(1, arms.Count);
            var inner = new Rectangle(_timelineRect.X + pad, _timelineRect.Y + pad, _timelineRect.Width - pad * 2, lanes * laneH);
            int slotsW = Math.Max(40, inner.Width - labelColW);
            int slotW = Math.Max(14, (slotsW - gap * (TIMESTEPS - 1)) / TIMESTEPS);
            int totalW = TIMESTEPS * slotW + (TIMESTEPS - 1) * gap;
            int relX = mouse.X - (inner.X + labelColW);
            if (relX < 0 || relX >= totalW) return -1;
            int step = relX / (slotW + gap);
            int insideX = relX % (slotW + gap);
            if (insideX >= slotW) return -1; // gap
            return Math.Clamp(step, 0, TIMESTEPS - 1);
        }

        int TimelineRowAt(Point mouse)
        {
            if (!_timelineRect.Contains(mouse)) return -1;
            int pad = 8; int laneH = 22;
            var arms = GetArmsSorted(); int lanes = Math.Max(1, arms.Count);
            var inner = new Rectangle(_timelineRect.X + pad, _timelineRect.Y + pad, _timelineRect.Width - pad * 2, lanes * laneH);
            if (mouse.Y < inner.Y || mouse.Y >= inner.Bottom) return -1;
            int relY = mouse.Y - inner.Y;
            int row = relY / laneH;
            return Math.Clamp(row, 0, lanes - 1);
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

        void DrawMachinePalette()
        {
            FillRect(_machinePaletteRect, new Color(30, 32, 38));
            DrawRect(_machinePaletteRect, new Color(80, 85, 98), 2);

            var inner = new Rectangle(_machinePaletteRect.X + 8, _machinePaletteRect.Y + 8, _machinePaletteRect.Width - 16, 56);
            FillRect(inner, new Color(255, 255, 255, 8));
            DrawRect(inner, Color.White, 1);

            if (_tiles != null)
            {
                var dest = new Rectangle(inner.X + 12, inner.Y + 12, 32, 32); // 2x scale preview
                _sb.Draw(_tiles, dest, SrcCR(1, 2), Color.White);
                if (_font != null) _sb.DrawString(_font, "Arm", new Vector2(dest.Right + 8, dest.Y + 8), Color.White);
            }
            else
            {
                if (_font != null) _sb.DrawString(_font, "Arm", new Vector2(inner.X + 8, inner.Y + 8), Color.White);
            }
        }

        void DrawShapePalette()
        {
            FillRect(_shapePaletteRect, new Color(30, 32, 38));
            DrawRect(_shapePaletteRect, new Color(80, 85, 98), 2);

            if (_font != null)
                _sb.DrawString(_font, "Shapes", new Vector2(_shapePaletteRect.X + 8, _shapePaletteRect.Y + 8), Color.White);

            var cellY = _shapePaletteRect.Y + 32;
            DrawShapePaletteEntry(new Rectangle(_shapePaletteRect.X + 8, cellY, _shapePaletteRect.Width - 16, 44), ShapeType.L);
            cellY += 52;
            DrawShapePaletteEntry(new Rectangle(_shapePaletteRect.X + 8, cellY, _shapePaletteRect.Width - 16, 44), ShapeType.Rect2x2);
        }

        void DrawShapePaletteEntry(Rectangle r, ShapeType t)
        {
            FillRect(r, new Color(255, 255, 255, 8));
            DrawRect(r, Color.White, 1);
            var center = new Point(r.X + r.Width / 2, r.Y + r.Height / 2);
            // tiny preview of shape footprint
            var cells = GetFootprint(t, new Point(0, 0), Direction.Right);
            foreach (var p in cells)
            {
                var pr = new Rectangle(center.X - 16 + p.X * 8, center.Y - 8 + p.Y * 8, 8, 8);
                FillRect(pr, new Color(160, 255, 180, 180));
                DrawRect(pr, new Color(90, 200, 120), 1);
            }
            if (_font != null) _sb.DrawString(_font, t.ToString(), new Vector2(r.X + 8, r.Bottom - 18), Color.White);
        }

        MachineType? PaletteMachineAt(Point mouse)
        {
            var inner = new Rectangle(_machinePaletteRect.X + 8, _machinePaletteRect.Y + 8, _machinePaletteRect.Width - 16, 56);
            if (inner.Contains(mouse)) return MachineType.Arm;
            return null;
        }

        ShapeType? PaletteShapeAt(Point mouse)
        {
            var r1 = new Rectangle(_shapePaletteRect.X + 8, _shapePaletteRect.Y + 32, _shapePaletteRect.Width - 16, 44);
            var r2 = new Rectangle(_shapePaletteRect.X + 8, _shapePaletteRect.Y + 84, _shapePaletteRect.Width - 16, 44);
            if (r1.Contains(mouse)) return ShapeType.L;
            if (r2.Contains(mouse)) return ShapeType.Rect2x2;
            return null;
        }

        IMachine MachineAtCell(Point cell)
        {
            foreach (var m in _machines) if (m.BasePos == cell) return m; return null;
        }

        ShapeSource SourceAtCell(Point cell)
        {
            foreach (var s in _shapeSources) if (s.BasePos == cell) return s; return null;
        }

        ShapeInstance InstanceAtCell(Point cell)
        {
            foreach (var si in _shapeInstances)
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
            foreach (var inst in _shapeInstances)
            {
                foreach (var c in inst.Cells)
                {
                    var r = CellRect(c);
                    FillRect(r, new Color(160, 255, 180, 80));
                    DrawRect(r, new Color(90, 200, 120), 2);
                }
            }

            // Draw sources (pins)
            foreach (var src in _shapeSources)
            {
                var r = CellRect(src.BasePos);
                FillRect(r, new Color(120, 170, 255, 25));
                DrawRect(r, new Color(120, 170, 255), 2);
                if (_font != null) _sb.DrawString(_font, src.Type.ToString(), new Vector2(r.X + 2, r.Y + 1), new Color(200, 220, 255));
            }
        }
    }
}
