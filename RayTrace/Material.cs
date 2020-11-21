
using System;
using System.Drawing;

namespace RayTrace
{
    public enum MaterialType
    {
        Diffuse, Reflective, RefractiveAndReflective, Phong
    }

    // Characteristics of a Shape's physical form.
    public class Material
    {
        public MaterialType Type { get; set; } = MaterialType.Diffuse;

        // Ratio of reflected light to incident light (Pd), i.e. how much light is reflected.
        //public Colour Albedo { get; set; } = new Colour(0.18f, 0.18f, 0.18f);

        // Phong reflection constants.
        public double PhongDiffuse = 0.75;      // Phong specular coefficient ('kd')
        public double PhongSpecular = 0.5;      // Specular reflection coefficient/constant ('ks')
        public double PhongShininess = 4.0;     // 'Shininess' of surface, 1.0 (matt) - 100 (shiny) ('ns'), 'specular exponent'

        private float roughness;
        private float transparency; // Transparency coefficient kt, range 0.0 to 1.0.
        private float reflectivity;

        public float Roughness { get { return roughness; } set { roughness = Math.Min(1.0f, Math.Max(0.0f, value)); } }
        public float Transparency { get { return transparency; } set { transparency = Math.Min(1.0f, Math.Max(0.0f, value)); } }
        public float Reflectivity { get { return reflectivity; } set { reflectivity = Math.Min(1.0f, Math.Max(0.0f, value)); } }

        public bool HasRoughness() => Roughness > 0.0f;
        public bool HasTransparency() => Transparency > 0.0f;
        public bool HasReflectivity() => Reflectivity > 0.0f;

        //// True if the Shape is considered solid, or false if at least part of the interior is empty.
        //public bool IsSolid { get; set; }

        public float RefractiveIndex { get; set; } = RayTrace.RefractiveIndex.WindowGlass;

        public Texture Texture { get; set; }

        public Material() : this(Palette.White) { }

        public Material(Colour c) : this(new TextureSolid(c)) { }

        public Material(Texture texture)
        {
            Texture = texture;

            roughness = 0.0f;
            transparency = 0.0f;
            reflectivity = 0.0f;
        }

        public virtual Colour GetColour(HitResult hr) { return Texture.GetColour(hr); }

        public Vector GetReflection(HitResult hr)
        {

            return new Vector() ;
        }

        public override string ToString()
        {
//            return "Texture[" + Texture.ToString() + "]";
            return "[" + GetType().Name + "]";
        }
    }
}
