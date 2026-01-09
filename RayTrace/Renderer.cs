using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Media.Converters;


namespace RayTrace
{
    // Represents a local coordinate system at a point on a surface, where
    //   the Y axis is aligned with the surface normal.
    public class LocalCoordinateSystem
    {
        public SVector N { get; set; }      // Normal
        public SVector Nt { get; set; }     // Tangent
        public SVector Nb { get; set; }     // Bitangent

        public LocalCoordinateSystem(in SVector normal)
        {
            N = normal;
            if (MathF.Abs(N.I) > MathF.Abs(N.J))
            {
                float invMagnitude = 1.0f / MathF.Sqrt(N.I * N.I + N.K * N.K);
                Nt = new SVector(N.K*invMagnitude, 0.0f, -N.I*invMagnitude, 0);
            }
            else
            {
                float invMagnitude = 1.0f / MathF.Sqrt(N.J * N.J + N.K * N.K);
                Nt = new SVector(0.0f, -N.K*invMagnitude, N.J*invMagnitude, 0);
            }

            Nb = N.VectorProduct(Nt);   // Possible the order of this shoudl be reversed.
        }

        // Reproject a vector from the lcs a world space with the specified normal.
        public SVector Reproject(in SVector normal)
        {
            float i = normal.I*Nb.I + normal.J*N.I + normal.K*Nt.I;
            float j = normal.I*Nb.J + normal.J*N.J + normal.K*Nt.J;
            float k = normal.I*Nb.K + normal.J*N.K + normal.K*Nt.K;

            return new(i, j, k, 0.0f);
        }

        // Reproject a vector from world space to the lcs.
        public SVector FromWorld(in SVector v)
        {
            float x = v.DotProduct(Nb);
            float y = v.DotProduct(N);
            float z = v.DotProduct(Nt);

            return new SVector(x, y, z, 0);
        }
    }


    // Negative Z axis towards observer.
    public class Renderer
    {
        public bool UseAmbient { set; get; } = false;
        public bool UseDiffuse { set; get; } = true;
        public bool UseShadows { set; get; } = true;
        public bool UseSpecular { set; get; } = true;
        public bool UseReflections { get; set; } = true;
        public bool UseRefraction { get; set; } = true;
        public bool UseSoftShadows { get; set; } = false;//true;
        public bool UseDepthOfField { get; set; } = false;//true;
        public bool UseGlobalIllumination { get; set; } = false;
        public bool UseBRDF { get; set; } = true;
        public bool UseImportanceSampling { get; set; } = true;

        public enum AntiAliasType { None, NoiseGrid, SuperSampling, AdaptiveSupersampling }
        public AntiAliasType AntiAlias { get; set; } = AntiAliasType.NoiseGrid;

        private int _sampleDepth = 15;
        public int SampleDepth { set { _sampleDepth = Math.Clamp(value, 0, MaxSampleDepth); } get { return _sampleDepth; } }
        public static int MaxSampleDepth { get => 50; }

        // Maximum recursive reflection depth when tracing reflections.
        private int _recursionDepth = 5;
        public int RecursionDepth { set { if (value <= 5) _recursionDepth = value; } get { return _recursionDepth; } }

        // Minimum depth before we start to apply Russian roulette termination.
        private int _minRecursionDepth = 3;

        // Maximum recursive global illumination depth when tracing scattered rays.
        private int _GIDepth = 1;
        public int GIDepth { set { if (value >= 0) _GIDepth = value; } get { return _GIDepth; } }

        // Number of scattered rays for each diffuse intersection when tracing scattered rays.
        private int _numGIRays = 20;
        public int NumGIRays { set { if (value >= 0) _numGIRays = value; } get { return _numGIRays; } }

        // Number of integrated samples when evaluating a BRDF at a point.
        private int _numBRDFSamples = 200;
        public int NumBRDFSamples {
            get { return _numBRDFSamples; }
            set { if (value >= 0) _numBRDFSamples = Math.Max(1, value); }
        }

        // Size of rendered area, so that it ranges from -Size/2 to +Size/2 in the
        //  x axis and -Size/2 to +Size/2 in the y axis.
        public float Size { get; set; }

        //private static readonly double jitterOffset = 0.075;
        private static readonly float jitterOffset = HexGrid.Jitter;   // 0.075;
        private float[,] hexArray = HexGrid.values;
        //private Random rand = new Random();

        // Array of RNGs for threads to use.
        private const int NumRandomisers = 64;
        private Random[] randomisers = new Random[NumRandomisers];

        private const int NumRandSeeds = 1000; 
        private readonly int[] seeds = new int[NumRandSeeds];
        private static int randIndex = 0;
        private static object RandLock = new object();

        private ulong _numRays = 0;
        public ulong NumRays { get => _numRays; set => _numRays = value; }

        [ThreadStatic]
        private static ulong NumThreadRays;

        //[ThreadStatic] private static Random threadRand;
        private ThreadLocal<Random> threadRand;
        //[DllImport("kernel32.dll")]
        //static extern int GetCurrentThreadId();

        //
        // Constructor.
        //
        public Renderer()
        {
            Size = 100;

            for (int i=0; i<NumRandomisers; i++)
            {
                randomisers[i] = new Random();
            }

            Random r = new Random();
            for (int i=0; i<1000; i++)
            {
                seeds[i] = r.Next(); 
            }
        }

        private Colour CalculateIlluminationColour(Scene scene, Vector ray, HitResult hr)
        {
            Colour colour = Palette.Black;

            return colour;
        }

        // Return a random Point within a unit disc.
        private static Point GetPointOnDisc(Random rand)
        {
            float x, y;
            do
            {
                x = -1.0f + 2.0f * rand.NextSingle();
                y = -1.0f + 2.0f * rand.NextSingle();
            } while (x * x + y * y > 1.0f);

            return new Point(x, y, 0.0f);
        }

        // Return a random Vector within a unit disc.
        private Vector GetVectorOnDisc(Random rand)
        {
            Point p = GetPointOnDisc(rand);

            return new Vector(p.X, p.Y, 0.0f);
        }

