using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ImageProcess;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.VisualBasic;

namespace ImagePro
{
    public partial class MainForm : Form
    {
        string ProgNAME = "Image Pro";
        string FileName = "";
        string FullFilePath = "";
        Bitmap ImageLoading = new Bitmap(1, 1);
        Bitmap ImageUndo = new Bitmap(1, 1);
        int ScrollDelta = 120;
        double DefaultZoom
        {
            get
            {
                return (以100比例打开图片ToolStripMenuItem.Checked) ? 1.0 : 0.0;
            }
        }
        double _zoom = 1.0;
        double MinZoom
        {
            get
            {
                double x = Math.Min(1.0 * panel1.Height / ImageLoading.Height
                    , 1.0 * panel1.Width / ImageLoading.Width);
                if (x > 1.0) x = 1.0;
                if (x < 0.01) x = 0.01;
                return x;
            }
        }
        double MaxZoom
        {
            get
            {
                double x = Math.Min(1.0 * 5000 / ImageLoading.Height
                    , 1.0 * 5000 / ImageLoading.Width);
                if (x < 1.0) x = 1.0;
                if (x > 5.0) x = 5.0;
                return x;
            }
        }
        double Zoom
        {
            set
            {
                Size size1 = pictureBoxShow.Size;
                double minz = MinZoom, maxz = MaxZoom;
                if (value <= minz)
                {
                    if (_zoom == minz) return;
                    _zoom = minz;
                }
                else if (value >= maxz)
                {
                    if (_zoom == maxz) return;
                    _zoom = maxz;
                }
                else
                {
                    _zoom = value;
                }
                PaintPictureBox();
                Point pt = pictureBoxShow.Location;
                Size size2 = pictureBoxShow.Size;
                Point mpt = this.pictureBoxShow.PointToClient(MousePosition);
                double ratioX = 1.0 * mpt.X / pictureBoxShow.Width;
                double ratioY = 1.0 * mpt.Y / pictureBoxShow.Height;
                pt.X -= (int)((size2.Width - size1.Width) * ratioX);
                pt.Y -= (int)((size2.Height - size1.Height) * ratioY);
                FitPictureLocation(pt);
            }
            get
            {
                return (_zoom < MinZoom) ? MinZoom : _zoom;
            }
        }
        void ZoomOut()
        {
            double zm=Zoom;
            if (zm < 1.0)
            {
                Zoom = zm - 0.2;
            }
            else if (zm < 2.0)
            {
                Zoom = zm - 0.5;
            }
            else
                Zoom = zm - 1.0;
        }
        void ZoomIn()
        {
            double zm = Zoom;
            if (zm < 1.0)
            {
                Zoom = zm + 0.2;
            }
            else if (zm < 2.0)
            {
                Zoom = zm + 0.5;
            }
            else
                Zoom = zm + 1.0;
        }
        Point MouseDownPoint;
        Point MouseMiddleDownPoint;
        Point DefaultOpenPosition
        {
            get
            {
                if (右上角ToolStripMenuItem.Checked)
                {
                    return new Point(panel1.Width - pictureBoxShow.Image.Width, 0);
                }
                else if (中间ToolStripMenuItem.Checked)
                {
                    return new Point((panel1.Width - pictureBoxShow.Image.Width) / 2
                        , (panel1.Height - pictureBoxShow.Image.Height) / 2);
                }
                else
                    return new Point(0, 0); //左上角及其他情况
            }
        }
        bool CtrlPress = false;
        bool MouseEnterPictureBox = false;
        bool MouseEnterPanel = false;

        //显示效果
        void PaintPictureBox()
        {
            pictureBoxShow.Image = new Bitmap(ImageLoading
                , new Size((int)(ImageLoading.Width * Zoom), (int)(ImageLoading.Height * Zoom)));
            pictureBoxShow.Size = pictureBoxShow.Image.Size;
        }

