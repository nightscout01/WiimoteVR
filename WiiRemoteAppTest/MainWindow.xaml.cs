// Copyright 2019 Maurice Montag
// 3D Transform Code based off of code by Johnny Lee



using HelixToolkit.Wpf;  // used for loading 3D models. Makes everything blue by default for some reason
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WiimoteLib;

namespace WiiRemoteAppTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ModelImporter modelImporter;
        private readonly ModelVisual3D modelVisual;
        private readonly PerspectiveCamera camera;
        private readonly AmbientLight light;

        private const float radiansPerPixel = (float)(Math.PI / 4) / 1024.0f;  // 45 degree field of view with a 1024x768 camera
        private const float dotDistanceInMM = 8.5f * 25.4f;  // width of the wii sensor bar (8.5 inches)
        private const float movementScaling = 1f;

        private float headX = 0;
        private float headY = 0;
        private float headDist = 0;
        private readonly bool cameraIsAboveScreen = false;  // has no affect until zeroing and then is set automatically.
        private float screenHeightinMM = 692.15f; //20 * 25.4f; <-from the original code, 25.4 mm is 1 inch
        private float cameraVerticalAngle = 0;  // begins assuming the camera is point straight forward
        private float relativeVerticalAngle = 0;  // current head position view angle
        private Point3D lastPosition = new Point3D();

        public MainWindow()
        {
            camera = new PerspectiveCamera(new Point3D(0, 0, 0), new Vector3D(0, 0, 1), new Vector3D(0, 1, 0), 45);  // create a new camera with the given transformation matrices
            Wiimote wm = new Wiimote();
            wm.Connect();  // connect to first wii remote found

            wm.WiimoteChanged += Wiimote_Update;  // register the event function
            wm.SetReportType(InputReport.IRAccel, true);  // report back only IR and Accelerometer data, and report it continiously.
            wm.SetLEDs(1);

            modelImporter = new ModelImporter();  // initialize some fields, and the model3D group
            modelVisual = new ModelVisual3D();
            Model3DGroup models = new Model3DGroup();




            InitializeComponent();
            // looks like the 3D coord system makes actual sense instead of WPFs usual weirdness with 0,0 being at the top left
            viewport3D1.Camera = camera;
            // Asign the camera to the viewport


            // in typical WPF fashion, this is an absolute mess
            light = new AmbientLight(Color.FromRgb(255, 255, 255));
            Model3DGroup group = modelImporter.Load("cube.obj");
            group.Children.Add(light);
            modelVisual.Content = group;
            viewport3D1.Children.Add(modelVisual);
        }

        public void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;
            this.KeyDown += new KeyEventHandler(MainWindow_KeyDown);
        }

        public void CalibrateWiimote()
        {
            // Zeros the head position and computes the camera tilt
            double angle = Math.Acos(.5 / headDist) - Math.PI / 2;//angle of head to screen
            if (!cameraIsAboveScreen)
                angle = -angle;
            cameraVerticalAngle = (float)angle; // (float)((angle - relativeVerticalAngle));//absolute camera angle 
            // I need to figure out which one is better
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key == Key.Space)
            {
                CalibrateWiimote();
            }
            if (args.Key == Key.A)
            {
                Transform3DGroup transformGroup = new Transform3DGroup();
                TranslateTransform3D XPlus = new TranslateTransform3D(new Vector3D(1, 0, 0));
                transformGroup.Children.Add(XPlus);
                transformGroup.Children.Add(modelVisual.Transform);
                modelVisual.Transform = transformGroup;
            }
            if (args.Key == Key.D)
            {
                TranslateTransform3D XMinus = new TranslateTransform3D(new Vector3D(-1, 0, 0));
                Transform3DGroup transformGroup = new Transform3DGroup();
                transformGroup.Children.Add(XMinus);
                transformGroup.Children.Add(modelVisual.Transform);
                modelVisual.Transform = transformGroup;
            }
            if (args.Key == Key.W)
            {
                Transform3DGroup transformGroup = new Transform3DGroup();
                TranslateTransform3D ZPlus = new TranslateTransform3D(new Vector3D(0, 0, 1));
                transformGroup.Children.Add(ZPlus);
                transformGroup.Children.Add(modelVisual.Transform);
                modelVisual.Transform = transformGroup;
            }
            if (args.Key == Key.S)
            {
                TranslateTransform3D ZMinus = new TranslateTransform3D(new Vector3D(0, 0, -1));
                Transform3DGroup transformGroup = new Transform3DGroup();
                transformGroup.Children.Add(ZMinus);
                transformGroup.Children.Add(modelVisual.Transform);
                modelVisual.Transform = transformGroup;
            }
            if (args.Key == Key.Q)
            {
                Transform3DGroup transformGroup = new Transform3DGroup();
                TranslateTransform3D YPlus = new TranslateTransform3D(new Vector3D(0, 1, 0));
                transformGroup.Children.Add(YPlus);
                transformGroup.Children.Add(modelVisual.Transform);
                modelVisual.Transform = transformGroup;
            }
            if (args.Key == Key.Z)
            {
                TranslateTransform3D YMinus = new TranslateTransform3D(new Vector3D(0, -1, 0));
                Transform3DGroup transformGroup = new Transform3DGroup();
                transformGroup.Children.Add(YMinus);
                transformGroup.Children.Add(modelVisual.Transform);
                modelVisual.Transform = transformGroup;
            }
        }

        private void Wiimote_Update(object sender, WiimoteChangedEventArgs args)
        {
            ParseWiimoteData(args.WiimoteState);

            if (Application.Current == null)  // needed to fix weird exceptions in visual studio when exiting a program
            {
                return;
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                HeadPositionTextBox.Text = "HeadX = " + headX + " HeadY = " + headY + " HeadDist = " + headDist;
                Point3D newHeadPos = new Point3D(headX, headY, -headDist);
                Vector3D cameraLookDir = camera.LookDirection;
                Point3D pointToLookAt = new Point3D(headX, headY, 0);
                cameraLookDir.Normalize();
                Point3D lookDirection = pointToLookAt;//(Point3D) (cameraLookDir * -newHeadPos.Z);
                // the way I have here ^ seems to work better than the way in the original 2007 code, but I need to make sure.
                camera.Position += (newHeadPos - lastPosition);
                CameraPosTexrbox.Text = camera.Position.ToString();
                lastPosition = newHeadPos;
                camera.FieldOfView = 107 - 0.1944 * headDist * screenHeightinMM / 10;
            });
        }

        public void ParseWiimoteData(WiimoteState wiimoteState)
        {
            PointF[] wiimotePointsNormalized = new PointF[4];
            if (wiimoteState == null)
                return;

            WiimoteLib.Point firstPoint = new WiimoteLib.Point();
            WiimoteLib.Point secondPoint = new WiimoteLib.Point();
            int numvisible = 0;

            if (wiimoteState.IRState.IRSensors[0].Found)
            {
                wiimotePointsNormalized[0].X = 1.0f - wiimoteState.IRState.IRSensors[0].RawPosition.X / 768.0f;  // wii IR camera resolution is 1024x768
                wiimotePointsNormalized[0].Y = wiimoteState.IRState.IRSensors[0].RawPosition.Y / 768.0f;
                firstPoint.X = wiimoteState.IRState.IRSensors[0].RawPosition.X;
                firstPoint.Y = wiimoteState.IRState.IRSensors[0].RawPosition.Y;
                numvisible = 1;
            }
            else
            {
                // not visible
                // blank else statement just in case
            }
            if (wiimoteState.IRState.IRSensors[1].Found)
            {
                wiimotePointsNormalized[1].X = 1.0f - wiimoteState.IRState.IRSensors[1].RawPosition.X / 768.0f;
                wiimotePointsNormalized[1].Y = wiimoteState.IRState.IRSensors[1].RawPosition.Y / 768.0f;
                if (numvisible == 0)
                {
                    firstPoint.X = wiimoteState.IRState.IRSensors[1].RawPosition.X;
                    firstPoint.Y = wiimoteState.IRState.IRSensors[1].RawPosition.Y;
                    numvisible = 1;
                }
                else
                {
                    secondPoint.X = wiimoteState.IRState.IRSensors[1].RawPosition.X;
                    secondPoint.Y = wiimoteState.IRState.IRSensors[1].RawPosition.Y;
                    numvisible = 2;
                }
            }
            else
            {
                // not visible
                // blank else statement just in case
            }
            if (wiimoteState.IRState.IRSensors[2].Found)
            {
                wiimotePointsNormalized[2].X = 1.0f - wiimoteState.IRState.IRSensors[2].RawPosition.X / 768.0f;
                wiimotePointsNormalized[2].Y = wiimoteState.IRState.IRSensors[2].RawPosition.Y / 768.0f;
                if (numvisible == 0)
                {
                    firstPoint.X = wiimoteState.IRState.IRSensors[2].RawPosition.X;
                    firstPoint.Y = wiimoteState.IRState.IRSensors[2].RawPosition.Y;
                    numvisible = 1;
                }
                else if (numvisible == 1)
                {
                    secondPoint.X = wiimoteState.IRState.IRSensors[2].RawPosition.X;
                    secondPoint.Y = wiimoteState.IRState.IRSensors[2].RawPosition.Y;
                    numvisible = 2;
                }
            }
            else
            {
                // not visible
                // blank else statement just in case
            }
            if (wiimoteState.IRState.IRSensors[3].Found)
            {
                wiimotePointsNormalized[3].X = 1.0f - wiimoteState.IRState.IRSensors[3].RawPosition.X / 768.0f;
                wiimotePointsNormalized[3].Y = wiimoteState.IRState.IRSensors[3].RawPosition.Y / 768.0f;
                if (numvisible == 1)
                {
                    secondPoint.X = wiimoteState.IRState.IRSensors[3].RawPosition.X;
                    secondPoint.Y = wiimoteState.IRState.IRSensors[3].RawPosition.Y;
                    numvisible = 2;
                }
            }
            else
            {
                // not visible
                // blank else statement just in case
            }

            if (numvisible == 2)
            {
                float dx = firstPoint.X - secondPoint.X;
                float dy = firstPoint.Y - secondPoint.Y;
                float pointDist = (float)Math.Sqrt(dx * dx + dy * dy);

                float angle = radiansPerPixel * pointDist / 2;
                // in units of screen height since the box is a unit cube and box height is 1
                headDist = movementScaling * (float)((dotDistanceInMM / 2) / Math.Tan(angle)) / screenHeightinMM;


                float avgX = (firstPoint.X + secondPoint.X) / 2.0f;
                float avgY = (firstPoint.Y + secondPoint.Y) / 2.0f;

                // for when we want to use mouse x and y coords instead of wii remote coords for testing

                //System.Windows.Point p = new System.Windows.Point();
                //// should calculate based on distance
                //Application.Current.Dispatcher.Invoke(() =>
                //{
                //    p = Mouse.GetPosition(Application.Current.MainWindow);
                //    Console.WriteLine(p.X);
                //});

                //avgX = (float)p.X;
                //avgY = (float)p.Y;
                headX = (float)(movementScaling * Math.Sin(radiansPerPixel * (avgX - 512)) * headDist);

                relativeVerticalAngle = (avgY - 384) * radiansPerPixel;  // relative angle to camera axis

                if (cameraIsAboveScreen)
                    headY = .5f + (float)(movementScaling * Math.Sin(relativeVerticalAngle + cameraVerticalAngle) * headDist);
                else
                    headY = -.5f + (float)(movementScaling * Math.Sin(relativeVerticalAngle + cameraVerticalAngle) * headDist);
            }
        }
    }
}
