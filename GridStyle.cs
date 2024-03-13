using System.Windows.Media;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Brushes = System.Windows.Media.Brushes;
namespace WPF;

class GridStyle {
   #region Properties -----------------------------------------------------------------------------
   /// <summary> Gets or sets the current grid style </summary>
   public EGridStyle CurrentGridStyle { get; set; } = EGridStyle.None;
   #endregion

   #region Methods --------------------------------------------------------------------------------
   /// <summary> Draws grid lines based on the current grid style </summary>
   /// <param name="dc">The drawing context.</param>
   /// <param name="actualWidth">The actual width of the drawing area</param>
   /// <param name="actualHeight">The actual height of the drawing area</param>
   public void DrawGridLines (DrawingContext dc, double actualWidth, double actualHeight) {
      Pen pen = new (Brushes.DarkGray, 0.5);
      switch (CurrentGridStyle) {
         case EGridStyle.Lines:
            // Draw horizontal lines
            for (double i = 0; i < actualHeight; i += mGridSize)
               dc.DrawLine (pen, new Point (0, i), new Point (actualWidth, i));
            // Draw vertical lines
            for (double i = 0; i < actualWidth; i += mGridSize)
               dc.DrawLine (pen, new Point (i, 0), new Point (i, actualHeight));
            break;
         case EGridStyle.Dots:
            // Draw dots at grid intersections
            for (double i = 0; i < actualHeight; i += mGridSize)
               for (double j = 0; j < actualWidth; j += mGridSize)
                  dc.DrawEllipse (pen.Brush, pen, new Point (j, i), 1, 1);
            break;
         default:
            break;
      }
   }
   #endregion

   #region Enmeration -----------------------------------------------------------------------------
   /// <summary> Enumeration representing different grid styles </summary>
   public enum EGridStyle {
      None,
      Dots,
      Lines,
   }
   #endregion

   #region PrivateData ----------------------------------------------------------------------------
   readonly int mGridSize = 50;
   #endregion
}