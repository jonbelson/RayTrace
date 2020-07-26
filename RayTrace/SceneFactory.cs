using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace RayTrace
{
    class SceneFactory
    {
        public static Scene PhongDemoScene()
        {
            Scene scene = new Scene();

            for (int j = 0; j < 5; j++)
            {
                for (int i = 0; i <= 5; i++)
                {
                    Sphere sphere = new Sphere(new Point(-50 + i * 20, -40 + j * 20, 80), 8, Palette.Green);
                    sphere.Material.ks = i / 5.0;
                    sphere.Material.ns = (j + 1) * (j + 1);
                    scene.AddShape(sphere);
                }
            }
            Light light = new PointLight(new Point(-50, 0, 0), /*0.5f*/ 5000.0f, new Colour(1.0f, 1.0f, 1.0f));
            scene.AddLight(light);

            return scene;
        }

        public static Scene TexturedSpheresDemoScene()
        {
            Scene scene = new Scene();

            Sphere sphere = new Sphere(new Point(-20, 0, 50), 20, Palette.White);
            //sphere.Material.Texture = new TextureSquares();
            sphere.Material.Texture = new TextureSolid(Palette.Black);
            sphere.Material.Transparency = 1.0f;
            scene.AddShape(sphere);

            sphere = new Sphere(new Point(20, 0, 50), 20, Palette.White);
            sphere.Material.Texture = new TextureSquares();
            sphere.Material.Texture = new TextureNoise(Palette.White, Palette.Black);
            sphere.Material.Texture = new TextureNoise(Palette.Red, Palette.Green) { Function = TextureNoise.ColourFunction.Blend, Scale = 5 };
            scene.AddShape(sphere);

            sphere = new Sphere(new Point(0, 0, 90), 20, Palette.Red);
            scene.AddShape(sphere);
            //Box box = new Box(new Point(0, 0, 110), 40);
            //scene.AddShape(box);


            Plane plane = new Plane(new Vector(0.0, 1.0, 0.0), new Point(0.0, -30.0, 0.0));
            plane.Material.Texture = new TextureSolid(Palette.Black);
            plane.Material.Texture = new TextureSquares() { Scale = /*32*/ 8 };
            //plane.Material.Reflectivity = 1.0f;
            scene.AddShape(plane);

            Light light = new PointLight(new Point(-50, 0, 0), /*0.5f*/ 5000.0f, new Colour(1.0f, 0.0f, 0.0f));
            //            scene.AddLight(light);
            light = new PointLight(new Point(50, 50, 0), /*.85f*/ 5000.0f, new Colour(1.0f, 1.0f, 1.0f));
            //            scene.AddLight(light);

            light = new PointLight(new Point(0, 100, 0), /*.85f*/ 5000.0f, new Colour(1.0f, 1.0f, 1.0f));
            scene.AddLight(light);


            //            light = new SpotLight(new Point(0, 100, 0), new Vector(0, -1, 0), 45, 5000, Palette.White);
            //            scene.AddLight(light);

            scene.Background = new Colour(Colors.SkyBlue);

            return scene;
        }

        public static Scene RefractionDemoScene()
        {
            Scene scene = new Scene();

            Sphere sphere = new Sphere(new Point(-20, 0, 50), 20, Palette.Black /* Palette.White*/);
            //sphere.Material.Texture = new TextureSquares();
            sphere.Material.Transparency = 1.0f;
            sphere.Material.Reflectivity = 0.0f;
            //sphere.Material.
            scene.AddShape(sphere);

            sphere = new Sphere(new Point(20, 0, 50), 20, Palette.Black);
            //sphere.Material.Texture = new TextureSquares() { Scale = 10 };
            //sphere.Material.Texture = new TextureNoise(Palette.White, Palette.Black);
            //sphere.Material.Texture = new TextureNoise(Palette.Red, Palette.Green) { Function = TextureNoise.ColourFunction.Blend, Scale = 5 };
            sphere.Material.Transparency = 0.25f;
            sphere.Material.Reflectivity = 0.0f;
            //sphere.Material.Transparency = 0.0f;
            //sphere.Material.Reflectivity = 1.0f;
            scene.AddShape(sphere);


            //Box box = new Box(new Point(20, 0, 0), 30, 30, 30);
            //box.Material.Transparency = 1.0f;
            //box.Material.Reflectivity = 0.0f;
            //scene.AddShape(box);


            Plane plane = new Plane(new Vector(0.0, 0.0, -1.0), new Point(0.0, 0.0, 150.0));
            //Plane plane = new Plane(new Vector(0.0, 1.0, 0.0), new Point(0.0, -30.0, 0.0), Palette.Grey);
            plane.Material.Reflectivity = 0.0f;
            //plane.Material.Texture = new TextureSolid(Palette.Blue);
            plane.Material.Texture = new TextureSquares() { Scale = 20 };
            scene.AddShape(plane);

            Light light = new PointLight(new Point(-50, 0, 0), 3000.0f, new Colour(1.0f, 0.0f, 0.0f));
            scene.AddLight(light);
            light = new PointLight(new Point(50, 50, 0), 3000.0f, Palette.White);
            scene.AddLight(light);

            return scene;
        }

        public static Scene BoxDemoScene()
        {
            Scene scene = new Scene();

            Box box = new Box(new Point(-40, 0, 0), 10, 10, 10);
            //scene.AddShape(box);

            box = new Box(new Point(0, 0, 240), 40);
            box.Material.Texture = new TextureSquares() { Scale = 40 };
            scene.AddShape(box);

            Plane plane = new Plane(new Vector(0.0, 1.0, 0.0), new Point(0.0, -30.0, 0.0), Palette.Grey);
            plane.Material.Reflectivity = 1.0f;
            //plane.Material.Texture = new TextureSolid(Palette.Blue);
            //plane.Material.Texture = new TextureSquares() { Scale = 20 };
            scene.AddShape(plane);


            Light light = new PointLight(new Point(50, 50, 0), 3000.0f, Palette.White);
            scene.AddLight(light);


            return scene;
        }

        public static Scene SphereDemoScene()
        {
            Scene scene = new Scene();

            double floorHeight = -60;
            double radius = 8;
            double bigRadius = 40;
            double sceneWidth = 200;

            Plane floor = new Plane(new Vector(0, 1, 0), new Point(0, floorHeight, 0), new Colour(Colors.LightGray));
            scene.AddShape(floor);
            //            Plane sky = new Plane(new Vector(0, 0, -1), new Point(0, 0, 1000), new Colour(Colors.SkyBlue));
            //            scene.AddShape(sky);

            Random random = new Random();

            Colour[] cols = new Colour[] { new Colour(Colors.LightPink), new Colour(Colors.LightGreen), new Colour(Colors.LightYellow) };

            double spacing = 30;

            for (int i = 0; i < 20; i++)
            {
                for (int j = -10; j < 10; j++)
                {
                    double x = j * spacing;
                    double z = 130 + 5 * spacing * i;

                    x += random.NextDouble() * (spacing / 2 - radius);
                    z += random.NextDouble() * (spacing / 2 - radius);

                    Sphere sphere = new Sphere(new Point(x, floorHeight + radius, z), (int)radius, Palette.Red);

                    int index = random.Next(3);
                    sphere.Material = new Material(cols[index]);

                    scene.AddShape(sphere);
                }
            }

            Sphere sphere2 = new Sphere(new Point(40, floorHeight + bigRadius, 400), (int)(bigRadius), Palette.Black);
            sphere2.Material.Transparency = 0.0f;
            sphere2.Material.Reflectivity = 1.0f;
            scene.AddShape(sphere2);

            Sphere sphere3 = new Sphere(new Point(20, floorHeight + bigRadius, 600), (int)(bigRadius), Palette.Black);
            sphere3.Material.Transparency = 1.0f;
            sphere3.Material.Reflectivity = 0.0f;
            scene.AddShape(sphere3);


            scene.AddLight(new PointLight(new Point(-25, 25, 0), 6000, Palette.White));
            //scene.AddLight(new DistantLight(new Vector(25, 25, 0), 6000, Palette.White));
            scene.AddLight(new PointLight(new Point(25, 25, 0), 6000, Palette.White));

            scene.Background = new Colour(Colors.LightSkyBlue);
            scene.Background = new Colour(Colors.PowderBlue);
            scene.Ambient = new Colour(.3f, .3f, .3f);

            return scene;
        }

        public static Scene SoftLightDemoScene()
        {
            Scene scene = new Scene();

            float floorHeight = -10;
            Plane floor = new Plane(new Vector(0, 1, 0), new Point(0, floorHeight, 0), new Colour(Colors.LightGray));
            scene.AddShape(floor);

            //Box box = new Box(new Point(0, 0, 40), 20.0f);
            //scene.AddShape(box);

            ///*Box*/ box = new Box(new Point(-30, -5, 40), 10.0f);
            //scene.AddShape(box);

            Sphere sphere = new Sphere(new Point(0, 0, 40), 10, Palette.Red);
            scene.AddShape(sphere);


            /*Sphere*/ sphere = new Sphere(new Point(0, floorHeight, 0), 2, Palette.Red);
            scene.AddShape(sphere);

            //Light light = new SpotLight(new Point(-30, 30, 40), new Vector(1, -1, 0, true), 25, 6000, Palette.White);
            Light light = new PointLight(new Point(-50, 50, 40), 6000, Palette.White);
            scene.AddLight(light);

            return scene;
        }

        public static Scene FovDemoScene()
        {
            Scene scene = new Scene();

            float floorHeight = -10;

            Sphere sphere = new Sphere(new Point(-20, 0, 20), 10, new Colour(Colors.Orange));
            //scene.AddShape(sphere);

            sphere = new Sphere(new Point(-10, 0, 30), 10, Palette.Red);
            scene.AddShape(sphere);

            sphere = new Sphere(new Point(10, 0, 70), 10, Palette.Green);
            scene.AddShape(sphere);

            sphere = new Sphere(new Point(40, 0, 110), 10, Palette.Blue);
            scene.AddShape(sphere);

            sphere = new Sphere(new Point(80, 0, 150), 10, new Colour(Colors.Yellow));
            scene.AddShape(sphere);

            sphere = new Sphere(new Point(0, floorHeight, 1), 5, Palette.Grey);
            scene.AddShape(sphere);

            Plane floor = new Plane(new Vector(0, 1, 0), new Point(0, floorHeight, 0), new Colour(Colors.LightGray));
            //floor.Material.Texture = new TextureSquares(Palette.Black, Palette.White);
            //floor.Material.Reflectivity = 0.1f;
            scene.AddShape(floor);

            Light light = new PointLight(new Point(-50, 50, 40), 6000, Palette.White);
            scene.AddLight(light);

            light = new PointLight(new Point(0, 50, 0), 3000, Palette.White);
            scene.AddLight(light);

            return scene;
        }

        public static Scene ModelScene()
        {
            Scene scene = new Scene();

            //Model model = new Model(new Point(0.0, 0.0, 0.0), modelData);
            Model model = ModelFactory.Cube();
            //Model model = ModelFactory.Octahedron();
            model = ModelFactory.Tetahedron();
            scene.AddShape(model);

            float floorHeight = -10;
            Plane floor = new Plane(new Vector(0, 1, 0), new Point(0, floorHeight, 0), new Colour(Colors.LightGray));
            //floor.Material.Texture = new TextureSquares(Palette.Black, Palette.White);
            //floor.Material.Reflectivity = 0.1f;
            scene.AddShape(floor);

            Light light = new PointLight(new Point(-50, 50, 40), 6000, Palette.White);
            scene.AddLight(light);

            return scene;
        }

        public static Scene ObjScene()
        {
            Scene scene = new Scene();

            ModelImporter importer = new ModelImporter();
            ModelData modelData = importer.ImportObj(@"C:\Users\Jon\Documents\Projects\RayTrace\RayTrace\Data\pbr-low-poly-fox-character");

            Model model = new Model(new Point(0.0, 0.0, 10.0), modelData);
            model.Material.Texture = new TextureSolid(Palette.White);
            //model = ModelFactory.Octahedron();
            // model = ModelFactory.Cube();
            //model = ModelFactory.Tetahedron();
            scene.AddShape(model);


            double floorHeight = model.MinY;
            Plane floor = new Plane(new Vector(0, 1, 0), new Point(0, floorHeight, 0), new Colour(Colors.LightGray));
            //floor.Material.Texture = new TextureSquares(Palette.Black, Palette.White);
            //floor.Material.Reflectivity = 0.1f;
            scene.AddShape(floor);

            Light light = new PointLight(new Point(-50, 50, 10), 6000, Palette.White);
            scene.AddLight(light);

            light = new PointLight(new Point(10, 50, 0), 3000, Palette.White);
            scene.AddLight(light);

            return scene;
        }
    }
}
