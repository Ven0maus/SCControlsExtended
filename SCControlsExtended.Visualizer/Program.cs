using SadConsole;
using SadConsole.UI;
using SadConsole.UI.Controls;
using SadConsole.UI.Themes;
using SadRogue.Primitives;
using SCControlsExtended.Controls;
using SCControlsExtended.Themes;

namespace SCControlsExtended.Visualizer
{
    class Program
    {
        public const int Width = 100;
        public const int Height = 40;

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

        private static void Init()
        {
            SetCustomThemes();

            var selectionMenu = new ControlsConsole(Width, Height);
            var options = new ControlsConsole[] 
            { 
                new FunctionalityTestWindow(Width, Height), 
                new ExcelWindow(Width, Height), 
                new DrawingTableWindow(Width, Height) 
            };

            int yCount = 10;
            foreach (var option in options)
            {
                var name = option.GetType().Name;
                var button = new Button(name.Length + 2)
                {
                    Position = new Point(Width / 2 - (name.Length / 2), yCount += 2),
                    Text = name
                };
                button.Click += ((sender, args) => 
                { 
                    selectionMenu.IsVisible = false;
                    Game.Instance.Screen = option;
                    if (option is ExcelWindow excelWindow)
                        excelWindow.Init();
                });
                selectionMenu.Controls.Add(button);
            }

            Game.Instance.Screen = selectionMenu;
        }
    }
}