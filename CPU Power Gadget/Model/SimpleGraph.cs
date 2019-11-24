using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CpuPowerGadget.Model
{
    public class SimpleGraph
    {
        private const int Stepping = 3;

        private static readonly SolidColorBrush GridLineBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));
        private static readonly SolidColorBrush GridLineLightBrush = new SolidColorBrush(Color.FromRgb(228, 228, 228));
        private static readonly DoubleCollection GridLineDashArray = DoubleCollection.Parse("3 2");

        public PathGeometry Geometry { get; }

        private readonly List<float> _history;
        private readonly PathFigure _figure;
        private readonly List<Line> _gridLines = new List<Line>();
        private readonly List<TextBlock> _axisLabels = new List<TextBlock>();

        public Canvas Canvas { get; set; }
        public Canvas AxisCanvas { get; set; }
        public float Max { get; set; }
        public float Min { get; set; }
        public SimpleGraph Primary { get; set; }
        public bool NoScale { get; set; }
        public bool Thin { get; set; }
        public float DefaultValue { get; set; }

        private float _defaultMax;
        private float _defaultMin;

        private float _width;
        private float _height;
        private float _dataPoints;

        public SimpleGraph()
        {
            _history = new List<float>();
            Geometry = new PathGeometry();
            _figure = new PathFigure();
            DefaultValue = -1;
        }

        public void Init()
        {
            if (Max > 10)
            {
                Max = (float) Math.Ceiling(Max / 10.0) * 10;
                Min = (float) Math.Floor(Min / 10.0) * 10;
            }
            _defaultMax = Max;
            _defaultMin = Min;

            _width = (int) Canvas.ActualWidth;
            _height = (int) Canvas.ActualHeight;
            _dataPoints = _width / Stepping;
            
            _figure.StartPoint = new Point(0, _height);
            _figure.Segments = new PathSegmentCollection();
            Geometry.Figures.Add(_figure);

            UpdateAxis();
            UpdateGridLines();

            for (var i = 0; i < _dataPoints; i++)
            {
                _history.Add(DefaultValue);
            }

            if (DefaultValue < 0)
            {
                UpdatePath();
            }
            else
            {
                Update(DefaultValue);
            }
        }

        private bool AutoScale(SimpleGraph other, float? otherVal)
        {
            if (Primary != null)
            {
                Max = Primary.Max;
                Min = Primary.Min;
                return false;
            }

            if (NoScale) return false;

            var historyWithValues = _history.Where(h => h >= 0).ToList();
            if (historyWithValues.Count == 0) return false;

            var changed = false;

            var histMax = _history.Where(h => h >= 0).Max();
            if (other != null)
            {
                var otherHistMax = other._history.Where(h => h >= 0).DefaultIfEmpty(0).Max();
                if (otherHistMax > histMax) histMax = otherHistMax;
                if (otherVal.HasValue && otherVal.Value > histMax) histMax = otherVal.Value;
            }
            var histMin = _history.Where(h => h >= 0).Min();

            while (histMax > Max)
            {
                changed = true;
                Max += 10;
            }

            while (histMax < Max - 10 && Max > _defaultMax)
            {
                changed = true;
                Max -= 10;
                if (Max < _defaultMax)
                {
                    Max = _defaultMax;
                    break;
                }
            }

            while (histMin < Min)
            {
                changed = true;
                Min -= 10;
            }

            while (histMin > Min + 10 && Min < _defaultMin)
            {
                changed = true;
                Min += 10;
                if (Min > _defaultMin)
                {
                    Min = _defaultMin;
                    break;
                }
            }

            return changed;
        }

        private void UpdateAxis()
        {
            if (Primary != null) return;

            foreach (var axisLabel in _axisLabels)
            {
                AxisCanvas.Children.Remove(axisLabel);
            }

            _axisLabels.Clear();

            var secondary = false;

            var step = Max < 10 ? 1 : 10;
            while ((Max - Min) / step > 12)
            {
                step += step;
            }

            for (var y = Min; y <= Max; y += step)
            {
                if (!secondary || (Max - Min) / step <= 5)
                {
                    var labelY = _height - (y - Min) / (Max - Min) * (_height - 1) - 1;

                    var axisLabel = new TextBlock
                    {
                        Text = Max < 10 ? $"{y:F1}" : $"{(int)y}",
                        FontSize = 9
                    };

                    Canvas.SetRight(axisLabel, 3);
                    Canvas.SetTop(axisLabel, (int) labelY + 4);

                    AxisCanvas.Children.Add(axisLabel);
                    _axisLabels.Add(axisLabel);
                }

                secondary = !secondary;
            }
        }

        private void UpdateGridLines()
        {
            if (Primary != null) return;

            foreach (var gridLine in _gridLines)
            {
                Canvas.Children.Remove(gridLine);
            }

            _gridLines.Clear();

            var secondary = false;

            var step = Max < 10 ? 1 : 10;
            while ((Max - Min) / step > 12)
            {
                step += step;
            }

            for (var y = Min; y <= Max; y += step)
            {
                var gridY = _height - (y - Min) / (Max - Min) * (_height - 1) - 1;

                var gridLine = new Line
                {
                    Stroke = secondary ? GridLineLightBrush : GridLineBrush,
                    StrokeThickness = 1,
                    StrokeDashArray = GridLineDashArray,
                    X2 = _width,
                    Y1 = gridY + 0.5,
                    Y2 = gridY + 0.5,
                    SnapsToDevicePixels = true
                };

                secondary = !secondary;

                Canvas.Children.Add(gridLine);
                _gridLines.Add(gridLine);
            }
        }

        private void UpdatePath()
        {
            double t = 0;
            var delta = _width / _dataPoints;
            var first = true;

            foreach (var val in _history)
            {
                if (val < 0) continue;

                var y = _height - (val - Min) / (Max - Min) * (_height - 2) - 1;

                var lineSegment = new LineSegment {Point = new Point((int) t, (int) y + (Thin ? -0.5 : 0))};
                if (first)
                {
                    lineSegment.IsStroked = false;
                    first = false;
                }
                _figure.Segments.Add(lineSegment);
                t += delta;
            }

            t -= delta;
            var lineSegmentEnd = new LineSegment {Point = new Point((int) t, _height), IsStroked = false};
            _figure.Segments.Add(lineSegmentEnd);
        }

        public void Update(float? val, SimpleGraph other = null, float? otherVal = null)
        {
            if (!val.HasValue) return;

            _history.RemoveAt(0);
            _history.Add(val.Value);

            if (AutoScale(other, otherVal))
            {
                UpdateAxis();
                UpdateGridLines();
            }

            _figure.Segments.Clear();

            UpdatePath();
        }
    }
}
