using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anotation_Tool
{
    public class PA
    {
        public static double Norm(Point pt)
        {
            double length = Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y);
            return length;
        }
        public static double Norm(Point pt1, Point pt2)
        {
            double length = PA.Norm(PA.Subtract(pt1, pt2));
            return length;
        }

        public static Point Subtract(Point pt1, Point pt2)
        {
            return new Point(pt1.X - pt2.X, pt1.Y - pt2.Y);
        }
        public static PointF Subtract(PointF pt1, PointF pt2)
        {
            return new PointF(pt1.X - pt2.X, pt1.Y - pt2.Y);
        }

        public static Point Add(Point pt1, Point pt2)
        {
            return new Point(pt2.X + pt1.X, pt2.Y + pt1.Y);
        }
        public static PointF Add(PointF pt1, PointF pt2)
        {
            return new PointF(pt2.X + pt1.X, pt2.Y + pt1.Y);
        }

        public static PointF Multiply(PointF pt, float scale)
        {
            return new PointF(pt.X * scale, pt.Y * scale);
        }
        public static Point Multiply(Point pt, float scale)
        {
            return new Point(Convert.ToInt32(pt.X * scale), Convert.ToInt32(pt.Y * scale));
        }

        public static double Dot(Point pt1, Point pt2)
        {
            return pt1.X * pt2.X + pt1.Y * pt2.Y;
        }
        public static double Dot(PointF pt1, PointF pt2)
        {
            return pt1.X * pt2.X + pt1.Y * pt2.Y;
        }

        public static Point Float2Int(PointF pt)
        {
            return new Point(Convert.ToInt32(pt.X), Convert.ToInt32(pt.Y));
        }
        public static PointF Int2Float(Point pt)
        {
            return new PointF(pt.X, pt.Y);
        }


        public static Point Project2Vector(Point pt1, Point pt2, Point pt3)
        {
            // project a vector v1 formed by pt1-pt3 into another vector v2 formed by pt1-pt2
            Point v1 = PA.Subtract(pt3, pt1);
            Point v2 = PA.Subtract(pt2, pt1);
            float scale = (float)(PA.Dot(v1, v2) / PA.Norm(v2) / PA.Norm(v2));
            PointF projected_pt3 = PA.Add(PA.Multiply(PA.Int2Float(v2), scale), pt1);
            return PA.Float2Int(projected_pt3);
        }

        public static Point Project2NormalVector(Point pt1, Point pt2, Point pt3)
        {
            /* project a vector v1 formed by pt1-pt3 into the direction of 
               the normal vector of another vector v2 formed by pt1-pt2 */

            // compute the unit normal vector of vector v2
            Point v2 = PA.Subtract(pt2, pt1);
            float magnitude = (float)(PA.Norm(pt1, pt2));
            PointF unit_normal = new PointF(v2.Y / magnitude, -v2.X / magnitude);
            // project v1 into the normal vector
            Point v1 = PA.Subtract(pt3, pt1);
            PointF projected = PA.Multiply(unit_normal, (float)PA.Dot(v1, unit_normal));
            PointF projected_v3 = PA.Add(projected, pt1);
            return PA.Float2Int(projected_v3);
        }

    }
}
