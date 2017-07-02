/**
 * Zhejiang University of Technology
 * Date: 2017/03/19
 * Author: Tach
 **/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;
using ImageManipulationExtensionMethods;

using System.ComponentModel;
using System.Windows.Threading;
using System.Runtime.InteropServices;

using System.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

using CommandEngine;
using System.Threading;


namespace ImageProcess
{
    /// <summary>
    /// MainWindow.xaml 的互动逻辑
    /// </summary>
    /// 


    
    public partial class MainWindow : Window
    {
        public delegate void EventHandler(object sender, ImageSource e);

        private BackgroundWorker updateCam;
        //private Capture webcam = null;
        private bool trackStart = false;
        private System.Drawing.Rectangle selectingWindow;
        private ObjectTracking objTracking = null;
        private bool isSelecting = false;
        private System.Windows.Point startPos;
        private System.Windows.Point endPos;


        private bool send_flag = true;
        public string ControlIp = "192.168.1.1";
        public string Port = "2001";
        public string CMD_Forward = "FF000100FF", CMD_Backward = "FF000200FF", CMD_TurnLeft = "FF000400FF", CMD_TurnRight = "FF000300FF", CMD_Stop = "FF000000FF";
        string command = "";

        private int selectArea = 0;
        private int curHorizontal;
        private bool needForword = false, needBack = false, needTurnLeft = false, needTurnRight = false;
        public bool hasSendCom = false;
        private bool getFlag = true;
        private double MAX_THRESHOLD = 0.15, MIN_THRESHOLD = 0.15;
        private int MIN_LEFT = 100,MAX_RIGHT = 540;
        private int SPEED = 0;
        //private string haarXmlPath = "haarcascade_frontalface_default.xml";
        private bool flag_faceDected = false;
        private int headImages = 0;

        public static Image<Bgr, byte> templateImage;

        public WifiRobotCMDEngine RobotEngine = new WifiRobotCMDEngine();//实例化引擎


        public MainWindow()
        {
            InitializeComponent();
        }

