using SadConsole;
using SadConsole.Input;
using SadConsole.UI;
using SadConsole.UI.Controls;
using SadRogue.Primitives;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SCControlsExtended.Windows
{
    public class ScrollableTextWindow : Window
    {
        /// <summary>
        /// String content to be drawn to the box, handles text wrapping automatically.
        /// </summary>
        public ObservableCollection<ColoredString> Content { get; }

        /// <summary>
        /// Automatically re-draw and adjust sliders when a string as added to the content collection.
        /// Default: False
        /// </summary>
        public bool DrawOnContentChange { get; set; } = true;

        public ScrollBar ScrollBar { get; }
        public Button CloseButton { get; }

        public ScrollableTextWindow(int width, int height, Action? closeButtonClick = null) : base(width, height)
        {
            DefaultBackground = Color.Black;
            DefaultForeground = Color.White;

            Title = string.Empty;

            Content = new ObservableCollection<ColoredString>();
            Content.CollectionChanged += Content_CollectionChanged;

            ScrollBar = new ScrollBar(Orientation.Vertical, height - 2);
            ScrollBar.Position = new Point(width - 2, 1);
            ScrollBar.ValueChanged += ScrollBar_ValueChanged;
            ScrollBar.IsVisible = false;
            Controls.Add(ScrollBar);

            CloseButton = new Button(9);
            CloseButton.Text = "Close";
            CloseButton.Position = new Point(width / 2 - 5, height - 1);
            if (closeButtonClick != null)
                CloseButton.Click += (sender, args) => closeButtonClick.Invoke();
            Controls.Add(CloseButton);
        }

        public ScrollableTextWindow(int width, int height, string title, Action? closeButtonClick = null) :
            this(width, height, closeButtonClick)
        {
            Title = title;
        }

        /// <summary>
        /// Draws and adjusts the scrollbar slider visibility based on the Content collection.
        /// </summary>
        public void AdjustContent()
        {
            AdjustForWordWrapping();
            AdjustSliderSize();
            DrawContent();
        }

        /// <inheritdoc/>
        public override bool ProcessMouse(MouseScreenObjectState state)
        {
            if (state.Mouse.ScrollWheelValueChange > 0)
                ScrollBar.Value++;
            else if (state.Mouse.ScrollWheelValueChange < 0)
                ScrollBar.Value--;
            return base.ProcessMouse(state);
        }

        private void AdjustSliderSize()
        {
            var diff = Content.Count - (Height - 2);
            ScrollBar.Value = 0;
            ScrollBar.Maximum = diff < 0 ? 1 : diff;
            ScrollBar.IsVisible = diff > 0;
        }

        private void AdjustForWordWrapping()
        {
            var maxTotal = Width - 2; // -2 for slider position at the end and window border
            if (!Content.Any(a => a.Length > maxTotal)) return;

            var invalidContents = Content
                .Select((str, index) => new { str, index })
                .Where(a => a.str.Length > maxTotal)
                .ToList();

            foreach (var obj in invalidContents)
            {
                Content.RemoveAt(obj.index);

                var str = obj.str;

                // Split up into multiples
                var splits = (int)Math.Ceiling((double)str.Length / maxTotal);
                int currentIndex = 0;
                for (int i = 0; i < splits; i++)
                {
                    var remainingLength = str.Length - currentIndex;
                    var batch = str.SubString(currentIndex, Math.Min(maxTotal, remainingLength));
                    currentIndex += batch.Length;
                    Content.Insert(obj.index + i, batch);
                }
            }
        }

        private void DrawContent()
        {
            // Clear the previous content
            for (int y = 0; y < Height - 2; y++)
                Surface.Clear(1, y + 1, Width - 2);

            if (Content.Count == 0) return;

            // Clear the inside console
            for (int y = 0; y < Height - 2; y++)
            {
                if (Content.Count > (y + 1))
                    Surface.Print(1, y + 1, Content[y + ScrollBar.Value]);
            }
            IsDirty = true;
        }

        private void Content_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (DrawOnContentChange)
            {
                AdjustContent();
            }
        }

        private void ScrollBar_ValueChanged(object? sender, System.EventArgs e)
        {
            DrawContent();
        }
    }
}
