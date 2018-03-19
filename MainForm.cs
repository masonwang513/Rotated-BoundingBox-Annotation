using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.IO;
using System.Threading;


namespace Anotation_Tool
{
    public partial class MainForm : Form
    {
        string treeNodeTxt = "Box";
        bool isMoving = false; // indicating the moving status
        bool isDrawing = false; // indicating the drawing status
        int counter = 0;   // the ith point the mouse is selecting for a new rect
        List<Point> pointList;  // buffer three selected points
        List<BoundingBox> bboxList; // store confirmed boundingboxes 
        int selectedBBoxInd = -1; // cursor-selecting bbox index
        int hoveredBBoxInd = -1; // curosr-hovering bbox index
        int hoveredPointInd = -1; // cursor-hovering the ith bbox' jth corner 
        int selectedPointInd = -1; // cursor-selecting the ith bbox' jth corner 
        int copyedBBoxInd = -1; // the bbox used for copy

        CoordinateConversion conversion;  // converse coordinates
        ReadWriter readWriter;  // readwrite txt file
        int imageInd = -1;  // the index of currently being labelled image 
        List<string> imageFileList;  // store all images to be labelled
        List<string> labelFileList;  // store all image annotation texts

        public MainForm()
        {
            InitializeComponent();
            pointList = new List<Point>();
            bboxList = new List<BoundingBox>();
            readWriter = new ReadWriter();
            initContextMenu();  
        }

        private void initContextMenu()
        {
            ContextMenu cm = new ContextMenu();
            MenuItem beginDraw = new MenuItem("BeginDrawing   Ctrl+B");
            MenuItem endDraw = new MenuItem("EndDrawing   Ctrl+E");
            MenuItem deleteBox = new MenuItem("DeleteBox   Ctrl+D");
            MenuItem copyBox = new MenuItem("CopyBox   Ctrl+C");
            MenuItem pasteBox = new MenuItem("PasteBox   Ctrl+V");
            MenuItem moveLeft = new MenuItem("MoveLeft   ←");
            MenuItem moveRight = new MenuItem("MoveRight   →");
            MenuItem moveUp = new MenuItem("MoveUp   ↑");
            MenuItem moveDown = new MenuItem("MoveDown   ↓");
            beginDraw.Click += btnDraw_Click;
            endDraw.Click += endDrawingMenuItem_Click;
            deleteBox.Click += deleteBoxMenuItem_Click;
            copyBox.Click += copyBoxMenuItem_Click;
            pasteBox.Click += pasteBoxMenuItem_Click;
            moveLeft.Click += moveLeftMenuItem_Click;
            moveRight.Click += moveRightMenuItem_Click;
            moveUp.Click += moveUpMenuItem_Click;
            moveDown.Click += moveDownMenuItem_Click;
            cm.MenuItems.AddRange(new MenuItem[] { beginDraw, endDraw, deleteBox, copyBox, pasteBox});
            cm.MenuItems.AddRange(new MenuItem[] { moveLeft, moveRight, moveUp, moveDown });
            picbox.ContextMenu = cm;
        }


