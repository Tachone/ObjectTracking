
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;
using CommandEngine;
using System.Diagnostics;
using System.IO;

namespace ImageProcess
{
    class ObjectTracking
    {
        public Image<Hsv, Byte> hsv;
        public Image<Gray, Byte> hue;
        public Image<Gray, Byte> mask;
        public Image<Gray, Byte> backproject;
        public DenseHistogram hist;
        private Rectangle trackingWindow;
        private MCvConnectedComp trackcomp;
        private MCvBox2D trackbox;
        private System.Drawing.Rectangle lastRect;
        private System.Drawing.Point tempPoint;
        private int retry = 0;
        public ObjectTracking(Image<Bgr, Byte> image, Rectangle ROI)
        {
            // Initialize parameters
            trackbox = new MCvBox2D();
            trackcomp = new MCvConnectedComp();
            hue = new Image<Gray, byte>(image.Width, image.Height);
            hue._EqualizeHist();
            mask = new Image<Gray, byte>(image.Width, image.Height);
            //hist = new DenseHistogram(10, new RangeF(0, 255));
            hist = new DenseHistogram(30, new RangeF(0, 180));
            backproject = new Image<Gray, byte>(image.Width, image.Height);

            // Assign Object's ROI from source image.
            trackingWindow = ROI;

            // Producing Object's hist
            CalObjectHist(image);
        }
        public Rectangle Tracking(Image<Bgr, Byte> image, MainWindow mw)
        {
            UpdateHue(image);

            // Calucate BackProject
            backproject = hist.BackProject(new Image<Gray, Byte>[] { hue });

            // Apply mask
            backproject._And(mask);

            // Tracking windows empty means camshift lost bounding-box last time
            // here we give camshift a new start window from 0,0 (you could change it)
            if (trackingWindow.IsEmpty || trackingWindow.Width==0 || trackingWindow.Height==0)
            {
                trackingWindow = new Rectangle(0, 0, 100, 100);
            }
            CvInvoke.cvCamShift(backproject, trackingWindow,
                new MCvTermCriteria(10, 1), out trackcomp, out trackbox);

            // update tracking window
            int width_ = trackcomp.rect.Width;
            int height_ = trackcomp.rect.Height;

            //形态学约束

            if (height_ >= width_ && (2.5 * width_ - 1.0 * height_ ) > 0.01)
            {
                trackingWindow = trackcomp.rect;
               // retry = 0;
            }
            else
            {
                //trackingWindow = trackcomp.rect;
                  mw.RobotEngine.SendDataInSocket(mw.CMD_Stop, mw.ControlIp, mw.Port);
                  mw.hasSendCom = false;
                  retry++;
                  if (retry>=6)
                  {
                      System.Environment.Exit(0); 
                  }
                 /*
                  Image<Gray, Byte> gray = image.Convert<Gray, Byte>(); //Convert it to Grayscale  
                  gray._EqualizeHist();//均衡化 
                  HaarCascade ccr = new HaarCascade("haarcascade_frontalface_default.xml");
                  MCvAvgComp[] rects = ccr.Detect(gray, 1.1, 2, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,new System.Drawing.Size(20,20),System.Drawing.Size.Empty);
                  //MCvAvgComp[] rects = ccr.Detect(gray, 1.3, 3, new System.Drawing.Size(20, 20), System.Drawing.Size.Empty);
                  foreach (MCvAvgComp r in rects)
                  {
                      //This will focus in on the face from the haar results its not perfect but it will remove a majoriy  
                      //of the background noise  
                      System.Drawing.Rectangle facesDetected = r.rect;

                      image.Draw(facesDetected, new Bgr(System.Drawing.Color.Red), 3);//绘制检测框  
                      Console.WriteLine("faceDected " + "(" + facesDetected.X + "," + facesDetected.Y + ")" + facesDetected.Width + " " + facesDetected.Height);

                      trackingWindow = facesDetected;
                  }
                  */
                  
                  image.Save("0.jpg");
                  Process p = new Process();
                  p.StartInfo.FileName = "HumanDection.exe";
                  p.StartInfo.Arguments = "0.jpg seeta_fd_frontal_v1.0.bin";
                  p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                  p.Start();
                  p.WaitForExit();//关键，等待外部程序退出后才能往下执行

                  FileStream fs = new FileStream("face.txt", FileMode.Open, FileAccess.Read);
                  StreamReader sr = new StreamReader(fs);
                  string s = sr.ReadLine();
                  fs.Close();
                  string[] strs = s.Split(' ');
                  trackingWindow.X = int.Parse(strs[0]);
                  trackingWindow.Y = int.Parse(strs[1]);
                  trackingWindow.Width = int.Parse(strs[2]);
                  trackingWindow.Height = int.Parse(strs[3]);
                  image.Draw(trackingWindow, new Bgr(System.Drawing.Color.Red), 3);//绘制检测框  
                  
            }

            //trackingWindow = trackcomp.rect;

            return trackingWindow;
        }

        private void CalObjectHist(Image<Bgr, Byte> image)
        {
            UpdateHue(image);

            // Set tracking object's ROI
            hue.ROI = trackingWindow;
            mask.ROI = trackingWindow;
            hist.Calculate(new Image<Gray, Byte>[] { hue }, false, mask);

            // Scale Historgram
            float max=0, min=0, scale=0;
            int[] minLocations, maxLocations;
            hist.MinMax(out min, out max, out minLocations, out maxLocations);
            if (max != 0)
            {
                scale = 255 / max;
            }
            CvInvoke.cvConvertScale(hist.MCvHistogram.bins, hist.MCvHistogram.bins, scale, 0);

            // Clear ROI
            hue.ROI = System.Drawing.Rectangle.Empty;
            mask.ROI = System.Drawing.Rectangle.Empty;

            // Now we have Object's Histogram, called hist.
        }

        private void UpdateHue(Image<Bgr, Byte> image)
        {
            // release previous image memory
            if (hsv != null)    hsv.Dispose();
            hsv = image.Convert<Hsv, Byte>();

            // Drop low saturation pixels
            mask = hsv.Split()[1].ThresholdBinary(new Gray(60), new Gray(255));
            CvInvoke.cvInRangeS(hsv, new MCvScalar(0, 30, Math.Min(10, 255), 0),
                new MCvScalar(180, 256, Math.Max(10, 255), 0), mask);

            // Get Hue
            hue = hsv.Split()[0];
        }


    }
}
