using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Cursors = System.Windows.Input.Cursors;
using MessageBox = System.Windows.MessageBox;
using ColorDialog = System.Windows.Forms.ColorDialog;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using System.Windows.Media.Imaging;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Brushes = System.Drawing.Brushes;

namespace WPF {
   public partial class MainWindow : Window {
      bool isDrawing = false;

      List<Scribble> mScribbles = new();
      Pen mPen = new(System.Windows.Media.Brushes.Aqua, 2);
      Stack<List<Point>> mUndoStack = new();
      Stack<List<Point>> mRedoStack = new();
      BitmapImage backgroundImage;
      Stack<Pen> mPenUndoStack = new();
      Stack<Pen> mPenRedoStack = new();

      public MainWindow () => InitializeComponent ();

      protected override void OnRender (DrawingContext drawingContext) {
         base.OnRender (drawingContext);
         if (backgroundImage != null) drawingContext.DrawImage (backgroundImage, new Rect (0, 0, ActualWidth, ActualHeight));
         foreach (var scribble in mScribbles) if (scribble.mPoints.Count > 1)
               for (int i = 1; i < scribble.mPoints.Count; i++)
                  drawingContext.DrawLine (scribble.mPen, scribble.mPoints[i - 1], scribble.mPoints[i]);
      }

      void OnMouseDown (object sender, MouseButtonEventArgs e) {
         if (e.LeftButton == MouseButtonState.Pressed) {
            Cursor = Cursors.Pen;
            isDrawing = true;
            var newScribble = new Scribble (mPen);
            newScribble.mPoints.Add (e.GetPosition (this));
            mScribbles.Add (newScribble);
         }
      }

      void OnMouseMove (object sender, MouseEventArgs e) {
         if (isDrawing) {
            Point currentPoint = e.GetPosition (this);
            mScribbles[^1].mPoints.Add (currentPoint);
            mUndoStack.Push (new List<Point> (mScribbles[^1].mPoints));
            mRedoStack.Clear ();
            InvalidateVisual ();
         }
      }

      void OnMouseUp (object sender, MouseButtonEventArgs e) {
         Cursor = Cursors.Arrow;
         isDrawing = false;
      }

      void OnPreviewMouseDown (object sender, MouseButtonEventArgs e) {
         if (isDrawing) {
            mScribbles[^1].mPoints.Clear ();
            mScribbles.RemoveAt (mScribbles.Count - 1);
         }
         InvalidateVisual ();
      }

