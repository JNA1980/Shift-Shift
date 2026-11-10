using NUnit.Framework;
using ShiftShift.Core;

namespace ShiftShift.Tests
{
    public class ColorContrastTests
    {
        private static double Hex(string a, string b)
        {
            double C(string h, int i) => System.Convert.ToInt32(h.Substring(i, 2), 16) / 255.0;
            return ColorContrast.Ratio(C(a, 1), C(a, 3), C(a, 5), C(b, 1), C(b, 3), C(b, 5));
        }

        [Test] public void BlackOnWhite_Is21() => Assert.AreEqual(21.0, Hex("#000000", "#FFFFFF"), 0.01);
        [Test] public void SameColour_Is1() => Assert.AreEqual(1.0, Hex("#336699", "#336699"), 1e-9);
        [Test] public void OrderDoesNotMatter() => Assert.AreEqual(Hex("#F27017", "#333333"), Hex("#333333", "#F27017"), 1e-12);

        [Test]
        public void ChosenColours_OnCharcoalRoad_AreAtLeast3To1()
        {
            // John, day 16: charcoal road #333333; vivid action colours identical on every level.
            foreach (string c in new[] { "#F27017", "#FCC41E", "#00FCF9", "#F40E9D", "#0E79F1", "#F21E3D" })
                Assert.That(Hex(c, "#333333"), Is.GreaterThanOrEqualTo(2.995), c);
        }
    }
}
