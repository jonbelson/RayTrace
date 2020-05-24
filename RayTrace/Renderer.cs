using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Net.NetworkInformation;

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

        private int _adaptiveSuperSamplingDepth = 5;
        public int AdaptiveSuperSamplingDepth { set { if (value >= 0) _adaptiveSuperSamplingDepth = value; } get { return _adaptiveSuperSamplingDepth; } }

        // Maximum recursive reflection depth when tracing reflections.
        private int _recursionDepth = 5;
        public int RecursionDepth { set { if (value >= 0) _recursionDepth = value; } get { return _recursionDepth; } }

        // Size of rendered area, so that it ranges from -Size/2 to +Size/2 in the
        //  x axis and -Size/2 to +Size/2 in the y axis.
        public double Size { get; }

        private static readonly double jitterOffset = 0.075;
        private double[,] hexArray = HexGrid.values;
        private Random rand = new Random();

        //
        // Constructor.
        //
        public Renderer()
        {
            vPos = new Point(0.0, 0.0, -focalLength);

            Size = 100.0;
        }

        private Colour CalculateIlluminationColour(Scene scene, Vector ray, HitResult hr)
        {
            Colour colour = Palette.Black;

            return colour;
        }

        // Given a vector to a Light and its notional radius, return a jittered ray.
        private Vector GetJitteredRay(int index, Vector vecToLight, double distToLight, double lightRadius)
        {
            // Calculate effective size of light rotated to Z axis.
            double theta = Math.Asin(lightRadius/distToLight);
            double circleRadius = lightRadius * Math.Cos(theta);
            double circleDistance = distToLight - lightRadius * Math.Sin(theta);

            double deltaX = jitterOffset * rand.NextDouble();
            double deltaY = jitterOffset * rand.NextDouble();
            Vector ray = new Vector();
            ray.I = circleRadius * (hexArray[index,0] + deltaX);
            ray.J = circleRadius * (hexArray[index,1] + deltaY);
            ray.K = circleDistance;

            // Project ray so Z axis aligns with vecToLight.
            double a = vecToLight.I;
            double b = vecToLight.J;
            double c = vecToLight.K;
            double mu = Math.Sqrt(b*b + c*c);

            Vector jitteredRay = new Vector();
            jitteredRay.I = mu * ray.I + a * ray.K;
            jitteredRay.J = -a * b * ray.I / mu + c * ray.J / mu + b * ray.K;
            jitteredRay.K = -a * c * ray.I / mu - b * ray.J / mu + c * ray.K;

            return jitteredRay;
        }

        private float GetSoftShadow(Camera camera, Scene scene, Point pos, Vector ray, PointLight light)
        {
            Vector L = light.Pos - pos;
            double distance = L.Magnitude();
            L.Normalise();

            int numUnblockedRays = 19;
            for (int rayNum=0; rayNum<19; rayNum++)
            {
                Vector jitteredRay = GetJitteredRay(rayNum, L, distance, light.Radius);

                Ray ray2 = new Ray(pos, jitteredRay);
                HitResult hr = scene.HitTest(camera, ray2 /*pos, jitteredRay*/);
                if (hr.HasEntry())
                {
                    numUnblockedRays--;
                }
                if (rayNum == 6)
                {
                    if (numUnblockedRays == 12)
                    {
                        numUnblockedRays = 0;
                        break;
                    }
                    else if (numUnblockedRays == 19)
                        break;
                }
            }

            float fraction = numUnblockedRays / 19.0f;

            return fraction;
        }

        // scene    Scene we are rendering.
        // ray      Incoming ray that produced HitResult.
        // hr       HitResult that detected intersection.
        private Colour CalculateColour(Camera camera, Scene scene, Vector ray, HitResult hr, int depth = 0)
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
                    var result = shape.GetRefractedVector(camera, ray, hr);

                    //Vector refracted = result.Item1;

                    Ray ray2 = new Ray(result.Item1, result.Item2);
                    HitResult hrRefracted = scene.HitTest(camera, ray2 /*result.Item1, result.Item2*/);
                    if (hrRefracted.HasEntry())
                    {
                        //Ray ray3 = new Ray(hrRefracted.Entry, result.Item2);
                        Colour c = CalculateColour(camera, scene, result.Item2, hrRefracted, depth);

                        refracted = c;
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

                    HitResult hrReflect = scene.HitTest(camera, hr.Entry, far);
                    if (hrReflect.HasEntry())
                    {
                        Colour c = CalculateColour(camera, scene, R, hrReflect, depth);
                        reflected = c;
                    }
                }
            }

            if (UseDiffuse)
            {
                List<Light> lights = scene.IsIlluminated(camera, hr);

                foreach (Light light in lights)
                {
                    Point p = hr.Entry;

                    Vector vecToLight = light.GetDirection(p).Negate();

                    // Assume light attenuatiaon is 1 over distance squared.
                    double lightAttenuation = light.GetAttenuation(p);

                    vecToLight.Normalise();

                    Vector shapeNormal = hr.EntryNormal;

                    double dpLN = vecToLight.DotProduct(shapeNormal);

                    if (dpLN > 0.0)
                    {
                        Colour incidentColour = light.Output * shape.GetColour(hr).Scale((float)dpLN);

                        incidentColour = incidentColour.Scale((float) lightAttenuation);

                        //Trace.Assert(false);
                        Debug.Assert(incidentColour.Red >= 0.0f && incidentColour.Green >= 0.0f && incidentColour.Blue >= 0.0);

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
        public void Render(Camera camera, Scene scene, Canvas canvas)
        {
            Render(camera, scene, canvas, 0, 0, canvas.Width, canvas.Height);
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

        private Colour GetSubPixelColour(Camera camera, Scene scene, Colour[,] colourBuffer, int w, int h, double x, double y, double width, double height)
        {
            double centreX = x + width / 2.0;
            double centreY = y - height / 2.0;

            Colour c0 = colourBuffer[w, h];
            Colour c1 = colourBuffer[w + 1, h];
            Colour c2 = colourBuffer[w, h + 1];
            Colour c3 = colourBuffer[w + 1, h + 1];

            if (c0 == null) { c0 = GetPixelColour(camera, scene, x, y);                     colourBuffer[w, h] = c0; }
            if (c1 == null) { c1 = GetPixelColour(camera, scene, x + width, y);             colourBuffer[w + 1, h] = c1; }
            if (c2 == null) { c2 = GetPixelColour(camera, scene, x + width, y - height);    colourBuffer[w, h + 1] = c2; }
            if (c3 == null) { c3 = GetPixelColour(camera, scene, x + width, y - height);    colourBuffer[w + 1, h + 1] = c3; }

            Colour c4 = GetPixelColour(camera, scene, centreX, centreY);

            Colour colour = c0 + c1 + c2 + c3 + c4;

            return colour.Scale(0.2f);
        }

        private Colour GetSubPixelColour(Camera camera, Scene scene, SubSample subSample, int depth)
        {
            if (depth > AdaptiveSuperSamplingDepth)
                return Palette.Black;

            double centreX = (subSample.TLx + subSample.BRx) / 2;
            double centreY = (subSample.TLy + subSample.BRy) / 2;

            var Colours = subSample.Colours;

            // Get the colours of each of the five sample points.
            if (Colours[0] == null) Colours[0] = GetPixelColour(camera, scene, subSample.TLx, subSample.TLy);
            if (Colours[1] == null) Colours[1] = GetPixelColour(camera, scene, subSample.BRx, subSample.TLy);
            if (Colours[2] == null) Colours[2] = GetPixelColour(camera, scene, subSample.TLx, subSample.BRy);
            if (Colours[3] == null) Colours[3] = GetPixelColour(camera, scene, subSample.BRx, subSample.BRy);
            if (Colours[4] == null) Colours[4] = GetPixelColour(camera, scene, centreX, centreY);

            Colour col04, col14, col24, col34;

            // Compare each corner to the centre. If they're similar, average them else subdivide further.
            if (Colours[0].IsSimilar(Colours[4]))
            {
                col04 = Colour.Average(Colours[0], Colours[4]);
            }
            else
            {
                SubSample subSubSample = new SubSample(subSample.TLx, subSample.TLy, centreX, centreY);
                subSubSample.Colours[0] = Colours[0];
                subSubSample.Colours[3] = Colours[4];
                col04 = GetSubPixelColour(camera, scene, subSubSample, depth + 1);
            }

            if (Colours[1].IsSimilar(Colours[4]))
            {
                col14 = Colour.Average(Colours[1], Colours[4]);
            }
            else
            {
                SubSample subSubSample = new SubSample(centreX, centreY, subSample.BRx, subSample.TLy);
                subSubSample.Colours[1] = Colours[1];
                subSubSample.Colours[2] = Colours[4];
                col14 = GetSubPixelColour(camera, scene, subSubSample, depth + 1);
            }

            if (Colours[2].IsSimilar(Colours[4]))
            {
                col24 = Colour.Average(Colours[2], Colours[4]);
            }
            else
            {
                SubSample subSubSample = new SubSample(subSample.TLx, subSample.BRy, centreX, centreY);
                subSubSample.Colours[1] = Colours[4];
                subSubSample.Colours[2] = Colours[2];
                col24 = GetSubPixelColour(camera, scene, subSubSample, depth + 1);
            }

            if (Colours[3].IsSimilar(Colours[4]))
            {
                col34 = Colour.Average(Colours[3], Colours[4]);
            }
            else
            {
                SubSample subSubSample = new SubSample(centreX, centreY, subSample.BRx, subSample.BRy);
                subSubSample.Colours[4] = Colours[0];
                subSubSample.Colours[3] = Colours[3];
                col34 = GetSubPixelColour(camera, scene, subSubSample, depth + 1);
            }

            // Average the four colours.
            Colour colour = col04 + col14 + col24 + col34;
            colour = colour.Scale(0.25f);

            return colour;
        }

        // Get the colour of specified pixel by casting a ray into the Scene.
        private Colour GetPixelColour(Camera camera, Scene scene, double x, double y)
        {
            Colour colour = scene.Background;   // new Colour();
//           Colour colour = new Colour();

            Vector ray = new Vector(x, y, focalLength, true);

            Ray r = new Ray(vPos, ray);

            //Matrix m = new Matrix();
            //m.SetLookAt(new Point(1, 0, 0), new Point(1, 0, 10));

            //ray = m * ray;

            HitResult hr = scene.HitTest(camera, r /*vPos, ray*/);
            if (hr.HasEntry())
            {
                colour = CalculateColour(camera, scene, ray, hr);
            }

            return colour;
        }

        // Render specified block of Scene to the Canvas.
        public void Render(Camera camera, Scene scene, Canvas canvas, int startX, int startY, int numColumns, int numRows)
        {
            int width = canvas.Width;
            int height = canvas.Height;

            if (startY + numRows > height)
            {
                numRows = height - startY;
            }

            //  Buffer to store the calculated Colour values for each corner of each pixel.
            Colour[,] colourBuffer = new Colour[numColumns + 1, numRows + 1];

            double sceneStartX = -Size / 2.0;
            double sceneStartY = Size / 2.0;
            double sceneStepX = Size / width;       // Step per pixel in Scene units.
            double sceneStepY = Size / height;

            double sceneY = sceneStartY;
            sceneY -= startY * sceneStepY;

            for (int h = startY; h < startY + numRows; h++)
            {
                double sceneX = sceneStartX;

                sceneX += startX * sceneStepX; 
                for (int w=startX; w<startX + numColumns; w++)
                {
                    //Debug.Assert(h != height/2 || w != width/2);

                    if (AntiAlias == AntiAliasType.None)
                    {
                        Colour colour = GetPixelColour(camera, scene, sceneX + sceneStepX / 2, sceneY - sceneStepY / 2);
                        colour.Clamp();
                        canvas.Set(w, h, colour.ToRGB());
                    }
                    else if (AntiAlias == AntiAliasType.SuperSampling)
                    {
                        //SubSample subSample = new SubSample(x, y, x + sceneStepX, y - sceneStepY);
                        Colour colour = GetSubPixelColour(camera, scene, colourBuffer, w - startX, h - startY, sceneX, sceneY, /*sceneX +*/ sceneStepX, /*sceneY -*/ sceneStepY);
                        colour.Clamp();
                        canvas.Set(w, h, colour.ToRGB());
                    }
                    else
                    {
                        SubSample subSample = new SubSample(sceneX, sceneY, sceneX + sceneStepX, sceneY - sceneStepY);
                        Colour colour = GetSubPixelColour(camera, scene, subSample, 1);
                        colour.Clamp();
                        canvas.Set(w, h, colour.ToRGB());
                    }

                    sceneX += sceneStepX;
                }

                sceneY -= sceneStepY;
            }

        }
    }
}