        // Return a random SPoint within a unit disc.
        private static SPoint GetSPointOnDisc(Random rand)
        {
            float x, y;
            do
            {
                x = -1.0f + 2.0f * rand.NextSingle();
                y = -1.0f + 2.0f * rand.NextSingle();
            } while (x * x + y * y > 1.0f);

            return new SPoint(x, y, 0.0f, 1.0f);
        }

        // Return a random SVector within a unit disc.
        private static SVector GetSVectorOnDisc(Random rand)
        {
            SPoint p = GetSPointOnDisc(rand);

            return new SVector(p.X, p.Y, 0.0f, 0.0f);
        }

        // Return a random Point within a unit sphere.
        private static Point GetPointInSphere(Random rand)
        {
            float x, y, z;
            do
            {
                x = -1.0f + 2.0f * rand.NextSingle();
                y = -1.0f + 2.0f * rand.NextSingle();
                z = -1.0f + 2.0f * rand.NextSingle();
            } while (x * x + y * y + z * z > 1.0f);

            return new Point(x, y, z);
        }

        private static SPoint GetSPointInSphere(Random rand)
        {
            float x, y, z;
            do
            {
                x = -1.0f + 2.0f * rand.NextSingle();
                y = -1.0f + 2.0f * rand.NextSingle();
                z = -1.0f + 2.0f * rand.NextSingle();
            } while (x * x + y * y + z * z > 1.0f);

            return new SPoint(x, y, z, 1.0f);
        }

        // Return a random Vector within a unit sphere.
        private Vector GetVectorInSphere(Random rand)
        {
            Point p = GetPointInSphere(rand);
            return new Vector(p.X, p.Y, p.Z);
        }

        private static SVector GetSVectorInSphere(Random rand)
        {
            SPoint p = GetSPointInSphere(rand);
            return new SVector(p.X, p.Y, p.Z, 0);
        }

        // Get random vector within hemisphere aligned with Y axis.
        /*private*/ public static SVector GetLocalVectorInHemisphere(Random rand)
        {
            float r1 = rand.NextSingle();
            float r2 = rand.NextSingle();

            float sinTheta = MathF.Sqrt(1f - r1 * r1);
            float phi = 2f * MathF.PI * r2;

            float x = sinTheta * MathF.Cos(phi);
            float y = r1;
            float z = sinTheta * MathF.Sin(phi);
/*
            float theta = MathF.PI/2f*rand.NextSingle();
            float phi = MathF.PI*2f*rand.NextSingle();

            float x = MathF.Sin(theta)*MathF.Cos(phi);
            float y = MathF.Cos(theta);
            float z = MathF.Sin(theta)*MathF.Sin(phi);
*/
            return new SVector(x, y, z, 0.0f);
        }

        public static SVector GetLocalCosineVector(Random rand)
        {
            float r1 = rand.NextSingle();
            float r2 = rand.NextSingle();

            float phi = 2f * MathF.PI * r1;
            float sqrtR2 = MathF.Sqrt(r2);

            float x = MathF.Cos(phi)*sqrtR2;
            float z = MathF.Sin(phi)*sqrtR2;
            float y = MathF.Sqrt(1f - r2);

            return new SVector(x, y, z, 0);
        }

