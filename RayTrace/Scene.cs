using System;

using System.Collections.Generic;

namespace RayTrace
{
    public class Scene
    {
        // Size 
        //public int Size { get; }

        /*private*/ public List<Shape> Shapes = new List<Shape>();
        /*private*/ public List<Light> Lights = new List<Light>();

        public Colour Ambient { get; set; } = new Colour(0.2f, 0.2f, 0.2f);

        public Colour Background { get; set; } = Palette.Black;

        public Scene()
        {
            //Ambient = new Colour(0.2f, 0.2f, 0.2f);

            //Size = 100.0;
        }

        // Return closest hit object.
//        public HitResult HitTest(Camera camera, Point pos, Vector ray)
        public HitResult HitTest(Camera camera, Ray ray)
        {
            HitResult hitResult = new HitResult(null);

            double closest = Double.MaxValue;

            foreach (Shape shape in Shapes)
            {
                if (!shape.Enabled) continue;

//                HitResult hr = shape.HitTest(camera, pos, ray);
                HitResult hr = shape.HitTest(camera, ray);
                if (hr.HasEntry())
                {
//                    double d = pos.Distance(hr.Entry);
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
            HitResult hitResult = new HitResult(null);

            Ray ray = new Ray(p1, p2);

            double closest = Double.MaxValue;

            foreach (Shape shape in Shapes)
            {
                if (!shape.Enabled) continue;

                HitResult hr = shape.HitTest(camera, ray /*p1, p2*/);
                if (hr.HasEntry())
                {
                    double d = p1.Distance(hr.Entry);
                    if (d < closest)
                    {
                        closest = d;
                        hitResult = hr;
                    }
                }
            }

            return hitResult;
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
    }
}
