using SadConsole.UI;
using SadRogue.Primitives;
using SCControlsExtended.Controls;

namespace SCControlsExtended.Visualizer
{
    internal class ExcelWindow : ControlsConsole
    {
        private readonly Table _grid;

        public ExcelWindow(int width, int height) : base(width, height)
        {
            _grid = new Table(width, height, 1);
            _grid.SetThemeColors(Colors.CreateSadConsoleBlue());
            _grid.DefaultBackground = Color.Lerp(Color.Gray, Color.Black, 0.6f);
            _grid.DefaultForeground = Color.Yellow;
            _grid.DrawOnlyIndexedCells = false;
            Controls.Add(_grid);
        }
    }
}