        # region MainForm Events
        private void MainForm_Load(object sender, EventArgs e)
        {
            if (imageFileList != null)
                return;
            labelFileList = readWriter.GetExistingLabelFiles();
            imageFileList = readWriter.GetAllImageFiles();
            if (imageFileList == null || imageFileList.Count == 0)
            {
                MessageBox.Show("No image is found.", "Alter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Environment.Exit(0);
            }           
        }
        private void MainForm_Shown(object sender, EventArgs e)
        {
            imageInd = labelFileList.Count < imageFileList.Count ? labelFileList.Count : 0;
            txtbox.Text = String.Format("Current Progress: {0}/{1}", imageInd + 1, imageFileList.Count);
            Image image = readWriter.GetImage(imageFileList[imageInd]);
            string fileName = Path.GetFileNameWithoutExtension(imageFileList[imageInd]);
            bboxList = readWriter.LoadFromFile(fileName + ".txt");
            picbox.Image = image;
            updateTreeView();
            conversion = new CoordinateConversion(image.Size);
            conversion.UpdateSizeChanges(picbox.Size, getImageCurrentSize());
            picbox.Refresh();
        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            string labelFileName = Path.GetFileNameWithoutExtension(imageFileList[imageInd]) + ".txt";
            readWriter.Save2File(labelFileName, this.bboxList);
        }
        #endregion


        #region PictureBox Events
        private void picbox_MouseClick(object sender, MouseEventArgs e)
        {
            if ((this.hoveredBBoxInd != -1) && (isDrawing == false))
            {
                //isMoving = true;
                this.selectedPointInd = this.hoveredPointInd;
                this.selectedBBoxInd = this.hoveredBBoxInd;
                highLightTreeNode(selectedBBoxInd, selectedPointInd);
            }
            else if ((this.hoveredBBoxInd == -1) && (isDrawing == false))
            {
                this.selectedBBoxInd = -1;
                highLightTreeNode(selectedBBoxInd, selectedPointInd);
            }
            picbox.Refresh();
        }
        private void picbox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // whether under drawing status
                if (isDrawing)
                {
                    counter++;
                    pointList.Add(new Point(e.X, e.Y));
                    if (counter == 3)
                    {
                        pointList = conversion.convert2ImageCoordinate(pointList);
                        BoundingBox bbox = new BoundingBox(pointList.ToArray());
                        bboxList.Add(bbox);
                        addNodeTreeView();
                        highLightTreeNode(bboxList.Count - 1, -1);
                        this.selectedBBoxInd = bboxList.Count - 1;
                        picbox.Cursor = Cursors.Arrow;
                        isDrawing = false;
                        pointList.Clear();
                        counter = 0;
                    }
                }
                // whether under moving status
                else if (this.hoveredBBoxInd != -1)
                {
                    isMoving = true;
                    this.selectedPointInd = this.hoveredPointInd;
                    this.selectedBBoxInd = this.hoveredBBoxInd;
                }           
                picbox.Refresh();
            }
            else if (e.Button == MouseButtons.Right)
            {
                picbox.ContextMenu.MenuItems[0].Enabled = isDrawing? false:true;
                picbox.ContextMenu.MenuItems[1].Enabled = isDrawing? true:false;
                picbox.ContextMenu.MenuItems[2].Enabled = selectedBBoxInd != -1 ? true : false;
                picbox.ContextMenu.MenuItems[3].Enabled = selectedBBoxInd != -1 ? true : false;
                picbox.ContextMenu.MenuItems[4].Enabled = copyedBBoxInd != -1 ? true : false;
                picbox.ContextMenu.MenuItems[5].Enabled = selectedBBoxInd != -1 ? true : false;
                picbox.ContextMenu.MenuItems[6].Enabled = selectedBBoxInd != -1 ? true : false;
                picbox.ContextMenu.MenuItems[7].Enabled = selectedBBoxInd != -1 ? true : false;
                picbox.ContextMenu.MenuItems[8].Enabled = selectedBBoxInd != -1 ? true : false;
                picbox.ContextMenu.Show(picbox, new Point(e.X, e.Y));
            }
        }
        private void picbox_MouseUp(object sender, MouseEventArgs e)
        {
            if (isMoving)
            {
                // stop moving a bbox         
                if (this.hoveredBBoxInd != -1)
                {
                    picbox.Cursor = picbox.Cursor = Cursors.Arrow;
                    this.selectedPointInd = -1;
                    isMoving = false;
                }
                picbox.Refresh();
            }
        }       
        private void picbox_MouseMove(object sender, MouseEventArgs e)
        {
            // whether under drawing status
            if (isDrawing)
            {
                if (counter == 2)
                {
                    Point pt = new Point(e.X, e.Y);
                    Point projected = PA.Project2NormalVector(pointList[1], pointList[0], pt);
                    if (PA.Norm(pt, projected) > 1)
                    {
                        Point screePt = picbox.PointToScreen(projected);
                        Cursor.Position = screePt;
                    }
                }
                picbox.Refresh();
            }
            // whether under moving status
            else if (isMoving)
            {
                //if (selectedBBoxInd != -1)
                if(this.hoveredBBoxInd != -1)
                {
                    // update bbox coordinates
                    Point pt = conversion.convert2ImageCoordinate(new Point(e.X, e.Y));
                    if (selectedPointInd == -1)                 
                        bboxList[selectedBBoxInd].ShiftCenterTo(pt);             
                    else           
                        bboxList[selectedBBoxInd].ShiftCornerTo(selectedPointInd, pt);                  
                }
                picbox.Refresh();
            }
            // wehther under hovering status
            else
            {
                int i = 0;
                // determine whether mouse is hovering around a box or one of its corners
                Point pt = conversion.convert2ImageCoordinate(new Point(e.X, e.Y));
                for (; i < bboxList.Count; i++)
                {
                    int status = getContainingStatus(bboxList[i], pt);
                    if (status == -1)
                        continue;
                    if (status == 0) // inside a box
                    {
                        this.hoveredBBoxInd = i;
                        this.hoveredPointInd = -1;
                        picbox.Cursor = Cursors.SizeAll;
                    }
                    else  // around a corner of a box
                    {
                        this.hoveredBBoxInd = i;
                        this.hoveredPointInd = status - 1;
                        picbox.Cursor = Cursors.NoMove2D;
                    }
                    picbox.Refresh();
                    break;          
                }
                // recover from last hovering status
                if ((i == bboxList.Count) && (this.hoveredBBoxInd != -1))
                {
                    this.hoveredBBoxInd = -1;
                    this.hoveredPointInd = -1;
                    picbox.Cursor = Cursors.Arrow;
                    picbox.Refresh();
                }
            }
        }      
        private void picbox_Paint(object sender, PaintEventArgs e)
        {
           
           // painting the under-drawing rect's lines and corners
            Point curpt = picbox.PointToClient(Cursor.Position);
            if (isDrawing && counter > 0)
            {
                e.Graphics.DrawLine(new Pen(Color.Brown), pointList.Last(), curpt);
                if (counter == 2)
                {
                    Point pt4 = PA.Add(PA.Subtract(curpt, pointList[1]), pointList[0]);
                    e.Graphics.DrawLine(new Pen(Color.Brown), pointList[0], pointList[1]);
                    e.Graphics.DrawLine(new Pen(Color.Brown), pointList[0], pt4);
                    e.Graphics.DrawLine(new Pen(Color.Brown), pt4, curpt);
                }
                for (int i = 0; i < pointList.Count; i++)
                {
                    e.Graphics.FillEllipse(new SolidBrush(Color.Yellow), new Rectangle(pointList[i].X - 5, pointList[i].Y - 5, 10, 10));
                }
            }
           
           // painting those rects having finish drawing and been stored
            for (int i = 0; i < bboxList.Count;i++ )
            {
                List<Point> ptList = conversion.convert2PicboxCoordinate(bboxList[i].corners.ToList());
                for (int j = 0; j < 4; j++)
                {
                    e.Graphics.FillEllipse(new SolidBrush(Color.Yellow), new Rectangle(ptList[j].X - 5, ptList[j].Y - 5, 10, 10));
                    if (this.selectedBBoxInd != i)
                        e.Graphics.DrawLine(new Pen(Color.Blue), ptList[j], ptList[(j + 1) % 4]);
                    else if (this.selectedBBoxInd == i) //highlight currently selected bbox       
                        e.Graphics.DrawLine(new Pen(Color.Red, 2.5F), ptList[j], ptList[(j + 1) % 4]);             
                }
                if (this.hoveredBBoxInd == i)
                {
                    e.Graphics.FillPolygon(new SolidBrush(Color.FromArgb(75, 255, 255, 255)), ptList.ToArray());
                }
            }
        }
        private void picbox_SizeChanged(object sender, EventArgs e)
        {             
            conversion.UpdateSizeChanges(picbox.Size, getImageCurrentSize());
            Point originOfImage = conversion.getImageOrigin();
            btnLast.Location = new Point(originOfImage.X, btnLast.Location.Y);
            Point upperRight = PA.Add(originOfImage, new Point(getImageCurrentSize().Width, 0));
            btnNext.Location = new Point(upperRight.X - btnNext.Size.Width, btnNext.Location.Y);
            btnDraw.Location = PA.Multiply(PA.Add(btnLast.Location, btnNext.Location), 0.5F);
            txtbox.Location = new Point(btnDraw.Location.X, txtbox.Location.Y);
        }
        private Size getImageCurrentSize()
        {
            PropertyInfo rectangleProperty = picbox.GetType().GetProperty("ImageRectangle", BindingFlags.Instance | BindingFlags.NonPublic);
            Rectangle rectangle = (Rectangle)rectangleProperty.GetValue(picbox, null);
            int currentWidth = rectangle.Width;
            int currentHeight = rectangle.Height;
            return new Size(currentWidth, currentHeight);
        }
        private int getContainingStatus(BoundingBox box, Point pt)
        {
            // -1  outside the box
            // 0   inside the box
            // 1-4 around ith corner of the box
            int status = -1;
            // whether hovering a corner of a bbox
            for (int j = 0; j < 4; j++)
            {
                Rectangle rect = new Rectangle(PA.Subtract(box.corners[j], new Point(5, 5)), new Size(10, 10));
                if (rect.Contains(pt))
                {
                    status = j + 1;
                    break;
                }
            }
            // whether hovering within a bbox
            if (status == -1 && box.Contains(pt))
                status = 0;
            return status;
        }
        #endregion 

