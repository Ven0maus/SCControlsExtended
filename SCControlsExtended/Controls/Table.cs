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

        /// <summary>
        /// By default, only cells that have been indexed (eg. accessing table[0, 0]) will mark this cell to be drawn.
        /// Turn this off, if the whole table should draw as many tables as it fits, even with no data.
        /// </summary>
        public bool DrawOnlyIndexedCells { get; set; } = true;

        internal Point? MousedOverCellPosition { get; private set; }

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

        public void Clear()
        {
            Cells.Clear();
        }

        protected override void OnMouseIn(ControlMouseState state)
        {
            base.OnMouseIn(state);

            var prev = MousedOverCellPosition;
            MousedOverCellPosition = GetMousedOverCell(state.MousePosition);
            if (prev != MousedOverCellPosition)
                IsDirty = true;
        }

        private Point? GetMousedOverCell(Point mousePosition)
        {
            foreach (var cell in Cells)
            {
                if (IsMouseWithinCell(mousePosition, cell.ControlPosition.Y, cell.ControlPosition.X, cell.Width, cell.Height))
                    return cell.ControlPosition;
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
    }

    public class Cells : IEnumerable<Cells.Cell>
    {
        private readonly Dictionary<Point, Cell> _cells = new();
        private readonly Table _table;

        internal Dictionary<int, Layout> ColumnLayout = new();
        internal Dictionary<int, Layout> RowLayout = new();

        public Cell this[int row, int col]
        {
            get { return GetOrCreateCell(row, col); }
            set { SetCell(row, col, value); }
        }

        internal Cells(Table table)
        {
            _table = table;
        }

        public Layout Column(int column)
        {
            var layout = ColumnLayout.GetValueOrDefault(column);
            if (layout == null)
            {
                AddColumnLayout(column);
                layout = ColumnLayout[column];
            }
            return layout;
        }

        public Layout Row(int row)
        {
            var layout = RowLayout.GetValueOrDefault(row);
            if (layout == null)
            {
                AddRowLayout(row);
                layout = RowLayout[row];
            }
            return layout;
        }

        internal void Remove(int row, int col)
        {
            _cells.Remove((row, col));
        }

        internal void Clear()
        {
            RowLayout.Clear();
            ColumnLayout.Clear();
            _cells.Clear();
        }

        /// <summary>
        /// Creates a range of cells that can be iterated.
        /// </summary>
        /// <param name="startRow"></param>
        /// <param name="startCol"></param>
        /// <param name="endRow"></param>
        /// <param name="endCol"></param>
        /// <returns></returns>
        public CellRange Range(int startRow, int startCol, int endRow, int endCol)
        {
            var width = endCol - startCol + 1;
            var height = endRow - startRow + 1;
            return new CellRange(_table.Cells, new Rectangle(startCol, startRow, width, height));
        }

        private void AddColumnLayout(int column, int? width = null, int? height = null, Color? foreground = null, Color? background = null)
        {
            ColumnLayout[column] = new()
            {
                Width = width,
                Height = height,
                Foreground = foreground,
                Background = background
            };
        }

        private void AddRowLayout(int row, int? width = null, int? height = null, Color? foreground = null, Color? background = null)
        {
            RowLayout[row] = new()
            {
                Width = width,
                Height = height,
                Foreground = foreground,
                Background = background
            };
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
                    // Calculate the control position of the cell
                    ControlPosition = GetControlPosition(row, col)
                };

                _cells[(row, col)] = cell;
            }
            return cell;
        }

        public Point GetControlPosition(int row, int col)
        {
            return new Point(GetControlColumnIndex(row, col), GetControlRowIndex(row, col));
        }

        private int GetControlColumnIndex(int row, int col)
        {
            int index = 0;
            int count = 0;
            while (count < col)
            {
                var cell = GetIfExists(row, count);
                if (cell != null)
                {
                    index += cell.Width;
                }
                else
                {
                    index += _table.DefaultCellSize.X;
                }
                count++;
            }
            return index;
        }

        private int GetControlRowIndex(int row, int col)
        {
            int index = 0;
            int count = 0;
            while (count < row)
            {
                var cell = GetIfExists(count, col);
                if (cell != null)
                {
                    index += cell.Height;
                }
                else
                {
                    index += _table.DefaultCellSize.Y;
                }
                count++;
            }
            return index;
        }

        private void SetCell(int row, int col, Cell cell)
        {
            if (_cells.TryGetValue((row, col), out Cell oldCell))
            {
                if (!oldCell.Width.Equals(cell.Width) ||
                    !oldCell.Height.Equals(cell.Height) ||
                    !oldCell.Foreground.Equals(cell.Foreground) ||
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

        public IEnumerator<Cell> GetEnumerator()
        {
            return _cells.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public class Layout
        {
            public int? Width;
            public int? Height;
            public Color? Foreground;
            public Color? Background;

            internal Layout()
            { }

            public void SetLayout(int? width = null, int? height = null, Color? foreground = null, Color? background = null)
            {
                Width = width;
                Height = height;
                Foreground = foreground;
                Background = background;
            }
        }

        public class CellRange : IEnumerable<Cell>
        {
            private readonly Cell[] _cells;

            internal CellRange(Cells cells, Rectangle rect)
            {
                _cells = new Cell[rect.Width * rect.Height];
                for (int x = rect.X; x < rect.X + rect.Width; x++)
                {
                    for (int y = rect.Y; y < rect.Y + rect.Height; y++)
                    {
                        _cells[(y - rect.Y) * rect.Width + (x - rect.X)] = cells[y, x];
                    }
                }
            }

            public void ForEach(Action<Cells.Cell> action)
            {
                foreach (var cell in this)
                {
                    action(cell);
                }
            }

            public IEnumerator<Cell> GetEnumerator()
            {
                return ((IEnumerable<Cell>)_cells).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class Cell
        {
            internal Point ControlPosition { get; set; }
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

            private int _width;
            public int Width
            {
                get { return _width; }
                set
                {
                    if (_width != value)
                    {
                        _width = value;
                        _table.IsDirty = true;
                    }
                }
            }

            private int _height;
            public int Height
            {
                get { return _height; }
                set
                {
                    if (_height != value)
                    {
                        _height = value;
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
                Width = table.DefaultCellSize.X;
                Height = table.DefaultCellSize.Y;
                Foreground = table.DefaultForeground;
                Background = table.DefaultBackground;

                // Set cell layout options
                var columnLayout = table.Cells.ColumnLayout.GetValueOrDefault(col);
                var rowLayout = table.Cells.RowLayout.GetValueOrDefault(row);
                var layoutOptions = new[] { columnLayout, rowLayout };
                foreach (var option in layoutOptions)
                {
                    if (option == null) continue;
                    if (option.Width != null)
                        Width = option.Width.Value;
                    if (option.Height != null)
                        Height = option.Height.Value;
                    if (option.Foreground != null)
                        Foreground = option.Foreground.Value;
                    if (option.Background != null)
                        Background = option.Background.Value;
                }
            }
        }
    }
}