        private void init_updateCam()
        {
            //webcam = new Capture(0);
            updateCam = new BackgroundWorker();
            updateCam.DoWork += updateCamWorker;
            updateCam.RunWorkerAsync();
            updateCam.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                updateCam.RunWorkerAsync();
            };
        }

 
        public Bitmap showVideo()
        {
            string sourceURL = "http://192.168.1.1:8080/?action=snapshot";
            byte[] buffer = new byte[500000];
            int read, total = 0;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(sourceURL);
            //req.Credentials = new NetworkCredential("root", "admin");
            WebResponse resp = req.GetResponse();
            Stream stream = resp.GetResponseStream();
            while ((read = stream.Read(buffer, total, 1024)) != 0)
            {
                total += read;
            }
            Console.WriteLine("Total " + total);
            Bitmap bmp = (Bitmap)Bitmap.FromStream(new MemoryStream(buffer, 0, total));
            return bmp;
            //Image<Bgr, byte> frame = new Image<Bgr, byte>(bmp);
            //inputImage.Source = frame.ToBitmapSource();
        }

      
        private void ControlCar(System.Drawing.Rectangle result)
        {
            int curArea = result.Width * result.Height;
            curHorizontal = result.X + result.Width / 2;
            if (getFlag)
            {
                selectArea = curArea;
                getFlag = false;
            }

            Console.WriteLine("CurArea " + curArea);
            Console.WriteLine("SelectArea " + selectArea);

            if (curHorizontal > 0 && curHorizontal <= MIN_LEFT)
            {
                needTurnLeft = true;
                if (needTurnRight)
                {
                    needTurnRight = false;
                    hasSendCom = false;
                }
            }
            else if (curHorizontal >= MAX_RIGHT && curHorizontal <= 640)
            {
                needTurnRight = true;
                if (needTurnLeft)
                {
                    needTurnLeft = false;
                    hasSendCom = false;
                }
            }
            else
            {
                needTurnRight = false;
                needTurnLeft = false;
            }

            if ((selectArea - curArea)*1.0 / selectArea*1.0 > MIN_THRESHOLD)
            {
                needForword = true;
                if (needBack)
                {
                    needBack = false;
                    hasSendCom = false;
                }
            }
            else if ((curArea - selectArea)*1.0 / selectArea*1.0 > MAX_THRESHOLD)
            {
                needBack = true;
                if (needForword)
                {
                    needForword = false;
                    hasSendCom = false;
                }

            }
            else
            {
                needForword = false;
                needBack = false;
            }

            Console.WriteLine("needBack " + needBack);
            Console.WriteLine("needForword " + needForword);
            Console.WriteLine("needTurnLeft " + needTurnLeft);
            Console.WriteLine("needTurnRight " + needTurnRight);
            Console.WriteLine("hasSendCom " + hasSendCom);

            if ((needForword || needBack || needTurnLeft || needTurnRight) && !hasSendCom)
            // if (needForword && !hasSendCom)
            {
                hasSendCom = true;
                if (needForword)
                {
                    if (needTurnLeft)
                        command = CMD_TurnLeft;
                    else if (needTurnRight)
                        command = CMD_TurnRight;
                    else
                        command = CMD_Forward;
                }
                else if (needBack)
                {
                    if (needTurnLeft)
                        command = CMD_TurnLeft;
                    else if (needTurnRight)
                        command = CMD_TurnRight;
                    else
                        command = CMD_Backward;
                }
                else if (needTurnLeft)
                    command = CMD_TurnLeft;
                else if (needTurnRight)
                    command = CMD_TurnRight;

                Console.WriteLine("CMD " + command);
                RobotEngine.SendDataInSocket(command, ControlIp, Port);
            }

            if ((!needForword && !needBack && !needTurnLeft && !needTurnRight) && hasSendCom)
            {
                hasSendCom = false;
                Console.WriteLine("CMD_Stop");
                RobotEngine.SendDataInSocket(CMD_Stop, ControlIp, Port);
            }
        }
        private void updateCamWorker(object sender, DoWorkEventArgs e)
        {
            Image<Bgr, Byte> frame_1 = new Image<Bgr, Byte>(showVideo());
            using (Image<Bgr, Byte> frame = frame_1.Flip(Emgu.CV.CvEnum.FLIP.HORIZONTAL))
            //using (Image<Bgr, Byte> frame = webcam.QueryFrame().Flip(Emgu.CV.CvEnum.FLIP.HORIZONTAL))
            {
                // 人脸检测

                if (flag_faceDected)
                {
                    /*
                    Image<Gray, Byte> gray = frame.Convert<Gray, Byte>(); //Convert it to Grayscale  
                    gray._EqualizeHist();//均衡化 
                    HaarCascade ccr = new HaarCascade(haarXmlPath);
                    MCvAvgComp[] rects = ccr.Detect(gray, 1.1, 2, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,new System.Drawing.Size(20,20),System.Drawing.Size.Empty);
                    //MCvAvgComp[] rects = ccr.Detect(gray, 1.3, 3, new System.Drawing.Size(20, 20), System.Drawing.Size.Empty);
                    foreach (MCvAvgComp r in rects)
                    {
                        //This will focus in on the face from the haar results its not perfect but it will remove a majoriy  
                        //of the background noise  
                        System.Drawing.Rectangle facesDetected = r.rect;

                        frame.Draw(facesDetected, new Bgr(System.Drawing.Color.Red), 3);//绘制检测框  
                        Console.WriteLine("faceDected " + "(" + facesDetected.X + "," + facesDetected.Y + ")" + facesDetected.Width + " " + facesDetected.Height);

                        selectingWindow = facesDetected;
                    }
                    */
                    frame.Save("0.jpg");
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
                    selectingWindow.X = int.Parse(strs[0]);
                    selectingWindow.Y = int.Parse(strs[1]);
                    selectingWindow.Width = int.Parse(strs[2]);
                    selectingWindow.Height = int.Parse(strs[3]);
                    frame.Draw(selectingWindow, new Bgr(System.Drawing.Color.Red), 3);//绘制检测框  

                    flag_faceDected = false;
                }
                if (trackStart)
                {
                    if (objTracking == null)
                    {
                        objTracking = new ObjectTracking(frame, selectingWindow);
                    }
                    else
                    {
                        System.Drawing.Rectangle result = objTracking.Tracking(frame,this);

                        frame.Draw(result, new Bgr(0, 255, 0), 3);

                        // 进行小车控制逻辑的编写
                        int imageWidth = frame.Width;
                        int imageHeight = frame.Height;

                        if (headImages >= 10)
                        {
                            ControlCar(result);
                        }
                        if (headImages++ >= 100000)
                        {
                            headImages = 100000;
                        }

                        Dispatcher.Invoke(DispatcherPriority.Normal,
                            new Action(
                                delegate()
                                {
                                    if (objTracking != null)
                                    {
                                        inputImage.Source = frame.ToBitmapSource();
                                        hueImage.Source = objTracking.hue.ToBitmapSource();
                                        backprojectImage.Source = objTracking.backproject.ToBitmapSource();
                                        maskImage.Source = objTracking.mask.ToBitmapSource();
                                    }
                                }
                                ));
                    }
                }
                else
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal,
                        new Action(
                            delegate()
                            {
                                inputImage.Source = frame.ToBitmapSource();
                            }
                            ));
                }
            }
        }

        private void exitBtn_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void inputImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isSelecting = true;
            startPos = new System.Windows.Point(e.GetPosition(inputImage).X, e.GetPosition(inputImage).Y);
            Console.WriteLine("DOWN!" + startPos);
        }

        private void inputImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            inputImageCanvas.Children.Clear();
            isSelecting = false;
     
            endPos = new System.Windows.Point(Math.Min(e.GetPosition(inputImage).X, inputImage.Width),
                                Math.Min(e.GetPosition(inputImage).Y, inputImage.Height));
            Console.WriteLine("UP!" + endPos);

            int xRate = (int)(inputImage.Source.Width / inputImage.Width);
            int yRate = (int)(inputImage.Source.Height / inputImage.Height);
            selectingWindow = new System.Drawing.Rectangle(
                (int)startPos.X * xRate, (int)startPos.Y * yRate,
                (int)(endPos.X - startPos.X) * xRate, (int)(endPos.Y - startPos.Y) * yRate);

            //selectArea = selectingWindow.Width * selectingWindow.Height;
            trackStart = true;
            objTracking = null;
        }

        private void inputImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                System.Windows.Point movePos = new System.Windows.Point(e.GetPosition(inputImage).X, e.GetPosition(inputImage).Y);
                inputImageCanvas.Children.Clear();
                System.Windows.Shapes.Rectangle rectangle = new System.Windows.Shapes.Rectangle();
                rectangle.SetValue(Canvas.LeftProperty, (double)startPos.X);//Math.Min(startPos.X, endPos.X)
                rectangle.SetValue(Canvas.TopProperty, (double)startPos.Y);//Math.Min(startPos.Y, endPos.Y)
                rectangle.Width = Math.Abs(movePos.X - startPos.X);
                rectangle.Height = Math.Abs(movePos.Y - startPos.Y);
                rectangle.Stroke = new SolidColorBrush() { Color = Colors.Red, Opacity = 0.75f };
                rectangle.StrokeThickness = 3;
                inputImageCanvas.Children.Add(rectangle);
                
            }
        }

        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            init_updateCam();
            startBtn.IsEnabled = false;
           // byte[] Speedright_data = RobotEngine.CreateData(0X02, 0X01, Convert.ToByte(SPEED));//舵机数据打包第一个参数代表舵机，第二个代表哪个舵机，第三个代表转动角度值
           // RobotEngine.SendDataInSocket(Speedright_data, ControlIp, Port);
        }

        
        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            if (send_flag)
            {
                send_flag = false;
                Thread t;
                t = new Thread(delegate()
                {
                     RobotEngine.SendDataInSocket(CMD_Forward, ControlIp, Port);
                });
                t.Start();              
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            send_flag = true;
            Thread t;
            t = new Thread(delegate()
            {
                 RobotEngine.SendDataInSocket(CMD_Stop, ControlIp, Port);
            });
            t.Start();
        }

        private void Track_Click(object sender, RoutedEventArgs e)
        {
            trackStart = true;
            objTracking = null;
            flag_faceDected = true;
        }
    }
}

