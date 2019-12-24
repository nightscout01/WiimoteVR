using HelixToolkit.Wpf;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
        AmbientLight light;
        Point2D[] wiimotePointsNormalized = new Point2D[4];

        // can be used to find the absolute rotation of an object around the 3 cardinal axes
        //double rotationX = Vector3D.AngleBetween(new Vector3D(1, 0, 0), yourMatrix3D.Transform(new Vector3D(1, 0, 0)));
        //double rotationY = Vector3D.AngleBetween(new Vector3D(0, 1, 0), yourMatrix3D.Transform(new Vector3D(0, 1, 0)));
        //double rotationZ = Vector3D.AngleBetween(new Vector3D(0, 0, 1), yourMatrix3D.Transform(new Vector3D(0, 0, 1)));

        private const float radiansPerPixel = (float)(Math.PI / 4) / 1024.0f;  // 45 degree field of view with a 1024x768 camera
        private const float dotDistanceInMM = 8.5f * 25.4f;  // width of the wii sensor bar
        private const float movementScaling = 1f;

        private float headX = 0;
        private float headY = 0;
        private float headDist = 0;
        private bool cameraIsAboveScreen = false;  // has no affect until zeroing and then is set automatically.
        private float screenHeightinMM = 350;//20 * 25.4f;
        private float cameraVerticalAngle = 0;  // begins assuming the camera is point straight forward
        private float relativeVerticalAngle = 0;  // current head position view angle
        private Point3D lastPosition = new Point3D();

        public MainWindow()
        {
            camera = new PerspectiveCamera(new Point3D(0,0,0),new Vector3D(0,0,1),new Vector3D(0,1,0),45);  // create a new camera with the given transformation matrices
            Wiimote wm = new Wiimote();
            wm.Connect();  // connect to first wii remote found

            wm.WiimoteChanged += Wiimote_Update;  // register the event function
            wm.SetReportType(InputReport.IRAccel, true);  // report back only IR and Accelerometer data, and report it continiously.
            wm.SetLEDs(1);

            modelImporter = new ModelImporter();  // initialize some fields, and the model3D group
            modelVisual = new ModelVisual3D();
            Model3DGroup models = new Model3DGroup();




            InitializeComponent();
            // Viewport3D viewport3D1 = new Viewport3D();  // screw XAML 
            // looks like the 3D coord system makes actual sense instead of WPFs usual wierdness with 0,0 being at the top left
            viewport3D1.Camera = camera;
            // Asign the camera to the viewport


            // in typical WPF fashion, this is an absolute mess
            light = new AmbientLight(Color.FromRgb(255, 255, 255));
            // models.Children.Add(light);
            //   mainGrid.Children.Add(viewport3D1);
            Model3DGroup group = modelImporter.Load("C:\\Users\\night\\source\\repos\\WiiRemoteAppTest\\WiiRemoteAppTest\\cube.obj");
            group.Children.Add(light);
         //   group.Transform = new TranslateTransform3D(-0.5, 0, 0);
            modelVisual.Content = group;
            viewport3D1.Children.Add(modelVisual);
            //viewport3D1.
        }

        public void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;
            this.KeyDown += new KeyEventHandler(MainWindow_KeyDown);
        }

        public void CalibrateWiimote()
        {
            //zeros the head position and computes the camera tilt
            double angle = Math.Acos(.5 / headDist) - Math.PI / 2;//angle of head to screen
            if (!cameraIsAboveScreen)
                angle = -angle;
            cameraVerticalAngle = (float)angle; // (float)((angle - relativeVerticalAngle));//absolute camera angle 
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs args)
        {
            if(args.Key == Key.Space)
            {
                CalibrateWiimote();
            }
            if(args.Key == Key.A)
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

        private void RotationButton_Click(object sender, RoutedEventArgs e)
        {
            //camera.Position = new Point3D(0, 0, 2);

        }

        private void Wiimote_Update(object sender, WiimoteChangedEventArgs args)
        {
            //if (args.WiimoteState.ButtonState.A)
            //{
            //    Random rand = new Random();
            //    Application.Current.Dispatcher.Invoke(() => { 
            //        light = new AmbientLight(Color.FromRgb((byte)rand.Next(256), (byte)rand.Next(256), (byte)rand.Next(256))); 
            //    });
            //}
            ParseWiimoteData(args.WiimoteState);
           
            if(Application.Current == null)
            {
                return;
            }
            Application.Current.Dispatcher.Invoke(() => {
                HeadPositionTextBox.Text = "HeadX = " + headX + " HeadY = " + headY + " HeadDist = " + headDist;
                Point3D newHeadPos = new Point3D(headX, headY, -headDist);
                Vector3D cameraLookDir = camera.LookDirection;
                Point3D pointToLookAt = new Point3D(headX, headY, 0);
                cameraLookDir.Normalize();
                Point3D lookDirection = pointToLookAt;//(Point3D) (cameraLookDir * -newHeadPos.Z);
                camera.Position += (newHeadPos - lastPosition);
                //Console.WriteLine((newHeadPos - lastPosition));
              //  camera.LookAt(lookDirection, 0);
                CameraPosTexrbox.Text = camera.Position.ToString();
                lastPosition = newHeadPos;
                camera.FieldOfView = 107 - 0.1944 * headDist * screenHeightinMM / 10;
            });

            //Vector firstPoint = new Vector();
            //Vector secondPoint = new Vector();
            //int numVisible = 0;
            /*
             * 
             * 
                             * Vector2 firstPoint = Vector2();
                    Vector2 secondPoint = Vector2();
                    int numvisible = 0;

                    for (int index = 0; index < 4; index++) {
                        if (mWiiMote.IR.Dot[index].bFound) {
                            if (numvisible == 0) {
                                firstPoint.x = mWiiMote.IR.Dot[index].RawX;
                                firstPoint.y = mWiiMote.IR.Dot[index].RawY;
                                numvisible = 1;
                            }
                            else if (numvisible == 1) {
                                secondPoint.x = mWiiMote.IR.Dot[index].RawX;
                                secondPoint.y = mWiiMote.IR.Dot[index].RawY;
                                numvisible = 2;
                                break;
                            }
                        }
                    }
                */
            //  SetCameraMatrices(camera);
            //if (Application.Current == null)  // just to eliminate exceptions when closing the program
            //{
            //    return;
            //}
            //Application.Current.Dispatcher.Invoke(() =>
            //{
            //    Transform3DGroup transformGroup = new Transform3DGroup();
            //    RotateTransform3D XRotateTransform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 360 * headX));
            //    RotateTransform3D YRotateTransform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 360 * headY));
            //    Transform3D transform = new TranslateTransform3D(0, 0, 0);
            //    transformGroup.Children.Add(XRotateTransform);
            //    transformGroup.Children.Add(YRotateTransform);
            //    transformGroup.Children.Add(transform);
            //    ScaleTransform3D transform3D = new ScaleTransform3D(.5, .5, .5);
            //    modelVisual.Transform = transformGroup;
            //    camera.Position = new Point3D(headX, headY, headDist);
            //});

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
                // wiiCursor1.isDown = true;
                firstPoint.X = wiimoteState.IRState.IRSensors[0].RawPosition.X;
                firstPoint.Y = wiimoteState.IRState.IRSensors[0].RawPosition.Y;
                numvisible = 1;
            }
            else
            {   //not visible
                // wiiCursor1.isDown = false;
            }
            if (wiimoteState.IRState.IRSensors[1].Found)
            {
                wiimotePointsNormalized[1].X = 1.0f - wiimoteState.IRState.IRSensors[1].RawPosition.X / 768.0f;
                wiimotePointsNormalized[1].Y = wiimoteState.IRState.IRSensors[1].RawPosition.Y / 768.0f;
                //    wiiCursor2.isDown = true;
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
            {//not visible
             //  wiiCursor2.isDown = false;
            }
            if (wiimoteState.IRState.IRSensors[2].Found)
            {
                wiimotePointsNormalized[2].X = 1.0f - wiimoteState.IRState.IRSensors[2].RawPosition.X / 768.0f;
                wiimotePointsNormalized[2].Y = wiimoteState.IRState.IRSensors[2].RawPosition.Y / 768.0f;
                //   wiiCursor3.isDown = true;
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
            {//not visible
             // wiiCursor3.isDown = false;
            }
            if (wiimoteState.IRState.IRSensors[3].Found)
            {
                wiimotePointsNormalized[3].X = 1.0f - wiimoteState.IRState.IRSensors[3].RawPosition.X / 768.0f;
                wiimotePointsNormalized[3].Y = wiimoteState.IRState.IRSensors[3].RawPosition.Y / 768.0f;
                //  wiiCursor4.isDown = true;
                if (numvisible == 1)
                {
                    secondPoint.X = wiimoteState.IRState.IRSensors[3].RawPosition.X;
                    secondPoint.Y = wiimoteState.IRState.IRSensors[3].RawPosition.Y;
                    numvisible = 2;
                }
            }
            else
            {//not visible
             // wiiCursor4.isDown = false;
            }

            if (numvisible == 2)
            {
                float dx = firstPoint.X - secondPoint.X;
                float dy = firstPoint.Y - secondPoint.Y;
                float pointDist = (float)Math.Sqrt(dx * dx + dy * dy);

                float angle = radiansPerPixel * pointDist / 2;
                //in units of screen hieght since the box is a unit cube and box height is 1
                headDist = movementScaling * (float)((dotDistanceInMM / 2) / Math.Tan(angle)) / screenHeightinMM;


                float avgX = (firstPoint.X + secondPoint.X) / 2.0f;
                float avgY = (firstPoint.Y + secondPoint.Y) / 2.0f;

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
    class Point2D
    {
        public float x = 0.0f;
        public float y = 0.0f;
        public void set(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
