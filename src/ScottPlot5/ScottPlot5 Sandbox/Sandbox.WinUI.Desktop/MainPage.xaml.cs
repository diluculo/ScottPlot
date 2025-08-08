using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ScottPlot;
using ScottPlot.AxisRules;
using System;

namespace Sandbox.WinUI;

public sealed partial class MainPage : Page
{
    private bool _isLargeSize = true;
    private bool _plotInitialized = false;

    public MainPage()
    {
        InitializeComponent();
        PlotControl.AppWindow = App.MainWindow;
        PlotControl.UserInputProcessor.IsEnabled = true;
    }

    private void OnBackgroundSettingChanged(object sender, RoutedEventArgs e)
    {
        if (!_plotInitialized) return;

        var plot = PlotControl.Plot;

        if (TransparentBackgroundCheckBox.IsChecked == true)
        {
            plot.FigureBackground.Color = ScottPlot.Colors.Transparent;
            plot.DataBackground.Color = ScottPlot.Colors.Transparent;
        }
        else
        {
            plot.FigureBackground.Color = ScottPlot.Colors.White;
            plot.DataBackground.Color = ScottPlot.Colors.White;
        }

        PlotControl.Refresh();
    }

    private void OnAxisRuleChanged(object sender, RoutedEventArgs e)
    {
        if (!_plotInitialized) return;

        ApplySelectedAxisRule();

        PlotControl.Refresh();
    }

    private void OnAutoScaleClick(object sender, RoutedEventArgs e)
    {
        if (!_plotInitialized) return;

        var plot = PlotControl.Plot;

        // Clear existing rules first
        plot.Axes.Rules.Clear();

        // Apply AutoScale
        plot.Axes.AutoScale();

        // Reset UI to None
        NoRuleRadioButton.IsChecked = true;

        PlotControl.Refresh();
    }

    private void OnReapplyRuleClick(object sender, RoutedEventArgs e)
    {
        if (!_plotInitialized) return;

        // Clear existing rules and apply selected rule
        var plot = PlotControl.Plot;

        plot.Axes.Rules.Clear();

        ApplySelectedAxisRule();

        PlotControl.Refresh();
    }

    private void OnToggleSizeClick(object sender, RoutedEventArgs e)
    {
        if (_isLargeSize)
        {
            PlotControl.Width = 400;
            PlotControl.Height = 300;
            ToggleSizeButton.Content = "Restore Size";
        }
        else
        {
            PlotControl.Width = double.NaN; // Auto
            PlotControl.Height = double.NaN; // Auto
            ToggleSizeButton.Content = "Toggle Plot Size";
        }

        _isLargeSize = !_isLargeSize;
    }

    private void OnPlotControlLoaded(object sender, RoutedEventArgs e)
    {
        InitializePlot();
        _plotInitialized = true;
        PlotControl.Refresh();
    }

    private void InitializePlot()
    {
        var plot = PlotControl.Plot;
        plot.Clear();

        // Create circle data
        double[] xData = new double[101];
        double[] yData = new double[101];

        for (int i = 0; i < 101; i++)
        {
            double angle = 2 * Math.PI * i / 100;
            xData[i] = 0.1 * Math.Cos(angle);
            yData[i] = 0.1 * Math.Sin(angle);
        }

        // Add scatter plot
        var scatter = plot.Add.Scatter(xData, yData);
        scatter.Color = ScottPlot.Color.FromHex("#1976D2");
        scatter.LineWidth = 2;
        scatter.MarkerSize = 0;

        // Set initial background (transparent by default)
        plot.FigureBackground.Color = ScottPlot.Colors.Transparent;
        plot.DataBackground.Color = ScottPlot.Colors.Transparent;

        // Set axis labels
        plot.Axes.Left.Label.Text = "Y Axis";
        plot.Axes.Bottom.Label.Text = "X Axis";

        plot.Title("Circle with Axis Rules Test");

        // Add margins and auto scale
        plot.Axes.Margins(0.1, 0.1);
        plot.Axes.AutoScale();
    }

