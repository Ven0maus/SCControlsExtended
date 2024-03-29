using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SadConsole;
using SadConsole.UI.Controls;
using SadRogue.Primitives;

namespace SCControlsExtended.Controls;

/// <summary>
/// A scrollable table control.
/// </summary>
public class Table : CompositeControl
{
    /// <summary>
    /// The cells collection used to modify the table cells
    /// </summary>
    public Cells Cells { get; }
    /// <summary>
    /// The default foreground color used for the table foreground and newly created cells
    /// </summary>
    public Color DefaultForeground { get; set; }
    /// <summary>
    /// The default background color used for the table background and newly created cells
    /// </summary>
    public Color DefaultBackground { get; set; }
    /// <summary>
    /// The default size a cell gets when it is newly created
    /// </summary>
    public Point DefaultCellSize { get; set; }
    /// <summary>
    /// The default visual hovering mode when hovering over cells
    /// </summary>
    public Cells.Layout.Mode DefaultHoverMode { get; set; }
    /// <summary>
    /// The default visual selection mode when selecting a cell
    /// </summary>
    public Cells.Layout.Mode DefaultSelectionMode { get; set; }

    private bool _useMouse = true;
    /// <summary>
    /// When <see langword="true"/>, this object will use the mouse; otherwise <see langword="false"/>.
    /// </summary>
    public new bool UseMouse
    {
        get => _useMouse;
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
    public Cell? CurrentMouseCell { get; private set; }

    private Cell? _selectedCell;
    /// <summary>
    /// Returns the current selected cell
    /// </summary>
    public Cell? SelectedCell
    {
        get => _selectedCell;
        internal set
        {
            Cell? prev = _selectedCell;
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
    public event EventHandler<CellEventArgs>? OnCellEnter;
    /// <summary>
    /// Fires an event when a cell is exited by the mouse.
    /// </summary>
    public event EventHandler<CellEventArgs>? OnCellExit;
    /// <summary>
    /// Fires an event when the selected cell has changed.
    /// </summary>
    public event EventHandler<CellChangedEventArgs>? SelectedCellChanged;
    /// <summary>
    /// Fires an event when a cell is left clicked.
    /// </summary>
    public event EventHandler<CellEventArgs>? OnCellLeftClick;
    /// <summary>
    /// Fires an event when a cell is right clicked.
    /// </summary>
    public event EventHandler<CellEventArgs>? OnCellRightClick;
    /// <summary>
    /// Fires an event when a cell is double clicked.
    /// </summary>
    public event EventHandler<CellEventArgs>? OnCellDoubleClick;
    /// <summary>
    /// Called when a fake cells is being drawn, you can use this to modify the cell layout.
    /// </summary>
    public event EventHandler<CellEventArgs>? OnDrawFakeCell;

    /// <summary>
    /// The vertical scrollbar, use the SetupScrollBar method with Vertical orientation to initialize it.
    /// </summary>
    public ScrollBar? VerticalScrollBar { get; private set; }
    /// <summary>
    /// The horizontal scrollbar, use the SetupScrollBar method with Horizontal orientation to initialize it.
    /// </summary>
    public ScrollBar? HorizontalScrollBar { get; private set; }

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
    /// The updated maximum value, incase the scrollbar object's maximum value is not yet updated by the theme.
    /// </summary>
    public int VerticalScrollBarMaximum
    {
        get
        {
            if (VerticalScrollBar == null) return 0;
            UpdateScrollBarMaximum(Orientation.Vertical);
            return VerticalScrollBar.Maximum;
        }
    }

    /// <summary>
    /// The updated maximum value, incase the scrollbar object's maximum value is not yet updated by the theme.
    /// </summary>
    public int HorizontalScrollBarMaximum
    {
        get
        {
            if (HorizontalScrollBar == null) return 0;
            UpdateScrollBarMaximum(Orientation.Horizontal);
            return HorizontalScrollBar.Maximum;
        }
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
    private void ScrollBar_ValueChanged(object? sender, EventArgs e)
    {
        var scrollBar = (ScrollBar?)sender;
        if (scrollBar == null) return;
        int previousScrollValue = scrollBar.Orientation == Orientation.Vertical ? _previousScrollValueVertical : _previousScrollValueHorizontal;
        bool increment = previousScrollValue < scrollBar.Value;

        int diff = Math.Abs(scrollBar.Value - previousScrollValue);
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

    /// <summary>
    /// Creates a new table with the default SadConsole colors, and cell size of (1 width, 1 height)
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public Table(int width, int height) : base(width, height)
    {
        Cells = new Cells(this);
        DefaultForeground = Color.White;
        DefaultBackground = Color.TransparentBlack;
        DefaultCellSize = new Point(1, 1);
    }

    /// <summary>
    /// Creates a new table with custom cell width and cell height params; Uses the default SadConsole colors
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="cellWidth"></param>
    /// <param name="cellHeight"></param>
    public Table(int width, int height, int cellWidth, int cellHeight = 1)
        : this(width, height)
    {
        DefaultCellSize = new Point(cellWidth, cellHeight);
    }

    /// <summary>
    /// Creates a new table with extra params to set the base default values of the table
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="cellWidth"></param>
    /// <param name="defaultForeground"></param>
    /// <param name="defaultBackground"></param>
    /// <param name="cellHeight"></param>
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
    /// <param name="size"></param>
    /// <param name="position">The position of the scrollbar.</param>
    public void SetupScrollBar(Orientation orientation, int size, Point position)
    {
        bool scrollBarExists = false;
        int value = 0;
        int max = 0;

        ScrollBar? existingScrollBar = orientation == Orientation.Vertical ? VerticalScrollBar : HorizontalScrollBar;
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

    internal HashSet<int> GetIndexesWithContent(Cells.Layout.LayoutType indexType)
    {
        return !Cells.Any() ? new HashSet<int>() :
            Cells.Select(a => indexType == Cells.Layout.LayoutType.Row ? a.Row : a.Column).ToHashSet();
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

    private void UpdateScrollBarMaximum(Orientation orientation)
    {
        var scrollBar = orientation == Orientation.Horizontal ? HorizontalScrollBar : VerticalScrollBar;
        if (scrollBar != null)
        {
            var scrollItems = GetScrollBarItems(orientation);
            scrollBar.Maximum = scrollItems < 0 ? 0 : scrollItems;
        }
    }

    internal int GetScrollBarItems(Orientation orientation)
    {
        IEnumerable<IGrouping<int, Cell>> indexes = orientation == Orientation.Vertical ?
            Cells.GroupBy(a => a.Row) : Cells.GroupBy(a => a.Column);
        IOrderedEnumerable<IGrouping<int, Cell>> orderedIndex = indexes.OrderBy(a => a.Key);

        Cells.Layout.LayoutType layoutType = orientation == Orientation.Vertical ? Cells.Layout.LayoutType.Row : Cells.Layout.LayoutType.Column;
        int maxSize = orientation == Orientation.Vertical ? Height : Width;
        int totalSize = 0;
        int items = 0;
        foreach (IGrouping<int, Cell> index in orderedIndex)
        {
            int size = Cells.GetSizeOrDefault(index.Key, layoutType);
            if (IsEntireRowOrColumnNotVisible(index.Key, layoutType))
                continue;

            totalSize += size;

            if (totalSize > maxSize)
            {
                items++;
            }
        }
        return items;
    }

    /// <summary>
    /// Shows the scroll bar when there are too many items to display; otherwise, hides it.
    /// </summary>
    /// <param name="scrollBar"></param>
    internal bool ShowHideScrollBar(ScrollBar scrollBar)
    {
        // process the scroll bar
        int scrollbarItems = GetScrollBarItems(scrollBar.Orientation);
        if (scrollbarItems > 0)
        {
            scrollBar.Maximum = scrollbarItems;
            return true;
        }
        else
        {
            scrollBar.Maximum = 0;
            return false;
        }
    }

    internal bool IsEntireRowOrColumnNotVisible(int index, Cells.Layout.LayoutType type)
    {
        Cells._hiddenIndexes.TryGetValue(type, out var indexes);
        return indexes != null && indexes.Contains(index);
    }

    /// <summary>
    /// Scrolls the list to the item currently selected.
    /// </summary>
    public void ScrollToSelectedItem()
    {
        if (!AutoScrollOnCellSelection) return;
        if (IsVerticalScrollBarVisible || IsHorizontalScrollBarVisible)
        {
            ScrollBar?[] scrollBars = new[] { VerticalScrollBar, HorizontalScrollBar };
            foreach (ScrollBar? scrollBar in scrollBars)
            {
                if (scrollBar == null) continue;

                Orientation orientation = scrollBar.Orientation;
                int selectedIndex = SelectedCell != null ? (orientation == Orientation.Vertical ? SelectedCell.Row : SelectedCell.Column) : 0;

                bool isRowType = orientation == Orientation.Vertical;
                IOrderedEnumerable<int> indexes = Cells
                    .GroupBy(a => isRowType ? a.Row : a.Column)
                    .Select(a => a.Key)
                    .OrderBy(a => a);
                int totalIndexSize = 0;
                foreach (int index in indexes)
                {
                    int cellSize = Cells.GetSizeOrDefault(index, isRowType ?
                        Cells.Layout.LayoutType.Row : Cells.Layout.LayoutType.Column);
                    totalIndexSize += cellSize;

                    if (index > selectedIndex)
                        break;
                }

                int maxIndexSize = orientation == Orientation.Vertical ? GetMaxRowsBasedOnRowSizes() : GetMaxColumnsBasedOnColumnSizes();
                int max = orientation == Orientation.Vertical ? VisibleRowsMax : VisibleColumnsMax;
                int total = orientation == Orientation.Vertical ? VisibleRowsTotal : VisibleColumnsTotal;
                int defaultIndexSize = orientation == Orientation.Vertical ? DefaultCellSize.Y : DefaultCellSize.X;

                var indexSize = (totalIndexSize - total) / defaultIndexSize;
                scrollBar.Value = totalIndexSize < max ? 0 : indexSize > scrollBar.Maximum ? scrollBar.Maximum : indexSize;
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

        int amountVertical = IsVerticalScrollBarVisible && VerticalScrollBar != null ? VerticalScrollBar.Value : 0;
        int amountHorizontal = IsHorizontalScrollBarVisible && HorizontalScrollBar != null ? HorizontalScrollBar.Value : 0;
        int max = amountVertical > amountHorizontal ? amountVertical : amountHorizontal;

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
        int scrollPos = GetNextScrollPos(increment, orientation);

        if (orientation == Orientation.Vertical)
            StartRenderYPos += scrollPos;
        else
            StartRenderXPos += scrollPos;

        StartRenderYPos = StartRenderYPos < 0 ? 0 : StartRenderYPos;
        StartRenderXPos = StartRenderXPos < 0 ? 0 : StartRenderXPos;
    }

    internal int GetNextScrollPos(bool increment, Orientation orientation)
    {
        Cells.Layout.LayoutType type = orientation == Orientation.Vertical ?
            Cells.Layout.LayoutType.Row : Cells.Layout.LayoutType.Column;
        bool isRowType = type == Cells.Layout.LayoutType.Row;
        IEnumerable<IGrouping<int, Cell>> cellGroups = Cells.GroupBy(a => isRowType ? a.Row : a.Column);
        IOrderedEnumerable<IGrouping<int, Cell>> orderedCells = increment ? cellGroups.OrderBy(a => a.Key) :
            cellGroups.OrderByDescending(a => a.Key);

        foreach (IGrouping<int, Cell> group in orderedCells)
        {
            foreach (Cell cell in group)
            {
                bool partialOverlap = false;
                int indexSizeCell = isRowType ? cell.Position.Y : cell.Position.X;
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
                    bool isPositionOfScreen = isRowType ? indexSizeCell >= Height : indexSizeCell >= Width;
                    if (!isPositionOfScreen)
                    {
                        // Here it is only > because if the real cell pos is 20 its the ending, so where the next cell starts
                        // which means its not off screen
                        int realCellPosition = isRowType ? (cell.Position.Y + cell.Height) : (cell.Position.X + cell.Width);
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
                Dictionary<int, Cells.Layout> layoutDict = isRowType ? Cells._rowLayout : Cells._columnLayout;
                int defaultSize = isRowType ? DefaultCellSize.Y : DefaultCellSize.X;
                int offScreenIndex = isRowType ? cell.Row : cell.Column;
                int cellSize = layoutDict.TryGetValue(offScreenIndex, out Cells.Layout? layout) ?
                    layout.Size : defaultSize;

                // Calculate the overlap amount
                if (partialOverlap)
                {
                    int overlapAmount = indexSizeCell + cellSize - (isRowType ? Height : Width);
                    cellSize = overlapAmount;
                }

                return increment ? cellSize : -cellSize;
            }
        }

        int defaultCellSize = isRowType ? DefaultCellSize.Y : DefaultCellSize.X;
        return increment ? defaultCellSize : -defaultCellSize;
    }

    /// <summary>
    /// Sets the scrollbar's theme to the current theme's <see cref="TableTheme.ScrollBarTheme"/>.
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

    /// <inheritdoc/>
    protected override void OnMouseIn(ControlMouseState state)
    {
        base.OnMouseIn(state);

        // Handle mouse hovering over cell
        Point? mousePosCellIndex = GetCellIndexByMousePosition(state.MousePosition);
        Point? currentPosition = CurrentMouseCell == null ? (Point?)null : (CurrentMouseCell.Column, CurrentMouseCell.Row);

        if (!Equals(mousePosCellIndex, currentPosition))
        {
            if (mousePosCellIndex != null)
            {
                Cell? cell = Cells.GetIfExists(mousePosCellIndex.Value.Y, mousePosCellIndex.Value.X, true);
                if (cell == null && DrawFakeCells)
                {
                    // A fake cell doesn't know if it should be selected if the row is hidden
                    cell = new Cell(mousePosCellIndex.Value.Y, mousePosCellIndex.Value.X, this, string.Empty)
                    {
                        Position = Cells.GetCellPosition(mousePosCellIndex.Value.Y, mousePosCellIndex.Value.X, out _, out _,
                            IsVerticalScrollBarVisible ? StartRenderYPos : 0, IsHorizontalScrollBarVisible ? StartRenderXPos : 0)
                    };
                }
                if (CurrentMouseCell != cell)
                    IsDirty = true;
                CurrentMouseCell = cell;

                if (CurrentMouseCell != null && CurrentMouseCell.IsVisible && (!CurrentMouseCell.IsSettingsInitialized || CurrentMouseCell.Settings.Interactable))
                    OnCellEnter?.Invoke(this, new CellEventArgs(CurrentMouseCell));
            }
            else
            {
                if (CurrentMouseCell != null && CurrentMouseCell.IsVisible && (!CurrentMouseCell.IsSettingsInitialized || CurrentMouseCell.Settings.Interactable))
                {
                    OnCellExit?.Invoke(this, new CellEventArgs(CurrentMouseCell));
                    CurrentMouseCell = null;
                    IsDirty = true;
                }
            }
        }

        if (state.OriginalMouseState.Mouse.ScrollWheelValueChange != 0)
        {
            ScrollBar? scrollBar = null;
            if (IsVerticalScrollBarVisible && !IsHorizontalScrollBarVisible)
                scrollBar = VerticalScrollBar;
            else if (!IsVerticalScrollBarVisible && IsHorizontalScrollBarVisible)
                scrollBar = HorizontalScrollBar;
            // If both scroll bars are not null, we only wanna scroll on the vertical scrollbar with the mousewheel
            else if (IsVerticalScrollBarVisible && IsHorizontalScrollBarVisible)
                scrollBar = VerticalScrollBar;

            if (scrollBar != null)
            {
                var prev = scrollBar.Value;
                if (state.OriginalMouseState.Mouse.ScrollWheelValueChange < 0)
                    scrollBar.Value -= 1;
                else
                    scrollBar.Value += 1;
                if (prev != scrollBar.Value)
                    IsDirty = true;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnLeftMouseClicked(ControlMouseState state)
    {
        base.OnLeftMouseClicked(state);

        if (CurrentMouseCell != null)
        {
            if (SelectedCell != CurrentMouseCell && CurrentMouseCell.IsVisible && (!CurrentMouseCell.IsSettingsInitialized || (CurrentMouseCell.Settings.Interactable && CurrentMouseCell.Settings.Selectable)))
            {
                SelectedCell = CurrentMouseCell;
                ScrollToSelectedItem();
            }
            else
            {
                // Unselect after clicking the selected cell again
                SelectedCell = null;
            }

            if (CurrentMouseCell.IsVisible && (!CurrentMouseCell.IsSettingsInitialized || CurrentMouseCell.Settings.Interactable))
                OnCellLeftClick?.Invoke(this, new CellEventArgs(CurrentMouseCell));

            DateTime click = DateTime.Now;
            bool doubleClicked = (click - _leftMouseLastClick).TotalSeconds <= 0.25 && state.MousePosition == _leftMouseLastClickPosition;
            _leftMouseLastClick = click;
            _leftMouseLastClickPosition = state.MousePosition;

            if (doubleClicked)
            {
                _leftMouseLastClick = DateTime.MinValue;
                _leftMouseLastClickPosition = null;
                if (CurrentMouseCell.IsVisible && (!CurrentMouseCell.IsSettingsInitialized || CurrentMouseCell.Settings.Interactable))
                    OnCellDoubleClick?.Invoke(this, new CellEventArgs(CurrentMouseCell));
            }
        }
        else
        {
            SelectedCell = null;
        }
    }

    /// <inheritdoc/>
    protected override void OnRightMouseClicked(ControlMouseState state)
    {
        base.OnRightMouseClicked(state);

        if (CurrentMouseCell != null && (CurrentMouseCell.IsVisible && (!CurrentMouseCell.IsSettingsInitialized || CurrentMouseCell.Settings.Interactable)))
        {
            OnCellRightClick?.Invoke(this, new CellEventArgs(CurrentMouseCell));
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseExit(ControlMouseState state)
    {
        base.OnMouseExit(state);

        if (CurrentMouseCell != null)
        {
            if (CurrentMouseCell.IsVisible && (!CurrentMouseCell.IsSettingsInitialized || CurrentMouseCell.Settings.Interactable))
                OnCellExit?.Invoke(this, new CellEventArgs(CurrentMouseCell));
            CurrentMouseCell = null;
            IsDirty = true;
        }
    }

    private Point? GetCellIndexByMousePosition(Point mousePosition)
    {
        for (int col = 0; col <= Cells.MaxColumn; col++)
        {
            for (int row = 0; row <= Cells.MaxRow; row++)
            {
                int rowValue = row + (IsVerticalScrollBarVisible && VerticalScrollBar != null ? VerticalScrollBar.Value : 0);
                int colValue = col + (IsHorizontalScrollBarVisible && HorizontalScrollBar != null ? HorizontalScrollBar.Value : 0);

                Point position = Cells.GetCellPosition(rowValue, colValue, out int rowSize, out int columnSize,
                    IsVerticalScrollBarVisible ? StartRenderYPos : 0, IsHorizontalScrollBarVisible ? StartRenderXPos : 0);
                if (IsMouseWithinCell(mousePosition, position.Y, position.X, columnSize, rowSize))
                {
                    var cell = Cells.GetIfExists(rowValue, colValue, false);
                    if (cell == null || (cell._row == rowValue && cell._column == colValue && cell.IsVisible))
                        return cell != null ? (cell.Column, cell.Row) : (colValue, rowValue);
                }
            }
        }
        return null;
    }

    private static bool IsMouseWithinCell(Point mousePosition, int row, int column, int width, int height)
    {
        int maxX = column + width;
        int maxY = row + height;
        return mousePosition.X >= column && mousePosition.X < maxX &&
            mousePosition.Y >= row && mousePosition.Y < maxY;
    }

    #region Event Args
    /// <summary>
    /// Cell args for a table event
    /// </summary>
    public class CellEventArgs : EventArgs
    {
        /// <summary>
        /// The cell that triggered this event
        /// </summary>
        public readonly Cell? Cell;

        internal CellEventArgs(Cell? cell)
        {
            Cell = cell;
        }
    }

    /// <inheritdoc/>
    public sealed class CellChangedEventArgs : CellEventArgs
    {
        /// <summary>
        /// The original cell before the event was triggered
        /// </summary>
        public readonly Cell? PreviousCell;

        internal CellChangedEventArgs(Cell? previousCell, Cell? cell)
            : base(cell)
        {
            PreviousCell = previousCell;
        }
    }
    #endregion

    /// <summary>
    /// A basic cell used in the Table control
    /// </summary>
    public sealed class Cell : IEquatable<Cell>
    {
        /// <summary>
        /// Contains the real position value when other cells are not being rendered
        /// This is used to obtain the real cell from mouse interactions.
        /// </summary>
        internal Point Position;
        /// <summary>
        /// Contains the real row value and column value when other cells are not being rendered
        /// This is used to obtain the real cell from mouse interactions.
        /// </summary>
        internal int _row, _column;

        /// <summary>
        /// The row this cell is part of
        /// </summary>
        public int Row { get; }
        /// <summary>
        /// The column this cell is part of
        /// </summary>
        public int Column { get; }
        /// <summary>
        /// The height of the row this cell is part of
        /// </summary>
        public int Height => _table.Cells.GetSizeOrDefault(Row, Cells.Layout.LayoutType.Row);
        /// <summary>
        /// The width of the column this cell is part of
        /// </summary>
        public int Width => _table.Cells.GetSizeOrDefault(Column, Cells.Layout.LayoutType.Column);

        private Color _foreground;
        /// <summary>
        /// The foreground color used by the cell
        /// </summary>
        public Color Foreground
        {
            get => _foreground;
            set => SetFieldValue(this, Foreground, ref _foreground, value, false);
        }

        private Color _background;
        /// <summary>
        /// The background color used by the cell
        /// </summary>
        public Color Background
        {
            get => _background;
            set => SetFieldValue(this, Background, ref _background, value, false);
        }

        /// <summary>
        /// The text shown within the cell, empty string when no value.
        /// </summary>
        public string StringValue
        {
            get => _value?.ToString() ?? string.Empty;
        }

        private object? _value;
        /// <summary>
        /// The value of the cell, .ToString() method is shown as the text result within the cell.
        /// </summary>
        public object? Value
        {
            get => _value;
            set => SetFieldValue(this, Value, ref _value, value, false);
        }

        private bool _isVisible = true;
        /// <summary>
        /// Set to false if the cell should not be rendered within the table (default IsVisible sadconsole behaviour),
        /// If an entire row or column IsVislbe is set to false in the layout, it will skip this row/column entirely (differs from default behaviour)
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetFieldValue(this, IsVisible, ref _isVisible, value, false);
        }

        private Options? _settings;
        /// <summary>
        /// The setting options used by the cell to define its layout
        /// </summary>
        public Options Settings => _settings ??= new Options(this);

        private readonly bool _addToTableIfModified;
        internal readonly Table _table;

        internal Cell(int row, int col, Table table, object value, bool addToTableIfModified = true)
        {
            _table = table;
            _value = value;
            _row = row;
            _column = col;
            _foreground = table.DefaultForeground;
            _background = table.DefaultBackground;
            _addToTableIfModified = addToTableIfModified;

            Row = row;
            Column = col;

            // Set cell layout options
            _ = table.Cells._columnLayout.TryGetValue(col, out Cells.Layout? columnLayout);
            _ = table.Cells._rowLayout.TryGetValue(row, out Cells.Layout? rowLayout);
            Cells.Layout?[] layoutOptions = new[] { columnLayout, rowLayout };
            foreach (Cells.Layout? option in layoutOptions)
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
            if (_addToTableIfModified && _table.Cells.GetIfExists(Row, Column, true) == null)
                _table.Cells[Row, Column] = this;
        }

        internal bool IsSettingsInitialized => _settings != null;

        /// <inheritdoc/>
        public static bool operator ==(Cell? a, Cell? b)
        {
            if (a is null && b is null) return true;
            return a is not null && b is not null && a.Equals(b);
        }
        /// <inheritdoc/>
        public static bool operator !=(Cell? a, Cell? b)
        {
            return !(a == b);
        }
        /// <inheritdoc/>
        public bool Equals(Cell? cell)
        {
            return cell != null && cell.Column == Column && cell.Row == Row;
        }
        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is Cell cell && Equals(cell);
        }
        /// <inheritdoc/>
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
        internal static void SetFieldValue<T>(Cell? cell, T previousValue, ref T field, T newValue, bool usedForLayout)
        {
            if (!EqualityComparer<T>.Default.Equals(previousValue, newValue))
            {
                field = newValue;
                if (usedForLayout || cell is null) return;
                cell.AddToTableIfNotExists();
                cell._table.IsDirty = true;
            }
        }

        /// <summary>
        /// A collection of settings that are used by <see cref="Table.Cell"/>
        /// </summary>
        public class Options : IEquatable<Options>
        {
            private HorizontalAlign _horizontalAlignment;
            /// <summary>
            /// The horizontal text alignment setting; Default: left
            /// </summary>
            public HorizontalAlign HorizontalAlignment
            {
                get => _horizontalAlignment;
                set => SetFieldValue(_cell, HorizontalAlignment, ref _horizontalAlignment, value, _usedForLayout);
            }

            private VerticalAlign _verticalAlignment;
            /// <summary>
            /// The vertical text alignment setting; Default: left
            /// </summary>
            public VerticalAlign VerticalAlignment
            {
                get => _verticalAlignment;
                set => SetFieldValue(_cell, VerticalAlignment, ref _verticalAlignment, value, _usedForLayout);
            }

            private bool _useFakeLayout = false;
            /// <summary>
            /// Defines if the cell should also trigger the fake layout event if the option is enabled on the table; Default: false
            /// </summary>
            public bool UseFakeLayout
            {
                get => _useFakeLayout;
                set => SetFieldValue(_cell, UseFakeLayout, ref _useFakeLayout, value, _usedForLayout);
            }

            private int? _maxCharactersPerLine;
            /// <summary>
            /// The maximum characters this cell can show per line; Default: cell width
            /// </summary>
            public int? MaxCharactersPerLine
            {
                get => _maxCharactersPerLine;
                set => SetFieldValue(_cell, MaxCharactersPerLine, ref _maxCharactersPerLine, value, _usedForLayout);
            }

            private bool _interactable = true;
            /// <summary>
            /// Defines if the cell can interact with mouse events; Default: true
            /// </summary>
            public bool Interactable
            {
                get => _interactable;
                set => SetFieldValue(_cell, Interactable, ref _interactable, value, _usedForLayout);
            }

            private bool _selectable = true;
            /// <summary>
            /// Defines if the cell can be selected by the mouse; Default: true
            /// </summary>
            public bool Selectable
            {
                get => _selectable;
                set => SetFieldValue(_cell, Selectable, ref _selectable, value, _usedForLayout);
            }

            private Cells.Layout.Mode _selectionMode;
            /// <summary>
            /// Defines the selection visual mode when the cell is selected; Default: single
            /// </summary>
            public Cells.Layout.Mode SelectionMode
            {
                get => _selectionMode;
                set => SetFieldValue(_cell, SelectionMode, ref _selectionMode, value, _usedForLayout);
            }

            private Cells.Layout.Mode _hoverMode;
            /// <summary>
            /// Defines the hover visual mode when the cell is hovered over by the mouse; Default: single
            /// </summary>
            public Cells.Layout.Mode HoverMode
            {
                get => _hoverMode;
                set => SetFieldValue(_cell, HoverMode, ref _hoverMode, value, _usedForLayout);
            }

            private readonly bool _usedForLayout;
            private readonly Cell? _cell;

            internal Options(Cell cell)
            {
                _usedForLayout = false;
                _cell = cell;
                _hoverMode = _cell._table.DefaultHoverMode;
                _selectionMode = _cell._table.DefaultSelectionMode;
            }

            /// <summary>
            /// Creates new options based on the default values of the table
            /// </summary>
            /// <param name="table"></param>
            public Options(Table table)
            {
                _usedForLayout = true;
                _hoverMode = table.DefaultHoverMode;
                _selectionMode = table.DefaultSelectionMode;
            }

            /// <summary>
            /// Alignment enum for the horizontal axis
            /// </summary>
            public enum HorizontalAlign
            {
                /// <summary>
                /// Text will be aligned to the left side of the cell
                /// </summary>
                Left = 0,
                /// <summary>
                /// Text will be aligned within the center of the cell
                /// </summary>
                Center,
                /// <summary>
                /// Text will be aligned to the right side of the cell
                /// </summary>
                Right
            }

            /// <summary>
            /// Alignment enum for the vertical axis
            /// </summary>
            public enum VerticalAlign
            {
                /// <summary>
                /// Text will be aligned to the top of the cell
                /// </summary>
                Top = 0,
                /// <summary>
                /// Text will be aligned in the center of the cell
                /// </summary>
                Center,
                /// <summary>
                /// Text will be aligned to the bottom of the cell
                /// </summary>
                Bottom
            }

            /// <inheritdoc/>
            public static bool operator ==(Options? a, Options? b)
            {
                if (a is null && b is null) return true;
                return a is not null && b is not null && a.Equals(b);
            }
            /// <inheritdoc/>
            public static bool operator !=(Options? a, Options? b)
            {
                return !(a == b);
            }
            /// <inheritdoc/>
            public bool Equals(Options? other)
            {
                return other != null
&& other.HorizontalAlignment == HorizontalAlignment &&
                    other.VerticalAlignment == VerticalAlignment &&
                    other.MaxCharactersPerLine == MaxCharactersPerLine &&
                    other.Selectable == Selectable &&
                    other.SelectionMode == SelectionMode &&
                    other.HoverMode == HoverMode &&
                    other.Interactable == Interactable &&
                    other.UseFakeLayout == UseFakeLayout;
            }
            /// <inheritdoc/>
            public override bool Equals(object? obj)
            {
                return obj is Options to && Equals(to);
            }
            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return HashCode.Combine(new object?[]
                {
                    HorizontalAlignment,
                    VerticalAlignment,
                    MaxCharactersPerLine,
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
                Selectable = settings.Selectable;
                SelectionMode = settings.SelectionMode;
                HoverMode = settings.HoverMode;
                Interactable = settings.Interactable;
                UseFakeLayout = settings.UseFakeLayout;
            }
        }
    }
}

/// <summary>
/// A collection class that contains all the cells of the <see cref="Table"/> and methods to modify them.
/// </summary>
public sealed class Cells : IEnumerable<Table.Cell>
{
    private readonly Table _table;
    private readonly Dictionary<Point, Table.Cell> _cells = new();
    internal readonly Dictionary<int, Layout> _columnLayout = new();
    internal readonly Dictionary<int, Layout> _rowLayout = new();
    /// <summary>
    /// Contains all rows and columns that aren't rendered
    /// </summary>
    internal readonly Dictionary<Layout.LayoutType, HashSet<int>> _hiddenIndexes = new();

    /// <summary>
    /// Gets or creates a new cell on the specified row and column
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public Table.Cell this[int row, int col]
    {
        get => GetOrCreateCell(row, col);
        internal set => SetCell(row, col, value);
    }

    /// <summary>
    /// The maximum row in the table.
    /// </summary>
    public int MaxRow { get; private set; }

    /// <summary>
    /// The maximum column in the table.
    /// </summary>
    public int MaxColumn { get; private set; }

    private bool _headerRow;
    /// <summary>
    /// True if row 0 should be the header of the table, and remain at the top when vertical scrolling.
    /// </summary>
    public bool HeaderRow
    {
        get { return _headerRow; }
        set
        {
            _headerRow = value;
            _table._checkScrollBarVisibility = true;
            _table.IsDirty = true;
        }
    }

    /// <summary>
    /// The amount of cells currently in the table.
    /// </summary>
    public int Count => _cells.Count;

    internal Cells(Table table)
    {
        _table = table;
    }

    #region Public Methods
    /// <summary>
    /// Sets the visibility of the entire row
    /// </summary>
    /// <param name="row"></param>
    /// <param name="visible"></param>
    public void Row(int row, bool visible)
    {
        if (!_hiddenIndexes.TryGetValue(Layout.LayoutType.Row, out var indexes))
        {
            if (visible) return;

            indexes = new HashSet<int>();
            _hiddenIndexes.Add(Layout.LayoutType.Row, indexes);
        }

        for (int col = 0; col <= _table.Cells.MaxColumn; col++)
            _table.Cells[row, col].IsVisible = visible;

        if (visible)
        {
            indexes.Remove(row);
            if (indexes.Count == 0)
                _hiddenIndexes.Remove(Layout.LayoutType.Row);
        }
        else
        {
            indexes.Add(row);
        }

        if (_table.HorizontalScrollBar != null)
            _table.HorizontalScrollBar.Value = 0;
        if (_table.VerticalScrollBar != null)
            _table.VerticalScrollBar.Value = 0;

        _table.IsDirty = true;
        _table._checkScrollBarVisibility = true;
    }

    /// <summary>
    /// Sets the visibility of the entire column
    /// </summary>
    /// <param name="column"></param>
    /// <param name="visible"></param>
    public void Column(int column, bool visible)
    {
        if (!_hiddenIndexes.TryGetValue(Layout.LayoutType.Column, out var indexes))
        {
            if (visible) return;

            indexes = new HashSet<int>();
            _hiddenIndexes.Add(Layout.LayoutType.Column, indexes);
        }

        for (int row = 0; row <= _table.Cells.MaxRow; row++)
            _table.Cells[row, column].IsVisible = visible;

        if (visible)
        {
            indexes.Remove(column);
            if (indexes.Count == 0)
                _hiddenIndexes.Remove(Layout.LayoutType.Column);
        }
        else
        {
            indexes.Add(column);
        }

        if (_table.HorizontalScrollBar != null)
            _table.HorizontalScrollBar.Value = 0;
        if (_table.VerticalScrollBar != null)
            _table.VerticalScrollBar.Value = 0;

        _table.IsDirty = true;
        _table._checkScrollBarVisibility = true;
    }

    /// <summary>
    /// Get the layout for the given column
    /// </summary>
    /// <param name="column"></param>
    /// <returns></returns>
    public Layout Column(int column)
    {
        _ = _columnLayout.TryGetValue(column, out Layout? layout);
        layout ??= _columnLayout[column] = new Layout(_table, column, Layout.LayoutType.Column);
        return layout;
    }

    /// <summary>
    /// Get the layout for the given row
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    public Layout Row(int row)
    {
        _ = _rowLayout.TryGetValue(row, out Layout? layout);
        layout ??= _rowLayout[row] = new Layout(_table, row, Layout.LayoutType.Row);
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
    /// <param name="column"></param>
    public void Select(int row, int column)
    {
        // Set existing cell, or a fake one if it does not yet exists, but modifying this fake cell with add it to the table
        _table.SelectedCell = GetIfExists(row, column, false) ?? new Table.Cell(row, column, _table, string.Empty)
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
        Remove(row, column, true);
    }

    /// <summary>
    /// Resets all the cells data
    /// </summary>
    /// <param name="clearLayoutOptions"></param>
    public void Clear(bool clearLayoutOptions = true)
    {
        if (clearLayoutOptions)
        {
            _rowLayout.Clear();
            _columnLayout.Clear();
        }
        MaxRow = 0;
        MaxColumn = 0;
        _cells.Clear();
        _table.IsDirty = true;
    }

    /// <summary>
    /// Removes all rows and columns except the header if applicable
    /// </summary>
    /// <param name="clearLayoutOptionsForContent"></param>
    public void ClearContent(bool clearLayoutOptionsForContent = true)
    {
        // X = row, Y = column
        var maxColumn = HeaderRow ? _cells.Where(a => a.Key.X == 0).Count() : 0;
        foreach (var cell in _cells.ToArray())
        {
            if (HeaderRow && cell.Key.X == 0) continue;

            Remove(cell.Key.X, cell.Key.Y, false);

            if (clearLayoutOptionsForContent)
            {
                _rowLayout.Remove(cell.Key.X);
                _columnLayout.Remove(cell.Key.Y);
            }
        }

        // Adjust maxes
        MaxRow = HeaderRow ? 1 : 0;
        MaxColumn = maxColumn;

        AdjustCellPositionsAfterResize();
        _table.SyncScrollAmountOnResize();
        _table.IsDirty = true;
    }
    #endregion

    #region Internal Methods
    /// <summary>
    /// Get's the cell position on the control based on the row and column
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="rowSize"></param>
    /// <param name="columnSize"></param>
    /// <param name="verticalScrollBarValue"></param>
    /// <param name="horizontalScrollbarValue"></param>
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
    /// <returns></returns>
    internal int GetSizeOrDefault(int index, Layout.LayoutType type)
    {
        return type switch
        {
            Layout.LayoutType.Column => _columnLayout.TryGetValue(index, out Layout? layout) ? layout.Size : _table.DefaultCellSize.X,
            Layout.LayoutType.Row => _rowLayout.TryGetValue(index, out Layout? layout) ? layout.Size : _table.DefaultCellSize.Y,
            _ => throw new NotSupportedException("Invalid layout type."),
        };
    }

    internal Table.Cell? GetIfExists(int row, int col, bool useRealRowAndCols)
    {
        Table.Cell? chosenCell = null;
        foreach (var cell in _cells)
        {
            if (useRealRowAndCols)
            {
                if (cell.Value.Row == row && cell.Value.Column == col)
                    chosenCell = cell.Value;
                continue;
            }

            if (cell.Value._row == row && cell.Value._column == col)
            {
                chosenCell = cell.Value;
            }
        }
        return chosenCell;
    }

    private Table.Cell GetOrCreateCell(int row, int col)
    {
        if (!_cells.TryGetValue((row, col), out Table.Cell? cell))
        {
            cell = new Table.Cell(row, col, _table, string.Empty)
            {
                Position = GetCellPosition(row, col, out _, out _,
                    _table.IsVerticalScrollBarVisible ? _table.StartRenderYPos : 0, _table.IsHorizontalScrollBarVisible ? _table.StartRenderXPos : 0)
            };

            _cells[(row, col)] = cell;
            if (MaxRow < row)
                MaxRow = row;
            if (MaxColumn < col)
                MaxColumn = col;
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

        var layoutDict = type == Layout.LayoutType.Column ? _columnLayout : _rowLayout;
        var defaultSize = type == Layout.LayoutType.Column ? _table.DefaultCellSize.X : _table.DefaultCellSize.Y;

        indexSize = layoutDict.TryGetValue(startIndex, out Layout? layout) ? layout.Size : defaultSize;

        // If entire row or column is hidden then skip it
        _hiddenIndexes.TryGetValue(type, out var indexes);
        if (indexes != null && indexes.Contains(startIndex))
            indexSize = 0;

        while (startIndex < index)
        {
            controlIndex += indexSize;
            startIndex++;

            indexSize = layoutDict.TryGetValue(startIndex, out layout) ? layout.Size : defaultSize;
            // If entire row or column is hidden then skip it
            _hiddenIndexes.TryGetValue(type, out indexes);
            if (indexes != null && indexes.Contains(startIndex) && startIndex < index)
                indexSize = 0;
        }
        return controlIndex;
    }

    internal int GetIndexAtCellPosition(int pos, Layout.LayoutType type, out int indexPos)
    {
        int total = type == Layout.LayoutType.Row ? (_table.Cells.MaxRow + 1) : (_table.Cells.MaxColumn + 1);
        Dictionary<int, Layout> layoutDict = type == Layout.LayoutType.Row ? _rowLayout : _columnLayout;
        int defaultSize = type == Layout.LayoutType.Row ? _table.DefaultCellSize.Y : _table.DefaultCellSize.X;
        int totalSize = 0;
        for (int i = 0; i < total; i++)
        {
            int indexSize = layoutDict.TryGetValue(i, out Layout? layout) ? layout.Size : defaultSize;
            _hiddenIndexes.TryGetValue(type, out var indexes);
            if (indexes != null && indexes.Contains(i))
                indexSize = 0;
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

    private void SetCell(int row, int col, Table.Cell? cell)
    {
        if (cell == null)
        {
            if (_cells.Remove((row, col)))
            {
                MaxRow = _cells.Count == 0 ? 0 : _cells.Values.Max(a => a.Row);
                MaxColumn = _cells.Count == 0 ? 0 : _cells.Values.Max(a => a.Column);
                _table._checkScrollBarVisibility = true;
                _table.IsDirty = true;
            }
            return;
        }

        _cells[(row, col)] = cell;
        if (MaxRow < row)
            MaxRow = row;
        if (MaxColumn < col)
            MaxColumn = col;
        _table._checkScrollBarVisibility = true;
        _table.IsDirty = true;
    }

    internal void AdjustCellPositionsAfterResize()
    {
        foreach (KeyValuePair<Point, Table.Cell> cell in _cells)
            cell.Value.Position = GetCellPosition(cell.Value.Row, cell.Value.Column, out _, out _,
                _table.IsVerticalScrollBarVisible ? _table.StartRenderYPos : 0, _table.IsHorizontalScrollBarVisible ? _table.StartRenderXPos : 0);
        _table._checkScrollBarVisibility = true;
        _table.IsDirty = true;
    }
    /// <inheritdoc/>
    public IEnumerator<Table.Cell> GetEnumerator()
    {
        return _cells.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion

    private void Remove(int row, int column, bool adjustMaxRowColumns)
    {
        int prev = _cells.Count;
        _ = _cells.Remove((row, column));
        if (prev != _cells.Count)
        {
            if (adjustMaxRowColumns)
            {
                MaxRow = _cells.Count == 0 ? 0 : _cells.Values.Max(a => a.Row);
                MaxColumn = _cells.Count == 0 ? 0 : _cells.Values.Max(a => a.Column);
            }
            AdjustCellPositionsAfterResize();
            _table.SyncScrollAmountOnResize();
            _table.IsDirty = true;
        }
    }

    /// <summary>
    /// Defines the layout for a row or a column defined in <see cref="Table.Cells"/>
    /// </summary>
    public class Layout
    {
        private int _size;
        /// <summary>
        /// The size of the row or column
        /// </summary>
        public int Size
        {
            get => _size;
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

        /// <summary>
        /// The foreground color used by the row or column
        /// </summary>
        public Color? Foreground { get; set; }
        /// <summary>
        /// The background color used by the row or column
        /// </summary>
        public Color? Background { get; set; }

        private Table.Cell.Options? _settings;
        /// <summary>
        /// The setting options used by the row or column
        /// </summary>
        public Table.Cell.Options Settings
        {
            get => _settings ??= new Table.Cell.Options(_table);
            set
            {
                if (value == null) return;
                (_settings ??= new Table.Cell.Options(_table)).CopyFrom(value);
            }
        }

        /// <summary>
        /// True if the Settings property has been accessed before.
        /// </summary>
        internal bool HasCustomSettings => _settings != null;

        private readonly Table _table;
        private readonly LayoutType _layoutType;
        private readonly int _index;

        internal Layout(Table table, int index, LayoutType type)
        {
            _table = table;
            _layoutType = type;
            _index = index;
            Size = type == LayoutType.Column ? table.DefaultCellSize.X : table.DefaultCellSize.Y;
        }


        /// <summary>
        /// Removes this entire layout from the table.
        /// </summary>
        public void Remove()
        {
            var layoutDict = _layoutType == LayoutType.Row ? _table.Cells._rowLayout : _table.Cells._columnLayout;
            layoutDict.Remove(_index);
            _table.IsDirty = true;
        }

        /// <summary>
        /// Set a default layout to be used for each new cell
        /// </summary>
        /// <param name="size"></param>
        /// <param name="foreground"></param>
        /// <param name="background"></param>
        /// <param name="settings"></param>
        public void SetLayout(int? size = null, Color? foreground = null, Color? background = null, Table.Cell.Options? settings = null)
        {
            int prevSize = _size;
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
        internal void SetLayoutInternal(int? size = null, Color? foreground = null, Color? background = null, Table.Cell.Options? settings = null)
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

        /// <summary>
        /// Defines several visual modes
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// Only a single cell will be visualized
            /// </summary>
            Single = 0,
            /// <summary>
            /// Nothing will be visualized
            /// </summary>
            None,
            /// <summary>
            /// The entire row of the cell will be visualized
            /// </summary>
            EntireRow,
            /// <summary>
            /// The entire column of the cell will be visualized
            /// </summary>
            EntireColumn
        }

        /// <summary>
        /// An enumerable range that contains the layouts of all the rows and columns defined by the size of the range method within <see cref="Table.Cells"/>
        /// </summary>
        public class RangeEnumerable : IEnumerable<Layout>
        {
            private readonly IEnumerable<Layout> _layouts;

            internal RangeEnumerable(IEnumerable<Layout> layouts)
            {
                _layouts = layouts;
            }

            /// <summary>
            /// Sets the layout of all the columns and rows for the given params
            /// </summary>
            /// <param name="size"></param>
            /// <param name="foreground"></param>
            /// <param name="background"></param>
            /// <param name="settings"></param>
            public void SetLayout(int? size = null, Color? foreground = null, Color? background = null, Table.Cell.Options? settings = null)
            {
                foreach (Layout layout in _layouts)
                    layout.SetLayout(size, foreground, background, settings);
            }

            /// <inheritdoc/>
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

/// <summary>
/// Extensions used for the <see cref="Table"/>
/// </summary>
public static class TableExtensions
{
    /// <summary>
    /// Returns a range of cells that fits the given parameter values.
    /// </summary>
    /// <param name="cells"></param>
    /// <param name="startRow"></param>
    /// <param name="startCol"></param>
    /// <param name="endRow"></param>
    /// <param name="endCol"></param>
    /// <returns></returns>
    public static IEnumerable<Table.Cell> Range(this Cells cells, int startRow, int startCol, int endRow, int endCol)
    {
        int width = endCol - startCol + 1;
        int height = endRow - startRow + 1;
        for (int x = startCol; x < startCol + width; x++)
        {
            for (int y = startRow; y < startRow + height; y++)
            {
                yield return cells[y, x];
            }
        }
    }

    /// <summary>
    /// Executes an action on each cell.
    /// </summary>
    /// <param name="range"></param>
    /// <param name="action"></param>
    public static void ForEach(this IEnumerable<Table.Cell> range, Action<Table.Cell> action)
    {
        foreach (Table.Cell cell in range)
        {
            action(cell);
        }
    }

    /// <summary>
    /// Sets the layout for the cell.
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="foreground"></param>
    /// <param name="background"></param>
    /// <param name="settings"></param>
    public static void SetLayout(this Table.Cell cell, Color? foreground = null, Color? background = null, Table.Cell.Options? settings = null)
    {
        cell._table.Cells.Column(cell.Column).SetLayout(null, foreground, background, settings);
        cell._table.Cells.Row(cell.Row).SetLayout(null, foreground, background, settings);
        cell._table.IsDirty = true;
    }

    /// <summary>
    /// Resizes the entire column and row to the specified sizes.
    /// If no sizes are specified for both row and column, the cell will be reset to the default size.
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="rowSize"></param>
    /// <param name="columnSize"></param>
    public static void Resize(this Table.Cell cell, int? rowSize = null, int? columnSize = null)
    {
        if (rowSize == null && columnSize == null)
        {
            rowSize = cell._table.DefaultCellSize.Y;
            columnSize = cell._table.DefaultCellSize.X;
        }

        cell._table.Cells.Column(cell.Column).SetLayoutInternal(columnSize);
        cell._table.Cells.Row(cell.Row).SetLayoutInternal(rowSize);
        cell._table.Cells.AdjustCellPositionsAfterResize();
        cell._table.SyncScrollAmountOnResize();
        cell._table.IsDirty = true;
    }

    /// <summary>
    /// Sets the cell as the selected cell.
    /// </summary>
    /// <param name="cell"></param>
    public static void Select(this Table.Cell cell)
    {
        cell._table.Cells.Select(cell.Row, cell.Column);
    }

    /// <summary>
    /// Incase this cell is the selected cell, it will unselect it.
    /// </summary>
    /// <param name="cell"></param>
    public static void Deselect(this Table.Cell cell)
    {
        cell._table.Cells.Deselect();
    }

    /// <summary>
    /// Get the layout for the given columns.
    /// </summary>
    /// <param name="cells"></param>
    /// <param name="columns"></param>
    /// <returns></returns>
    public static Cells.Layout.RangeEnumerable Column(this Cells cells, params int[] columns)
    {
        IEnumerable<Cells.Layout> layouts = columns.Select(a => cells.Column(a));
        return new Cells.Layout.RangeEnumerable(layouts);
    }

    /// <summary>
    /// Get the layout for the given rows.
    /// </summary>
    /// <param name="cells"></param>
    /// <param name="rows"></param>
    /// <returns></returns>
    public static Cells.Layout.RangeEnumerable Row(this Cells cells, params int[] rows)
    {
        IEnumerable<Cells.Layout> layouts = rows.Select(a => cells.Row(a));
        return new Cells.Layout.RangeEnumerable(layouts);
    }

    /// <summary>
    /// Removes the cell from its table.
    /// </summary>
    /// <param name="cell"></param>
    public static void Remove(this Table.Cell cell)
    {
        cell._table.Cells.Remove(cell.Row, cell.Column);
    }
}
