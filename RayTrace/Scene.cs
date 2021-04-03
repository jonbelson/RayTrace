using System;

using System.Collections.Generic;
using System.ComponentModel;

namespace RayTrace
{
    public class Scene
    {
        // Size 
        //public int Size { get; }

        public List<Shape> Shapes = new List<Shape>();
        public List<Light> Lights = new List<Light>();

        private Material defaultMaterial = new Material(Palette.Grey);
        public List<Material> Materials = new List<Material>();

        [NotifyParentProperty(true)]
        public Camera Camera { get; } = new Camera(new Point(0, 0, 0), new Point(0, 0, 1));

        public Colour Ambient { get; set; } = new Colour(0.2f, 0.2f, 0.2f);

        public Colour Background { get; set; } = Palette.Black;

        public Scene()
        {
            //Ambient = new Colour(0.2f, 0.2f, 0.2f);

            //Size = 100.0;
        }

        public Scene(XMLScene xmlScene)
        {
            Camera = new Camera(xmlScene.Camera);
            Ambient = new Colour(xmlScene.Ambient);
            Background = new Colour(xmlScene.Background);
            
            // or use 'Camera XMLCamera.FromXML()'

            foreach (XMLShape xmlShape in xmlScene.Shapes)
            {
                Shapes.Add(xmlShape.Create());
            }
            foreach (XMLLight xmlLight in xmlScene.Lights)
            {
                Lights.Add(xmlLight.Create());
            }
        }

        public void CreateCaches()
        {
            Camera.UpdateMatrix();

            foreach (Shape shape in Shapes)
            {
                shape.CreateCache(Camera);
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

            return hitResult;
        }

        // Return closest hit object.
        public HitResult HitTest(Camera camera, Point p1, Point p2)
        {
            Ray ray = new Ray(p1, p2);
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
        public bool HitTestFast(Camera camera, Point p1, Point p2)
        {
            Ray ray = new Ray(p1, p2);

            return HitTestFast(camera, ray);
        }

        public List<Light> IsIlluminated(Camera camera, HitResult hr)
        {
            Point pos = hr.Entry;

            List<Light> lights = new List<Light>();

            foreach (Light light in Lights)
            {
                if (!light.Enabled) continue;

                PointLight pointLight = light as PointLight;
                if (pointLight == null)
                {
                    continue;
                }

                //Point pos2 = pointLight.Pos;
                Point pos2 = camera.WorldToView * pointLight.Pos;
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
            Shapes.Add(shape);
        }

        public void AddLight(Light light)
        {
            Lights.Add(light);
        }

        public void AddMaterial(Material material)
        {
            if (!Materials.Contains(material))
            {
                Materials.Add(material);
            }
        }

        public Material GetMaterial(Guid guid)
        {
            Material material = Materials.Find(x => x.Guid == guid);

            if (material != null)
            {
                return material;
            }

            return defaultMaterial;
        }

        public virtual XMLScene ToXML()
        {
            XMLScene xml = new XMLScene();

            xml.Ambient = Ambient.ToXML();
            xml.Camera = Camera.ToXML();
            xml.Background = Background.ToXML();

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

    }
}
