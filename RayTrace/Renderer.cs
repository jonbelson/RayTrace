using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

namespace RayTrace
{
    // Negative Z axis towards observer.
    public class Renderer
    {
        public enum AntiAliasType { None, SuperSampling, AdaptiveSupersampling }

        // Distance of observer along Z axis.
        //private double focalLength = 500.0;

        // Position of viewer.
        //private Point vPos;

        public bool UseAmbient { set; get; } = true;
        public bool UseDiffuse { set; get; } = true;
        public bool UsePhong { set; get; } = true;
        public bool UseReflections { get; set; } = true;
        public bool UseRefraction { get; set; } = true;
        public bool UseSoftShadows { get; set; } = true;
        public bool UseDepthOfField { get; set; } = true;

        public AntiAliasType AntiAlias { get; set; } = AntiAliasType.None;

        private int _adaptiveSuperSamplingDepth = 5;
        public int AdaptiveSuperSamplingDepth { set { if (value >= 0) _adaptiveSuperSamplingDepth = value; } get { return _adaptiveSuperSamplingDepth; } }

        // Maximum recursive reflection depth when tracing reflections.
        private int _recursionDepth = 5;
        public int RecursionDepth { set { if (value >= 0) _recursionDepth = value; } get { return _recursionDepth; } }

        // Size of rendered area, so that it ranges from -Size/2 to +Size/2 in the
        //  x axis and -Size/2 to +Size/2 in the y axis.
        public double Size { get; set; }

        //private static readonly double jitterOffset = 0.075;
        private static readonly double jitterOffset = HexGrid.Jitter;   // 0.075;
        private double[,] hexArray = HexGrid.values;
        //private Random rand = new Random();

        //
        // Constructor.
        //
        public Renderer()
        {
//            vPos = new Point(0.0, 0.0, -focalLength);
            //vPos = new Point(0.0, 0.0, 0.0);

            Size = 100.0;

            //HexGrid.DumpGrid();
        }

        private Colour CalculateIlluminationColour(Scene scene, Vector ray, HitResult hr)
        {
            Colour colour = Palette.Black;

            return colour;
        }

        private Point GetPointOnDisc(Random rand)
        {
            double x, y;
            do
            {
                //double r1 = rand.NextDouble();
                //double r2 = rand.NextDouble();
                x = -1.0 + 2.0 * rand.NextDouble();
                y = -1.0 + 2.0 * rand.NextDouble();
            } while (x * x + y * y > 1.0);

            return new Point(x, y, 0.0);
        }

        private Vector GetVectorOnDisc(Random rand)
        {
            Point p = GetPointOnDisc(rand);

            return new Vector(p.X, p.Y, 0.0);
        }

        // Based on 'AimZ()' function from 'RenderMan Companion' page 143.
        private Vector Aim(Vector vecToLight)
        {
            double yrot;
            double xzlen = Math.Sqrt(vecToLight.I * vecToLight.I + vecToLight.K * vecToLight.K);
            if (xzlen == 0.0)
            {
                yrot = (vecToLight.J< 0.0) ? Math.PI : 0.0;
            }
            else
            {
                yrot = Math.Acos(vecToLight.K/xzlen);
            }

            double yzlen = Math.Sqrt(vecToLight.J * vecToLight.J + xzlen * xzlen);
            double xrot = Math.Acos(xzlen / yzlen);

            // Set direcion of rotation appropriately.
            if (vecToLight.J <= 0.0) xrot = -xrot;
            if (vecToLight.I > 0.0) yrot = -yrot;


            Matrix xrotation = new Matrix();
            xrotation.SetRotationX(xrot);

            Matrix yrotation = new Matrix();
            yrotation.SetRotationY(yrot);

            Vector test = new Vector(vecToLight.I, vecToLight.J, vecToLight.K);

            Vector v1 = yrotation * test;

            Vector v2 = xrotation * v1;

            Matrix align = yrotation * xrotation;

            Vector test2 = align * test;


            // Create matricies to rotation from Z axis to vecToLight.
            Matrix xrotation2 = new Matrix();
            xrotation2.SetRotationX(-xrot);

            Matrix yrotation2 = new Matrix();
            yrotation2.SetRotationY(-yrot);

            Matrix align2 = xrotation2 * yrotation2;

            Vector zvec = new Vector(0, 0, 1);

            Vector test3 = align2 * zvec;

            // Transpose of 'pure rotation' LookAt() matrix effectively reverses direction of rotations.
            Matrix lookAt2 = new Matrix();
            lookAt2.SetLookAt(new Point(0, 0, 0), new Point(vecToLight.I, vecToLight.J, vecToLight.K));
            lookAt2.Transpose();

            Vector test4 = lookAt2 * zvec;

            return v2;
        }


