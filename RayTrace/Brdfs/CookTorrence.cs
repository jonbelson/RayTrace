using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace.Brdfs
{
    // Implementaiton of Cook-Torrence reflection model.
    // http://www.codinglabs.net/article_physically_based_rendering_cook_torrance.aspx
    public class CookTorrence : Brdf
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
        public float Shininess
        {
            get { return _shininess; }
            set { _shininess = Math.Clamp(value, 0f, 1f); }
        }

        public enum SurfaceType { Dielectric, Conductor };
        public SurfaceType Type { get; set; } = SurfaceType.Conductor;

        public float Ior { get; set; } = 1.2f;

        // Cosine-weighted random vector in local hemisphere.
        public override SVector GetSample(HitResult hr, LocalCoordinateSystem lcs, Random rand, SColour colour, out float pdf)
        {
            float r1 = rand.NextSingle();
            float r2 = rand.NextSingle();

            float phi = 2f * MathF.PI * r1;
            float sqrtR2 = MathF.Sqrt(r2);

            float x = MathF.Cos(phi)*sqrtR2;
            float z = MathF.Sin(phi)*sqrtR2;
            float y = MathF.Sqrt(1f - r2);

            pdf = y/MathF.PI;

            return new SVector(x, y, z, 0f);
        }

        // Calculate reflectance.
        // colour   Colour of surface at hit point (linear RGB).
        // Returns reflectance at hit point (linear RGB).
        public override SColour GetBrdfReflectance(HitResult hr, LocalCoordinateSystem lcs, Random rand, SColour colour, SVector source)
        {
            Debug.Assert(!source.IsNull());
            Debug.Assert(!hr.Ray.IsNull());

            //colour = colour.ToLinearRGB();

            SVector normal = hr.EntryNormal;
            SVector viewDirection = hr.Ray.Normalise().Negate();
            SVector halfVector = (viewDirection + source).Normalise();

            //float fresnel = 1.0f;
            SColour Fc;
            if (Type == SurfaceType.Conductor)
            {
                float fresnel = CalculateFConductor(halfVector.DotProductSaturated(viewDirection), 1.0f, Ior);
                Debug.Assert(!float.IsInfinity(fresnel) && !float.IsNaN(fresnel));
                Fc = colour * fresnel;
            }
            else
            {
                float fresnel = CalculateFDielectric(halfVector.DotProductSaturated(viewDirection), 1.0f);
                Fc = new SColour(fresnel, fresnel, fresnel);
            }

            //float geometry = CalculateG(viewDirection, normal, halfVector)* CalculateG(source, normal, halfVector);
            float geometryView = CalculateG_GGX(viewDirection, normal);
            float geometryLight = CalculateG_GGX(source, normal);
            float geometry = geometryView * geometryLight;

            //            float distribution = CalculateD(source, normal, halfVector);
            float distribution = CalculateD_GGX(halfVector, normal);

            //            float denominator = 4 * (normal.DotProductSaturated(viewDirection) * halfVector.DotProductSaturated(normal));
            float denominator = 4 * (normal.DotProductSaturated(viewDirection) * normal.DotProductSaturated(source));
            denominator = Math.Clamp(denominator, 0.001f, 1f);

            Debug.Assert(denominator > 0f);

            //float dp = normal.DotProductSaturated(viewDirection);
            //            float dp = normal.DotProductSaturated(source);  // ??
            float dp = normal.DotProduct(source);  // ??

            //Debug.Assert(dp >= 0f && dp <= 1f);

            //Colour diffuse = Li * Colour * (1 - Shininess) * dp / Math.PI;
            SColour diffuse = colour * (1f - Shininess) / MathF.PI;

            //float DFG = distribution * fresnel * geometry;
            SColour DFG = Fc * distribution * geometry;
            //Colour specular = colour * Li * Shininess * DFG / denominator * dp;
            //float specularf = Shininess * DFG / denominator;
            SColour specular = DFG * Shininess / denominator;
            //Colour specular = new(specularf, specularf, specularf);

            SColour Lo = (diffuse + specular);

            //float pdf = dp/MathF.PI;
            //Lo /= pdf;

            return Lo;//.FromLinearRGB();
        }

        protected override float F(in SVector v) { return 1f; }
        protected override float Pdf(in SVector v) { return 1f; }

        // Return 1 if x > 0, 0 if x <= 0
        private static float CalculateChi(float x)
        {
            return x > 0 ? 1 : 0;
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
            float denominator = MathF.PI * val * val;

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
            float denominator = MathF.PI * val * val;

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
            float v = 1 - MathF.Cos(theta);
            float vPow5 = v * v * v * v * v;
            float F = F0 + (1f - F0) * vPow5;

            return F;
        }

        // F is Fresnel factor.
        // theta    Angle between View vector and half vector.
        // ior1     IOR of medium.
        // ior2     IOR of conductor.
        private float CalculateFConductor(float theta, float ior1, float ior2)
        {
            float v2 = (ior2 - 1f) * (ior2 - 1f);
            float v3 = (ior2 + 1f) * (ior2 + 1f);

            float v = 1.0f - MathF.Cos(theta);
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

            float numerator = 2f * NdotV;

            float alphaSqr = Roughness * Roughness;
            float val = alphaSqr + (1f - alphaSqr) * NdotV * NdotV;
            float denominator = NdotV + MathF.Sqrt(val);

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
            float dpwm = Math.Clamp(wi.DotProduct(m), 0f, 1f);
            float dpwn = Math.Clamp(wi.DotProduct(n), 0f, 1f);

            float chi = CalculateChi(dpwm/dpwn);

            float dpwmSqr = dpwm * dpwm;

            float val = 1f + Roughness * Roughness * (1f - dpwmSqr) / dpwmSqr;
            float denominator = 1f + MathF.Sqrt(val);
            float numerator = chi * 2f;

            return numerator / denominator;
        }

        //
        // Constructor.
        //
        public CookTorrence()
        {
            Name = "Cook-Torrence (BRDF)";
        }

        //
        // Constructor.
        //
        public CookTorrence(XMLCookTorrence xml)
        {
            Name = xml.Name;
            Roughness = xml.Roughness;
            Absorption = xml.Absorption;
            Shininess = xml.Shininess;
            Type = xml.Type;
            Ior = xml.Ior;
        }

        // Calculate Colour at point given in hr based on radiance (Li) coming from direction 'source'.
        // colour   Colour of surface at hit point.
        // source   A vector from hr towards the source of the incoming light.
        // Li       The colour of the incoming light.
        public override SColour GetColour(HitResult hr, LocalCoordinateSystem lcs, Random rand, SColour colour, SVector source, SColour Li)
        {
            Debug.Assert(!source.IsNull());
            Debug.Assert(!hr.Ray.IsNull());

            SVector N = hr.EntryNormal.Normalise();
            SVector L = source;

            float NdotL = N.DotProduct(source);
            if (NdotL <= 0f)
            {
                return SPalette.Black;
            }

            NdotL = Math.Clamp(NdotL, 0.0001f, 1f);

            SColour Lo = GetBrdfReflectance(hr, lcs, rand, colour, source)*Li*NdotL;

            return Lo;
        }

        public override XMLBrdf ToXML()
        {
            XMLCookTorrence xml = new()
            {
                Name = Name,
                Ior = Ior,
                Roughness = Roughness,
                Absorption = Absorption,
                Shininess = Shininess,
                Type = Type
            };

            return xml;
        }
    }
}
