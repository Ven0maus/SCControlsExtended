﻿using SadConsole;
using SadConsole.UI.Controls;
using SadConsole.UI.Themes;
using SadRogue.Primitives;
using SCControlsExtended.Themes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SCControlsExtended.Tests")]
namespace SCControlsExtended.Controls
{
    public class Table : CompositeControl
    {
        public Cells Cells { get; }
        public Color DefaultForeground { get; set; }
        public Color DefaultBackground { get; set; }
        public Point DefaultCellSize { get; set; }
        public Cells.Layout.Mode DefaultHoverMode { get; set; }
        public Cells.Layout.Mode DefaultSelectionMode { get; set; }

        private bool _useMouse = true;
        /// <summary>
        /// When <see langword="true"/>, this object will use the mouse; otherwise <see langword="false"/>.
        /// </summary>
        public new bool UseMouse
        {
            get { return _useMouse; }
            set
            {
                _useMouse = value;
                if (!_useMouse)
                {
                    SelectedCell = null;
                    CurrentMouseCell = null;
                    IsDirty = true;
                }
            }
        }

        /// <summary>
        /// Returns the cell the mouse is over, if <see cref="UseMouse"/> is <see langword="true"/>.
        /// </summary>
        public Cell CurrentMouseCell { get; private set; }

        private Cell _selectedCell;
        /// <summary>
        /// Returns the current selected cell
        /// </summary>
        public Cell SelectedCell
        {
            get { return _selectedCell; }
            internal set
            {
                var prev = _selectedCell;
                _selectedCell = value;
                if (prev != _selectedCell)
                {
                    SelectedCellChanged?.Invoke(this, new CellChangedEventArgs(prev, _selectedCell));
                    IsDirty = true;
                }
            }
        }

        /// <summary>
        /// By default, only cells that have been modified in anyway will be rendered on the table control.
        /// Turn this off, if the whole table should draw as many cells as it fits with their default layout.
        /// </summary>
        public bool DrawFakeCells { get; set; } = false;

        /// <summary>
        /// Fires an event when a cell is entered by the mouse.
        /// </summary>
        public event EventHandler<CellEventArgs> OnCellEnter;
        /// <summary>
        /// Fires an event when a cell is exited by the mouse.
        /// </summary>
        public event EventHandler<CellEventArgs> OnCellExit;
        /// <summary>
        /// Fires an event when the selected cell has changed.
        /// </summary>
        public event EventHandler<CellChangedEventArgs> SelectedCellChanged;
        /// <summary>
        /// Fires an event when a cell is left clicked.
        /// </summary>
        public event EventHandler<CellEventArgs> OnCellLeftClick;
        /// <summary>
        /// Fires an event when a cell is right clicked.
        /// </summary>
        public event EventHandler<CellEventArgs> OnCellRightClick;
        /// <summary>
        /// Fires an event when a cell is double clicked.
        /// </summary>
        public event EventHandler<CellEventArgs> OnCellDoubleClick;
        /// <summary>
        /// Called when a fake cells is being drawn, you can use this to modify the cell layout.
        /// </summary>
        public event EventHandler<CellEventArgs> OnDrawFakeCell;

        /// <summary>
        /// The vertical scrollbar, use the SetupScrollBar method with Vertical orientation to initialize it.
        /// </summary>
        public ScrollBar VerticalScrollBar { get; private set; }
        /// <summary>
        /// The horizontal scrollbar, use the SetupScrollBar method with Horizontal orientation to initialize it.
        /// </summary>
        public ScrollBar HorizontalScrollBar { get; private set; }

        /// <summary>
        /// Returns true if the vertical scroll bar is currently visible.
        /// </summary>
        public bool IsVerticalScrollBarVisible
        {
            get => VerticalScrollBar != null && VerticalScrollBar.IsVisible;
            internal set { if (VerticalScrollBar == null) return; VerticalScrollBar.IsVisible = value; }
        }
        /// <summary>
        /// Returns true if the horizontal scroll bar is currently visible.
        /// </summary>
        public bool IsHorizontalScrollBarVisible
        {
            get => HorizontalScrollBar != null && HorizontalScrollBar.IsVisible;
            internal set { if (HorizontalScrollBar == null) return; HorizontalScrollBar.IsVisible = value; }
        }

        /// <summary>
        /// By default the table will automatically scroll to the selected cell if possible.
        /// </summary>
        public bool AutoScrollOnCellSelection { get; set; } = true;

        /// <summary>
        /// The total rows visible in the table.
        /// </summary>
        internal int VisibleRowsTotal { get; set; }
        /// <summary>
        /// The maximum amount of rows that can be shown in the table.
        /// </summary>
        internal int VisibleRowsMax { get; set; }
        /// <summary>
        /// The total columns visible in the table.
        /// </summary>
        internal int VisibleColumnsTotal { get; set; }
        /// <summary>
        /// The maximum amount of columns that can be shown in the table.
        /// </summary>
        internal int VisibleColumnsMax { get; set; }

        private DateTime _leftMouseLastClick = DateTime.Now;
        private Point? _leftMouseLastClickPosition;
        internal bool _checkScrollBarVisibility;