        // Given a vector to a Light and its notional radius, return a jittered ray.
        // matrix       Matrix to rotate Z axis to align with light vector.
        // vecToLight   Normalised vector to light.
        private Vector GetJitteredRay(Matrix matrix, int index, Vector vecToLight, double distToLight, double lightRadius, Random rand)
        {
            // Calculate effective size of light from view point.
            double theta = Math.Asin(lightRadius/distToLight);
            double circleRadius = lightRadius * Math.Cos(theta);
            double circleDistance = distToLight - lightRadius * Math.Sin(theta);

            double deltaX = jitterOffset * (-1.0 + 2*rand.NextDouble());
            double deltaY = jitterOffset * (-1.0 + 2*rand.NextDouble());
            //double deltaX = jitterOffset * rand.NextDouble();
            //double deltaY = jitterOffset * rand.NextDouble();

            Debug.Assert(!Double.IsNaN(deltaX));
            Debug.Assert(!Double.IsNaN(deltaY));

            Vector ray = new Vector();
            ray.I = circleRadius * (hexArray[index,0] + deltaX);
            ray.J = circleRadius * (hexArray[index,1] + deltaY);
            ray.K = circleDistance;

            Debug.Assert(!Double.IsNaN(ray.I));
            Debug.Assert(!Double.IsNaN(ray.J));
            Debug.Assert(!Double.IsNaN(ray.K));


            /*
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
            */

            // Transpose of 'pure rotation' LookAt() matrix effectively reverses direction of rotations.
            //Matrix lookAt2 = new Matrix();
            //lookAt2.SetLookAt(new Point(0, 0, 0), new Point(vecToLight.I, vecToLight.J, vecToLight.K));
            //lookAt2.Transpose();

            //            Vector jitteredDir = lookAt2*jitteredRay;
            //            Vector jitteredDir = lookAt2 * ray;
            
            // Rotate the jittered vector (which is relative to the Z axis) to be aligned with the original light vector.
            Vector jitteredDir = matrix * ray;

            Debug.Assert(!Double.IsNaN(jitteredDir.I));
            Debug.Assert(!Double.IsNaN(jitteredDir.J));
            Debug.Assert(!Double.IsNaN(jitteredDir.K));

            return jitteredDir;
            
            //// Find magnitude of vecToLight projected on the YZ plane
            //double H = vecToLight.Magnitude();
            //double A = vecToLight.I;
            //double yzLen = Math.Sqrt(H*H - A*A);

            //// Get angle between projected vector and XZ plane.
            //double sinPhi = vecToLight.J/yzLen;
            //double phi = Math.Asin(sinPhi);

            //// phi is rotation around X axis.

            //double cosTheta = yzLen / H;
            //double theta2 = Math.Acos(cosTheta);

            //// theta2 is rotation around Y axis.

            //Matrix m = new Matrix();
            //m.SetRotationX(-phi);

            //Vector v1 = m * new Vector(0, 0, 1);

            ////double dotProduct = vecToLight.Angle(new Vector(0, 0, 1));

            //Matrix m2 = new Matrix();
            //m2.SetRotationY(phi /*theta2*/);

            //Matrix m3 = m * m2;

            //Vector v2 = m2 * v1;

            //// TEST - unit vector down Z axis should align with vecToLight. 
            //Vector vv = m3 * new Vector(0, 0, 1);

            //return jitteredRay;

        }

        // scene    Scene we are rendering.
        // ray      Incoming ray that produced HitResult.
        private float GetSoftShadow(Camera camera, Scene scene, Point pos, Vector ray, PointLight light)
        {
            Random rand = new Random(123456);

            Vector L = light.Pos - pos;
            double distance = L.Magnitude();
            L = L.Normalise();

            Matrix lookAt = new Matrix();
            lookAt.SetLookAt(new Point(0, 0, 0), new Point(L.I, L.J, L.K));
            //            lookAt2.SetLookAt(new Point(0, 0, 0), new Point(jitteredRay.I, jitteredRay.J, jitteredRay.K));
            lookAt.Transpose();

            // Do more interations of the hex grid for 'wider' PointLights.
            int numIterations = light.Radius;

            int maxIterations = 19 * numIterations;

            int totalNumUnblockedRays = maxIterations;
            for (int i = 0; i < numIterations; i++)
            {
                int numUnblockedRays = 19;
                for (int rayNum = 0; rayNum < 19; rayNum++)
                {
                    Vector jitteredRay = GetJitteredRay(lookAt, rayNum, L, distance, light.Radius, rand);

                    Ray ray2 = new Ray(pos, jitteredRay);
                    HitResult hr = scene.HitTest(camera, ray2);
                    if (hr.HasEntry())
                    {
                        Debug.Assert(hr.shape is Sphere);

                        numUnblockedRays--;
                        totalNumUnblockedRays--;
                    }
                    /*
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
                    */
                }
                //totalNumUnblockedRays -= (19 - numUnblockedRays);
            }
            float fraction = ((float) totalNumUnblockedRays) / maxIterations;

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
                    else
                    {
                        // Refraction didn't hit anything, so just use the Scene backgorund colour.
                        refracted = scene.Background;
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
                    R = R.Normalise();

                    // Make point in direction of 'R' to use as an endpoint.
                    Vector v = R.Scale(1000.0f);
                    Point far = hr.Entry.Offset(v);

                    HitResult hrReflect = scene.HitTest(camera, hr.Entry, far);
                    if (hrReflect.HasEntry())
                    {
                        Colour c = CalculateColour(camera, scene, R, hrReflect, depth);
                        reflected = c;
                    }
                    else
                    {
                        // Reflection didn't hit anything, so just use the Scene backgorund colour.
                        reflected = scene.Background;
                    }
                }
            }

