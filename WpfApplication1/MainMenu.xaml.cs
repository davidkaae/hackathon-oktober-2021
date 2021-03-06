using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Coding4Fun.Kinect.Wpf.Controls;
using YeelightAPI;

namespace WpfApplication1
{
    public class State
    {
        public int Red { get; }
        public int Green { get; }
        public int Blue { get; }
        private readonly string _colorName;

        public State(int red, int green, int blue, string colorName)
        {
            Red = red;
            Green = green;
            Blue = blue;
            _colorName = colorName;
        }

        public bool IsMatched(int red, int green, int blue)
        {
            var isRed = red == 255;
            var isGreen = green == 255;
            var isBlue = blue == 255;

            switch (_colorName)
            {
                case "red":
                    return isRed && !isGreen && !isBlue;
                case "green":
                    return !isRed && isGreen && !isBlue;
                case "blue":
                    return !isRed && !isGreen && isBlue;
                case "yellow":
                    return isRed && isGreen && !isBlue;
                case "purple":
                    return isRed && !isGreen && isBlue;
                default:
                    return false;

            }
        }
    }

    /// <summary>
    /// Interaction logic for Page.xaml
    /// </summary>
    public partial class MainMenu : Page
    {
        private KinectSensor _Kinect;
        private WriteableBitmap _ColorImageBitmap;
        private Int32Rect _ColorImageBitmapRect;
        private int _ColorImageStride;
        private Skeleton[] FrameSkeletons;
        private Device lampBig;
        private Device lampSmall;
        private static int defaultStartLength = 3;
        private int timeToPlay = defaultStartLength;

        List<Button> buttons;
        static Button selected;

        float handX;
        float handY;

        public List<State> ColorStates { get; set; }
        public int stateIndex = 0;

        public MainMenu()
        {

            ColorStates = new List<State>
            {
                new State(250, 0, 0, "red"),
                new State(0, 255, 0, "green"),
                new State(0, 0, 255, "blue"),
                
                new State(255, 255, 0, "yellow"),
                
                new State(255, 0, 255, "purple")
            };
         
          
            InitializeComponent();
            InitializeButtons();
            Generics.ResetHandPosition(kinectButton);
            Generics.ResetHandPosition(kinectButton2);
            kinectButton.Click += new RoutedEventHandler(kinectButton_Click);
            
            this.Loaded += (s, e) => { DiscoverKinectSensor(); };

        }

        #region "Hand Gesture"

        //initialize buttons to be checked
        private void InitializeButtons()
        {
            //Label1.Content = "Looking for you...";
            //Label1.Visibility = Visibility.Visible;
            //Label1.FontSize = 50;

            lampSmall = new Device("172.16.1.144");
            lampSmall.Connect();
            var currentTargetColor = ColorStates[stateIndex];
            lampSmall.SetRGBColor(currentTargetColor.Red,currentTargetColor.Green, currentTargetColor.Blue);
            lbColorTarget.Background = new SolidColorBrush(Color.FromRgb((byte)currentTargetColor.Red, (byte)currentTargetColor.Green, (byte)currentTargetColor.Blue));



            //buttons = new List<Button> { GAME1, GAME2, GAME3 };
            lampBig = new Device("172.16.1.147");
            lampBig.Connect();
            Videoplayer.LoadedBehavior = MediaState.Manual;
          //  Videoplayer.Play();
            Videoplayer.MediaEnded += Videoplayer_MediaEnded;
        //    Videoplayer.UnloadedBehavior = MediaState.Manual;
        }

        private void Videoplayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            showWinningMessage();
        }