        // Based on 'AimZ()' function from 'RenderMan Companion' page 143.
        private Vector Aim(Vector vecToLight)
        {
            float yrot;
            float xzlen = MathF.Sqrt(vecToLight.I * vecToLight.I + vecToLight.K * vecToLight.K);
            if (xzlen == 0.0f)
            {
                yrot = (vecToLight.J < 0.0f) ? MathF.PI : 0.0f;
            }
            else
            {
                yrot = MathF.Acos(vecToLight.K / xzlen);
            }

            float yzlen = MathF.Sqrt(vecToLight.J * vecToLight.J + xzlen * xzlen);
            float xrot = MathF.Acos(xzlen / yzlen);

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


            // Create matrices to rotation from Z axis to vecToLight.
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
        private SVector GetJitteredRay(Matrix matrix, int index, SVector vecToLight, float distToLight, float lightRadius, Random rand)
        {
            // Calculate effective size of light from view point.
            float radiusOverDist = lightRadius / distToLight;
            if (radiusOverDist > 1.0f) radiusOverDist = 1.0f;

            float theta = MathF.Asin(radiusOverDist);
            float circleRadius = lightRadius * MathF.Cos(theta);
            float circleDistance = distToLight - lightRadius * MathF.Sin(theta);

            float deltaX = jitterOffset * (-1.0f + 2f * rand.NextSingle());
            float deltaY = jitterOffset * (-1.0f + 2f * rand.NextSingle());
            //double deltaX = jitterOffset * rand.NextDouble();
            //double deltaY = jitterOffset * rand.NextDouble();

            Debug.Assert(!Double.IsNaN(deltaX));
            Debug.Assert(!Double.IsNaN(deltaY));

            SVector ray = new SVector(
                circleRadius * (hexArray[index, 0] + deltaX),
                circleRadius * (hexArray[index, 1] + deltaY),
                circleDistance,
                0.0f
            );

            Debug.Assert(!Single.IsNaN(ray.I));
            Debug.Assert(!Single.IsNaN(ray.J));
            Debug.Assert(!Single.IsNaN(ray.K));


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
            SVector jitteredDir = matrix * ray;

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
        private float GetSoftShadow(Camera camera, Scene scene, SPoint pos, SVector ray, PointLight light, Random rand)
        {
            //Random rand = new Random(123456);
            //            Random rand = new Random();

            SPoint lightPos = camera.WorldToView * light._pos;

            SVector L = lightPos - pos;
            float distance = L.Magnitude();
            L = L.Normalise();

            Matrix lookAt = new Matrix();
            lookAt.SetLookAt(new Point(0, 0, 0), new Point(L.I, L.J, L.K));
            //            lookAt2.SetLookAt(new Point(0, 0, 0), new Point(jitteredRay.I, jitteredRay.J, jitteredRay.K));
            lookAt.Transpose();

            // Do more interations of the hex grid for 'wider' PointLights.
            int numIterations = light.Radius;

            //int maxIterations = 19 * numIterations;

            float totalFraction = 0.0f;

            //int totalNumUnblockedRays = maxIterations;
            for (int i = 0; i < numIterations; i++)
            {
                int numUnblockedRays = 19;
                for (int rayNum = 0; rayNum < 19; rayNum++)
                {
                    SVector jitteredRay = GetJitteredRay(lookAt, rayNum, L, distance, light.Radius, rand);

                    Ray ray2 = Ray.FromPoints(pos, jitteredRay.AsPoint());
                    bool hit = scene.HitTestFast(camera, ray2);
                    if (hit)
                    {
                        //Debug.Assert(hr.shape is Sphere);

                        numUnblockedRays--;
                        //totalNumUnblockedRays--;
                    }
                    if (rayNum == 6)
                    {
                        if (numUnblockedRays == 12)     // Corners and centre blocked.
                        {
                            numUnblockedRays = 0;
                            break;
                        }
                        else if (numUnblockedRays == 19)    // Corners and centre not blocked.
                        {
                            break;
                        }
                    }
                }

                totalFraction += numUnblockedRays / 19.0f;
            }

            //totalNumUnblockedRays -= (19 - numUnblockedRays);
            //}
            //            float fraction = ((float) totalNumUnblockedRays) / maxIterations;
            //float fraction = ((float)numUnblockedRays) / maxIterations;
            float fraction = totalFraction / numIterations;

            return fraction;
        }

        private SColour GetReflectedColour(Camera camera, Scene scene, SVector ray, Random rand, HitResult hr, int depth)
        {
            SColour c = SPalette.Black;

            // Get reflection angle 'R'.
            SVector vecToViewer = ray.Scale(-1.0f);

            float dpVE = vecToViewer.DotProduct(hr.EntryNormal);

            SVector R = hr.EntryNormal.Scale(2.0f * dpVE) - vecToViewer;
            R = R.Normalise();

            // Make point in direction of 'R' to use as an endpoint.
            SVector v = R.Scale(1000.0f);
            SPoint far = hr.Entry.Offset(v);

            HitResult hrReflect = scene.HitTest(camera, hr.Entry, far);
            if (hrReflect.HasEntry())
            {
                c = CalculateColour(camera, scene, R, rand, hrReflect, depth);
            }
            else
            {
                // Reflection didn't hit anything, so just use the Scene backgorund colour.
                //                c = scene.Background;
                c = scene.GetSkydomeColour(camera, R);
            }

            return c;
        }

        private SColour GetRefractedColour(Camera camera, Scene scene, SVector ray, Random rand, HitResult hr, int depth)
        {
            SColour c = SPalette.Black;

            Shape shape = hr.shape;

            // Hit object refracts, so work out direction of refracted ray (in then out) and check if it hits anything.
            var result = shape.GetRefractedVector(camera, ray, hr);

            Ray ray2 = new Ray(result.Item1, result.Item2);
            HitResult hrRefracted = scene.HitTest(camera, ray2);
            if (hrRefracted.HasEntry())
            {
                //Ray ray3 = new Ray(hrRefracted.Entry, result.Item2);
                c = CalculateColour(camera, scene, result.Item2, rand, hrRefracted, depth);

                //refracted = c;
            }
            else
            {
                // Refraction didn't hit anything, so just use the Scene backgorund colour.
                //c = scene.Background;
                c = scene.GetSkydomeColour(camera, result.Item2);
            }

            return c;
        }

        private SColour GetPixelColour2(Camera camera, Scene scene, float x, float y)
        {
            Random rand = threadRand.Value;

            SColour throughput = SPalette.White;
            SColour radiance = SPalette.Black;

            SPoint origin = new SPoint(0.0f, 0.0f, 0f, 1.0f);
            SVector dir = new SVector(x, y, -1f, 0f);
            Ray ray = new Ray(origin, dir.Normalise());

            for (int depth = 0; depth < RecursionDepth; depth++)
            {
                NumThreadRays++;
                SColour ambient = SPalette.Black;
                SColour refracted = SPalette.Black;
                SColour reflected = SPalette.Black;
                SColour diffuse = SPalette.Black;
                SColour specular = SPalette.Black;
                SColour emissive = SPalette.Black;

                HitResult hr = scene.HitTest(camera, ray);
                if (hr.HasEntry())
                {
                    //hr.EntryNormal = new SVector(hr.EntryNormal.X, hr.EntryNormal.Y, -hr.EntryNormal.Z, 0.0f);

                    Shape shape = hr.shape;
                    Material material = shape.GetMaterial(hr);

                    MaterialType materialType = material.Type;

                    emissive = material?.GetEmissiveColour(hr) ?? SPalette.Black;

                    switch (materialType)
                    {
                        case MaterialType.BRDF_Lambertian:
                        case MaterialType.BRDF_CookTorrence:
                        case MaterialType.BRDF_Gltf:
                            {
                                LocalCoordinateSystem lcs = new LocalCoordinateSystem(hr.EntryNormal);

                                SVector scattered = SVector.Null();
                                float pdf = 1.0f;

                                //SPoint origin = hr.Entry + hr.EntryNormal.Scale(0.01f);
                                if (UseImportanceSampling)
                                {
                                    const int NumRetries = 5;

                                    for (int i = 0; i<NumRetries; i++)
                                    {
                                        //SVector v = GetLocalVectorInHemisphere(rand);
                                        scattered = material.Brdf.GetSample(hr, lcs, rand, material.GetSColour(hr), out /*float*/ pdf);

                                        if (scattered != System.Numerics.Vector4.Null())
                                        {
                                            Debug.Assert(!float.IsNaN(scattered.I));

                                            scattered = scattered.Normalise();

                                            Debug.Assert(!float.IsNaN(scattered.I));

                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    SPoint p = hr.Entry + hr.EntryNormal + GetSVectorInSphere(rand);
                                    scattered = p - hr.Entry;
                                    scattered = scattered.Normalise();

                                    // Check scattered Vector is in the corect hemishere
                                    double dpSN = scattered.DotProduct(hr.EntryNormal);
                                    if (dpSN < 0.0)
                                    {
                                        // Reverse the direction on the Vector.
                                        scattered = -scattered;
                                    }
                                }

                                if (scattered.IsNull())
                                    break;

                                Debug.Assert(pdf >= 0);
                                if (pdf == 0.0f) break;
                                if (scattered.DotProduct(hr.EntryNormal) <= 0.0f)
                                    break;

                                float cosTheta = scattered.DotProduct(hr.EntryNormal);

                                SColour brdfColour = material.Brdf.GetBrdfReflectance(hr, lcs, rand, material.GetSColour(hr), scattered) / pdf;

                                SColour brdfWeight = brdfColour * cosTheta;

                                SColour newThroughput = throughput * brdfWeight;

                                if (depth >= _minRecursionDepth)
                                {
                                    float q = newThroughput.MaxComponent();

                                    if (q < 0.01f) break;

                                    if (rand.NextSingle() > q) break;

                                    newThroughput = newThroughput / q;
                                }

                                throughput = newThroughput;

                                //                            ray = new Ray(hr.Entry + scattered * 0.01f, scattered);
                                ray = new Ray(hr.Entry + hr.EntryNormal * 0.01f, scattered);

                                //                            ray.Origin = hr.Entry + scattered * 0.01f;//.Scale(0.01);
                                //                           ray.Direction = scattered;

                            }
                            break;

                        case MaterialType.Reflective:
                            {
                                // Get reflection angle 'R'.
                                SVector vecToViewer = ray.Direction.Scale(-1.0f);

                                float dpVE = vecToViewer.DotProduct(hr.EntryNormal);

                                SVector reflection = hr.EntryNormal.Scale(2.0f * dpVE) - vecToViewer;
                                reflection = reflection.Normalise();

                                ray = new Ray(hr.Entry + hr.EntryNormal * 0.01f, reflection);
                            }
                            break;

                        case MaterialType.RefractiveAndReflective:
                            {
                                // Schlicks approximation of Fresnal's factor.
                                // L = k_fresnal * L_reflected_ + (1 - k_fresnal) * L_refracted
                                float n1 = RefractiveIndex.Vacuum;
                                float n2 = shape.Material.RefractiveIndex;
                                float R0 = ((n1 - n2) / (n1 + n2)) * ((n1 - n2) / (n1 + n2));

                                SVector vecToViewer = ray.Direction.Scale(-1.0f).Normalise();
                                float cosTheta = vecToViewer.DotProduct(hr.EntryNormal);

                                float Rtheta = R0 + (1.0f - R0) * (1.0f * cosTheta);

                                // Probability that light is reflected.
                                float fresnalFactor = Rtheta;

                                fresnalFactor = Math.Max(0.0f, fresnalFactor);
                                fresnalFactor = Math.Min(1.0f, fresnalFactor);

                                Debug.Assert(fresnalFactor >= 0.0);
                                Debug.Assert(fresnalFactor <= 1.0);

                                //ray = ray.Normalise();

                                if (rand.NextSingle() >= fresnalFactor)
                                {
                                    // Get reflection angle 'R'.
                                    //SVector vecToViewer = ray.Direction.Scale(-1.0f);

                                    float dpVE = vecToViewer.DotProduct(hr.EntryNormal);

                                    SVector reflection = hr.EntryNormal.Scale(2.0f * dpVE) - vecToViewer;
                                    reflection = reflection.Normalise();

                                    ray = new Ray(hr.Entry + hr.EntryNormal * 0.01f, reflection);
                                }
                                else
                                {
                                    var result = shape.GetRefractedVector(camera, ray.Direction, hr);
                                    ray = new(result.Item1 + result.Item2*0.01f, result.Item2);
                                }
                            }
                            break;

                        default:
                            break;

                    }


                    //colour = CalculateColour(camera, scene, dir, rand, hr);
                }
                else
                {
                    // No further bounces, so just add the skydome colour then drop out.
                    radiance += throughput*scene.GetSkydomeColour(camera, ray.Direction);
                    break;

                }

                radiance += throughput*emissive;
            }

            return radiance;
        }

        // scene    Scene we are rendering.
        // ray      Incoming ray that produced HitResult.
        // hr       HitResult that detected intersection.
        internal SColour CalculateColour(Camera camera, Scene scene, SVector ray, Random rand, HitResult hr, int depth = 0, bool BrdfRay = false)
        {
            ////////Debug.Assert(hr.HasEntry());

            if (depth > RecursionDepth) return SPalette.Black;

            NumThreadRays++;

            SColour ambient = SPalette.Black;
            SColour refracted = SPalette.Black;
            SColour reflected = SPalette.Black;
            SColour diffuse = SPalette.Black;
            SColour specular = SPalette.Black;
            SColour emissive = SPalette.Black;

            //SColour throughput = SPalette.White;
            float throughput = 1.0f;

            Shape shape = hr.shape;
            if (shape is null) return SPalette.Black;

            Material material = shape.GetMaterial(hr);
            if (material is null) return SPalette.Black;

            MaterialType materialType = material.Type;

            if (!UseBRDF && (materialType == MaterialType.BRDF_Lambertian || materialType == MaterialType.BRDF_CookTorrence || materialType == MaterialType.BRDF_Gltf))
            {
                materialType = MaterialType.Diffuse;
            }

            emissive = material?.GetEmissiveColour(hr) ?? SPalette.Black;

            //if (depth > _minRecursiveDepth)
            //{
            //    float p = Math.Max(0.05f, 1.0f - material.Reflectivity);
            //    if (rand.NextSingle() < p) return emissive;
            //}


            depth++;



            switch (materialType)
            {
                case MaterialType.BRDF_Lambertian:
                case MaterialType.BRDF_CookTorrence:
                case MaterialType.BRDF_Gltf:
                    {
                        SColour L = new();

                        LocalCoordinateSystem lcs = new LocalCoordinateSystem(hr.EntryNormal);

                        int numSamples = BrdfRay ? NumBRDFSamples / 4 : NumBRDFSamples;
                        if (/*false &&*/ BrdfRay)
                        {
                            // Don't bother doing a full calculation if this is already a scattered ray from a BRDF surface.
                            Ray ray2 = new Ray(hr.Entry + hr.EntryNormal.Scale(0.01f), hr.EntryNormal);
                            HitResult hr2 = scene.HitTest(camera, ray2);
                            if (hr2.HasEntry())
                            {
                                // Calculate light arriving from the random direction.
                                SColour incoming = CalculateColour(camera, scene, hr.EntryNormal, rand, hr2, depth, true);
                                L += material.GetBrdfColour(lcs, hr, rand, hr.EntryNormal, incoming);
                            }
                            else
                            {
                                SColour incoming = scene.GetSkydomeColour(camera, ray2.Direction);

                                L += material.GetBrdfColour(lcs, hr, rand, hr.EntryNormal, incoming);
                            }
                        }
                        else
                        {
                            SPoint origin = hr.Entry + hr.EntryNormal.Scale(0.01f);

                          for (int i = 0; i < numSamples; i++)
//                            for (int i = 0; i < 1; i++)
                            {
                                SVector scattered;

                                float pdf = 1;

                                if (UseImportanceSampling)
                                {
                                    //SVector v = GetLocalVectorInHemisphere(rand);
                                    SVector v = material.Brdf.GetSample(hr, lcs, rand, material.GetSColour(hr), out /*float*/ pdf);

                                    scattered = v;// lcs.Reproject(v);

                                    if (scattered != System.Numerics.Vector4.Null())
                                    {
                                        Debug.Assert(!float.IsNaN(v.I));

                                        //Debug.Assert(System.Numerics.Vector4.IsNaN(v) != System.Numerics.Vector4.Zero);

                                        scattered = scattered.Normalise();

                                        Debug.Assert(!float.IsNaN(scattered.I));
                                    }
                                }
                                else
                                {
                                    SPoint p = hr.Entry + hr.EntryNormal + GetSVectorInSphere(rand);
                                    scattered = p - hr.Entry;
                                    scattered = scattered.Normalise();

                                    // Check scattered Vector is in the corect hemishere
                                    double dpSN = scattered.DotProduct(hr.EntryNormal);
                                    if (dpSN < 0.0)
                                    {
                                        // Reverse the direction on the Vector.
                                        scattered = -scattered;
                                        //dpSN = scattered.DotProduct(hr.EntryNormal);
                                    }
                                }

                                if (scattered.IsNull()) continue;

                                //Debug.Assert(scattered.DotProduct(hr.EntryNormal) >= 0);
                                //double dp = scattered.DotProduct(hr.EntryNormal);
                                ///Debug.Assert(dp >= 0.0);

                                SColour incoming;

                                Ray ray2 = new Ray(origin, scattered);
                                HitResult hr2 = scene.HitTest(camera, ray2);
                                if (hr2.HasEntry())
                                {
                                    // Calculate light arriving from the random direction.
                                    /*SColour*/ incoming = CalculateColour(camera, scene, scattered, rand, hr2, depth, /*false*/ true);

                                    //L += material.GetBrdfColour(lcs, hr, rand, scattered, incoming)/pdf;
                                }
                                else
                                {
                                    /*SColour*/ incoming = scene.GetSkydomeColour(camera, ray2.Direction);

                                    //L += material.GetBrdfColour(lcs, hr, rand, scattered, incoming)/pdf;
                                }

                                SColour brdfColour = material.GetBrdfColour(lcs, hr, rand, scattered, incoming) / pdf;

                                //throughput = throughput * brdfColour;

                                if (depth > 2)
                                {
                                    throughput = brdfColour.MaxComponent();
                                    throughput = Math.Clamp(throughput, 0.05f, 0.95f);

                                    float r = rand.NextSingle();

                                    if (r > throughput)
                                    {
                                        //depth = RecursionDepth + 1;
                                        return emissive;
                                    }
                                    else
                                    {
                                        //throughput = throughput / maxThroughput;
                                    }

                                    brdfColour /= throughput;
                                }

                                L += brdfColour;

                            }

                            L /= NumBRDFSamples;
                        }

                        foreach (Light light in scene.Lights)
                        {
                            if (!light.Enabled) continue;

                            PointLight pointLight = light as PointLight;
                            if (pointLight != null && pointLight.Radius > 0)
                            {
                                SPoint lightPos = camera.WorldToView*pointLight._pos;
                                bool gotHit = scene.HitTestFast(camera, hr.Entry, lightPos);
                                if (!gotHit)
                                {
                                    SVector toLight = lightPos - hr.Entry;
                                    toLight = toLight.Normalise();
                                    //SVector vecToLight = vecToLight.Normalise();
//                                    double dpLN = 1.0;// toLight.DotProduct(hr.EntryNormal);
                                    float dpLN = toLight.DotProductSaturated(hr.EntryNormal);

                                    ///Debug.Assert();
                                    if (dpLN > 0)
                                    {
                                        SColour incoming = pointLight.GetIntensityAtPoint(camera, hr.Entry) * dpLN;
                                        L += hr.shape.Material.GetBrdfColour(lcs, hr, rand, toLight, incoming);
                                        //L += hr.shape.Material.Brdf.GetColour(hr, toLight, incoming) / (4 * Math.PI * pointLight.Radius * pointLight.Radius);
                                        //L += c;
                                    }
                                }

                            }

                        }

                        diffuse = L;
                    }
                    break;

                case MaterialType.Reflective:
                    {
                        reflected = GetReflectedColour(camera, scene, ray, rand, hr, depth) * 0.8f;
                    }
                    break;

                case MaterialType.RefractiveAndReflective:
                    {
                        //var result = shape.GetRefractedVector(camera, ray, hr);

                        // Schlicks approximation of Fresnal's factor.
                        // L = k_fresnal * L_reflected_ + (1 - k_fresnal) * L_refracted
                        float n1 = RefractiveIndex.Vacuum;
                        float n2 = shape.Material.RefractiveIndex;
                        float R0 = ((n1 - n2) / (n1 + n2)) * ((n1 - n2) / (n1 + n2));

                        SVector vecToViewer = ray.Scale(-1.0f).Normalise();
                        //vecToViewer = vecToViewer.Normalise();
                        float cosTheta = (float)vecToViewer.DotProduct(hr.EntryNormal);

                        float Rtheta = R0 + (1.0f - R0) * (1.0f * cosTheta);
                        float fresnalFactor = Rtheta;

                        fresnalFactor = Math.Max(0.0f, fresnalFactor);
                        fresnalFactor = Math.Min(1.0f, fresnalFactor);

                        Debug.Assert(fresnalFactor >= 0.0);
                        Debug.Assert(fresnalFactor <= 1.0);

                        //fresnalFactor = 1.0f;

                        //ray = ray.Normalise();

                        SColour fresnalReflect = GetReflectedColour(camera, scene, ray, rand, hr, depth);
                        SColour fresnalRefract = GetRefractedColour(camera, scene, ray, rand, hr, depth);

                        //                    refracted = fresnalReflect * fresnalFactor + fresnalRefract*(1.0f - fresnalFactor);
                        refracted = fresnalRefract * fresnalFactor + fresnalReflect * (1.0f - fresnalFactor);
                    }
                    break;
                case MaterialType.Diffuse:
                    {
                        SColour directLight = SPalette.Black;
                        SColour indirectLight = SPalette.Black;

                        // Direct illumination from each of the lights.
                        foreach (Light light in scene.Lights)
                        {
                            if (!light.Enabled) continue;

                            SVector vecToLight = light.GetDirection(camera, hr.Entry).Negate();

                            float visibleFraction = 0.0f;
                            if (!UseShadows)
                            {
                                visibleFraction = 1.0f;
                            }
                            else
                            {
                                if (UseSoftShadows && light.Radius > 0)
                                {
                                    PointLight pointLight = light as PointLight;
                                    visibleFraction = GetSoftShadow(camera, scene, hr.Entry, ray, pointLight, rand);

                                    if (visibleFraction == 0.0f) continue;
                                }
                                else
                                {
                                    if (scene.HitTestFast(camera, Ray.FromPoints(hr.Entry, vecToLight.AsPoint())))
                                    {
                                        continue;
                                    }
                                    visibleFraction = 1.0f;
                                }
                            }

                            vecToLight = vecToLight.Normalise();
                            float dpLN = vecToLight.DotProduct(hr.EntryNormal);
                            if (dpLN > 0.0)
                            {
                                //diffuse += light.GetIntensityAtPoint(camera, hr.Entry) * dpLN * shape.Material.Albedo;
                                directLight += light.GetIntensityAtPoint(camera, hr.Entry) * dpLN * visibleFraction;
                            }
                        }

                        if (UseGlobalIllumination)
                        {
                            // Indirect illumination from other objects in the scene.

                            //Random rand = new Random(/*24680*/);
                            //Colour giColour = Palette.Black;
                            for (int i = 0; i < NumGIRays; i++)
                            {
                                /*
                                // Get random vector around surface normal to use as a reflection vector.
                                Vector scattered = GetVectorInSphere(rand);
                                */

                                SPoint p = hr.Entry + hr.EntryNormal + GetSVectorInSphere(rand);
                                SVector scattered = p - hr.Entry;
                                scattered = scattered.Normalise();

                                // Check scattered Vector is in the corect hemishere
                                float dpSN = scattered.DotProduct(hr.EntryNormal);
                                if (dpSN < 0.0f)
                                {
                                    // Reverse the direction on the Vector.
                                    scattered = -scattered;
                                    dpSN = scattered.DotProduct(hr.EntryNormal);
                                }

                                Debug.Assert(scattered.DotProduct(hr.EntryNormal) >= 0);
                                Debug.Assert(dpSN >= 0.0f);

                                HitResult hr2 = scene.HitTest(camera, new Ray(hr.Entry + hr.EntryNormal.Scale(0.01f), scattered));
                                if (hr2.HasEntry())
                                {
                                    //Debug.Assert(hr.shape != hr2.shape);
                                    indirectLight += CalculateColour(camera, scene, scattered, rand, hr2, depth) * dpSN / MathF.PI;
                                }
                            }
                            indirectLight /= NumGIRays;
                        }

                        diffuse += directLight + indirectLight * shape.GetSColour(hr);
                    }
                    break;

                case MaterialType.Phong:
                    foreach (Light light in scene.Lights)
                    {
                        if (!light.Enabled) continue;

                        SVector vecToLight = light.GetDirection(camera, hr.Entry).Negate();

                        if (UseShadows)
                        {
                            if (scene.HitTestFast(camera, new Ray(hr.Entry, vecToLight)))
                            {
                                continue;
                            }
                        }

                        // Diffuse part.
                        SColour lightIntensity = light.GetIntensityAtPoint(camera, hr.Entry);
                        vecToLight = vecToLight.Normalise();
                        float dpLN = vecToLight.DotProduct(hr.EntryNormal);
                        if (dpLN > 0.0f)
                        {
                            // diffuse += light.GetIntensityAtPoint(camera, hr.Entry) * dpLN * shape.Material.Albedo;
                            diffuse += lightIntensity * dpLN * shape.GetSColour(hr) * material.PhongDiffuse;

                            if (UseSpecular)
                            {
                                // Specular part.
                                SVector vecReflect = hr.EntryNormal.Scale(2.0f * dpLN) - vecToLight;
                                vecReflect = vecReflect.Normalise();

                                SVector vecToViewer = ray.Scale(-1.0f).Normalise();

                                //double Il = light.Intensity;// 1.0;    // Intensity of light source.
                                float dpVR = vecToViewer.DotProduct(vecReflect);

                                // Ignore negative dp as it means the vectors are pointing away from each other.
                                if (dpVR > 0.0f)
                                {
                                    float d = MathF.Pow(dpVR, (float)material.PhongShininess);
                                    if (d < 0.0f) d = 0.0f;

                                    SColour specularColour = lightIntensity * d;

                                    specular += specularColour * material.PhongSpecular;
                                }
                            }
                        }
                    }
                    break;
            }

            //if (UseAmbient && hr.shape.Material.Type != MaterialType.BRDF)
            if (UseAmbient)
            {
                ambient = SColour.FromColour(shape.GetColour(hr).Multiply(scene.Ambient));
            }

            emissive = material?.GetEmissiveColour(hr) ?? SPalette.Black;

            SColour finalColour = ambient + emissive + diffuse + reflected + refracted + specular;

            //finalColour /= throughput;

            return finalColour;
        }

        // Render entire Scene to the Canvas.
        public void Render(Scene scene, Canvas canvas, CancellationToken cancellationToken)
        {
            Render(scene, canvas, 0, 0, canvas.Width, canvas.Height, cancellationToken);
        }

        private struct SubSample
        {
            public float TLx;
            public float TLy;
            public float BRx;
            public float BRy;
            public SColour?[] Colours;

            // 0     1
            //
            //    4
            //
            // 2     3
            public SubSample(float tlX, float tlY, float brX, float brY)
            {
                TLx = tlX;
                TLy = tlY;
                BRx = brX;
                BRy = brY;
                Colours = new SColour?[5];
            }
        }

        private SColour GetSubPixelColour(Camera camera, Scene scene, SColour?[,] colourBuffer, int w, int h, float x, float y, float width, float height)
        {
            float centreX = x + width / 2.0f;
            float centreY = y - height / 2.0f;

            SColour? c0 = colourBuffer[w, h];
            SColour? c1 = colourBuffer[w + 1, h];
            SColour? c2 = colourBuffer[w, h + 1];
            SColour? c3 = colourBuffer[w + 1, h + 1];

            if (c0 == null) { c0 = GetPixelColour(camera, scene, x, y); colourBuffer[w, h] = c0; }
            if (c1 == null) { c1 = GetPixelColour(camera, scene, x + width, y); colourBuffer[w + 1, h] = c1; }
            if (c2 == null) { c2 = GetPixelColour(camera, scene, x + width, y - height); colourBuffer[w, h + 1] = c2; }
            if (c3 == null) { c3 = GetPixelColour(camera, scene, x + width, y - height); colourBuffer[w + 1, h + 1] = c3; }

            SColour c4 = GetPixelColour(camera, scene, centreX, centreY);

            SColour colour = c0.Value + c1.Value + c2.Value + c3.Value + c4;

            return colour.Scale(0.2f);
        }

        private SColour GetSubPixelColour(Camera camera, Scene scene, SubSample subSample, int depth)
        {
            if (depth > SampleDepth)
                return SPalette.Black;

            float centreX = (subSample.TLx + subSample.BRx) / 2;
            float centreY = (subSample.TLy + subSample.BRy) / 2;

            var Colours = subSample.Colours;

            // Get the colours of each of the five sample points.
            if (Colours[0] == null) Colours[0] = GetPixelColour(camera, scene, subSample.TLx, subSample.TLy);
            if (Colours[1] == null) Colours[1] = GetPixelColour(camera, scene, subSample.BRx, subSample.TLy);
            if (Colours[2] == null) Colours[2] = GetPixelColour(camera, scene, subSample.TLx, subSample.BRy);
            if (Colours[3] == null) Colours[3] = GetPixelColour(camera, scene, subSample.BRx, subSample.BRy);
            if (Colours[4] == null) Colours[4] = GetPixelColour(camera, scene, centreX, centreY);

            SColour col04, col14, col24, col34;

            // Compare each corner to the centre. If they're similar, average them else subdivide further.
            if (Colours[0].Value.IsSimilar(Colours[4].Value))
            {
                col04 = SColour.Average(Colours[0].Value, Colours[4].Value);
            }
            else
            {
                SubSample subSubSample = new SubSample(subSample.TLx, subSample.TLy, centreX, centreY);
                subSubSample.Colours[0] = Colours[0];
                subSubSample.Colours[3] = Colours[4];
                col04 = GetSubPixelColour(camera, scene, subSubSample, depth + 1);
            }

            if (Colours[1].Value.IsSimilar(Colours[4].Value))
            {
                col14 = SColour.Average(Colours[1].Value, Colours[4].Value);
            }
            else
            {
                SubSample subSubSample = new SubSample(centreX, centreY, subSample.BRx, subSample.TLy);
                subSubSample.Colours[1] = Colours[1];
                subSubSample.Colours[2] = Colours[4];
                col14 = GetSubPixelColour(camera, scene, subSubSample, depth + 1);
            }

            if (Colours[2].Value.IsSimilar(Colours[4].Value))
            {
                col24 = SColour.Average(Colours[2].Value, Colours[4].Value);
            }
            else
            {
                SubSample subSubSample = new SubSample(subSample.TLx, subSample.BRy, centreX, centreY);
                subSubSample.Colours[1] = Colours[4];
                subSubSample.Colours[2] = Colours[2];
                col24 = GetSubPixelColour(camera, scene, subSubSample, depth + 1);
            }

            if (Colours[3].Value.IsSimilar(Colours[4].Value))
            {
                col34 = SColour.Average(Colours[3].Value, Colours[4].Value);
            }
            else
            {
                SubSample subSubSample = new SubSample(centreX, centreY, subSample.BRx, subSample.BRy);
                subSubSample.Colours[4] = Colours[0];
                subSubSample.Colours[3] = Colours[3];
                col34 = GetSubPixelColour(camera, scene, subSubSample, depth + 1);
            }

            // Average the four colours.
            SColour colour = col04 + col14 + col24 + col34;
            colour = colour.Scale(0.25f);

            return colour;
        }

        //private static Random r = new Random();
        //private int[] seeds = new int[16] { r.Next(), r.Next(), r.Next(), r.Next(), r.Next(), r.Next(), r.Next(), r.Next(), r.Next(), r.Next(), r.Next(), r.Next(), r.Next(), r.Next(), r.Next(), r.Next() };
        //private int seedIndex = 0;

        // Get the colour of specified pixel by casting a ray into the Scene.
        private SColour GetPixelColour(Camera camera, Scene scene, /*Random rand,*/ float x, float y)
        {
            SColour colour = scene.Background.ToSColour();

            //NumRays++;

            //Vector ray = new Vector(x, y, 1, true);

            //http://cg.skeelogy.com/depth-of-field-using-raytracing/

            //Random rand = new Random(System.Threading.Thread.CurrentThread.ManagedThreadId);
            //Random rand = new Random(1234);
            //Random rand = new Random(seeds[seedIndex++]);

            // Pick a RNG to use.
            //int managedThreadId = Thread.CurrentThread.ManagedThreadId;
            //Random rand = randomisers[managedThreadId % NumRandomisers];
            /*
                        int index = 0;
                        lock (RandLock)
                        {
                            index = randIndex++;
                            if (randIndex >= 1000) randIndex = 0;
                            //Console.Write("Using index {0} seed {1}\n", index, seeds[index]);
                        }
                        Random rand = new Random(seeds[index]);
            */
            //Random rand = threadRand;
            Random rand = threadRand.Value;

            if (UseDepthOfField)
            {
                SVector viewVector = camera.GetViewVector().ToSVector();
                float d = 1.0f;
                SVector viewToPixel = new SVector(x, y, 1, 0);
                float dtick = viewToPixel.Magnitude();
                SVector v = viewToPixel.Normalise();
                float f = camera.DepthOfField; // camera.GetViewVector().Magnitude();

                SPoint e = new SPoint(0.0f, 0.0f, 0.0f, 1.0f);
                SPoint P = e + v * (dtick / (d / (d + f)));  // Point on focal plane.
                float aperture = camera.Aperture;

                //Colour sum = Palette.Black;


                float sceneWidth = camera.GetSceneWidth();
                float pixelWidth = sceneWidth / 800;

                // Create rays from area around image pixel to P.
                int numSamples = (int)aperture;
                if (numSamples == 0) numSamples = 1;
                for (int i = 0; i < numSamples; i++)
                {
                    SPoint o = new SPoint(x, y, 1.0f, 1.0f);
                    SVector offset = GetSVectorOnDisc(rand) * aperture * pixelWidth;
                    o += offset;
                    SVector dir = P - o;
                    dir = dir.Normalise();
                    Ray r = new Ray(o, dir);
                    HitResult hit = scene.HitTest(camera, r);
                    if (hit.HasEntry())
                    {
                        colour += CalculateColour(camera, scene, dir, rand, hit);
                    }
                }

                colour = colour.Scale(1.0f / numSamples);
            }
            else
            {
                SPoint origin = new SPoint(0.0f, 0.0f, 0f, 1.0f);
                SVector dir = new SVector(x, y, -1, 0);
                Ray ray = new Ray(origin, dir.Normalise());

                HitResult hr = scene.HitTest(camera, ray);
                if (hr.HasEntry())
                {
                    colour = CalculateColour(camera, scene, dir, rand, hr);
                }
                else
                {
                    colour = scene.GetSkydomeColour(camera, dir);

                    NumThreadRays++;  // Still counts as a ray even though it didn't hit anything.
                }
            }

            return colour;
        }

        // Render specified block of Scene to the Canvas.
        // startX   X coordinate of top left of block to render.
        // startY   Y coordinate of top left of block to render.
        // numColumns   Width of block to render.
        // numRows      Height of block to render.
        public void Render(Scene scene, Canvas canvas, int startX, int startY, int numColumns, int numRows, CancellationToken cancellationToken)
        {
            Camera camera = scene.Camera;

            int index = 0;
            lock (RandLock)
            {
                index = randIndex++;
                if (randIndex >= NumRandSeeds) randIndex = 0;
                //Console.Write("Using index {0} seed {1}\n", index, seeds[index]);
            }

            //threadRand = new Random(seeds[index]);
            threadRand = new ThreadLocal<Random>(() => { return new Random(seeds[index]); });

            NumThreadRays = 0;

            // Calculate Scene size based on camera FOV.
            float sceneWidth = camera.GetSceneWidth();
            float sceneHeight = camera.GetSceneHeight();

            Size = sceneWidth;

            int width = canvas.Width;
            int height = canvas.Height;

            if (startY + numRows > height)
            {
                numRows = height - startY;
            }

            //  Buffer to store the calculated Colour values for each corner of each pixel.
            SColour?[,] colourBuffer = new SColour?[numColumns + 1, numRows + 1];

            float sceneStartX = -Size / 2.0f;
            float sceneStartY = Size / 2.0f;
            float sceneStepX = Size / width;       // Step per pixel in Scene units.
            float sceneStepY = Size / height;

            float sceneY = sceneStartY;
            sceneY -= startY * sceneStepY;

            Random rand = threadRand.Value;// new Random(1234);
            float[,] grid = null;

            for (int h = startY; h < startY + numRows; h++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    break;
                }

                float sceneX = sceneStartX;

                sceneX += startX * sceneStepX; 
                for (int w=startX; w<startX + numColumns; w++)
                {
                    if (AntiAlias == AntiAliasType.None)
                    {
                        SColour colour = GetPixelColour(camera, scene, sceneX + sceneStepX / 2, sceneY - sceneStepY / 2);
                        canvas.Set(w, h, colour.ToRGB());
                    }
                    else if (AntiAlias == AntiAliasType.NoiseGrid)
                    {
                        //float[,] grid = SampleGrid.GetOffsetGrid(rand, SampleDepth);
                        if (grid is null)
                            grid = SampleGrid.GetOffsetGrid(rand, SampleDepth);
                        else
                            grid = SampleGrid.UpdateOffsetGrid(rand, grid);

                        SColour colour = SPalette.Black;

                        for (int i=0; i<grid.GetLength(0); i++)
                        {
                            //colour += GetPixelColour(camera, scene, sceneX + sceneStepX*grid[i, 0], sceneY - sceneStepY*grid[i, 1]);
                            colour += GetPixelColour2(camera, scene, sceneX + sceneStepX * grid[i, 0], sceneY - sceneStepY * grid[i, 1]);
                        }
                        colour = colour.Scale(1.0f/grid.GetLength(0));
                        canvas.Set(w, h, colour.ToRGB());
                    }
                    else if (AntiAlias == AntiAliasType.SuperSampling)
                    {
                        //SubSample subSample = new SubSample(x, y, x + sceneStepX, y - sceneStepY);
                        SColour colour = GetSubPixelColour(camera, scene, colourBuffer, w - startX, h - startY, sceneX, sceneY, /*sceneX +*/ sceneStepX, /*sceneY -*/ sceneStepY);
                        canvas.Set(w, h, colour.ToRGB());
                    }
                    else
                    {
                        SubSample subSample = new SubSample(sceneX, sceneY, sceneX + sceneStepX, sceneY - sceneStepY);
                        SColour colour = GetSubPixelColour(camera, scene, subSample, 1);
                        canvas.Set(w, h, colour.ToRGB());
                    }

                    sceneX += sceneStepX;
                }

                sceneY -= sceneStepY;
            }

            _ = Interlocked.Add(ref _numRays, NumThreadRays);
        }
    }
}
