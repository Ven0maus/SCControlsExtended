using SadConsole.UI.Controls;
using SadRogue.Primitives;
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

        private DateTime _leftMouseLastClick = DateTime.Now;

        public Table(int tableWidth, int tableHeight, int cellWidth, Color foreground, Color background, int cellHeight = 1) : base(tableWidth, tableHeight)
        {
            DefaultCellSize = new(cellWidth, cellHeight);
            DefaultForeground = foreground;
            DefaultBackground = background;
            Cells = new(this);
        }

        public Table(int tableWidth, int tableHeight, int cellWidth, int cellHeight = 1)
            : this(tableWidth, tableHeight, cellWidth, Color.White, Color.Transparent, cellHeight)
        { }

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
                            Position = Cells.GetCellPosition(mousePosCellIndex.Value.Y, mousePosCellIndex.Value.X, out _, out _)
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
                bool doubleClicked = (click - _leftMouseLastClick).TotalSeconds <= 0.5;
                _leftMouseLastClick = click;

                if (doubleClicked)
                {
                    _leftMouseLastClick = DateTime.MinValue;
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
                    var position = Cells.GetCellPosition(row, col, out int rowSize, out int columnSize);
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

        public sealed class Cell : IEquatable<Cell>
        {
            internal Point Position { get; set; }
            public int Row { get; }
            public int Column { get; }

            // Adjustable settings
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
                    if (Settings != value)
                    {
                        _settings = value;
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
                        _settings = option.Settings;
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
            set { SetCell(row, col, value); }
        }

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
            layout ??= ColumnLayout[column] = new Layout(_table, Layout.Type.Col);
            return layout;
        }

        /// <summary>
        /// Get the layout for the given columns
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public Layout.Range Column(params int[] columns)
        {
            var layouts = columns.Select(a => Column(a));
            return new Layout.Range(layouts);
        }

        /// <summary>
        /// Get the layout for the given row
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public Layout Row(int row)
        {
            RowLayout.TryGetValue(row, out Layout layout);
            layout ??= RowLayout[row] = new Layout(_table, Layout.Type.Row);
            return layout;
        }

        /// <summary>
        /// Get the layout for the given rows
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public Layout.Range Row(params int[] rows)
        {
            var layouts = rows.Select(a => Row(a));
            return new Layout.Range(layouts);
        }

        /// <summary>
        /// Resets all the cells and layout options
        /// </summary>
        public void Clear(bool clearLayout = true)
        {
            if (clearLayout)
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
        internal Point GetCellPosition(int row, int col, out int rowSize, out int columnSize)
        {
            return new Point(GetControlIndex(col, Layout.Type.Col, out columnSize), GetControlIndex(row, Layout.Type.Row, out rowSize));
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
                    Position = GetCellPosition(row, col, out _, out _)
                };

                _cells[(row, col)] = cell;
            }
            return cell;
        }

        private int GetControlIndex(int index, Layout.Type type, out int indexSize)
        {
            int count = 0;
            indexSize = type == Layout.Type.Col ?
                _table.Cells.Column(count).Size :
                _table.Cells.Row(count).Size;

            int controlIndex = 0;
            while (count < index)
            {
                controlIndex += indexSize;
                count++;

                indexSize = type == Layout.Type.Col ?
                    _table.Cells.Column(count).Size :
                    _table.Cells.Row(count).Size;
            }
            return controlIndex;
        }

        private void SetCell(int row, int col, Table.Cell cell)
        {
            if (cell == null)
            {
                if (_cells.Remove((row, col)))
                    _table.IsDirty = true;
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
                    _table.IsDirty = true;
                }
            }
            else
            {
                _cells[(row, col)] = cell;
                _table.IsDirty = true;
            }
        }

        internal void AdjustCellsAfterResize()
        {
            foreach (var cell in _cells)
                cell.Value.Position = GetCellPosition(cell.Value.Row, cell.Value.Column, out _, out _);
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

        public class Layout : ILayout
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

            public Color? Foreground;
            public Color? Background;

            private Table.Cell.Options _settings;
            public Table.Cell.Options Settings
            {
                get { return _settings ??= new Table.Cell.Options(_table); }
                set 
                {
                    if (value == null) return;
                    _settings = value;
                }
            }

            internal bool SettingsInitialized { get { return _settings != null; } }

            private readonly Table _table;

            internal enum Type
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

            internal Layout(Table table, Type type)
            {
                _table = table;
                Size = type == Type.Col ? table.DefaultCellSize.X : table.DefaultCellSize.Y;
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

            internal void SetLayoutInternal(int? size = null, Color? foreground = null, Color? background = null, Table.Cell.Options settings = null)
            {
                if (size != null) _size = size.Value;
                Foreground = foreground;
                Background = background;
                Settings = settings;
            }

            public class Range : IEnumerable<Layout>, ILayout
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

        interface ILayout
        {
            void SetLayout(int? size = null, Color? foreground = null, Color? background = null, Table.Cell.Options settings = null);
        }
    }
}
