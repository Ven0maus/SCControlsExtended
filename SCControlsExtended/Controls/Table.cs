using SadConsole;
using SadConsole.UI.Controls;
using SadConsole.UI.Themes;
using SadRogue.Primitives;
using SCControlsExtended.Themes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
        /// Returns the cell the mouse is over, if the property <see cref="IsMouseEnabled"/> is true.
        /// </summary>
        public Cell CurrentMouseCell { get; private set; }
        /// <summary>
        /// Returns the current selected cell
        /// </summary>
        public Cell SelectedCell { get; private set; }

        /// <summary>
        /// By default, only cells that have been indexed (eg. accessing table[0, 0]) will be rendered on the table control.
        /// Turn this off, if the whole table should draw as many cells  as it fits, even with no data.
        /// </summary>
        public bool DrawOnlyIndexedCells { get; set; } = true;

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

        internal ScrollBar ScrollBar { get; private set; }

        internal bool IsScrollBarVisible
        {
            get => ScrollBar != null && ScrollBar.IsVisible;
            set { if (ScrollBar == null) return; ScrollBar.IsVisible = value; }
        }

        /// <summary>
        /// The total rows visible in the table.
        /// </summary>
        internal int VisibleIndexesTotal { get; set; }

        /// <summary>
        /// The maximum amount of rows that can be shown in the table.
        /// </summary>
        internal int VisibleIndexesMax { get; set; }

        private DateTime _leftMouseLastClick = DateTime.Now;
        private Point? _leftMouseLastClickPosition;
        internal bool _checkScrollBarVisibility;

        private void _scrollbar_ValueChanged(object sender, EventArgs e)
        {
            Cells.AdjustCellsAfterResize();
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
        /// Configures the associated <see cref="ScrollBar"/>.
        /// </summary>
        /// <param name="orientation">The orientation of the scrollbar.</param>
        /// <param name="sizeValue">The size of the scrollbar.</param>
        /// <param name="position">The position of the scrollbar.</param>
        public void SetupScrollBar(Orientation orientation, int size, Point position)
        {
            bool scrollBarExists = false;
            int value = 0;
            int max = 0;

            if (ScrollBar != null)
            {
                ScrollBar.ValueChanged -= _scrollbar_ValueChanged;
                value = ScrollBar.Value;
                max = ScrollBar.Maximum;
                RemoveControl(ScrollBar);
                scrollBarExists = true;
            }

            ScrollBar = new ScrollBar(orientation, size);

            if (scrollBarExists)
            {
                ScrollBar.Maximum = max;
                ScrollBar.Value = value;
            }

            ScrollBar.ValueChanged += _scrollbar_ValueChanged;
            ScrollBar.Position = position;
            _checkScrollBarVisibility = true;
            AddControl(ScrollBar);

            OnThemeChanged();
            DetermineState();
        }

        /// <summary>
        /// Scrolls the list to the item currently selected.
        /// </summary>
        public void ScrollToSelectedItem()
        {
            // TODO: Fix
            if (IsScrollBarVisible)
            {
                int selectedRow = SelectedCell != null ? SelectedCell.Row * Cells.GetSizeOrDefault(SelectedCell.Row, Cells.Layout.LayoutType.Row) : 0;
                if (selectedRow < VisibleIndexesMax)
                    ScrollBar.Value = 0;
                else if (selectedRow > Cells.MaxRows - VisibleIndexesTotal)
                    ScrollBar.Value = ScrollBar.Maximum;
                else
                    ScrollBar.Value = selectedRow - VisibleIndexesTotal;
            }
        }

        /// <summary>
        /// Sets the scrollbar's theme to the current theme's <see cref="ListBoxTheme.ScrollBarTheme"/>.
        /// </summary>
        protected override void OnThemeChanged()
        {
            if (ScrollBar == null) return;

            if (Theme is TableTheme theme)
                ScrollBar.Theme = theme.ScrollBarTheme;
            else
                ScrollBar.Theme = null;
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
                    CurrentMouseCell = Cells.GetIfExists(mousePosCellIndex.Value.Y, mousePosCellIndex.Value.X) ??
                        new Cell(mousePosCellIndex.Value.Y, mousePosCellIndex.Value.X, this, string.Empty)
                        {
                            Position = Cells.GetCellPosition(mousePosCellIndex.Value.Y, mousePosCellIndex.Value.X, out _, out _, IsScrollBarVisible ? ScrollBar.Value : 0)
                        };
                    if (CurrentMouseCell.Settings.Interactable)
                        OnCellEnter?.Invoke(this, new CellEventArgs(CurrentMouseCell));
                }
                else
                {
                    if (CurrentMouseCell != null && CurrentMouseCell.Settings.Interactable)
                        OnCellExit?.Invoke(this, new CellEventArgs(CurrentMouseCell));
                }
                IsDirty = true;
            }

            if (state.OriginalMouseState.Mouse.ScrollWheelValueChange != 0)
            {
                if (state.OriginalMouseState.Mouse.ScrollWheelValueChange < 0)
                    ScrollBar.Value -= 1;
                else
                    ScrollBar.Value += 1;
            }
        }

        protected override void OnLeftMouseClicked(ControlMouseState state)
        {
            base.OnLeftMouseClicked(state);

            if (CurrentMouseCell != null)
            {
                if (SelectedCell != CurrentMouseCell && CurrentMouseCell.Settings.Interactable && CurrentMouseCell.Settings.IsVisible && CurrentMouseCell.Settings.Selectable)
                {
                    var previous = SelectedCell;
                    SelectedCell = CurrentMouseCell;
                    ScrollToSelectedItem();
                    SelectedCellChanged?.Invoke(this, new CellChangedEventArgs(previous, SelectedCell));
                }
                else
                {
                    // Unselect after clicking the selected cell again
                    var previous = SelectedCell;
                    SelectedCell = null;
                    SelectedCellChanged?.Invoke(this, new CellChangedEventArgs(previous, SelectedCell));
                }

                if (CurrentMouseCell.Settings.Interactable && CurrentMouseCell.Settings.IsVisible)
                    OnCellLeftClick?.Invoke(this, new CellEventArgs(CurrentMouseCell));

                DateTime click = DateTime.Now;
                bool doubleClicked = (click - _leftMouseLastClick).TotalSeconds <= 0.25 && state.MousePosition == _leftMouseLastClickPosition;
                _leftMouseLastClick = click;
                _leftMouseLastClickPosition = state.MousePosition;

                if (doubleClicked)
                {
                    _leftMouseLastClick = DateTime.MinValue;
                    _leftMouseLastClickPosition = null;
                    if (CurrentMouseCell.Settings.Interactable && CurrentMouseCell.Settings.IsVisible)
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

            if (CurrentMouseCell != null && CurrentMouseCell.Settings.Interactable && CurrentMouseCell.Settings.IsVisible)
            {
                OnCellRightClick?.Invoke(this, new CellEventArgs(CurrentMouseCell));
            }
        }

        protected override void OnMouseExit(ControlMouseState state)
        {
            base.OnMouseExit(state);

            if (CurrentMouseCell != null)
            {
                if (CurrentMouseCell.Settings.Interactable && CurrentMouseCell.Settings.IsVisible)
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
                    var position = Cells.GetCellPosition(row, col, out int rowSize, out int columnSize, IsScrollBarVisible ? ScrollBar.Value : 0);
                    if (IsMouseWithinCell(mousePosition, position.Y, position.X, columnSize, rowSize))
                        return (col, row);
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

            private Color _foreground;
            public Color Foreground
            {
                get { return _foreground; }
                set
                {
                    if (Foreground != value)
                    {
                        _foreground = value;
                        AddToTableIfNotExists();
                        Table.IsDirty = true;
                    }
                }
            }

            private Color _background;
            public Color Background
            {
                get { return _background; }
                set
                {
                    if (Background != value)
                    {
                        _background = value;
                        AddToTableIfNotExists();
                        Table.IsDirty = true;
                    }
                }
            }

            private string _text;
            public string Text
            {
                get { return _text; }
                set
                {
                    if (Text != value)
                    {
                        _text = value;
                        AddToTableIfNotExists();
                        Table.IsDirty = true;
                    }
                }
            }

            private Options _settings;
            public Options Settings
            {
                get { return _settings ??= new Options(this); }
                set
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

            internal readonly Table Table;

            internal Cell(int row, int col, Table table, string text)
            {
                Table = table;
                _text = text;
                _foreground = table.DefaultForeground;
                _background = table.DefaultBackground;

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
                    if (option.SettingsInitialized)
                        (_settings ??= new Options(this)).CopyFrom(option.Settings);
                }
            }

            internal void AddToTableIfNotExists()
            {
                if (Table.Cells.GetIfExists(Row, Column) == null)
                    Table.Cells[Row, Column] = this;
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

            public class Options : IEquatable<Options>
            {
                private HorizontalAlign _horizontalAlignment;
                public HorizontalAlign HorizontalAlignment
                {
                    get { return _horizontalAlignment; }
                    set
                    {
                        if (value != HorizontalAlignment)
                        {
                            _horizontalAlignment = value;
                            if (_usedForLayout) return;
                            _cell.AddToTableIfNotExists();
                            _cell.Table.IsDirty = true;
                        }
                    }
                }

                private VerticalAlign _verticalAlignment;
                public VerticalAlign VerticalAlignment
                {
                    get { return _verticalAlignment; }
                    set
                    {
                        if (value != VerticalAlignment)
                        {
                            _verticalAlignment = value;
                            if (_usedForLayout) return;
                            _cell.AddToTableIfNotExists();
                            _cell.Table.IsDirty = true;
                        }
                    }
                }

                private int? _maxCharactersPerLine;
                public int? MaxCharactersPerLine
                {
                    get { return _maxCharactersPerLine; }
                    set
                    {
                        if (value != MaxCharactersPerLine)
                        {
                            _maxCharactersPerLine = value;
                            if (_usedForLayout) return;
                            _cell.AddToTableIfNotExists();
                            _cell.Table.IsDirty = true;
                        }
                    }
                }

                private bool _interactable = true;
                public bool Interactable
                {
                    get { return _interactable; }
                    set
                    {
                        if (value != Interactable)
                        {
                            _interactable = value;
                            if (_usedForLayout) return;
                            _cell.AddToTableIfNotExists();
                            _cell.Table.IsDirty = true;
                        }
                    }
                }

                private bool _selectable = true;
                public bool Selectable
                {
                    get { return _selectable; }
                    set
                    {
                        if (value != Selectable)
                        {
                            _selectable = value;
                            if (_usedForLayout) return;
                            _cell.AddToTableIfNotExists();
                            _cell.Table.IsDirty = true;
                        }
                    }
                }

                private bool _isVisible = true;
                public bool IsVisible
                {
                    get { return _isVisible; }
                    set
                    {
                        if (value != IsVisible)
                        {
                            _isVisible = value;
                            if (_usedForLayout) return;
                            _cell.AddToTableIfNotExists();
                            _cell.Table.IsDirty = true;
                        }
                    }
                }

                private Cells.Layout.Mode _selectionMode;
                public Cells.Layout.Mode SelectionMode
                {
                    get { return _selectionMode; }
                    set
                    {
                        if (value != SelectionMode)
                        {
                            _selectionMode = value;
                            if (_usedForLayout) return;
                            _cell.AddToTableIfNotExists();
                            _cell.Table.IsDirty = true;
                        }
                    }
                }

                private Cells.Layout.Mode _hoverMode;
                public Cells.Layout.Mode HoverMode
                {
                    get { return _hoverMode; }
                    set
                    {
                        if (value != HoverMode)
                        {
                            _hoverMode = value;
                            if (_usedForLayout) return;
                            _cell.AddToTableIfNotExists();
                            _cell.Table.IsDirty = true;
                        }
                    }
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
                        other.Interactable == Interactable;
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
                        Interactable
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
        /// The maximum rows the table currently holds.
        /// </summary>
        public int MaxRows { get { return _cells.Count == 0 ? 0 : _cells.Values.Max(a => a.Row); } }

        /// <summary>
        /// The maximum columns the table currently holds.
        /// </summary>
        public int MaxColumns { get { return _cells.Count == 0 ? 0 : _cells.Values.Max(a => a.Column); } }

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
            layout ??= ColumnLayout[column] = new Layout(_table, Layout.LayoutType.Col);
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
        internal Point GetCellPosition(int row, int col, out int rowSize, out int columnSize, int scrollBarValue = 0)
        {
            var colScrollbarValue = _table.ScrollBar != null && _table.ScrollBar.Orientation == Orientation.Horizontal ? scrollBarValue : 0;
            var rowScrollbarValue = _table.ScrollBar != null && _table.ScrollBar.Orientation == Orientation.Vertical ? scrollBarValue : 0;
            int columnIndex = GetControlIndex(col, colScrollbarValue, Layout.LayoutType.Col, out columnSize);
            int rowIndex = GetControlIndex(row, rowScrollbarValue, Layout.LayoutType.Row, out rowSize);
            return new Point(columnIndex, rowIndex);
        }

        /// <summary>
        /// Get the size of the column or row or the default if no layout exists, without allocating a new layout object.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        internal int GetSizeOrDefault(int index, Layout.LayoutType type)
        {
            switch (type)
            {
                case Layout.LayoutType.Col:
                    return ColumnLayout.TryGetValue(index, out Layout layout) ? layout.Size : _table.DefaultCellSize.X;
                case Layout.LayoutType.Row:
                    return RowLayout.TryGetValue(index, out layout) ? layout.Size : _table.DefaultCellSize.Y;
                default:
                    throw new NotSupportedException("Invalid layout type.");
            }
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
                    Position = GetCellPosition(row, col, out _, out _, _table.IsScrollBarVisible ? _table.ScrollBar.Value : 0)
                };

                _cells[(row, col)] = cell;
                _table._checkScrollBarVisibility = true;
            }
            return cell;
        }

        private int GetControlIndex(int index, int scrollBarValue, Layout.LayoutType type, out int indexSize)
        {
            int count = scrollBarValue;
            indexSize = type == Layout.LayoutType.Col ?
                (ColumnLayout.TryGetValue(count, out Layout layout) ? layout.Size : _table.DefaultCellSize.X) :
                (RowLayout.TryGetValue(count, out layout) ? layout.Size : _table.DefaultCellSize.Y);

            int controlIndex = 0;
            while (count < index)
            {
                controlIndex += indexSize;
                count++;

                indexSize = type == Layout.LayoutType.Col ?
                    (ColumnLayout.TryGetValue(count, out layout) ? layout.Size : _table.DefaultCellSize.X) :
                    (RowLayout.TryGetValue(count, out layout) ? layout.Size : _table.DefaultCellSize.Y);
            }
            return controlIndex;
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

        internal void AdjustCellsAfterResize()
        {
            foreach (var cell in _cells)
                cell.Value.Position = GetCellPosition(cell.Value.Row, cell.Value.Column, out _, out _, _table.IsScrollBarVisible ? _table.ScrollBar.Value : 0);
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
                        _table.Cells.AdjustCellsAfterResize();
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

            internal bool SettingsInitialized { get { return _settings != null; } }

            private readonly Table _table;

            internal Layout(Table table, LayoutType type)
            {
                _table = table;
                Size = type == LayoutType.Col ? table.DefaultCellSize.X : table.DefaultCellSize.Y;
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
                    _table.Cells.AdjustCellsAfterResize();
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
                if (size != null) _size = size.Value;
                Foreground = foreground;
                Background = background;
                Settings = settings;
            }

            internal enum LayoutType
            {
                Col,
                Row
            }

            public enum Mode
            {
                Single = 0,
                None,
                EntireRow,
                EntireColumn
            }

            public class Range : IEnumerable<Layout>
            {
                private readonly IEnumerable<Layout> _layouts;

                internal Range(IEnumerable<Layout> layouts)
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
