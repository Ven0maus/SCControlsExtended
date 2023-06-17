using SadConsole.Input;
using SadConsole;
using SadRogue.Primitives;
using SadConsole.UI;
using System;

namespace SCControlsExtended.Windows
{
    /// <summary>
    /// Shows all the characters of a font, clicking on a character raises an event with the glyph index.
    /// </summary>
    public class FontWindow : Window
    {
        /// <summary>
        /// Event raised when a glyph is clicked, returns glyph index
        /// </summary>
        public event EventHandler<int>? OnClick;

        public FontWindow(IFont font)
            : base(16 + 2, 16 + 2)
        {
            Font = font;
            Surface.DefaultBackground = Color.Black;
            Surface.DefaultForeground = Color.White;
            Title = "TileSet";
        }

        public void DrawFontSurface()
        {
            int count = 0;
            for (int y = 1; y < 16 + 1; y++)
            {
                for (int x = 1; x < 16 + 1; x++)
                {
                    Surface[x, y].Glyph = count;
                    Surface[x, y].Foreground = Surface.DefaultForeground;
                    count++;
                }
            }
            Surface.IsDirty = true;
            Show();
        }

        private Point? _prevPoint;
        public override bool ProcessMouse(MouseScreenObjectState state)
        {
            if (state.IsOnScreenObject)
            {
                if (_prevPoint != null)
                {
                    Surface.SetForeground(_prevPoint.Value.X, _prevPoint.Value.Y, Color.White);
                    _prevPoint = null;
                }

                if (state.SurfaceCellPosition.X > 0 && state.SurfaceCellPosition.Y > 0 &&
                    state.SurfaceCellPosition.X < Width - 1 && state.SurfaceCellPosition.Y < Height - 1)
                {
                    _prevPoint = state.SurfaceCellPosition;
                    Surface.SetForeground(_prevPoint.Value.X, _prevPoint.Value.Y, Color.Red);

                    if (state.Mouse.LeftClicked)
                        OnClick?.Invoke(this, Surface[state.SurfaceCellPosition.X, state.SurfaceCellPosition.Y].Glyph);
                }
            }
            else
            {
                if (_prevPoint != null)
                {
                    Surface.SetForeground(_prevPoint.Value.X, _prevPoint.Value.Y, Color.White);
                    _prevPoint = null;
                }
            }
            return base.ProcessMouse(state);
        }
    }
}
