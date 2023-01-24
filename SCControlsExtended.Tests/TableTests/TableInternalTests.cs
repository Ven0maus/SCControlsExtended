﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCControlsExtended.Tests.TableTests
{
    /// <summary>
    /// Tests the internal code structure of the table, that is not accessible to the users
    /// </summary>
    internal class TableInternalTests : TableTestsBase
    {
        public TableInternalTests(int width, int height, int cellWidth, int cellHeight)
            : base(width, height, cellWidth, cellHeight)
        { }
    }
}