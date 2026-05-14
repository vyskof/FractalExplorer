using System;
using System.Threading;
using System.Windows.Media;
using FractalExplorer.Models;

namespace FractalExplorer.Rendering;

/// <summary>
/// Renders fractals into a raw BGRA pixel buffer suitable for WPF BitmapSource/WriteableBitmap.
/// </summary>
public class FractalRenderer
{
    /// <summary>
    /// Renders a full fractal image to a byte array in Bgr32-compatible layout (4 bytes per pixel).
    /// </summary>
    public byte[] RenderPixels(FractalSettings settings, IProgress<int>? progress)
    {
        return RenderPixels(settings, progress, CancellationToken.None);
    }

    /// <summary>
    /// Renders a full fractal image to a byte array in Bgr32-compatible layout (4 bytes per pixel).
    /// This overload supports cancellation.
    /// </summary>
    public byte[] RenderPixels(FractalSettings settings, IProgress<int>? progress, CancellationToken cancellationToken)
    {
        int stride = settings.Width * 4;
        byte[] pixels = new byte[stride * settings.Height];

        for (int y = 0; y < settings.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (int x = 0; x < settings.Width; x++)
            {
                var (mathX, mathY) = FractalMath.PixelToMath(x, y, settings);

                int iterations;
                double finalMagnitude;

                if (settings.FractalType == "Julia")
                {
                    (iterations, finalMagnitude) = FractalMath.ComputeJuliaIterations(
                        mathX,
                        mathY,
                        settings.JuliaReal,
                        settings.JuliaImaginary,
                        settings.MaxIterations);
                }
                else
                {
                    (iterations, finalMagnitude) = FractalMath.ComputeIterations(mathX, mathY, settings.MaxIterations);
                }

                Color color = iterations >= settings.MaxIterations
                    ? Colors.Black
                    : GetPaletteColor(settings.ColorMode, FractalMath.ComputeSmoothColor(iterations, settings.MaxIterations, finalMagnitude));

                int index = (y * stride) + (x * 4);
                pixels[index] = color.B;
                pixels[index + 1] = color.G;
                pixels[index + 2] = color.R;
                pixels[index + 3] = 255;
            }

            if (y % 50 == 0)
            {
                progress?.Report((int)((y / (double)settings.Height) * 100));
            }
        }

        progress?.Report(100);
        return pixels;
    }

    private static Color GetPaletteColor(string colorMode, double t)
    {
        return colorMode switch
        {
            "Ocean" => GetOceanColor(t),
            "Rainbow" => GetRainbowColor(t),
            _ => GetFireColor(t)
        };
    }

    private static Color GetFireColor(double t)
    {
        if (t < 0.25)
        {
            return InterpolateColor(Colors.Black, Color.FromRgb(90, 0, 0), t / 0.25);
        }

        if (t < 0.55)
        {
            return InterpolateColor(Color.FromRgb(90, 0, 0), Color.FromRgb(220, 80, 0), (t - 0.25) / 0.30);
        }

        if (t < 0.85)
        {
            return InterpolateColor(Color.FromRgb(220, 80, 0), Color.FromRgb(255, 220, 0), (t - 0.55) / 0.30);
        }

        return InterpolateColor(Color.FromRgb(255, 220, 0), Colors.White, (t - 0.85) / 0.15);
    }

    private static Color GetOceanColor(double t)
    {
        if (t < 0.35)
        {
            return InterpolateColor(Colors.Black, Color.FromRgb(0, 25, 90), t / 0.35);
        }

        if (t < 0.75)
        {
            return InterpolateColor(Color.FromRgb(0, 25, 90), Color.FromRgb(0, 200, 220), (t - 0.35) / 0.40);
        }

        return InterpolateColor(Color.FromRgb(0, 200, 220), Colors.White, (t - 0.75) / 0.25);
    }

    private static Color GetRainbowColor(double t)
    {
        // Full hue circle with high saturation for vivid colors.
        return HsvToRgb(360.0 * t, 0.95, 1.0);
    }

    private static Color InterpolateColor(Color start, Color end, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);

        byte r = (byte)(start.R + ((end.R - start.R) * t));
        byte g = (byte)(start.G + ((end.G - start.G) * t));
        byte b = (byte)(start.B + ((end.B - start.B) * t));

        return Color.FromRgb(r, g, b);
    }

    private static Color HsvToRgb(double hue, double saturation, double value)
    {
        hue %= 360.0;
        if (hue < 0)
        {
            hue += 360.0;
        }

        double c = value * saturation;
        double x = c * (1 - Math.Abs(((hue / 60.0) % 2) - 1));
        double m = value - c;

        double rPrime;
        double gPrime;
        double bPrime;

        if (hue < 60)
        {
            rPrime = c; gPrime = x; bPrime = 0;
        }
        else if (hue < 120)
        {
            rPrime = x; gPrime = c; bPrime = 0;
        }
        else if (hue < 180)
        {
            rPrime = 0; gPrime = c; bPrime = x;
        }
        else if (hue < 240)
        {
            rPrime = 0; gPrime = x; bPrime = c;
        }
        else if (hue < 300)
        {
            rPrime = x; gPrime = 0; bPrime = c;
        }
        else
        {
            rPrime = c; gPrime = 0; bPrime = x;
        }

        byte r = (byte)((rPrime + m) * 255);
        byte g = (byte)((gPrime + m) * 255);
        byte b = (byte)((bPrime + m) * 255);

        return Color.FromRgb(r, g, b);
    }
}
