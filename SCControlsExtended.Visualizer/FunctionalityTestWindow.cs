using SadConsole.UI;
using SadRogue.Primitives;
using SCControlsExtended.Controls;
using System;

namespace SCControlsExtended.Visualizer
{
    internal class FunctionalityTestWindow : ControlsConsole
    {
        private readonly Table _table;

        public FunctionalityTestWindow(int width, int height) : base(width, height)
        {
            // Construct table control
            _table = new Table(width, height, 10, 2)
            {
                // Set default background color
                DefaultBackground = Color.Wheat
            };

            // Set some default theme colors, for selection & hovering appearances
            _table.SetThemeColors(Colors.CreateSadConsoleBlue());

            // Test events
            _table.OnCellDoubleClick += Table_OnCellDoubleClick;
            _table.OnCellLeftClick += Table_OnCellLeftClick;
            _table.OnCellRightClick += Table_OnCellRightClick;
            _table.SelectedCellChanged += Table_SelectedCellChanged;
            _table.OnCellEnter += Table_OnCellEnter;
            _table.OnCellExit += Table_OnCellExit;

            // Only add a few cells, and let console draw the rest
            _table.DrawOnlyIndexedCells = false;
            Controls.Add(_table);

            AdjustTableValues(_table);
        }

        private static void AdjustTableValues(Table table)
        {
            table.Cells.Row(0).SetLayout(background: Color.Lerp(Color.Gray, Color.Black, 0.8f));
            table.Cells.Column(0).SetLayout(background: Color.Lerp(Color.Gray, Color.Black, 0.8f));

            var innerCellColor = Color.Lerp(Color.Gray, Color.Black, 0.6f);
            int col = 1, row = 1;

            // Set column
            table.Cells[0, 0].Text = "C/R 0";

            // Set column, row texts
            table.Cells.Range(0, 1, 0, 5).ForEach(cell => cell.Text = "Column " + col++);
            table.Cells.Range(1, 0, 10, 0).ForEach(cell => cell.Text = "Row " + row++);

            // Set inner cells color
            table.Cells.Range(1, 1, 10, 5).ForEach(cell => cell.Background = innerCellColor);

            // Custom cell size
            table.Cells[5, 7].Text = "Support custom cell sizes!";
            table.Cells[5, 7].SetLayout(6, 20);
            table.Cells[5, 7].Background = Color.Yellow;
            table.Cells[5, 7].Foreground = Color.Black;

            table.Cells[6, 7].Background = Color.Magenta;
            table.Cells[5, 8].Background = Color.Orange;
            table.Cells[6, 8].Background = Color.Blue;
        }

        private static void Table_OnCellExit(object sender, Table.CellEventArgs e)
        {
            Console.WriteLine($"Exited cell: [{e.Cell.RowIndex},{e.Cell.ColumnIndex}]");
        }

        private static void Table_OnCellEnter(object sender, Table.CellEventArgs e)
        {
            Console.WriteLine($"Entered cell: [{e.Cell.RowIndex},{e.Cell.ColumnIndex}]");
        }

        private static void Table_SelectedCellChanged(object sender, Table.CellChangedEventArgs e)
        {
            if (e.PreviousCell != null)
            {
                e.PreviousCell.Text = string.Empty;
                Console.WriteLine($"Unselected cell: [{e.PreviousCell.RowIndex},{e.PreviousCell.ColumnIndex}]");
            }
            if (e.Cell != null)
            {
                e.Cell.Text = "Selected";
                Console.WriteLine($"Selected cell: [{e.Cell.RowIndex},{e.Cell.ColumnIndex}]");
            }
        }

        private static void Table_OnCellRightClick(object sender, Table.CellEventArgs e)
        {
            Console.WriteLine($"Right clicked cell: [{e.Cell.RowIndex},{e.Cell.ColumnIndex}]");
        }

        private static void Table_OnCellLeftClick(object sender, Table.CellEventArgs e)
        {
            Console.WriteLine($"Left clicked cell: [{e.Cell.RowIndex},{e.Cell.ColumnIndex}]");
        }

        private static void Table_OnCellDoubleClick(object sender, Table.CellEventArgs e)
        {
            Console.WriteLine($"Double clicked cell: [{e.Cell.RowIndex},{e.Cell.ColumnIndex}]");
        }
    }
}
