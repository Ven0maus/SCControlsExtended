using SadConsole.UI;
using SadRogue.Primitives;
using SCControlsExtended.ControlExtensions;
using SCControlsExtended.Controls;
using System;

namespace SCControlsExtended.Visualizer.ExampleConsoles
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

            // Only add layout and let the console draw the rest
            _table.DrawFakeCells = true;

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
            _table.Cells[0, 0].Text = "C/R 0";

            // Set column, row texts
            _table.Cells.Range(0, 1, 0, 5).ForEach(cell => cell.Text = "Column " + col++);
            _table.Cells.Range(1, 0, 10, 0).ForEach(cell => cell.Text = "Row " + row++);

            // Set inner cells color
            _table.Cells.Range(1, 1, 10, 5).ForEach(cell => cell.Background = innerCellColor);

            // Custom cell size
            _table.Cells[5, 7].Text = "Support custom cell sizes!";
            _table.Cells[5, 7].Resize(6, 20);
            _table.Cells[5, 7].Background = Color.Yellow;
            _table.Cells[5, 7].Foreground = Color.Black;
            _table.Cells[6, 7].Background = Color.Magenta;
            _table.Cells[5, 8].Background = Color.Orange;
            _table.Cells[6, 8].Background = Color.Blue;
            _table.Cells[6, 8].Settings.Interactable = false;
            _table.Cells[7, 8].Settings.IsVisible = false;
            _table.Cells[1, 5].Select();
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
                e.PreviousCell.Text = string.Empty;
                Console.WriteLine($"Unselected cell: [{e.PreviousCell.Row},{e.PreviousCell.Column}]");
            }
            if (e.Cell != null)
            {
                e.Cell.Text = "Selected";
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
