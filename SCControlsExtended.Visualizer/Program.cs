using SadConsole;
using SadConsole.UI;
using SadConsole.UI.Themes;
using SCControlsExtended.Controls;
using SCControlsExtended.Themes;
using SadRogue.Primitives;

namespace SCControlsExtended.Visualizer
{
    class Program
    {
        public const int Width = 100;
        public const int Height = 40;

        private static Table _table;

        static void Main(string[] args)
        {
            Settings.WindowTitle = "SadConsole Controls Extended";

            // Setup the engine and create the main window.
            Game.Create(Width, Height);

            // Hook the start event so we can add consoles to the system.
            Game.Instance.OnStart = Init;

            // Start the game.
            Game.Instance.Run();
            Game.Instance.Dispose();
        }

        private static void SetCustomThemes()
        {
            Library.Default.SetControlTheme(typeof(Table), new TableTheme());
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

        private static void Init()
        {
            SetCustomThemes();

            var visual = new ControlsConsole(Width, Height);

            // Construct table control
            _table = new Table(visual.Width, visual.Height, 10, 2)
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

            visual.Controls.Add(_table);

            AdjustTableValues(_table);

            Game.Instance.Screen = visual;
        }

        private static void Table_OnCellExit(object sender, Table.CellEventArgs e)
        {
            System.Console.WriteLine($"Exited cell: [{e.Cell.RowIndex},{e.Cell.ColumnIndex}]");
        }

        private static void Table_OnCellEnter(object sender, Table.CellEventArgs e)
        {
            System.Console.WriteLine($"Entered cell: [{e.Cell.RowIndex},{e.Cell.ColumnIndex}]");
        }

        private static void Table_SelectedCellChanged(object sender, Table.CellEventArgs e)
        {
            System.Console.WriteLine($"Selected cell: [{e.Cell.RowIndex},{e.Cell.ColumnIndex}]");
        }

        private static void Table_OnCellRightClick(object sender, Table.CellEventArgs e)
        {
            System.Console.WriteLine($"Right clicked cell: [{e.Cell.RowIndex},{e.Cell.ColumnIndex}]");
        }

        private static void Table_OnCellLeftClick(object sender, Table.CellEventArgs e)
        {
            System.Console.WriteLine($"Left clicked cell: [{e.Cell.RowIndex},{e.Cell.ColumnIndex}]");

            if (_table.SelectedCell == null)
            {
                e.Cell.Text = string.Empty;
            }
            else
            {
                e.Cell.Text = "Selected";
            }
        }

        private static void Table_OnCellDoubleClick(object sender, Table.CellEventArgs e)
        {
            System.Console.WriteLine($"Double clicked cell: [{e.Cell.RowIndex},{e.Cell.ColumnIndex}]");
        }
    }
}