      private void OnPenButton (object sender, RoutedEventArgs e) {
         var colorDialog = new ColorDialog {
            AllowFullOpen = true
         };
         if (colorDialog.ShowDialog () == System.Windows.Forms.DialogResult.OK) {
            var color = new SolidColorBrush (System.Windows.Media.Color.FromArgb (colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
            mPen = new Pen (color, 2);
            var newScribble = new Scribble (mPen);
            mScribbles.Add (newScribble);

            InvalidateVisual ();
         }
      }
      void OnEraserButton (object sender, RoutedEventArgs e) {
       
      }
     

      void OnSaveButton (object sender, RoutedEventArgs e) {
         SaveFileDialog saveFileDialog = new SaveFileDialog () {
            Filter = "Text files (*.txt)|*.txt|Binary files (*.bin)|*.bin|Image files (*.png)|*.png|All files (*.*)|*.*"
         };
         if (saveFileDialog.ShowDialog () == true) {
            string filePath = saveFileDialog.FileName;
            string extension = System.IO.Path.GetExtension (filePath).ToLower ();
            switch (extension) {
               case ".txt":
                  SaveAsText (filePath);
                  break;
               case ".bin":
                  SaveAsBinary (filePath);
                  break;
               case ".png":
                  SaveAsImage (filePath);
                  break;
               default:
                  MessageBox.Show ("Unsupported file format.");
                  break;
            }
         }
      }

      void SaveAsBinary (string filePath) {
         using (FileStream fs = new (filePath, FileMode.Create)) {
            using (BinaryWriter bw = new (fs)) {
               foreach (var scribble in mScribbles) {
                  // Serialize pen color
                  bw.Write (scribble.mPen.Brush.GetType ().ToString ());
                  bw.Write (scribble.mPen.Brush.ToString ());
                  foreach (var point in scribble.mPoints) {
                     bw.Write (point.X);
                     bw.Write (point.Y);
                  }
                  bw.Write (double.NaN);
               }
            }
         }
      }

      void LoadFromBinary (string filePath) {
         using (FileStream fs = new (filePath, FileMode.Open)) {
            using BinaryReader reader = new (fs);
            mScribbles.Clear ();
            while (reader.BaseStream.Position < reader.BaseStream.Length) {
               string brushType = reader.ReadString ();
               string brushValue = reader.ReadString ();
               System.Windows.Media.Brush brush = (System.Windows.Media.Brush)new BrushConverter ().ConvertFromString (brushValue);
               Pen pen = new (brush, 2);
               var scribble = new Scribble (pen);
               while (true) {
                  double x = reader.ReadDouble ();
                  if (double.IsNaN (x)) {
                     mScribbles.Add (scribble);
                     break;
                  }
                  double y = reader.ReadDouble ();
                  scribble.mPoints.Add (new Point (x, y));
               }
            }
         }
         InvalidateVisual ();
      }

      void SaveAsImage (string filePath) {
         RenderTargetBitmap rtb = new ((int)ActualWidth, (int)ActualHeight, 96, 96, PixelFormats.Pbgra32);
         rtb.Render (this);
         PngBitmapEncoder encoder = new ();
         encoder.Frames.Add (BitmapFrame.Create (rtb));
         using FileStream fs = new (filePath, FileMode.Create);
         encoder.Save (fs);
      }

      void OnNewButton (object sender, RoutedEventArgs e) {
         mScribbles.Clear ();
         backgroundImage = null;
         InvalidateVisual ();

      }

      void OnOpenButton (object sender, RoutedEventArgs e) {
         OpenFileDialog openFileDialog = new () {
            Filter = "Text files (*.txt)|*.txt|Binary files (*.bin)|*.bin|Image files (*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif|All files (*.*)|*.*"
         };
         if (openFileDialog.ShowDialog () == true) {
            string filePath = openFileDialog.FileName;
            string extension = System.IO.Path.GetExtension (filePath).ToLower ();
            switch (extension) {
               case ".txt":
                  LoadFromText (filePath);
                  break;
               case ".bin":
                  LoadFromBinary (filePath);
                  break;
               case ".png":
               case ".jpg":
               case ".jpeg":
               case ".gif":
                  LoadFromImage (filePath);
                  break;
               default:
                  MessageBox.Show ("Unsupported file format.");
                  break;
            }
         }
      }

      void SaveAsText (string filePath) {
         using StreamWriter sw = new (filePath);
         foreach (var scribble in mScribbles) {
            sw.WriteLine (scribble.mPen.Brush.ToString ());
            foreach (var point in scribble.mPoints) sw.WriteLine ($"{point.X},{point.Y}");
            sw.WriteLine ("END");
         }
      }

      void LoadFromText (string filePath) {
         string[] lines = File.ReadAllLines (filePath);
         mScribbles.Clear ();
         var penConverter = new BrushConverter ();
         var scribble = new Scribble (mPen);
         foreach (string line in lines) {
            if (line == "END") {
               mScribbles.Add (scribble);
               scribble = new Scribble (mPen);
            } else {
               if (line.Contains (",")) {
                  string[] parts = line.Split (',');
                  double x, y;
                  if (double.TryParse (parts[0], out x) && double.TryParse (parts[1], out y)) {
                     scribble.mPoints.Add (new Point (x, y));
                  }
               } else {
                  System.Windows.Media.Brush brush = (System.Windows.Media.Brush)penConverter.ConvertFromString (line);
                  mPen = new Pen (brush, mPen.Thickness);
                  scribble = new Scribble (mPen);
               }
            }
         }
         InvalidateVisual ();
      }

      void LoadFromImage (string filePath) {
         BitmapImage bitmap = new ();
         bitmap.BeginInit ();
         bitmap.UriSource = new Uri (filePath);
         bitmap.EndInit ();
         backgroundImage = bitmap;
         InvalidateVisual ();
      }

      void OnUndoButton (object sender, RoutedEventArgs e) {
         if (mScribbles.Count > 0) {
            mRedoStack.Push (new List<Point> (mScribbles[mScribbles.Count - 1].mPoints));
            mScribbles.RemoveAt (mScribbles.Count - 1);
            InvalidateVisual ();
         }
      }

      void OnRedoButton (object sender, RoutedEventArgs e) {
         if (mRedoStack.Count > 0) {
            var newScribble = new Scribble (mPen);
            newScribble.mPoints.AddRange (mRedoStack.Pop ());
            mScribbles.Add (newScribble);
            InvalidateVisual ();
         }
      }
   }

   public class Scribble {
      public Pen mPen { get; set; }
      public List<Point> mPoints { get; set; }

      public Scribble (Pen pen) {
         mPen = pen;
         mPoints = new List<Point> ();
      }
   }
}