        //raise event for Kinect sensor status changed
        private void DiscoverKinectSensor()
        {
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            this.Kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
        }


        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    if (this.Kinect == null)
                    {
                        this.Kinect = e.Sensor;
                    }
                    break;
                case KinectStatus.Disconnected:
                    if (this.Kinect == e.Sensor)
                    {
                        this.Kinect = null;
                        this.Kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
                        if (this.Kinect == null)
                        {
                            MessageBox.Show("Sensor Disconnected. Please reconnect to continue.");
                        }
                    }
                    break;
            }
        }

        public KinectSensor Kinect
        {
            get { return this._Kinect; }
            set
            {
                if (this._Kinect != value)
                {
                    if (this._Kinect != null)
                    {
                        this._Kinect = null;
                    }
                    if (value != null && value.Status == KinectStatus.Connected)
                    {
                        this._Kinect = value;
                        InitializeKinectSensor(this._Kinect);
                    }
                }
            }
        }



        private void InitializeKinectSensor(KinectSensor kinectSensor)
        {
            if (kinectSensor != null)
            {
                ColorImageStream colorStream = kinectSensor.ColorStream;
                colorStream.Enable();
                this._ColorImageBitmap = new WriteableBitmap(colorStream.FrameWidth, colorStream.FrameHeight,
                    96, 96, PixelFormats.Bgr32, null);
                this._ColorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth, colorStream.FrameHeight);
                this._ColorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;
                // videoStream.Source = this._ColorImageBitmap;

                kinectSensor.SkeletonStream.Enable(new TransformSmoothParameters()
                {
                    Correction = 0.5f,
                    JitterRadius = 0.05f,
                    MaxDeviationRadius = 0.04f,
                    Smoothing = 0.5f
                });

                kinectSensor.SkeletonFrameReady += Kinect_SkeletonFrameReady;
                kinectSensor.ColorFrameReady += Kinect_ColorFrameReady;

                if (!kinectSensor.IsRunning)
                {
                    kinectSensor.Start();
                }

                this.FrameSkeletons = new Skeleton[this.Kinect.SkeletonStream.FrameSkeletonArrayLength];

            }
        }

        private void Kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    byte[] pixelData = new byte[frame.PixelDataLength];
                    frame.CopyPixelDataTo(pixelData);
                    this._ColorImageBitmap.WritePixels(this._ColorImageBitmapRect, pixelData,
                        this._ColorImageStride, 0);
                }
            }
        }
        
        private int count = 0;
        private void Kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            pg2.Value = timeToPlay - Videoplayer.Position.Seconds;
            if (Videoplayer.Position.Seconds > timeToPlay)
            {
                Videoplayer.Pause();
            }





            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame != null)
                {
                    frame.CopySkeletonDataTo(this.FrameSkeletons);
                    Skeleton skeleton = GetPrimarySkeleton(this.FrameSkeletons);

                    if (skeleton == null)
                    {
                        kinectButton.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        //Joint primaryHand = GetPrimaryHand(skeleton);
                        //TrackHand(primaryHand);;
                        Joint rightHand = skeleton.Joints[JointType.HandRight];
                        TrackHand(rightHand,kinectButton);

                        Joint leftHand = skeleton.Joints[JointType.HandLeft];
                        TrackHand(leftHand,kinectButton2);

                        var center = skeleton.Joints[JointType.ShoulderCenter];
                        var rightFoot = skeleton.Joints[JointType.FootRight];
                        var leftFoot = skeleton.Joints[JointType.FootLeft];


                        var redColor =  (CalculateDiffOnAxis(rightHand.Position.X, center.Position.X) + CalculateDiffOnAxis(rightHand.Position.Y, center.Position.Y)) * 2.5;
                        var greenColor = (CalculateDiffOnAxis(leftHand.Position.X, center.Position.X) + CalculateDiffOnAxis(leftHand.Position.Y, center.Position.Y)) * 2.5;
                        var blueColor = (CalculateDiffOnAxis(leftFoot.Position.X, rightFoot.Position.X) + CalculateDiffOnAxis(leftFoot.Position.Y, rightFoot.Position.Y)) * 2.5;
                        if (redColor > 150) redColor = 255;
                        if (greenColor > 150) greenColor = 255; 
                        if (blueColor > 125) blueColor = 255;

                        if (redColor < 100) redColor = 0;
                        if (greenColor < 100) greenColor = 0;
                        if (blueColor < 100) blueColor = 0;

                        var brightness = (int) ((rightHand.Position.X - leftHand.Position.X) * 100);
                        
                        rh.Content = "x: " + Math.Abs((int)((rightHand.Position.X - center.Position.X) * 100)) + " y: " + Math.Abs((int)((rightHand.Position.Y - center.Position.Y) * 100));
                        lh.Content = "x: " + Math.Abs((int)((leftHand.Position.X - center.Position.X) * 100)) + " y: " + Math.Abs((int)((leftHand.Position.Y - center.Position.Y) * 100));
                        
                        rhxy.Content = $"MatchOnRed: { redColor}";
                        lhxy.Content = $"MatchOnGreen: { greenColor}";
                        rkxy.Content = $"MatchOnBlue: { blueColor}";
                        lbColor.Background = new SolidColorBrush(Color.FromRgb((byte)redColor, (byte)greenColor, (byte)blueColor));

                        if (count % 15 == 0)
                        {
                            bool updateSuccess = false;

                            //if(brightness >= 0 && brightness <= 100)
                            //    lampBig.SetBrightness(brightness);
                            if (redColor >= 0 && redColor <= 255 && greenColor >= 0 && greenColor <= 255 && blueColor >= 0 && blueColor <= 255)
                            {
                                lampBig.SetRGBColor((int)redColor, (int)greenColor, (int)blueColor);
                            }

                            if (count % 150 == 0)
                            {
                                
                                lampBig.Connect();
                                //lampSmall.Connect();
                            }

                            var currentTargetColor = ColorStates[stateIndex];
                            if (currentTargetColor.IsMatched((int) redColor, (int) greenColor, (int)blueColor))
                            {
                                stateIndex++;
                                
                                if (stateIndex >= ColorStates.Count)
                                    stateIndex = 0;
                                
                                currentTargetColor = ColorStates[stateIndex];
                                
                                Task.Run(()=>lampSmall.Connect());
                                Thread.Sleep(55);

                                Task.Run(()=> lampSmall.SetRGBColor(currentTargetColor.Red,currentTargetColor.Green, currentTargetColor.Blue));
                                lbColorTarget.Background = new SolidColorBrush(Color.FromRgb((byte)currentTargetColor.Red, (byte)currentTargetColor.Green, (byte)currentTargetColor.Blue));

                                MoveCompleted();
                            }

                        }

                        count++;

                    }
                }
            }
        }

        private int CalculateDiffOnAxis (float x, float y)
        {
            return (int)(Math.Abs(x - y) * 100);
        }

        //track and display hand
        private void TrackHand(Joint hand,HoverButton kinectButton)
        {
            if (hand.TrackingState == JointTrackingState.NotTracked)
            {
                kinectButton.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                kinectButton.Visibility = System.Windows.Visibility.Visible;

                DepthImagePoint point = this.Kinect.CoordinateMapper.MapSkeletonPointToDepthPoint(hand.Position, DepthImageFormat.Resolution640x480Fps30);
                handX = (int)((point.X * LayoutRoot.ActualWidth / this.Kinect.DepthStream.FrameWidth) -
                    (kinectButton.ActualWidth / 2.0));
                handY = (int)((point.Y * LayoutRoot.ActualHeight / this.Kinect.DepthStream.FrameHeight) -
                    (kinectButton.ActualHeight / 2.0));
                Canvas.SetLeft(kinectButton, handX);
                Canvas.SetTop(kinectButton, handY);

               // if (isHandOver(kinectButton, buttons)) kinectButton.Hovering();
               // else kinectButton.Release();
                
                if (hand.JointType == JointType.HandRight)
                {
                    kinectButton.ImageSource = "/WpfApplication1;component/Images/myhand.png";
                    kinectButton.ActiveImageSource = "/RVI_Education;component/Images/myhand.png";
                }
                else
                {
                    kinectButton.ImageSource = "/WpfApplication1;component/Images/myhand.png";
                    kinectButton.ActiveImageSource = "/WpfApplication1;component/Images/myhand.png";
                }
            }
        }

        //detect if hand is overlapping over any button
        //private bool isHandOver(FrameworkElement hand, List<Button> buttonslist)
        //{
        //    var handTopLeft = new Point(Canvas.GetLeft(hand), Canvas.GetTop(hand));
        //    var handX = handTopLeft.X + hand.ActualWidth / 2;
        //    var handY = handTopLeft.Y + hand.ActualHeight / 2;

        //    foreach (Button target in buttonslist)
        //    {

        //        if (target != null)
        //        {
        //            Point targetTopLeft = new Point(Canvas.GetLeft(target), Canvas.GetTop(target));
        //            if (handX > targetTopLeft.X &&
        //                handX < targetTopLeft.X + target.Width &&
        //                handY > targetTopLeft.Y &&
        //                handY < targetTopLeft.Y + target.Height)
        //            {
        //                selected = target;
        //                return true;
        //            }
        //        }
        //    }
        //    return false;
        //}

        //get the hand closest to the Kinect sensor
        private static Joint GetPrimaryHand(Skeleton skeleton)
        {
            Joint primaryHand = new Joint();
            if (skeleton != null)
            {
                primaryHand = skeleton.Joints[JointType.HandLeft];
                Joint rightHand = skeleton.Joints[JointType.HandRight];
                if (rightHand.TrackingState != JointTrackingState.NotTracked)
                {
                    if (primaryHand.TrackingState == JointTrackingState.NotTracked)
                    {
                        primaryHand = rightHand;
                    }
                    else
                    {
                        if (primaryHand.Position.Z > rightHand.Position.Z)
                        {
                            primaryHand = rightHand;
                        }
                    }
                }
            }
            return primaryHand;
        }

        //get the skeleton closest to the Kinect sensor
        private static Skeleton GetPrimarySkeleton(Skeleton[] skeletons)
        {
            Skeleton skeleton = null;
            if (skeletons != null)
            {
                for (int i = 0; i < skeletons.Length; i++)
                {
                    if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                    {
                        if (skeleton == null)
                        {
                            skeleton = skeletons[i];
                        }
                        else
                        {
                            if (skeleton.Position.Z > skeletons[i].Position.Z)
                            {
                                skeleton = skeletons[i];
                            }
                        }
                    }
                }
            }
            return skeleton;
        }

        void kinectButton_Click(object sender, RoutedEventArgs e)
        {
            selected.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, selected));
        }

        #endregion 
        private void GAME1_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow.FindName("_mainFrame") as Frame).Source = new Uri("Game1.xaml", UriKind.Relative); 
        }

        private void GAME2_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow.FindName("_mainFrame") as Frame).Source = new Uri("Game2.xaml", UriKind.Relative); 
        }

        private void GAME3_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow.FindName("_mainFrame") as Frame).Source = new Uri("Game3.xaml", UriKind.Relative); 
        }

        private void MoveCompleted()
        {
            timeToPlay += defaultStartLength;
            Videoplayer.Play();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            timeToPlay = defaultStartLength;
            Videoplayer.Position = TimeSpan.MinValue;
            Videoplayer.Play();

        }

        private void showWinningMessage()
        {

        }
    }
}