        void ScrollPictrueBox(int deltaX, int deltaY)
        {
            FitPictureLocation(new Point(pictureBoxShow.Location.X + deltaX
                    , pictureBoxShow.Location.Y + deltaY));
            FitScrollBar();
        }
        void FitScrollBar()
        {
            if (pictureBoxShow.Location.Y <= 0)
            {
                ScrollBarVer.Enabled = true;
                ScrollBarVer.Maximum = pictureBoxShow.Height;
                ScrollBarVer.LargeChange = panel1.Height;
                ScrollBarVer.Value = -pictureBoxShow.Location.Y;
            }
            else
            {
                ScrollBarVer.Enabled = false;
            }

            if (pictureBoxShow.Location.X > 0)
            {
                ScrollBarHon.Visible = false;
            }
            else
            {
                ScrollBarHon.Visible = true;
                ScrollBarHon.Maximum = pictureBoxShow.Width;
                ScrollBarHon.LargeChange = panel1.Width;
                ScrollBarHon.Value = -pictureBoxShow.Location.X;
            }
        }

        void FitPictureLocation()
        {
            FitPictureLocation(pictureBoxShow.Location);
        }
        void FitPictureLocation(Point pt)
        {
            if (pictureBoxShow.Width < panel1.Width)
            {
                pt.X = (panel1.Width - pictureBoxShow.Width) / 2;
            }
            else
            {
                pt.X = (pt.X + pictureBoxShow.Width < panel1.Width) ? panel1.Width - pictureBoxShow.Width : pt.X;
                pt.X = (pt.X > 0) ? 0 : pt.X;
            }
            if (pictureBoxShow.Height < panel1.Height)
            {
                pt.Y = (panel1.Height - pictureBoxShow.Height) / 2;
            }
            else
            {
                pt.Y = (pt.Y + pictureBoxShow.Height < panel1.Height) ? panel1.Height - pictureBoxShow.Height : pt.Y;
                pt.Y = (pt.Y > 0) ? 0 : pt.Y;
            }
            pictureBoxShow.Location = pt;
            FitScrollBar();
        }

        //撤销
        void RemUndo()
        {
            ImageUndo = ImageLoading.Clone() as Bitmap;
        }
        void Undo()
        {
            Bitmap t = ImageUndo.Clone() as Bitmap;
            ImageUndo = ImageLoading;
            ImageLoading = t;
            PaintPictureBox();
            FitPictureLocation();
        }


        //打开文件

        /// <summary>
        /// 文件跳转
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// ╮(╯_╰)╭ 能用就行 挑战极限 人不丧心病狂枉少年
        bool GotoPic(string _directory,int index)
        {
            if (File.Exists(_directory))
            {
                _directory = Path.GetDirectoryName(_directory);
            }
            string[] strs = Directory.GetFiles(_directory, "*.*", SearchOption.TopDirectoryOnly);
            if (strs.Length <= 1) return false;
            Array.Sort(strs);
            int length = strs.Length
                , bias = (index == 0) ? (new Random()).Next(length - 1) + 1 : 0;
            if (index == -1 || index == 2)
            {
                Array.Reverse(strs);
            }
            if (index < 0)
            {
                index = -index;
            }
            bool tag = false;
            for (int i = 0; i < length; i ++)
            {
                if (index == 0)
                {
                    string tmp = strs[(i + bias) % length];
                    if (tmp != FullFilePath && SupportedFile(tmp))
                    {
                        return _openFile(strs[(i + bias) % length]);
                    }
                }
                if (SupportedFile(strs[i]))
                {
                    if (index == 2)
                    {
                        return _openFile(strs[i]);
                    }
                    if (index == 1 && tag)
                    {
                        return _openFile(strs[i]);
                    }
                }
                if (FullFilePath == strs[i])
                {
                    tag = true;
                }
            }
            return false;
        }


        //open
        bool Open(string _name)
        {
            return Open(new string[] { _name });
        }

        bool Open(string[] strs)
        {
            this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
            if (strs.Length > 0)
            {
                for (int i = 0; i < strs.Length;i++ )
                {
                    if (File.Exists(strs[i]))
                    {
                        return _openFile(strs[i]);
                    }
                    else if (Directory.Exists(strs[i]))
                    {
                        GotoPic(strs[i], -2);
                    }
                }
            }
            this.Cursor = System.Windows.Forms.Cursors.Default;
            return false;
        }

        bool SupportedFile(string _filename)
        {
            string ext = _filename.Substring(_filename.Length - 4).ToLower();
            return (ext == ".jpg" || ext == ".bmp" || ext == ".raw");
        }


        bool _openFile(string _filename)
        {
            if (!File.Exists(_filename)) return false;
            this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
            RemUndo();
            return (_openColorFile(_filename) || _openGrayRawFile(_filename)) && _afterOpen(_filename);
        }

