using System;
using System.Collections.Generic;
using System.IO;
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

            Sphere sphere = new Sphere(scene, new Point(0, 0, 0), 2, Palette.Red);
            sphere.Material = mat;
            scene.AddShape(sphere);

            scene.Camera.Pos = new Point(3, 5, -16);
            scene.Camera.Target = new Point(0, 0, 0);

            new PointLight(scene, new Point(0, 10, 0), 1000, Palette.White);

            scene.Skydome = new Skybox("Data\\skyboxes\\Forest");

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
                    Sphere sphere = new Sphere(scene, new Point(-50 + i * 20, -40 + j * 20, 80), 8, Palette.Green);
                    sphere.Material.SetType(MaterialType.Phong);
                    sphere.Material.PhongDiffuse = 0.95;
                    sphere.Material.PhongSpecular = 2 * ks[i];  // i / 5.0;
                    sphere.Material.PhongShininess = n[j];  // (j + 1) * (j + 1);
                    scene.AddShape(sphere);
                }
            }
            //Light light = new PointLight(new Point(-50, 0, 0), /*0.5f*/ 50000.0f, new Colour(1.0f, 1.0f, 1.0f));
            Light light = new DistantLight(scene, new Vector(1, 0, 1), 2, Palette.White);
           //scene.AddLight(light);

            return scene;
        }

        public static Scene TexturedSpheresDemoScene()
        {
            Scene scene = new Scene();

            Material matTransparent = new() { Name = "Glass" };
            matTransparent.SetType(MaterialType.RefractiveAndReflective);
            matTransparent.Texture = new TextureSolid(Palette.Black);
            matTransparent.Transparency = 1.0f;

            scene.AddMaterial(matTransparent);

            Material matSquares = new() { Name = "Squares" };
            matSquares.SetType(MaterialType.Diffuse);
            matSquares.Texture = new TextureSquares();

            scene.AddMaterial(matSquares);

            Material matSolid = new(MaterialType.Diffuse) { Name = "Solid" };
            matSolid.Texture = new TextureSolid(Palette.Red);

            scene.AddMaterial(matSolid);

            Material matPlane = new() { Name="Ground" };
            matPlane.Texture = new TextureSquares(Palette.Black, Palette.White) { Scale = /*32*/ 8 };
            scene.AddMaterial(matPlane);

            Material matGradient = new(MaterialType.Diffuse) { Name = "Gradient" };
            matGradient.Texture = new TextureGradient(Palette.Red, Palette.Blue) { Blend = TextureGradient.BlendType.Smooth };
            scene.AddMaterial(matGradient);

            Material matGradient2 = new(MaterialType.Diffuse) { Name = "Gradient2" };
            matGradient2.Texture = new TextureGradient(Palette.Red, Palette.Blue) { Blend = TextureGradient.BlendType.Linear };
            scene.AddMaterial(matGradient2);


            Sphere sphere = new Sphere(scene, new Point(-20, 0, 20), 10, Palette.White);
            sphere.Material = matTransparent;
            sphere.Material = matGradient;
            scene.AddShape(sphere);

            sphere = new Sphere(scene, new Point(20, 0, 20), 10, Palette.White);
            sphere.Material= matSquares;
            sphere.Material = matGradient2;
            scene.AddShape(sphere);

            sphere = new Sphere(scene, new Point(0, 0, 40), 10, Palette.Red);
            sphere.Material = matSolid;
            scene.AddShape(sphere);

            Plane plane = new Plane(scene, new Point(0, -10, 0), new Vector(0, 1, 0));
//            Plane plane = new Plane(new Vector(0.707, 0.707, 0.0), new Point(0.0, -10.0, 0.0));
            plane.Material = matPlane;
            //scene.AddShape(plane);


            Disc disc = new Disc(scene, new Point(0, -10, 0), new Vector(0, 1, 0), 200);
            disc.Material = matPlane;
            scene.AddShape(disc);


            //Light light = new PointLight(new Point(-50, 0, 0), /*0.5f*/ 5000.0f, new Colour(1.0f, 0.0f, 0.0f));
            //            scene.AddLight(light);
            //light = new PointLight(new Point(50, 50, 0), /*.85f*/ 5000.0f, new Colour(1.0f, 1.0f, 1.0f));
            //            scene.AddLight(light);

            Light light = new PointLight(scene, new Point(0, 50, 0), /*.85f*/ 5000.0f, new Colour(1.0f, 1.0f, 1.0f));
            //scene.AddLight(light);

            light = new DistantLight(scene, new Vector(-50, 50, 50).Negate(), 5, Palette.White);
            //scene.AddLight(light);


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

            Sphere sphere = new Sphere(scene, new Point(-20, 0, 50), 20, Palette.Black /* Palette.White*/);
            //sphere.Material.Texture = new TextureSquares();
            sphere.Material.SetType(MaterialType.Reflective);
            sphere.Material.Transparency = 1.0f;
            sphere.Material.Reflectivity = 0.0f;
            //sphere.Material.
            scene.AddShape(sphere);

            sphere = new Sphere(scene, new Point(20, 0, 50), 20, Palette.Black);
            sphere.Material.SetType(MaterialType.RefractiveAndReflective);
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


            Plane plane = new Plane(scene, new Point(0, 0, 150), new Vector(0, 0, -1));
            //Plane plane = new Plane(new Vector(0.0, 1.0, 0.0), new Point(0.0, -30.0, 0.0), Palette.Grey);
            plane.Material.Reflectivity = 0.0f;
            //plane.Material.Texture = new TextureSolid(Palette.Blue);
            plane.Material.Texture = new TextureSquares() { Scale = 20 };
            scene.AddShape(plane);

            Light light = new PointLight(scene, new Point(-50, 0, 0), 3000.0f, new Colour(1.0f, 0.0f, 0.0f));
            //scene.AddLight(light);
            light = new PointLight(scene, new Point(50, 50, 0), 3000.0f, Palette.White);
            //scene.AddLight(light);

            return scene;
        }

        public static Scene BoxDemoScene()
        {
            Scene scene = new Scene();

            Box box = new Box(scene, new Point(-40, 0, 0), 10, 10, 10);
            //scene.AddShape(box);

            box = new Box(scene, new Point(0, 0, 240), 40);
            box.Material.Texture = new TextureSquares() { Scale = 40 };
            scene.AddShape(box);

            Plane plane = new Plane(scene, new Point(0, -30, 0), new Vector(0, 1, 0), Palette.Grey);
            plane.Material.Reflectivity = 1.0f;
            //plane.Material.Texture = new TextureSolid(Palette.Blue);
            //plane.Material.Texture = new TextureSquares() { Scale = 20 };
            scene.AddShape(plane);


            Light light = new PointLight(scene, new Point(50, 50, 0), 3000.0f, Palette.White);
            //scene.AddLight(light);

            return scene;
        }

        public static Scene SphereDemoScene()
        {
            Scene scene = new Scene();

            float floorHeight = -60;
            float radius = 8;
            float bigRadius = 40;
            //double sceneWidth = 200;

            Plane floor = new Plane(scene, new Point(0, floorHeight, 0), new Vector(0, 1, 0), new Colour(Colors.LightGray));
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

                    Sphere sphere = new Sphere(scene, new Point(x, floorHeight + radius, z), (int)radius, Palette.Red);

                    int index = random.Next(3);
                    sphere.Material = new Material(cols[index]);

                    scene.AddShape(sphere);
                }
            }

            Sphere sphere2 = new Sphere(scene, new Point(40, floorHeight + bigRadius, 400), (int)(bigRadius), Palette.Black);
            sphere2.Material.Transparency = 0.0f;
            sphere2.Material.Reflectivity = 1.0f;
            scene.AddShape(sphere2);

            Sphere sphere3 = new Sphere(scene, new Point(20, floorHeight + bigRadius, 600), (int)(bigRadius), Palette.Black);
            sphere3.Material.Transparency = 1.0f;
            sphere3.Material.Reflectivity = 0.0f;
            scene.AddShape(sphere3);


            //scene.AddLight(new PointLight(new Point(-25, 25, 0), 6000, Palette.White));
            //scene.AddLight(new DistantLight(new Vector(25, 25, 0), 6000, Palette.White));
            //scene.AddLight(new PointLight(new Point(25, 25, 0), 6000, Palette.White));

            PointLight pl = new PointLight(scene, new Point(-25, 25, 0), 6000, Palette.White);
            pl = new PointLight(scene, new Point(25, 25, 0), 6000, Palette.White);

            scene.Background = new Colour(Colors.LightSkyBlue);
            scene.Background = new Colour(Colors.PowderBlue);
            scene.Ambient = new Colour(.3f, .3f, .3f);

            return scene;
        }

        public static Scene SoftLightDemoScene()
        {
            Scene scene = new Scene();

            float floorHeight = -10;
            Plane floor = new Plane(scene, new Point(0, floorHeight, 0), new Vector(0, 1, 0), new Colour(Colors.LightGray));
            scene.AddShape(floor);

            //Box box = new Box(new Point(0, 0, 40), 20.0f);
            //scene.AddShape(box);

            ///*Box*/ box = new Box(new Point(-30, -5, 40), 10.0f);
            //scene.AddShape(box);

            Sphere sphere = new Sphere(scene, new Point(0, 0, 40), 10, Palette.Red);
            //scene.AddShape(sphere);

            /*Sphere*/
            sphere = new Sphere(scene, new Point(0, floorHeight, 0), 2, Palette.Red);
            //scene.AddShape(sphere);

            //Light light = new SpotLight(new Point(-30, 30, 40), new Vector(1, -1, 0, true), 25, 6000, Palette.White);
            Light light = new PointLight(scene, new Point(-25, 25, 40), 10000, Palette.White);
            //scene.AddLight(light);

            return scene;
        }

        public static Scene FovDemoScene()
        {
            Scene scene = new Scene();

            float floorHeight = -10;

            Sphere sphere = new Sphere(scene, new Point(-20, 0, 20), 10, new Colour(Colors.Orange));
            //scene.AddShape(sphere);

            sphere = new Sphere(scene, new Point(-10, 0, 30), 10, Palette.Red);
            //scene.AddShape(sphere);

            sphere = new Sphere(scene, new Point(10, 0, 70), 10, Palette.Green);
            //scene.AddShape(sphere);

            sphere = new Sphere(scene, new Point(40, 0, 110), 10, Palette.Blue);
            //scene.AddShape(sphere);

            sphere = new Sphere(scene, new Point(80, 0, 150), 10, new Colour(Colors.Yellow));
            //scene.AddShape(sphere);

            sphere = new Sphere(scene, new Point(0, floorHeight, 1), 5, Palette.Grey);
            //scene.AddShape(sphere);

            Plane floor = new Plane(scene, new Point(0, floorHeight, 0), new Vector(0, 1, 0), new Colour(Colors.LightGray));
            //floor.Material.Texture = new TextureSquares(Palette.Black, Palette.White);
            //floor.Material.Reflectivity = 0.1f;
            //scene.AddShape(floor);

            Light light = new PointLight(scene, new Point(-50, 50, 40), 6000, Palette.White);
            //scene.AddLight(light);

            light = new PointLight(scene, new Point(0, 50, 0), 3000, Palette.White);
            //scene.AddLight(light);

            return scene;
        }

        public static Scene ModelScene()
        {
            Scene scene = new Scene();

            //Model model = new Model(new Point(0.0, 0.0, 0.0), modelData);
            Model model = ModelFactory.Cube(scene);
            //Model model = ModelFactory.Octahedron();
            model = ModelFactory.Tetahedron(scene);
            //scene.AddShape(model);

            float floorHeight = -10;
            Plane floor = new Plane(scene, new Point(0, floorHeight, 0), new Vector(0, 1, 0), new Colour(Colors.LightGray));
            //floor.Material.Texture = new TextureSquares(Palette.Black, Palette.White);
            //floor.Material.Reflectivity = 0.1f;
            //scene.AddShape(floor);

            Light light = new PointLight(scene, new Point(-50, 50, 40), 6000, Palette.White);
            //scene.AddLight(light);

            return scene;
        }

        public static Scene GltfScene()
        {
            Scene scene = new();

            scene.Camera.Pos = new Point(-5.994f, 4.712f, -9.275f);
            scene.Camera.Target = new Point(0, 3, 1);

            glTFImporter importer = new();
//            ModelData modelData = importer.ImportModel("C:\\Users\\Jon\\Downloads\\thunderjaw_from_horizon_zero_dawn\\scene.gltf");
            ModelData modelData = importer.ImportModel(@"Data\thunderjaw_from_horizon_zero_dawn\scene.gltf");
            if (modelData != null)
            {
                Model model = new Model(scene, new Point(0, 0, 0), modelData, true);

                scene.AddShape(model);
            }

            Light light = new PointLight(scene, new Point(-50, 50, 10), 6000, Palette.White);
            //scene.AddLight(light);

            light = new PointLight(scene, new Point(10, 30, -30), 3000, Palette.White);
            //scene.AddLight(light);

            //scene.Skydome = new SkydomeBitmap("Data\\skydomes\\immenstadter_horn_2048.jpg");
            //            scene.Skydome = new Skybox("Data\\skyboxes\\Yokohama2");
            try
            {
                scene.Skydome = new Skybox("Data\\skyboxes\\Forest");
            }
            catch (Exception ex)
            {

            }

            return scene;
        }

        public static Scene ObjScene()
        {
            //string baseDir = 

            Scene scene = new Scene();

            ObjImporter importer = new ObjImporter();

            //Matrix objectToWorld = new Matrix();

//            ModelData modelData = importer.ImportObj(@"Data\Firefox_IdaFabere\LP_Firefox.obj");
            ModelData modelData = importer.ImportObj(@"Data\pbr-low-poly-fox-character\source\LP_Firefox.obj");

            TextureBitmap objTexture = new(@"Data\pbr-low-poly-fox-character\source\LP_Firefox_1001_BaseColor.png");

            NormalMap normalMap = new(@"Data\pbr-low-poly-fox-character\source\LP_Firefox_1001_Normal.png");

            scene.Camera.Pos = new Point(-5.994f, 4.712f, -9.275f);
            scene.Camera.Target = new Point(0, 3, 1);


            /*
            ModelData modelData = importer.ImportObj(@"C:\Users\Jon\Documents\Projects\RayTrace\RayTrace\Data\dinosaur");
            scene.Camera.Pos = new Point(-30, 14, -8);
            scene.Camera.Target = new Point(0, 12, -4);
            */

            //ModelData modelData = importer.ImportObj(@"C:\Users\Jon\Documents\Projects\RayTrace\RayTrace\Data\apollo-und-daphne");
            //Matrix objectToWorld = Matrix.GetRotationX((float)Math.PI / 2);
            //objectToWorld = Matrix.GetRotationY((float)Math.PI / 2) * objectToWorld;

            //ModelData modelData = importer.ImportObj(@"C:\Users\Jon\Documents\Projects\RayTrace\RayTrace\Data\day-18-office-rat");


            Model model = new Model(scene, new Point(0, 0, 0), modelData, true);
            //model.ObjectToWorld = objectToWorld;

            //BoundingBox bb = modelData.GetBoundingBox();

            //model.Pos.X = -bb.GetCentreX();
            //model.Pos.Y = -bb.MinY;
            //model.Pos.Z = -bb.GetCentreZ();

            Material objMaterial = new(MaterialType.Diffuse);
            //objMaterial.Texture = new TextureSolid(Palette.White);
            objMaterial.Texture = objTexture;
            objMaterial.NormalMap = normalMap;

            string? fileName = Path.GetFileNameWithoutExtension(modelData.fileName);
            objMaterial.Name = fileName ?? "OBJ Material";



            scene.AddMaterial(objMaterial);

            //model = ModelFactory.Octahedron();
            // model = ModelFactory.Cube();
            //model = ModelFactory.Tetahedron();
            scene.AddShape(model);

            Material mat = new Material() { Name = "CookTorrence" };
            {
                CookTorrence cookTorrence = new()
                {
                    Shininess = 0.9f,
                    Roughness = 0.1f,
                    Absorption = 1.25f
                };

                // mat.Texture = new TextureSolid(/*Palette.MetallicGold;*/ Palette.Copper);
                mat.Texture = new TextureSolid(Palette.MetallicGold);
                mat.SetType(MaterialType.BRDF_CookTorrence);
                mat.Brdf = cookTorrence;
                mat.NormalMap = new(@"Data\pbr-low-poly-fox-character\source\LP_Firefox_1001_Normal.png");

                scene.AddMaterial(mat);

                model.Material = mat;
            }

            Sphere sphere = new(scene, new Point(-2, 0, 0), 1);
            sphere.Material.SetType(MaterialType.Diffuse);

            float floorHeight = model.MinY;

            Disc floor = new Disc(scene, new Point(0, floorHeight, 0), new Vector(0, 1, 0), 25);
            floor.Material.Texture = new TextureSolid(Palette.Grey);

            scene.AddMaterial(floor.Material);
            //floor.Material.Reflectivity = 0.1f;

            Light light = new PointLight(scene, new Point(-50, 50, 10), 6000, Palette.White);

            light = new PointLight(scene, new Point(10, 30, -30), 3000, Palette.White);

            //scene.Skydome = new SkydomeBitmap("Data\\skydomes\\immenstadter_horn_2048.jpg");
//            scene.Skydome = new Skybox("Data\\skyboxes\\Yokohama2");
            scene.Skydome = new Skybox("Data\\skyboxes\\Forest");

            return scene;
        }

        public static Scene GIDemoScene()
        {
            Scene scene = new();

            scene.Camera.Pos = new Point(0, 3, -26);
            scene.Camera.Target = new Point(0, 0, 0);

            const int radius = 4;

            //            Sphere sphere = new Sphere(new Point(-20, 0, 50), 20, new Colour(0.7f, 0.7f, 0.7f) /* Palette.White*/);
            Sphere sphere = new(scene, new Point(-10, radius, 0), radius /*, new Colour(0.5f, 0, 0)*/ /* Palette.White*/);
            //sphere.Material.Texture = new TextureSquares();

            Material material = new() { Name = "Red", Transparency = 0, Reflectivity = 0, Texture = new TextureSolid(new Colour(0.5f, 0, 0)) };
            //sphere.Material.Transparency = 0.0f;
            //sphere.Material.Reflectivity = 0.0f;
            //sphere.Material.
            //scene.AddShape(sphere);
            //scene.AddMaterial(sphere.Material);
            sphere.Material = material;

            //            sphere = new Sphere(new Point(20, 0, 50), 20, new Colour(0.7f, 0, 0));
            sphere = new(scene, new Point(0, radius, 5), 5 /*, new Colour(0.5f, 0.5f, 0.5f)*/);
            //sphere.Material.Texture = new TextureSquares() { Scale = 10 };
            //sphere.Material.Texture = new TextureNoise(Palette.White, Palette.Black);
            //sphere.Material.Texture = new TextureNoise(Palette.Red, Palette.Green) { Function = TextureNoise.ColourFunction.Blend, Scale = 5 };
            //sphere.Material.Transparency = 0.0f;
            //sphere.Material.Reflectivity = 0.0f;
            //sphere.Material.Transparency = 0.0f;
            //sphere.Material.Reflectivity = 1.0f;
            //scene.AddShape(sphere);
            material = new() { Name = "White", Transparency = 0, Reflectivity = 0, Texture = new TextureSolid(new Colour(0.5f, 0.5f, 0.5f)) };
            sphere.Material = material;

            sphere = new(scene, new Point(10, radius, 0), radius /*, new Colour(0.0f, 0.0f, 0.5f)*/ /* Palette.White*/);
            //scene.AddShape(sphere);
            //scene.AddMaterial(sphere.Material);
            //sphere.Material = material;
            material = new() { Name = "Blue", Transparency = 0, Reflectivity = 0, Texture = new TextureSolid(new Colour(0, 0, 0.5f)) };
            sphere.Material = material;


            //Box box = new Box(new Point(20, 0, 0), 30, 30, 30);
            //box.Material.Transparency = 1.0f;
            //box.Material.Reflectivity = 0.0f;
            //scene.AddShape(box);


            //            Plane plane = new Plane(new Vector(0.0, 0.0, -1.0), new Point(0.0, 0.0, 150.0));
            //Plane plane = new Plane(new Vector(0.0, 1.0, 0.0), new Point(0.0, -5.0, 0.0), Palette.Grey);
            //plane.Material.Reflectivity = 0.0f;

            Disc disc = new Disc(scene, new Point(0, 0, 0), new Vector(0, 1, 0), 25);
            material = new() { Name = "Grey", Transparency = 0, Reflectivity = 0, Texture = new TextureSolid(Palette.Grey) };
            //disc.Material.Texture = new TextureSolid(Palette.Grey);

            //plane.Material.Texture = new TextureSolid(Palette.Blue);
            //plane.Material.Texture = new TextureSquares() { Scale = 20 };
            //scene.AddShape(disc);
            //scene.AddMaterial(disc.Material);
            disc.Material = material;
/*
            Plane left = new Plane(scene, new Point(-18, 0, 0), new Vector(1, 0, 0), new Colour(0.5f, 0.5f, 0.5f));
           // scene.AddShape(left);

            Plane back = new Plane(scene, new Point(0, 0, 50), new Vector(0, 0, -1), new Colour(0.5f, 0.5f, 0.5f));
            //scene.AddShape(back);
*/

            //Light light = new PointLight(new Point(-50, 0, 0), 3000.0f, new Colour(1.0f, 0.0f, 0.0f));
            // scene.AddLight(light);
            //light = new PointLight(new Point(30, 30, 0), 3000.0f, Palette.White);
            DistantLight distantLight = new(scene, new Vector(-8, 0, 20), 2, Palette.White);
            //scene.AddLight(light);
            //scene.AddLight(distantLight);

            return scene;
        }

        public static Scene GIDemoScene2()
        {
            Scene scene = new Scene();

            const int size = 2;

            Sphere sphere = new Sphere(scene, new Point(-4, size, 11), 2, new Colour(0.25f, 0.25f, 0.25f)  /* Palette.White*/);
            scene.AddShape(sphere);
            sphere = new Sphere(scene, new Point(4, size, 11), size, /*new Colour(0.0f, 0.0f, 0.5f)*/ new Colour(0.25f, 0.25f, 0.25f));
            scene.AddShape(sphere);

            Model cornellBox = ModelFactory.CornellBox(scene, 14, 12, 16);
            cornellBox.Material.Texture = new TextureSolid(Palette.Grey);
            //cornellBox.ObjectToWorld = Matrix.GetTranslation(0, 0, 2);

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
            Light light = new PointLight(scene, new Point(0, 9, 0), 2000.0f, new Colour(1.0f, 1.0f, 1.0f));
            //scene.AddLight(light);

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
                            mat.SetType(MaterialType.BRDF_CookTorrence);

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
            Sphere sphere = new Sphere(scene, new Point(-2f, radius, 11), radius, new Colour(0.25f, 0.25f, 0.25f)  /* Palette.White*/);
            //sphere.Material = mat;
            //scene.AddShape(sphere);

            sphere = new Sphere(scene, new Point(2f, radius, 11), radius, new Colour(0.0f, 0.0f, 0.5f) /*new Colour(0.25f, 0.25f, 0.25f)*/);
            //scene.AddShape(sphere);
            scene.AddMaterial(sphere.Material);

            Disc disc = new Disc(scene, new Point(0, 0, 11), new Vector(0, 1, 0), DiscRadius);
            disc.Material.Texture = new TextureSolid(Palette.Red);
            //scene.AddShape(disc);
            scene.AddMaterial(disc.Material);

            disc = new Disc(scene, new Point(-4.5f, 0, 11), new Vector(1, 0, 0), DiscRadius);
            disc.Material.Texture = new TextureSolid(Palette.Green);
            //scene.AddShape(disc);
            scene.AddMaterial(disc.Material);

            Light light = new PointLight(scene, new Point(0, 9, 0), 2000.0f, new Colour(1.0f, 1.0f, 1.0f));
            //scene.AddLight(light);

            scene.Camera.Pos = new Point(0, 4, -2);
            scene.Camera.Target = new Point(0, 4, 1);

            return scene;
        }

        public static Scene SensorScene()
        {
            Scene scene = new Scene();

            Sphere sphere = new Sphere(scene, new Point(0,0,0), 10);
            sphere.Material.Texture = new TextureSolid(Palette.Green);
            scene.AddShape(sphere);

            Disc leftDisc = new Disc(scene, new Point(0,0,0), new Vector(0.707107f, 0, 0.707107f), 11);
            scene.AddShape(leftDisc);

            Disc rightDisc = new Disc(scene, new Point(0, 0, 0), new Vector(-0.707107f, 0, 0.707107f), 11);
            scene.AddShape(rightDisc);

            Disc topDisc = new Disc(scene, new Point(0, 0, 0), new Vector(0, 0.707107f, 0.707107f), 11);
            scene.AddShape(topDisc);

            Disc bottomDisc = new Disc(scene, new Point(0, 0, 0), new Vector(0, -0.707107f, 0.707107f), 11);
            scene.AddShape(bottomDisc);

            scene.Camera.Pos = new Point(0, 0, -20);
            scene.Camera.Target = new Point(0, 0, 0);


            scene.Ambient = new Colour(0.3f, 0.3f, 0.3f);

            return scene;
        }

        public static Scene NormalMapScene()
        {
            Scene scene = new();

            TextureBitmap earthTexture = new(@"Data\Textures\earth_gebco8_texture_8192x4096.jpg");
//            NormalMap normalMap = new(@"Data\Textures\earth_normalmap_8192x4096.jpg");
            NormalMap earthNormal = new(@"Data\Textures\earth_normalmap_flat_8192x4096.jpg");

            Material material = new(MaterialType.Diffuse);
            material.Texture = earthTexture;
            //material.Texture = new TextureSolid(Palette.Red);
            material.NormalMap = earthNormal;

            scene.AddMaterial(material);

            TextureBitmap moonTexture = new(@"Data\Textures\mars_mola_hypsometric_8192x4096.jpg");
            //            NormalMap normalMap = new(@"Data\Textures\earth_normalmap_8192x4096.jpg");
            NormalMap moonNormalMap = new(@"Data\Textures\mars_mola_normalmap_8192x4096.jpg");

            Material flatMoonMaterial = new(MaterialType.Diffuse);
            flatMoonMaterial.Texture = moonTexture;
            //material.NormalMap = moonNormalMap;

            scene.AddMaterial(flatMoonMaterial);

            Material bumpyMoonMaterial = new(MaterialType.Diffuse);
            bumpyMoonMaterial.Texture = moonTexture;
            //material.Texture = new TextureSolid(Palette.Red);
            bumpyMoonMaterial.NormalMap = moonNormalMap;

            scene.AddMaterial(bumpyMoonMaterial);


            int option = 3;

            switch (option)
            {
                case 0:
                    {
                        Rectangle rectangle = new(scene, new Point(0, 0, 0), new Vector(0, 0, -1), new Vector(0, 1, 0), 8, 4);
                        ////Rectangle rectangle = new(scene, new Point(0, 0, 0), new Vector(0, 1, -1).Normalise(), new Vector(0, 1, 1).Normalise(), 8, 4);

                        rectangle.Material = material;
                    }
                    break;
                case 1:
                    {
                        Sphere sphere = new(scene, new Point(0, 0, 0), 1);
                        sphere.Material = material;
                    }
                    break;

                case 2:
                    {
                        Sphere sphere = new(scene, new Point(-1.2f, 0, 0), 1);
                        Sphere sphere2 = new(scene, new Point(1.2f, 0, 0), 1);
                        sphere.Material = flatMoonMaterial;
                        sphere2.Material = bumpyMoonMaterial;
                    }
                    break;
                case 3:
                    {
                        Model rectangle = ModelFactory.Rectangle(scene, 4, 2, 1);
                        rectangle.Material = material;
                    }
                    break;
            }


            Light light = new PointLight(scene, new Point(0, 30, -10), 6000, Palette.White);

            scene.Camera.Pos = new Point(0, 0, -4);
            scene.Camera.Target = new Point(0, 0, 0);

            //scene.AddShape

            return scene;
        }

    }
}
