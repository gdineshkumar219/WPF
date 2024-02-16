using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Point = System.Windows.Point;
using Cursors = System.Windows.Input.Cursors;
using Pen = System.Windows.Media.Pen;
using Brushes = System.Windows.Media.Brushes;

namespace WPF {
   public partial class MainWindow : Window {
      bool isDrawing = false;
      Point mStartPoint;
      List<List<Point>> mScribbles = new List<List<Point>> ();
      Pen mPen = new Pen (Brushes.Aqua, 2);

      public Pen Pen => mPen;

      public MainWindow () => InitializeComponent ();

      protected override void OnRender (DrawingContext drawingContext) {
         base.OnRender (drawingContext);
         foreach (var scribble in mScribbles) {
            if (scribble.Count > 1)
               for (int i = 1; i < scribble.Count; i++)
                  drawingContext.DrawLine (mPen, scribble[i - 1], scribble[i]);
         }
      }

      void OnMouseDown (object sender, MouseButtonEventArgs e) {
         if (e.LeftButton == MouseButtonState.Pressed) {
            Cursor = Cursors.Pen;
            isDrawing = true;
            List<Point> list = new () { e.GetPosition (this) };
            mScribbles.Add (list);
         }
      }

      void OnMouseMove (object sender, System.Windows.Input.MouseEventArgs e) {
         if (isDrawing) {
            Point currentPoint = e.GetPosition (this);
            mScribbles[mScribbles.Count - 1].Add (currentPoint);
            InvalidateVisual (); // Request a redraw
         }
      }

      void OnMouseUp (object sender, MouseButtonEventArgs e) {
         Cursor = Cursors.Arrow;
         isDrawing = false;
      }

      void OnPreviewMouseDown (object sender, MouseButtonEventArgs e) {
         if (isDrawing) {
            mScribbles[^1].Clear (); // Clear the current scribble points
            mScribbles.RemoveAt (mScribbles.Count - 1); // Remove the current empty scribble
         }
         InvalidateVisual (); // Request a redraw
      }

      private void OnPenButton (object sender, RoutedEventArgs e) {
         var colorDialog = new ColorDialog ();
         colorDialog.AllowFullOpen = true;
         if (colorDialog.ShowDialog () == System.Windows.Forms.DialogResult.OK) {
            var color = new SolidColorBrush(System.Windows.Media.Color.FromArgb (colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
            mPen.Brush = color;
            InvalidateVisual (); // Request a redraw

         }
      }
   }
}
