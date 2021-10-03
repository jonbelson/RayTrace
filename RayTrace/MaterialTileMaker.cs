using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
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

        private async Task<bool> Render(Renderer renderer, Canvas canvas)
        {
            await DoRender(renderer, canvas);

            return true;
        }

        private Task<bool> DoRender(Renderer renderer, Canvas canvas)
        {
            Task task1 = Task.Factory.StartNew(() =>
            {
                renderer.Render(scene, canvas, 0, 0, Size/2, Size/2);
            });
            Task task2 = Task.Factory.StartNew(() =>
            {
                renderer.Render(scene, canvas, Size/2, 0, Size / 2, Size / 2);
            });
            Task task3 = Task.Factory.StartNew(() =>
            {
                renderer.Render(scene, canvas, 0, Size/2, Size / 2, Size / 2);
            });
            Task task4 = Task.Factory.StartNew(() =>
            {
                renderer.Render(scene, canvas, Size/2, Size/2, Size / 2, Size / 2);
            });

            Task[] tasks = new Task[4] { task1, task2, task3, task4 };

            Task.WaitAll(tasks);

            return Task.FromResult(true);
        }

        public MaterialTileMaker()
        {
            scene = new Scene();
            //_scene.Ambient = Palette.Blue;
            //_scene.Background = Palette.Red;
            scene.Camera.Pos = new Point(0, 0, 0);
            scene.Camera.Target = new Point(0, 0, 1);

            sphere = new Sphere(new Point(0, 0, 5), 2, Palette.White);
            scene.AddShape(sphere);

            scene.AddLight(new PointLight(new Point(-6, 4, 0), 500, Palette.White));

            //Plane plane = new Plane(new Vector(0, 0, -1), new Point(0, 0, 20));
            //plane.Material.Texture = new TextureSquares(new Colour(System.Windows.Media.Colors.DarkGray), new Colour(System.Windows.Media.Colors.Gray)) { Scale = 50 };
            Disc disc = new Disc(new Point(0, 0, 20), new Vector(0, 0, -1), 16);

            Material matSquares = new Material();
            matSquares.Type = MaterialType.Diffuse;
            matSquares.PhongShininess = 1.0;
            matSquares.Texture = new TextureSquares(new Colour(System.Windows.Media.Colors.DarkGray), new Colour(System.Windows.Media.Colors.Gray)) { Scale = 500 };
            disc.Material = matSquares;

            scene.AddShape(disc);

            scene.AddMaterial(matSquares);

            scene.Skydome = new SkydomeBitmap("Data\\skydomes\\immenstadter_horn_1024.jpg");

            scene.Camera.UpdateMatrix();    // XXX Needed?
        }

//        public async Task<MaterialTile> MakeTile(Material material)
        public MaterialTile MakeTile(Material material)
        {
            sphere.Material = material;
            scene.AddMaterial(sphere.Material);
            scene.CreateCaches();

            Renderer renderer = new();
            renderer.UseAmbient = true;
            renderer.UseShadows = false;
            //scene.Ambient = new Colour(0.5f, 0.5f, 0.5f);
            renderer.NumBRDFSamples = 100;
            renderer.RecursionDepth = 2;
            Canvas canvas = new(Size, Size, Palette.Red);
            //renderer.Render(scene, canvas);

            //await DoRender(renderer, canvas);

            bool b = DoRender(renderer, canvas).Result;

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
