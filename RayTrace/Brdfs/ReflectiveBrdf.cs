using System;
using System.Collections.Generic;
using System.Text;

namespace RayTrace.Brdfs
{
    public class ReflectiveBrdf : Brdf
    {
        public override SColour GetBrdfReflectance(HitResult hr, LocalCoordinateSystem lcs, Random rand, SColour colour, SVector source)
        {
            throw new NotImplementedException();
        }

        public override SColour GetColour(HitResult hr, LocalCoordinateSystem lcs, Random rand, SColour colour, SVector source, SColour Li)
        {
            throw new NotImplementedException();
        }

        public override SVector GetSample(HitResult hr, LocalCoordinateSystem lcs, Random rand, SColour colour, out float pdf)
        {
            // Get reflection angle 'R'.
            SVector vecToViewer = hr.Ray.Scale(-1.0f);

            float dpVE = vecToViewer.DotProduct(hr.EntryNormal);

            SVector reflection = hr.EntryNormal.Scale(2.0f * dpVE) - vecToViewer;
            reflection = reflection.Normalise();

            pdf = 1.0f;

            return reflection;
        }

        public override XMLBrdf ToXML()
        {
            throw new NotImplementedException();
        }

        protected override float F(in SVector v)
        {
            throw new NotImplementedException();
        }

        protected override float Pdf(in SVector v)
        {
            throw new NotImplementedException();
        }
    }
}
