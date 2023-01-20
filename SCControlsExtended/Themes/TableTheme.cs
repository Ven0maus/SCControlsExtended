using SadConsole;
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

            // Define based on table width/height and DefaultCellSize how many columns/rows should be added
            var columns = table.Width / table.DefaultCellSize.X;
            var rows = table.Height / table.DefaultCellSize.Y;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    var cell = table.Cells.GetIfExists(row, col);
                    if (table.DrawOnlyIndexedCells && cell == null) continue;

                    if (cell == null)
                    {
                        cell = new Cells.Cell(row, col, table, string.Empty)
                        {
                            Position = table.Cells.GetCellPosition(row, col)
                        };
                    }

                    var mouseOverCell = table.MousedOverCellPosition != null &&
                        table.MousedOverCellPosition.Value.X == cell.Position.X &&
                        table.MousedOverCellPosition.Value.Y == cell.Position.Y;

                    AdjustControlSurface(control, cell, mouseOverCell);
                    PrintText(control, cell);
                }
            }

            control.IsDirty = false;
        }

        public override void Attached(ControlBase control)
        {
            if (control is not Table)
                throw new Exception("Added TableTheme to a control that isn't a Table.");

            base.Attached(control);
        }

        private void AdjustControlSurface(ControlBase control, Cells.Cell cell, bool mouseOver = false)
        {
            for (int x = 0; x < cell.Width; x++)
            {
                for (int y = 0; y < cell.Height; y++)
                {
                    int colIndex = cell.Position.X + x;
                    int rowIndex = cell.Position.Y + y;
                    control.Surface.SetForeground(colIndex, rowIndex, cell.Foreground);
                    control.Surface.SetBackground(colIndex, rowIndex, !mouseOver ? cell.Background : ControlThemeState.MouseOver.Background);
                }
            }
        }

        private static void PrintText(ControlBase control, Cells.Cell cell)
        {
            if (cell.Text == null) return;

            // Split the character array into parts based on cell width
            var splittedTextArray = Split(cell.Text.ToCharArray(), cell.Width).ToArray();
            for (int y = 0; y < cell.Height; y++)
            {
                // Don't go out of bounds of the cell height
                if (splittedTextArray.Length <= y)
                    break;

                // Print each array to the correct y index
                var textArr = splittedTextArray[y];
                int index = 0;
                foreach (var character in textArr)
                {
                    control.Surface.SetGlyph(cell.Position.X + index++, cell.Position.Y + y, character);
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
