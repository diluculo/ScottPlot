﻿// This colormap was created by Scott Harden on 2020-06-16 and is released under a MIT license.

namespace ScottPlot.Colormaps
{
    public class Grayscale: IColormap
    {
        public string Name => "Grayscale";

        public Color GetColor(double intensity, Range? domain)
        {
            if (double.IsNaN(intensity))
            {
                return Colors.Transparent;
            }

            domain ??= Range.UnitRange;

            double normalized = domain.Value.NormalizeAndClampToUnitRange(intensity);

            byte value = (byte)(255 * normalized);
            return Color.Gray(value);
        }
    }
}