        #region ContextMenu and Shortcut Events
        private void beginDrawingMenuItem_Click(object sender, EventArgs e)
        {
            startDrawing();
        }     
        private void endDrawingMenuItem_Click(object sender, EventArgs e)
        {
            endDrawing();
        }
        private void deleteBoxMenuItem_Click(object sender, EventArgs e)
        {
            deleteBoundingBox();
        }
        private void copyBoxMenuItem_Click(object sender, EventArgs e)
        {
            copyBoundingBox();
        }
        private void pasteBoxMenuItem_Click(object sender, EventArgs e)
        {
            pasteBoundingBox();
        }
        private void moveLeftMenuItem_Click(object sender, EventArgs e)
        {
            this.bboxList[selectedBBoxInd].ShiftDirection(0);
        }
        private void moveRightMenuItem_Click(object sender, EventArgs e)
        {
            this.bboxList[selectedBBoxInd].ShiftDirection(1);
        }
        private void moveUpMenuItem_Click(object sender, EventArgs e)
        {
            this.bboxList[selectedBBoxInd].ShiftDirection(2);
        }
        private void moveDownMenuItem_Click(object sender, EventArgs e)
        {
            this.bboxList[selectedBBoxInd].ShiftDirection(3);
        }

        private void copyBoundingBox()
        {
            copyedBBoxInd = this.selectedBBoxInd;
        }
        private void pasteBoundingBox()
        {
            if (copyedBBoxInd != -1)
            {
                BoundingBox nbox = new BoundingBox(bboxList[copyedBBoxInd].corners, bboxList[copyedBBoxInd].angle);
                Point pt = conversion.convert2ImageCoordinate(picbox.PointToClient(Cursor.Position));
                nbox.ShiftCenterTo(PA.Add(pt, new Point(10, 10)));  //nbox.center
                this.bboxList.Add(nbox);
                this.selectedBBoxInd = bboxList.Count - 1;
                picbox.Refresh();
                updateTreeView();
            }
        }
        private void deleteBoundingBox()
        {
            if (selectedBBoxInd != -1)
            {
                bboxList.RemoveAt(this.selectedBBoxInd);
                this.selectedBBoxInd = -1;
                this.hoveredBBoxInd = hoveredBBoxInd == selectedBBoxInd ? selectedBBoxInd : hoveredBBoxInd;
                picbox.Refresh();
                updateTreeView();
            }
        }
        private void startDrawing()
        {
            counter = 0;
            isDrawing = true;
            pointList = new List<Point>();
            picbox.Cursor = System.Windows.Forms.Cursors.Cross;
        }
        private void endDrawing()
        {
            counter = 0;
            pointList.Clear();
            isDrawing = false;
            picbox.Cursor = Cursors.Arrow;
            picbox.Refresh();
        }
        #endregion


