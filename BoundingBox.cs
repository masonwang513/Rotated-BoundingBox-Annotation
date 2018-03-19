using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anotation_Tool
{
    public class BoundingBox
    {
        public Point[] corners; // fout corners
        public double angle;  // measured by degree
        public Point center;
        public double width;
        public double length;

        public BoundingBox() { }

        public BoundingBox(Point[] corners)
        {
            // compute the fourth point of the rotated rect, pt4 = pt3 - pt2 + p1
            this.corners = new Point[4];
            for (int i = 0; i < 3; i++)
                this.corners[i] = corners[i];
            this.corners[3] = PA.Add(PA.Subtract(corners[2], corners[1]), corners[0]);

            // reorder points 
            this.corners = reOderPoints(this.corners);
            this.length = PA.Norm(this.corners[0], this.corners[1]);
            this.width = PA.Norm(this.corners[0], this.corners[3]);
            this.angle = Math.Atan2(this.corners[1].Y - this.corners[0].Y,
                                    this.corners[1].X - this.corners[0].X) / Math.PI * 180;
            this.angle = this.angle > 90 ? this.angle - 180 : this.angle; // (-90, 90]                          
            // compute the center of rotated rect
            int xc = 0, yc = 0;
            for (int i = 0; i < 4; i++)
            {
                xc += this.corners[i].X;
                yc += this.corners[i].Y;
            }
            this.center = new Point(xc / 4, yc / 4);
        }

        public BoundingBox(Point[] corners, double angle)
        {
            this.corners = new Point[4];
            for (int i = 0; i < 4; i++)
                this.corners[i] = corners[i];
            this.angle = angle;
            // the longer axis is length
            this.length = PA.Norm(corners[0], corners[1]);
            this.width = PA.Norm(corners[0], corners[3]);        
            // compute the center of rotated rect
            int xc = 0, yc = 0;
            for (int i = 0; i < 4; i++)
            {
                xc += this.corners[i].X;
                yc += this.corners[i].Y;
            }
            this.center = new Point(xc / 4, yc / 4);

        }

        public bool Contains(Point pt)
        {
            // roate the rectangle angle degree counter-clockwise 
            Point[] ptsRotated = new Point[4];
            Point transpt;
            int x, y;
            double sin = Math.Sin(angle / 180 * Math.PI);
            double cosin = Math.Cos(angle / 180 * Math.PI);
            for (int i = 0; i < 4; i++)
            {
                transpt = PA.Subtract(corners[i], center);
                x = Convert.ToInt32(transpt.X * cosin + transpt.Y * sin + center.X);
                y = Convert.ToInt32(transpt.X * -sin + transpt.Y * cosin + center.Y);
                ptsRotated[i] = new Point(x, y);
            }
            List<int> xs = ptsRotated.OrderBy(p=>p.X).Select(p=>p.X).ToList();
            List<int> ys = ptsRotated.OrderBy(p => p.Y).Select(p => p.Y).ToList();
            //Size size = new Size(Math.Abs(ptsRotated[0].X - ptsRotated[2].X),
            //                     Math.Abs(ptsRotated[0].Y - ptsRotated[2].Y));
            Rectangle rect = new Rectangle(xs[0], ys[0], xs[3] - xs[0], ys[3] - ys[0]);
            // apply rotation to point p
            transpt = new Point(pt.X - center.X, pt.Y - center.Y);
            x = Convert.ToInt32(transpt.X * cosin + transpt.Y * sin + center.X);
            y = Convert.ToInt32(transpt.X * -sin + transpt.Y * cosin + center.Y);
            bool isContained = rect.Contains(new Point(x, y));
            return isContained;
        }

        public void ShiftCenterTo(Point pt)
        {
            Point transpt = PA.Subtract(pt, this.center);
            for (int i = 0; i < 4; i++)
            {
                this.corners[i] = PA.Add(this.corners[i], transpt);
            }
            this.center = pt;
        }

        public void ShiftCornerTo(int ind, Point pt)
        {
            if (PA.Norm(pt, corners[ind]) < 1)
                return;
            // recompute the coordinates of two adjacent points of this shifted point    
            Point prjpt1 = PA.Project2Vector(corners[(ind + 2) % 4], corners[(ind + 1) % 4], pt);
            Point prjpt2 = PA.Project2Vector(corners[(ind + 2) % 4], corners[(ind + 3) % 4], pt);
            // rectify projected pt2/pt1 make sure two vectors formed by these three points are well orthogonal
            prjpt2 = PA.Project2NormalVector(corners[(ind + 2) % 4], prjpt1, prjpt2);

            this.corners[(ind + 1) % 4] = prjpt1;
            this.corners[(ind + 3) % 4] = prjpt2;
            // update the shifted point itself
            this.corners[ind] = pt;
            // reorder
             this.corners = reOderPoints(this.corners);
            // update width, length
            this.length = PA.Norm(this.corners[0], this.corners[1]);
            this.width = PA.Norm(this.corners[0], this.corners[3]);
            this.angle = Math.Atan2(this.corners[1].Y - this.corners[0].Y,
                                    this.corners[1].X - this.corners[0].X) / Math.PI * 180;
            this.angle = this.angle > 90 ? this.angle - 180 : this.angle; // (-90, 90]  
            // update the center of new rotated rect
            int xc = 0, yc = 0;
            for (int i = 0; i < 4; i++)
            {
                xc += this.corners[i].X;
                yc += this.corners[i].Y;
            }
            this.center = new Point(xc / 4, yc / 4);
        }

        public void ShiftDirection(int ind, int step=3)
        {
            // ind = 0 --> move left
            // ind = 1 --> move right
            // ind = 2 --> move up
            // ind = 3 --> move down
            Point transpt = new Point(0, 0);
            switch (ind)
            {
                case 0: transpt = new Point(-step, 0); break;
                case 1: transpt = new Point(step, 0); break;
                case 2: transpt = new Point(0, -step); break;
                case 3: transpt = new Point(0, step); break;            
            }
            for (int i = 0; i < 4; i++)
            {
                this.corners[i] = PA.Add(this.corners[i], transpt);
            }
            this.center = PA.Add(this.center, transpt);
        }

        private Point[] reOderPoints(Point[] pts)
        {
            Point[] ordered_pts = new Point[4];
            int ind = 0;
            var inds = pts.Select((pt, i) => new KeyValuePair<int, int>(i, pt.Y))
                          .OrderBy(x => x.Value).Select(x => x.Key).ToArray();
            if ((pts[inds[0]].Y == pts[inds[1]].Y) && (pts[inds[0]].X > pts[inds[1]].X))
                ind = inds[1];
            else
                ind = inds[0];
            double dis1 = PA.Norm(pts[ind], pts[(ind + 1) % 4]);
            double dis2 = PA.Norm(pts[ind], pts[(ind + 3) % 4]);
            ordered_pts[1] = dis1 >= dis2 ? pts[(ind + 1) % 4] : pts[(ind + 3) % 4];
            ordered_pts[3] = dis1 < dis2 ? pts[(ind + 1) % 4] : pts[(ind + 3) % 4];         
            ordered_pts[0] = pts[ind];
            ordered_pts[2] = pts[(ind + 2) % 4];
            return ordered_pts;
        }
    }
}
