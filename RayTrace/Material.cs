
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;

namespace RayTrace
{
    public enum MaterialType
    {
        Diffuse, Reflective, RefractiveAndReflective, Phong, BRDF_Lambertian, BRDF_CookTorrence
    }

    public class MaterialManager
    {
        private List<Material> _materials;

        public ReadOnlyCollection<Material> Materials { get { return _materials.AsReadOnly(); } }

        private readonly Material _defaultMaterial = new Material(Palette.Grey) { Name = "Default" };


        public MaterialManager()
        {
            _materials.Add(_defaultMaterial);

        }

        public Material GetDefaultMaterial()
        {
            return _defaultMaterial;
        }

        public Material CreateMaterial()
        {
            Material material = new Material();
            _materials.Add(material);
            return material;
        }

    }

    // Characteristics of a Shape's physical form.
    public class Material
    {
        public string Name { get; set; } = "Material";
        public Guid Guid { get; set; } = Guid.NewGuid();

        private MaterialType _type = MaterialType.Diffuse;
        public MaterialType Type { get { return _type; } }

        // Ratio of reflected light to incident light (Pd), i.e. how much light is reflected.
        //public Colour Albedo { get; set; } = new Colour(0.18f, 0.18f, 0.18f);

        // Phong reflection constants.
        public double PhongDiffuse { get; set; } = 0.75;      // Phong specular coefficient ('kd')
        public double PhongSpecular { get; set; } = 0.5;      // Specular reflection coefficient/constant ('ks')
        public double PhongShininess { get; set; } = 4.0;     // 'Shininess' of surface, 1.0 (matt) - 100 (shiny) ('ns'), 'specular exponent'

        private float roughness;
        private float transparency; // Transparency coefficient kt, range 0.0 to 1.0.
        private float reflectivity;

        public float Roughness { get => roughness; set => roughness = Math.Clamp(value, 0.0f, 1.0f); }
        public float Transparency { get => transparency; set => transparency = Math.Clamp(value, 0.0f, 1.0f); }
        public float Reflectivity { get => reflectivity; set => reflectivity = Math.Clamp(value, 0.0f, 1.0f); }

        public bool HasRoughness()
        {
            return Roughness > 0.0f;
        }

        public bool HasTransparency()
        {
            return Transparency > 0.0f;
        }

        public bool HasReflectivity()
        {
            return Reflectivity > 0.0f;
        }

        //// True if the Shape is considered solid, or false if at least part of the interior is empty.
        //public bool IsSolid { get; set; }

        public float RefractiveIndex { get; set; } = RayTrace.RefractiveIndex.WindowGlass;

        public BRDF Brdf { get; set; }
        public Texture Texture { get; set; }

        public Material() : this(Palette.White) { }

        public Material(Colour c) : this(new TextureSolid(c)) { }

        public Material(MaterialType type)
        {
            SetType(type);
        }

        public Material(Texture texture)
        {
            Texture = texture;

            roughness = 0.0f;
            transparency = 0.0f;
            reflectivity = 0.0f;
        }

        public Material(XMLMaterial xmlMaterial)
        {
            _type = xmlMaterial.Type;
            PhongDiffuse = xmlMaterial.PhongDiffuse;
            PhongShininess = xmlMaterial.PhongShininess;
            PhongSpecular = xmlMaterial.PhongSpecular;
            roughness = xmlMaterial.roughness;
            transparency = xmlMaterial.transparency;
            reflectivity = xmlMaterial.reflectivity;
            RefractiveIndex = xmlMaterial.RefractiveIndex;

            Texture = xmlMaterial.Texture.Create();
        }

        public void SetType(MaterialType type)
        {
            if (type != _type)
            {
                switch (type)
                {
                    case MaterialType.Diffuse:
                    case MaterialType.Reflective:
                    case MaterialType.RefractiveAndReflective:
                    case MaterialType.Phong:
                        Brdf = null;
                        break;
                    case MaterialType.BRDF_Lambertian:
                        Brdf = new Lambertian();
                        break;
                    case MaterialType.BRDF_CookTorrence:
                        Brdf = new CookTorrence();
                        break;
                    default:
                        throw new NotImplementedException();
                }

                _type = type;
            }
        }

        public virtual Colour GetColour(HitResult hr)
        {
            return Texture.GetColour(hr);
        }

        public Colour GetBrdfColour(HitResult hr, SVector source, Colour Li)
        {
            if (Brdf != null)
            {
                return Brdf.GetColour(hr, GetColour(hr), source, Li);
            }

            return Palette.Black;
        }

        public virtual Colour GetBaseColour()
        {
            //if (Brdf != null) return Brdf.GetBaseColour();

            return Texture.GetBaseColour();
        }

        //public Vector GetReflection(HitResult hr)
        //{

        //    return new Vector() ;
        //}

        public XMLMaterial ToXML()
        {
            XMLMaterial xmlMaterial = new();

            xmlMaterial.Type = Type;
            xmlMaterial.PhongDiffuse = PhongDiffuse;
            xmlMaterial.PhongShininess = PhongShininess;
            xmlMaterial.PhongSpecular = PhongSpecular;

            xmlMaterial.roughness = roughness;
            xmlMaterial.transparency = transparency;
            xmlMaterial.reflectivity = reflectivity;
            xmlMaterial.RefractiveIndex = RefractiveIndex;

            xmlMaterial.Texture = Texture.ToXML();

            return xmlMaterial;
        }

        public override string ToString()
        {
            //return Name;
            if (Name.Length > 12)
            {
                return Name.Substring(0, 10) + "...";
            }
            return Name;
        }
    }
}