        private int _previousScrollValueVertical, _previousScrollValueHorizontal;
        private void ScrollBar_ValueChanged(object sender, EventArgs e)
        {
            var scrollBar = (ScrollBar)sender;
            var previousScrollValue = scrollBar.Orientation == Orientation.Vertical ? _previousScrollValueVertical : _previousScrollValueHorizontal;
            var increment = previousScrollValue < scrollBar.Value;

            var diff = Math.Abs(scrollBar.Value - previousScrollValue);
            for (int i = 0; i < diff; i++)
            {
                SetScrollAmount(scrollBar.Orientation, increment);
                Cells.AdjustCellPositionsAfterResize();
            }

            if (scrollBar.Orientation == Orientation.Vertical)
                _previousScrollValueVertical = scrollBar.Value;
            else
                _previousScrollValueHorizontal = scrollBar.Value;

            IsDirty = true;
        }

        public Table(int width, int height) : base(width, height)
        {
            Cells = new(this);
            DefaultForeground = Color.White;
            DefaultBackground = Color.TransparentBlack;
            DefaultCellSize = new(1, 1);
        }

        public Table(int width, int height, int cellWidth, int cellHeight = 1)
            : this(width, height)
        {
            if (cellWidth < 1 || cellHeight < 1)
                throw new Exception("Cell width/height cannot be smaller than 1.");

            DefaultCellSize = new(cellWidth, cellHeight);
        }

        public Table(int width, int height, int cellWidth, Color defaultForeground, Color defaultBackground, int cellHeight = 1)
            : this(width, height, cellWidth, cellHeight)
        {
            DefaultForeground = defaultForeground;
            DefaultBackground = defaultBackground;
        }

        /// <summary>
        /// Called when a fake cell is being drawn, this fake cell cannot be added to the table when it is modified.
        /// This method can only be used to modify the cell layout when drawn, and thus will not count as a new cell within the table.
        /// </summary>
        /// <param name="cell"></param>
        internal void DrawFakeCell(Cell cell)
        {
            OnDrawFakeCell?.Invoke(this, new CellEventArgs(cell));
        }

        /// <summary>
        /// Configures the associated <see cref="VerticalScrollBar"/>.
        /// </summary>
        /// <param name="orientation">The orientation of the scrollbar.</param>
        /// <param name="sizeValue">The size of the scrollbar.</param>
        /// <param name="position">The position of the scrollbar.</param>
        public void SetupScrollBar(Orientation orientation, int size, Point position)
        {
            bool scrollBarExists = false;
            int value = 0;
            int max = 0;

            var existingScrollBar = orientation == Orientation.Vertical ? VerticalScrollBar : HorizontalScrollBar;
            if (existingScrollBar != null)
            {
                existingScrollBar.ValueChanged -= ScrollBar_ValueChanged;
                value = existingScrollBar.Value;
                max = existingScrollBar.Maximum;
                RemoveControl(existingScrollBar);
                scrollBarExists = true;
            }

            existingScrollBar = new ScrollBar(orientation, size);

            if (scrollBarExists)
            {
                existingScrollBar.Maximum = max;
                existingScrollBar.Value = value;
            }

            existingScrollBar.ValueChanged += ScrollBar_ValueChanged;
            existingScrollBar.Position = position;
            AddControl(existingScrollBar);

            if (orientation == Orientation.Vertical)
                VerticalScrollBar = existingScrollBar;
            else
                HorizontalScrollBar = existingScrollBar;

            _checkScrollBarVisibility = true;

            OnThemeChanged();
            DetermineState();
        }

        internal int GetMaxRowsBasedOnRowSizes()
        {
            return !Cells.Any() ? 0 : Cells
                .GroupBy(a => a.Row)
                .Select(a => Cells.GetSizeOrDefault(a.Key, Cells.Layout.LayoutType.Row))
                .Sum();
        }

        internal int GetMaxColumnsBasedOnColumnSizes()
        {
            return !Cells.Any() ? 0 : Cells
                .GroupBy(a => a.Column)
                .Select(a => Cells.GetSizeOrDefault(a.Key, Cells.Layout.LayoutType.Column))
                .Sum();
        }

        /// <summary>
        /// Scrolls the list to the item currently selected.
        /// </summary>
        public void ScrollToSelectedItem()
        {
            if (!AutoScrollOnCellSelection) return;
            if (IsVerticalScrollBarVisible || IsHorizontalScrollBarVisible)
            {
                var scrollBars = new[] { VerticalScrollBar, HorizontalScrollBar };
                foreach (var scrollBar in scrollBars)
                {
                    if (scrollBar == null) continue;

                    var orientation = scrollBar.Orientation;
                    int selectedIndex = SelectedCell != null ? (orientation == Orientation.Vertical ? SelectedCell.Row : SelectedCell.Column) : 0;

                    var isRowType = orientation == Orientation.Vertical;
                    var indexes = Cells
                        .GroupBy(a => isRowType ? a.Row : a.Column)
                        .Select(a => a.Key)
                        .OrderBy(a => a);
                    int totalIndexSize = 0;
                    foreach (var index in indexes)
                    {
                        var cellSize = Cells.GetSizeOrDefault(index, isRowType ? 
                            Cells.Layout.LayoutType.Row : Cells.Layout.LayoutType.Column);
                        totalIndexSize += cellSize;

                        if (index > selectedIndex)
                            break;
                    }

                    var maxIndexSize = orientation == Orientation.Vertical ? GetMaxRowsBasedOnRowSizes() : GetMaxColumnsBasedOnColumnSizes();
                    var max = orientation == Orientation.Vertical ? VisibleRowsMax : VisibleColumnsMax;
                    var total = orientation == Orientation.Vertical ? VisibleRowsTotal : VisibleColumnsTotal;
                    var defaultIndexSize = orientation == Orientation.Vertical ? DefaultCellSize.Y : DefaultCellSize.X;

                    if (totalIndexSize < max)
                        scrollBar.Value = 0;
                    else if (totalIndexSize > maxIndexSize - total)
                        scrollBar.Value = scrollBar.Maximum;
                    else
                        scrollBar.Value = (totalIndexSize - total) / defaultIndexSize;
                }
            }
        }

