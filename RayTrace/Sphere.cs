using System;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;

namespace RayTrace
{
    public class Sphere : Shape
    {
        public int Radius { get; set; }

        public void CalculateUV(Camera camera, HitResult hr)
        {
            // Sn   Normal vector at point of interest.
            // Sp   Vector of north pole.
            // Se   Vector to reference point on equator.

			Vector Sn = hr.EntryNormal;
			Vector Sp = new Vector(0.0, 1.0, 0.0);
			Sp = camera.NormalMatrix*Sp;
			double dp = Sn.DotProduct(Sp);
			double phi = Math.Acos(-dp);

			Debug.Assert(!Double.IsNaN(phi));

			double v = phi / Math.PI;

			Vector Se = new Vector(0.0, 0.0, 1.0);
			Se = camera.NormalMatrix * Se;
			dp = Se.DotProduct(Sn);

			double d = dp / Math.Sin(phi);
			if (d > 1.0) d = 1.0;
			if (d < -1.0) d = -1.0;
			double theta = (Math.Acos(d)) / (2.0*Math.PI);

			double u = theta;

			Vector Spe = Sp.VectorProduct(Se);
			if (Spe.DotProduct(Sn) <= 0.0)
			{
				u = 1.0 - theta;
			}

			Debug.Assert(!Double.IsNaN(u) && !Double.IsNaN(v));

			hr.u = (float) (1.0 - u);
			hr.v = (float) v;
        }

        public Sphere(Point p, int radius, Colour col) : base(p, col)
        {
            Radius = radius;
        }

        // Hit test between line a sphere.
        // segmentOnly If true, only the line segment defined by p1 and p2 is considered.
        private HitResult HitTestImpl(Camera camera, Point p1, Point p2, bool segmentOnly)
        {
			HitResult hr = new HitResult(this);

			Point sc = camera.WorldToView * Pos;

			double r = Radius;

			const double EPS = 0.00001;

			// Vector from p1 to p2.
			Vector dp = p2 - p1;

			double a = dp.MagnitudeSquared();
			double b = 2 * (dp.I * (p1.X - sc.X) + dp.J * (p1.Y - sc.Y) + dp.K * (p1.Z - sc.Z));
			double c = sc.X * sc.X + sc.Y * sc.Y + sc.Z * sc.Z;
			c += p1.X * p1.X + p1.Y * p1.Y + p1.Z * p1.Z;
			c -= 2 * (sc.X * p1.X + sc.Y * p1.Y + sc.Z * p1.Z);
			c -= r * r;

			double bb4ac = b * b - 4 * a * c;
			if (Math.Abs(a) < EPS || bb4ac < 0)
			{
				return hr;
			}

			// Check if line is tangental to the sphere.
			if (bb4ac == 0.0)
			{
				double u = -b / (2 * a);

				Point p = p1 + dp.Scale(u);
				hr.Entry = p;

				CalculateUV(camera, hr);

				return hr;
			}

			double mu1 = (-b + Math.Sqrt(bb4ac)) / (2 * a);
			double mu2 = (-b - Math.Sqrt(bb4ac)) / (2 * a);

			bool mu1Valid = true;
			bool mu2Valid = true;

			double THRESH = 0.000001;

			// Generally we're not interested in hits that are very close to start position.
			if (mu1 < THRESH)	{ mu1Valid = false;	}
			if (mu2 < THRESH)	{ mu2Valid = false;	}

			if (segmentOnly)
			{
				// Check if segment actually intersects us.
				if (mu1 > 1.0 - THRESH) mu1Valid = false;
				if (mu2 > 1.0 - THRESH) mu2Valid = false;
			}

			double mu;
			if (mu1Valid && mu2Valid)
				mu = Math.Min(mu1, mu2);
			else if (mu1Valid)
				mu = mu1;
			else if (mu2Valid)
				mu = mu2;
			else
			{
				// No valid intersections.
				return hr;
			}

			Point hit = p1 + dp.Scale(mu);

			Vector surfaceNormal = /*Pos*/ sc.Difference(hit);
			surfaceNormal = surfaceNormal.Normalise();

			hr.Entry = hit;
			hr.EntryNormal = surfaceNormal;

			CalculateUV(camera, hr);

			// XXX Should normal perturbation affect surface texture?
			if (HasRoughness())
			{
				double Scale = 10;

				Vector normal = hr.EntryNormal;

				double val = Perlin.Noise(Scale * normal.I, Scale * normal.J, Scale * normal.K);
				val /= 10;

				//val = (val + noiseRange) / (2.0 * noiseRange);

				normal = normal.Add(new Vector(val, val, val));
				normal = normal.Normalise();

				hr.EntryNormal = normal;
			}

			return hr;

		}

//		public override HitResult HitTest(Camera camera, Point pos, Vector ray)
		public override HitResult HitTest(Camera camera, Ray ray)
		{
			bool segmentOnly = (ray.TMax == 1.0);

			//Debug.Assert(!segmentOnly);

			Point p2 = ray.Origin.Offset(ray.Direction);

			return HitTestImpl(camera, ray.Origin, p2, segmentOnly);

			// Based on http://paulbourke.net/geometry/circlesphere/index.html#linesphere

		}

		// ignoreClosest    If true, 
		//      public override HitResult HitTest(Camera camera, Point p1, Point p2)
		//      {
		//	return HitTestImpl(camera, p1, p2, true);
		//}

		public override (Point, Vector) GetRefractedVector(Camera camera, Vector ray, HitResult hr)
		{
			Debug.Assert(hr.HasEntry());

			float n1 = RefractiveIndex.Vacuum;
			float n2 = hr.shape.Material.RefractiveIndex;

			//			Vector refraction1 = Refraction.GetAngle(ray, hr.EntryNormal, RefractiveIndex.Vacuum, RefractiveIndex.WindowGlass);
			Vector refraction1 = Refraction.GetAngle(ray, hr.EntryNormal, n1, n2);

			if (!refraction1.IsNull())
			{
				Ray ray2 = new Ray(hr.Entry, refraction1);

				// Hit test refracted ray against ourself to find exit point.
				HitResult hrInternal = HitTest(camera, ray2 /*hr.Entry, refraction1*/);
				if (hrInternal.HasEntry())
				{
					// Found the exit point, so calculate refraction as ray exits the sphere.
					Vector refraction2 = Refraction.GetAngle(refraction1, hrInternal.EntryNormal.Negate(), n2, n1);

					return (hrInternal.Entry, refraction2);
				}
			}

			// Didn't mange to get e refracted ray, so return values passed.
			return (hr.Entry, ray);	// XXX Or return Vector.Null()?
		}

		public override string ToString()
		{
			return "Sphere[" + Pos.ToString() + "]," + Material.ToString() + "]";
		}
	}
}
