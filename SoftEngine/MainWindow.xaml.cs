using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpDX;

namespace SoftEngine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {

        public WriteableBitmap Bmp { get; set; }

        private Device device;
        private Mesh[] meshes;
        private Camera camera = new Camera();
        private DateTime previousDate;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += LoadedProc;
        }

        private async void LoadedProc(object sender, EventArgs args)
        {
            Bmp = new WriteableBitmap((int)Width, (int) Height, 96, 96, PixelFormats.Pbgra32, null);
            device = new Device(Bmp);

            var image = new Image { Source = Bmp };
            XCanvas.Children.Add(image);

            meshes = await device.LoadJsonFileAsync("monkey.babylon");
            
            camera.Position = new Vector3(0, 0, 10.0f);
            camera.Target = Vector3.Zero;

            CompositionTarget.Rendering += Rendering;
        }

        void Rendering(object sender, object e)
        {
            var now = DateTime.Now;
            var currentFps = 1000.0 / (now - previousDate).TotalMilliseconds;
            previousDate = now;

            Fps.Content = $"{currentFps:0.00} fps";
            
            device.Clear(0, 0, 0, 255);
            foreach (var mesh in meshes)
            {
                mesh.Rotation = new Vector3(mesh.Rotation.X + 0.01f, mesh.Rotation.Y + 0.01f, mesh.Rotation.Z + 0.01f);
                device.Render(camera, mesh);
            }
            
            device.Present();
        }
    }
}