using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DeviceTesterUI.Animations
{
    /// <summary>
    /// Interaction logic for LoadingSpinner.xaml
    /// </summary>
    public partial class LoadingSpinner : UserControl
    {
        public LoadingSpinner()
        {
            InitializeComponent();
            UpdateArc();
        }

        // Spinner Size
        public double SpinnerSize
        {
            get => (double)GetValue(SpinnerSizeProperty);
            set => SetValue(SpinnerSizeProperty, value);
        }
        public static readonly DependencyProperty SpinnerSizeProperty =
            DependencyProperty.Register(nameof(SpinnerSize), typeof(double), typeof(LoadingSpinner),
                new PropertyMetadata(50.0, OnPropertiesChanged));

        // Arc Brush (Foreground)
        public Brush ArcBrush
        {
            get => (Brush)GetValue(ArcBrushProperty);
            set => SetValue(ArcBrushProperty, value);
        }
        public static readonly DependencyProperty ArcBrushProperty =
            DependencyProperty.Register(nameof(ArcBrush), typeof(Brush), typeof(LoadingSpinner), new PropertyMetadata(Brushes.DodgerBlue));

        // Background Brush
        public Brush BackgroundBrush
        {
            get => (Brush)GetValue(BackgroundBrushProperty);
            set => SetValue(BackgroundBrushProperty, value);
        }
        public static readonly DependencyProperty BackgroundBrushProperty =
            DependencyProperty.Register(nameof(BackgroundBrush), typeof(Brush), typeof(LoadingSpinner), new PropertyMetadata(Brushes.LightGray));

        // Stroke Thickness
        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(LoadingSpinner),
                new PropertyMetadata(4.0, OnPropertiesChanged));

        // Arc Length in degrees (e.g., 90 = quarter circle)
        public double ArcLength
        {
            get => (double)GetValue(ArcLengthProperty);
            set => SetValue(ArcLengthProperty, value);
        }
        public static readonly DependencyProperty ArcLengthProperty =
            DependencyProperty.Register(nameof(ArcLength), typeof(double), typeof(LoadingSpinner),
                new PropertyMetadata(90.0, OnPropertiesChanged));

        // Rotation Duration
        public Duration RotationDuration
        {
            get => (Duration)GetValue(RotationDurationProperty);
            set => SetValue(RotationDurationProperty, value);
        }
        public static readonly DependencyProperty RotationDurationProperty =
            DependencyProperty.Register(nameof(RotationDuration), typeof(Duration), typeof(LoadingSpinner),
                new PropertyMetadata(new Duration(System.TimeSpan.FromSeconds(1.5))));

        // Arc Geometry Properties
        public Point ArcStartPoint { get; private set; }
        public Point ArcEndPoint { get; private set; }
        public Size ArcSize { get; private set; }
        public bool IsLargeArc { get; private set; }

        private static void OnPropertiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LoadingSpinner spinner)
            {
                spinner.UpdateArc();
            }
        }

        private void UpdateArc()
        {
            double radius = SpinnerSize / 2 - StrokeThickness / 2;
            ArcSize = new Size(radius, radius);

            double startAngle = 0;
            double endAngle = ArcLength;

            // Convert degrees to radians
            double startRad = startAngle * Math.PI / 180;
            double endRad = endAngle * Math.PI / 180;

            Point center = new Point(SpinnerSize / 2, SpinnerSize / 2);

            ArcStartPoint = new Point(
                center.X + radius * Math.Cos(startRad),
                center.Y + radius * Math.Sin(startRad));

            ArcEndPoint = new Point(
                center.X + radius * Math.Cos(endRad),
                center.Y + radius * Math.Sin(endRad));

            IsLargeArc = ArcLength > 180;

            // Force refresh
            DataContext = null;
            DataContext = this;
        }
    }
}
