using SadConsole;
using SadConsole.UI;
using SadConsole.UI.Controls;
using SadConsole.UI.Themes;
using SadRogue.Primitives;
using SCControlsExtended.Controls;
using SCControlsExtended.Demo.ExampleConsoles;
using SCControlsExtended.Windows;
using System;
using System.Linq;

namespace SCControlsExtended.Demo
{
    class Program
    {
        public const int Width = 100;
        public const int Height = 40;

        static void Main()
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
            Library.Default.SetControlTheme(typeof(Table), new TableTheme(new ScrollBarTheme()));
        }

        private static Group[] DefineGroups()
        {
            return new[]
            {
                new Group
                {
                    Name = "Table",
                    Options = new ScreenSurface[]
                    {
                        new FunctionalityTestWindow(Width, Height),
                        new ExcelWindow(Width - 1, Height - 1),
                    }
                },
                new Group
                {
                    Name = "Scrollable Text Window",
                    Options = new ScreenSurface[]
                    {
                        new ScrollableTextWindow(Width / 2, Height / 2, "Scrollable Text Window").Prepare((a) =>
                        {
                            a.DrawOnContentChange = false;
                                                        a.Content.Add(new ColoredString("Did I also mention, this control fully supports automatic word wrapping, oh yeah you best believe it!", Color.White, Color.DarkBlue));
                            var r = new Random();
                            for (int i=0; i < a.Height + 10; i++)
                                a.Content.Add(new ColoredString("Hello world!", new Color(r.Next(0, 255), r.Next(0, 255), r.Next(0, 255)), a.DefaultBackground));
                            a.AdjustContent();
                            a.Show();
                        }),
                    }
                },
            };
        }

        private static void Init()
        {
            SetCustomThemes();

            var selectionMenu = new ControlsConsole(Width + 1, Height + 1);
            var groups = DefineGroups();

            int yCount = 10;
            foreach (var group in groups)
            {
                var groupButton = new Button(group.Name.Length + 2)
                {
                    Position = new Point(Width / 2 - (group.Name.Length / 2), yCount += 2),
                    Text = group.Name
                };

                // Store the group's options in a local variable
                var options = group.Options;

                // Handle groupButton click event
                groupButton.Click += ((sender, args) =>
                {
                    // Clear the existing buttons
                    selectionMenu.Controls.Clear();

                    if (options.Length == 1)
                    {
                        selectionMenu.IsVisible = false;
                        var option = options.First();
                        Game.Instance.Screen = option;
                        return;
                    }

                    // Add buttons for the group's options
                    int optionYCount = groupButton.Position.Y + 2;
                    foreach (var option in options)
                    {
                        var name = option.GetType().Name;
                        var button = new Button(name.Length + 2)
                        {
                            Position = new Point(Width / 2 - (name.Length / 2), optionYCount += 2),
                            Text = name
                        };
                        button.Click += ((optionSender, optionArgs) =>
                        {
                            selectionMenu.IsVisible = false;
                            Game.Instance.Screen = option;
                        });
                        selectionMenu.Controls.Add(button);
                    }

                    var backButton = new Button(8);
                    backButton.Text = "Back";
                    backButton.Position = new Point(Width / 2 - 2, optionYCount += 2);
                    backButton.Click += (sender, args) => { Init(); };
                    selectionMenu.Controls.Add(backButton);
                });

                selectionMenu.Controls.Add(groupButton);
            }

            Game.Instance.Screen = selectionMenu;
        }

        // New class to represent a group of options
        private class Group
        {
            public string Name { get; set; }
            public ScreenSurface[] Options { get; set; }
        }
    }
}