using System;

namespace ShiftShift.Core
{
    /// <summary>WCAG 2 contrast ratio between two sRGB colours (components 0-1). Used to keep palettes readable.</summary>
    public static class ColorContrast
    {
        public static double Ratio(double r1, double g1, double b1, double r2, double g2, double b2)
        {
            double l1 = Luminance(r1, g1, b1), l2 = Luminance(r2, g2, b2);
            double hi = Math.Max(l1, l2), lo = Math.Min(l1, l2);
            return (hi + 0.05) / (lo + 0.05);
        }

        public static double Luminance(double r, double g, double b) =>
            0.2126 * Linear(r) + 0.7152 * Linear(g) + 0.0722 * Linear(b);

        private static double Linear(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
