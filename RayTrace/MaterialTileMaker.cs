using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace RayTrace
{
    public class MaterialTile
    {
        [NotifyParentProperty(true)]
        public string Name { get; set; }
        public Guid Guid { get; set; }
        public Material Material { get; set; }
        public Bitmap Bitmap { get; set; }

        public WriteableBitmap WriteableBitmap { get; set; }

        public MaterialTile()
        {

        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class MaterialTileMaker
    {
        private Scene scene;
        private Sphere sphere;

        public int Size { get; set; } = 200;

        public MaterialTileMaker()
        {
            scene = new Scene();
            //_scene.Ambient = Palette.Blue;
            //_scene.Background = Palette.Red;
            scene.Camera.Pos = new Point(0.0, 0.0, -0.0);
            scene.Camera.Target = new Point(0.0, 0.0, 1.0);

            sphere = new Sphere(new Point(0.0, 0.0, 4.0), 2, Palette.White);
            scene.AddShape(sphere);

            scene.AddLight(new PointLight(new Point(-6.0, 4.0, 0.0), 500, Palette.White));

            Plane plane = new Plane(new Vector(0.0, 0.0, -1.0), new Point(0.0, 0.0, 20.0));
            //plane.Material.Texture = new TextureSquares(new Colour(System.Windows.Media.Colors.DarkGray), new Colour(System.Windows.Media.Colors.Gray)) { Scale = 50 };

            Material matSquares = new Material();
            matSquares.Type = MaterialType.Diffuse;
            matSquares.PhongShininess = 1.0;
            matSquares.Texture = new TextureSquares(new Colour(System.Windows.Media.Colors.DarkGray), new Colour(System.Windows.Media.Colors.Gray)) { Scale = 500 };
            plane.Material = matSquares;

            scene.AddShape(plane);

            scene.Camera.UpdateMatrix();    // XXX Needed?
            scene.CreateCaches();
        }

        public MaterialTile MakeTile(Material material)
        {
            sphere.Material = material;

            Renderer renderer = new();
            renderer.UseAmbient = true;
            renderer.UseShadows = false;
            //scene.Ambient = new Colour(0.5f, 0.5f, 0.5f);
            Canvas canvas = new(Size, Size, Palette.Red);
            renderer.Render(scene, canvas);

            MaterialTile materialTile = new();

            materialTile.Name = material.Name;
            materialTile.Guid = material.Guid;
            materialTile.Bitmap = canvas.GetAsBitmap();
            materialTile.Material = material;

            //string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            //canvas.Save(documents + "/" + material.Guid.ToString());

            return materialTile;
        }
    }
}
