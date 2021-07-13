using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class DistantLight : Light
    {
        public Vector Direction { get; set; }

        public DistantLight(Vector direction, float intensity, Colour c) : base(intensity, c)
        {
            Direction = direction.Normalise();
        }

        public DistantLight(XMLDistantLight xmlDistantLight)
        {
            Direction = new Vector(xmlDistantLight.Direction);
        }

        private SVector _direction = new SVector();

        // Allow Shape to pre-transform data using the Camera's WorldToView Matrix.
        public override void CreateCache(Camera camera)
        {
            _direction = new SVector(Direction);
        }

        public override SVector GetDirection(Camera camera, SPoint p)
        {
            return _direction * 10000;
        }

        public override float GetAttenuation(Camera camera, SPoint p)
        {
            return 1.0f;
        }

        public override Colour GetIntensityAtPoint(Camera camera, SPoint p)
        {
            return Output / Math.PI;
        }

        protected override void AsXMLImpl(XMLEntity xml)
        {
            base.AsXMLImpl(xml);

            if (xml is XMLDistantLight xmlDistantLight)
            {
                xmlDistantLight.Direction = Direction.ToXML();
            }
        }

        public override XMLEntity ToXML()
        {
            XMLDistantLight xml = new XMLDistantLight();
            AsXMLImpl(xml);
            return xml;
        }
    }
}
