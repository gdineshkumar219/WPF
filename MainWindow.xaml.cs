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
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using static WPF.Drawing.EShapeType;
using static WPF.Drawing;
using System.ComponentModel;
using Point = System.Windows.Point;
namespace WPF;
public partial class MainWindow : Window {
   #region Constructor -------------------------------------------------------------------------
   /// <summary> Constructor for the MainWindow </summary>
   public MainWindow () {
      InitializeComponent ();
      WindowState = WindowState.Maximized;
      Closing += OnClosing;
   }
   #endregion

   #region ClickEvents -------------------------------------------------------------------------
   /// <summary>
   /// 
   /// </summary>
   /// <param name="sender"></param>
   /// <param name="e"></param>
   void OnClosing (object? sender, CancelEventArgs e) {
      if (MessageBox.Show (this, "Do you really want to close ?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
         e.Cancel = true;
   }

   /// <summary> Sets the current drawing state to line </summary>
   void OnDrawLine (object sender, RoutedEventArgs e) => currentDrawingState = LINE;

   /// <summary> Sets the current drawing state to rectangle </summary>
   void OnDrawRectangle (object sender, RoutedEventArgs e) => currentDrawingState = RECTANGLE;

   /// <summary> Sets the current drawing state to circle </summary>
   void OnDrawCircle (object sender, RoutedEventArgs e) => currentDrawingState = CIRCLE;

   /// <summary> Sets the current drawing state to circle </summary>
   void OnScribble (object sender, RoutedEventArgs e) => currentDrawingState = SCRIBBLE;

   /// <summary> Handles the MouseDown event </summary>
   void OnMouseDown (object sender, MouseButtonEventArgs e) {
      if (e.LeftButton == MouseButtonState.Pressed) {
         Cursor = Cursors.Pen;
         isDrawing = true;
         startPoint = e.GetPosition (this);
         switch (currentDrawingState) {
            case NULL:
               startPoint = e.GetPosition (this);
               currentDrawingState = NULL;
               break;
            case LINE:
               mLineDrawing = new Line (mPen);
               mDrawings.Add (mLineDrawing);
               mLineDrawing.StartPoint = startPoint;
               break;
            case RECTANGLE:
               mRectDrawing = new Rectangle (mPen);
               mRectDrawing.StartPoint = startPoint;
               mDrawings.Add (mRectDrawing);
               break;
            case CIRCLE:
               mCircleDrawing = new Circle (mPen);
               mDrawings.Add (mCircleDrawing);
               mCircleDrawing.StartPoint = startPoint;
               break;

            case SCRIBBLE:
               mScribble = new (mPen);
               mDrawings.Add (mScribble);
               mScribble.AddScribblePoints (startPoint);
               break;
         }
      }
   }

   /// <summary> Handles the MouseMove event </summary>
   void OnMouseMove (object sender, MouseEventArgs e) {
      if (e.LeftButton == MouseButtonState.Pressed && isDrawing) {
         System.Windows.Point currentPoint = e.GetPosition (this);
         switch (currentDrawingState) {
            case LINE:
               mLineDrawing.EndPoint = currentPoint; break;
            case RECTANGLE:
               mRectDrawing.EndPoint = currentPoint; break;
            case CIRCLE:
               mCircleDrawing.EndPoint = currentPoint; break;
            case SCRIBBLE:
               mScribble.AddScribblePoints (currentPoint); break;
         }
      }
      InvalidateVisual ();
   }

   /// <summary> Handles the MouseUp event </summary>
   void OnMouseUp (object sender, MouseButtonEventArgs e) {
      if (e.LeftButton == MouseButtonState.Released && isDrawing) {
         endPoint = e.GetPosition (this);
         switch (currentDrawingState) {
            case LINE:
               mLineDrawing.EndPoint = endPoint; break;
            case RECTANGLE:
               mRectDrawing.EndPoint = endPoint; break;
            case CIRCLE:
               mCircleDrawing.EndPoint = endPoint; break;
            case SCRIBBLE:
               mScribble.AddScribblePoints (endPoint); break;
         }
      }
      mUndoStack.Push (mDrawings[^1]);
      InvalidateVisual ();
      mRedoStack.Clear ();
      Cursor = Cursors.Arrow;
      isDrawing = false;
   }

   /// <summary> Clears the page and create new page </summary>
   void OnNewButton (object sender, RoutedEventArgs e) {
      mUndoStack.Clear ();
      mRedoStack.Clear ();
      mDrawings.Clear ();
      backgroundImage = null;
      InvalidateVisual ();
   }

   /// <summary> Opens file from different format </summary>
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

   /// <summary> To change the pen color </summary>
   void OnPenButton (object sender, RoutedEventArgs e) {
      var colorDialog = new ColorDialog {
         AllowFullOpen = true
      };
      if (colorDialog.ShowDialog () == System.Windows.Forms.DialogResult.OK) {
         var color = new SolidColorBrush (System.Windows.Media.Color.FromArgb (colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
         mPen = new Pen (color, 2);
         var newScribble = new Scribble (mPen);
         mScribbles.Add ((newScribble, mPen));

         InvalidateVisual ();
      }
   }

   /// <summary> Handles redo property </summary>
   void OnRedoButton (object sender, RoutedEventArgs e) {
      if (mRedoStack.Count > 0) {
         mDrawings.Add (mRedoStack.Pop ());
         InvalidateVisual ();
      }
   }

   /// <summary> Handles undo operation </summary>
   void OnUndoButton (object sender, RoutedEventArgs e) {
      if (mDrawings.Count > 0 && mUndoStack.Count != 0) {
         mDrawings.RemoveAt (mDrawings.Count - 1);
         mRedoStack.Push (mUndoStack.Pop ());
         InvalidateVisual ();
      }
   }

   /// <summary> Save drawings as specified format </summary>
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
   #endregion

   #region Methods -----------------------------------------------------------------------------
   /// <summary> To draw various shapes </summary>
   protected override void OnRender (DrawingContext drawingContext) {
      base.OnRender (drawingContext);
      foreach (Drawing drawing in mDrawings) {
         drawing.DrawShapes (drawingContext);
      }
   }

   /// <summary> Saves drawings as a text </summary>
   /// <param name="filePath"></param>
   void SaveAsText (string filePath) {
      using StreamWriter sw = new (filePath);
      foreach (var drawing in mDrawings) drawing.SaveText (sw);
   }

   /// <summary> Saves drawings as a binary file </summary>
   /// <param name="filePath"></param>
   void SaveAsBinary (string filePath) {
      using FileStream fs = new (filePath, FileMode.Create);
      using BinaryWriter bw = new (fs);
      foreach (var drawing in mDrawings) drawing.SaveBinary (bw);
   }

   /// <summary> Load drawings from the text file </summary>
   /// <param name="filePath"></param>
   void LoadFromText (string filePath) {
      using StreamReader sr = new (filePath);
      Drawing? drawing;
      while (sr.Peek () != -1) {
         string? parts = sr.ReadLine ();
         if (Enum.TryParse (parts, out EShapeType shapeType)) {
            drawing = shapeType switch {
               SCRIBBLE => new Scribble (mPen),
               LINE => new Line (mPen),
               RECTANGLE => new Rectangle (mPen),
               CIRCLE => new Circle (mPen),
               _ => null
            };
            if (drawing == null) break;
            mDrawings.Add (drawing.LoadText (sr));
         }
      }
      InvalidateVisual ();
   }

   /// <summary> Load drawings from binary file </summary>
   /// <param name="filePath"></param>
   void LoadFromBinary (string filePath) {
      using (FileStream fs = new (filePath, FileMode.Open)) {
         using BinaryReader br = new (fs);
         Drawing? drawing;
         while (br.PeekChar () != -1) {
            EShapeType shapeType = (EShapeType)br.ReadInt32 ();
            drawing = shapeType switch {
               SCRIBBLE => new Scribble (mPen),
               LINE => new Line (mPen),
               RECTANGLE => new Rectangle (mPen),
               CIRCLE => new Circle (mPen),
               _ => null
            };
            if (drawing == null) break;
            mDrawings.Add (drawing.LoadBinary (br));
         }
      }
      InvalidateVisual ();
   }

   /// <summary>
   /// Save drawings as image
   /// </summary>
   /// <param name="filePath"></param>
   void SaveAsImage (string filePath) {
      RenderTargetBitmap rtb = new ((int)ActualWidth, (int)ActualHeight, 96, 96, PixelFormats.Pbgra32);
      rtb.Render (this);
      PngBitmapEncoder encoder = new ();
      encoder.Frames.Add (BitmapFrame.Create (rtb));
      using FileStream fs = new (filePath, FileMode.Create);
      encoder.Save (fs);
   }

   /// <summary>
   /// Load drawings from image
   /// </summary>
   /// <param name="filePath"></param>
   void LoadFromImage (string filePath) {
      BitmapImage bitmap = new ();
      bitmap.BeginInit ();
      bitmap.UriSource = new Uri (filePath);
      bitmap.EndInit ();
      backgroundImage = bitmap;
      InvalidateVisual ();
   }
   #endregion

   #region Private Data ---------------------------------------------------------------------------
   EShapeType currentDrawingState = SCRIBBLE;
   Point startPoint;
   Point endPoint;
   bool isDrawing = false;
   List<Drawing> mDrawings = new ();
   List<(Scribble, Pen)> mScribbles = new ();
   Pen mPen = new (System.Windows.Media.Brushes.Aqua, 2);
   BitmapImage backgroundImage;
   Stack<Drawing> mUndoStack = new ();
   Stack<Drawing> mRedoStack = new ();
   Line mLineDrawing;
   Scribble mScribble;
   Rectangle mRectDrawing;
   Circle mCircleDrawing;
   #endregion
}