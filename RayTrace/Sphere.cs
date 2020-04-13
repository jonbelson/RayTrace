using System;
using System.Diagnostics;

namespace RayTrace
{
    public class Sphere : Shape
    {
        public int Radius { get; set; }

        public void CalculateUV(HitResult hr)
        {
            // Sn   Normal vector at point of interest.
            // Sp   Vector of north pole.
            // Se   Vector to reference point on equator.

			Vector Sn = hr.EntryNormal;
			Vector Sp = new Vector(0.0, 1.0, 0.0);
			double dp = Sn.DotProduct(Sp);
			double phi = Math.Acos(-dp);

			double v = phi / Math.PI;

			Vector Se = new Vector(0.0, 0.0, 1.0);
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
        private HitResult HitTest(Point p1, Point p2, bool segmentOnly, bool ignoreClosest)
        {
			HitResult hr = new HitResult(this);

			Point sc = Pos;
			double r = Radius;

			const double EPS = 0.00001;

			Point dp = new Point
			{
				X = p2.X - p1.X,
				Y = p2.Y - p1.Y,
				Z = p2.Z - p1.Z
			};

			double a = dp.X * dp.X + dp.Y * dp.Y + dp.Z * dp.Z;
			double b = 2 * (dp.X * (p1.X - sc.X) + dp.Y * (p1.Y - sc.Y) + dp.Z * (p1.Z - sc.Z));
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
				double mu = -b / (2 * a);

				double X = p1.X + mu * (p2.X - p1.X);
				double Y = p1.Y + mu * (p2.Y - p1.Y);
				double Z = p1.Z + mu * (p2.Z - p1.Z);

				hr.Entry = new Point(X, Y, Z);

				CalculateUV(hr);

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

			if (!mu1Valid && !mu2Valid)
			{
				return hr;
			}

			Point hit1 = new Point
			{
				X = p1.X + mu1 * (p2.X - p1.X),
				Y = p1.Y + mu1 * (p2.Y - p1.Y),
				Z = p1.Z + mu1 * (p2.Z - p1.Z)
			};

			Point hit2 = new Point
			{
				X = p1.X + mu2 * (p2.X - p1.X),
				Y = p1.Y + mu2 * (p2.Y - p1.Y),
				Z = p1.Z + mu2 * (p2.Z - p1.Z)
			};

			Vector surfaceNormal1 = Pos.Difference(hit1);
			surfaceNormal1.Normalise();

			Vector surfaceNormal2 = Pos.Difference(hit2);
			surfaceNormal2.Normalise();

			if (mu1Valid && !mu2Valid)
			{
				hr.Entry = hit1;
				hr.EntryNormal = surfaceNormal1;
			}
			else if (!mu1Valid && mu2Valid)
			{
				hr.Entry = hit2;
				hr.EntryNormal = surfaceNormal2;
			}
			else
			{
				// Nearest intersection is the entry point.
				double dist1 = hit1.Distance(p1);
				double dist2 = hit2.Distance(p1);

				if (dist1 < dist2)
				{
					hr.Entry = hit1;
					hr.Exit = hit2;
					hr.EntryNormal = surfaceNormal1;
				}
				else
				{
					hr.Entry = hit2;
					hr.Exit = hit1;
					hr.EntryNormal = surfaceNormal2;
				}
			}

			CalculateUV(hr);

			// XXX Should normal perturbation affect surface texture?
			if (Smoothness < 1.0f)
			{
				double Scale = 10;

				Vector normal = hr.EntryNormal;

				double val = Perlin.Noise(Scale * normal.I, Scale * normal.J, Scale * normal.K);
				val /= 10;

				//val = (val + noiseRange) / (2.0 * noiseRange);

				normal = normal.Add(new Vector(val, val, val));
				normal.Normalise();

				hr.EntryNormal = normal;
			}

			return hr;

		}

		public override HitResult HitTest(Point pos, Vector ray)
		{
			Point p2 = pos.Offset(ray);

			return HitTest(pos, p2, false, false);

			// Based on http://paulbourke.net/geometry/circlesphere/index.html#linesphere

		}

        // ignoreClosest    If true, 
        public override HitResult HitTest(Point p1, Point p2, bool ignoreClosest = false)
        {
			return HitTest(p1, p2, true, ignoreClosest);
		}

		public override (Point, Vector) GetRefractedVector(Vector ray, HitResult hr)
		{
			Debug.Assert(hr.HasEntry());

			float n1 = RefractiveIndex.Vacuum;
			float n2 = RefractiveIndex.WindowGlass;

			//			Vector refraction1 = Refraction.GetAngle(ray, hr.EntryNormal, RefractiveIndex.Vacuum, RefractiveIndex.WindowGlass);
			Vector refraction1 = Refraction.GetAngle(ray, hr.EntryNormal, n1, n2);

			if (!refraction1.IsNull())
			{
				// Hit test refracted ray against ourself to find exit point.
				HitResult hrInternal = HitTest(hr.Entry, refraction1);
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
