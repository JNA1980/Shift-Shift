using System.Collections.Generic;
using NUnit.Framework;
using ShiftShift.Core;

namespace ShiftShift.Tests
{
    public class ShiftMenuKeysTests
    {
        private static List<MenuKey> Run(ShiftMenuKeys k, params (bool l, bool r)[] frames)
        {
            var seen = new List<MenuKey>();
            foreach ((bool l, bool r) in frames)
            {
                MenuKey key = k.Poll(l, r);
                if (key != MenuKey.None) seen.Add(key);
            }
            return seen;
        }

        [Test]
        public void SingleKey_ReportsOnRelease()
        {
            var k = new ShiftMenuKeys();
            CollectionAssert.IsEmpty(Run(k, (true, false), (true, false)));
            CollectionAssert.AreEqual(new[] { MenuKey.Left }, Run(k, (false, false)));
            CollectionAssert.AreEqual(new[] { MenuKey.Right }, Run(k, (false, true), (false, false)));
        }

        [Test]
        public void BothKeys_ReportOnce_WhenTheSecondGoesDown()
        {
            var k = new ShiftMenuKeys();
            CollectionAssert.AreEqual(new[] { MenuKey.Both },
                Run(k, (true, false), (true, true), (true, true), (false, true), (false, false)));
        }

        [Test]
        public void AfterBoth_LettingGoIsNotALeftOrRight_AndNextPressWorks()
        {
            var k = new ShiftMenuKeys();
            Run(k, (true, true), (true, false), (false, false));
            CollectionAssert.AreEqual(new[] { MenuKey.Right }, Run(k, (false, true), (false, false)));
        }

        [Test]
        public void Reset_WithAKeyHeld_WaitsForAllUp()
        {
            var k = new ShiftMenuKeys();
            k.Reset(true, false); // arrived on this screen still holding Left
            CollectionAssert.IsEmpty(Run(k, (true, false), (false, false)));
            CollectionAssert.AreEqual(new[] { MenuKey.Left }, Run(k, (true, false), (false, false)));
        }

        [Test]
        public void BrowserCase_OneReleaseHidden_StillOneBoth()
        {
            // In the browser both keys read as down until the last one is let go.
            var k = new ShiftMenuKeys();
            CollectionAssert.AreEqual(new[] { MenuKey.Both }, Run(k, (true, true), (true, true), (false, false)));
        }
    }
}
