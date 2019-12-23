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
        private readonly MatrixCamera camera;
        AmbientLight light;

        // can be used to find the absolute rotation of an object around the 3 cardinal axes
        //double rotationX = Vector3D.AngleBetween(new Vector3D(1, 0, 0), yourMatrix3D.Transform(new Vector3D(1, 0, 0)));
        //double rotationY = Vector3D.AngleBetween(new Vector3D(0, 1, 0), yourMatrix3D.Transform(new Vector3D(0, 1, 0)));
        //double rotationZ = Vector3D.AngleBetween(new Vector3D(0, 0, 1), yourMatrix3D.Transform(new Vector3D(0, 0, 1)));

        private const float radiansPerPixel = (float)(Math.PI / 4) / 1024.0f;  // 45 degree field of view with a 1024x768 camera
        private const float dotDistanceInMM = 8.5f * 25.4f;  // width of the wii sensor bar
        private const float movementScaling = 1f;

        private float headX = 0;
        private float headY = 0;
        private float headDist = 2;
        private bool cameraIsAboveScreen = false;  // has no affect until zeroing and then is set automatically.
        private float screenHeightinMM = 20 * 25.4f;
        private float cameraVerticaleAngle = 0;  // begins assuming the camera is point straight forward
        private float relativeVerticalAngle = 0;  // current head position view angle

        public MainWindow()
        {
            Wiimote wm = new Wiimote();
            wm.Connect();  // connect to first wii remote found

            wm.WiimoteChanged += Wiimote_Update;  // register the event function
            wm.SetReportType(InputReport.IRAccel, true);  // report back only IR and Accelerometer data, and report it continiously.
            wm.SetLEDs(1);

            modelImporter = new ModelImporter();  // initialize some fields, and the model3D group
            modelVisual = new ModelVisual3D();
            Model3DGroup models = new Model3DGroup();


            InitializeComponent();
            Viewport3D viewport3D1 = new Viewport3D();  // screw XAML 
                                                        // looks like the 3D coord system makes actual sense instead of WPFs usual wierdness with 0,0 being at the top left
            camera = new MatrixCamera();
            // camera.ViewMatrix

            // We use a 4x4 matrix for these so we can do both transformation (scaling,rotation,etc..) and translation
            // I think I get it???
            // We gotta hope that this is basically the same idea from direct3D or else I'm gonna have to figure out some m a t h
            //Matrix3D vmatrix = new Matrix3D(
            //    headX,headY,headDist,0,  // row 1
            //    headX,headY,0,0,  // row 2
            //    1,0,0,0,  // row 3
            //    0,0,0,0);  // row 4 is just offsets (I hope)
            // nope it's math time

            Vector3D cameraPosition = new Vector3D(headX, headY, headDist);
            Vector3D cameraTarget = new Vector3D(headX, headY, 0);
            Vector3D cameraUpVector = new Vector3D(0, 1, 0);

            // if we're trying to recreate the D3D version of this, we build the viewmatrix as so:
            // apologies for the gross formatting, I'll either fix or remove this if I get around to it
            /*
             * 
                zaxis = normal(cameraTarget - cameraPosition)
                xaxis = normal(cross(cameraUpVector, zaxis))
                yaxis = cross(zaxis, xaxis)

                 xaxis.x           yaxis.x           zaxis.x          0
                 xaxis.y           yaxis.y           zaxis.y          0
                 xaxis.z           yaxis.z           zaxis.z          0
                -dot(xaxis, cameraPosition)  -dot(yaxis, cameraPosition)  -dot(zaxis, cameraPosition)  1
              */


            Vector3D zAxis = (cameraTarget - cameraPosition);
            zAxis.Normalize();  // normalize the Z-Axis vector

            Vector3D xAxis = Vector3D.CrossProduct(cameraUpVector, zAxis);
            xAxis.Normalize();  // normalize the X-Axis vector

            Vector3D yAxis = Vector3D.CrossProduct(zAxis, xAxis);

            // public Matrix3D (double m11, double m12, double m13, double m14, double m21, double m22, double m23, double m24, double m31, 
            // double m32, double m33, double m34, double offsetX, double offsetY, double offsetZ, double m44);

            // first we need to create our viewMatrix, in the same way Johhny did it , so therefore, we have to do it the same way as 
            // Direct3D

            Matrix3D viewMatrix = new Matrix3D(
                xAxis.X, yAxis.X, zAxis.X, 0,
                xAxis.Y, yAxis.Y, zAxis.Y, 0,
                xAxis.Z, yAxis.Z, zAxis.Z, 0,
                -Vector3D.DotProduct(xAxis, cameraPosition), -Vector3D.DotProduct(yAxis, cameraPosition),  // this down here is 1 row
                    -Vector3D.DotProduct(zAxis, cameraPosition), 1);  // what a nightmare


            // now we need to create our projection matrix
            // it is also a Matrix3D struct, formatted like below
            float nearPlane = 0.5f;
            float screenAspect = 16 / 9;  // oh boy using float values in 2019 
            float left = nearPlane * (-.5f * screenAspect + headX) / headDist;

            /*
             2*znearPlane/(right-left)  0                           0                                            0
             0                          2*znearPlane/(top-bottom)   0                                            0
             (left+right)/(left-right)  (top+bottom)/(bottom-top)   zfarPlane/(zfarPlane-znearPlane)             1
             0                          0                           znearPlane*zfarPlane/(znearPlane-zfarPlane)  0 
             */

            // in typical WPF fashion, this is an absolute mess
            light = new AmbientLight(Color.FromRgb(255, 255, 255));
            // models.Children.Add(light);
            mainGrid.Children.Add(viewport3D1);
            Model3DGroup group = modelImporter.Load("C:\\Users\\night\\source\\repos\\WiiRemoteAppTest\\WiiRemoteAppTest\\cube.obj");
            group.Children.Add(light);
            group.Transform = new TranslateTransform3D(-0.5, 0, 0);
            modelVisual.Content = group;
            viewport3D1.Children.Add(modelVisual);
        }
        // public MainWindow()
        // {
        //    


        //     modelImporter = new ModelImporter();  // initialize some fields, and the model3D group
        //     modelVisual = new ModelVisual3D();
        //     Model3DGroup models = new Model3DGroup();


        //     InitializeComponent();
        //     Viewport3D viewport3D1 = new Viewport3D();  // screw XAML 
        //     // looks like the 3D coord system makes actual sense instead of WPFs usual wierdness with 0,0 being at the top left
        //     camera = new MatrixCamera();
        //     // camera.ViewMatrix

        //     // We use a 4x4 matrix for these so we can do both transformation (scaling,rotation,etc..) and translation
        //     // I think I get it???
        //     // We gotta hope that this is basically the same idea from direct3D or else I'm gonna have to figure out some m a t h
        //     //Matrix3D vmatrix = new Matrix3D(
        //     //    headX,headY,headDist,0,  // row 1
        //     //    headX,headY,0,0,  // row 2
        //     //    1,0,0,0,  // row 3
        //     //    0,0,0,0);  // row 4 is just offsets (I hope)
        //     // nope it's math time

        //     Vector3D cameraPosition = new Vector3D(headX, headY, headDist);
        //     Vector3D cameraTarget = new Vector3D(headX, headY, 0);
        //     Vector3D cameraUpVector = new Vector3D(0, 1, 0);

        //     float nearPlane = .05f;
        //     Matrix3D pmatrix = new Matrix3D();
        //   //  camera.Position = new Point3D(0, 0, 3);

        //     // Specify the direction that the camera is pointing.
        //.//     camera.LookDirection = new Vector3D(0, 0, -1);

        //     // Define camera's horizontal field of view in degrees.
        // //    camera.FieldOfView = 60;

        //     // Asign the camera to the viewport
        //     viewport3D1.Camera = camera;

        //     // in typical WPF fashion, this is an absolute mess
        //     light = new AmbientLight(Color.FromRgb(255, 255, 255));
        //    // models.Children.Add(light);
        //     mainGrid.Children.Add(viewport3D1);
        //     Model3DGroup group = modelImporter.Load("C:\\Users\\night\\source\\repos\\WiiRemoteAppTest\\WiiRemoteAppTest\\cube.obj");
        //     group.Children.Add(light);
        //     group.Transform = new TranslateTransform3D(-0.5, 0, 0);
        //     modelVisual.Content = group;
        //     viewport3D1.Children.Add(modelVisual);
        // }

        public void Window_Loaded(object sender, RoutedEventArgs e)
        {

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
            if (Application.Current == null)  // just to eliminate exceptions when closing the program
            {
                return;
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                //Transform3DGroup transformGroup = new Transform3DGroup();
                //RotateTransform3D XRotateTransform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 360 * headX));
                //RotateTransform3D YRotateTransform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 360 * headY));

                //transformGroup.Children.Add(XRotateTransform);
                //transformGroup.Children.Add(YRotateTransform);
                // modelVisual.Transform = transformGroup;
                //  camera.Position = new Point3D(headX, headY, headDist);
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


                // should calculate based on distance

                headX = (float)(movementScaling * Math.Sin(radiansPerPixel * (avgX - 512)) * headDist);

                relativeVerticalAngle = (avgY - 384) * radiansPerPixel;//relative angle to camera axis

                if (cameraIsAboveScreen)
                    headY = .5f + (float)(movementScaling * Math.Sin(relativeVerticalAngle + cameraVerticaleAngle) * headDist);
                else
                    headY = -.5f + (float)(movementScaling * Math.Sin(relativeVerticalAngle + cameraVerticaleAngle) * headDist);
            }
        }
    }
}
