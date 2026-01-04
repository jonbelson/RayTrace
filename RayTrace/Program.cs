using System;

namespace RayTrace
{
    class MainClass
    {
        public static void Main2(string[] args)
        {
            //Console.WriteLine("Hello World!");

            Point p1 = new Point(1, 1, 1);
            Point p2 = new Point(2, 2, 2);

            Vector v = p1 - p2;
            Vector v2 = p1.Difference(p2);

            Canvas canvas = new Canvas(1000, 1000, SPalette.Black);
  //          Canvas canvas = new Canvas(400, 400, Palette.Black);
            //            Canvas canvas = new Canvas(100, 100, Palette.Black);
            //canvas.Background = 0xff0000;
            //canvas.Clear();

            Scene scene = new Scene();
            /*
            // Reflecting spheres with square texture.
                        Sphere sphere = new Sphere(new Point(-20, 0, 50), 20, Palette.White);
                        sphere.Material = new TextureSquares();
                        scene.AddShape(sphere);

                        //Sphere sphere = new Sphere(new Point(-20, 0, 50), 20, Palette.Red);
                        //scene.AddShape(sphere);
                        sphere = new Sphere(new Point(20, 0, 50), 20, Palette.White);
            //            sphere.Material = new TextureSquares();
                        sphere.Material = new TextureSquares();
                        scene.AddShape(sphere);

                        Light light = new Light(new Point(-50, 0, 0), 0.5f, new Colour(1.0f, 0.0f, 0.0f));
                        //Light light = new Light(new Point(-50, 0, 0), 0.5f, new Colour(1.0f, 1.0f, 1.0f));
                        scene.AddLight(light);
                        light = new Light(new Point(50, 50, 0), .85f, new Colour(1.0f, 1.0f, 1.0f));
                        scene.AddLight(light);

                        */

            /*
             // Planets
                        Sphere sphere = new Sphere(new Point(-30, 0, 50), 10, Palette.White);
                        sphere.Material = new TextureBitmap("earth2.jpg");
            //            sphere.Material = new TextureBitmap("earth.jpg");
            //            sphere.Material = new TextureBitmap("jupiter.jpg");
                        scene.AddShape(sphere);
                        sphere = new Sphere(new Point(10, 0, 150), 40, Palette.White);
                        sphere.Material = new TextureBitmap("jupiter.jpg");
                        scene.AddShape(sphere);

                        //            Light light = new Light(new Point(-50, 0, 0), 0.5f, new Colour(1.0f, 0.0f, 0.0f));
                        Light light = new Light(new Point(-50, 0, 0), 0.5f, new Colour(1.0f, 1.0f, 1.0f));
                        //scene.AddLight(light);
                        light = new Light(new Point(50, 50, 0), 0.5f, new Colour(1.0f, 1.0f, 1.0f));
                        scene.AddLight(light);
            */

            ///*
            ///
            for (int j = 0; j < 5; j++)
            {
                for (int i = 0; i <= 5; i++)
                {

                    Sphere sphere = new Sphere(scene, new Point(-50 + i * 20, -40+j*20, 80), 8, Palette.Green);
                    sphere.Material.PhongSpecular = i / 5.0f;
                    sphere.Material.PhongShininess = (j + 1) * (j + 1);
                    scene.AddShape(sphere);
                }
            }
                    Light light = new PointLight(scene, new Point(-50, 0, 0), 0.5f, new Colour(1.0f, 1.0f, 1.0f));
                    //scene.AddLight(light);

            //*/

            Renderer renderer = new Renderer();
            //renderer.UseLighting = false;
            //renderer.UsePhong = false;
            renderer.UseReflections = false;
//            renderer.Render(scene, canvas);

            canvas.Save("Canvas.tiff");

        }
    }
}
