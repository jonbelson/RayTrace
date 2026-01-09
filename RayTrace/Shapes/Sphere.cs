using System;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;

namespace RayTrace
{
    public class Sphere : Shape
    {
		// Precalculated values to speed up calculations.
		private float _viewRadius;
		private SPoint _viewPos;
		private SVector _viewUp;
		private SVector _viewAway;

		public void CalculateUV(Camera camera, ref HitResult hr)
        {
			// Sn   Normal vector at point of interest.
			// Sp   Vector of north pole.
			// Se   Vector to reference point on equator.

			Debug.Assert(!float.IsNaN(hr.EntryNormal.I));

			SVector Sn = hr.EntryNormal;
			SVector Sp = new SVector(0.0f, 1.0f, 0.0f, 0.0f);
			Sp = _viewUp;
			Sp = camera.NormalMatrix*Sp;
			double dp = Sn.DotProduct(Sp);
			double phi = Math.Acos(-dp);

			Debug.Assert(!Double.IsNaN(phi));

			double v = phi / Math.PI;

			//SVector Se = new SVector(0.0f, 0.0f, 1.0f, 0.0f);
            SVector Se = _viewAway;
			Se = camera.NormalMatrix * Se;
			dp = Se.DotProduct(Sn);

			double d = 0.0;
			if (phi != 0.0)
			{
				d = dp / Math.Sin(phi);
			}
			if (d > 1.0) d = 1.0;
			if (d < -1.0) d = -1.0;
			double theta = (Math.Acos(d)) / (2.0*Math.PI);

			double u = theta;

			SVector Spe = Sp.VectorProduct(Se);
			if (Spe.DotProduct(Sn) <= 0.0)
			{
				u = 1.0 - theta;
			}

			Debug.Assert(!Double.IsNaN(u) && !Double.IsNaN(v));

			hr.u = (float) u;
			hr.v = (float) v;
        }

		//
		// Constructor.
		//
		public Sphere(Scene scene, Point p, int radius) : base(scene, p)
        {
			Scale = radius;

			Pos.PropertyChanged += Pos_PropertyChanged;
		}

		//
		// Constructor.
		//
		public Sphere(Scene scene, Point p, int radius, Colour col) : base(scene, p, col)
        {
            Scale = radius;

            Pos.PropertyChanged += Pos_PropertyChanged;
        }

		//
		// Constructor.
		//
		public Sphere(Scene scene, XMLSphere xmlSphere) : base(scene, xmlSphere)
		{
			//Radius = xmlSphere.Radius;

			Pos.PropertyChanged += Pos_PropertyChanged;
		}

		private void Pos_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
			Notify("Pos");
        }

		// Allow Shape to pre-transform data using the Camera's WorldToView Matrix.
		public override void CreateCache(Camera camera)
		{
			_viewPos = Pos.ToSPoint();
			_viewRadius = Scale;

			Vector up = new(0, 1, 0);
			_viewUp = (ObjectToWorldNormal*up).ToSVector();
			Vector away = new(0, 0, -1);
			_viewAway = (ObjectToWorldNormal*away).ToSVector();

			_viewUp = _viewUp.Normalise();
			_viewAway = _viewAway.Normalise();
		}

