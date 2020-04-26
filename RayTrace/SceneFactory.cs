using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Light light = new Light(new Point(-50, 0, 0), /*0.5f*/ 5000.0f, new Colour(1.0f, 1.0f, 1.0f));
            scene.AddLight(light);

            return scene;
        }

        public static Scene TexturedSpheresDemoScene()
        {
            Scene scene = new Scene();

            Sphere sphere = new Sphere(new Point(-20, 0, 50), 20, Palette.White);
            sphere.Material.Texture = new TextureSquares();
            scene.AddShape(sphere);

            sphere = new Sphere(new Point(20, 0, 50), 20, Palette.White);
            sphere.Material.Texture = new TextureSquares();
            sphere.Material.Texture = new TextureNoise(Palette.White, Palette.Black);
            sphere.Material.Texture = new TextureNoise(Palette.Red, Palette.Green) { Function = TextureNoise.ColourFunction.Blend, Scale = 5 };
            scene.AddShape(sphere);

            Plane plane = new Plane(new Vector(0.0, 1.0, 0.0), new Point(0.0, -30.0, 0.0));
            plane.Material.Texture = new TextureSolid(Palette.White);
            //plane.Material.Texture = new TextureSquares() { Scale = 4 };
            scene.AddShape(plane);

            Light light = new Light(new Point(-50, 0, 0), /*0.5f*/ 5000.0f, new Colour(1.0f, 0.0f, 0.0f));
            scene.AddLight(light);
            light = new Light(new Point(50, 50, 0), /*.85f*/ 5000.0f, new Colour(1.0f, 1.0f, 1.0f));
            scene.AddLight(light);

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
//            scene.AddShape(sphere);


            Box box = new Box(new Point(20, 0, 0), 30, 30, 30);
            box.Material.Transparency = 1.0f;
            box.Material.Reflectivity = 0.0f;
            scene.AddShape(box);


            //Plane plane = new Plane(new Vector(0.0, 0.0, -1.0), new Point(0.0, 0.0, 100.0));
            Plane plane = new Plane(new Vector(0.0, 1.0, 0.0), new Point(0.0, -30.0, 0.0), Palette.Grey);
            plane.Material.Reflectivity = 0.0f;
            //plane.Material.Texture = new TextureSolid(Palette.Blue);
            plane.Material.Texture = new TextureSquares() { Scale = 20 };
            scene.AddShape(plane);

            Light light = new Light(new Point(-50, 0, 0), 3000.0f, new Colour(1.0f, 0.0f, 0.0f));
            scene.AddLight(light);
            light = new Light(new Point(50, 50, 0), 3000.0f, Palette.White);
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


            Light light = new Light(new Point(50, 50, 0), 3000.0f, Palette.White);
            scene.AddLight(light);


            return scene;
        }
    }
}
