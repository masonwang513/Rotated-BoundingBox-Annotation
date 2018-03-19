using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anotation_Tool
{
    public class CoordinateConversion
    {
        public Size CurrentPicboxSize;
        public Size CurrentImageSize;
        public Size OriginalImageSize;

        public CoordinateConversion(Size originalImageSize)
        {
            this.OriginalImageSize = originalImageSize;
        }
        public void UpdateSizeChanges(Size picboxSize, Size imageSize)
        {
            this.CurrentImageSize = imageSize;
            this.CurrentPicboxSize = picboxSize;
        }

        public Point convert2ImageCoordinate(Point pt)
        {
            // from a coordinate system defined by picturebox converting to a standard 
            // coordinate system which is w.r.t to the image, taking upper-left of image
            //  as origin and takes original (width, height) of image as (x, y) axises.
            Point origin = getImageOrigin();
            double scale = getImageZoomRatio();
            int x = Convert.ToInt32((pt.X - origin.X) / scale);
            int y = Convert.ToInt32((pt.Y - origin.Y) / scale);
            return new Point(x, y);
        }


        public List<Point> convert2ImageCoordinate(List<Point> ptList)
        {
            List<Point> newPtList = new List<Point>();
            foreach (var pt in ptList)
            {
                newPtList.Add(convert2ImageCoordinate(pt));
            }
            return newPtList;
        }


        public Point convert2PicboxCoordinate(Point pt)
        {
            // from a coordinate system defined by image converting to another coordinate
            //  system which is w.r.t to the picbox, taking upper-left of picbox as origin
            //  and takes currently scaled (width, height) of image as (x, y) axises.
            Point origin = getImageOrigin();
            double scale = getImageZoomRatio();
            int x = Convert.ToInt32(pt.X * scale + origin.X);
            int y = Convert.ToInt32(pt.Y * scale + origin.Y);
            return new Point(x, y);
        }


        public List<Point> convert2PicboxCoordinate(List<Point> ptList)
        {
            List<Point> newPtList = new List<Point>();
            foreach (var pt in ptList)
            {
                newPtList.Add(convert2PicboxCoordinate(pt));
            }
            return newPtList;
        }


        private double getImageZoomRatio()
        {
            return CurrentImageSize.Width * 1.0 / OriginalImageSize.Width;
        }


        public Point getImageOrigin()
        {
            // take the upper left corner of picturebox as the origin 
            int x = (CurrentPicboxSize.Width - CurrentImageSize.Width) / 2;
            int y = (CurrentPicboxSize.Height - CurrentImageSize.Height) / 2;
            return new Point(x, y);
        }


    }
}
