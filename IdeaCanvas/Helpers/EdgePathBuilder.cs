using System.Globalization;
using System.Text;
using Microsoft.Msagl.Core.Geometry.Curves;

public static class EdgePathBuilder
{
    public static string ToSvgPath(ICurve curve)
    {
        var sb = new StringBuilder();
        var start = curve.Start;
        sb.Append($"M {F(start.X)} {F(start.Y)} ");

        // Curve = mehrere Segmente hintereinander, sonst ein einzelnes Segment
        var segments = curve is Curve c ? c.Segments : new List<ICurve> { curve };

        foreach (var segment in segments)
        {
            switch (segment)
            {
                case LineSegment line:
                    sb.Append($"L {F(line.End.X)} {F(line.End.Y)} ");
                    break;

                case CubicBezierSegment bezier:
                    sb.Append($"C {F(bezier.B(1).X)} {F(bezier.B(1).Y)} " +
                               $"{F(bezier.B(2).X)} {F(bezier.B(2).Y)} " +
                               $"{F(bezier.B(3).X)} {F(bezier.B(3).Y)} ");
                    break;

                default:
                    // Fallback: einfach zum Endpunkt springen, falls ein unbekannter Segmenttyp auftaucht
                    sb.Append($"L {F(segment.End.X)} {F(segment.End.Y)} ");
                    break;
            }
        }

        return sb.ToString();

        static string F(double value) => value.ToString(CultureInfo.InvariantCulture);
    }
}