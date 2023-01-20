using SadConsole.UI.Controls;
using SadRogue.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SCControlsExtended.Controls
{
    public class Table : CompositeControl
    {
        public Cells Cells { get; }
        public Color DefaultForeground { get; set; }
        public Color DefaultBackground { get; set; }
        public Point DefaultCellSize { get; set; }

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
        public Cells.Cell CurrentMouseCell { get; private set; }
        /// <summary>
        /// Returns the current selected cell
        /// </summary>
        public Cells.Cell SelectedCell { get; private set; }

        /// <summary>
        /// By default, only cells that have been indexed (eg. accessing table[0, 0]) will be rendered on the table control.
        /// Turn this off, if the whole table should draw as many cells  as it fits, even with no data.
        /// </summary>
        public bool DrawOnlyIndexedCells { get; set; } = true;

        /// <summary>
        /// By default, the cell hover event effect is rendered on the control
        /// </summary>
        public bool RenderHoverEffect { get; set; } = true;

        /// <summary>
        /// By default, the cell selection event effect is rendered on the control
        /// </summary>
        public bool RenderSelectionEffect { get; set; } = true;

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
            Point? currentPosition = CurrentMouseCell == null ? null : (CurrentMouseCell.ColumnIndex, CurrentMouseCell.RowIndex);

            if (!Equals(mousePosCellIndex, currentPosition))
            {
                if (mousePosCellIndex != null)
                {
                    // TODO: If cell does not yet exist in table, then add a check in cell when any property is modified it is added to the table automatically.
                    CurrentMouseCell = Cells.GetIfExists(mousePosCellIndex.Value.Y, mousePosCellIndex.Value.X) ??
                        new Cells.Cell(mousePosCellIndex.Value.Y, mousePosCellIndex.Value.X, this, string.Empty)
                        {
                            Position = Cells.GetCellPosition(mousePosCellIndex.Value.Y, mousePosCellIndex.Value.X, out _, out _)
                        };
                    OnCellEnter?.Invoke(this, new CellEventArgs(CurrentMouseCell));
                }
                else
                    OnCellExit?.Invoke(this, new CellEventArgs(CurrentMouseCell));
                IsDirty = true;
            }
        }

        protected override void OnLeftMouseClicked(ControlMouseState state)
        {
            base.OnLeftMouseClicked(state);

            if (CurrentMouseCell != null)
            {
                if (SelectedCell != CurrentMouseCell)
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

                OnCellLeftClick?.Invoke(this, new CellEventArgs(CurrentMouseCell));

                DateTime click = DateTime.Now;
                bool doubleClicked = (click - _leftMouseLastClick).TotalSeconds <= 0.5;
                _leftMouseLastClick = click;

                if (doubleClicked)
                {
                    _leftMouseLastClick = DateTime.MinValue;
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

            if (CurrentMouseCell != null)
            {
                OnCellRightClick?.Invoke(this, new CellEventArgs(CurrentMouseCell));
            }
        }

        protected override void OnMouseExit(ControlMouseState state)
        {
            base.OnMouseExit(state);

            if (CurrentMouseCell != null)
            {
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
            public readonly Cells.Cell Cell;

            internal CellEventArgs(Cells.Cell cell)
            {
                Cell = cell;
            }
        }

        public class CellChangedEventArgs : CellEventArgs
        {
            public readonly Cells.Cell PreviousCell;

            internal CellChangedEventArgs(Cells.Cell previousCell, Cells.Cell cell)
                : base(cell)
            {
                PreviousCell = previousCell;
            }
        }
    }

    public class Cells : IEnumerable<Cells.Cell>
    {
        private readonly Table _table;
        private readonly Dictionary<Point, Cell> _cells = new();
        private readonly Dictionary<int, Layout> ColumnLayout = new();
        private readonly Dictionary<int, Layout> RowLayout = new();

        public Cell this[int row, int col]
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
        /// Get the layout for a specific column
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
        /// Get the layout for a specific row
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
        /// Creates a range of cells that can be iterated.
        /// </summary>
        /// <param name="startRow"></param>
        /// <param name="startCol"></param>
        /// <param name="endRow"></param>
        /// <param name="endCol"></param>
        /// <returns></returns>
        public IEnumerable<Cell> Range(int startRow, int startCol, int endRow, int endCol)
        {
            var width = endCol - startCol + 1;
            var height = endRow - startRow + 1;
            for (int x = startCol; x < startCol + width; x++)
            {
                for (int y = startRow; y < startRow + height; y++)
                {
                    yield return this[y, x];
                }
            }
        }

        /// <summary>
        /// Resets all the cells and layout options
        /// </summary>
        public void Clear()
        {
            RowLayout.Clear();
            ColumnLayout.Clear();
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

        internal Cell GetIfExists(int row, int col)
        {
            if (_cells.TryGetValue((row, col), out Cell cell))
                return cell;
            return null;
        }

        private Cell GetOrCreateCell(int row, int col)
        {
            if (!_cells.TryGetValue((row, col), out Cell cell))
            {
                cell = new Cell(row, col, _table, string.Empty)
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

        private void SetCell(int row, int col, Cell cell)
        {
            if (_cells.TryGetValue((row, col), out Cell oldCell))
            {
                if (!oldCell.Foreground.Equals(cell.Foreground) ||
                    !oldCell.Background.Equals(cell.Background) ||
                    !oldCell.Text.Equals(cell.Text))
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
            {
                cell.Value.Position = GetCellPosition(cell.Value.RowIndex, cell.Value.ColumnIndex, out _, out _);
            }
        }

        public IEnumerator<Cell> GetEnumerator()
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

            public Color? Foreground;
            public Color? Background;

            private readonly Table _table;

            internal enum Type
            {
                Col,
                Row
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
            public void SetLayout(int? size = null, Color? foreground = null, Color? background = null)
            {
                var prevSize = _size;
                SetLayoutInternal(size, foreground, background);
                if (prevSize != _size)
                {
                    _table.Cells.AdjustCellsAfterResize();
                    _table.IsDirty = true;
                }
            }

            internal void SetLayoutInternal(int? size = null, Color? foreground = null, Color? background = null)
            {
                if (size != null) _size = size.Value;
                Foreground = foreground;
                Background = background;
            }
        }

        public class Cell : IEqualityComparer<Cell>
        {
            internal Point Position { get; set; }
            public int RowIndex { get; }
            public int ColumnIndex { get; }

            // Adjustable settings
            private Color _foreground;
            public Color Foreground
            {
                get { return _foreground; }
                set
                {
                    if (_foreground != value)
                    {
                        _foreground = value;
                        AddToTableIfNotExists();
                        _table.IsDirty = true;
                    }
                }
            }

            private Color _background;
            public Color Background
            {
                get { return _background; }
                set
                {
                    if (_background != value)
                    {
                        _background = value;
                        AddToTableIfNotExists();
                        _table.IsDirty = true;
                    }
                }
            }

            private string _text;
            public string Text
            {
                get { return _text; }
                set
                {
                    if (_text != value)
                    {
                        _text = value;
                        AddToTableIfNotExists();
                        _table.IsDirty = true;
                    }
                }
            }

            private readonly Table _table;

            internal Cell(int row, int col, Table table, string text)
            {
                _table = table;
                _text = text;
                _foreground = table.DefaultForeground;
                _background = table.DefaultBackground;

                RowIndex = row;
                ColumnIndex = col;

                // Set cell layout options
                table.Cells.ColumnLayout.TryGetValue(col, out var columnLayout);
                table.Cells.RowLayout.TryGetValue(row, out var rowLayout);
                var layoutOptions = new[] { rowLayout, columnLayout };
                foreach (var option in layoutOptions)
                {
                    if (option == null) continue;
                    if (option.Foreground != null)
                        _foreground = option.Foreground.Value;
                    if (option.Background != null)
                        _background = option.Background.Value;
                }
            }

            private void AddToTableIfNotExists()
            {
                if (_table.Cells.GetIfExists(RowIndex, ColumnIndex) == null)
                {
                    _table.Cells[RowIndex, ColumnIndex] = this;
                }
            }

            public void SetLayout(int? rowSize = null, int? columnSize = null, Color? foreground = null, Color? background = null)
            {
                _table.Cells.Column(ColumnIndex).SetLayoutInternal(columnSize, foreground, background);
                _table.Cells.Row(RowIndex).SetLayoutInternal(rowSize, foreground, background);
                if (rowSize != null || columnSize != null)
                    _table.Cells.AdjustCellsAfterResize();
                _table.IsDirty = true;
            }

            public bool Equals(Cell cell1, Cell cell2)
            {
                if (cell1 == null && cell2 != null) return false;
                if (cell1 != null && cell2 == null) return false;
                if (cell1 == null && cell2 == null) return true;
                return cell1.ColumnIndex == cell2.ColumnIndex && cell1.RowIndex == cell2.RowIndex;
            }

            public int GetHashCode([DisallowNull] Cell obj)
            {
                return HashCode.Combine(obj.RowIndex, obj.ColumnIndex);
            }
        }
    }
}
