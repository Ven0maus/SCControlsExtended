using SadConsole.UI;
using SadRogue.Primitives;
using SCControlsExtended.Controls;
using System;

namespace SCControlsExtended.Visualizer
{
    internal class ExcelWindow : ControlsConsole
    {
        private readonly Table _table;

        public ExcelWindow(int width, int height) : base(width, height)
        {
            _table = new Table(width, height, 10, 2);
            _table.SetThemeColors(Colors.CreateSadConsoleBlue());
            _table.DefaultForeground = Color.Black;
            _table.DrawOnlyIndexedCells = true;
            Controls.Add(_table);

            AdjustTable();
        }

        private void AdjustTable()
        {
            _table.Cells.Row(0).SetLayout(background: Color.Lerp(Color.WhiteSmoke, Color.Black, 0.25f));
            _table.Cells.Column(0).SetLayout(background: Color.Lerp(Color.WhiteSmoke, Color.Black, 0.25f));

            var innerCellColor = Color.Lerp(Color.WhiteSmoke, Color.Black, 0.075f);
            int col = 1, row = 1;

            // Set column
            _table.Cells[0, 0].Text = ">";

            // Set column, row texts
            _table.Cells.Range(0, 1, 0, Width / _table.DefaultCellSize.X).ForEach(cell => cell.Text = GetExcelColumnName(col++));
            _table.Cells.Range(1, 0, Height / _table.DefaultCellSize.Y, 0).ForEach(cell => cell.Text = row++.ToString());

            // Set inner cells color
            _table.Cells.Range(1, 1, Height / _table.DefaultCellSize.Y, Width / _table.DefaultCellSize.X).ForEach(cell => cell.Background = innerCellColor);
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
                d = (int)((d - mod) / 26);
            }

            return name;
        }
    }
}
