using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    // Utility class to calculate offsets for hexagonally-spaced points centred on a unit circle.
    static class HexGrid
    {
        private static readonly double unitCircle = 2.0;        // Diameter of unit circle.
        private static readonly int numHexagons = 4;            // How many hexagon centres across the unit circle.
        private static readonly double jitter = (unitCircle / numHexagons) / 2.0;   // Maximum jitter to ensure points stay within their hexagon's minimal circle.
        private static readonly double d = (unitCircle - jitter - jitter) / numHexagons;    // minimal diameter.
        private static readonly double r = d / 2;               // minimal radius.
        private static readonly double root3 = Math.Sqrt(3.0);  // square root of 3.
        private static readonly double D = (2 * d) / root3;     // maximal diameter.       
        private static readonly double R = D / 2.0;             // maximal radius.
        private static readonly double t = R;                   // side length.
        private static readonly double row = R + t / 2.0;       // vertical offset from one row to another.

        public static double Jitter { get; } = jitter;

        public static readonly double[,] values = new double[19, 2]
        {
            { -d, row*2 }, { 0.0, row*2 }, { d, row*2 },
            { -3*r, row }, { -r, row }, { r, row }, { 3*r, row },
            { -2*d, 0.0 }, { -d, 0.0 }, { 0.0, 0.0 }, { d, 0.0 }, { 2*d, 0.0 },
            { -3*r, -row }, { -r, -row }, { r, -row }, { 3*r, -row },
            { -d, -row*2 }, { 0.0, -row*2 }, { d, -row*2 },
        };


        /*
                public static readonly double[,] values = new double[19, 2]
                {
                    {0.75,0.433 },
                    {0,0.866 },
                    {-0.750,-0.433 },
                    {-0.750,0.433 },
                    {0,-0.866 },
                    {0.75,-0.433 },
                    {0,0 },
                    {0.75,0 },
                    {0.375,0.65 },
                    {-0.375,0.65 },
                    {-0.75,0 },
                    {-0.375,-0.65 },
                    {0.375,-0.65 },
                    {-0.375,0.216 },
                    {0,0.433 },
                    {0.375,0.217 },
                    {-0.375,-0.216 },
                    {0,-0.433 },
                    { 0.375,-0.217 }
                };
      */


        // Write grid values to console for debugging purposes.
        public static void DumpGrid()
        {
            int hexIndex = 0;
            int[] rows = new int[] { 3, 4, 5, 4, 3 };
            foreach (int row in rows)
            {
                for (int i = 0; i < row; i++)
                {
                    Console.Write(values[hexIndex, 0].ToString("F2") + ", " + values[hexIndex, 1].ToString("F2") + " ");
                    hexIndex++;
                }
                Console.WriteLine();
            }
        }
    }
}
