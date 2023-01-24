using Microsoft.VisualBasic;
using NUnit.Framework;
using SadConsole.UI.Themes;
using SadRogue.Primitives;
using SCControlsExtended.ControlExtensions;
using SCControlsExtended.Controls;
using SCControlsExtended.Themes;
using System.Linq;

namespace Tests
{
    public class TableTests
    {
        private Table _table;

        public TableTests()
        {
            Library.Default.SetControlTheme(typeof(Table), new TableTheme(new ScrollBarTheme()));
        }

        [SetUp]
        public void Setup()
        {
            _table = new Table(100, 20, 10, 2);
        }

        [Test]
        public void CellsIndexer_CreatesNewCell()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_table.Cells.Count(), Is.EqualTo(0));
                Assert.That(_table.Cells[0, 0], Is.Not.Null);
            });
            Assert.That(_table.Cells.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Cells_GetCell_CreatesNewCell()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_table.Cells.Count(), Is.EqualTo(0));
                Assert.That(_table.Cells.GetCell(0, 0), Is.Not.Null);
            });
            Assert.That(_table.Cells.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Cells_Cell_SetLayout()
        {
            var settings = new Table.Cell.Options(_table)
            {
                Interactable = false
            };
            _table.Cells[0, 0].SetLayout(Color.Green, Color.Black, settings);
            Assert.Multiple(() =>
            {
                Assert.That(_table.Cells.Row(0).Foreground, Is.EqualTo(Color.Green));
                Assert.That(_table.Cells.Column(0).Background, Is.EqualTo(Color.Black));
                Assert.That(_table.Cells.Row(0).Settings, Is.EqualTo(settings));
                Assert.That(_table.Cells.Column(0).Settings, Is.EqualTo(settings));
            });
        }

        [Test]
        public void ResizeCell_SetsLayout()
        {
            _table.Cells[0, 0].Resize();
            Assert.Multiple(() =>
            {
                Assert.That(_table.Cells.Row(0).Size, Is.EqualTo(_table.DefaultCellSize.Y));
                Assert.That(_table.Cells.Column(0).Size, Is.EqualTo(_table.DefaultCellSize.X));
            });
            _table.Cells[0, 0].Resize(2, 4);
            Assert.Multiple(() =>
            {
                Assert.That(_table.Cells.Row(0).Size, Is.EqualTo(2));
                Assert.That(_table.Cells.Column(0).Size, Is.EqualTo(4));
            });
            _table.Cells[0, 0].Resize(rowSize: 8);
            Assert.Multiple(() =>
            {
                Assert.That(_table.Cells.Row(0).Size, Is.EqualTo(8));
                Assert.That(_table.Cells.Column(0).Size, Is.EqualTo(4));
            });
            _table.Cells[0, 0].Resize(columnSize: 5);
            Assert.Multiple(() =>
            {
                Assert.That(_table.Cells.Row(0).Size, Is.EqualTo(8));
                Assert.That(_table.Cells.Column(0).Size, Is.EqualTo(5));
            });
        }

        [Test]
        public void Cells_Range_ReturnsCorrectCells()
        {
            // 0 based, 0 -> 4 = 5 indexes
            var cells = _table.Cells.Range(0, 0, 4, 4).ToArray();
            Assert.That(cells, Has.Length.EqualTo(25));
            int y = 0, x = 0;
            foreach (var cell in cells)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(cell.Row, Is.EqualTo(y));
                    Assert.That(cell.Column, Is.EqualTo(x));
                });
                if (y == 4)
                {
                    y = 0;
                    x++;
                }
                else
                {
                    y++;
                }
            }
        }

        [Test]
        public void Cells_Range_ForEach_AppliesActionToAll()
        {
            var cells = _table.Cells.Range(0, 0, 4, 4).ToArray();
            cells.ForEach((cell) => cell.Text = "Hello!");
            Assert.That(cells.All(a => a.Text == "Hello!"));
        }

        [Test]
        public void Cells_Select_Deselect_Cell_Correct()
        {
            Assert.That(_table.SelectedCell, Is.Null);
            _table.Cells[0, 0].Select();
            Assert.That(_table.SelectedCell, Is.EqualTo(_table.Cells[0, 0]));
            _table.Cells[0, 0].Deselect();
            Assert.That(_table.SelectedCell, Is.Null);
        }

        [Test]
        public void Cells_Layout_GetMultiple_Correct()
        {
            var rowLayouts = _table.Cells.Row(0, 1, 2);
            Assert.That(rowLayouts.Count(), Is.EqualTo(3));
            var columnLayouts = _table.Cells.Column(0, 1, 2);
            Assert.That(columnLayouts.Count(), Is.EqualTo(3));
        }

        [Test]
        public void Cells_Layout_SetLayout_Multiple_AppliedToAll()
        {
            var settings = new Table.Cell.Options(_table)
            {
                Interactable = false
            };
            var rowLayouts = _table.Cells.Row(0, 1, 2);
            rowLayouts.SetLayout(3, Color.White, Color.Orange, settings);
            foreach (var layout in rowLayouts)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(layout.Size, Is.EqualTo(3));
                    Assert.That(layout.Foreground, Is.EqualTo(Color.White));
                    Assert.That(layout.Background, Is.EqualTo(Color.Orange));
                    Assert.That(layout.Settings, Is.EqualTo(settings));
                });
            }
        }

        [Test]
        public void Can_Enumerate_Cells()
        {
            _table.Cells[0, 0].Text = "Hello 1";
            _table.Cells[1, 0].Text = "Hello 2";
            int count = 0;
            foreach (var cell in _table.Cells)
                count++;
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void Can_Enumerate_LayoutRange()
        {
            var layouts = _table.Cells.Row(0, 1);
            int count = 0;
            foreach (var layout in layouts)
                count++;
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void Total_Count_IsCorrect()
        {
            _table.Cells[0, 0].Text = "Col / Row 1";
            _table.Cells[1, 0].Text = "Row 2";
            _table.Cells[2, 0].Text = "Row 3";
            _table.Cells[0, 1].Text = "Column 2";
            Assert.Multiple(() =>
            {
                Assert.That(_table.Cells.TotalColumns, Is.EqualTo(2));
                Assert.That(_table.Cells.TotalRows, Is.EqualTo(3));
            });
        }

        [Test]
        public void Cells_Clear_Correct()
        {
            _table.Cells.Row(0).Size = 4;
            _table.Cells[0, 0].Text = "Hello";
            _table.Cells[0, 1].Text = "Hello";

            Assert.That(_table.Cells.Count(), Is.EqualTo(2));

            _table.Cells.Clear(false);
            Assert.Multiple(() =>
            {
                Assert.That(_table.Cells.Count(), Is.EqualTo(0));
                Assert.That(_table.Cells.Row(0).Size, Is.EqualTo(4));
            });
            Assert.That(_table.Cells.Row(0).Size, Is.EqualTo(4));

            _table.Cells.Clear(true);

            Assert.That(_table.Cells.Row(0).Size, Is.EqualTo(_table.DefaultCellSize.Y));
        }
    }
}