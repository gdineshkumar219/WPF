using System.IO;
using System.Windows;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace WPF;

#region Drawing -----------------------------------------------------------------------------------
/// <summary> Base class for different types of drawings</summary>
public class Drawing {

   #region Properties -----------------------------------------------------------------------------
   /// <summary> Gets or sets the type of the shape </summary>
   public EShapeType Type { get; set; }

   /// <summary> Gets or sets the start point of the shape </summary>
   public Point StartPoint { get; set; }

   /// <summary> Gets or sets the end point of the shape </summary>
   public Point EndPoint { get; set; }

   /// <summary> Gets or sets the brush used to draw the shape </summary>
   public Brush? Brush { get; set; }

   /// <summary> Gets or sets the thickness of the shape </summary>
   public double Thickness { get; set; }
   #endregion

   #region Methods --------------------------------------------------------------------------------
   /// <summary> Draws the shape on the specified drawing context </summary>
   /// <param name="dc">The drawing context </param>
   public virtual void DrawShapes (DrawingContext dc) {
   }

   /// <summary> Saves the shape as text to the specified text writer </summary>
   /// <param name="tw">The text writer </param>
   public virtual void SaveText (TextWriter tw) {
      tw.WriteLine ($"{Type}\n{StartPoint.X},{StartPoint.Y},{EndPoint.X},{EndPoint.Y},{Thickness},{Brush}");

   }

   /// <summary> Saves the shape as binary to the specified binary writer </summary>
   /// <param name="bw">The binary writer </param>
   public virtual void SaveBinary (BinaryWriter bw) {
      bw.Write ((int)Type);
      bw.Write (StartPoint.X);
      bw.Write (StartPoint.Y);
      bw.Write (EndPoint.X);
      bw.Write (EndPoint.Y);
      bw.Write (Thickness);
      var brushConverter = new BrushConverter ();
      string? brushString = brushConverter.ConvertToString (Brush);
      bw.Write (brushString);
   }

   /// <summary> Loads the shape from the specified text reader </summary>
   /// <param name="sr">The text reader </param>
   /// <returns>The loaded shape </returns>
   public virtual Drawing LoadText (StreamReader sr) {
      string[] parts = sr.ReadLine ().Split (',');
      //Type = (ShapeType)Enum.Parse (typeof (ShapeType), parts[0]);
      StartPoint = new Point (double.Parse (parts[0]), double.Parse (parts[1]));
      EndPoint = new Point (double.Parse (parts[2]), double.Parse (parts[3]));
      Thickness = double.Parse (parts[4]);
      var brushConverter = new BrushConverter ();
      Brush = (Brush)brushConverter.ConvertFromString (parts[5]);
      return this;
   }

   /// <summary> Loads the shape from the specified binary reader </summary>
   /// <param name="br">The binary reader </param>
   /// <returns>The loaded shape </returns>
   public virtual Drawing LoadBinary (BinaryReader br) {
      StartPoint = new Point (br.ReadDouble (), br.ReadDouble ());
      EndPoint = new Point (br.ReadDouble (), br.ReadDouble ());
      Thickness = br.ReadDouble ();
      string brushString = br.ReadString ();
      var brushConverter = new BrushConverter ();
      Brush = (Brush)brushConverter.ConvertFromString (brushString);
      return this;
   }
   #endregion

   #region Enmerations ----------------------------------------------------------------------------
   /// <summary> Enumeration representing different types of shapes </summary>
   public enum EShapeType {
      SCRIBBLE,
      LINE,
      RECTANGLE,
      CIRCLE,
      NULL
   }
   #endregion
}
#endregion

#region Scribble ----------------------------------------------------------------------------------
/// <summary>
/// Class representing a scribble drawing.
/// </summary>
class Scribble : Drawing {

   #region Constructor ----------------------------------------------------------------------------
   /// <summary>Initializes a new instance of the <see cref="Scribble"/> class </summary>
   /// <param name="pen">The pen used to draw the scribble </param>
   public Scribble (Pen pen) {
      Brush = pen.Brush;
      Thickness = pen.Thickness = 2;
      Type = EShapeType.SCRIBBLE;
      mPoints = new List<System.Windows.Point> ();
   }
   #endregion

