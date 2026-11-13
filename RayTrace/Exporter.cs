// Copyright (c) 2026 Jonathan Belson
// Licensed under the MIT License — use freely, keep this notice.
// SPDX-License-Identifier: MIT
// Full terms: see LICENSE in the project root.

﻿using RayTrace.Brdfs;
using RayTrace.Skydomes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
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
    [XmlInclude(typeof(XMLLambertian))]
    [XmlInclude(typeof(XMLCookTorrence))]
    [XmlInclude(typeof(XMLGltfBrdf))]
    public class XMLBrdf
    {
        public string Name;

        public virtual Brdf Create() { return null; }
    }

    [Serializable]
    public class XMLLambertian : XMLBrdf
    {
        public override Brdf Create() => new Lambertian(this);

    }

    [Serializable]
    public class XMLCookTorrence : XMLBrdf
    {
        public float Roughness;
        public float Absorption;
        public float Shininess;
        public CookTorrence.SurfaceType Type;
        public float Ior;

        public override Brdf Create() => new CookTorrence(this);
    }

    [Serializable]
    public class XMLGltfBrdf : XMLBrdf
    {
        public float Metallic;
        public float Ior;
        public float Roughness;

        public override Brdf Create() => new GltfBrdf(this);
    }


    [Serializable]
    [XmlInclude(typeof(XMLSkybox))]
    [XmlInclude(typeof(XMLSkydomeBitmap))]
    [XmlInclude(typeof(XMLSkydomeGradient))]
    public class XMLSkydome
    {
        public virtual Skydome Create() { return null; }
    }

    [Serializable]
    public class XMLSkybox : XMLSkydome
    {
        public string Directory { get; set; }

        public override Skydome Create() { return new Skybox(this); }
    }

    [Serializable]
    public class XMLSkydomeBitmap : XMLSkydome
    {
        public string FileName { get; set; }

        public override Skydome Create() { return new SkydomeBitmap(this); }
    }

    [Serializable]
    public class XMLSkydomeGradient : XMLSkydome
    {
        public XMLColour Top { get; set; }
        public XMLColour Middle { get; set; }
        public XMLColour Bottom { get; set; }

        public override Skydome Create() { return new SkydomeGradient(this); }
    }

    [Serializable]
    [XmlInclude(typeof(XMLTextureSolid))]
    [XmlInclude(typeof(XMLTexturePerTriangle))]
    [XmlInclude(typeof(XMLTextureSquares))]
    [XmlInclude(typeof(XMLTextureBitmap))]
    public class XMLTexture
    {
        public bool ConvertToLinearRGB;

        public virtual Texture Create() { return null; }
    }

    [Serializable]
    public class XMLTextureSolid : XMLTexture
    {
        public XMLColour Colour;

        public override Texture Create() => new TextureSolid(this);
    }

    [Serializable]
    public class XMLNormalMap
    {
        public string Name { get; set; }
        public string FileName { get; set; }

        public virtual NormalMap Create() => new NormalMap(this);
    }

    [Serializable]
    [XmlInclude(typeof(XMLTextureBitmap))]
    public class XMLMaterial
    {
        public string Name;
        public Guid Guid;

        public MaterialType Type;

        public SamplerWrapMode SamplerWrapModeU;
        public SamplerWrapMode SamplerWrapModeV;

        public float PhongDiffuse;
        public float PhongSpecular;
        public float PhongShininess;

        public float roughness;
        public float transparency;
        public float reflectivity;

        public XMLNormalMap NormalMap;

        public XMLTexture /*Bitmap*/ EmissiveTexture;
        public XMLColour EmissiveFactor;
        public float EmissiveStrength;

        public float RefractiveIndex;

        public XMLBrdf Brdf;
        public XMLTexture Texture;

        public virtual Material Create() => new Material(this);
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
        public Guid UID { get; set; }
        public bool Enabled;
        public string Name;
    }

    [Serializable]
    public class XMLShape : XMLEntity
    {
        public XMLPoint Pos { get; set; }

        public float Scale { get; set; }
        public float RotateX { get; set; }
        public float RotateY { get; set; }
        public float RotateZ { get; set; }

        //public XMLMaterial Material;
        public Guid MaterialGuid { get; set; }

        public virtual Shape Create(Scene scene) { return null; }
    }

    [Serializable]
    public class XMLSphere : XMLShape
    {
        public int Radius;

        public override Shape Create(Scene scene) { return new Sphere(scene, this); }
    }

    [Serializable]
    public class XMLDisc : XMLShape
    {
        public XMLVector Normal;
        //public float Radius;

        public override Shape Create(Scene scene) { return new Disc(scene, this); }
    }

    [Serializable]
    public class XMLPlane : XMLShape
    {
        public XMLVector Normal;

        public override Shape Create(Scene scene) { return new Plane(scene, this); }
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
        public string DirectoryName;

        public List<XMLPoint> Points;
        public List<XMLTexel> Texels;
        public List<XMLVector> Normals;
        public List<XMLTriangle> Triangles;

//        public XMLTexture Texure;
        //public Guid MaterialGuid;

        public override Shape Create(Scene scene)
        {
            if (string.IsNullOrEmpty(FileName))
            {
                return new Model(scene, this);
            }
            else
            {
                Shape shape = scene.ImportModel(Path.Join(DirectoryName, FileName));
                if (shape is not null)
                {
                    shape.Pos = new Point(Pos);
                    shape.Scale = Scale;
                    shape.RotateX = RotateX;
                    shape.RotateY = RotateY;
                    shape.RotateZ = RotateZ;
                }
                return shape;
            }
        }
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

        public virtual Light Create(Scene scene) { return null; }
    }

    [Serializable]
    public class XMLPointLight : XMLLight
    {
        public XMLPoint Pos;

        public override Light Create(Scene scene) { return new PointLight(scene, this); }
    }

    [Serializable]
    public class XMLDistantLight : XMLLight
    {
        public XMLVector Direction;

        public override Light Create(Scene scene) { return new DistantLight(scene, this); }
    }

    [Serializable]
    public class XMLScene
    {
        public int Version = 1;
        public DateTime Date = DateTime.Now;

        public List<XMLMaterial> Materials = new();
        public List<XMLShape> Shapes = new();
        public List<XMLLight> Lights = new();

        public XMLCamera Camera;
        public XMLColour Ambient;
        public XMLColour Background;
        public XMLSkydome Skydome { get; set; }
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
