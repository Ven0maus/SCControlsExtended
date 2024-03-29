﻿using SadConsole;
using SadConsole.UI;
using SadRogue.Primitives;
using SCControlsExtended.Controls;
using System;

namespace SCControlsExtended.Demo.ExampleConsoles
{
    internal class ExcelWindow : ControlsConsole
    {
        private readonly Table _table;

        public ExcelWindow(int width, int height) : base(width + 1, height + 1)
        {
            _table = new Table(Width - 1, Height - 1, 10, 3);
            _table.Cells.HeaderRow = true;
            _table.SetThemeColors(Colors.CreateSadConsoleBlue());
            _table.DefaultForeground = Color.Black;
            _table.DefaultBackground = Color.Lerp(Color.WhiteSmoke, Color.Black, 0.075f);
            _table.SetupScrollBar(Orientation.Vertical, Height, new Point(Width - 1, 0));
            _table.SetupScrollBar(Orientation.Horizontal, Width - 1, new Point(0, Height - 1));

            // Don't render row 2 at all, show feature where other rows takes its place
            // Good for filtering features
            _table.Cells.Row(2, false);

            // Only add layout and let the console draw the rest
            _table.OnDrawFakeCell += DrawFakeCell;
            _table.DrawFakeCells = true;

            Controls.Add(_table);

            AdjustTable();
        }

        private void DrawFakeCell(object sender, Table.CellEventArgs args)
        {
            var cell = args.Cell;

            // Setting the headers
            bool isHeader = cell.Row == 0 || cell.Column == 0;
            if (isHeader)
            {
                // Skip the first row and column
                if (cell.Row == 0 && cell.Column == 0) 
                    return;

                cell.Value = cell.Row == 0 ? GetExcelColumnName(cell.Column) : cell.Row.ToString();
                cell.Settings.HorizontalAlignment = Table.Cell.Options.HorizontalAlign.Center;
                cell.Settings.VerticalAlignment = Table.Cell.Options.VerticalAlign.Center;
                cell.Settings.Interactable = false;
                return;
            }

            // Setting the inner cells
            cell.Foreground = Color.Lerp(Color.WhiteSmoke, Color.Black, 0.3f);
            cell.Background = Color.Lerp(Color.WhiteSmoke, Color.Black, 0.075f);
            cell.Value = GetExcelColumnName(cell.Column) + cell.Row;
            cell.Settings.Interactable = true;
        }

        private void AdjustTable()
        {
            _table.Cells.Row(0).SetLayout(background: Color.Lerp(Color.WhiteSmoke, Color.Black, 0.25f));
            _table.Cells.Column(0).SetLayout(background: Color.Lerp(Color.WhiteSmoke, Color.Black, 0.25f));

            // Set column
            _table.Cells[0, 0].Value = ">";
            _table.Cells[0, 0].Settings.HorizontalAlignment = Table.Cell.Options.HorizontalAlign.Center;
            _table.Cells[0, 0].Settings.VerticalAlignment = Table.Cell.Options.VerticalAlign.Center;
            _table.Cells[0, 0].Settings.Interactable = false;

            // Generate fake data
            for (int i=1; i < 100; i++)
            {
                _table.Cells[i, 1].Settings.UseFakeLayout = true;
                _table.Cells[1, i].Settings.UseFakeLayout = true;
            }
        }

        private static string GetExcelColumnName(int index)
        {
            int d = index;
            string name = "";
            int mod;

            while (d > 0)
            {
                mod = (d - 1) % 26;
                name = Convert.ToChar('A' + mod).ToString() + name;
                d = (d - mod) / 26;
            }

            return name;
        }
    }
}
