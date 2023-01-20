using SadConsole.Input;
using SadConsole.UI;
using SadRogue.Primitives;
using SCControlsExtended.Controls;

namespace SCControlsExtended.Visualizer
{
    public class DrawingGridWindow : ControlsConsole
    {
        private readonly Table _grid;

        public DrawingGridWindow(int width, int height) : base(width, height)
        {
            _grid = new Table(width, height, 2);
            _grid.SetThemeColors(Colors.CreateSadConsoleBlue());
            _grid.DefaultBackground = Color.Lerp(Color.Gray, Color.Black, 0.85f);
            _grid.DefaultForeground = Color.Yellow;
            _grid.DrawOnlyIndexedCells = false;
            _grid.RenderSelectionEffect = false;
            Controls.Add(_grid);
        }

        public override bool ProcessMouse(MouseScreenObjectState state)
        {
            if (state.Mouse.LeftButtonDown && _grid.CurrentMouseCell != null)
            {
                _grid.CurrentMouseCell.Background = Color.DarkOrchid;
            }
            else if (state.Mouse.RightClicked)
            {
                _grid.Cells.Clear();
            }

            return base.ProcessMouse(state);
        }
    }
}
