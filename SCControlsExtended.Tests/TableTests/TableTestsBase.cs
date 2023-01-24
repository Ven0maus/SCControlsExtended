using NUnit.Framework;
using SadConsole.UI.Themes;
using SCControlsExtended.Controls;
using SCControlsExtended.Themes;

namespace SCControlsExtended.Tests.TableTests
{
    internal abstract class TableTestsBase
    {
        protected Table Table { get; set; }
        protected readonly int Width, Height, CellWidth, CellHeight;

        protected TableTestsBase(int width, int height, int cellWidth, int cellHeight)
        {
            Width = width;
            Height = height;
            CellWidth = cellWidth;
            CellHeight = cellHeight;

            Library.Default.SetControlTheme(typeof(Table), new TableTheme(new ScrollBarTheme()));
        }

        [SetUp]
        public virtual void Setup()
        {
            Table = new Table(Width, Height, CellWidth, CellHeight);
        }
    }
}
