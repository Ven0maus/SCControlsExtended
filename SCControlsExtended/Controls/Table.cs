using SadConsole.UI.Controls;
using SadRogue.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SCControlsExtended.Controls
{
    public class Table : CompositeControl
    {
        public Cells Cells { get; }
        public Color DefaultForeground { get; set; }
        public Color DefaultBackground { get; set; }
        public Point DefaultCellSize { get; set; }

        private bool _isMouseEnabled = true;
        public bool IsMouseEnabled 
        { 
            get { return _isMouseEnabled; }
            set
            {
                _isMouseEnabled = value;
                if (!_isMouseEnabled)
                {
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
        /// By default, only cells that have been indexed (eg. accessing table[0, 0]) will be rendered on the table control.
        /// Turn this off, if the whole table should draw as many cells  as it fits, even with no data.
        /// </summary>
        public bool DrawOnlyIndexedCells { get; set; } = true;

        public event EventHandler<CellEventArgs> OnCellEnter;
        public event EventHandler<CellEventArgs> OnCellExit;

        // TODO: Events
        public event EventHandler<CellEventArgs> OnCellLeftClick;
        public event EventHandler<CellEventArgs> OnCellRightClick;
        public event EventHandler<CellEventArgs> OnCellDoubleClick;

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
            if (!IsMouseEnabled) return;

            base.OnMouseIn(state);

            // Handle mouse hovering over cell
            var mousePosCellIndex = GetCellIndexByMousePosition(state.MousePosition);
            Point? currentPosition = CurrentMouseCell == null ? null : (CurrentMouseCell.RowIndex, CurrentMouseCell.ColumnIndex);

            if (!Equals(mousePosCellIndex, currentPosition))
            {
                if (mousePosCellIndex != null)
                {
                    // TODO: If cell does not yet exist in table, then add a check in cell when any property is modified it is added to the table automatically.
                    CurrentMouseCell = Cells.GetIfExists(mousePosCellIndex.Value.Y, mousePosCellIndex.Value.X) ??
                        new Cells.Cell(mousePosCellIndex.Value.Y, mousePosCellIndex.Value.X, this, string.Empty);
                    OnCellEnter?.Invoke(this, new CellEventArgs(CurrentMouseCell));
                }
                else
                    OnCellExit?.Invoke(this, new CellEventArgs(CurrentMouseCell));
                IsDirty = true;
            }
        }

        protected override void OnMouseExit(ControlMouseState state)
        {
            if (!IsMouseEnabled) return;

            base.OnMouseExit(state);

            if (CurrentMouseCell != null)
            {
                OnCellExit?.Invoke(this, new CellEventArgs(CurrentMouseCell));
                CurrentMouseCell = null;
                IsDirty = true;
            }
        }

        private Point? GetCellIndexByMousePosition(Point mousePosition)
        {
            // TODO: Adjust to find the position of the cell relative to the mouse position, without having to loop all cells
            foreach (var cell in Cells)
            {
                if (IsMouseWithinCell(mousePosition, cell.Position.Y, cell.Position.X, 
                    Cells.Column(cell.ColumnIndex).Size, Cells.Row(cell.RowIndex).Size))
                    return (cell.ColumnIndex, cell.RowIndex);
            }

            // TODO: Implement for non existing cells

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
            public Cells.Cell Cell { get; }

            public CellEventArgs(Cells.Cell cell)
            {
                Cell = cell;
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
            var layout = ColumnLayout.GetValueOrDefault(column);
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
            var layout = RowLayout.GetValueOrDefault(row);
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
        /// Get's the cell position on the control based on the row and column
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public Point GetCellPosition(int row, int col)
        {
            return new Point(GetControlIndex(col, Layout.Type.Col), GetControlIndex(row, Layout.Type.Row));
        }

        /// <summary>
        /// Resets all the cells and layout options
        /// </summary>
        public void Clear()
        {
            RowLayout.Clear();
            ColumnLayout.Clear();
            _cells.Clear();
        }
        #endregion

        #region Internal Methods
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
                    // Calculate the control position of the cell
                    Position = GetCellPosition(row, col)
                };

                _cells[(row, col)] = cell;
            }
            return cell;
        }

        private int GetControlIndex(int index, Layout.Type type)
        {
            int controlIndex = 0;
            int count = 0;
            while (count < index)
            {
                controlIndex += type == Layout.Type.Col ? 
                    _table.Cells.Column(count).Size :
                    _table.Cells.Row(count).Size;
                count++;
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
            // TODO: only adjust the right cells without having to loop over the entire collection
            foreach (var cell in _cells)
            {
                cell.Value.Position = GetCellPosition(cell.Value.RowIndex, cell.Value.ColumnIndex);
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

            public Color Foreground;
            public Color Background;

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
                Foreground = table.DefaultForeground;
                Background = table.DefaultBackground;
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
                if (foreground != null) Foreground = foreground.Value;
                if (background != null) Background = background.Value;
            }
        }

        public class Cell
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
                        _table.IsDirty = true;
                    }
                }
            }

            private readonly Table _table;

            internal Cell(int row, int col, Table table, string text)
            {
                _table = table;
                Text = text;
                RowIndex = row;
                ColumnIndex = col;

                // Default settings
                Foreground = table.DefaultForeground;
                Background = table.DefaultBackground;

                // Set cell layout options
                var columnLayout = table.Cells.ColumnLayout.GetValueOrDefault(col);
                var rowLayout = table.Cells.RowLayout.GetValueOrDefault(row);
                var layoutOptions = new[] { columnLayout, rowLayout };
                foreach (var option in layoutOptions)
                {
                    if (option == null) continue;
                    Foreground = option.Foreground;
                    Background = option.Background;
                }
            }

            public void SetLayout(int? rowSize = null, int? columnSize = null, Color? foreground = null, Color? background = null)
            {
                _table.Cells.Column(ColumnIndex).SetLayoutInternal(columnSize, foreground, background);
                _table.Cells.Row(RowIndex).SetLayoutInternal(rowSize, foreground, background);
                _table.Cells.AdjustCellsAfterResize();
                _table.IsDirty = true;
            }
        }
    }
}
