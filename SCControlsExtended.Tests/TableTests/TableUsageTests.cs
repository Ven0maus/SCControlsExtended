using NUnit.Framework;
using SadConsole;
using SadRogue.Primitives;
using SCControlsExtended.Controls;
using System.Linq;
namespace SCControlsExtended.Tests.TableTests;

/// <summary>
/// Tests the code structure of the table, that is accessible to the users
/// </summary>
[TestFixture(100, 20, 10, 2)]
[TestFixture(77, 35, 5, 3)]
internal class TableUsageTests : TableTestsBase
{
    public TableUsageTests(int width, int height, int cellWidth, int cellHeight)
        : base(width, height, cellWidth, cellHeight)
    { }

    [Test]
    public void CellsIndexer_CreatesNewCell()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Table.Cells, Is.Empty);
            Assert.That(Table.Cells[0, 0], Is.Not.Null);
        });
        Assert.That(Table.Cells, Has.Count.EqualTo(1));
    }

    [Test]
    public void Cells_GetCell_CreatesNewCell()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Table.Cells, Is.Empty);
            Assert.That(Table.Cells.GetCell(0, 0), Is.Not.Null);
        });
        Assert.That(Table.Cells, Has.Count.EqualTo(1));
    }

    [Test]
    public void Cells_Cell_SetLayout()
    {
        var settings = new Table.Cell.Options(Table)
        {
            Interactable = false
        };
        Table.Cells[0, 0].SetLayout(Color.Green, Color.Black, settings);
        Assert.Multiple(() =>
        {
            Assert.That(Table.Cells.Row(0).Foreground, Is.EqualTo(Color.Green));
            Assert.That(Table.Cells.Column(0).Background, Is.EqualTo(Color.Black));
            Assert.That(Table.Cells.Row(0).Settings, Is.EqualTo(settings));
            Assert.That(Table.Cells.Column(0).Settings, Is.EqualTo(settings));
        });
    }

    [Test]
    public void ResizeCell_SetsLayout()
    {
        Table.Cells[0, 0].Resize();
        Assert.Multiple(() =>
        {
            Assert.That(Table.Cells.Row(0).Size, Is.EqualTo(Table.DefaultCellSize.Y));
            Assert.That(Table.Cells.Column(0).Size, Is.EqualTo(Table.DefaultCellSize.X));
        });
        Table.Cells[0, 0].Resize(2, 4);
        Assert.Multiple(() =>
        {
            Assert.That(Table.Cells.Row(0).Size, Is.EqualTo(2));
            Assert.That(Table.Cells.Column(0).Size, Is.EqualTo(4));
        });
        Table.Cells[0, 0].Resize(rowSize: 8);
        Assert.Multiple(() =>
        {
            Assert.That(Table.Cells.Row(0).Size, Is.EqualTo(8));
            Assert.That(Table.Cells.Column(0).Size, Is.EqualTo(4));
        });
        Table.Cells[0, 0].Resize(columnSize: 5);
        Assert.Multiple(() =>
        {
            Assert.That(Table.Cells.Row(0).Size, Is.EqualTo(8));
            Assert.That(Table.Cells.Column(0).Size, Is.EqualTo(5));
        });
    }

    [Test]
    public void Cells_Range_ReturnsCorrectCells()
    {
        // 0 based, 0 -> 4 = 5 indexes
        var cells = Table.Cells.Range(0, 0, 4, 4).ToArray();
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
        var cells = Table.Cells.Range(0, 0, 4, 4).ToArray();
        cells.ForEach((cell) => cell.Value = "Hello!");
        Assert.That(cells.All(a => a.StringValue == "Hello!"));
    }

    [Test]
    public void Cells_Select_Deselect_Cell_Correct()
    {
        Assert.That(Table.SelectedCell, Is.Null);
        Table.Cells[0, 0].Select();
        Assert.That(Table.SelectedCell, Is.EqualTo(Table.Cells[0, 0]));
        Table.Cells[0, 0].Deselect();
        Assert.That(Table.SelectedCell, Is.Null);
    }

    [Test]
    public void Cells_Width_Height_Correct()
    {
        Table.Cells.Row(0).Size = 11;
        Assert.That(Table.Cells[0, 0].Height, Is.EqualTo(11));

        Table.Cells.Column(0).Size = 6;
        Assert.That(Table.Cells[0, 0].Width, Is.EqualTo(6));
    }

    [Test]
    public void Cells_Layout_Settings_SetCorrectly()
    {
        var layout = Table.Cells.Row(0);
        layout.Settings.Selectable = false;
        Assert.That(Table.Cells[0, 0].Settings.Selectable, Is.False);
    }

    [Test]
    public void Cells_Layout_GetMultiple_Correct()
    {
        var rowLayouts = Table.Cells.Row(0, 1, 2);
        Assert.That(rowLayouts.Count(), Is.EqualTo(3));
        var columnLayouts = Table.Cells.Column(0, 1, 2);
        Assert.That(columnLayouts.Count(), Is.EqualTo(3));
    }

    [Test]
    public void Cells_Layout_SetLayout_Multiple_AppliedToAll()
    {
        var settings = new Table.Cell.Options(Table)
        {
            Interactable = false
        };
        var rowLayouts = Table.Cells.Row(0, 1, 2);
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
        Table.Cells[0, 0].Value = "Hello 1";
        Table.Cells[1, 0].Value = "Hello 2";
        int count = 0;
        foreach (var cell in Table.Cells)
            count++;
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void Can_Enumerate_LayoutRange()
    {
        var layouts = Table.Cells.Row(0, 1);
        int count = 0;
        foreach (var layout in layouts)
            count++;
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void Total_Count_IsCorrect()
    {
        Table.Cells[0, 0].Value = "Col / Row 1";
        Table.Cells[1, 0].Value = "Row 2";
        Table.Cells[2, 0].Value = "Row 3";
        Table.Cells[0, 1].Value = "Column 2";
        Assert.Multiple(() =>
        {
            Assert.That(Table.Cells.MaxColumn, Is.EqualTo(1));
            Assert.That(Table.Cells.MaxRow, Is.EqualTo(2));
        });
    }

    [Test]
    public void Cells_Remove_Correct()
    {
        Table.Cells[0, 0].Value = "Hello";
        Table.Cells[1, 0].Value = "Hello";
        Assert.That(Table.Cells, Has.Count.EqualTo(2));
        Table.Cells.Remove(0, 0);
        Assert.That(Table.Cells, Has.Count.EqualTo(1));
        Table.Cells[1, 0].Remove();
        Assert.That(Table.Cells, Is.Empty);
    }

    [Test]
    public void Cells_Clear_Correct()
    {
        Table.Cells.Row(0).Size = 4;
        Table.Cells[0, 0].Value = "Hello";
        Table.Cells[0, 1].Value = "Hello";

        Assert.That(Table.Cells, Has.Count.EqualTo(2));

        Table.Cells.Clear(false);
        Assert.Multiple(() =>
        {
            Assert.That(Table.Cells, Is.Empty);
            Assert.That(Table.Cells.Row(0).Size, Is.EqualTo(4));
        });
        Assert.That(Table.Cells.Row(0).Size, Is.EqualTo(4));

        Table.Cells.Clear(true);

        Assert.That(Table.Cells.Row(0).Size, Is.EqualTo(Table.DefaultCellSize.Y));
    }

    [Test]
    public void Cells_Different_Cell_NotEquals()
    {
        var cellA = Table.Cells[0, 0];
        cellA.Value = "Hello";

        var cellB = Table.Cells[0, 1];
        cellB.Value = "Hello";

        Assert.That(cellA, Is.Not.EqualTo(cellB));

        cellB = null;

        Assert.That(cellA, Is.Not.EqualTo(cellB));
    }

    [Test]
    public void Cells_Cell_CopyAppearanceFrom_Correct()
    {
        var cellA = Table.Cells[0, 0];
        cellA.Background = Color.Brown;
        cellA.Settings.UseFakeLayout = true;
        cellA.Value = "Not Hello World!";

        var cellB = Table.Cells[0, 1];
        cellB.Value = "Hello World!";
        cellB.CopyAppearanceFrom(cellA);
        Assert.Multiple(() =>
        {
            Assert.That(cellA.Value, Is.EqualTo("Not Hello World!"));
            Assert.That(cellB.Value, Is.EqualTo("Hello World!"));
            Assert.That(cellB.Background, Is.EqualTo(Color.Brown));
            Assert.That(cellB.Settings.UseFakeLayout, Is.EqualTo(true));
        });
        cellA.Settings.Selectable = false;
        cellB.CopyAppearanceFrom(cellA);
        Assert.Multiple(() =>
        {
            Assert.That(cellB.Settings.UseFakeLayout, Is.EqualTo(true));
            Assert.That(cellB.Settings.Selectable, Is.EqualTo(false));
        });

        // Settings is not initialized here, copy it over, everything should be default again
        var cellC = Table.Cells[0, 2];
        cellB.CopyAppearanceFrom(cellC);
        Assert.Multiple(() =>
        {
            Assert.That(cellB.Settings.UseFakeLayout, Is.EqualTo(false));
            Assert.That(cellB.Settings.Selectable, Is.EqualTo(true));
        });
    }

    [Test]
    public void Table_ScrollBar_Vertical_Scrolling_EqualSizes_Correct()
    {
        const int extraRowsOffScreen = 5;
        Table.SetupScrollBar(SadConsole.Orientation.Vertical, 5, new Point(0, 0));

        var rows = (Table.Height / Table.DefaultCellSize.Y) + extraRowsOffScreen;
        for (int row = 0; row < rows; row++)
        {
            Table.Cells[row, 0].Value = "Row " + row;
        }

        Table.Theme.UpdateAndDraw(Table, new System.TimeSpan());
        Assert.Multiple(() =>
        {
            Assert.That(Table.IsVerticalScrollBarVisible, Is.True);
            Assert.That(Table.VerticalScrollBar.Maximum, Is.EqualTo(extraRowsOffScreen));
            Assert.That(Table.VerticalScrollBar.Value, Is.EqualTo(0));
        });

        // Increment
        int totalHeight = 0;
        int maximum = Table.VerticalScrollBar.Maximum;
        for (int i = 0; i < maximum; i++)
        {
            totalHeight += GetLastVisibleCellSize(Table, Orientation.Vertical, true);
            totalHeight = totalHeight < 0 ? 0 : totalHeight;
            Table.VerticalScrollBar.Value += 1;
            Table.Theme.UpdateAndDraw(Table, new System.TimeSpan());
            Assert.Multiple(() =>
            {
                Assert.That(Table.StartRenderYPos, Is.EqualTo(totalHeight));
                Assert.That(Table.VerticalScrollBar.Maximum, Is.EqualTo(maximum));
            });
        }

        // Decrement
        for (int i = maximum; i > 0; i--)
        {
            totalHeight += GetLastVisibleCellSize(Table, Orientation.Vertical, false);
            totalHeight = totalHeight < 0 ? 0 : totalHeight;
            Table.VerticalScrollBar.Value -= 1;
            Table.Theme.UpdateAndDraw(Table, new System.TimeSpan());
            Assert.Multiple(() =>
            {
                Assert.That(Table.StartRenderYPos, Is.EqualTo(totalHeight));
                Assert.That(Table.VerticalScrollBar.Maximum, Is.EqualTo(maximum));
            });
        }

        Assert.That(Table.StartRenderYPos, Is.EqualTo(0));
    }

    [Test]
    public void Table_ScrollBar_Horizontal_Scrolling_EqualSizes_Correct()
    {
        const int extraColumnsOffScreen = 5;
        Table.SetupScrollBar(SadConsole.Orientation.Horizontal, 5, new Point(0, 0));

        var columns = (Table.Width / Table.DefaultCellSize.X) + extraColumnsOffScreen;
        for (int column = 0; column < columns; column++)
        {
            Table.Cells[0, column].Value = "Column " + column;
        }

        Table.Theme.UpdateAndDraw(Table, new System.TimeSpan());
        Assert.Multiple(() =>
        {
            Assert.That(Table.IsHorizontalScrollBarVisible, Is.True);
            Assert.That(Table.HorizontalScrollBar.Maximum, Is.EqualTo(extraColumnsOffScreen));
            Assert.That(Table.HorizontalScrollBar.Value, Is.EqualTo(0));
        });

        // Increment
        int totalWidth = 0;
        int maximum = Table.HorizontalScrollBar.Maximum;
        for (int i = 0; i < maximum; i++)
        {
            totalWidth += GetLastVisibleCellSize(Table, Orientation.Horizontal, true);
            totalWidth = totalWidth < 0 ? 0 : totalWidth;
            Table.HorizontalScrollBar.Value += 1;
            Table.Theme.UpdateAndDraw(Table, new System.TimeSpan());
            Assert.Multiple(() =>
            {
                Assert.That(Table.StartRenderXPos, Is.EqualTo(totalWidth));
                Assert.That(Table.HorizontalScrollBar.Maximum, Is.EqualTo(maximum));
            });
        }

        // Decrement
        for (int i = maximum; i > 0; i--)
        {
            totalWidth += GetLastVisibleCellSize(Table, Orientation.Horizontal, false);
            totalWidth = totalWidth < 0 ? 0 : totalWidth;
            Table.HorizontalScrollBar.Value -= 1;
            Table.Theme.UpdateAndDraw(Table, new System.TimeSpan());
            Assert.Multiple(() =>
            {
                Assert.That(Table.StartRenderXPos, Is.EqualTo(totalWidth));
                Assert.That(Table.HorizontalScrollBar.Maximum, Is.EqualTo(maximum));
            });
        }

        Assert.That(Table.StartRenderXPos, Is.EqualTo(0));
    }

    [Test]
    public void Table_ScrollBar_Vertical_Scrolling_DifferentSizes_Correct()
    {
        const int extraRowsOffScreen = 5;
        Table.SetupScrollBar(Orientation.Vertical, 5, new Point(0, 0));

        var rows = (Table.Height / Table.DefaultCellSize.Y) + extraRowsOffScreen;
        for (int row = 0; row < rows; row++)
        {
            Table.Cells[row, 0].Value = "Row " + row;
        }

        // Resize columns
        Table.Cells[1, 0].Resize(rowSize: 4);
        Table.Cells[2, 0].Resize(rowSize: 8);
        Table.Cells[3, 0].Resize(rowSize: 1);

        Table.Theme.UpdateAndDraw(Table, new System.TimeSpan());

        var maximum = GetMaximumScrollBarItems(Table, Orientation.Vertical);
        Assert.Multiple(() =>
        {
            Assert.That(Table.IsVerticalScrollBarVisible, Is.True);
            Assert.That(Table.VerticalScrollBar.Maximum, Is.EqualTo(maximum));
            Assert.That(Table.VerticalScrollBar.Value, Is.EqualTo(0));
        });

        // Increment
        int totalHeight = 0;
        for (int i = 0; i < maximum; i++)
        {
            totalHeight += GetLastVisibleCellSize(Table, Orientation.Vertical, true);
            totalHeight = totalHeight < 0 ? 0 : totalHeight;
            Table.VerticalScrollBar.Value += 1;
            Table.Theme.UpdateAndDraw(Table, new System.TimeSpan());
            Assert.Multiple(() =>
            {
                Assert.That(Table.StartRenderYPos, Is.EqualTo(totalHeight));
                Assert.That(Table.VerticalScrollBar.Maximum, Is.EqualTo(maximum));
            });
        }

        // Decrement
        for (int i = maximum; i > 0; i--)
        {
            totalHeight += GetLastVisibleCellSize(Table, Orientation.Vertical, false);
            totalHeight = totalHeight < 0 ? 0 : totalHeight;
            Table.VerticalScrollBar.Value -= 1;
            Table.Theme.UpdateAndDraw(Table, new System.TimeSpan());
            Assert.Multiple(() =>
            {
                Assert.That(Table.StartRenderYPos, Is.EqualTo(totalHeight));
                Assert.That(Table.VerticalScrollBar.Maximum, Is.EqualTo(maximum));
            });
        }

        Assert.That(Table.StartRenderYPos, Is.EqualTo(0));
    }

    [Test]
    public void Table_ScrollBar_Horizontal_Scrolling_DifferentSizes_Correct()
    {
        const int extraColumnsOffScreen = 5;
        Table.SetupScrollBar(SadConsole.Orientation.Horizontal, 5, new Point(0, 0));

        var columns = (Table.Width / Table.DefaultCellSize.X) + extraColumnsOffScreen;
        for (int column = 0; column < columns; column++)
        {
            Table.Cells[0, column].Value = "Column " + column;
        }

        // Resize columns
        Table.Cells[0, 1].Resize(columnSize: 4);
        Table.Cells[0, 2].Resize(columnSize: 8);
        Table.Cells[0, 3].Resize(columnSize: 1);

        Table.Theme.UpdateAndDraw(Table, new System.TimeSpan());

        var maximum = GetMaximumScrollBarItems(Table, Orientation.Horizontal);
        Assert.Multiple(() =>
        {
            Assert.That(Table.IsHorizontalScrollBarVisible, Is.True);
            Assert.That(Table.HorizontalScrollBar.Maximum, Is.EqualTo(maximum));
            Assert.That(Table.HorizontalScrollBar.Value, Is.EqualTo(0));
        });

        // Increment
        int totalWidth = 0;
        for (int i = 0; i < maximum; i++)
        {
            totalWidth += GetLastVisibleCellSize(Table, Orientation.Horizontal, true);
            totalWidth = totalWidth < 0 ? 0 : totalWidth;
            Table.HorizontalScrollBar.Value += 1;
            Table.Theme.UpdateAndDraw(Table, new System.TimeSpan());
            Assert.Multiple(() =>
            {
                Assert.That(Table.StartRenderXPos, Is.EqualTo(totalWidth));
                Assert.That(Table.HorizontalScrollBar.Maximum, Is.EqualTo(maximum));
            });
        }

        for (int i = maximum; i > 0; i--)
        {
            totalWidth += GetLastVisibleCellSize(Table, Orientation.Horizontal, false);
            totalWidth = totalWidth < 0 ? 0 : totalWidth;
            Table.HorizontalScrollBar.Value -= 1;
            Table.Theme.UpdateAndDraw(Table, new System.TimeSpan());
            Assert.Multiple(() =>
            {
                Assert.That(Table.StartRenderXPos, Is.EqualTo(totalWidth));
                Assert.That(Table.HorizontalScrollBar.Maximum, Is.EqualTo(maximum));
            });
        }

        Assert.That(Table.StartRenderXPos, Is.EqualTo(0));
    }

    [Test]
    public void Table_ScrollBar_Horizontal_ChangeScrollMaximum_OnResize_Correct()
    {
        const int extraColumnsOffScreen = 5;
        Table.SetupScrollBar(Orientation.Horizontal, 5, new Point(0, 0));

        var columns = (Table.Width / Table.DefaultCellSize.X) + extraColumnsOffScreen;
        for (int column = 0; column < columns; column++)
        {
            Table.Cells[0, column].Value = "Column " + column;
        }

        // Resize columns
        Table.Cells[0, 1].Resize(columnSize: 4);
        Table.Cells[0, 2].Resize(columnSize: 8);
        Table.Cells[0, 3].Resize(columnSize: 1);

        Table.HorizontalScrollBar.Value = 1;
        Table.Theme.UpdateAndDraw(Table, new System.TimeSpan());

        var maximum = GetMaximumScrollBarItems(Table, Orientation.Horizontal);
        Assert.Multiple(() =>
        {
            Assert.That(Table.IsHorizontalScrollBarVisible, Is.True);
            Assert.That(Table.HorizontalScrollBar.Maximum, Is.EqualTo(maximum));
            Assert.That(Table.HorizontalScrollBar.Value, Is.EqualTo(1));
        });

        // Resize existing cell
        Table.Cells[0, 1].Resize(columnSize: 9);
        Table.Cells[0, 2].Resize(columnSize: 16);
        Table.Theme.UpdateAndDraw(Table, new System.TimeSpan());
        Assert.That(Table.HorizontalScrollBar.Maximum, Is.Not.EqualTo(maximum));

        // Update max
        maximum = GetMaximumScrollBarItems(Table, Orientation.Horizontal);
        Assert.That(Table.HorizontalScrollBar.Maximum, Is.EqualTo(maximum));
    }

    [Test]
    public void Table_ScrollBar_Vertical_ChangeScrollMaximum_OnResize_Correct()
    {
        const int extraRowsOffScreen = 5;
        Table.SetupScrollBar(Orientation.Vertical, 5, new Point(0, 0));

        var rows = (Table.Height / Table.DefaultCellSize.Y) + extraRowsOffScreen;
        for (int row = 0; row < rows; row++)
        {
            Table.Cells[row, 0].Value = "Row " + row;
        }

        // Resize columns
        Table.Cells[1, 0].Resize(rowSize: 4);
        Table.Cells[2, 0].Resize(rowSize: 8);
        Table.Cells[3, 0].Resize(rowSize: 1);

        Table.VerticalScrollBar.Value = 1;
        Table.Theme.UpdateAndDraw(Table, new System.TimeSpan());

        var maximum = GetMaximumScrollBarItems(Table, Orientation.Vertical);
        Assert.Multiple(() =>
        {
            Assert.That(Table.IsVerticalScrollBarVisible, Is.True);
            Assert.That(Table.VerticalScrollBar.Maximum, Is.EqualTo(maximum));
            Assert.That(Table.VerticalScrollBar.Value, Is.EqualTo(1));
        });

        // Resize existing cell
        Table.Cells[1, 0].Resize(rowSize: 9);
        Table.Cells[2, 0].Resize(rowSize: 16);
        Table.Theme.UpdateAndDraw(Table, new System.TimeSpan());
        Assert.That(Table.VerticalScrollBar.Maximum, Is.Not.EqualTo(maximum));

        // Update max
        maximum = GetMaximumScrollBarItems(Table, Orientation.Vertical);
        Assert.That(Table.VerticalScrollBar.Maximum, Is.EqualTo(maximum));
    }

    [Test]
    public void Table_ScrollBar_ScrollToSelectedItem_Vertical_Works()
    {
        const int extraRowsOffScreen = 5;
        Table.SetupScrollBar(Orientation.Vertical, 5, new Point(0, 0));

        var rows = (Table.Height / Table.DefaultCellSize.Y) + extraRowsOffScreen;
        for (int row = 0; row < rows; row++)
        {
            Table.Cells[row, 0].Value = "Row " + row;
        }

        // Resize columns
        Table.Cells[1, 0].Resize(rowSize: 4);
        Table.Cells[2, 0].Resize(rowSize: 8);
        Table.Cells[3, 0].Resize(rowSize: 1);

        Table.VerticalScrollBar.Value = 0;
        Table.Theme.UpdateAndDraw(Table, new System.TimeSpan());

        Table.Cells.Select(rows - 1, 0);
        Table.ScrollToSelectedItem();

        Assert.That(Table.VerticalScrollBar.Value, Is.Not.EqualTo(0));
    }

    [Test]
    public void Table_ScrollBar_ScrollToSelectedItem_Horizontal_Works()
    {
        const int extraColumnsOffScreen = 5;
        Table.SetupScrollBar(Orientation.Horizontal, 5, new Point(0, 0));

        var columns = (Table.Width / Table.DefaultCellSize.X) + extraColumnsOffScreen;
        for (int col = 0; col < columns; col++)
        {
            Table.Cells[0, col].Value = "Col " + col;
        }

        // Resize columns
        Table.Cells[0, 1].Resize(columnSize: 4);
        Table.Cells[0, 2].Resize(columnSize: 8);
        Table.Cells[0, 3].Resize(columnSize: 1);

        Table.HorizontalScrollBar.Value = 0;
        Table.Theme.UpdateAndDraw(Table, new System.TimeSpan());

        Table.Cells.Select(0, columns - 1);
        Table.ScrollToSelectedItem();

        Assert.That(Table.HorizontalScrollBar.Value, Is.Not.EqualTo(0));
    }
}