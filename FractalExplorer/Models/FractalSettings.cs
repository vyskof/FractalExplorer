namespace FractalExplorer.Models;

/// <summary>
/// Holds all parameters needed to render and navigate a fractal image.
/// </summary>
public class FractalSettings
{
    /// <summary>Width of the rendered fractal image in pixels.</summary>
    public int Width { get; set; } = 800;

    /// <summary>Height of the rendered fractal image in pixels.</summary>
    public int Height { get; set; } = 600;

    /// <summary>Current X center of the visible area in the complex plane.</summary>
    public double CenterX { get; set; } = -0.5;

    /// <summary>Current Y center of the visible area in the complex plane.</summary>
    public double CenterY { get; set; } = 0.0;

    /// <summary>Current zoom level. 1.0 means default view.</summary>
    public double Zoom { get; set; } = 1.0;

    /// <summary>Maximum number of iterations used for escape-time calculation.</summary>
    public int MaxIterations { get; set; } = 100;

    /// <summary>Selected fractal type: Mandelbrot or Julia.</summary>
    public string FractalType { get; set; } = "Mandelbrot";

    /// <summary>Selected color mode: Fire, Ocean, or Rainbow.</summary>
    public string ColorMode { get; set; } = "Fire";

    /// <summary>Real part of the Julia constant c.</summary>
    public double JuliaReal { get; set; } = -0.7;

    /// <summary>Imaginary part of the Julia constant c.</summary>
    public double JuliaImaginary { get; set; } = 0.27;

    /// <summary>
    /// Restores the default view position and zoom.
    /// </summary>
    public void Reset()
    {
        CenterX = -0.5;
        CenterY = 0.0;
        Zoom = 1.0;
    }
}