            if (UseDiffuse)
            {
                foreach (Light light in scene.Lights)
                {
                    if (!light.Enabled) continue;

                    PointLight pointLight = light as PointLight;
                    if (pointLight == null)
                    {
                        continue;
                    }

                    float visibleFraction = 0.0f;
                    if (UseSoftShadows)
                    {
                        visibleFraction = GetSoftShadow(camera, scene, hr.Entry, ray, pointLight);
                    }
                    else
                    {
                        Point pos = hr.Entry;
                        Point pos2 = camera.WorldToView * pointLight.Pos;

                        HitResult hitResult = scene.HitTest(camera, pos, pos2);
                        if (hitResult.HasEntry())
                        {
                            continue;
                        }
                        visibleFraction = 1.0f;

                    }
                    if (visibleFraction == 0.0f)
                    {
                        continue;
                    }

                    Point p = hr.Entry;
                    Vector vecToLight = light.GetDirection(camera, p).Negate();

                    // Assume light attenuation is 1 over distance squared.
                    double lightAttenuation = light.GetAttenuation(camera, p);

                    vecToLight = vecToLight.Normalise();

                    Vector shapeNormal = hr.EntryNormal;

                    double dpLN = vecToLight.DotProduct(shapeNormal);

                    if (dpLN > 0.0)
                    {
                        Colour incidentColour = light.Output * shape.GetColour(hr).Scale((float)dpLN);

                        incidentColour = incidentColour.Scale((float) lightAttenuation);

                        incidentColour = incidentColour.Scale(visibleFraction);

                        //Trace.Assert(false);
                        Debug.Assert(incidentColour.Red >= 0.0f && incidentColour.Green >= 0.0f && incidentColour.Blue >= 0.0);

                        diffuse += incidentColour;
                    }

                    if (UsePhong)
                    {
                        // Phong shading.
                        Vector R = shapeNormal.Scale(2.0 * dpLN) - vecToLight;
                        R = R.Normalise();

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
                            //E = E.Normalise();
                            //double s = Math.Pow(Math.Max(0.0, E.DotProduct(R)), 1.2);

                            Colour spec = new Colour(1f, 1f, 1f);
                            //spec = light.Colour;
                            double factor = Ispec * lightAttenuation;
                            Colour specularColour = spec * (float)factor;

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
            Colour colour = scene.Background;

            //Vector ray = new Vector(x, y, 1, true);

            //http://cg.skeelogy.com/depth-of-field-using-raytracing/

            if (UseDepthOfField)
            {
                Vector viewVector = camera.GetViewVector();
                double d = 1.0;
                Vector viewToPixel = new Vector(x, y, 1);
                double dtick = viewToPixel.Magnitude();
                Vector v = viewToPixel.Normalise();
                double f = camera.GetViewVector().Magnitude();

                Point e = new Point(0.0, 0.0, 0.0);
                Point P = e + v * (dtick / (d / (d + f)));  // Point on focal plane.
                double aperture = 30.0;// 50.0;

                //Colour sum = Palette.Black;

//                Random rand = new Random(System.Threading.Thread.CurrentThread.ManagedThreadId);
                Random rand = new Random(1234);

                double sceneWidth = camera.GetSceneWidth();
                double pixelWidth = sceneWidth / 800.0;

                // Create rays from area around image pixel to P.
                int numSamples = 20;
                for (int i = 0; i < numSamples; i++)
                {
                    Point o = new Point(x, y, 1.0);
                    Vector offset = GetVectorOnDisc(rand) * aperture * pixelWidth;
                    o += offset;
                    Vector dir = P - o;

                    Ray r = new Ray(o, dir);
                    HitResult hit = scene.HitTest(camera, r);
                    if (hit.HasEntry())
                    {
                        colour += CalculateColour(camera, scene, dir, hit);
                    }
                }

                colour = colour.Scale(1.0f / numSamples);
            }
            else
            {
                Point origin = new Point(0.0, 0.0, 0.0);
                Vector dir = new Vector(x, y, 1);
                Ray ray = new Ray(origin, dir);

                HitResult hr = scene.HitTest(camera, ray);
                if (hr.HasEntry())
                {
                    colour = CalculateColour(camera, scene, dir, hr);
                }
            }

            return colour;
        }

        // Render specified block of Scene to the Canvas.
        public void Render(Camera camera, Scene scene, Canvas canvas, int startX, int startY, int numColumns, int numRows)
        {
            // Calculate Scene size based on camera FOV.
            double sceneWidth = camera.GetSceneWidth();
            double sceneHeight = camera.GetSceneHeight();

            Size = sceneWidth;

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