        bool _openColorFile(string _filename)
        {
            try
            {
                ImageLoading = new Bitmap(_filename);
                oriImage = ImageLoading;
                int sh=Screen.AllScreens[0].Bounds.Height,sw=Screen.AllScreens[0].Bounds.Width;
                if (ImageLoading.Width < sw && ImageLoading.Height > sh)
                {
                    以100比例打开图片ToolStripMenuItem.Checked = true; //临时性措施 ╮(╯_╰)╭
                    if (this.Width + 30 < ImageLoading.Width)
                    {
                        this.Width = ImageLoading.Width + 30;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        bool _openGrayRawFile(string _filename)
        {
            try
            {
                string str = Interaction.InputBox("输入图像大小，【Width Height】：", "输入", "512 512");
                string[] strm = str.Split(new string[] { " " }, 2, StringSplitOptions.RemoveEmptyEntries);
                ImageLoading = BitmapEx.GrayRawToBitmap(_filename
                    , Convert.ToInt16(strm[0]), Convert.ToInt16(strm[1]));
                return true;
            }
            catch
            {
                return false;
            }
        }

        bool _afterOpen(string _filename)
        {
            Zoom = DefaultZoom;
            PaintPictureBox();
            FitPictureLocation(DefaultOpenPosition);
            FullFilePath = _filename;
            FileName = Path.GetFileName(_filename);
            this.Text = ProgNAME + " - " + FileName;
            this.Cursor = System.Windows.Forms.Cursors.Default;
            return true;
        }

        #region from FINAL project

        Bitmap oriImage;
        Bitmap dispImage
        {
            get
            {
                return ImageLoading;
            }
            set
            {
                ImageLoading = value;
                pictureBoxShow.Image = value;
            }
        }

        int WinSize
        {
            get { return Convert.ToInt32(textBoxWinSize.Text); }
        }
        int MaxWinSize
        {
            get { return Convert.ToInt32(textBoxMaxWinSize.Text); }
        }
        double Gamma
        {
            get { return Convert.ToDouble(textBoxGamma.Text); }
        }
        double Saturation
        {
            get { return Convert.ToDouble(textBoxSaturation.Text); }
        }
        double Contrast
        {
            get { return Convert.ToDouble(textBoxContrast.Text); }
        }
        int conTH
        {
            get { return Convert.ToInt32(textBoxConTH.Text); }
        }
        int D0
        {
            get { return Convert.ToInt32(textBoxFilterD0.Text); }
        }


        private void EditButton_Click(object sender, EventArgs e)
        {
            //labelInfo.Text = "";
            //labelInfo.Text = "waiting...";
            DateTime dt = DateTime.Now;
            switch (((Button)sender).Text)
            {
                //spatial filter
                case "Mean":
                    dispImage = dispImage.Mean3Filter();
                    break;
                case "Gaussian":
                    dispImage = dispImage.Correlation(new int[5, 5]
                    {{1,4,6,4,1}
                    ,{4,16,24,16,4}
                    ,{6,24,36,24,6}
                    ,{4,16,24,16,4}
                    ,{1,4,6,4,1}}, 256);
                    break;
                case "Unsharp":
                    dispImage = dispImage.UnsharpFilter();
                    break;
                //order
                case "Min":
                    dispImage = dispImage.MinFilter(WinSize);
                    break;
                case "Max":
                    dispImage = dispImage.MaxFilter(WinSize);
                    break;
                case "Median":
                    dispImage = dispImage.MedianFilter(WinSize);
                    break;
                //adaptive
                case "AdaptiveMedian":
                    dispImage = dispImage.AdaptiveMedianFilter(MaxWinSize);
                    break;
                //LPF
                case "ILPF":
                    dispImage = dispImage.IdealLPF(D0);
                    break;
                case "BLPF":
                    dispImage = dispImage.ButterworthLPF(D0
                        , Convert.ToInt32(textBoxButterworthN.Text));
                    break;
                case "GLPF":
                    dispImage = dispImage.GaussianLPF(D0);
                    break;
                //HPF
                case "IHPF":
                    dispImage = dispImage.IdealHPF(D0);
                    break;
                case "BHPF":
                    dispImage = dispImage.ButterworthHPF(D0
                        , Convert.ToInt32(textBoxButterworthN.Text));
                    break;
                case "GHPF":
                    dispImage = dispImage.GaussianHPF(D0);
                    break;
                //hist
                case "Hist.I":
                    dispImage = dispImage.HistogramEqualizationI();
                    break;
                case "Hist.R":
                    dispImage = dispImage.HistogramEqualizationRGB(true, false, false);
                    break;
                case "Hist.G":
                    dispImage = dispImage.HistogramEqualizationRGB(false, true, false);
                    break;
                case "Hist.B":
                    dispImage = dispImage.HistogramEqualizationRGB(false, false, true);
                    break;
                //adjust
                case "Saturation":
                    dispImage = dispImage.Saturation(Saturation);
                    break;
                case "Contrast.R":
                    dispImage = dispImage.Contrast(Contrast, conTH, true, false, false);
                    break;
                case "Contrast.G":
                    dispImage = dispImage.Contrast(Contrast, conTH, false, true, false);
                    break;
                case "Contrast.B":
                    dispImage = dispImage.Contrast(Contrast, conTH, false, false, true);
                    break;
                //gamma
                case "Gamma.I":
                    dispImage = dispImage.GammaI(1 + Gamma);
                    break;
                case "R+":
                    dispImage = dispImage.GammaRGB(1 + Gamma, 1, 1);
                    break;
                case "R-":
                    dispImage = dispImage.GammaRGB(1 - Gamma, 1, 1);
                    break;
                case "G+":
                    dispImage = dispImage.GammaRGB(1, 1 + Gamma, 1);
                    break;
                case "G-":
                    dispImage = dispImage.GammaRGB(1, 1 - Gamma, 1);
                    break;
                case "B+":
                    dispImage = dispImage.GammaRGB(1, 1, 1 + Gamma);
                    break;
                case "B-":
                    dispImage = dispImage.GammaRGB(1, 1, 1 - Gamma);
                    break;
                //color shift
                case "Shift.R":
                    dispImage = dispImage.ColorShift(Convert.ToInt32(textBoxColorShift.Text), 0, 0);
                    break;
                case "Shift.G":
                    dispImage = dispImage.ColorShift(0, Convert.ToInt32(textBoxColorShift.Text), 0);
                    break;
                case "Shift.B":
                    dispImage = dispImage.ColorShift(0, 0, Convert.ToInt32(textBoxColorShift.Text));
                    break;
                //wavelet
                case "WAVELET":
                    dispImage = WaveletFilter.Threshold(dispImage, textBoxDistrictW.Text
                        , Convert.ToInt32(textBoxThresholdW.Text)
                        , radioButtonHardW.Checked);
                    break;
                //其它情况
                default:
                    labelInfo.Text = "void button";
                    return;
            }
            DispMode(dispImage);
            //labelInfo.Text = ErrorInfo() + "    " + (int)((DateTime.Now - dt).TotalMilliseconds) + "ms";
        }

        double DispMagK
        {
            get { return Convert.ToDouble(textBoxMagK.Text); }
        }
        int Iterations
        {
            get { return Convert.ToInt32(textBoxWTIterations.Text); }
        }

        private void EditButtonDispMode_Click(object sender, EventArgs e)
        {
            DispMode(dispImage);
        }
        void DispMode(Bitmap b)
        {
            if (radioButtonHist.Checked)
            {
                pictureBoxShow.Image = b.HistBitmap();
            }
            else if (radioButtonSpectral.Checked)
            {
                pictureBoxShow.Image = FrequencyFilter.GetMagnitudeBitmap(FrequencyFilter.FFT(b)
                    , DispMagK);
            }
            else if (radioButtonWavelet.Checked)
            {
                pictureBoxShow.Image = WaveletFilter.WaveletTransform(b, Iterations);
            }
            else
            {
                pictureBoxShow.Image = dispImage;
            }
        }
        private int rem_iterations = 2;
        string ErrorInfo()
        {
            MseRGB mse = checkBoxWaveletDispMode.Checked
                ? Comparison.MSE(
                        WaveletFilter.IWaveletTransform(dispImage, rem_iterations), oriImage)
                : Comparison.MSE(dispImage, oriImage);
            return "PSNR:" + mse.PSNR().ToString("0.00") + "  MSE:" + mse.ToString();
        }
        #endregion







        #region 事件

        //Mainform
        public MainForm(string[] argv)
        {
            InitializeComponent();
            pictureBoxShow.Image = new Bitmap(1, 1);//防出错
            左上角ToolStripMenuItem_Click(null, null);
            if (argv.Length > 0)
            {
                Open(argv);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            
        }

        //滚轮
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (CtrlPress && (MouseEnterPictureBox || MouseEnterPanel))
            {
                if (e.Delta > 0)
                    ZoomIn();
                else
                    ZoomOut();
            }
            else
            {
                ScrollPictrueBox(0, (e.Delta > 0) ? ScrollDelta : -ScrollDelta);
            }
            base.OnMouseWheel(e);
        }

        //拖拽
        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else e.Effect = DragDropEffects.None;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            Open(((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString());
        }

        //按键
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            CtrlPress = e.Control;
            try
            {
                switch (e.KeyCode)
                {
                    case Keys.Up:
                    case Keys.NumPad8:
                        ScrollPictrueBox(0, ScrollDelta);
                        return;
                    case Keys.Down:
                    case Keys.NumPad2:
                        ScrollPictrueBox(0, -ScrollDelta);
                        return;
                    case Keys.NumPad4:
                        ScrollPictrueBox(ScrollDelta, 0);
                        return;
                    case Keys.NumPad6:
                        ScrollPictrueBox(-ScrollDelta, 0);
                        return;
                }
            }
            catch (System.Exception ex)
            {

            }

        }
        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            CtrlPress = e.Control;
            try
            {
                switch (e.KeyCode)
                {
                    case Keys.Left:
                    case Keys.NumPad1:
                    case Keys.PageUp:
                        GotoPic(FullFilePath, -1);
                        return;
                    case Keys.Right:
                    case Keys.NumPad3:
                    case Keys.PageDown:
                        GotoPic(FullFilePath, 1);
                        return;
                    case Keys.Home:
                        GotoPic(FullFilePath, -2);
                        return;
                    case Keys.End:
                        GotoPic(FullFilePath, 2);
                        return;
                    //小键盘
                    case Keys.NumPad7:
                        向左旋转90度ToolStripMenuItem_Click(null, null);
                        return;
                    case Keys.NumPad9:
                        向右旋转90度ToolStripMenuItem_Click(null, null);
                        return;
                    case Keys.Add:
                        ZoomIn();
                        return;
                    case Keys.Subtract:
                        ZoomOut();
                        return;
                    case Keys.Multiply:
                        Zoom = 1;
                        return;
                    case Keys.NumPad5:
                        Zoom = 0;
                        return;
                    case Keys.NumPad0:
                        GotoPic(FullFilePath, 0);
                        return;
                }
            }
            catch (System.Exception ex)
            {

            }
        }

        //
        private void MainForm_Deactivate(object sender, EventArgs e)
        {
            //when press some hotkeys to open dialog, keydown event will not called,
            //this can prevent zoom when back to the main form.
            CtrlPress = false;
        }

        //picturebox
        private void pictureBoxShow_MouseMove(object sender, MouseEventArgs e)
        {
            MouseEventArgs mouse = e as MouseEventArgs;
            if (mouse.Button == MouseButtons.Left)
            {
                Point mousePosNow = mouse.Location;
                FitPictureLocation(
                    new Point(pictureBoxShow.Location.X + mousePosNow.X - MouseDownPoint.X
                            , pictureBoxShow.Location.Y + mousePosNow.Y - MouseDownPoint.Y));
                FitScrollBar();
            }
        }

        private void pictureBoxShow_MouseDown(object sender, MouseEventArgs e)
        {
            MouseEventArgs mouse = e as MouseEventArgs;
            if (mouse.Button == MouseButtons.Left)
            {
                MouseDownPoint = mouse.Location;
            }
            else if (mouse.Button == MouseButtons.Middle)
            {
                MouseMiddleDownPoint = mouse.Location;
            }
        }

        private void pictureBoxShow_MouseUp(object sender, MouseEventArgs e)
        {
            MouseEventArgs mouse = e as MouseEventArgs;
            if (mouse.Button == MouseButtons.Middle && mouse.Location == MouseMiddleDownPoint)
            {
                全屏ToolStripMenuItem_Click(null, null);
            }
        }

        //picturebox 鼠标
        private void pictureBoxShow_MouseEnter(object sender, EventArgs e)
        {
            MouseEnterPictureBox = true;
        }

        private void pictureBoxShow_MouseLeave(object sender, EventArgs e)
        {
            MouseEnterPictureBox = false;
        }

        //panel1
        private void panel1_Resize(object sender, EventArgs e)
        {
            PaintPictureBox();
            FitPictureLocation();
            FitScrollBar();
        }

        private void panel1_MouseEnter(object sender, EventArgs e)
        {
            MouseEnterPanel = true;
        }

        private void panel1_MouseLeave(object sender, EventArgs e)
        {
            MouseEnterPanel = false;
        }

        //ScrollBar
        private void ScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            FitPictureLocation(new Point(-ScrollBarHon.Value, -ScrollBarVer.Value));
        }

        #endregion

#region 菜单

        private void 打开OToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Supported|*.jpg;*.jpeg;*.bmp;*.raw|Picture(*.jpg;*.bmp)|*.jpg;*.bmp|Gray Raw(*.raw)|*.raw|All(*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Open(ofd.FileName);
            }
        }

        private void 重新打开ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Open(FullFilePath);
        }

