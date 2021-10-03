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
        public static Scene EmptyScene()
        {
            Scene scene = new();

            Material mat = new Material() { Name = "Gradient" };
            mat.Texture = new TextureGradient() { Gradient = TextureGradient.GradientType.Radial };

            scene.AddMaterial(mat);

            Sphere sphere = new Sphere(new Point(0, 0, 00), 2, Palette.Red);
            sphere.Material = mat;
            scene.AddShape(sphere);

            scene.Camera.Pos = new Point(3, 5, -16);
            scene.Camera.Target = new Point(0, 0, 0);

            //scene.AddLight(new PointLight(new Point(0, 10, 0), 1000, Palette.White));

            return scene;
        }

        public static Scene PhongDemoScene()
        {
            Scene scene = new Scene();

            double[] n = { 2, 10, 50, 250, 1250 };
            double[] ks = { 0.02, 0.04, 0.08, 0.1, 0.15, 0.2 };

            for (int j = 0; j < 5; j++)
            {
                for (int i = 0; i <= 5; i++)
                {
                    Sphere sphere = new Sphere(new Point(-50 + i * 20, -40 + j * 20, 80), 8, Palette.Green);
                    sphere.Material.Type = MaterialType.Phong;
                    sphere.Material.PhongDiffuse = 0.95;
                    sphere.Material.PhongSpecular = 2 * ks[i];  // i / 5.0;
                    sphere.Material.PhongShininess = n[j];  // (j + 1) * (j + 1);
                    scene.AddShape(sphere);
                }
            }
            //Light light = new PointLight(new Point(-50, 0, 0), /*0.5f*/ 50000.0f, new Colour(1.0f, 1.0f, 1.0f));
            Light light = new DistantLight(new Vector(1, 0, 1), 2, Palette.White);
            scene.AddLight(light);

            return scene;
        }

        public static Scene TexturedSpheresDemoScene()
        {
            Scene scene = new Scene();

            Material matTransparent = new() { Name = "Glass" };
            matTransparent.Type = MaterialType.RefractiveAndReflective;
            matTransparent.Texture = new TextureSolid(Palette.Black);
            matTransparent.Transparency = 1.0f;

            scene.AddMaterial(matTransparent);

            Material matSquares = new() { Name = "Squares" };
            matSquares.Type = MaterialType.Diffuse;
            matSquares.Texture = new TextureSquares();

            scene.AddMaterial(matSquares);

            Material matSolid = new() { Name = "Solid" };
            matSolid.Texture = new TextureSolid(Palette.Red);
            matSolid.Type = MaterialType.Diffuse;

            scene.AddMaterial(matSolid);

            Material matPlane = new() { Name="Ground" };
            matPlane.Texture = new TextureSquares(Palette.Black, Palette.White) { Scale = /*32*/ 8 };
            scene.AddMaterial(matPlane);

            Material matGradient = new() { Name = "Gradient" };
            matGradient.Texture = new TextureGradient(Palette.Red, Palette.Blue) { Blend = TextureGradient.BlendType.Smooth };
            matGradient.Type = MaterialType.Diffuse;
            scene.AddMaterial(matGradient);

            Material matGradient2 = new() { Name = "Gradient2" };
            matGradient2.Texture = new TextureGradient(Palette.Red, Palette.Blue) { Blend = TextureGradient.BlendType.Linear };
            matGradient2.Type = MaterialType.Diffuse;
            scene.AddMaterial(matGradient2);


            Sphere sphere = new Sphere(new Point(-20, 0, 20), 10, Palette.White);
            sphere.Material = matTransparent;
            sphere.Material = matGradient;
            scene.AddShape(sphere);

            sphere = new Sphere(new Point(20, 0, 20), 10, Palette.White);
            sphere.Material= matSquares;
            sphere.Material = matGradient2;
            scene.AddShape(sphere);

            sphere = new Sphere(new Point(0, 0, 40), 10, Palette.Red);
            sphere.Material = matSolid;
            scene.AddShape(sphere);

            Plane plane = new Plane(new Vector(0, 1, 0), new Point(0, -10, 0));
//            Plane plane = new Plane(new Vector(0.707, 0.707, 0.0), new Point(0.0, -10.0, 0.0));
            plane.Material = matPlane;
            //scene.AddShape(plane);


            Disc disc = new Disc(new Point(0, -10, 0), new Vector(0, 1, 0), 200);
            disc.Material = matPlane;
            scene.AddShape(disc);


            //Light light = new PointLight(new Point(-50, 0, 0), /*0.5f*/ 5000.0f, new Colour(1.0f, 0.0f, 0.0f));
            //            scene.AddLight(light);
            //light = new PointLight(new Point(50, 50, 0), /*.85f*/ 5000.0f, new Colour(1.0f, 1.0f, 1.0f));
            //            scene.AddLight(light);

            Light light = new PointLight(new Point(0, 50, 0), /*.85f*/ 5000.0f, new Colour(1.0f, 1.0f, 1.0f));
            scene.AddLight(light);

            light = new DistantLight(new Vector(-50, 50, 50).Negate(), 5, Palette.White);
            scene.AddLight(light);


            //            light = new SpotLight(new Point(0, 100, 0), new Vector(0, -1, 0), 45, 5000, Palette.White);
            //            scene.AddLight(light);

            scene.Background = new Colour(Colors.SkyBlue);

            scene.Camera.Pos = new Point(0, 5, -50);
            scene.Camera.Target = new Point(0, 0, 50);

//           scene.Skydome = new SkydomeGradient(Palette.Red, Palette.Green, Palette.Blue);

            scene.Skydome = new SkydomeBitmap("Data\\skydomes\\immenstadter_horn_2048.jpg");


            return scene;
        }

        public static Scene RefractionDemoScene()
        {
            Scene scene = new Scene();

            Sphere sphere = new Sphere(new Point(-20, 0, 50), 20, Palette.Black /* Palette.White*/);
            //sphere.Material.Texture = new TextureSquares();
            sphere.Material.Type = MaterialType.Reflective;
            sphere.Material.Transparency = 1.0f;
            sphere.Material.Reflectivity = 0.0f;
            //sphere.Material.
            scene.AddShape(sphere);

            sphere = new Sphere(new Point(20, 0, 50), 20, Palette.Black);
            sphere.Material.Type = MaterialType.RefractiveAndReflective;
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


            Plane plane = new Plane(new Vector(0, 0, -1), new Point(0, 0, 150));
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

            Plane plane = new Plane(new Vector(0, 1, 0), new Point(0, -30, 0), Palette.Grey);
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

            float floorHeight = -60;
            float radius = 8;
            float bigRadius = 40;
            //double sceneWidth = 200;

            Plane floor = new Plane(new Vector(0, 1, 0), new Point(0, floorHeight, 0), new Colour(Colors.LightGray));
            scene.AddShape(floor);
            //            Plane sky = new Plane(new Vector(0, 0, -1), new Point(0, 0, 1000), new Colour(Colors.SkyBlue));
            //            scene.AddShape(sky);

            Random random = new Random();

            Colour[] cols = new Colour[] { new Colour(Colors.LightPink), new Colour(Colors.LightGreen), new Colour(Colors.LightYellow) };

            float spacing = 30;

            for (int i = 0; i < 20; i++)
            {
                for (int j = -10; j < 10; j++)
                {
                    float x = j * spacing;
                    float z = 130 + 5 * spacing * i;

                    x += (float)random.NextDouble() * (spacing / 2 - radius);
                    z += (float)random.NextDouble() * (spacing / 2 - radius);

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

            /*Sphere*/
            sphere = new Sphere(new Point(0, floorHeight, 0), 2, Palette.Red);
            scene.AddShape(sphere);

            //Light light = new SpotLight(new Point(-30, 30, 40), new Vector(1, -1, 0, true), 25, 6000, Palette.White);
            Light light = new PointLight(new Point(-25, 25, 40), 10000, Palette.White);
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

            Matrix objectToWorld = new Matrix();

            ModelData modelData = importer.ImportObj(@"Data\pbr-low-poly-fox-character");
            scene.Camera.Pos = new Point(-3.5f, 4, -5);
            scene.Camera.Target = new Point(0, 3, 1);

            /*
            ModelData modelData = importer.ImportObj(@"C:\Users\Jon\Documents\Projects\RayTrace\RayTrace\Data\dinosaur");
            scene.Camera.Pos = new Point(-30, 14, -8);
            scene.Camera.Target = new Point(0, 12, -4);
            */

            //            ModelData modelData = importer.ImportObj(@"C:\Users\Jon\Documents\Projects\RayTrace\RayTrace\Data\apollo-und-daphne");
            //            Matrix objectToWorld = Matrix.GetRotationX((float)Math.PI / 2);
            //            objectToWorld = Matrix.GetRotationY((float)Math.PI / 2) * objectToWorld;

            //ModelData modelData = importer.ImportObj(@"C:\Users\Jon\Documents\Projects\RayTrace\RayTrace\Data\day-18-office-rat");


            Model model = new Model(new Point(0, 0, 10), modelData);
            model.ObjectToWorld = objectToWorld;
            model.Material.Type = MaterialType.Diffuse;
            model.Material.Texture = new TextureSolid(Palette.White);
            //model = ModelFactory.Octahedron();
            // model = ModelFactory.Cube();
            //model = ModelFactory.Tetahedron();
            scene.AddShape(model);

            Material mat = new Material() { Name = "CookTorrence" };
            {
                CookTorrence cookTorrence = new();
                //cookTorrence.Colour = /*Palette.MetallicGold;*/ Palette.Copper;
                cookTorrence.Shininess = 0.75f;
                cookTorrence.Roughness = 0.1f;
                cookTorrence.Absorption = 1.25f;
                //cookTorrence.Specular = 0;

//                mat.Texture = new TextureSolid(/*Palette.MetallicGold;*/ Palette.Copper);
                mat.Texture = new TextureSolid(Palette.MetallicGold);
                mat.Brdf = cookTorrence;
                mat.Type = MaterialType.BRDF_CookTorrence;

                scene.AddMaterial(mat);

                model.Material = mat;
            }

            Sphere sphere = new(new Point(-2, 0, 0), 1);
            sphere.Material.Type = MaterialType.Diffuse;
        //    scene.AddShape(sphere);

            float floorHeight = model.MinY;
            Disc floor = new Disc(new Point(0, floorHeight, 0), new Vector(0, 1, 0), 25);
            floor.Material.Texture = new TextureSolid(Palette.Grey);
            //floor.Material.Reflectivity = 0.1f;
            scene.AddShape(floor);

            Light light = new PointLight(new Point(-50, 50, 10), 6000, Palette.White);
            scene.AddLight(light);

            light = new PointLight(new Point(10, 30, -30), 3000, Palette.White);
            scene.AddLight(light);

            scene.Skydome = new SkydomeBitmap("Data\\skydomes\\immenstadter_horn_2048.jpg");

            return scene;
        }

        public static Scene GIDemoScene()
        {
            Scene scene = new Scene();

            //            Sphere sphere = new Sphere(new Point(-20, 0, 50), 20, new Colour(0.7f, 0.7f, 0.7f) /* Palette.White*/);
            Sphere sphere = new Sphere(new Point(-10, 0, 20), 5, new Colour(0.5f, 0, 0) /* Palette.White*/);
            //sphere.Material.Texture = new TextureSquares();
            sphere.Material.Transparency = 0.0f;
            sphere.Material.Reflectivity = 0.0f;
            //sphere.Material.
            scene.AddShape(sphere);

            //            sphere = new Sphere(new Point(20, 0, 50), 20, new Colour(0.7f, 0, 0));
            sphere = new Sphere(new Point(0, 0, 23), 5, new Colour(0.5f, 0.5f, 0.5f));
            //sphere.Material.Texture = new TextureSquares() { Scale = 10 };
            //sphere.Material.Texture = new TextureNoise(Palette.White, Palette.Black);
            //sphere.Material.Texture = new TextureNoise(Palette.Red, Palette.Green) { Function = TextureNoise.ColourFunction.Blend, Scale = 5 };
            sphere.Material.Transparency = 0.0f;
            sphere.Material.Reflectivity = 0.0f;
            //sphere.Material.Transparency = 0.0f;
            //sphere.Material.Reflectivity = 1.0f;
            scene.AddShape(sphere);

            sphere = new Sphere(new Point(10, 0, 20), 5, new Colour(0.0f, 0.0f, 0.5f) /* Palette.White*/);
            scene.AddShape(sphere);


            //Box box = new Box(new Point(20, 0, 0), 30, 30, 30);
            //box.Material.Transparency = 1.0f;
            //box.Material.Reflectivity = 0.0f;
            //scene.AddShape(box);


            //            Plane plane = new Plane(new Vector(0.0, 0.0, -1.0), new Point(0.0, 0.0, 150.0));
            //Plane plane = new Plane(new Vector(0.0, 1.0, 0.0), new Point(0.0, -5.0, 0.0), Palette.Grey);
            //plane.Material.Reflectivity = 0.0f;

            RayTrace.Disc disc = new Disc(new Point(0, -5, 23), new Vector(0, 1, 0), 25);
            disc.Material.Texture = new TextureSolid(Palette.Grey);

            //plane.Material.Texture = new TextureSolid(Palette.Blue);
            //plane.Material.Texture = new TextureSquares() { Scale = 20 };
            scene.AddShape(disc);

            Plane left = new Plane(new Vector(1, 0, 0), new Point(-18, 0, 0), new Colour(0.5f, 0.5f, 0.5f));
           // scene.AddShape(left);

            Plane back = new Plane(new Vector(0, 0, -1), new Point(0, 0, 50), new Colour(0.5f, 0.5f, 0.5f));
            //scene.AddShape(back);

            Light light = new PointLight(new Point(-50, 0, 0), 3000.0f, new Colour(1.0f, 0.0f, 0.0f));
            // scene.AddLight(light);
            light = new PointLight(new Point(30, 30, 0), 3000.0f, Palette.White);
            DistantLight distantLight = new DistantLight(new Vector(-8, 0, 20), 2, Palette.White);
            //scene.AddLight(light);
            scene.AddLight(distantLight);

            return scene;
        }

        public static Scene GIDemoScene2()
        {
            Scene scene = new Scene();

            const int size = 2;

            Sphere sphere = new Sphere(new Point(-4, size, 11), 2, new Colour(0.25f, 0.25f, 0.25f)  /* Palette.White*/);
            scene.AddShape(sphere);
            sphere = new Sphere(new Point(4, size, 11), size, /*new Colour(0.0f, 0.0f, 0.5f)*/ new Colour(0.25f, 0.25f, 0.25f));
            scene.AddShape(sphere);

            Model cornellBox = ModelFactory.CornellBox(14, 12, 16);
            cornellBox.Material.Texture = new TextureSolid(Palette.Grey);
            cornellBox.ObjectToWorld = Matrix.GetTranslation(0, 0, 2);

            scene.AddShape(cornellBox);

/*
            Plane plane = new Plane(new Vector(0.0, 1.0, 0.0), new Point(0.0, -4.0, 0.0), Palette.Grey);
            scene.AddShape(plane);

            Plane left = new Plane(new Vector(1.0, 0.0, 0.0), new Point(-size, 0.0, 0.0), new Colour(0.5f, 0.0f, 0.0f));
            scene.AddShape(left);
            Plane right = new Plane(new Vector(-1.0, 0.0, 0.0), new Point(size, 0.0, 0.0), new Colour(0.0f, 0.5f, 0.0f));
            scene.AddShape(right);

            Plane back = new Plane(new Vector(0.0, 0.0, -1.0), new Point(0.0, 0.0, 20), new Colour(0.5f, 0.5f, 0.5f));
            scene.AddShape(back);
*/
            Light light = new PointLight(new Point(0, 9, 0), 2000.0f, new Colour(1.0f, 1.0f, 1.0f));
            scene.AddLight(light);

            scene.Camera.Pos = new Point(0, 4, 0);
            scene.Camera.Target = new Point(0, 4, 1);

            return scene;
        }

        public static Scene BRDFScene()
        {
            Scene scene = new Scene();

            const int radius = 2;
            const int DiscRadius = 10;
            /*
                        Material mat = new Material() { Name = "Lambertian (BRDF)" };
                        Lambertian lambertian = new();
                        lambertian.Colour = Palette.Copper;
                        mat.Brdf = lambertian;
                        mat.Type = MaterialType.BRDF_Lambertian;
            */

            float[] Shininess = new float[] { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };
            float[] Roughness = new float[] { 0.1f, 0.25f, 0.5f, 0.75f, 1.0f };
            float[] Absorption = new float[] { 1.0f, 1.25f, 1.5f, 2.0f };

            foreach (float shininess in Shininess)
            {
                foreach (float roughness in Roughness)
                {
                    foreach (float absorption in Absorption)
                    {
                        string name = String.Format("BRDF s{0:F2} r{1:F2} a{2:F2}", shininess, roughness, absorption);
                        Material mat = new Material() { Name = name };
                        {
                            CookTorrence cookTorrence = new();
                            //cookTorrence.Colour = /*Palette.MetallicGold;*/ Palette.Copper;
                            cookTorrence.Shininess = shininess;
                            cookTorrence.Roughness = roughness;
                            cookTorrence.Absorption = absorption;
                            //cookTorrence.Specular = 0;

                            mat.Texture = new TextureSolid(/*Palette.MetallicGold;*/ Palette.Copper);
                            mat.Brdf = cookTorrence;
                            mat.Type = MaterialType.BRDF_CookTorrence;

                            scene.AddMaterial(mat);

                        }
                    }
                }
            }
/*
            Material mat2 = new Material() { Name = "Cook-Torrence (BRDF) 2" };
            {
                CookTorrence cookTorrence = new();
                cookTorrence.Colour = Palette.Copper;
                cookTorrence.Diffuse = 0.25f;
                cookTorrence.Roughness = 0.80f;
                //cookTorrence.Specular = 0;
                mat2.Brdf = cookTorrence;
                mat2.Type = MaterialType.BRDF_CookTorrence;
            }

            Material mat3 = new Material() { Name = "Cook-Torrence (BRDF) 3" };
            {
                CookTorrence cookTorrence = new();
                cookTorrence.Colour = Palette.Copper;
                cookTorrence.Diffuse = 0.25f;
                cookTorrence.Roughness = 0.20f;
                //cookTorrence.Specular = 0;
                mat3.Brdf = cookTorrence;
                mat3.Type = MaterialType.BRDF_CookTorrence;
            }

            scene.AddMaterial(mat);
            scene.AddMaterial(mat2);
            scene.AddMaterial(mat3);
*/
            Sphere sphere = new Sphere(new Point(-2f, radius, 11), radius, new Colour(0.25f, 0.25f, 0.25f)  /* Palette.White*/);
            //sphere.Material = mat;
            scene.AddShape(sphere);

            sphere = new Sphere(new Point(2f, radius, 11), radius, new Colour(0.0f, 0.0f, 0.5f) /*new Colour(0.25f, 0.25f, 0.25f)*/);
            scene.AddShape(sphere);
            scene.AddMaterial(sphere.Material);

            Disc disc = new Disc(new Point(0, 0, 11), new Vector(0, 1, 0), DiscRadius);
            disc.Material.Texture = new TextureSolid(Palette.Red);
            scene.AddShape(disc);
            scene.AddMaterial(disc.Material);

            disc = new Disc(new Point(-4.5f, 0, 11), new Vector(1, 0, 0), DiscRadius);
            disc.Material.Texture = new TextureSolid(Palette.Green);
            scene.AddShape(disc);
            scene.AddMaterial(disc.Material);

            Light light = new PointLight(new Point(0, 9, 0), 2000.0f, new Colour(1.0f, 1.0f, 1.0f));
            scene.AddLight(light);

            scene.Camera.Pos = new Point(0, 4, -2);
            scene.Camera.Target = new Point(0, 4, 1);

            return scene;
        }

        public static Scene SensorScene()
        {
            Scene scene = new Scene();

            Sphere sphere = new Sphere(new Point(0,0,0), 10);
            sphere.Material.Texture = new TextureSolid(Palette.Green);
            scene.AddShape(sphere);

            Disc leftDisc = new Disc(new Point(0,0,0), new Vector(0.707107f, 0, 0.707107f), 11);
            scene.AddShape(leftDisc);

            Disc rightDisc = new Disc(new Point(0, 0, 0), new Vector(-0.707107f, 0, 0.707107f), 11);
            scene.AddShape(rightDisc);

            Disc topDisc = new Disc(new Point(0, 0, 0), new Vector(0, 0.707107f, 0.707107f), 11);
            scene.AddShape(topDisc);

            Disc bottomDisc = new Disc(new Point(0, 0, 0), new Vector(0, -0.707107f, 0.707107f), 11);
            scene.AddShape(bottomDisc);

            scene.Camera.Pos = new Point(0, 0, -20);
            scene.Camera.Target = new Point(0, 0, 0);


            scene.Ambient = new Colour(0.3f, 0.3f, 0.3f);

            return scene;
        }

    }
}
