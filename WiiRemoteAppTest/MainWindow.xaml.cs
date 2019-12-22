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
        public MainWindow()
        {
            Wiimote wm = new Wiimote();
            modelImporter = new ModelImporter();
            modelVisual = new ModelVisual3D();
            Model3DGroup models = new Model3DGroup();
            InitializeComponent();
            Viewport3D viewport3D1 = new Viewport3D();  // screw XAML 
            // looks like the 3D coord system makes actual sense instead of WPFs usual wierdness with 0,0 being at the top left
            PerspectiveCamera camera = new PerspectiveCamera();
            camera.Position = new Point3D(0, 0, 2);

            // Specify the direction that the camera is pointing.
            camera.LookDirection = new Vector3D(0, 0, -2);

            // Define camera's horizontal field of view in degrees.
            camera.FieldOfView = 60;

            // Asign the camera to the viewport
            viewport3D1.Camera = camera;

            // in typical WPF fashion, this is an absolute mess
            AmbientLight light = new AmbientLight(Color.FromRgb(255, 255, 255));
            models.Children.Add(light);
            mainGrid.Children.Add(viewport3D1);
            Model3DGroup group = modelImporter.Load("C:\\Users\\night\\source\\repos\\WiiRemoteAppTest\\WiiRemoteAppTest\\cube.obj");
            group.Children.Add(light);
            modelVisual.Content = group;
            viewport3D1.Children.Add(modelVisual);
        }

        public void Window_Loaded(object sender, RoutedEventArgs e)
        {
           
        }

        private void RotationButton_Click(object sender, RoutedEventArgs e)
        {
            camera.Position = new Point3D(0, 0, 2);
            modelVisual.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 2, 1), ));
        }
    }
}
