using System;
using FractalExplorer.Models;

namespace FractalExplorer.Rendering;

/// <summary>
/// Mathematical helper methods used to compute fractals.
/// </summary>
public static class FractalMath
{
    /// <summary>
    /// Computes Mandelbrot iterations for a single complex point c.
    /// Formula: z(n+1) = z(n)^2 + c, with z(0) = 0.
    /// </summary>
    /// <param name="cReal">Real part of c.</param>
    /// <param name="cImaginary">Imaginary part of c.</param>
    /// <param name="maxIterations">Maximum allowed iteration count.</param>
    /// <returns>
    /// Iteration count when the point escaped (or maxIterations if it did not)
    /// and final magnitude |z|.
    /// </returns>
    public static (int iterations, double finalMagnitude) ComputeIterations(double cReal, double cImaginary, int maxIterations)
    {
        double zReal = 0.0;
        double zImaginary = 0.0;
        double zRealSquared = 0.0;
        double zImaginarySquared = 0.0;

        int iterations = 0;
        while (iterations < maxIterations && (zRealSquared + zImaginarySquared) <= 4.0)
        {
            // (a + bi)^2 = (a^2 - b^2) + 2abi
            zImaginary = (2.0 * zReal * zImaginary) + cImaginary;
            zReal = (zRealSquared - zImaginarySquared) + cReal;

            zRealSquared = zReal * zReal;
            zImaginarySquared = zImaginary * zImaginary;
            iterations++;
        }

        return (iterations, Math.Sqrt(zRealSquared + zImaginarySquared));
    }

    /// <summary>
    /// Computes Julia-set iterations for one starting point z, with fixed constant c.
    /// Formula: z(n+1) = z(n)^2 + c.
    /// </summary>
    public static (int iterations, double finalMagnitude) ComputeJuliaIterations(
        double startReal,
        double startImaginary,
        double cReal,
        double cImaginary,
        int maxIterations)
    {
        double zReal = startReal;
        double zImaginary = startImaginary;
        double zRealSquared = zReal * zReal;
        double zImaginarySquared = zImaginary * zImaginary;

        int iterations = 0;
        while (iterations < maxIterations && (zRealSquared + zImaginarySquared) <= 4.0)
        {
            zImaginary = (2.0 * zReal * zImaginary) + cImaginary;
            zReal = (zRealSquared - zImaginarySquared) + cReal;

            zRealSquared = zReal * zReal;
            zImaginarySquared = zImaginary * zImaginary;
            iterations++;
        }

        return (iterations, Math.Sqrt(zRealSquared + zImaginarySquared));
    }

    /// <summary>
    /// Computes smooth coloring value normalized to 0..1.
    ///
    /// Instead of using only integer iteration count, we refine the value by using
    /// the final magnitude of z. This reduces visible color banding and produces
    /// smoother gradients.
    /// </summary>
    public static double ComputeSmoothColor(int iterations, int maxIterations, double finalMagnitude)
    {
        // Points that did not escape are considered inside the set and must be black.
        if (iterations >= maxIterations)
        {
            return 0.0;
        }

        // Guard against invalid logarithm arguments in edge cases.
        if (finalMagnitude <= 1.0)
        {
            return Math.Clamp(iterations / (double)maxIterations, 0.0, 1.0);
        }

        double smooth = iterations + 1.0 - (Math.Log(Math.Log(finalMagnitude)) / Math.Log(2.0));
        double normalized = smooth / maxIterations;

        return Math.Clamp(normalized, 0.0, 1.0);
    }

    /// <summary>
    /// Converts a pixel position (screen space) into a complex-plane coordinate (math space).
    ///
    /// At zoom = 1:
    /// - visible width in math space is 3.5 units
    /// - visible height in math space is 2.0 units
    ///
    /// As zoom increases, the visible area shrinks proportionally.
    /// The center point (CenterX, CenterY) stays in the middle of the view.
    /// </summary>
    public static (double mathX, double mathY) PixelToMath(int pixelX, int pixelY, FractalSettings settings)
    {
        double visibleWidth = 3.5 / settings.Zoom;
        double visibleHeight = 2.0 / settings.Zoom;

        // Convert pixel coordinates to normalized coordinates in range [0, 1].
        double normalizedX = pixelX / (double)settings.Width;
        double normalizedY = pixelY / (double)settings.Height;

        // Shift normalized coordinates so 0 is at center, then scale to math dimensions.
        // X grows to the right, Y grows upward in math space (therefore minus for screen Y).
        double mathX = settings.CenterX + ((normalizedX - 0.5) * visibleWidth);
        double mathY = settings.CenterY - ((normalizedY - 0.5) * visibleHeight);

        return (mathX, mathY);
    }
}