    private void ApplySelectedAxisRule()
    {
        var plot = PlotControl.Plot;
        plot.Axes.Rules.Clear();

        if (SquareRuleRadioButton.IsChecked == true)
        {
            plot.Axes.Rules.Add(new SquareZoomOut(plot.Axes.Bottom, plot.Axes.Left));
        }
        else if (SquarePreserveXRadioButton.IsChecked == true)
        {
            plot.Axes.Rules.Add(new SquarePreserveX(plot.Axes.Bottom, plot.Axes.Left));
        }
        else if (SquarePreserveYRadioButton.IsChecked == true)
        {
            plot.Axes.Rules.Add(new SquarePreserveY(plot.Axes.Bottom, plot.Axes.Left));
        }
        else if (AutoSquareRadioButton.IsChecked == true)
        {
            plot.Axes.Rules.Add(new AutoSquareRule(plot.Axes.Bottom, plot.Axes.Left));
        }
    }
}

/// <summary>
/// Always performs AutoScale to fit all data first, then applies square pixel aspect ratio.
/// This rule ensures that data is always visible and circles appear as perfect circles.
/// </summary>
/// <param name="xAxis">The horizontal axis to control</param>
/// <param name="yAxis">The vertical axis to control</param>
public class AutoSquareRule(IXAxis xAxis, IYAxis yAxis) : IAxisRule
{
    private readonly IXAxis _xAxis = xAxis ?? throw new ArgumentNullException(nameof(xAxis));
    private readonly IYAxis _yAxis = yAxis ?? throw new ArgumentNullException(nameof(yAxis));

    /// <summary>
    /// Applies the AutoScale + Square rule to the axes
    /// </summary>
    /// <param name="rp">Render pack containing plot and layout information</param>
    /// <param name="beforeLayout">If true, this method returns early since DataRect is needed</param>
    public void Apply(RenderPack rp, bool beforeLayout)
    {
        // Rules that refer to the DataRect must wait for the layout to occur
        if (beforeLayout)
            return;

        // Step 1: Use existing AutoScaler with its configured margins
        var autoScaler = rp.Plot.Axes.AutoScaler;
        var scaledLimits = autoScaler.GetAxisLimits(rp.Plot, _xAxis, _yAxis);

        // Step 2: Apply square logic directly to the calculated limits
        ApplySquareLogicToLimits(rp, scaledLimits);
    }

    /// <summary>
    /// Applies square pixel aspect ratio logic to the calculated auto-scaled limits
    /// while preserving axis inversion state
    /// </summary>
    /// <param name="rp">Render pack containing DataRect dimensions for pixel calculations</param>
    /// <param name="scaledLimits">The auto-scaled axis limits to apply square logic to</param>
    private void ApplySquareLogicToLimits(RenderPack rp, AxisLimits scaledLimits)
    {
        var xWidth = scaledLimits.HorizontalSpan;
        var yHeight = scaledLimits.VerticalSpan;
        var xCenter = scaledLimits.HorizontalCenter;
        var yCenter = scaledLimits.VerticalCenter;

        // Calculate units per pixel based on auto-scaled data
        var unitsPerPxX = xWidth / rp.DataRect.Width;
        var unitsPerPxY = yHeight / rp.DataRect.Height;
        var maxUnitsPerPx = Math.Max(unitsPerPxX, unitsPerPxY);

        // Calculate new ranges
        var halfHeight = rp.DataRect.Height / 2 * maxUnitsPerPx;
        var halfWidth = rp.DataRect.Width / 2 * maxUnitsPerPx;

        var yMin = yCenter - halfHeight;
        var yMax = yCenter + halfHeight;
        var xMin = xCenter - halfWidth;
        var xMax = xCenter + halfWidth;

        // Set final axis values once
        var invertedY = _yAxis.Min > _yAxis.Max;
        var invertedX = _xAxis.Min > _xAxis.Max;

        if (invertedY)
            _yAxis.Range.Set(yMax, yMin);
        else
            _yAxis.Range.Set(yMin, yMax);

        if (invertedX)
            _xAxis.Range.Set(xMax, xMin);
        else
            _xAxis.Range.Set(xMin, xMax);
    }
}
