using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using FractalExplorer.Models;
using FractalExplorer.Rendering;

namespace FractalExplorer;

public partial class MainWindow : Window
{
    private readonly FractalRenderer _renderer = new();

    private FractalSettings _settings = new();
    private bool _isRendering;
    private bool _isDragging;
    private Point _dragStart;
    private double _dragStartCenterX;
    private double _dragStartCenterY;
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();

        cmbFractalType.SelectedIndex = 0;
        cmbPalette.SelectedIndex = 0;
        sldIterations.Value = _settings.MaxIterations;
        lblIterationValue.Text = _settings.MaxIterations.ToString(CultureInfo.InvariantCulture);

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await RenderAsync();
    }

    private async Task RenderAsync()
    {
        if (_isRendering)
        {
            _cts?.Cancel();
        }

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        CancellationTokenSource localCts = _cts;

        ReadSettingsFromControls();
        UpdateRenderSizeFromImage();

        _isRendering = true;
        btnRender.IsEnabled = false;
        prgRender.Value = 0;
        lblStatus.Text = "Vykreslování...";

        var snapshot = CloneSettings(_settings);
        var progress = new Progress<int>(value => prgRender.Value = value);
        Stopwatch timer = Stopwatch.StartNew();

        try
        {
            byte[] pixels = await Task.Run(() => _renderer.RenderPixels(snapshot, progress, localCts.Token), localCts.Token);

            localCts.Token.ThrowIfCancellationRequested();

            // BitmapSource must be created on the UI thread, because it is directly bound to WPF controls.
            BitmapSource bitmap = BitmapSource.Create(
                snapshot.Width,
                snapshot.Height,
                96,
                96,
                System.Windows.Media.PixelFormats.Bgr32,
                null,
                pixels,
                snapshot.Width * 4);

            fractalImage.Source = bitmap;
            timer.Stop();
            lblStatus.Text = $"Hotovo za {timer.ElapsedMilliseconds} ms";
            prgRender.Value = 100;
        }
        catch (OperationCanceledException)
        {
            lblStatus.Text = "Vykreslování zrušeno";
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Chyba: {ex.Message}";
        }
        finally
        {
            if (ReferenceEquals(_cts, localCts))
            {
                _isRendering = false;
                btnRender.IsEnabled = true;
            }
        }
    }

    private void ReadSettingsFromControls()
    {
        _settings.FractalType = ((ComboBoxItem)cmbFractalType.SelectedItem).Content.ToString() ?? "Mandelbrot";
        _settings.ColorMode = ((ComboBoxItem)cmbPalette.SelectedItem).Content.ToString() ?? "Fire";
        _settings.MaxIterations = (int)sldIterations.Value;

        if (!double.TryParse(txtJuliaReal.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double juliaReal))
        {
            juliaReal = -0.7;
        }

        if (!double.TryParse(txtJuliaImaginary.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double juliaImaginary))
        {
            juliaImaginary = 0.27;
        }

        _settings.JuliaReal = juliaReal;
        _settings.JuliaImaginary = juliaImaginary;
    }

    private void UpdateRenderSizeFromImage()
    {
        double width = Math.Max(400, fractalImage.ActualWidth);
        double height = Math.Max(300, fractalImage.ActualHeight);

        _settings.Width = (int)Math.Clamp(width, 400, 3000);
        _settings.Height = (int)Math.Clamp(height, 300, 3000);
    }

    private static FractalSettings CloneSettings(FractalSettings settings)
    {
        return new FractalSettings
        {
            Width = settings.Width,
            Height = settings.Height,
            CenterX = settings.CenterX,
            CenterY = settings.CenterY,
            Zoom = settings.Zoom,
            MaxIterations = settings.MaxIterations,
            FractalType = settings.FractalType,
            ColorMode = settings.ColorMode,
            JuliaReal = settings.JuliaReal,
            JuliaImaginary = settings.JuliaImaginary
        };
    }

    private async void btnRender_Click(object sender, RoutedEventArgs e)
    {
        await RenderAsync();
    }

    private async void btnReset_Click(object sender, RoutedEventArgs e)
    {
        _settings.Reset();
        await RenderAsync();
    }

    private void sldIterations_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (lblIterationValue is not null)
        {
            lblIterationValue.Text = ((int)e.NewValue).ToString(CultureInfo.InvariantCulture);
        }
    }

    private void cmbFractalType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (juliaPanel is null || cmbFractalType.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        string selected = item.Content.ToString() ?? "Mandelbrot";
        juliaPanel.Visibility = selected == "Julia" ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void fractalImage_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!TryGetPixelFromMouse(e.GetPosition(fractalImage), out double pixelX, out double pixelY))
        {
            return;
        }

        // Keep the complex-plane point under the cursor fixed while zooming.
        // We compute the math coordinate before zoom, then adjust center after zoom.
        double visibleWidth = 3.5 / _settings.Zoom;
        double visibleHeight = 2.0 / _settings.Zoom;

        double normalizedX = pixelX / _settings.Width;
        double normalizedY = pixelY / _settings.Height;

        double fixedMathX = _settings.CenterX + ((normalizedX - 0.5) * visibleWidth);
        double fixedMathY = _settings.CenterY - ((normalizedY - 0.5) * visibleHeight);

        const double zoomFactor = 1.3;
        double step = e.Delta > 0 ? zoomFactor : 1.0 / zoomFactor;
        _settings.Zoom *= step;

        double newVisibleWidth = 3.5 / _settings.Zoom;
        double newVisibleHeight = 2.0 / _settings.Zoom;

        _settings.CenterX = fixedMathX - ((normalizedX - 0.5) * newVisibleWidth);
        _settings.CenterY = fixedMathY + ((normalizedY - 0.5) * newVisibleHeight);

        await RenderAsync();
    }

    private void fractalImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        _isDragging = true;
        _dragStart = e.GetPosition(fractalImage);
        _dragStartCenterX = _settings.CenterX;
        _dragStartCenterY = _settings.CenterY;
        fractalImage.CaptureMouse();
    }

    private void fractalImage_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Point current = e.GetPosition(fractalImage);
        double deltaX = current.X - _dragStart.X;
        double deltaY = current.Y - _dragStart.Y;

        GetDisplayedImageSize(out double displayedWidth, out double displayedHeight);

        if (displayedWidth <= 0 || displayedHeight <= 0)
        {
            return;
        }

        double pixelDeltaX = deltaX * (_settings.Width / displayedWidth);
        double pixelDeltaY = deltaY * (_settings.Height / displayedHeight);

        double visibleWidth = 3.5 / _settings.Zoom;
        double visibleHeight = 2.0 / _settings.Zoom;

        _settings.CenterX = _dragStartCenterX - (pixelDeltaX / _settings.Width) * visibleWidth;
        _settings.CenterY = _dragStartCenterY + (pixelDeltaY / _settings.Height) * visibleHeight;

        _ = RenderAsync();
    }

    private void fractalImage_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        fractalImage.ReleaseMouseCapture();
    }

    private bool TryGetPixelFromMouse(Point mousePosition, out double pixelX, out double pixelY)
    {
        GetDisplayedImageRect(out Rect displayRect);

        if (!displayRect.Contains(mousePosition))
        {
            pixelX = 0;
            pixelY = 0;
            return false;
        }

        double normalizedX = (mousePosition.X - displayRect.Left) / displayRect.Width;
        double normalizedY = (mousePosition.Y - displayRect.Top) / displayRect.Height;

        pixelX = normalizedX * _settings.Width;
        pixelY = normalizedY * _settings.Height;

        return true;
    }

    private void GetDisplayedImageSize(out double width, out double height)
    {
        GetDisplayedImageRect(out Rect rect);
        width = rect.Width;
        height = rect.Height;
    }

    private void GetDisplayedImageRect(out Rect rect)
    {
        double sourceWidth = _settings.Width;
        double sourceHeight = _settings.Height;
        double hostWidth = fractalImage.ActualWidth;
        double hostHeight = fractalImage.ActualHeight;

        if (hostWidth <= 0 || hostHeight <= 0 || sourceWidth <= 0 || sourceHeight <= 0)
        {
            rect = Rect.Empty;
            return;
        }

        double scale = Math.Min(hostWidth / sourceWidth, hostHeight / sourceHeight);
        double drawWidth = sourceWidth * scale;
        double drawHeight = sourceHeight * scale;
        double left = (hostWidth - drawWidth) / 2.0;
        double top = (hostHeight - drawHeight) / 2.0;

        rect = new Rect(left, top, drawWidth, drawHeight);
    }
}
