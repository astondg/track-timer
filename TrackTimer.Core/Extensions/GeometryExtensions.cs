namespace TrackTimer.Core.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Spatial;

    public static class GeometryExtensions
    {
        public static GeometryPoint[] ConvertToLine(this GeometryPoint point, double heading, double lineLength)
        {
            double distance = lineLength / 2;
            List<GeometryPoint> linePoints = new List<GeometryPoint>();
            if (heading == 0 || heading == 180 || heading == 360)
            {
                linePoints.Add(GeometryPoint.Create(point.X + distance, point.Y));
                linePoints.Add(GeometryPoint.Create(point.X - distance, point.Y));
            }
            else if (heading == 90 || heading == 270)
            {
                linePoints.Add(GeometryPoint.Create(point.X, point.Y + distance));
                linePoints.Add(GeometryPoint.Create(point.X, point.Y - distance));
            }
            else
            {
                double m = Math.Tan(heading);
                double c = point.Y - (m * point.X);

                double positiveX = point.X + (distance / (Math.Sqrt(1 + (m * m))));
                double positiveY = (m * positiveX) + c;
                linePoints.Add(GeometryPoint.Create(positiveX, positiveY, null, m));

                double negativeX = point.X + ((-distance) / (Math.Sqrt(1 + (m * m))));
                double negativeY = (m * positiveX) + c;
                linePoints.Add(GeometryPoint.Create(negativeX, negativeY, null, m));

                //double xDifference = point.X - x;
                //double yDifference = point.Y - (m * x + c);
                //double d = Math.Sqrt(((point.X - x) * (point.X - x)) + ((point.Y - (m * x + c)) * (point.Y - (m * x + c))));
            }

            return linePoints.ToArray();
        }
    }
}