using SCControlsExtended.Controls;
using System.Collections.Generic;
using static SCControlsExtended.Controls.Cells.Cell;
using System.Data.Common;
using SadRogue.Primitives;
using System;

namespace SCControlsExtended.ControlExtensions
{
    public static class TableExtensions
    {
        /// <summary>
        /// Returns a range of cells that fits the given parameter values.
        /// </summary>
        /// <param name="startRow"></param>
        /// <param name="startCol"></param>
        /// <param name="endRow"></param>
        /// <param name="endCol"></param>
        /// <returns></returns>
        public static IEnumerable<Cells.Cell> Range(this Cells cells, int startRow, int startCol, int endRow, int endCol)
        {
            var width = endCol - startCol + 1;
            var height = endRow - startRow + 1;
            for (int x = startCol; x < startCol + width; x++)
            {
                for (int y = startRow; y < startRow + height; y++)
                {
                    yield return cells[y, x];
                }
            }
        }

        /// <summary>
        /// Executes an action on each cell
        /// </summary>
        /// <param name="range"></param>
        /// <param name="action"></param>
        public static void ForEach(this IEnumerable<Cells.Cell> range, Action<Cells.Cell> action)
        {
            foreach (var cell in range)
            {
                action(cell);
            }
        }

        /// <summary>
        /// Sets the layout for the cell
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="foreground"></param>
        /// <param name="background"></param>
        /// <param name="settings"></param>
        public static void SetLayout(this Cells.Cell cell, Color? foreground = null, Color? background = null, Options settings = null)
        {
            cell.Table.Cells.Column(cell.Column).SetLayout(null, foreground, background, settings);
            cell.Table.Cells.Row(cell.Row).SetLayout(null, foreground, background, settings);
            cell.Table.IsDirty = true;
        }

        /// <summary>
        /// Resizes the entire column and row to the specified sizes
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="rowSize"></param>
        /// <param name="columnSize"></param>
        public static void Resize(this Cells.Cell cell, int? rowSize, int? columnSize = null)
        {
            cell.Table.Cells.Column(cell.Column).SetLayoutInternal(columnSize);
            cell.Table.Cells.Row(cell.Row).SetLayoutInternal(rowSize);
            if (rowSize != null || columnSize != null)
                cell.Table.Cells.AdjustCellsAfterResize();
            cell.Table.IsDirty = true;
        }
    }
}
