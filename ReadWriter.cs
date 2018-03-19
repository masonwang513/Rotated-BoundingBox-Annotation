using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Anotation_Tool
{
    public class ReadWriter
    {
        // Get the current directory.
        private string root = Directory.GetCurrentDirectory();
        private string labelFolderName = "Labels";
        private string imageFolderName = "Images";

        public ReadWriter()
        {
            string labelPath = Path.Combine(root, labelFolderName);
            if (!Directory.Exists(labelPath))
                Directory.CreateDirectory(labelPath);
            //string filePath = Path.Combine(root, labelFolderName, "099.txt");
            //MessageBox.Show(filePath);
        }


        public List<string> GetAllImageFiles()
        {
            string imagePath = Path.Combine(root, imageFolderName);
            if (!Directory.Exists(imagePath))
            {
                string alertInfo = String.Format("Can't find target image folder named 'Image' in {0}.", root) +
                               Environment.NewLine + "Please new such a folder and put images inside it.";
                MessageBox.Show(alertInfo, "Alert", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            List<string> imageList = new List<string>();
            var files = Directory.GetFiles(imagePath, "*.*", SearchOption.TopDirectoryOnly);
            foreach (string filePath in files)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(filePath, @".jpg|.png|.bmp$"))
                    imageList.Add(Path.GetFileName(filePath));
            }
            return imageList;
        }


        public List<string> GetExistingLabelFiles()
        {
            string labelPath = Path.Combine(root, labelFolderName);
            List<string> fileList = Directory.GetFiles(labelPath, "*.txt", SearchOption.TopDirectoryOnly)
                                             .Select(filePath => Path.GetFileName(filePath)).ToList();
            return fileList;
        }


        public Image GetImage(string imageFileName)
        {
            string imagePath = Path.Combine(root, imageFolderName, imageFileName);
            Image img = Image.FromFile(imagePath);
            return img;
        }


        public void Save2File(string fileName, List<BoundingBox> bboxList)
        {
            if (bboxList == null || bboxList.Count == 0)
                return;
            string filePath = Path.Combine(root, labelFolderName, fileName);
            //if (!File.Exists(filePath))
            using (File.CreateText(filePath)) { };

            foreach (var box in bboxList)
            {
                string paramStr = String.Empty;
                for (int i = 0; i < 4; i++)
                    paramStr += string.Format("{0} {1} ", box.corners[i].X.ToString(), box.corners[i].Y.ToString());
                paramStr += (Math.Truncate(box.angle * 100) / 100).ToString();
                File.AppendAllText(filePath, paramStr + Environment.NewLine);
            }
        }


        public List<BoundingBox> LoadFromFile(string fileName)
        {
            string filePath = Path.Combine(root, labelFolderName, fileName);
            List<BoundingBox> bboxList = new List<BoundingBox>();
            if (!File.Exists(filePath))
                return bboxList;

            // Read in lines from file.
            foreach (string line in File.ReadLines(filePath))
            {
                string[] info = line.Split(new char[] { ' ' });
                List<double> valueList = new List<double>();
                foreach (string param in info)
                {
                    double value;
                    if (double.TryParse(param, out value))
                        valueList.Add(value);
                }
                if (valueList.Count == 9) // 4 points + an angle 
                {
                    Point[] corners = new Point[4];
                    for (int i = 0; i < 4; i++)
                    {
                        corners[i] = new Point((int)valueList[i*2], (int)valueList[i * 2 + 1]);
                    }
                    double angle = valueList.Last();
                    bboxList.Add(new BoundingBox(corners, angle));
                }
            }
            return bboxList;
        }
    }
}