        #region treeview updates
        private void updateTreeView()
        {
            treeView.Nodes.Clear();
            treeView.Nodes.Add(treeNodeTxt + "es");
            for(int i=0;i<bboxList.Count;i++)
            {
                TreeNode fish = new TreeNode(treeNodeTxt + (i + 1).ToString("D2"));
                for (int j = 0; j < 4; j++)
                {
                    TreeNode corner = new TreeNode(String.Format("Corner"+(j+1).ToString()+": "+"[{0}, {1}]", 
                                                         bboxList[i].corners[j].X, bboxList[i].corners[j].Y));
                    fish.Nodes.Add(corner);
                }
                fish.Nodes.Add("RotatedAngle: "+ Math.Round(bboxList[i].angle, 2).ToString());
                //fish.Nodes.Add("HeadRight: ");
                treeView.Nodes[0].Nodes.Add(fish);
                treeView.Nodes[0].Expand();
            }
        }
        private void addNodeTreeView()
        {
            TreeNode fish = new TreeNode(treeNodeTxt + (bboxList.Count + 1).ToString("D2"));
            for (int j = 0; j < 4; j++)
            {
                TreeNode corner = new TreeNode(String.Format("Corner" + (j + 1).ToString() + ": " + "[{0}, {1}]",
                                                     bboxList.Last().corners[j].X, bboxList.Last().corners[j].Y));
                fish.Nodes.Add(corner);
            }
            fish.Nodes.Add("RotatedAngle: " + Math.Round(bboxList.Last().angle, 2).ToString());
            //fish.Nodes.Add("HeadRight: ");
            treeView.Nodes[0].Nodes.Add(fish);
            treeView.Nodes[0].Expand();
        }
        private void highLightTreeNode(int i, int j)
        {
            foreach (TreeNode node in treeView.Nodes[0].Nodes)
            {
                node.ForeColor = Color.Black;
                foreach (TreeNode child in node.Nodes)
                {
                    child.ForeColor = Color.Black;
                }
                node.Collapse();
            }
            if (i != -1)
            { 
                treeView.Nodes[0].Nodes[i].ForeColor = Color.Blue;
                if (j != -1)
                    treeView.Nodes[0].Nodes[i].Nodes[j].ForeColor = Color.Red; 
                treeView.Nodes[0].Nodes[i].Expand();
            }                      
        }
        #endregion

