using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RayTrace
{
    // Negative Z axis towards observer.
    public class Renderer
    {
        public enum AntiAliasType { None, SuperSampling, AdaptiveSupersampling }

        // Distance of observer along Z axis.
        private double focalLength = 500.0;

        // Position of viewer.
        private Point vPos;

        public bool UseAmbient { set; get; } = true;
        public bool UseDiffuse { set; get; } = true;
        public bool UsePhong { set; get; } = true;
        public bool UseReflections { get; set; } = true;
        public bool UseRefraction { get; set; } = true;

        public AntiAliasType AntiAlias { get; set; } = AntiAliasType.None;

        // Maximum recursive reflection depth when tracing reflections.
        private int _recursionDepth = 5;
        public int RecursionDepth { set { if (value >= 0) _recursionDepth = value; } get { return _recursionDepth; } }

        // Size of rendered area, so that it ranges from -Size/2 to +Size/2 in the
        //  x axis and -Size/2 to +Size/2 in the y axis.
        public double Size { get; }

        // Constructor.
        public Renderer()
        {
            vPos = new Point(0.0, 0.0, -focalLength);

            Size = 100.0;
        }

        // scene    Scene we are rendering.
        // ray      Incoming ray that produced HitResult.
        // hr       HitResult that detected intersection.
        private Colour CalculateColour(Scene scene, Vector ray, HitResult hr, int depth = 0)
        {
            Debug.Assert(hr.HasEntry());

            depth++;
            if (depth > RecursionDepth) return Palette.Black;

            Shape shape = hr.shape;

            Colour ambient = Palette.Black;
            Colour refracted = Palette.Black;
            Colour reflected = Palette.Black;
            Colour diffuse = Palette.Black;
            Colour specular = Palette.Black;

            if (UseAmbient)
            {
                ambient = shape.GetColour(hr).Multiply(scene.Ambient);
            }

            if (UseRefraction)
            {
                if (shape.HasTransparency())
                {
                    // Hit object refracts, so work out direction of refracted ray (in then out) and check if it hits anything.
                    var result = shape.GetRefractedVector(ray, hr);

                    //Vector refracted = result.Item1;

                    HitResult hrRefracted = scene.HitTest(result.Item1, result.Item2);
                    if (hrRefracted.HasEntry())
                    {
                        Colour c = CalculateColour(scene, result.Item2, hrRefracted, depth);

                        refracted = c;

                        //colour += c;
                    }
                }
            }

            if (UseReflections)
            {
                if (shape.HasReflectivity())
                {
                    // Get reflection angle 'R'.
                    //Vector vecToViewer = new Vector(-x, -y, -focalLength, true);
                    Vector vecToViewer = ray.Scale(-1.0f);

                    double dpVE = vecToViewer.DotProduct(hr.EntryNormal);

                    Vector R = hr.EntryNormal.Scale(2.0 * dpVE) - vecToViewer;
                    R.Normalise();

                    // Make point in direction of 'R' to use as an endpoint.
                    Vector v = R.Scale(1000.0f);
                    Point far = hr.Entry.Offset(v);

                    HitResult hrReflect = scene.HitTest(hr.Entry, far);
                    if (hrReflect.HasEntry())
                    {
                        Colour c = CalculateColour(scene, R, hrReflect, depth);
                        //colour += c.Scale(0.5f);
                        reflected = c;
                        //colour += hrReflect.shape.Colour * 0.5f;
                    }
                }
            }

            if (UseDiffuse)
            {
                List<Light> lights = scene.IsIlluminated(hr);

                foreach (Light light in lights)
                {
                    Point p = hr.Entry;
                    Vector vecToLight = p.Difference(light.Pos);

                    // Assume light attenuatiaon is 1 over distance squared.
                    double lightAttenuation = 1.0 / vecToLight.MagnitudeSquared();

                    vecToLight.Normalise();

                    Vector shapeNormal = hr.EntryNormal;

                    double dpLN = vecToLight.DotProduct(shapeNormal);

                    if (dpLN > 0.0)
                    {
                        Colour incidentColour = light.Output * shape.GetColour(hr).Scale((float)dpLN);

                        incidentColour = incidentColour.Scale((float) lightAttenuation);

                        //Trace.Assert(false);
                        Debug.Assert(incidentColour.Red >= 0.0f && incidentColour.Green >= 0.0f && incidentColour.Blue >= 0.0);

                        //colour = colour.Add(incidentColour);
                        diffuse += incidentColour;
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
                            double k = shape.Material.ks;    // 0.5;     // Specular reflection coefficient/constant.
                            double ns = shape.Material.ns;  // 4.0;    // 'Shininess' of surface.

                            double d = Math.Pow(dpVR, ns);

                            double Ispec = k * Il * d;
                            //Ispec = Math.Pow(Ispec, ns);

                            //Vector E = new Vector(-x, -y, -focalLength);
                            //E.Normalise();
                            //double s = Math.Pow(Math.Max(0.0, E.DotProduct(R)), 1.2);

                            Colour spec = new Colour(1f, 1f, 1f);
                            //spec = light.Colour;
                            double factor = Ispec * lightAttenuation;
                            Colour specularColour = spec * (float)factor;
                            //colour += specularColour;

                            specular += specularColour;
                        }
                    }

                }
            }

            // Combine all the colour components.
            Colour colour = ambient + diffuse + specular;

            float transparency = shape.Material.Transparency;
            colour += refracted.Scale(transparency) + reflected.Scale(1.0f - transparency);

            return colour;
        }

        // Render entire Scene to the Canvas.
        public void Render(Scene scene, Canvas canvas)
        {
            Render(scene, canvas, 0, 0, canvas.Width, canvas.Height);
        }

        struct SubSample
        {
            public double TLx;
            public double TLy;
            public double BRx;
            public double BRy;
            public Colour[] Colours;

            // 0     1
            //
            //    4
            //
            // 2     3
            public SubSample(double tlX, double tlY, double brX, double brY)
            {
                TLx = tlX;
                TLy = tlY;
                BRx = brX;
                BRy = brY;
                Colours = new Colour[5];
            }
        }

        private Colour GetSubPixelColour(Scene scene, SubSample subSample)
        {
            double centreX = (subSample.TLx + subSample.BRx) / 2;
            double centreY = (subSample.TLy + subSample.BRy) / 2;

            var Colours = subSample.Colours;

            // Get the colours of each of the five sample points.
            Colours[0] = GetPixelColour(scene, subSample.TLx, subSample.TLy);
            Colours[1] = GetPixelColour(scene, subSample.BRx, subSample.TLy);
            Colours[2] = GetPixelColour(scene, subSample.TLx, subSample.BRy);
            Colours[3] = GetPixelColour(scene, subSample.BRx, subSample.BRy);
            Colours[4] = GetPixelColour(scene, centreX, centreY);

            Colour colour = Palette.Black;
            for (int i=0; i<5; i++)
            {
                colour += Colours[i];
            }

            return colour.Scale(0.2f);
        }

        private Colour GetSubPixelColour(Scene scene, SubSample subSample, int depth)
        {
            if (depth > 4) return Palette.Black;

            double centreX = (subSample.TLx + subSample.BRx) / 2;
            double centreY = (subSample.TLy + subSample.BRy) / 2;

            var Colours = subSample.Colours;

            // Get the colours of each of the five sample points.
            Colours[0] = GetPixelColour(scene, subSample.TLx, subSample.TLy);
            Colours[1] = GetPixelColour(scene, subSample.BRx, subSample.TLy);
            Colours[2] = GetPixelColour(scene, subSample.TLx, subSample.BRy);
            Colours[3] = GetPixelColour(scene, subSample.BRx, subSample.BRy);
            Colours[4] = GetPixelColour(scene, centreX, centreY);

            Colour col04;
            Colour col14;
            Colour col24;
            Colour col34;

            // Compare each corner to the centre. If they're simialr, average them else subdivide further.
            if (Colours[0].IsSimilar(Colours[4]))
            {
                col04 = Colour.Average(Colours[0], Colours[4]);
            }
            else
            {
                SubSample subSubSample = new SubSample(subSample.TLx, subSample.TLy, centreX, centreY);
                col04 = GetSubPixelColour(scene, subSubSample, depth + 1);
            }

            if (Colours[1].IsSimilar(Colours[4]))
            {
                col14 = Colour.Average(Colours[1], Colours[4]);
            }
            else
            {
                SubSample subSubSample = new SubSample(centreX, subSample.TLy, subSample.BRx, subSample.TLy);
                col14 = GetSubPixelColour(scene, subSubSample, depth + 1);
            }

            if (Colours[2].IsSimilar(Colours[4]))
            {
                col24 = Colour.Average(Colours[2], Colours[4]);
            }
            else
            {
                SubSample subSubSample = new SubSample(subSample.TLx, subSample.BRy, centreX, centreY);
                col24 = GetSubPixelColour(scene, subSubSample, depth + 1);
            }

            if (Colours[3].IsSimilar(Colours[4]))
            {
                col34 = Colour.Average(Colours[3], Colours[4]);
            }
            else
            {
                SubSample subSubSample = new SubSample(centreX, centreY, subSample.BRx, subSample.BRy);
                col34 = GetSubPixelColour(scene, subSubSample, depth + 1);
            }

            // Average the four colours.
            Colour colour = col04 + col14 + col24 + col34;
            colour = colour.Scale(0.25f);

            return colour;
        }

        // Get the colour of specified pixel by casting a ray into the Scene.
        private Colour GetPixelColour(Scene scene, double x, double y)
        {
            Colour colour = new Colour();

            Vector ray = new Vector(x, y, focalLength, true);

            HitResult hr = scene.HitTest(vPos, ray);
            if (hr.HasEntry())
            {
                colour = CalculateColour(scene, ray, hr);
            }

            return colour;
        }

            // Render specified block of Scene to the Canvas.
        public void Render(Scene scene, Canvas canvas, int startX, int startY, int numColumns, int numRows)
        {
            int width = canvas.Width;
            int height = canvas.Height;

            if (startY + numRows > height)
            {
                numRows = height - startY;
            }

            double sceneStartX = -Size / 2.0;
            double sceneStartY = Size / 2.0;
            double sceneStepX = Size / width;
            double sceneStepY = Size / height;

            double y = sceneStartY;
            y -= startY * sceneStepY;

            for (int h = startY; h < startY + numRows; h++)
            {
                double x = sceneStartX;

                x += startX * sceneStepX; 
                for (int w=startX; w<startX + numColumns; w++)
                {
                    //Debug.Assert(h != height/2 || w != width/2);

                    if (AntiAlias == AntiAliasType.None)
                    {
                        Colour colour = GetPixelColour(scene, x + sceneStepX / 2, y + sceneStepY / 2);
                        colour.Clamp();
                        canvas.Set(w, h, colour.ToRGB());
                    }
                    else if (AntiAlias == AntiAliasType.SuperSampling)
                    {
                        SubSample subSample = new SubSample(x, y, x + sceneStepX, y - sceneStepY);
                        Colour colour = GetSubPixelColour(scene, subSample);
                        colour.Clamp();
                        canvas.Set(w, h, colour.ToRGB());
                    }
                    else
                    {
                        SubSample subSample = new SubSample(x, y, x + sceneStepX, y - sceneStepY);
                        Colour colour = GetSubPixelColour(scene, subSample, 1);
                        colour.Clamp();
                        canvas.Set(w, h, colour.ToRGB());
                    }

                    /*
                                        // Make ray from viewpoint to projection plane.
                                        Vector ray = new Vector(x + sceneStepX/2, y + sceneStepY/2, focalLength, true);

                                        HitResult hr = scene.HitTest(vPos, ray);
                                        if (hr.HasEntry())
                                        {
                                            Shape shape = hr.shape;

                                            Colour colour = CalculateColour(scene, ray, hr);

                                            colour.Clamp();
                                            //colour.Normalise();
                                            //colour = colour.Exposure(1.2f);
                                            canvas.Set(w, h, colour.ToRGB());

                                            //canvas.buffer[h*width + w] = colour.ToRGB();
                                        }
                                        */
                    x += sceneStepX;
                }

                y -= sceneStepY;
            }

        }
    }
}
