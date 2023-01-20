﻿using SadConsole;
using SadConsole.UI.Controls;
using SadConsole.UI.Themes;
using SCControlsExtended.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace SCControlsExtended.Themes
{
    public class TableTheme : ThemeBase
    {
        /// <summary>
        /// When true, only uses <see cref="ThemeStates.Normal"/> for drawing.
        /// </summary>
        [DataMember]
        public bool UseNormalStateOnly { get; set; } = true;

        /// <summary>
        /// The current appearance based on the control state.
        /// </summary>
        [DataMember]
        public ColoredGlyph Appearance { get; protected set; }

        /// <inheritdoc />
        public override void UpdateAndDraw(ControlBase control, TimeSpan time)
        {
            if (!control.IsDirty || control is not Table table)
                return;

            RefreshTheme(control.FindThemeColors(), control);

            if (!UseNormalStateOnly)
                Appearance = ControlThemeState.GetStateAppearance(control.State);
            else
                Appearance = ControlThemeState.Normal;

            // Draw the basic table surface foreground and background
            control.Surface.Fill(Appearance.Foreground, Appearance.Background, Appearance.Glyph);

            var columns = table.Width;
            var rows = table.Height;
            int rowIndex = 0;
            for (int row = 0; row < rows; row++)
            {
                int colIndex = 0;
                int fullRowSize = 0;
                for (int col = 0; col < columns; col++)
                {
                    _ = table.Cells.GetCellPosition(row, col, out fullRowSize, out int columnSize);

                    var cell = table.Cells.GetIfExists(rowIndex, colIndex);
                    if (table.DrawOnlyIndexedCells && cell == null) continue;

                    cell ??= new Cells.Cell(row, col, table, string.Empty)
                    {
                        Position = table.Cells.GetCellPosition(row, col, out _, out _)
                    };

                    AdjustControlSurface(table, cell, GetCustomStateAppearance(table, cell));
                    PrintText(table, cell);

                    col += columnSize - 1;
                    colIndex++;
                }

                row += fullRowSize - 1;
                rowIndex++;
            }

            control.IsDirty = false;
        }

        private ColoredGlyph GetCustomStateAppearance(Table table, Cells.Cell cell)
        {
            var mouseOverCell = table.CurrentMouseCell != null &&
                table.CurrentMouseCell.ColumnIndex == cell.ColumnIndex &&
                table.CurrentMouseCell.RowIndex == cell.RowIndex;

            if (table.SelectedCell != null && cell.Equals(table.SelectedCell))
                return ControlThemeState.Selected;
            if (mouseOverCell)
                return ControlThemeState.MouseOver;
            return null;
        }

        public override void Attached(ControlBase control)
        {
            if (control is not Table)
                throw new Exception("Added TableTheme to a control that isn't a Table.");

            base.Attached(control);
        }

        private void AdjustControlSurface(Table table, Cells.Cell cell, ColoredGlyph customStateAppearance)
        {
            var width = table.Cells.Column(cell.ColumnIndex).Size;
            var height = table.Cells.Row(cell.RowIndex).Size;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int colIndex = cell.Position.X + x;
                    int rowIndex = cell.Position.Y + y;
                    table.Surface.SetForeground(colIndex, rowIndex, customStateAppearance != null ? customStateAppearance.Foreground : cell.Foreground);
                    table.Surface.SetBackground(colIndex, rowIndex, customStateAppearance != null ? customStateAppearance.Background : cell.Background);
                }
            }
        }

        private static void PrintText(Table table, Cells.Cell cell)
        {
            if (cell.Text == null) return;

            var width = table.Cells.Column(cell.ColumnIndex).Size;
            var height = table.Cells.Row(cell.RowIndex).Size;

            // Split the character array into parts based on cell width
            var splittedTextArray = Split(cell.Text.ToCharArray(), width).ToArray();
            for (int y = 0; y < height; y++)
            {
                // Don't go out of bounds of the cell height
                if (splittedTextArray.Length <= y)
                    break;

                // Print each array to the correct y index
                var textArr = splittedTextArray[y];
                int index = 0;
                foreach (var character in textArr)
                {
                    table.Surface.SetGlyph(cell.Position.X + index++, cell.Position.Y + y, character);
                }
            }
        }

        private static IEnumerable<IEnumerable<T>> Split<T>(T[] array, int size)
        {
            for (var i = 0; i < (float)array.Length / size; i++)
            {
                yield return array.Skip(i * size).Take(size);
            }
        }

        /// <inheritdoc />
        public override ThemeBase Clone() => new TableTheme()
        {
            ControlThemeState = ControlThemeState.Clone(),
            UseNormalStateOnly = UseNormalStateOnly,
        };
    }
}