        private void 另存为ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = FileName;
            sfd.Filter = "*.jpg|*.jpg|*.bmp|*.bmp";
            sfd.DefaultExt = "*.jpg";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                ImageLoading.Save(sfd.FileName);
            }
        }

        private void 画图板ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("mspaint", "\"" + FullFilePath + "\"");
        }

        private void 打开文件位置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("explorer", "/select, \"" + FullFilePath + "\"");
        }

        private void 退出QToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        //编辑
        private void 撤销ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Undo();
        }

        //视图
        private void 全屏ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
            if (this.FormBorderStyle == FormBorderStyle.Sizable)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                //this.TopMost = true;
            }
            else
            {
                WindowState = FormWindowState.Normal;
                this.FormBorderStyle = FormBorderStyle.Sizable;
                //this.TopMost = false;
            }
        }

        private void 以100比例打开图片ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            以100比例打开图片ToolStripMenuItem.Checked = !以100比例打开图片ToolStripMenuItem.Checked;
        }

        //初始位置
        private void 左上角ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            左上角ToolStripMenuItem.Checked = true;
            右上角ToolStripMenuItem.Checked = false;
            中间ToolStripMenuItem.Checked = false;
        }

        private void 右上角ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            左上角ToolStripMenuItem.Checked = false;
            右上角ToolStripMenuItem.Checked = true;
            中间ToolStripMenuItem.Checked = false;
        }

        private void 中间ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            左上角ToolStripMenuItem.Checked = false;
            右上角ToolStripMenuItem.Checked = false;
            中间ToolStripMenuItem.Checked = true;
        }

        //翻转
        private void 水平翻转ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImageLoading.RotateFlip(RotateFlipType.RotateNoneFlipX);
            PaintPictureBox();
        }

        private void 垂直翻转ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImageLoading.RotateFlip(RotateFlipType.RotateNoneFlipY);
            PaintPictureBox();
        }

        private void 向右旋转90度ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImageLoading.RotateFlip(RotateFlipType.Rotate90FlipNone);
            PaintPictureBox();
            FitPictureLocation();
        }

        private void 向左旋转90度ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImageLoading.RotateFlip(RotateFlipType.Rotate270FlipNone);
            PaintPictureBox();
            FitPictureLocation();
        }

        //右键菜单

        private void 按窗口大小显示ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Zoom = 0;
        }

        private void 实际大小ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Zoom = 1.0;
        }

        private void 复制到剪切板ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetImage(ImageLoading);
        }

#endregion

        //帮助
        private void 关于ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("╮(╯_╰)╭", ".....");
        }

        private void 使用快捷键ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(@"小键盘快捷键
8、2、4、6 上下左右
1、3 上一张，下一张
7、9 旋转
+ - 5 * 放大 缩小 按窗口大小显示 原始比例
0 随机打开同目录图片", @"\(≧▽≦)/");
        }

        private void EditPannelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel1Collapsed = !splitContainer1.Panel1Collapsed;
        }

    }
}

                //方向
                //int orientation_index = Array.IndexOf(ImageLoading.PropertyIdList, OrientationId);
                //orientation_index         对应序号
                //TopLeft = 1,      none       none              1.90        
                //TopRight = 2,     X          4                 2.180
                //BottomRight = 3,  180        2                 3.270
                //BottomLeft = 4,   Y          6                 4.X
                //LeftTop = 5,      90X        5                 5.90X
                //RightTop = 6,     270        1                 6.Y
                //RightBottom = 7,  90Y        7                 7.90Y
                //LeftBottom = 8,   90         3                 RotateFlipType

