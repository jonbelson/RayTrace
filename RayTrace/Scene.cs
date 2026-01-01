using System;

using System.Collections.Generic;
using System.ComponentModel;
using RayTrace.Brdfs;
using RayTrace.Importers;
using RayTrace.Skydomes;

namespace RayTrace
{
    public class Scene
    {
        // Size 
        //public int Size { get; }

        public List<Shape> Shapes { get; set; } = new List<Shape>();
        public List<Light> Lights { get; set; } = new List<Light>();

        private readonly Material defaultMaterial = new Material(MaterialType.BRDF_Lambertian) { Texture = new TextureSolid(Palette.Grey), Brdf = new Lambertian(), Name = "Default" };

        private List<Material> materials = new List<Material>();

        public List<Material> Materials
        {
            get { return materials; }
        }

        public string BaseDirectory { get; set; }

        public Camera Camera { get; } = new Camera(new Point(0, 0, 0), new Point(0, 0, 1));

        public Colour Ambient { get; set; } = new Colour(0.2f, 0.2f, 0.2f);

        public Colour Background { get; set; } = Palette.Black;

        public Skydome Skydome { get; set; } = new SkydomeGradient();

        public Scene()
        {
            materials.Add(defaultMaterial);
        }

        public Scene(XMLScene xmlScene)
        {
            Camera = new Camera(xmlScene.Camera);
            Ambient = new Colour(xmlScene.Ambient);
            Background = new Colour(xmlScene.Background);

            if (xmlScene.Skydome != null)
            {
                Skydome = xmlScene.Skydome.Create();
            }

            // or use 'Camera XMLCamera.FromXML()'

            foreach (XMLMaterial xmlMaterial in xmlScene.Materials)
            {
                Material material = xmlMaterial.Create();
                Materials.Add(material);
            }

            foreach (XMLShape xmlShape in xmlScene.Shapes)
            {
                Shape shape = xmlShape.Create(this);
                shape.Material = GetMaterial(xmlShape.MaterialGuid);
                //Shapes.Add(shape);
            }
            foreach (XMLLight xmlLight in xmlScene.Lights)
            {
                //Lights.Add(xmlLight.Create(this));
                Light light = xmlLight.Create(this);
            }
        }

        public void CreateCaches()
        {
            Camera.UpdateMatrix();

            foreach (Shape shape in Shapes)
            {
                shape.CreateCache(Camera);
            }
            foreach (Light light in Lights)
            {
                light.CreateCache(Camera);
            }
        }

        // Return closest hit object.
        public HitResult HitTest(Camera camera, Ray ray)
        {
            HitResult hitResult = new HitResult(null);

            double closest = Double.MaxValue;

            foreach (Shape shape in Shapes)
            {
                if (!shape.Enabled) continue;

                HitResult hr = shape.HitTest(camera, ray);
                if (hr.HasEntry())
                {
                    double d = ray.Origin.Distance(hr.Entry);
                    if (d < closest)
                    {
                        closest = d;
                        hitResult = hr;
                    }
                }
            }

            hitResult.Ray = ray.Direction;
            return hitResult;
        }

        // Return closest hit object.
        public HitResult HitTest(Camera camera, SPoint p1, SPoint p2)
        {
            Ray ray = Ray.FromPoints(p1, p2);
            return HitTest(camera, ray);
        }

        // Return true if an object is intersected.
        public bool HitTestFast(Camera camera, Ray ray)
        {
            foreach (Shape shape in Shapes)
            {
                if (!shape.Enabled) continue;

                if (shape.HitTestQuick(camera, ray))
                {
                    return true;
                }
            }

            return false;
        }

        // Return true if an object is intersected.
        public bool HitTestFast(Camera camera, SPoint p1, SPoint p2)
        {
            Ray ray = Ray.FromPoints(p1, p2);

            return HitTestFast(camera, ray);
        }

        public List<Light> IsIlluminated(Camera camera, HitResult hr)
        {
            SPoint pos = hr.Entry;

            List<Light> lights = new List<Light>();

            foreach (Light light in Lights)
            {
                if (!light.Enabled) continue;

                if (light is not PointLight pointLight)
                {
                    continue;
                }

                //Point pos2 = pointLight.Pos;
                SPoint pos2 = camera.WorldToView * pointLight._pos;
                //Vector v = pointLight.Pos.Difference(pos);
                //Vector v = pos2.Difference(pos);

                //Vector step = light.Pos.Difference(pos);
                //step = step.Normalise();
                //pos = pos.Offset(step);

                Camera nullCamera = new Camera(new Point(0, 0, 0), new Point(0, 0, 0));
                nullCamera.WorldToView.SetIdentity();

                HitResult hitResult = HitTest(/*nullCamera*/ camera, pos, pos2);
                //HitResult hitResult = HitTest(camera, pos, pos2);
                if (hitResult.HasEntry())
                {
                    // Ray from hit point to light hit an object, so no direct illumination.
                    //return null;
                }
                else
                {
                    //return light;
                    lights.Add(light);
                }
            }

            return lights;
        }

