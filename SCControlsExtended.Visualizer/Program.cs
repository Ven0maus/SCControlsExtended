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

        static void Main(string[] args)
        {
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
            table.Cells.Range(0, 1, 0, 10).ForEach(cell => cell.Text = "Column " + col++);
            table.Cells.Range(1, 0, 25, 0).ForEach(cell => cell.Text = "Row " + row++);

            // Set inner cells color
            table.Cells.Range(1, 1, 25, 10).ForEach(cell => cell.Background = innerCellColor);
        }

        private static void Init()
        {
            SetCustomThemes();

            var visual = new ControlsConsole(Width, Height);

            // Construct table control
            var table = new Table(visual.Width, visual.Height, 10, 2);
            table.SetThemeColors(Colors.CreateSadConsoleBlue());
            visual.Controls.Add(table);

            AdjustTableValues(table);

            Game.Instance.Screen = visual;
        }
    }
}