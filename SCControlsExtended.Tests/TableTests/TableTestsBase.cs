using NUnit.Framework;
using SadConsole;
using SadConsole.UI.Themes;
using SCControlsExtended.Controls;
using System.Linq;

namespace SCControlsExtended.Tests.TableTests
{
    internal abstract class TableTestsBase
    {
        protected Table Table { get; set; }
        protected readonly int Width, Height, CellWidth, CellHeight;

        protected TableTestsBase(int width, int height, int cellWidth, int cellHeight)
        {
            Width = width;
            Height = height;
            CellWidth = cellWidth;
            CellHeight = cellHeight;

            Library.Default.SetControlTheme(typeof(Table), new TableTheme(new ScrollBarTheme()));
        }

        [SetUp]
        public virtual void Setup()
        {
            Table = new Table(Width, Height, CellWidth, CellHeight);
        }

        protected static int GetLastVisibleCellSize(Table table, Orientation orientation, bool increment)
        {
            var type = orientation == Orientation.Vertical ?
                Cells.Layout.LayoutType.Row : Cells.Layout.LayoutType.Column;
            var isRowType = type == Cells.Layout.LayoutType.Row;
            var cellGroups = table.Cells.GroupBy(a => isRowType ? a.Row : a.Column);
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
                        if (indexSizeCell >= (isRowType ? table.Height : table.Width))
                            break;
                    }
                    else
                    {
                        // Check if cell position is the next off-screen
                        // >= because it assumes the cell starts at Height, and thats off screen
                        var isPositionOfScreen = isRowType ? indexSizeCell >= table.Height : indexSizeCell >= table.Width;
                        if (!isPositionOfScreen)
                        {
                            // Here it is only > because if the real cell pos is 20 its the ending, so where the next cell starts
                            // which means its not off screen
                            var realCellPosition = isRowType ? (cell.Position.Y + cell.Height) : (cell.Position.X + cell.Width);
                            if (realCellPosition > (isRowType ? table.Height : table.Width))
                            {
                                partialOverlap = true;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    var size = table.Cells.GetSizeOrDefault(indexSizeCell, type);
                    if (partialOverlap)
                    {
                        var overlapAmount = indexSizeCell + size - (isRowType ? table.Height : table.Width);
                        size = overlapAmount;
                    }
                    return increment ? size : -size;
                }
            }
            var defaultSize = type == Cells.Layout.LayoutType.Row ? table.DefaultCellSize.Y : table.DefaultCellSize.X;
            return increment ? defaultSize : -defaultSize;
        }

        protected static int GetMaximumScrollBarItems(Table table, Orientation orientation)
        {
            var indexes = orientation == Orientation.Vertical ?
                table.Cells.GroupBy(a => a.Row) : table.Cells.GroupBy(a => a.Column);
            var orderedIndex = indexes.OrderBy(a => a.Key);

            var layoutType = orientation == Orientation.Vertical ? Cells.Layout.LayoutType.Row : Cells.Layout.LayoutType.Column;
            var maxSize = orientation == Orientation.Vertical ? table.Height : table.Width;
            var totalSize = 0;
            var items = 0;
            foreach (var index in orderedIndex)
            {
                var size = table.Cells.GetSizeOrDefault(index.Key, layoutType);
                totalSize += size;

                if (totalSize > maxSize)
                {
                    items++;
                }
            }
            return items;
        }
    }
}
