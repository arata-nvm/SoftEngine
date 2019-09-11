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
        Mesh mesh = new Mesh("Cube", 8);
        Camera camera = new Camera();

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            Bmp = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Pbgra32, null);
            device = new Device(Bmp);
            
            Image image = new Image();
            image.Source = Bmp;
            XCanvas.Children.Add(image);
            
            mesh.Vertices[0] = new Vector3(-1, 1, 1);
            mesh.Vertices[1] = new Vector3(1, 1, 1);
            mesh.Vertices[2] = new Vector3(-1, -1, 1);
            mesh.Vertices[3] = new Vector3(-1, -1, -1);
            mesh.Vertices[4] = new Vector3(-1, 1, -1);
            mesh.Vertices[5] = new Vector3(1, 1, -1);
            mesh.Vertices[6] = new Vector3(1, -1, 1);
            mesh.Vertices[7] = new Vector3(1, -1, -1);
            
            camera.Position = new Vector3(0, 0, 10.0f);
            camera.Target = Vector3.Zero;

            CompositionTarget.Rendering += Rendering;
        }

        void Rendering(object sender, object e)
        {
            device.Clear(0, 0, 0, 255);
            mesh.Rotation = new Vector3(mesh.Rotation.X + 0.01f, mesh.Rotation.Y + 0.01f, mesh.Rotation.Z + 0.01f);

            device.Render(camera, mesh);
            device.Present();
        }
    }
}