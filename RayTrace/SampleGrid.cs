using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    // Return a unit sample grid of the specified size. Uses a jittered grid to simulate blue noise.
    public class SampleGrid
    {
        static public void DumpGrid(double[,] grid)
        {
            int bound = grid.GetLength(0);
            for (int i=0; i<bound; i++)
            {
                Console.WriteLine(grid[i, 0] + ", " + grid[i, 1]);
            }
        }

        // Get sample grid over range 0->1 with bottom left at [0, 0]
        public static float[,] GetOffsetGrid(Random rand, int size)
        {
            float[,] grid = new float[size, 2];

            return UpdateOffsetGrid(rand, grid);
        }

        // Update sample grid over range 0->1 with bottom left at [0, 0]
        public static float[,] UpdateOffsetGrid(Random rand, float[,] grid)
        {
            int size = (int) MathF.Sqrt(grid.GetLength(0));

            float halfStep = 1.0f / (2 * size);
            float step = halfStep + halfStep;

            float yVal = 0.0f;
            for (int y = 0; y < size; y++)
            {
                float xVal = 0.0f;
                for (int x = 0; x < size; x++)
                {
                    grid[y * size + x, 0] = xVal + halfStep * rand.NextSingle();
                    grid[y * size + x, 1] = yVal + halfStep * rand.NextSingle();
                    xVal += step;
                }
                yVal += step;
            }

            //DumpGrid(grid);

            return grid;
        }

        // Get sample 2x2 grid centred on [0, 0]
        public static double[,] GetCentredGrid(Random rand, int size)
        {
            double[,] grid = new double[size*size, 2];

            double halfStep = 2.0 / (2 * size);
            double step = halfStep + halfStep;

            //double yVal = -halfStep*(size - 1);
            //for (int y=0; y<size; y++)
            //{
            //    double xVal = -halfStep*(size - 1);
            //    for (int x=0; x<size; x++)
            //    {
            //        grid[y * size + x, 0] = xVal + halfStep*(-1.0 + 2 * rand.NextDouble());
            //        grid[y * size + x, 1] = yVal + halfStep*(-1.0 + 2 * rand.NextDouble());
            //        xVal += step;
            //    }
            //    yVal += step;
            //}

            double yVal = -halfStep * (size);
            for (int y = 0; y < size; y++)
            {
                double xVal = -halfStep * (size);
                for (int x = 0; x < size; x++)
                {
                    grid[y * size + x, 0] = xVal + halfStep * (rand.NextDouble());
                    grid[y * size + x, 1] = yVal + halfStep * (rand.NextDouble());
                    xVal += step;
                }
                yVal += step;
            }

            //DumpGrid(grid);

            return grid;
        }
    }
}
