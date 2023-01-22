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
            _table.DrawOnlyIndexedCells = true;
            _table.SetupScrollBar(Orientation.Horizontal, Width, new Point(0, Height - 2));
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

        private void AdjustTable()
        {
            _table.Cells.Row(0).SetLayout(background: Color.Lerp(Color.WhiteSmoke, Color.Black, 0.25f));
            _table.Cells.Column(0).SetLayout(background: Color.Lerp(Color.WhiteSmoke, Color.Black, 0.25f));

            var innerCellColor = Color.Lerp(Color.WhiteSmoke, Color.Black, 0.075f);
            int col = 1, row = 1;

            // Set column
            _table.Cells[0, 0].Text = ">";
            _table.Cells[0, 0].Settings.HorizontalAlignment = Table.Cell.Options.HorizontalAlign.Center;
            _table.Cells[0, 0].Settings.VerticalAlignment = Table.Cell.Options.VerticalAlign.Center;
            _table.Cells[0, 0].Settings.Interactable = false;

            // Set column, row texts
            _table.Cells.Range(0, 1, 0, Width / _table.DefaultCellSize.X).ForEach(cell =>
            {
                cell.Text = GetExcelColumnName(col++);
                cell.Settings.HorizontalAlignment = Table.Cell.Options.HorizontalAlign.Center;
                cell.Settings.VerticalAlignment = Table.Cell.Options.VerticalAlign.Center;
                cell.Settings.Interactable = false;
            });
            _table.Cells.Range(1, 0, (Height / _table.DefaultCellSize.Y)+3, 0).ForEach(cell =>
            {
                cell.Text = row++.ToString();
                cell.Settings.HorizontalAlignment = Table.Cell.Options.HorizontalAlign.Center;
                cell.Settings.VerticalAlignment = Table.Cell.Options.VerticalAlign.Center;
                cell.Settings.Interactable = false;
            });

            // Set inner cells color
            _table.Cells.Range(1, 1, (Height / _table.DefaultCellSize.Y)+3, Width / _table.DefaultCellSize.X).ForEach(cell => cell.Background = innerCellColor);
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
