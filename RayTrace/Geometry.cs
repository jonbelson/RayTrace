using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    static class Geometry
    {
		private const float EPS = 0.00001f;

		//public static bool HitTestSphere(SPoint centre, int radius, SVector line, bool segmentOnly)
		//{
		//	return HitTestSphere(centre, radius, p2 - p1, segmentOnly);
		//}

		public static bool HitTestSphere(SPoint centre, int radius, SPoint p1, SPoint p2, bool segmentOnly)
        {
			SVector line = p2 - p1;

			float a = line.MagnitudeSquared();
			float b = 2 * (line.I * (p1.X - centre.X) + line.J * (p1.Y - centre.Y) + line.K * (p1.Z - centre.Z));
			float c = centre.X * centre.X + centre.Y * centre.Y + centre.Z * centre.Z;
			c += p1.X * p1.X + p1.Y * p1.Y + p1.Z * p1.Z;
			c -= 2 * (centre.X * p1.X + centre.Y * p1.Y + centre.Z * p1.Z);
			c -= radius * radius;

			float bb4ac = b * b - 4 * a * c;
			if (Math.Abs(a) < EPS || bb4ac < 0)
			{
				return false;
			}

			float mu1 = (float)((-b + Math.Sqrt(bb4ac)) / (2 * a));
			float mu2 = (float)((-b - Math.Sqrt(bb4ac)) / (2 * a));

			bool mu1Valid = true;
			bool mu2Valid = true;

			float THRESH = 0.001f;

			// Generally we're not interested in hits that are very close to start position.
			if (mu1 < THRESH) { mu1Valid = false; }
			if (mu2 < THRESH) { mu2Valid = false; }

			if (segmentOnly)
			{
				// Check if segment actually intersects us.
				if (mu1 > 1.0 - THRESH) mu1Valid = false;
				if (mu2 > 1.0 - THRESH) mu2Valid = false;
			}

			return mu1Valid || mu2Valid;
		}
	}
}
