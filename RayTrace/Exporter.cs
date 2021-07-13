using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RayTrace
{
    [Serializable]
    public class XMLColour
    {
        [XmlAttribute]
        public float Red, Green, Blue;
    }

    [Serializable]
    public class XMLVector
    {
        [XmlAttribute]
        public float I, J, K;
    }

    [Serializable]
    public class XMLPoint
    {
        [XmlAttribute]
        public float X, Y, Z;
    }

    [Serializable]
    public class XMLMaterial
    {
        public MaterialType Type;
        public double PhongDiffuse;
        public double PhongSpecular;
        public double PhongShininess;

        public float roughness;
        public float transparency;
        public float reflectivity;

        public float RefractiveIndex;

        public XMLTexture Texture;
    }

    [Serializable]
    [XmlInclude(typeof(XMLTextureSolid))]
    [XmlInclude(typeof(XMLTexturePerTriangle))]
    [XmlInclude(typeof(XMLTextureSquares))]
    [XmlInclude(typeof(XMLTextureBitmap))]
    public class XMLTexture
    {
        public virtual Texture Create() { return null; }
    }

    [Serializable]
    public class XMLTextureSolid : XMLTexture
    {
        public XMLColour Colour;

        public override Texture Create() => new TextureSolid(this);
    }

    [Serializable]
    public class KeyPairIntXMLColour
    {
        public int Key;
        public XMLColour Value;
    }

    [Serializable]
    public class XMLTexturePerTriangle : XMLTexture
    {
//        public List<KeyValuePair<int, XMLColour>> ColourList;
        public List<KeyPairIntXMLColour> ColourList;

        public override Texture Create() => new TexturePerTriangle(this);
    }

    [Serializable]
    public class XMLTextureSquares : XMLTexture
    {
        public XMLColour Colour1;
        public XMLColour Colour2;
        public int Scale;

        public override Texture Create() => new TextureSquares(this);
    }

    [Serializable]
    public class XMLTextureBitmap : XMLTexture
    {
        //public int width;
        //public int height;
        public String fileName;

        public override Texture Create() => new TextureBitmap(this);
    }

    [Serializable]
    [XmlInclude(typeof(XMLDisc))]
    [XmlInclude(typeof(XMLPlane))]
    [XmlInclude(typeof(XMLShape))]
    [XmlInclude(typeof(XMLSphere))]
    [XmlInclude(typeof(XMLModel))]
    public class XMLEntity
    {
        public bool Enabled;
        public string Name;
    }

    [Serializable]
    public class XMLShape : XMLEntity
    {
        public XMLPoint Pos;
        public XMLMaterial Material;

        public virtual Shape Create() { return null; }
    }

    [Serializable]
    public class XMLSphere : XMLShape
    {
        public int Radius;

        public override Shape Create() { return new Sphere(this); }
    }

    [Serializable]
    public class XMLDisc : XMLShape
    {
        public XMLVector Normal;
        public double Radius;

        public override Shape Create() { return new Disc(this); }
    }

    [Serializable]
    public class XMLPlane : XMLShape
    {
        public XMLVector Normal;

        public override Shape Create() { return new Plane(this); }
    }

    [Serializable]
    public class XMLTexel
    {
        [XmlAttribute]
        public float u, v;
    }

    [Serializable]
    public class XMLTriangle
    {
        [XmlAttribute]
        public int v0, v1, v2;  // Indices into Point array.
        [XmlAttribute]
        public int t0, t1, t2;  // Indexes into Texel array.
        [XmlAttribute]
        public int n0, n1, n2;  // Indices into normal array.
        [XmlAttribute]
        public int n;           // Index into normal array for triangle normal.
    }

    [Serializable]
    public class XMLModel : XMLShape
    {
        public string FileName;

        public List<XMLPoint> Points;
        public List<XMLTexel> Texels;
        public List<XMLVector> Normals;
        public List<XMLTriangle> Triangles;

        public XMLTexture Texure;

        public override Shape Create() { return new Model(this); }
    }

    [Serializable]
    public class XMLCamera : XMLEntity
    {
        public XMLPoint Pos;
        public XMLPoint Target;
        public float DepthOfField;
        public float Aperture;
        public float FovH;
    }

    [Serializable]
    [XmlInclude(typeof(XMLPointLight))]
    [XmlInclude(typeof(XMLDistantLight))]
    public class XMLLight : XMLEntity
    {
        public float Intensity;
        public XMLColour Colour;
        public int Radius;

        public virtual Light Create() { return null; }
    }

    [Serializable]
    public class XMLPointLight : XMLLight
    {
        public XMLPoint Pos;

        public override Light Create() { return new PointLight(this); }
    }

    [Serializable]
    public class XMLDistantLight : XMLLight
    {
        public XMLVector Direction;

        public override Light Create() { return new DistantLight(this); }
    }

    [Serializable]
    public class XMLScene
    {
        public int Version = 1;
        public DateTime Date = DateTime.Now;

        public List<XMLShape> Shapes = new List<XMLShape>();
        public List<XMLLight> Lights = new List<XMLLight>();
        public XMLCamera Camera;
        public XMLColour Ambient;
        public XMLColour Background;
    }

    [Serializable]
    public class Exporter
    {
        static public XMLScene Import(string fileName)
        {
            XMLScene xmlScene = null;

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(XMLScene));

                using (Stream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    xmlScene = (XMLScene) serializer.Deserialize(fileStream);
                }
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(e.Message);
            }

            return xmlScene;
        }

        static public bool Export(XMLScene xmlScene, string fileName)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(XMLScene));
                using (Stream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    serializer.Serialize(fileStream, xmlScene);
                }
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(e.Message);

                return false;
            }

            return true;
        }

    }
}
