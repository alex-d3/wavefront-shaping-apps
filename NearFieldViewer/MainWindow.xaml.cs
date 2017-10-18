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
using NearFieldViewer.ViewModels;

namespace NearFieldViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FieldViewModel _fieldViewModel;

        private bool roiDrawingStarted = false;
        private Point startPoint;
        private Rectangle roi;

        public MainWindow()
        {
            InitializeComponent();
            _fieldViewModel = new FieldViewModel(new Models.FieldModel());
            DataContext = _fieldViewModel;
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void image_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(image);
            p.X = _fieldViewModel.NodesX / image.ActualWidth * p.X * _fieldViewModel.StepX + _fieldViewModel.MinX;
            p.Y = -(_fieldViewModel.NodesY / image.ActualHeight * p.Y * _fieldViewModel.StepY) - _fieldViewModel.MinY;
            statusBar_text.Text = string.Format("Position: {0}; {1}", p.X.ToString("G2"), p.Y.ToString("G2"));
        }

        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            roiDrawingStarted = true;

            startPoint = e.GetPosition(canvas);

            if (roi == null)
            {
                roi = new Rectangle();
                roi.Stroke = Brushes.Green;
                roi.StrokeThickness = 1.0;
            }

            roi.Width = 0.0;
            roi.Height = 0.0;

            Canvas.SetLeft(roi, startPoint.X);
            Canvas.SetTop(roi, startPoint.Y);

            if (!canvas.Children.Contains(roi))
                canvas.Children.Add(roi);
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(image);
            p.X = _fieldViewModel.NodesX / image.ActualWidth * p.X * _fieldViewModel.StepX + _fieldViewModel.MinX;
            p.Y = -(_fieldViewModel.NodesY / image.ActualHeight * p.Y * _fieldViewModel.StepY) - _fieldViewModel.MinY;
            statusBar_text.Text = string.Format("Position: {0}; {1}", p.X.ToString("G2"), p.Y.ToString("G2"));

            if (e.LeftButton == MouseButtonState.Released || roi == null)
                return;

            Point pos = e.GetPosition(canvas);

            double x = Math.Min(pos.X, startPoint.X);
            double y = Math.Min(pos.Y, startPoint.Y);

            double w = Math.Max(pos.X, startPoint.X) - x;
            double h = Math.Max(pos.Y, startPoint.Y) - y;

            roi.Width = w;
            roi.Height = h;

            Canvas.SetLeft(roi, x);
            Canvas.SetTop(roi, y);

            _fieldViewModel.RoiHeight = Convert.ToInt32(roi.ActualWidth / canvas.ActualWidth * _fieldViewModel.NodesX);
            _fieldViewModel.RoiWidth = Convert.ToInt32(roi.ActualHeight / canvas.ActualHeight * _fieldViewModel.NodesY);
            _fieldViewModel.RoiX = Convert.ToInt32(Canvas.GetLeft(roi) / canvas.ActualWidth * _fieldViewModel.NodesX);
            _fieldViewModel.RoiY = Convert.ToInt32(Canvas.GetTop(roi) / canvas.ActualHeight * _fieldViewModel.NodesY);
        }

        private void canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (roi == null)
                return;

            _fieldViewModel.RoiHeight = Convert.ToInt32(roi.ActualWidth / canvas.ActualWidth * _fieldViewModel.NodesX);
            _fieldViewModel.RoiWidth = Convert.ToInt32(roi.ActualHeight / canvas.ActualHeight * _fieldViewModel.NodesY);
            _fieldViewModel.RoiX = Convert.ToInt32(Canvas.GetLeft(roi) / canvas.ActualWidth * _fieldViewModel.NodesX);
            _fieldViewModel.RoiY = Convert.ToInt32(Canvas.GetTop(roi) / canvas.ActualHeight * _fieldViewModel.NodesY);

            roiDrawingStarted = false;
        }

        private void image_MouseLeave(object sender, MouseEventArgs e)
        {
            if (roiDrawingStarted)
                canvas_MouseUp(sender, null);
        }
    }
}
