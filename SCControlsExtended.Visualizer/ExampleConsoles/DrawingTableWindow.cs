using SadConsole.Input;
using SadConsole.UI;
using SadRogue.Primitives;
using SCControlsExtended.Controls;

namespace SCControlsExtended.Visualizer.ExampleConsoles
{
    public class DrawingTableWindow : ControlsConsole
    {
        private readonly Table _table;

        public DrawingTableWindow(int width, int height) : base(width, height)
        {
            _table = new Table(width, height, 2);
            _table.SetThemeColors(Colors.CreateSadConsoleBlue());
            _table.DefaultBackground = Color.Lerp(Color.Gray, Color.Black, 0.85f);
            _table.DefaultForeground = Color.Yellow;
            _table.DrawOnlyIndexedCells = false;
            _table.SelectionMode = Table.Mode.None;
            Controls.Add(_table);
        }

        public override bool ProcessMouse(MouseScreenObjectState state)
        {
            if (state.Mouse.LeftButtonDown && _table.CurrentMouseCell != null)
            {
                _table.CurrentMouseCell.Background = Color.DarkOrchid;
            }
            else if (state.Mouse.RightClicked)
            {
                _table.Cells.Clear();
            }

            return base.ProcessMouse(state);
        }
    }
}
