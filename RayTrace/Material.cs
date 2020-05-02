using System;
namespace RayTrace
{
    // Characteristics of a Shape's surface.
    public class Material
    {
        public double ka = 1.0;     // Ambient reflection coefficient.
        public double kd = 1.0;     // Diffuse reflection coefficient (0 - 1).

        // Phong reflection constants.
        //public double Il = 1.0;
        public double ks = 0.5;     // Specular reflection coefficient/constant.
        public double ns = 4.0;    // 'Shininess' of surface, 1.0 (matt) - 100 (shiny)

        private float roughness;
        private float transparency; // Transparency coefficient kt, range 0.0 to 1.0.
        private float reflectivity;

        public float Roughness { get { return roughness; } set { roughness = Math.Min(1.0f, Math.Max(0.0f, value)); } }
        public float Transparency { get { return transparency; } set { transparency = Math.Min(1.0f, Math.Max(0.0f, value)); } }
        public float Reflectivity { get { return reflectivity; } set { reflectivity = Math.Min(1.0f, Math.Max(0.0f, value)); } }

        public bool HasRoughness() => Roughness > 0.0f;
        public bool HasTransparency() => Transparency > 0.0f;
        public bool HasReflectivity() => Reflectivity > 0.0f;

//        public float IndexOfRefractive { get; set; } = RefractiveIndex.WindowGlass;
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

        public override string ToString()
        {
//            return "Texture[" + Texture.ToString() + "]";
            return "[" + GetType().Name + "]";
        }
    }
}