		// Hit test between line a sphere.
		// segmentOnly If true, only the line segment defined by p1 and p2 is considered.
		//private HitResult HitTestImpl(Camera camera, SPoint p1, SPoint p2, bool segmentOnly)
		private HitResult HitTestImpl(Camera camera, Ray ray)
		{
			HitResult hr = new HitResult(this);
			hr.Ray = ray.Direction;

			SPoint vPos = camera.WorldToView * _viewPos;

			float r = _viewRadius;

			SPoint p1 = ray.Origin;
			//SPoint p2 = p1 + ray.Direction;

			// Vector from p1 to p2.
			//SVector dp = p2 - p1;
			SVector dp = ray.Direction;

			float a = dp.MagnitudeSquared();
			float b = 2 * (dp.I * (p1.X - vPos.X) + dp.J * (p1.Y - vPos.Y) + dp.K * (p1.Z - vPos.Z));
			float c = vPos.X * vPos.X + vPos.Y * vPos.Y + vPos.Z * vPos.Z;
			c += p1.X * p1.X + p1.Y * p1.Y + p1.Z * p1.Z;
			c -= 2 * (vPos.X * p1.X + vPos.Y * p1.Y + vPos.Z * p1.Z);
			c -= r * r;

			const float EPS = 0.00001f;

			float bb4ac = b * b - 4 * a * c;
			if (Math.Abs(a) < EPS || bb4ac < 0)
			{
				return hr;
			}

			// Check if line is tangental to the sphere.
			if (bb4ac == 0.0)
			{
				float u = -b / (2 * a);

				SPoint p = p1 + dp.Scale(u);
				hr.Entry = p;

				SPoint hp = p + dp.Scale(u);

				//Vector surfaceNormal = vPos.Difference(hit);
				//surfaceNormal = surfaceNormal.Normalise();

				hr.EntryNormal = vPos.Difference(hp).Normalise();

				CalculateUV(camera, ref hr);

				return hr;
			}

			float mu1 = (-b + MathF.Sqrt(bb4ac)) / (2f * a);
			float mu2 = (-b - MathF.Sqrt(bb4ac)) / (2f * a);

			bool mu1Valid = true;
			bool mu2Valid = true;

			//float THRESH = 0.001f;
				
			// Generally we're not interested in hits that are very close to start position.
			if (mu1 < ray.TMin)	{ mu1Valid = false;	}
			if (mu2 < ray.TMin)	{ mu2Valid = false;	}

			// Check if segment actually intersects us.
			if (mu1 > ray.TMax) mu1Valid = false;
			if (mu2 > ray.TMax) mu2Valid = false;

			float mu;
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

			SPoint hit = p1 + dp.Scale(mu);

			SVector surfaceNormal = vPos.Difference(hit);
			surfaceNormal = surfaceNormal.Normalise();

			hr.t = mu;
			hr.Entry = hit;
			hr.EntryNormal = surfaceNormal;

			CalculateUV(camera, ref hr);

			if (Material != null && Material.NormalMap != null)
			{
				SVector normalMap = Material.NormalMap.GetNormal(hr.u, hr.v);

				SVector up = new(0, 1, 0, 0);
				SVector vUp = camera.NormalMatrix*up;
				SVector vNormal = hr.EntryNormal;
				SVector vRight = vNormal.VectorProduct(vUp);

				SVector xaxis = vRight;
				SVector yaxis = vUp;
				SVector zaxis = vNormal;

				Matrix m = new(xaxis, yaxis, -zaxis);

				//m.Transpose();

				hr.EntryNormal = m * normalMap;
			}
			else if (HasRoughness())
			{
			// XXX Should normal perturbation affect surface texture?
				float Scale = 10;

				SVector normal = hr.EntryNormal;

				float val = (float) Perlin.Noise(Scale * normal.I, Scale * normal.J, Scale * normal.K);
				val /= 10;

				//val = (val + noiseRange) / (2.0 * noiseRange);

				normal = normal.Add(new SVector(val, val, val, 0.0f));
				normal = normal.Normalise();

				hr.EntryNormal = normal;
			}

			return hr;

		}

//		public override HitResult HitTest(Camera camera, Point pos, Vector ray)
		public override HitResult HitTest(Camera camera, Ray ray)
		{
			//bool segmentOnly = (ray.TMax == 1.0);

			//Debug.Assert(!segmentOnly);

			//SPoint p2 = ray.Origin.Offset(ray.Direction);

			HitResult hr = HitTestImpl(camera, ray);
			hr.Ray = ray.Direction;
			return hr;

			// Based on http://paulbourke.net/geometry/circlesphere/index.html#linesphere

		}

		// ignoreClosest    If true, 
		//      public override HitResult HitTest(Camera camera, Point p1, Point p2)
		//      {
		//	return HitTestImpl(camera, p1, p2, true);
		//}

		public override (SPoint, SVector) GetRefractedVector(Camera camera, SVector ray, HitResult hr)
		{
			Debug.Assert(hr.HasEntry());

			float n1 = RefractiveIndex.Vacuum;
			float n2 = hr.shape.Material.RefractiveIndex;

			//			Vector refraction1 = Refraction.GetAngle(ray, hr.EntryNormal, RefractiveIndex.Vacuum, RefractiveIndex.WindowGlass);
			SVector refraction1 = Refraction.GetAngle(ray, hr.EntryNormal, n1, n2);

			if (!refraction1.IsNull())
			{
				Ray ray2 = new Ray(hr.Entry, refraction1);

				// Hit test refracted ray against ourself to find exit point.
				HitResult hrInternal = HitTest(camera, ray2 /*hr.Entry, refraction1*/);
				if (hrInternal.HasEntry())
				{
					// Found the exit point, so calculate refraction as ray exits the sphere.
					SVector refraction2 = Refraction.GetAngle(refraction1, hrInternal.EntryNormal.Negate(), n2, n1);

					return (hrInternal.Entry, refraction2);
				}
			}

			// Didn't mange to get e refracted ray, so return values passed.
			return (hr.Entry, ray);	// XXX Or return Vector.Null()?
		}

		public override Colour GetColour(HitResult hr)
		{
			return Material.GetColour(hr);
		}

		public override string ToString()
		{
			return "Sphere[" + Pos.ToString() + "]," + Material.ToString() + "]";
		}

		protected override void AsXMLImpl(XMLEntity xml)
		{
			base.AsXMLImpl(xml);

			if (xml is XMLSphere xmlSphere)
			{
				//xmlSphere.Radius = Radius;
			}
		}

		public override XMLEntity ToXML()
		{
			XMLSphere xml = new XMLSphere();
			AsXMLImpl(xml);
			return xml;
		}

        public override (Point, Point) GetBoundingBox()
        {
//            return (new Point(-Scale, -Scale, -Scale), new Point(Scale, Scale, Scale));
            return (new Point(-1, -1, -1), new Point(1, 1, 1));
        }

    }
}