   #region Methods --------------------------------------------------------------------------------
   /// <summary>Adds a point to the scribble </summary>
   /// <param name="point">The point to add </param>
   public void AddScribblePoints (Point point) => mPoints.Add (point);

   public override void DrawShapes (DrawingContext dc) {
      var pen = new Pen (Brush, Thickness);
      if (mPoints.Count > 1) {
         for (int i = 1; i < mPoints.Count; i++) {
            var startPoint = mPoints[i - 1];
            var endPoint = mPoints[i];
            dc.DrawLine (pen, startPoint, endPoint);
         }
      }
   }

   public override void SaveText (TextWriter tw) {
      base.SaveText (tw);
      foreach (var point in mPoints) tw.WriteLine ($"{point.X},{point.Y}");
   }

   public override void SaveBinary (BinaryWriter bw) {
      base.SaveBinary (bw);
      foreach (var point in mPoints) {
         bw.Write (point.X);
         bw.Write (point.Y);
      }
      bw.Write (double.NaN);
   }

   public override Drawing LoadText (StreamReader sr) {
      base.LoadText (sr);
      string line;
      while (char.IsDigit ((char)sr.Peek ())) {
         string[] parts = sr.ReadLine ().Split (',');
         mPoints.Add (new Point (double.Parse (parts[0]), double.Parse (parts[1])));
      }
      return this;
   }

   public override Drawing LoadBinary (BinaryReader br) {
      base.LoadBinary (br);
      while (true) {
         double x = br.ReadDouble ();
         if (double.IsNaN (x)) break;
         double y = br.ReadDouble ();
         mPoints.Add (new Point (x, y));
      }
      return this;
   }
   #endregion

   #region Members --------------------------------------------------------------------------------
   public List<Point> mPoints; // List of points defining the scribble
   #endregion
}
#endregion

#region Line --------------------------------------------------------------------------------------
/// <summary> Class representing a line drawing </summary>
class Line : Drawing {
   #region Constructor ----------------------------------------------------------------------------
   /// <summary> Initializes a new instance of the <see cref="Line"/> class </summary>
   /// <param name="pen">The pen used to draw the line </param>
   public Line (Pen pen) {
      Brush = pen.Brush;
      Thickness = pen.Thickness = 2;
      Type = EShapeType.LINE;
   }
   #endregion

   #region Methods --------------------------------------------------------------------------------
   public override void DrawShapes (DrawingContext dc) => dc.DrawLine (new Pen (Brush, Thickness), StartPoint, EndPoint);
   #endregion
}
#endregion

#region Rectangle ---------------------------------------------------------------------------------
/// <summary> Class representing a rectangle drawing </summary>
class Rectangle : Drawing {
   #region Constructor ----------------------------------------------------------------------------
   /// <summary>Initializes a new instance of the <see cref="Rectangle"/> class</summary>
   /// <param name="pen">The pen used to draw the rectangle.</param>
   public Rectangle (Pen pen) {
      Brush = pen.Brush;
      Thickness = pen.Thickness = 2;
      Type = EShapeType.RECTANGLE;
   }
   #endregion

   public override void DrawShapes (DrawingContext dc) {
      var pen = new Pen (Brush, Thickness);
      var rect = new Rect (StartPoint, EndPoint);
      dc.DrawRectangle (null, pen, rect);
   }
}
#endregion

#region Circle ------------------------------------------------------------------------------------
/// <summary> Class representing a circle drawing </summary>
class Circle : Drawing {
   #region Constructor ----------------------------------------------------------------------------
   /// <summary> Initializes a new instance of the <see cref="Circle"/> class </summary>
   /// <param name="pen">The pen used to draw the circle.</param>
   public Circle (Pen pen) {
      Brush = pen.Brush;
      Thickness = pen.Thickness = 2;
      Type = EShapeType.CIRCLE;
   }
   #endregion

   public override void DrawShapes (DrawingContext dc) {
      var pen = new Pen (Brush, Thickness);
      var center = new Point ((StartPoint.X + EndPoint.X) / 2, (StartPoint.Y + EndPoint.Y) / 2);
      var radiusX = Math.Abs (EndPoint.X - StartPoint.X) / 2;
      dc.DrawEllipse (null, pen, StartPoint, radiusX, radiusX);
   }
}
#endregion



