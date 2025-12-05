
using RayTrace.Brdfs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace RayTrace
{
    public enum MaterialType
    {
        Diffuse, Reflective, RefractiveAndReflective, Phong, BRDF_Lambertian, BRDF_CookTorrence, BRDF_Gltf
    }

    public enum SamplerWrapMode
    {
        Repeat,
        Clamp,
        Mirror
    }

    /*
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
    */

    //
    // Characteristics of a Shape's physical form.
    //
    public class Material : INotifyPropertyChanged
    {
        public string Name { get; set; } = "Material";
        public Guid Guid { get; set; } = Guid.NewGuid();

        private MaterialType _type = MaterialType.Diffuse;
        public MaterialType Type { get { return _type; } }

        public SamplerWrapMode SamplerWrapModeU { get; set; } = SamplerWrapMode.Repeat;
        public SamplerWrapMode SamplerWrapModeV { get; set; } = SamplerWrapMode.Repeat;

        // Ratio of reflected light to incident light (Pd), i.e. how much light is reflected.
        //public Colour Albedo { get; set; } = new Colour(0.18f, 0.18f, 0.18f);

        // Phong reflection constants.
        public float PhongDiffuse { get; set; } = 0.75f;      // Phong specular coefficient ('kd')
        public float PhongSpecular { get; set; } = 0.5f;      // Specular reflection coefficient/constant ('ks')
        public float PhongShininess { get; set; } = 4.0f;     // 'Shininess' of surface, 1.0 (matt) - 100 (shiny) ('ns'), 'specular exponent'

        private float roughness;
        private float transparency; // Transparency coefficient kt, range 0.0 to 1.0.
        private float reflectivity;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void Notify([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public float Roughness { get => roughness; set { roughness = Math.Clamp(value, 0.0f, 1.0f); Notify(); } }
        public float Transparency { get => transparency; set { transparency = Math.Clamp(value, 0.0f, 1.0f); Notify(); } }
        public float Reflectivity { get => reflectivity; set { reflectivity = Math.Clamp(value, 0.0f, 1.0f); Notify(); } }

        public NormalMap NormalMap { get; set; }

        public Texture /*Bitmap*/ EmissiveTexture { get; set; }

        public float EmissiveStrength { get; set { reflectivity = Math.Clamp(value, 0.0f, 1.0f); Notify(); } } = 1;
        public Colour EmissiveFactor { get; set { field = value; Notify(); } } = new(1f, 1f, 1f);

        public float RefractiveIndex { get; set; } = RayTrace.RefractiveIndex.WindowGlass;

        public Brdfs.Brdf Brdf { get; set; }

        private Texture _texture = null;
        public Texture Texture
        {
            get => _texture;
            set { _texture = value; Notify("Texture"); }
        }

        public bool HasRoughness() => Roughness > 0.0f;

        public bool HasTransparency() => Transparency > 0.0f;

        public bool HasReflectivity() => Reflectivity > 0.0f;

        public bool HasNormalMap() => NormalMap!= null;

        //// True if the Shape is considered solid, or false if at least part of the interior is empty.
        //public bool IsSolid { get; set; }

        //
        // Constructor
        //
        public Material() : this(Palette.White) { }

        //
        // Constructor
        //
        public Material(Colour c) : this(new TextureSolid(c)) { }

        //
        // Constructor
        //
        public Material(MaterialType type)
        {
            SetType(type);
        }

        //
        // Constructor
        //
        public Material(Texture texture)
        {
            Texture = texture;

            roughness = 0.0f;
            transparency = 0.0f;
            reflectivity = 0.0f;
        }

        //
        // Constructor
        //
        public Material(XMLMaterial xmlMaterial)
        {
            Restore(xmlMaterial);
        }

        public void Restore(XMLMaterial xmlMaterial)
        {
            _type = xmlMaterial.Type;

            Name = xmlMaterial.Name;
            Guid = xmlMaterial.Guid;

            SamplerWrapModeU = xmlMaterial.SamplerWrapModeU;
            SamplerWrapModeV = xmlMaterial.SamplerWrapModeV;

            PhongDiffuse = xmlMaterial.PhongDiffuse;
            PhongShininess = xmlMaterial.PhongShininess;
            PhongSpecular = xmlMaterial.PhongSpecular;
            roughness = xmlMaterial.roughness;
            transparency = xmlMaterial.transparency;
            reflectivity = xmlMaterial.reflectivity;
            RefractiveIndex = xmlMaterial.RefractiveIndex;

            EmissiveStrength = xmlMaterial.EmissiveStrength;
            EmissiveFactor = new(xmlMaterial.EmissiveFactor);

            if (xmlMaterial.NormalMap is not null)
            {
                NormalMap = xmlMaterial.NormalMap.Create();
            }

            if (xmlMaterial.EmissiveTexture is not null)
            {
                Texture bmp = xmlMaterial.EmissiveTexture.Create();
                if (bmp is TextureBitmap textureBitmap)
                {
                    EmissiveTexture = textureBitmap;
                }
            }

            if (xmlMaterial.Brdf != null)
            {
                Brdf = xmlMaterial.Brdf.Create();
            }

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
                    case MaterialType.BRDF_Gltf:
                        Brdf = new GltfBrdf();
                        break;
                    default:
                        throw new NotImplementedException();
                }

                _type = type;
            }
        }

        public virtual Colour GetColour(HitResult hr)
        {
            if (Texture != null)
            {
                return Texture.GetColour(hr);
            }

            return Palette.Grey;
        }

        // Calculate the colour of the hit point based on a light sample from the specified direction.
        // lcs      A local coordinate system based around the 'hit' surface normal.
        // hr       The point hit on this Material.
        // source   A vector from hr towards the source of the incoming light.
        // Li       The colour of the incoming light.
        public SColour GetBrdfColour(LocalCoordinateSystem lcs, HitResult hr, Random rand, SVector source, SColour Li)
        {
            if (Brdf != null)
            {
                SColour c = Brdf.GetColour(hr, lcs, rand, GetColour(hr).ToSColour(), source, Li);

                //if (EmissiveTexture != null)
                //{
                //    Colour e = EmissiveTexture.GetColour(hr);
                //    c += e;
                //}

                return c;
            }

            return SPalette.Black;
        }

        public SColour GetEmissiveColour(HitResult hr)
        {
            Colour c = EmissiveTexture?.GetColour(hr) ?? Palette.Black;
            
            c = c * EmissiveStrength * EmissiveFactor;

            SColour sc = c.ToSColour();
            return sc;
        }

        public virtual Colour GetBaseColour()
        {
            //if (Brdf != null) return Brdf.GetBaseColour();

            if (Texture != null)
            {
                return Texture.GetBaseColour();
            }

            return Palette.Grey;
        }

        //public Vector GetReflection(HitResult hr)
        //{

        //    return new Vector() ;
        //}

        public XMLMaterial ToXML()
        {
            XMLMaterial xmlMaterial = new();

            xmlMaterial.Name = Name;
            xmlMaterial.Guid = Guid;
            xmlMaterial.Type = Type;

            xmlMaterial.SamplerWrapModeU = SamplerWrapModeU;
            xmlMaterial.SamplerWrapModeV = SamplerWrapModeV;

            xmlMaterial.PhongDiffuse = PhongDiffuse;
            xmlMaterial.PhongShininess = PhongShininess;
            xmlMaterial.PhongSpecular = PhongSpecular;

            xmlMaterial.roughness = roughness;
            xmlMaterial.transparency = transparency;
            xmlMaterial.reflectivity = reflectivity;
            xmlMaterial.RefractiveIndex = RefractiveIndex;

            xmlMaterial.EmissiveStrength = EmissiveStrength;
            xmlMaterial.EmissiveFactor = EmissiveFactor.ToXML();

            if (EmissiveTexture != null)
            {
                xmlMaterial.EmissiveTexture = EmissiveTexture.ToXML();
            }

            if (NormalMap != null)
            {
                xmlMaterial.NormalMap = NormalMap.ToXML();
            }

            if (Brdf != null)
            {
                xmlMaterial.Brdf = Brdf.ToXML();
            }

            if (Texture != null)
            {
                xmlMaterial.Texture = Texture.ToXML();
            }

            return xmlMaterial;
        }

        public override string ToString()
        {
            //return Name;
            if (Name is not null && Name.Length > 12)
            {
                return string.Concat(Name.AsSpan(0, 10), "...");
            }
            return Name ?? "Unnamed";
        }
    }
}
