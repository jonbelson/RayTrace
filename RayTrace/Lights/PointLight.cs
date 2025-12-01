using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class PointLight : Light
    {
        private void Pos_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Notify("Pos");
        }

        public Point Pos { get; set; }

        //
        // Constructor
        //
        public PointLight(Scene scene, Point p, float intensity, Colour c) : base(scene, intensity, c)
        {
            Pos = p;
            Radius = 2;// 1;

            Pos.PropertyChanged += Pos_PropertyChanged;
        }

        //
        // Constructor
        //
        public PointLight(Scene scene, XMLPointLight xmlPointLight) : base(scene, xmlPointLight)
        {
            Pos = new Point(xmlPointLight.Pos);

            Pos.PropertyChanged += Pos_PropertyChanged;
        }

        /*private*/ public SPoint _pos; // XXX

        // Allow Shape to pre-transform data using the Camera's WorldToView Matrix.
        public override void CreateCache(Camera camera)
        {
            _pos = new SPoint(Pos);
            //_pos = camera.WorldToView * Pos;
        }

        public override SVector GetDirection(Camera camera, SPoint p)
        {
            SPoint viewPos = camera.WorldToView*_pos;
            return viewPos.Difference(p);

/*
            Vector vecToLight = Pos.Difference(p);
//            Vector vecToLight = p.Difference(Pos);

            return vecToLight;
*/
        }

        public override float GetAttenuation(Camera camera, SPoint p)
        {
            SPoint viewPos = camera.WorldToView * _pos;

            //Vector vecToLight = p.Difference(Pos).Negate();
            SVector vecToLight = viewPos.Difference(p);

            // Assume light attenuatiaon is 1 over distance squared.
            double lightAttenuation = 1.0 / vecToLight.MagnitudeSquared();

            return (float) lightAttenuation;
        }

        public override SColour GetIntensityAtPoint(Camera camera, SPoint p)
        {
            SVector vecToLight = GetDirection(camera, p);
            float dDistanceSqr = vecToLight.MagnitudeSquared();
            return (Output / (4.0f * MathF.PI * dDistanceSqr)).ToSColour();
        }

        public override string ToString()
        {
            return "Light[" + Pos.ToString() + "]," + Intensity + "," + Colour.ToString() + "]";
        }

        protected override void AsXMLImpl(XMLEntity xml)
        {
            base.AsXMLImpl(xml);

            if (xml is XMLPointLight xmlPointLight)
            {
                xmlPointLight.Pos = Pos.ToXML();
            }
        }

        public override XMLEntity ToXML()
        {
            XMLPointLight xml = new XMLPointLight();
            AsXMLImpl(xml);
            return xml;
        }

        public override HitResult HitTest(Camera camera, Ray ray)
        {
            return new HitResult(null);
        }
    }
}