        /// <summary>
        /// When a cell is resized after the bar has been scrolled, it must be updated with the new values for the rendering.
        /// </summary>
        internal void SyncScrollAmountOnResize()
        {
            if ((!IsVerticalScrollBarVisible && !IsHorizontalScrollBarVisible) || 
                (StartRenderXPos == 0 && StartRenderYPos == 0)) 
                return;

            StartRenderYPos = 0;
            StartRenderXPos = 0;

            Cells.AdjustCellPositionsAfterResize();

            var amountVertical = IsVerticalScrollBarVisible ? VerticalScrollBar.Value : 0;
            var amountHorizontal = IsHorizontalScrollBarVisible ? HorizontalScrollBar.Value : 0;
            var max = amountVertical > amountHorizontal ? amountVertical : amountHorizontal;

            for (int i = 0; i < max; i++)
            {
                if (i < amountVertical)
                    SetScrollAmount(Orientation.Vertical, true);
                if (i < amountHorizontal)
                    SetScrollAmount(Orientation.Horizontal, true);
                Cells.AdjustCellPositionsAfterResize();
            }
        }

        /// <summary>
        /// The row the rendering should start at
        /// </summary>
        internal int StartRenderYPos { get; private set; }
        /// <summary>
        /// The column the rendering should start at
        /// </summary>
        internal int StartRenderXPos { get; private set; }
        private void SetScrollAmount(Orientation orientation, bool increment)
        {
            var scrollPos = GetNextScrollPos(increment, orientation);

            if (orientation == Orientation.Vertical)
                StartRenderYPos += scrollPos;
            else
                StartRenderXPos += scrollPos;

            StartRenderYPos = StartRenderYPos < 0 ? 0 : StartRenderYPos;
            StartRenderXPos = StartRenderXPos < 0 ? 0 : StartRenderXPos;
        }

