using SadConsole;
using SadConsole.UI;
using SadRogue.Primitives;
using SCControlsExtended.ControlExtensions;
using SCControlsExtended.Controls;
using System;

namespace SCControlsExtended.Visualizer.ExampleConsoles
{
    internal class ExcelWindow : ControlsConsole
    {
        private readonly Table _table;

        public ExcelWindow(int width, int height) : base(width, height)
        {
            _table = new Table(Width, Height, 10, 3);
            _table.SetThemeColors(Colors.CreateSadConsoleBlue());
            _table.DefaultForeground = Color.Black;
            _table.DefaultBackground = Color.Lerp(Color.WhiteSmoke, Color.Black, 0.075f);
            _table.SetupScrollBar(Orientation.Vertical, Height -1, new Point(Width - 1, 0));
            _table.SetupScrollBar(Orientation.Horizontal, Width - 1, new Point(0, Height - 2));

            // Only add layout and let the console draw the rest
            _table.OnDrawFakeCell += DrawFakeCell;
            _table.DrawFakeCells = true;

            Controls.Add(_table);

            AdjustTable();
        }

        public void Init()
        {
            Game.Instance.ResizeWindow(Width, Height - 1, Game.Instance.DefaultFont.GetFontSize(Game.Instance.DefaultFontSize));
            Resize(Width, Height - 1, false);
            UsePixelPositioning = true;
            Position = new Point(0, 8);
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

                cell.Text = cell.Row == 0 ? GetExcelColumnName(cell.Column) : cell.Row.ToString();
                cell.Settings.HorizontalAlignment = Table.Cell.Options.HorizontalAlign.Center;
                cell.Settings.VerticalAlignment = Table.Cell.Options.VerticalAlign.Center;
                cell.Settings.Interactable = false;
                return;
            }

            // Setting the inner cells
            cell.Foreground = Color.Lerp(Color.WhiteSmoke, Color.Black, 0.3f);
            cell.Background = Color.Lerp(Color.WhiteSmoke, Color.Black, 0.075f);
            cell.Text = GetExcelColumnName(cell.Column) + cell.Row;
            cell.Settings.Interactable = true;
        }

        private void AdjustTable()
        {
            _table.Cells.Row(0).SetLayout(background: Color.Lerp(Color.WhiteSmoke, Color.Black, 0.25f));
            _table.Cells.Column(0).SetLayout(background: Color.Lerp(Color.WhiteSmoke, Color.Black, 0.25f));

            // Set column
            _table.Cells[0, 0].Text = ">";
            _table.Cells[0, 0].Settings.HorizontalAlignment = Table.Cell.Options.HorizontalAlign.Center;
            _table.Cells[0, 0].Settings.VerticalAlignment = Table.Cell.Options.VerticalAlign.Center;
            _table.Cells[0, 0].Settings.Interactable = false;
            _table.Cells[5, 0].Settings.UseFakeLayout = true;
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