        public void AddShape(Shape shape)
        {
            if (!Shapes.Contains(shape))
            {
                Shapes.Add(shape);
            }
        }

        public bool RemoveShape(Shape shape)
        {
            return shape != null ? Shapes.Remove(shape) : false;
        }

        public void AddLight(Light light)
        {
            if (!Lights.Contains(light))
            {
                Lights.Add(light);
            }
        }

        public bool RemoveLight(Light light)
        {
            return light != null ? Lights.Remove(light) : false;
        }

        public void AddMaterial(Material material)
        {
            if (!Materials.Contains(material))
            {
                Materials.Add(material);
            }
        }

        public bool RemoveMaterial(Material material)
        {
            if (material != null)
            {
                return Materials.Remove(material);
            }
            return false;
        }

        public Material GetDefaultMaterial()
        {
            return defaultMaterial;
        }

        public Material GetMaterial(Guid guid)
        {
            Material material = Materials.Find(x => x.Guid == guid);

            return material ?? defaultMaterial;
        }

        private string GetUniqueName<T>(List<T> list, Type type) where T : Entity
        {
            string baseName = type.Name;

            for (int suffix = 1; ; suffix++)
            {
                bool found = false;
                string newName = baseName + (suffix == 1 ? "" : suffix);

                foreach (T item in list)
                {
                    if (item.GetType() == type && item.Name == newName)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return newName;
                }
            }
        }

        //
        // Return a unique name for a Shape based on its type name.
        //
        public string GetUniqueShapeName(Type type)
        {
            return GetUniqueName<Shape>(Shapes, type);
            /*
                        string baseName = type.Name;

                        for (int suffix=1; ; suffix++)
                        {
                            bool found = false;
                            string newName = baseName + (suffix == 1 ? "" : suffix);

                            foreach (Shape shape in Shapes)
                            {
                                if (shape.GetType() == type && shape.Name == newName)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                return newName;
                            }
                        }
              */
        }

        //
        // Return a unique name for a Light based on its type name.
        //
        public string GetUniqueLightName(Type type)
        {
            return GetUniqueName(Lights, type);
        }

        public SColour GetSkydomeColour(Camera camera, SVector ray)
        {
            if (Skydome != null && Skydome.Enabled)
            {
                return Skydome.GetColour(camera, ray);
            }

            return SPalette.Black;
        }

        public virtual XMLScene ToXML()
        {
            XMLScene xml = new XMLScene();

            xml.Ambient = Ambient.ToXML();
            xml.Camera = Camera.ToXML();
            xml.Background = Background.ToXML();

            xml.Skydome = Skydome.ToXML();

            foreach (Material material in Materials)
            {
                XMLMaterial xmlMaterial = material.ToXML();
                xml.Materials.Add(xmlMaterial);
            }

            foreach (Shape shape in Shapes)
            {
                XMLEntity xmlEntity = shape.ToXML();
                xml.Shapes.Add(xmlEntity as XMLShape);
            }

            foreach (Light light in Lights)
            {
                XMLEntity xmlEntity = light.ToXML();
                xml.Lights.Add(xmlEntity as XMLLight);
            }

            return xml;
        }

        public Model ImportModel(string fileName)
        {
            ModelData modelData = null;
            List<Material> materials = [];

            Model model = null;

            string ext = System.IO.Path.GetExtension(fileName);

            if (ext.Equals(".obj", StringComparison.OrdinalIgnoreCase))
            {
                ObjImporter importer = new();

                modelData = importer.ImportModel(fileName);
                materials = importer.CreateMaterials();
            }
            else if (ext.Equals(".gltf", StringComparison.OrdinalIgnoreCase))
            {
                glTFImporter importer = new();

                modelData = importer.ImportModel(fileName);
                materials = importer.CreateMaterials();
            }

            if (modelData != null)
            {
                model = new(this, new Point(0, 0, 0), modelData, materials, true);

                model.Name = System.IO.Path.GetFileNameWithoutExtension(modelData.fileName);

                //shape = model;

                foreach (var material in materials)
                {
                    AddMaterial(material);
                }
            }

            return model;
        }

    }
}
