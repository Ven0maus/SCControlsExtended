using SadConsole;
using SadConsole.UI;
using SadConsole.UI.Controls;
using SadConsole.UI.Themes;
using SCControlsExtended.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCControlsExtended.Themes
{
    public class TableTheme : ThemeBase
    {
        /// <summary>
        /// The appearance of the scrollbar used by the table control.
        /// </summary>
        public ScrollBarTheme ScrollBarTheme { get; set; }

        public TableTheme(ScrollBarTheme scrollBarTheme)
        {
            ScrollBarTheme = scrollBarTheme;
        }

        public override void Attached(ControlBase control)
        {
            if (control is not Table)
                throw new Exception("Added TableTheme to a control that isn't a Table.");

            base.Attached(control);
        }

        public override void RefreshTheme(Colors colors, ControlBase control)
        {
            base.RefreshTheme(colors, control);

            if (control is not Table table) return;

            var scrollBars = new[] { table.VerticalScrollBar, table.HorizontalScrollBar }; 
            foreach (var scrollBar in scrollBars )
            {
                if (scrollBar != null)
                {
                    scrollBar.Theme = ScrollBarTheme;
                    ScrollBarTheme?.RefreshTheme(_colorsLastUsed, scrollBar);
                }
            }
        }

        /// <summary>
        /// Shows the scroll bar when there are too many items to display; otherwise, hides it.
        /// </summary>
        /// <param name="table">Reference to the listbox being processed.</param>
        private static bool ShowHideScrollBar(Table table, ScrollBar scrollBar)
        {
            // process the scroll bar
            int scrollbarItems = GetScrollBarItems(table, scrollBar.Orientation);
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

        private static int GetScrollBarItems(Table table, Orientation orientation)
        {
            var order = orientation == Orientation.Vertical ?
                table.Cells.OrderByDescending(a => a.Row).GroupBy(a => a.Row) :
                table.Cells.OrderByDescending(a => a.Column).GroupBy(a => a.Column);
            var lastIndexes = order.Select(a => a.Key);
            int indexes = 0;
            foreach (var index in lastIndexes)
            {
                // + 1 because its 0 based
                int totalSize = ((index == 0 ? 1 : index) + 1) * table.Cells.GetSizeOrDefault(index,
                    orientation == Orientation.Vertical ? Cells.Layout.LayoutType.Row : Cells.Layout.LayoutType.Column);
                if (totalSize > (orientation == Orientation.Vertical ? table.Height : table.Width))
                {
                    indexes++;
                }
            }
            return indexes;
        }

        private void SetScrollBarPropertiesOnTable(Table table, ScrollBar scrollBar, int maxRowsHeight, int maxColumnsWidth)
        {
            if (scrollBar != null)
            {
                var total = scrollBar.Orientation == Orientation.Vertical ?
                    (maxRowsHeight >= table.Height ? table.Height : maxRowsHeight) :
                    (maxColumnsWidth >= table.Width ? table.Width : maxColumnsWidth);
                var max = scrollBar.Orientation == Orientation.Vertical ? table.Height : table.Width;

                if (scrollBar.Orientation == Orientation.Vertical)
                {
                    table.VisibleRowsTotal = total;
                    table.VisibleRowsMax = max;
                }
                else
                {
                    table.VisibleColumnsTotal = total;
                    table.VisibleColumnsMax = max;
                }
            }
        }
        
        private void SetScrollBarVisibility(Table table, int maxRowsHeight, int maxColumnsWidth)
        {
            if (table._checkScrollBarVisibility)
            {
                if (table.VerticalScrollBar != null)
                {
                    table.IsVerticalScrollBarVisible = ShowHideScrollBar(table, table.VerticalScrollBar);
                    SetScrollBarPropertiesOnTable(table, table.VerticalScrollBar, maxRowsHeight, maxColumnsWidth);
                }
                if (table.HorizontalScrollBar != null)
                {
                    table.IsHorizontalScrollBarVisible = ShowHideScrollBar(table, table.HorizontalScrollBar);
                    SetScrollBarPropertiesOnTable(table, table.HorizontalScrollBar, maxRowsHeight, maxColumnsWidth);
                }
            }
        }

        /// <inheritdoc />
        public override void UpdateAndDraw(ControlBase control, TimeSpan time)
        {
            if (control is not Table table || !table.IsDirty)
                return;

            RefreshTheme(control.FindThemeColors(), control);

            // Draw the basic table surface foreground and background, and clear the glyphs
            control.Surface.Fill(table.DefaultForeground, table.DefaultBackground, 0);

            var maxColumnsWidth = table.GetMaxColumnsBasedOnColumnSizes();
            var maxRowsHeight = table.GetMaxRowsBasedOnRowSizes();

            SetScrollBarVisibility(table, maxRowsHeight, maxColumnsWidth);

            var columns = maxColumnsWidth;
            var rows = maxRowsHeight;
            int rowIndex = table.IsVerticalScrollBarVisible ? table.VerticalScrollBar.Value : 0;
            for (int row = 0; row < rows; row++)
            {
                int colIndex = table.IsHorizontalScrollBarVisible ? table.HorizontalScrollBar.Value : 0;
                int fullRowSize = 0;
                for (int col = 0; col < columns; col++)
                {
                    var verticalScrollBarValue = table.IsVerticalScrollBarVisible ? table.VerticalScrollBar.Value : 0;
                    var horizontalScrollBarValue = table.IsHorizontalScrollBarVisible ? table.HorizontalScrollBar.Value : 0;
                    var cellPosition = table.Cells.GetCellPosition(rowIndex, colIndex, out fullRowSize, out int columnSize, 
                        verticalScrollBarValue, horizontalScrollBarValue);

                    col += columnSize - 1;

                    // Don't attempt to render off-screen rows/columns
                    if (cellPosition.X > table.Width || cellPosition.Y > table.Height)
                    {
                        colIndex++;
                        continue;
                    }

                    var cell = table.Cells.GetIfExists(rowIndex, colIndex);
                    if (table.DrawOnlyIndexedCells && cell == null)
                    {
                        colIndex++;
                        continue;
                    }

                    cell ??= new Table.Cell(rowIndex, colIndex, table, string.Empty)
                    {
                        Position = cellPosition
                    };

                    AdjustControlSurface(table, cell, GetCustomStateAppearance(table, cell));
                    PrintText(table, cell);

                    colIndex++;
                }

                row += fullRowSize - 1;
                rowIndex++;
            }

            control.IsDirty = false;
        }

        private ColoredGlyph GetCustomStateAppearance(Table table, Table.Cell cell)
        {
            if (!cell.Settings.IsVisible || !cell.Settings.Interactable) return null;

            if (cell.Settings.Selectable && table.SelectedCell != null)
            {
                switch (cell.Settings.SelectionMode)
                {
                    case Cells.Layout.Mode.Single:
                        if (!cell.Equals(table.SelectedCell)) break;
                        return ControlThemeState.Selected;
                    case Cells.Layout.Mode.EntireRow:
                        if (cell.Row != table.SelectedCell.Row) break;
                        return ControlThemeState.Selected;
                    case Cells.Layout.Mode.EntireColumn:
                        if (cell.Column != table.SelectedCell.Column) break;
                        return ControlThemeState.Selected;
                    case Cells.Layout.Mode.None:
                        break;
                }
            }

            switch (cell.Settings.HoverMode)
            {
                case Cells.Layout.Mode.Single:
                    if (table.CurrentMouseCell == null || !cell.Equals(table.CurrentMouseCell)) break;
                    return ControlThemeState.MouseOver;
                case Cells.Layout.Mode.EntireRow:
                    if (table.CurrentMouseCell == null || table.CurrentMouseCell.Row != cell.Row) break;
                    return ControlThemeState.MouseOver;
                case Cells.Layout.Mode.EntireColumn:
                    if (table.CurrentMouseCell == null || table.CurrentMouseCell.Column != cell.Column) break;
                    return ControlThemeState.MouseOver;
                case Cells.Layout.Mode.None:
                    break;
            }
            return null;
        }

        private static void AdjustControlSurface(Table table, Table.Cell cell, ColoredGlyph customStateAppearance)
        {
            var width = table.Cells.GetSizeOrDefault(cell.Column, Cells.Layout.LayoutType.Column);
            var height = table.Cells.GetSizeOrDefault(cell.Row, Cells.Layout.LayoutType.Row);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int colIndex = cell.Position.X + x;
                    int rowIndex = cell.Position.Y + y;
                    if (!table.Surface.IsValidCell(colIndex, rowIndex)) continue;
                    table.Surface[colIndex, rowIndex].IsVisible = cell.Settings.IsVisible;
                    table.Surface.SetForeground(colIndex, rowIndex, customStateAppearance != null ? customStateAppearance.Foreground : cell.Foreground);
                    table.Surface.SetBackground(colIndex, rowIndex, customStateAppearance != null ? customStateAppearance.Background : cell.Background);
                }
            }
        }

        private static void PrintText(Table table, Table.Cell cell)
        {
            if (cell.Text == null || !cell.Settings.IsVisible) return;

            var width = table.Cells.GetSizeOrDefault(cell.Column, Cells.Layout.LayoutType.Column);
            var height = table.Cells.GetSizeOrDefault(cell.Row, Cells.Layout.LayoutType.Row);

            // Handle alignments
            var vAlign = cell.Settings.VerticalAlignment;
            var hAlign = cell.Settings.HorizontalAlignment;
            GetTotalCellSize(cell, width, height, out int totalWidth, out int totalHeight);

            // Set the amount of characters to split on for wrapping
            var maxCharsPerLine = cell.Settings.MaxCharactersPerLine ?? width;
            if (maxCharsPerLine > width)
                maxCharsPerLine = width;

            // Split the character array into parts based on cell width
            var splittedTextArray = Split(cell.Text.ToCharArray(), maxCharsPerLine).ToArray();
            for (int y = 0; y < height; y++)
            {
                // Don't go out of bounds of the cell height
                if (splittedTextArray.Length <= y)
                    break;

                // Print each array to the correct y index
                // Remove spaces in the front on the newline
                var textArr = splittedTextArray[y].SkipWhile(a => a == ' ').ToArray();
                var startPosX = GetHorizontalAlignment(hAlign, totalWidth, textArr);
                var startPosY = GetVerticalAlignment(vAlign, totalHeight, splittedTextArray);

                int index = 0;
                foreach (var character in textArr)
                {
                    table.Surface.SetGlyph(startPosX + cell.Position.X + index++, startPosY + cell.Position.Y + y, character);
                }
            }
        }

        private static void GetTotalCellSize(Table.Cell cell, int width, int height, out int totalWidth, out int totalHeight)
        {
            int startX = cell.Position.X;
            int startY = cell.Position.Y;
            int endX = cell.Position.X + width;
            int endY = cell.Position.Y + height;
            totalWidth = endX - startX;
            totalHeight = endY - startY;
        }

        private static int GetHorizontalAlignment(Table.Cell.Options.HorizontalAlign hAlign, int totalWidth, char[] textArr)
        {
            int startPosX = 0;
            switch (hAlign)
            {
                case Table.Cell.Options.HorizontalAlign.Left:
                    startPosX = 0;
                    break;
                case Table.Cell.Options.HorizontalAlign.Center:
                    startPosX = (totalWidth - textArr.Length) / 2;
                    break;
                case Table.Cell.Options.HorizontalAlign.Right:
                    startPosX = totalWidth - textArr.Length;
                    break;
            }
            return startPosX;
        }

        private static int GetVerticalAlignment(Table.Cell.Options.VerticalAlign vAlign, int totalHeight, IEnumerable<char>[] textArrs)
        {
            int position = 0;
            switch (vAlign)
            {
                case Table.Cell.Options.VerticalAlign.Top:
                    position = 0;
                    break;
                case Table.Cell.Options.VerticalAlign.Center:
                    position = (totalHeight - textArrs.Length) / 2;
                    break;
                case Table.Cell.Options.VerticalAlign.Bottom:
                    position = totalHeight - textArrs.Length;
                    break;
            }
            return position;
        }

        private static IEnumerable<IEnumerable<T>> Split<T>(T[] array, int size)
        {
            for (var i = 0; i < (float)array.Length / size; i++)
            {
                yield return array.Skip(i * size).Take(size);
            }
        }

        /// <inheritdoc />
        public override ThemeBase Clone() => new TableTheme((ScrollBarTheme)ScrollBarTheme.Clone())
        {
            ControlThemeState = ControlThemeState.Clone(),
        };
    }
}