        internal int GetNextScrollPos(bool increment, Orientation orientation)
        {
            var type = orientation == Orientation.Vertical ?
                Cells.Layout.LayoutType.Row : Cells.Layout.LayoutType.Column;
            var isRowType = type == Cells.Layout.LayoutType.Row;
            var cellGroups = Cells.GroupBy(a => isRowType ? a.Row : a.Column);
            var orderedCells = increment ? cellGroups.OrderBy(a => a.Key) :
                cellGroups.OrderByDescending(a => a.Key);

            foreach (var group in orderedCells)
            {
                foreach (var cell in group)
                {
                    var partialOverlap = false;
                    var indexSizeCell = isRowType ? cell.Position.Y : cell.Position.X;
                    if (!increment)
                    {
                        // Check if cell position is the last cell on screen
                        if (indexSizeCell >= (isRowType ? Height : Width))
                            break;
                    }
                    else
                    {
                        // Check if cell position is the next off-screen
                        // >= because it assumes the cell starts at Height, and thats off screen
                        var isPositionOfScreen = isRowType ? indexSizeCell >= Height : indexSizeCell >= Width;
                        if (!isPositionOfScreen)
                        {
                            // Here it is only > because if the real cell pos is 20 its the ending, so where the next cell starts
                            // which means its not off screen
                            var realCellPosition = isRowType ? (cell.Position.Y + cell.Height) : (cell.Position.X + cell.Width);
                            if (realCellPosition > (isRowType ? Height : Width))
                            {
                                partialOverlap = true;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    // Get size of current cell
                    var layoutDict = isRowType ? Cells.RowLayout : Cells.ColumnLayout;
                    var defaultSize = isRowType ? DefaultCellSize.Y : DefaultCellSize.X;
                    int offScreenIndex = isRowType ? cell.Row : cell.Column;
                    var cellSize = layoutDict.TryGetValue(offScreenIndex, out var layout) ?
                        layout.Size : defaultSize;

                    // Calculate the overlap amount
                    if (partialOverlap)
                    {
                        var overlapAmount = indexSizeCell + cellSize - (isRowType ? Height : Width);
                        cellSize -= overlapAmount;
                    }

                    return increment ? cellSize : -cellSize;
                }
            }

            var defaultCellSize = (isRowType ? DefaultCellSize.Y : DefaultCellSize.X);
            return increment ? defaultCellSize : -defaultCellSize;
        }

        /// <summary>
        /// Sets the scrollbar's theme to the current theme's <see cref="ListBoxTheme.ScrollBarTheme"/>.
        /// </summary>
        protected override void OnThemeChanged()
        {
            if (VerticalScrollBar == null && HorizontalScrollBar == null) return;

            if (Theme is TableTheme theme)
            {
                if (VerticalScrollBar != null)
                    VerticalScrollBar.Theme = theme.ScrollBarTheme;
                if (HorizontalScrollBar != null)
                    HorizontalScrollBar.Theme = theme.ScrollBarTheme;
            }
            else
            {
                if (VerticalScrollBar != null)
                    VerticalScrollBar.Theme = null;
                if (HorizontalScrollBar != null)
                    HorizontalScrollBar.Theme = null;
            }
        }

        protected override void OnMouseIn(ControlMouseState state)
        {
            base.OnMouseIn(state);

            // Handle mouse hovering over cell
            var mousePosCellIndex = GetCellIndexByMousePosition(state.MousePosition);
            Point? currentPosition = CurrentMouseCell == null ? null : (CurrentMouseCell.Column, CurrentMouseCell.Row);

            if (!Equals(mousePosCellIndex, currentPosition))
            {
                if (mousePosCellIndex != null)
                {
                    var cell = Cells.GetIfExists(mousePosCellIndex.Value.Y, mousePosCellIndex.Value.X);
                    if (cell == null && DrawFakeCells)
                    {
                        cell = new Cell(mousePosCellIndex.Value.Y, mousePosCellIndex.Value.X, this, string.Empty)
                        {
                            Position = Cells.GetCellPosition(mousePosCellIndex.Value.Y, mousePosCellIndex.Value.X, out _, out _,
                                IsVerticalScrollBarVisible ? StartRenderYPos : 0, IsHorizontalScrollBarVisible ? StartRenderXPos : 0)
                        };
                    }
                    if (CurrentMouseCell != cell)
                        IsDirty = true;
                    CurrentMouseCell = cell;

                    if (CurrentMouseCell != null && (!CurrentMouseCell.IsSettingsInitialized || CurrentMouseCell.Settings.Interactable))
                        OnCellEnter?.Invoke(this, new CellEventArgs(CurrentMouseCell));
                }
                else
                {
                    if (CurrentMouseCell != null && (!CurrentMouseCell.IsSettingsInitialized || CurrentMouseCell.Settings.Interactable))
                        OnCellExit?.Invoke(this, new CellEventArgs(CurrentMouseCell));
                }
            }

            if (state.OriginalMouseState.Mouse.ScrollWheelValueChange != 0)
            {
                ScrollBar scrollBar = null;
                if (IsVerticalScrollBarVisible && !IsHorizontalScrollBarVisible)
                    scrollBar = VerticalScrollBar;
                else if (!IsVerticalScrollBarVisible && IsHorizontalScrollBarVisible)
                    scrollBar = HorizontalScrollBar;
                // If both scroll bars are not null, we only wanna scroll on the vertical scrollbar with the mousewheel
                else if (IsVerticalScrollBarVisible && IsHorizontalScrollBarVisible)
                    scrollBar = VerticalScrollBar;

                if (scrollBar != null)
                {
                    if (state.OriginalMouseState.Mouse.ScrollWheelValueChange < 0)
                        scrollBar.Value -= 1;
                    else
                        scrollBar.Value += 1;
                }
            }
        }

        protected override void OnLeftMouseClicked(ControlMouseState state)
        {
            base.OnLeftMouseClicked(state);

            if (CurrentMouseCell != null)
            {
                if (SelectedCell != CurrentMouseCell && (!CurrentMouseCell.IsSettingsInitialized || (CurrentMouseCell.Settings.Interactable && 
                    CurrentMouseCell.Settings.IsVisible && CurrentMouseCell.Settings.Selectable)))
                {
                    SelectedCell = CurrentMouseCell;
                    ScrollToSelectedItem();
                }
                else
                {
                    // Unselect after clicking the selected cell again
                    SelectedCell = null;
                }

                if (!CurrentMouseCell.IsSettingsInitialized || (CurrentMouseCell.Settings.Interactable && CurrentMouseCell.Settings.IsVisible))
                    OnCellLeftClick?.Invoke(this, new CellEventArgs(CurrentMouseCell));

                DateTime click = DateTime.Now;
                bool doubleClicked = (click - _leftMouseLastClick).TotalSeconds <= 0.25 && state.MousePosition == _leftMouseLastClickPosition;
                _leftMouseLastClick = click;
                _leftMouseLastClickPosition = state.MousePosition;

                if (doubleClicked)
                {
                    _leftMouseLastClick = DateTime.MinValue;
                    _leftMouseLastClickPosition = null;
                    if (!CurrentMouseCell.IsSettingsInitialized || (CurrentMouseCell.Settings.Interactable && CurrentMouseCell.Settings.IsVisible))
                        OnCellDoubleClick?.Invoke(this, new CellEventArgs(CurrentMouseCell));
                }
            }
            else
            {
                SelectedCell = null;
            }
        }

        protected override void OnRightMouseClicked(ControlMouseState state)
        {
            base.OnRightMouseClicked(state);

            if (CurrentMouseCell != null && (!CurrentMouseCell.IsSettingsInitialized || (CurrentMouseCell.Settings.Interactable && CurrentMouseCell.Settings.IsVisible)))
            {
                OnCellRightClick?.Invoke(this, new CellEventArgs(CurrentMouseCell));
            }
        }

        protected override void OnMouseExit(ControlMouseState state)
        {
            base.OnMouseExit(state);

            if (CurrentMouseCell != null)
            {
                if (!CurrentMouseCell.IsSettingsInitialized || (CurrentMouseCell.Settings.Interactable && CurrentMouseCell.Settings.IsVisible))
                    OnCellExit?.Invoke(this, new CellEventArgs(CurrentMouseCell));
                CurrentMouseCell = null;
            }
        }

        private Point? GetCellIndexByMousePosition(Point mousePosition)
        {
            for (int col = 0; col < Width; col++)
            {
                for (int row = 0; row < Height; row++)
                {
                    var colValue = col + (IsHorizontalScrollBarVisible ? StartRenderXPos : col);
                    var rowValue = row + (IsVerticalScrollBarVisible ? StartRenderYPos : row);
                    var position = Cells.GetCellPosition(rowValue, colValue, out int rowSize, out int columnSize, 
                        IsVerticalScrollBarVisible ? StartRenderYPos : 0, IsHorizontalScrollBarVisible ? StartRenderXPos : 0);
                    if (IsMouseWithinCell(mousePosition, position.Y, position.X, columnSize, rowSize))
                        return (colValue, rowValue);
                }
            }
            return null;
        }

        private static bool IsMouseWithinCell(Point mousePosition, int row, int column, int width, int height)
        {
            var maxX = column + width;
            var maxY = row + height;
            return mousePosition.X >= column && mousePosition.X < maxX &&
                mousePosition.Y >= row && mousePosition.Y < maxY;
        }

        #region Event Args
        public class CellEventArgs : EventArgs
        {
            public readonly Cell Cell;

            internal CellEventArgs(Cell cell)
            {
                Cell = cell;
            }
        }

        public sealed class CellChangedEventArgs : CellEventArgs
        {
            public readonly Cell PreviousCell;

            internal CellChangedEventArgs(Cell previousCell, Cell cell)
                : base(cell)
            {
                PreviousCell = previousCell;
            }
        }
        #endregion

        public sealed class Cell : IEquatable<Cell>
        {
            internal Point Position { get; set; }
            public int Row { get; }
            public int Column { get; }
            public int Height { get { return Table.Cells.GetSizeOrDefault(Row, Cells.Layout.LayoutType.Row); } }
            public int Width { get { return Table.Cells.GetSizeOrDefault(Column, Cells.Layout.LayoutType.Column); } }

            private Color _foreground;
            public Color Foreground
            {
                get { return _foreground; }
                set { SetFieldValue(this, Foreground, ref _foreground, value, false); }
            }

            private Color _background;
            public Color Background
            {
                get { return _background; }
                set { SetFieldValue(this, Background, ref _background, value, false); }
            }

            private string _text;
            public string Text
            {
                get { return _text; }
                set { SetFieldValue(this, Text, ref _text, value, false); }
            }

            private Options _settings;
            public Options Settings
            {
                get { return _settings ??= new Options(this); }
                internal set
                {
                    if (value == null) return;
                    if (_settings != value)
                    {
                        (_settings ??= new Options(this)).CopyFrom(value);
                        AddToTableIfNotExists();
                        Table.IsDirty = true;
                    }
                }
            }

            private readonly bool _addToTableIfModified;
            internal readonly Table Table;

            internal Cell(int row, int col, Table table, string text, bool addToTableIfModified = true)
            {
                Table = table;
                _text = text;
                _foreground = table.DefaultForeground;
                _background = table.DefaultBackground;
                _addToTableIfModified = addToTableIfModified;

                Row = row;
                Column = col;

                // Set cell layout options
                table.Cells.ColumnLayout.TryGetValue(col, out var columnLayout);
                table.Cells.RowLayout.TryGetValue(row, out var rowLayout);
                var layoutOptions = new[] { columnLayout, rowLayout };
                foreach (var option in layoutOptions)
                {
                    if (option == null) continue;
                    if (option.Foreground != null)
                        _foreground = option.Foreground.Value;
                    if (option.Background != null)
                        _background = option.Background.Value;
                    if (option.HasCustomSettings)
                        (_settings ??= new Options(this)).CopyFrom(option.Settings);
                }
            }

            internal void AddToTableIfNotExists()
            {
                if (_addToTableIfModified && Table.Cells.GetIfExists(Row, Column) == null)
                    Table.Cells[Row, Column] = this;
            }

            internal bool IsSettingsInitialized
            {
                get { return _settings != null; }
            }

            public static bool operator ==(Cell a, Cell b)
            {
                var refEqualNullA = a is null;
                var refEqualNullB = b is null;
                if (refEqualNullA && refEqualNullB) return true;
                if (refEqualNullA || refEqualNullB) return false;
                return a.Equals(b);
            }
            public static bool operator !=(Cell a, Cell b)
            {
                return !(a == b);
            }

            public bool Equals(Cell cell)
            {
                if (cell == null) return false;
                return cell.Column == Column && cell.Row == Row;
            }

            public override bool Equals(object obj)
            {
                if (obj is not Cell cell) return false;
                return Equals(cell);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Column, Row);
            }

            /// <summary>
            /// Copies the appearance of the cell passed to this method, onto the this cell.
            /// </summary>
            /// <param name="cell"></param>
            public void CopyAppearanceFrom(Cell cell)
            {
                Text = cell.Text;
                Foreground = cell.Foreground;
                Background = cell.Background;
                if (_settings != cell._settings)
                {
                    if (cell._settings == null)
                        _settings = null;
                    else
                        Settings.CopyFrom(cell.Settings);
                }
            }

            /// <summary>
            /// Helper to set the underlying field value with some checks.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="cell"></param>
            /// <param name="previousValue"></param>
            /// <param name="field"></param>
            /// <param name="newValue"></param>
            /// <param name="usedForLayout"></param>
            internal static void SetFieldValue<T>(Cell cell, T previousValue, ref T field, T newValue, bool usedForLayout)
            {
                if (!EqualityComparer<T>.Default.Equals(previousValue, newValue))
                {
                    field = newValue;
                    if (usedForLayout) return;
                    cell.AddToTableIfNotExists();
                    cell.Table.IsDirty = true;
                }
            }

            public class Options : IEquatable<Options>
            {
                private HorizontalAlign _horizontalAlignment;
                public HorizontalAlign HorizontalAlignment
                {
                    get { return _horizontalAlignment; }
                    set { SetFieldValue(_cell, HorizontalAlignment, ref _horizontalAlignment, value, _usedForLayout); }
                }

                private VerticalAlign _verticalAlignment;
                public VerticalAlign VerticalAlignment
                {
                    get { return _verticalAlignment; }
                    set { SetFieldValue(_cell, VerticalAlignment, ref _verticalAlignment, value, _usedForLayout); }
                }

                private bool _useFakeLayout = false;
                public bool UseFakeLayout
                {
                    get { return _useFakeLayout; }
                    set { SetFieldValue(_cell, UseFakeLayout, ref _useFakeLayout, value, _usedForLayout); }
                }

                private int? _maxCharactersPerLine;
                public int? MaxCharactersPerLine
                {
                    get { return _maxCharactersPerLine; }
                    set { SetFieldValue(_cell, MaxCharactersPerLine, ref _maxCharactersPerLine, value, _usedForLayout); }
                }

                private bool _interactable = true;
                public bool Interactable
                {
                    get { return _interactable; }
                    set { SetFieldValue(_cell, Interactable, ref _interactable, value, _usedForLayout); }
                }

                private bool _selectable = true;
                public bool Selectable
                {
                    get { return _selectable; }
                    set { SetFieldValue(_cell, Selectable, ref _selectable, value, _usedForLayout); }
                }

                private bool _isVisible = true;
                public bool IsVisible
                {
                    get { return _isVisible; }
                    set { SetFieldValue(_cell, IsVisible, ref _isVisible, value, _usedForLayout); }
                }

                private Cells.Layout.Mode _selectionMode;
                public Cells.Layout.Mode SelectionMode
                {
                    get { return _selectionMode; }
                    set { SetFieldValue(_cell, SelectionMode, ref _selectionMode, value, _usedForLayout); }
                }

                private Cells.Layout.Mode _hoverMode;
                public Cells.Layout.Mode HoverMode
                {
                    get { return _hoverMode; }
                    set { SetFieldValue(_cell, HoverMode, ref _hoverMode, value, _usedForLayout); }
                }

                private readonly bool _usedForLayout;
                private readonly Cell _cell;

                internal Options(Cell cell)
                {
                    _usedForLayout = false;
                    _cell = cell;
                    _hoverMode = _cell.Table.DefaultHoverMode;
                    _selectionMode = _cell.Table.DefaultSelectionMode;
                }

                public Options(Table table)
                {
                    _usedForLayout = true;
                    _hoverMode = table.DefaultHoverMode;
                    _selectionMode = table.DefaultSelectionMode;
                }

                public enum HorizontalAlign
                {
                    Left = 0,
                    Center,
                    Right
                }

                public enum VerticalAlign
                {
                    Top = 0,
                    Center,
                    Bottom
                }

                public static bool operator ==(Options a, Options b)
                {
                    var refEqualNullA = a is null;
                    var refEqualNullB = b is null;
                    if (refEqualNullA && refEqualNullB) return true;
                    if (refEqualNullA || refEqualNullB) return false;
                    return a.Equals(b);
                }
                public static bool operator !=(Options a, Options b)
                {
                    return !(a == b);
                }

                public bool Equals(Options other)
                {
                    if (other == null) return false;
                    return other.HorizontalAlignment == HorizontalAlignment &&
                        other.VerticalAlignment == VerticalAlignment &&
                        other.MaxCharactersPerLine == MaxCharactersPerLine &&
                        other.IsVisible == IsVisible &&
                        other.Selectable == Selectable &&
                        other.SelectionMode == SelectionMode &&
                        other.HoverMode == HoverMode &&
                        other.Interactable == Interactable &&
                        other.UseFakeLayout == UseFakeLayout;
                }

                public override bool Equals(object obj)
                {
                    if (obj is not Options to) return false;
                    return Equals(to);
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(new object[]
                    {
                        HorizontalAlignment,
                        VerticalAlignment,
                        MaxCharactersPerLine,
                        IsVisible,
                        Selectable,
                        SelectionMode,
                        HoverMode,
                        Interactable,
                        UseFakeLayout
                    });
                }

                internal void CopyFrom(Options settings)
                {
                    HorizontalAlignment = settings.HorizontalAlignment;
                    VerticalAlignment = settings.VerticalAlignment;
                    MaxCharactersPerLine = settings.MaxCharactersPerLine;
                    IsVisible = settings.IsVisible;
                    Selectable = settings.Selectable;
                    SelectionMode = settings.SelectionMode;
                    HoverMode = settings.HoverMode;
                    Interactable = settings.Interactable;
                    UseFakeLayout = settings.UseFakeLayout;
                }
            }
        }
    }

    public sealed class Cells : IEnumerable<Table.Cell>
    {
        private readonly Table _table;
        private readonly Dictionary<Point, Table.Cell> _cells = new();
        internal readonly Dictionary<int, Layout> ColumnLayout = new();
        internal readonly Dictionary<int, Layout> RowLayout = new();

        public Table.Cell this[int row, int col]
        {
            get { return GetOrCreateCell(row, col); }
            internal set { SetCell(row, col, value); }
        }

        /// <summary>
        /// The rows the table currently holds.
        /// </summary>
        public int TotalRows { get { return _cells.Count == 0 ? 0 : _cells.Values.Max(a => a.Row) + 1; } }

        /// <summary>
        /// The columns the table currently holds.
        /// </summary>
        public int TotalColumns { get { return _cells.Count == 0 ? 0 : _cells.Values.Max(a => a.Column) + 1; } }

        /// <summary>
        /// The amount of cells currently in the table.
        /// </summary>
        public int Count { get { return _cells.Count; } }

        internal Cells(Table table)
        {
            _table = table;
        }

        #region Public Methods
        /// <summary>
        /// Get the layout for the given column
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public Layout Column(int column)
        {
            ColumnLayout.TryGetValue(column, out Layout layout);
            layout ??= ColumnLayout[column] = new Layout(_table, Layout.LayoutType.Column);
            return layout;
        }

        /// <summary>
        /// Get the layout for the given row
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public Layout Row(int row)
        {
            RowLayout.TryGetValue(row, out Layout layout);
            layout ??= RowLayout[row] = new Layout(_table, Layout.LayoutType.Row);
            return layout;
        }

        /// <summary>
        /// Gets the cell at the given row and col
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public Table.Cell GetCell(int row, int col)
        {
            return this[row, col];
        }

        /// <summary>
        /// Sets the specified cell as the selected cell if it exists.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        public void Select(int row, int column)
        {
            // Set existing cell, or a fake one if it does not yet exists, but modifying this fake cell with add it to the table
            _table.SelectedCell = GetIfExists(row, column) ?? new Table.Cell(row, column, _table, string.Empty)
            {
                Position = GetCellPosition(row, column, out _, out _, 
                    _table.IsVerticalScrollBarVisible ? _table.StartRenderYPos : 0, _table.IsHorizontalScrollBarVisible ? _table.StartRenderXPos : 0)
            };
        }

        /// <summary>
        /// Deselects the current selected cell.
        /// </summary>
        public void Deselect()
        {
            _table.SelectedCell = null;
        }

        /// <summary>
        /// Removes a cell from the table.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        public void Remove(int row, int column)
        {
            var prev = _cells.Count;
            _cells.Remove((row, column));
            if (prev != _cells.Count)
            {
                AdjustCellPositionsAfterResize();
                _table.SyncScrollAmountOnResize();
                _table.IsDirty = true;
            }
        }

        /// <summary>
        /// Resets all the cells data
        /// </summary>
        /// <param name="clearLayoutOptions"></param>
        public void Clear(bool clearLayoutOptions = true)
        {
            if (clearLayoutOptions)
            {
                RowLayout.Clear();
                ColumnLayout.Clear();
            }
            _cells.Clear();
            _table.IsDirty = true;
        }
        #endregion

        #region Internal Methods
        /// <summary>
        /// Get's the cell position on the control based on the row and column
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        internal Point GetCellPosition(int row, int col, out int rowSize, out int columnSize, int verticalScrollBarValue = 0, int horizontalScrollbarValue = 0)
        {
            int columnIndex = GetControlIndex(col, horizontalScrollbarValue, Layout.LayoutType.Column, out columnSize);
            int rowIndex = GetControlIndex(row, verticalScrollBarValue, Layout.LayoutType.Row, out rowSize);
            return new Point(columnIndex, rowIndex);
        }

        /// <summary>
        /// Get the size of the column or row or the default if no layout exists, without allocating a new layout object.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        internal int GetSizeOrDefault(int index, Layout.LayoutType type)
        {
            return type switch
            {
                Layout.LayoutType.Column => ColumnLayout.TryGetValue(index, out Layout layout) ? layout.Size : _table.DefaultCellSize.X,
                Layout.LayoutType.Row => RowLayout.TryGetValue(index, out Layout layout) ? layout.Size : _table.DefaultCellSize.Y,
                _ => throw new NotSupportedException("Invalid layout type."),
            };
        }

        internal Table.Cell GetIfExists(int row, int col)
        {
            if (_cells.TryGetValue((row, col), out Table.Cell cell))
                return cell;
            return null;
        }

        private Table.Cell GetOrCreateCell(int row, int col)
        {
            if (!_cells.TryGetValue((row, col), out Table.Cell cell))
            {
                cell = new Table.Cell(row, col, _table, string.Empty)
                {
                    Position = GetCellPosition(row, col, out _, out _, 
                        _table.IsVerticalScrollBarVisible ? _table.StartRenderYPos : 0, _table.IsHorizontalScrollBarVisible ? _table.StartRenderXPos : 0)
                };

                _cells[(row, col)] = cell;
                _table._checkScrollBarVisibility = true;
            }
            return cell;
        }

        private int GetControlIndex(int index, int startPos, Layout.LayoutType type, out int indexSize)
        {
            // Matches the right cell we should start at, but it could be we need to start somewhere within this cell.
            int startIndex = GetIndexAtCellPosition(startPos, type, out int indexPos);
            int controlIndex = 0;
            if (indexPos < startPos)
            {
                controlIndex = indexPos - startPos;
            }

            indexSize = type == Layout.LayoutType.Column ?
                (ColumnLayout.TryGetValue(startIndex, out Layout layout) ? layout.Size : _table.DefaultCellSize.X) :
                (RowLayout.TryGetValue(startIndex, out layout) ? layout.Size : _table.DefaultCellSize.Y);

            while (startIndex < index)
            {
                controlIndex += indexSize;
                startIndex++;

                indexSize = type == Layout.LayoutType.Column ?
                    (ColumnLayout.TryGetValue(startIndex, out layout) ? layout.Size : _table.DefaultCellSize.X) :
                    (RowLayout.TryGetValue(startIndex, out layout) ? layout.Size : _table.DefaultCellSize.Y);
            }
            return controlIndex;
        }

        internal int GetIndexAtCellPosition(int pos, Layout.LayoutType type, out int indexPos)
        {
            var total = type == Layout.LayoutType.Row ? _table.Cells.TotalRows : _table.Cells.TotalColumns;
            var layoutDict = type == Layout.LayoutType.Row ? RowLayout : ColumnLayout;
            var defaultSize = type == Layout.LayoutType.Row ? _table.DefaultCellSize.Y : _table.DefaultCellSize.X;
            int totalSize = 0;
            for (int i=0; i < total; i++)
            {
                var indexSize = layoutDict.TryGetValue(i, out Layout layout) ? layout.Size : defaultSize;
                totalSize += indexSize;
                if (pos < totalSize)
                {
                    indexPos = totalSize - indexSize;
                    return i;
                }
            }
            indexPos = 0;
            return 0;
        }

        private void SetCell(int row, int col, Table.Cell cell)
        {
            if (cell == null)
            {
                if (_cells.Remove((row, col)))
                {
                    _table._checkScrollBarVisibility = true;
                    _table.IsDirty = true;
                }
                return;
            }

            if (_cells.TryGetValue((row, col), out Table.Cell oldCell))
            {
                if (!oldCell.Foreground.Equals(cell.Foreground) ||
                    !oldCell.Background.Equals(cell.Background) ||
                    !oldCell.Text.Equals(cell.Text) ||
                    !oldCell.Settings.Equals(cell.Settings))
                {
                    _cells[(row, col)] = cell;
                    _table._checkScrollBarVisibility = true;
                    _table.IsDirty = true;
                }
            }
            else
            {
                _cells[(row, col)] = cell;
                _table._checkScrollBarVisibility = true;
                _table.IsDirty = true;
            }
        }

        internal void AdjustCellPositionsAfterResize()
        {
            foreach (var cell in _cells)
                cell.Value.Position = GetCellPosition(cell.Value.Row, cell.Value.Column, out _, out _,
                    _table.IsVerticalScrollBarVisible ? _table.StartRenderYPos : 0, _table.IsHorizontalScrollBarVisible ? _table.StartRenderXPos : 0);
            _table._checkScrollBarVisibility = true;
            _table.IsDirty = true;
        }

        public IEnumerator<Table.Cell> GetEnumerator()
        {
            return _cells.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        public class Layout
        {
            private int _size;
            public int Size
            {
                get { return _size; }
                set
                {
                    if (_size != value)
                    {
                        _size = value;
                        _table.Cells.AdjustCellPositionsAfterResize();
                        _table.SyncScrollAmountOnResize();
                        _table.IsDirty = true;
                    }
                }
            }

            public Color? Foreground { get; set; }
            public Color? Background { get; set; }

            private Table.Cell.Options _settings;
            public Table.Cell.Options Settings
            {
                get { return _settings ??= new Table.Cell.Options(_table); }
                set
                {
                    if (value == null) return;
                    (_settings ??= new Table.Cell.Options(_table)).CopyFrom(value);
                }
            }

            /// <summary>
            /// True if the Settings property has been accessed before.
            /// </summary>
            internal bool HasCustomSettings { get { return _settings != null; } }

            private readonly Table _table;

            internal Layout(Table table, LayoutType type)
            {
                _table = table;
                Size = type == LayoutType.Column ? table.DefaultCellSize.X : table.DefaultCellSize.Y;
            }

            /// <summary>
            /// Set a default layout to be used for each new cell
            /// </summary>
            /// <param name="width"></param>
            /// <param name="height"></param>
            /// <param name="foreground"></param>
            /// <param name="background"></param>
            public void SetLayout(int? size = null, Color? foreground = null, Color? background = null, Table.Cell.Options settings = null)
            {
                var prevSize = _size;
                SetLayoutInternal(size, foreground, background, settings);
                if (prevSize != _size)
                {
                    _table.Cells.AdjustCellPositionsAfterResize();
                    _table.SyncScrollAmountOnResize();
                    _table.IsDirty = true;
                }
            }

            /// <summary>
            /// Sets the layout without adjusting cells or setting the table dirty
            /// </summary>
            /// <param name="size"></param>
            /// <param name="foreground"></param>
            /// <param name="background"></param>
            /// <param name="settings"></param>
            internal void SetLayoutInternal(int? size = null, Color? foreground = null, Color? background = null, Table.Cell.Options settings = null)
            {
                if (size != null) 
                    _size = size.Value;
                if (foreground != null)
                    Foreground = foreground;
                if (background != null)
                    Background = background;
                if (settings != null)
                    Settings = settings;
            }

            internal enum LayoutType
            {
                Column,
                Row
            }

            public enum Mode
            {
                Single = 0,
                None,
                EntireRow,
                EntireColumn
            }

            public class RangeEnumerable : IEnumerable<Layout>
            {
                private readonly IEnumerable<Layout> _layouts;

                internal RangeEnumerable(IEnumerable<Layout> layouts)
                {
                    _layouts = layouts;
                }

                public void SetLayout(int? size = null, Color? foreground = null, Color? background = null, Table.Cell.Options settings = null)
                {
                    foreach (var layout in _layouts)
                        layout.SetLayout(size, foreground, background, settings);
                }

                public IEnumerator<Layout> GetEnumerator()
                {
                    return _layouts.GetEnumerator();
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }
            }
        }
    }
}
