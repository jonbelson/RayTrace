using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RayTrace
{
    // Negative Z axis towards observer.
    public class Renderer
    {
        // Distance of observer along Z axis.
        private double focalLength = 500.0;

        // Position of viewer.
        private Point vPos;

        public bool UseLighting { set; get; } = true;
        public bool UsePhong { set; get; } = true;
        public bool UseReflections { get; set; } = true;

        // Maximum recursive reflection depth when tracing reflections.
        private int _recursionDepth = 10;
        public int RecursionDepth { set { if (value >= 0) _recursionDepth = value; } get { return _recursionDepth; } }

        // Size of rendered area, so that it ranges from -Size/2 to +Size/2 in the
        //  x axis and -Size/2 to +Size/2 in the y axis.
        public double Size { get; }

        public Renderer()
        {
            vPos = new Point(0.0, 0.0, -focalLength);

            Size = 100.0;
        }

        // scene    Scene we are rendering.
        // ray      Incoming ray that produced HitResult.
        // hr       HitResult that detected intersection.
        private Colour CalculateColour(Scene scene, Vector ray, HitResult hr, int depth = 1)
        {
            depth++;
            if (depth > RecursionDepth) return Palette.Black;

            Shape shape = hr.shape;
            //Colour colour = shape.GetColour(hr).Multiply(scene.Ambient);
            //Colour colour = Palette.Black;

            //((Sphere)shape).CalculateUV(hr);
            Colour colour = shape.GetColour(hr).Multiply(scene.Ambient);

            if (UseReflections)
            {
                // Get reflection angle 'R'.
                //Vector vecToViewer = new Vector(-x, -y, -focalLength, true);
                Vector vecToViewer = ray.Scale(-1.0f);

                double dpVE = vecToViewer.DotProduct(hr.EntryNormal);

                Vector R = hr.EntryNormal.Scale(2.0 * dpVE) - vecToViewer;
                R.Normalise();

                // Mkae point in direction of 'R' to use as an endpoint.
                Vector v = R.Scale(1000.0f);
                Point far = hr.Entry.Offset(v);

                HitResult hrReflect = scene.HitTest(hr.Entry, far);
                if (hrReflect.HasEntry())
                {
                    Colour c = CalculateColour(scene, R, hrReflect, depth);
                    colour += c.Scale(0.5f);
                    //colour += hrReflect.shape.Colour * 0.5f;
                }

            }

            if (UseLighting)
            {

                List<Light> lights = scene.IsIlluminated(hr);
                if (lights.Count == 2)
                {
                    int i = 0;
                }
                foreach (Light light in lights)
                {
                    Point p = hr.Entry;
                    Vector vecToLight = p.Difference(light.Pos);
                    vecToLight.Normalise();

                    Vector shapeNormal = hr.EntryNormal;

                    double dpLN = vecToLight.DotProduct(shapeNormal);

                    if (dpLN > 0.0)
                    {
                        Colour incidentColour = light.Colour * shape.GetColour(hr).Scale((float)dpLN);

                        //Trace.Assert(false);
                        Debug.Assert(incidentColour.Red >= 0.0f && incidentColour.Green >= 0.0f && incidentColour.Blue >= 0.0);

                        if (!(incidentColour.Red >= 0.0f && incidentColour.Green >= 0.0f && incidentColour.Blue >= 0.0))
                        {
                            int k = 0;
                        }

                        colour = colour.Add(incidentColour);
                    }

                    if (UsePhong)
                    {
                        // Phong shading.
                        Vector R = shapeNormal.Scale(2.0 * dpLN) - vecToLight;
                        R.Normalise();

                        //Vector vecToViewer = new Vector(-x, -y, -focalLength);
                        //vecToViewer.Normalise();
                        Vector vecToViewer = ray.Scale(-1.0f);


                        double Il = light.Intensity;// 1.0;    // Intensity of light source.
                        double dpVR = vecToViewer.DotProduct(R);

                        // Ignore negative dp as it means the vectors are pointing away from each other.
                        if (dpVR > 0.0)
                        {
                            //double k = 0.5;     // Specular reflection coefficient/constant.
                            //double ns = 4.0;    // 'Shininess' of surface.

                            double k = shape.Material.k;    // 0.5;     // Specular reflection coefficient/constant.
                            double ns = shape.Material.ns;  // 4.0;    // 'Shininess' of surface.

                            double d = Math.Pow(dpVR, ns);

                            double Ispec = k * Il * d;
                            //Ispec = Math.Pow(Ispec, ns);

                            //Vector E = new Vector(-x, -y, -focalLength);
                            //E.Normalise();
                            //double s = Math.Pow(Math.Max(0.0, E.DotProduct(R)), 1.2);

                            Colour spec = new Colour(1f, 1f, 1f);
                            //spec = light.Colour;
                            colour = colour + spec * (float)Ispec;
                        }
                    }

                }
            }

            return colour;
        }

        public void Render(Scene scene, Canvas canvas)
        {
            int width = canvas.Width;
            int height = canvas.Height;

            double sceneStartX = -Size / 2.0;
            double sceneStartY = Size / 2.0;
            double sceneStepX = Size / width;
            double sceneStepY = Size / height;

            double y = sceneStartY;
            for (int h=0; h<height; h++)
            {
                double x = sceneStartX;
                for (int w=0; w<width; w++)
                {
                    // Make ray from viewpoint to projection plane.
                    Vector ray = new Vector(x, y, focalLength, true);

                    HitResult hr = scene.HitTest(vPos, ray);
                    if (hr.HasEntry())
                    {
                        Shape shape = hr.shape;

                        Colour colour = CalculateColour(scene, ray, hr);

                        //colour += shape.Colour.Multiply(scene.Ambient);
                        
                        colour.Clamp();
                        //colour.Normalise();
                        //colour = colour.Exposure(1.2f);
                        canvas.Set(w, h, colour.ToRGB());

                        //canvas.buffer[h*width + w] = colour.ToRGB();
                    }

                    x += sceneStepX;
                }

                y -= sceneStepY;
            }

        }
    }
}
