using System;
using System.Windows.Forms;
using OpenCvSharp;

namespace TrafficManagement
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        CvCapture capture;
        IplImage src;

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                /*video*/

                capture = CvCapture.FromFile("../../../project_video.mp4");
                LaneDetection Convert = new LaneDetection();

                /* camera
                capture = CvCapture.FromCamera(CaptureDevice.DShow, 0);
                capture.SetCaptureProperty(capture.FrameWidth, 640);
                capture.SetCaptureProperty(capture.FrameHeight, 480);
                */
            }
            catch
            {
                timer1.Enabled = false;
            }
        }

       
        private void timer1_Tick(object sender, EventArgs e)
        {
            LaneDetection Convert = new LaneDetection();
            src = capture.QueryFrame();
            
            if(src != null)
            {                
                IplImage yw_canny = new IplImage(src.Size, BitDepth.U8, 3);
                IplImage y_canny = new IplImage(src.Size, BitDepth.U8, 3);
                IplImage w_canny = new IplImage(src.Size, BitDepth.U8, 3);

                 w_canny = Convert.CannyLine(src, 'w');
                y_canny = Convert.CannyLine(src, 'y');
                yw_canny = w_canny + y_canny;
                pictureBoxIpl4.ImageIpl = yw_canny;                

                pictureBoxIpl2.ImageIpl = Convert.HoughLines(src);
            }
            else
            {
                pictureBoxIpl2.ImageIpl = null;
                timer1.Enabled = false;
            }

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Cv.ReleaseImage(src);
            if (src != null) src.Dispose();
        }
    }
}
