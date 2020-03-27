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

        public Colour Ambient { get; set; }

        public Scene()
        {
            Ambient = new Colour(0.2f, 0.2f, 0.2f);

            //Size = 100.0;
        }

        public HitResult HitTest(Point pos, Vector ray)
        {
            //Shape nearest = null;
            double distance = 1000000.0;
            HitResult hitResult = new HitResult(null);

            foreach (Shape shape in Shapes)
            {
                HitResult hr = shape.HitTest(pos, ray);
                if (hr.HasEntry())
                {
                    double d = pos.Distance(hr.Entry);
                    if (d < distance)
                    {
                        distance = d;
                        //nearest = shape;
                        hitResult = hr;
                    }
                }
            }

            return hitResult;
        }
        public HitResult HitTest(Point p1, Point p2)
        {
            HitResult hitResult = new HitResult(null);

            foreach (Shape shape in Shapes)
            {
                HitResult hr = shape.HitTest(p1, p2);
                if (hr.HasEntry())
                {
                    return hr;
                }
            }

            return hitResult;
        }

        public List<Light> IsIlluminated(HitResult hr)
        {
            Point pos = hr.Entry;

            List<Light> lights = new List<Light>();

            foreach (Light light in Lights)
            {
                Point pos2 = light.Pos;
                Vector v = light.Pos.Difference(pos);

                //Vector step = light.Pos.Difference(pos);
                //step.Normalise();
                //pos = pos.Offset(step);

                HitResult hitResult = HitTest(pos, pos2);
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
