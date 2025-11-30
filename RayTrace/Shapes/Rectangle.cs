using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class Rectangle : Shape
    {
        private Vector _normal;
        public Vector Normal {
            get { return _normal; }
            set { if (_normal != value) { _normal = value; Notify("Normal"); } }
        }

        private Vector _up;
        public Vector Up
        {
            get { return _up; }
            set { if (_up != value) { _up = value; Notify("Up"); } }
        }

        // Precalculated values to speed up calculations.
        private SPoint pos;
        private SVector normal;
        private SVector up;

        public float Width { get; set; }
        public float Height { get; set; }

        // Allow Shape to pre-transform data using the Camera's WorldToView Matrix.
        public override void CreateCache(Camera camera)
        {
            pos = new(Pos.X, Pos.Y, Pos.Z);
            normal = new(Normal.I, Normal.J, Normal.K, 0.0f);
            up = new(Up.I, Up.J, Up.K, 0.0f);
        }

        private HitResult HitTestImpl(Camera camera, SPoint p1, SPoint p2, bool segmentOnly)
        {
            HitResult hr = new(this);

            SPoint vPos = camera.WorldToView * pos;
            SVector vNormal = camera.NormalMatrix * normal;

            SVector vUp = camera.NormalMatrix * up;


            float A = vNormal.I;
            float B = vNormal.J;
            float C = vNormal.K;

            // Since Ax + By + Cz + D = 0...
            float D = -(A * vPos.X + B * vPos.Y + C * vPos.Z);

            float num = A * p1.X + B * p1.Y + C * p1.Z + D;
            float denom = A * (p1.X - p2.X) + B * (p1.Y - p2.Y) + C * (p1.Z - p2.Z);

            if (denom == 0)
            {
                return hr;
            }

            float mu = num / denom;

            if (mu == 0.0)
            {
                // Either parallel to plane or on the plane - treat both as no intersection for our purposes.
                return hr;
            }

            double THRESH = 0.000001;

            if (mu < THRESH)
            {
                // Not interested in intersections that happen behind us.
                return hr;
            }

            if (segmentOnly)
            {
                // Check if segment actually intersects us.
                if (mu < THRESH || mu > 1.0 - THRESH)
                {
                    return hr;
                }
            }

            SVector vecP2P1 = p2 - p1;
            //Vector vecP2P1 = p1 - p2;
            SPoint Intersection = p1 + vecP2P1.Scale(mu);

            // We have a plane intersection, so next we need to check if it was within our bounds.

            //SPoint vRectCentre = new(vPos.X + Width/2, vPos.Y+Height/2, vPos.Z);

            SVector vRight = vNormal.VectorProduct(vUp);

            float xOffset;
            SPoint start = vPos - vUp.Normalise().Scale(Height/2);
            SPoint end = vPos + vUp.Normalise().Scale(Height/2);
            //SPoint start = vPos - vUp.Normalise().Scale(Height/2);
            //SPoint end = vPos + vUp.Normalise().Scale(Height/2);
            if (!Geometry.DistancePointToLine(Intersection, start, end, out xOffset, true))
            {
                return hr;
            }

            if (xOffset > Width/2)
            {
                return hr;
            }

            float yOffset;
            start = vPos - vRight.Normalise().Scale(Width/2);
            end = vPos + vRight.Normalise().Scale(Width/2);
            if (!Geometry.DistancePointToLine(Intersection, start, end, out yOffset, true))
            {
                return hr;
            }

            if (yOffset > Height/2)
            {
                return hr;
            }

            hr.Entry = Intersection;

            // Calculate texture coordinates relative to bottom left.
            SPoint bl = vPos - vRight.Normalise().Scale(Width/2) - vUp.Normalise().Scale(Height/2);

            start = bl;
            end = bl + vUp.Normalise().Scale(Height);
            Geometry.DistancePointToLine(Intersection, start, end, out xOffset, true);

            start = bl;
            end = bl + vRight.Normalise().Scale(Width);
            Geometry.DistancePointToLine(Intersection, start, end, out yOffset, true);


            float u = xOffset/Width;
            float v = yOffset/Height;

            hr.u = u;
            hr.v = v;

            //Debug.WriteLine($"u/v = {u}, {v}");

            if (Material != null && Material.NormalMap != null)
            {
                SVector normalMap = Material.NormalMap.GetNormal(u, v);

                SVector xaxis = vRight;
                SVector yaxis = vUp;
                SVector zaxis = vNormal;

                Matrix m = new(xaxis, yaxis, -zaxis);

                //m.Transpose();

                hr.EntryNormal = m * normalMap;
//                hr.EntryNormal = camera.NormalMatrix * normalMap;
            }
            else
            {
                // Intersection could have occurred from either side of the Rectangle, so adjust the surface normal appropriately.
                //Vector vecP2P1 = p1 - p2;
                vecP2P1 = vecP2P1.Normalise();

                //double dp = vNormal.DotProduct(vecP2P1);
                //if (dp > 0.0)
                //{
                //    hr.EntryNormal = vNormal;
                //}
                //else
                //{
                //    hr.EntryNormal = vNormal.Negate();
                //}

                hr.EntryNormal = vNormal;
            }
            return hr;
        }

        private void Normal_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Notify("Vector");
        }

        private void Up_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Notify("Vector");
        }

        public Rectangle(Scene scene, Point p, Vector normal, Vector up, float width, float height) : base(scene, p)
        {
            Normal = normal;
            Up = up;
            Width = width;
            Height = height;

            Normal.PropertyChanged += Normal_PropertyChanged;
            Up.PropertyChanged += Up_PropertyChanged;
        }
        /*
        public Plane(Scene scene, XMLRectangle xmlRectangle) : base(scene, xmlRectangle)
        {
            Normal = new Vector(xmlRectangle.Normal);
            Up = new Vector(xmlRectangle.Up);

            Width = xmlRectangle.Widht;
            Height = xmlRectangle.Height;

            Normal.PropertyChanged += Normal_PropertyChanged;
            Up.PropertyChanged += Up_PropertyChanged;
        }
        */
        public override HitResult HitTest(Camera camera, Ray ray)
        {
            HitResult hr = new HitResult(this);

            bool segmentOnly = ray.TMax == 1.0;

            SPoint p1 = ray.Origin;
            SPoint p2 = ray.Origin + ray.Direction;

            return HitTestImpl(camera, p1, p2, segmentOnly);
        }

        //public override HitResult HitTest(Camera camera, Point p1, Point p2)
        //{
        //    return HitTestImpl(camera, p1, p2, true);
        //}

        public override string ToString()
        {
            return "Rectangle[" + Normal + "," + Pos.ToString() + "]," + Material.ToString() + "]";
        }
        /*
        protected override void AsXMLImpl(XMLEntity xml)
        {
            base.AsXMLImpl(xml);

            if (xml is XMLRectangle xmlRectangle)
            {
                xmlRectangle.Normal = Normal.ToXML();
                xmlRectangle.Up = Up.ToXML();
                xmlRectangle.Width = Width;
                xmlRectangle.Height = Height;
            }
        }
        */
        /*
        public override XMLEntity ToXML()
        {
            XMLRectangle xml = new();
            AsXMLImpl(xml);
            return xml;
        }
        */
    }
}
