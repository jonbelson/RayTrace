using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    // Base class of Bidirectional Reflectance Distribution Functions.
    public abstract class BRDF
    {
        public string Name { get; set; }

        public abstract Colour GetColour(HitResult hr, Colour colour, SVector source, Colour Li);

        public abstract XMLBrdf ToXML();
    }

    public class Lambertian : BRDF
    {
        //public Colour Colour { get; set; } = Palette.Copper;    // XXX Should use TextureSolid
        //public Texture Texture { get; set; }

        public Lambertian()
        {
            Name = "Lambertian (BRDF)";
        }

        public Lambertian(XMLBrdf xml)
        {
            Name = xml.Name;
        }

        //public Lambertian(Colour colour) : base()
        //{
        //    Colour = colour;
        //}

        //public Lambertian(Texture texture) : base()
        //{
        //    Texture = texture;
        //}

        // Calculate Colour at point given in hr based on radiance (L) coming from direction source.
        // colour   Colour of surface at bit point.
        // source   Vector pointing outwards from hit point.
        // Li       Colour received from hit point.
        public override Colour GetColour(HitResult hr, Colour colour, SVector source, Colour Li)
        {
            // Calculate wo (L towards viewer)
/*            SVector up = new(0, 0, 1);
            float coswo = up.DotProduct(hr.Ray.Normalise());
            float wo = (float) Math.Acos(coswo);

            // Calculate wi (L from source)
            float coswi = up.DotProduct(source.Normalise().Negate());
            float wi = (float)Math.Acos(coswi);
*/
            Colour Lo = (colour / Math.PI) * Li * hr.EntryNormal.DotProduct(source);

            return Lo;
        }

        //public override Colour GetBaseColour()
        //{
        //    // XXX Texture
        //    return Texture.GetColour(new HitResult(null) { u = 0.5f, v = 0.5f });
        //}

        public override XMLBrdf ToXML()
        {
            XMLLambertian xml = new();

            xml.Name = Name;

            return xml;
        }
    }

    // Implementaiton of Cook-Torrence reflection model.
    // http://www.codinglabs.net/article_physically_based_rendering_cook_torrance.aspx
    public class CookTorrence : BRDF
    {
        //public Colour Colour { get; set; } = Palette.Copper;    // XXX Should use TextureSolid
        //public Texture Texture { get; set; }

        // 'alpha' in Range 0 - 1 (lower = shinier, higher = rougher)
        public float Roughness { get; set; } = 0.5f;

        // 'eta' - absorption coefficient 1+
        public float Absorption { get; set; } = 1.2f;

        // 'kd' - proportion of light that is specular, range 0 - 1
        // Specular part is 1-kd;
        private float _shininess = 0.5f;
        public float Shininess {
            get { return _shininess; }
            set { _shininess = Math.Clamp(value, 0, 1); }
        }

        public enum SurfaceType { Dielectric, Conductor };
        public SurfaceType Type { get; set; } = SurfaceType.Conductor;

        public float Ior { get; set; } = 1.2f;

        // Return 1 if x > 0, 0 if x <= 0
        private static float CalculateChi(float x)
        {
            return (x > 0) ? 1 : 0;
        }

        // GGX (Trowbridge-Reitz)
        // http://graphicrants.blogspot.com/2013/08/specular-brdf-reference.html
        // m    Microfacet normal, i.e. the half vector.
        // n    Macro surface normal.
        private float CalculateD_GGX(SVector m, SVector n /*, SVector h*/)
        {
            float alphaSqr = Roughness * Roughness;
            float NdotM = n.DotProductSaturated(m);
            float val = NdotM * NdotM * (alphaSqr - 1) + 1;

            float numerator = alphaSqr;
            float denominator = (float) Math.PI * val * val;

            return numerator / denominator;
        }

        // D is distribution factor.
        // GGX model
        // m    Direction vector to check.
        // n    Surface normal.
        // h    Half vector.
        private float CalculateD(SVector m, SVector n, SVector h)
        {
            float alphaSqr = Roughness*Roughness;
            float numerator = alphaSqr * CalculateChi(h.DotProduct(n));

            float dpmn = m.DotProduct(n);
            float dpmnSqr = dpmn * dpmn;

            float val2 = alphaSqr + (1.0f - dpmnSqr) / dpmnSqr; // XXX Check extra divide of dpmnSqr
            float val = dpmnSqr * val2;
            float denominator = (float)Math.PI * val * val;

            float f = numerator / denominator;

            return f;
        }

        // F is Fresnel factor.
        // theta    Angle between View vector and half vector.
        // ior1     IOR of medium.
        // ior2     IOR of conductor.
        private float CalculateFDielectric(float theta, float ior1 /*, float ior2*/)
        {
            // For dielectric.
            float f = (ior1 - Ior) / (ior1 + Ior);
            float F0 = f * f;
            float v = 1 - (float)Math.Cos(theta);
            float vPow5 = v * v * v * v * v;
            float F = F0 + (1 - F0) * vPow5;

            return F;
        }

        // F is Fresnel factor.
        // theta    Angle between View vector and half vector.
        // ior1     IOR of medium.
        // ior2     IOR of conductor.
        private float CalculateFConductor(float theta, float ior1, float ior2)
        {
            float v2 = (ior2 - 1) * (ior2 - 1);
            float v3 = (ior2 + 1) * (ior2 + 1);

            float v = 1 - (float)Math.Cos(theta);
            float vPow5 = v * v * v * v * v;

            float numerator = v2 + 4 * ior2 * vPow5 + Absorption * Absorption;
            float denominator = v3 + Absorption * Absorption;

            return numerator / denominator;
        }

        // w    Angle to check, view vector.
        // m    Half vector.
        // n    Surface normal.
        //private float CalculateGp(SVector w, SVector m, SVector n)
        //{

        //}


        // v    Direction of viewer.
        // n    Normal at point.
        private float CalculateG_GGX(SVector v, SVector n)
        {
            float NdotV = n.DotProductSaturated(v);

            float numerator = 2 * NdotV;

            float alphaSqr = Roughness * Roughness;
            float val = alphaSqr + (1 - alphaSqr) * NdotV * NdotV;
            float denominator = NdotV + (float) Math.Sqrt(val);

            return numerator / denominator;
        }

        // G is geometry factor.
        // Attenuation of light due to microfacet shadowing.
        // wi   Incoming vector
        // wo   Outgoing vector.
        // m    Direction vector to check (half vector?)
        // n    Surface normal.
        //        private float CalculateG(SVector wi, SVector wo, SVector m, SVector n)
        private float CalculateG(SVector wi, SVector m, SVector n)
        {
            float dpwm = Math.Clamp(wi.DotProduct(m), 0, 1);
            float dpwn = Math.Clamp(wi.DotProduct(n), 0, 1);

            float chi = CalculateChi(dpwm/dpwn);

            float dpwmSqr = dpwm * dpwm;

            float val = 1 + Roughness * Roughness * (1 - dpwmSqr) / dpwmSqr;
            float denominator = 1 + (float)Math.Sqrt(val);
            float numerator = chi * 2;

            return numerator / denominator;
        }

        public CookTorrence()
        {
            Name = "Cook-Torrence (BRDF)";
        }

        public CookTorrence(XMLCookTorrence xml)
        {
            Name = xml.Name;
            Roughness = xml.Roughness;
            Absorption = xml.Absorption;
            Shininess = xml.Shininess;
            Type = xml.Type;
            Ior = xml.Ior;
        }

        //public CookTorrence(Colour colour) : base()
        //{
        //    Colour = colour;
        //}

        //public CookTorrence(Texture texture) : base()
        //{
        //    Texture = texture;
        //}

        // Calculate Colour at point given in hr based on radiance (Li) coming from direction source.
        // colour   Colour of surface at hit point.
        // source   Vector pointing outwards from hit point.
        // Li       Colour received from hit point.
        public override Colour GetColour(HitResult hr, Colour colour, SVector source, Colour Li)
        {
            Debug.Assert(!source.IsNull());
            Debug.Assert(!hr.Ray.IsNull());

            SVector normal = hr.EntryNormal;
            SVector viewDirection = hr.Ray.Normalise().Negate();
            SVector halfVector = (viewDirection + source).Normalise();

            //float fresnel = 1.0f;
            Colour Fc;
            if (Type == SurfaceType.Conductor)
            {
                float fresnel = CalculateFConductor(halfVector.DotProductSaturated(viewDirection), 1.0f, Ior);
                Debug.Assert(!float.IsInfinity(fresnel) && !float.IsNaN(fresnel));
                Fc = colour * fresnel;
            }
            else
            {
                float fresnel = CalculateFDielectric(halfVector.DotProductSaturated(viewDirection), 1.0f);
                Fc = new Colour(fresnel, fresnel, fresnel);
            }

            //float geometry = CalculateG(viewDirection, normal, halfVector)* CalculateG(source, normal, halfVector);
            float geometryView = CalculateG_GGX(viewDirection, normal);
            float geometryLight = CalculateG_GGX(source, normal);
            float geometry = geometryView * geometryLight;

//            float distribution = CalculateD(source, normal, halfVector);
            float distribution = CalculateD_GGX(halfVector, normal);

//            float denominator = 4 * (normal.DotProductSaturated(viewDirection) * halfVector.DotProductSaturated(normal));
            float denominator = 4 * (normal.DotProductSaturated(viewDirection) * normal.DotProductSaturated(source));
            denominator = Math.Clamp(denominator, 0.001f, 1);

            Debug.Assert(denominator > 0);

            //float dp = normal.DotProductSaturated(viewDirection);
            float dp = normal.DotProductSaturated(source);  // ??
//            Colour diffuse = Li * Colour * (1 - Shininess) * dp / Math.PI;
            Colour diffuse = colour * (1 - Shininess) / Math.PI;

            //float DFG = distribution * fresnel * geometry;
            Colour DFG = Fc * distribution * geometry;
            //Colour specular = colour * Li * Shininess * DFG / denominator * dp;
            //float specularf = Shininess * DFG / denominator;
            Colour specular = DFG * Shininess / denominator;
           //Colour specular = new(specularf, specularf, specularf);

            return (diffuse + specular) * Li * dp; ;
        }

        //public override Colour GetBaseColour()
        //{
        //    // XXX Texture
        //    return Texture.GetColour(new HitResult(null) { u = 0.5f, v = 0.5f });
        //}

        public override XMLBrdf ToXML()
        {
            XMLCookTorrence xml = new();

            xml.Name = Name;
            xml.Ior = Ior;
            xml.Roughness = Roughness;
            xml.Absorption = Absorption;
            xml.Shininess = Shininess;
            xml.Type = Type;

            return xml;
        }
    }
}
