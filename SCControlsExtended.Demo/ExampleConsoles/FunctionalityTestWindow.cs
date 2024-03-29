﻿using SadConsole.UI;
using SadRogue.Primitives;
using SCControlsExtended.Controls;
using System;

namespace SCControlsExtended.Demo.ExampleConsoles
{
    internal class FunctionalityTestWindow : ControlsConsole
    {
        private readonly Table _table;

        public FunctionalityTestWindow(int width, int height) : base(width, height)
        {
            // Construct table control
            _table = new Table(100, 40, 10, 2)
            {
                // Set default background color
                DefaultBackground = Color.Lerp(Color.Gray, Color.Black, 0.85f)
            };

            // Set some default theme colors, for selection & hovering appearances
            _table.SetThemeColors(Colors.CreateSadConsoleBlue());
            _table.SetupScrollBar(SadConsole.Orientation.Vertical, Height, new Point(Width - 1, 0));

            // Test events
            _table.OnCellDoubleClick += Table_OnCellDoubleClick;
            _table.OnCellLeftClick += Table_OnCellLeftClick;
            _table.OnCellRightClick += Table_OnCellRightClick;
            _table.SelectedCellChanged += Table_SelectedCellChanged;
            _table.OnCellEnter += Table_OnCellEnter;
            _table.OnCellExit += Table_OnCellExit;

            // Only add layout and let the console draw the rest
            _table.DrawFakeCells = false;

            Controls.Add(_table);

            AdjustTableValues();
        }

        private void AdjustTableValues()
        {
            _table.Cells.Row(0).SetLayout(background: Color.Lerp(Color.Gray, Color.Black, 0.8f));
            _table.Cells.Column(0).SetLayout(background: Color.Lerp(Color.Gray, Color.Black, 0.8f));

            var innerCellColor = Color.Lerp(Color.Gray, Color.Black, 0.6f);
            int col = 1, row = 1;

            // Set column
            _table.Cells[0, 0].Value = "C/R 0";

            // Set column, row texts
            _table.Cells.Range(0, 1, 0, 5).ForEach(cell => cell.Value = "Column " + col++);
            _table.Cells.Range(1, 0, 10, 0).ForEach(cell => cell.Value = "Row " + row++);

            // Set inner cells color
            _table.Cells.Range(1, 1, 10, 5).ForEach(cell => cell.Background = innerCellColor);

            // Hide row 13
            _table.Cells.Row(13, false);

            // Custom cell size
            _table.Cells[5, 7].Value = "Support custom cell sizes and text alignment!";
            _table.Cells[5, 7].Settings.HorizontalAlignment = Table.Cell.Options.HorizontalAlign.Center;
            _table.Cells[5, 7].Settings.VerticalAlignment = Table.Cell.Options.VerticalAlign.Center;
            _table.Cells[5, 7].Resize(7, 20);
            _table.Cells[5, 7].Background = Color.Yellow;
            _table.Cells[5, 7].Foreground = Color.Black;
            _table.Cells[6, 7].Background = Color.Magenta;
            _table.Cells[5, 8].Background = Color.Orange;
            _table.Cells[6, 8].Background = Color.Blue;
            _table.Cells[6, 8].Settings.Interactable = false;
            _table.Cells[7, 8].IsVisible = false;
            _table.Cells[1, 5].Select();
            _table.Cells[9, 0].Resize(1);
            _table.Cells[10, 0].Resize(8);
            for (int i=1; i <= 9; i++)
                _table.Cells[10 + i, 0].Value = (10 + i).ToString();
            _table.Cells[19, 0].Background = Color.Blue;
            _table.Cells[20, 0].Background = Color.Red;
            _table.Cells[20, 0].Value = "hello";
            _table.Cells[20, 0].Resize(10);
            _table.Cells[21, 0].Value = "test";
        }

        private static void Table_OnCellExit(object sender, Table.CellEventArgs e)
        {
            Console.WriteLine($"Exited cell: [{e.Cell.Row},{e.Cell.Column}]");
        }

        private static void Table_OnCellEnter(object sender, Table.CellEventArgs e)
        {
            Console.WriteLine($"Entered cell: [{e.Cell.Row},{e.Cell.Column}]");
        }

        private static void Table_SelectedCellChanged(object sender, Table.CellChangedEventArgs e)
        {
            if (e.PreviousCell != null)
            {
                //e.PreviousCell.Text = string.Empty;
                Console.WriteLine($"Unselected cell: [{e.PreviousCell.Row},{e.PreviousCell.Column}]");
            }
            if (e.Cell != null)
            {
                //e.Cell.Text = "Selected";
                Console.WriteLine($"Selected cell: [{e.Cell.Row},{e.Cell.Column}]");
            }
        }

        private static void Table_OnCellRightClick(object sender, Table.CellEventArgs e)
        {
            Console.WriteLine($"Right clicked cell: [{e.Cell.Row},{e.Cell.Column}]");
        }

        private static void Table_OnCellLeftClick(object sender, Table.CellEventArgs e)
        {
            Console.WriteLine($"Left clicked cell: [{e.Cell.Row},{e.Cell.Column}]");
        }

        private static void Table_OnCellDoubleClick(object sender, Table.CellEventArgs e)
        {
            Console.WriteLine($"Double clicked cell: [{e.Cell.Row},{e.Cell.Column}]");
        }
    }
}
