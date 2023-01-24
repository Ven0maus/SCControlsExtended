﻿using SadRogue.Primitives;
using SCControlsExtended.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public static IEnumerable<Table.Cell> Range(this Cells cells, int startRow, int startCol, int endRow, int endCol)
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
        /// Executes an action on each cell.
        /// </summary>
        /// <param name="range"></param>
        /// <param name="action"></param>
        public static void ForEach(this IEnumerable<Table.Cell> range, Action<Table.Cell> action)
        {
            foreach (var cell in range)
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
        public static void SetLayout(this Table.Cell cell, Color? foreground = null, Color? background = null, Table.Cell.Options settings = null)
        {
            cell.Table.Cells.Column(cell.Column).SetLayout(null, foreground, background, settings);
            cell.Table.Cells.Row(cell.Row).SetLayout(null, foreground, background, settings);
            cell.Table.IsDirty = true;
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
                rowSize = cell.Table.DefaultCellSize.Y;
                columnSize = cell.Table.DefaultCellSize.X;
            }

            cell.Table.Cells.Column(cell.Column).SetLayoutInternal(columnSize);
            cell.Table.Cells.Row(cell.Row).SetLayoutInternal(rowSize);
            cell.Table.Cells.AdjustCellPositionsAfterResize();
            cell.Table.SyncScrollAmountOnResize();
            cell.Table.IsDirty = true;
        }

        /// <summary>
        /// Sets the cell as the selected cell.
        /// </summary>
        /// <param name="cell"></param>
        public static void Select(this Table.Cell cell)
        {
            cell.Table.Cells.Select(cell.Row, cell.Column);
        }

        /// <summary>
        /// Incase this cell is the selected cell, it will unselect it.
        /// </summary>
        /// <param name="cell"></param>
        public static void Deselect(this Table.Cell cell)
        {
            cell.Table.Cells.Deselect();
        }

        /// <summary>
        /// Get the layout for the given columns.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public static Cells.Layout.RangeEnumerable Column(this Cells cells, params int[] columns)
        {
            var layouts = columns.Select(a => cells.Column(a));
            return new Cells.Layout.RangeEnumerable(layouts);
        }

        /// <summary>
        /// Get the layout for the given rows.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public static Cells.Layout.RangeEnumerable Row(this Cells cells, params int[] rows)
        {
            var layouts = rows.Select(a => cells.Row(a));
            return new Cells.Layout.RangeEnumerable(layouts);
        }

        /// <summary>
        /// Removes the cell from its table.
        /// </summary>
        /// <param name="cell"></param>
        public static void Remove(this Table.Cell cell)
        {
            cell.Table.Cells.Remove(cell.Row, cell.Column);
        }
    }
}