        #region Button Click Events
        private void btnDraw_Click(object sender, EventArgs e)
        {
            startDrawing();
        }
        private void btnNext_Click(object sender, EventArgs e)
        {
            string labelFileName = Path.GetFileNameWithoutExtension(imageFileList[imageInd]) + ".txt";
            readWriter.Save2File(labelFileName, this.bboxList);
            if (imageFileList.Count <= imageInd + 1)
            {
                MessageBox.Show("This is the last image.", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            initPerImageVaraibles();

            imageInd += 1;
            txtbox.Text = String.Format("Current Progress: {0}/{1}", imageInd + 1, imageFileList.Count);
            Image image = readWriter.GetImage(imageFileList[imageInd]);
            string fileName = Path.GetFileNameWithoutExtension(imageFileList[imageInd]);
            bboxList = readWriter.LoadFromFile(fileName + ".txt");
            picbox.Image = image;
            updateTreeView();
            conversion = new CoordinateConversion(image.Size);
            conversion.UpdateSizeChanges(picbox.Size, getImageCurrentSize());
            picbox.Refresh();
        }
        private void btnLast_Click(object sender, EventArgs e)
        {
            string labelFileName = Path.GetFileNameWithoutExtension(imageFileList[imageInd]) + ".txt";
            readWriter.Save2File(labelFileName, this.bboxList);
            if (imageInd <= 0)
            {
                MessageBox.Show("This is already the first image.", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            initPerImageVaraibles();

            imageInd -= 1;
            txtbox.Text = String.Format("Current Progress: {0}/{1}", imageInd + 1, imageFileList.Count);
            Image image = readWriter.GetImage(imageFileList[imageInd]);
            string fileName = Path.GetFileNameWithoutExtension(imageFileList[imageInd]);
            bboxList = readWriter.LoadFromFile(fileName + ".txt");
            picbox.Image = image;
            updateTreeView();
            conversion = new CoordinateConversion(image.Size);
            conversion.UpdateSizeChanges(picbox.Size, getImageCurrentSize());
            picbox.Refresh();
        }
        private void initPerImageVaraibles()
        {
            this.counter = 0;
            this.isDrawing = false;
            this.isMoving = false;
            this.pointList.Clear();
            this.bboxList.Clear();
            this.selectedBBoxInd = -1;
            this.selectedPointInd = -1;
            this.hoveredBBoxInd = -1;
            this.hoveredPointInd = -1;
        }
        #endregion


        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.B | Keys.Control: startDrawing(); return true;
                case Keys.E | Keys.Control: endDrawing(); return true;
                case Keys.D | Keys.Control: deleteBoundingBox(); return true;
                case Keys.C | Keys.Control: copyBoundingBox(); return true;
                case Keys.V | Keys.Control: pasteBoundingBox(); return true;
            }

            if (this.selectedBBoxInd != -1)
            {
                switch (keyData)
                {
                    case Keys.Left: bboxList[selectedBBoxInd].ShiftDirection(0); break;
                    case Keys.Right: bboxList[selectedBBoxInd].ShiftDirection(1); break;
                    case Keys.Up: bboxList[selectedBBoxInd].ShiftDirection(2); break;
                    case Keys.Down: bboxList[selectedBBoxInd].ShiftDirection(3); break;
                }
                picbox.Refresh();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
       

    }


   

   


    


  

}
