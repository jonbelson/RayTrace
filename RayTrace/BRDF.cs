using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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

        // Calculate Colour at point given in hr based on radiance (Li) coming from direction 'source'.
        // colour   Colour of surface at hit point.
        // source   A vector from hr towards the source of the incoming light.
        // Li       The colour of the incoming light.
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

    // Reference BRDF in glTF specification.
    // V    Normalised vector towards eye.
    // L    Normalised vector to light.
    // N    Surface normal.
    // H    Normalised half vector |L + V|
    public class GltfBRDF : BRDF
    {
        public float _metallic { get; set; }

        public float Metallic {
            get => _metallic;
            set { Math.Clamp(_metallic, 0.0f, 1.0f); _metallic = value; }
        }

        public float Ior { get; set; } = 1.5f;

        private float _roughness = 0.5f;
        public float Roughness {
            get => _roughness;
            set { _roughness = Math.Clamp(value, 0, 1); }
        }

        private float Alpha { get => _roughness*_roughness; }

        private Colour _f0 = new(0.04f, 0.04f, 0.04f);
        private Colour _f90 = new(1.0f, 1.0f, 1.0f);

        // https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#fresnel
        // f0   Reflection colour at 0 degree/normal incidence.
        // f90  Reflection colour at 90 degree incidence.
        private static float ConductorFresnel(float f0, float VdotH)
        {
            //float v = Math.Clamp(1.0f - Math.Abs(VdotH), 0.0f, 1.0f);
            float v = 1.0f - VdotH;
            float v5 = v*v*v*v*v;

            Debug.Assert(v >= 0.0f && v <= 1.0f);

            return f0 + (1.0f - f0)*v5;
        }
/*
        private static Colour ConductorFresnel(Colour f0, Colour bsdf, float VdotH)
        {
            //float v = Math.Clamp(1.0f - Math.Abs(VdotH), 0.0f, 1.0f);
            float v = 1.0f - VdotH;
            float v5 = v*v*v*v*v;

            Colour tint = new(1.0f - f0.Red, 1.0f - f0.Green, 1.0f - f0.Blue);
            //Colour tint = Palette.White - f0;
            return bsdf*(f0 + (tint*v5));
        }
*/
        private static Colour ConductorFresnel(Colour f0, Colour f90, float VdotH)
        {
            //float v = Math.Clamp(1.0f - Math.Abs(VdotH), 0.0f, 1.0f);
            float v = 1.0f - VdotH;
            float v5 = v*v*v*v*v;

            //Colour tint = new(1.0f - f0.Red, 1.0f - f0.Green, 1.0f - f0.Blue);
            //Colour tint = Palette.White - f0;

            return f0 + (f90 - f0)*v5;
        }



        // Dialectric BRDF
        // https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#fresnel
        // base     Diffuse component.
        // layer    Specular component.
        private static Colour FresnelMix(float ior, Colour _base, Colour layer, float VdotH)
        {
            float f = (1.0f - ior)/(1.0f + ior);
            float f0 = f*f;
//            float v = 1.0f - Math.Abs(VdotH);
            float v = 1.0f - VdotH;
            float v5 = v*v*v*v*v;

            float f90 = 1.0f;
            float fr = f0 + (f90 - f0)*v5;

            Debug.Assert(fr >= 0.0f && fr <= 1.0f);

            return Colour.LinearBlend(_base, layer, fr);
        }

        private static float Heaviside(float x)
        {
            return (x > 0.0f) ? 1.0f : 0.0f;
        }

        // Calculate GGX microfactet distribution, or 'D'.
        // https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#specular-brdf
        private float CalculateD_GGX(float NdotH)
        {
            float alpha = Alpha;
            float alphaSql = alpha*alpha;

            float val = NdotH*NdotH*(alphaSql - 1.0f) + 1.0f;
            float denominator = (float)Math.PI*val*val;

            //float NdotH = N.DotProductSaturated(H);

            if (denominator != 0)
            {
                float numerator = alphaSql;// *Heaviside(NdotH);

                return numerator/denominator;
            }

            return 0.0f;
        }
        private float CalculateD_Ref(float NdotH)
        {
            float alphaSqr = Alpha*Alpha;

            float val = (NdotH*alphaSqr - NdotH)*NdotH + 1.0f;

            return alphaSqr*((float)Math.PI*val*val);
        }

/*
        // Calculate GGX microfactet distribution, or 'D'.
        // https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#specular-brdf
        private float CalculateD_GGX(SVector N, SVector H)
        {
            float alpha = Alpha;
            float alphaSql = alpha*alpha;

            float NdotH = N.DotProductSaturated(H);

            float numerator = alphaSql*Heaviside(NdotH);

            float val = NdotH*NdotH*(alphaSql - 1.0f) + 1.0f;
            float denominator = (float) Math.PI*val*val;

            return numerator/denominator;
        }
*/
        // https://cdn2.unrealengine.com/Resources/files/2013SiggraphPresentationsNotes-26915738.pdf
        // [Equation 4]
        private float CalculateG_Eq4(float NdotL, float NdotV)
        {
            float val = _roughness + 1;
            float k = val*val/8.0f;

            float Gl = NdotL/(NdotL*(1.0f - k) + k);
            float Gv = NdotV/(NdotV*(1.0f - k) + k);

            Debug.Assert(!Single.IsNaN(Gl));
            Debug.Assert(!Single.IsNaN(Gv));

            return Gl*Gv;
        }


        private float CalculateG_Ref(float NdotL, float NdotV)
        {
            //float val = _roughness + 1;
            //float k = val*val/8.0f;

            float alphaSqr = Alpha*Alpha;

            double denom1 = NdotL*Math.Sqrt(NdotV*NdotV*(1.0f - alphaSqr) + alphaSqr);
            double denom2 = NdotV*Math.Sqrt(NdotL*NdotL*(1.0f - alphaSqr) + alphaSqr);

            Debug.Assert(!Double.IsNaN(denom1));
            Debug.Assert(!Double.IsNaN(denom2));

            float denominator = (float)(denom1 + denom2);
            if (denominator > 0)
            {
                return 0.5f/denominator;
            }
            return 0;
        }

        private float CalculateG_Ref2(float NdotL, float NdotV)
        {
            //float val = _roughness + 1;
            //float k = val*val/8.0f;

            float alphaSqr = Alpha*Alpha;

            double attenuationL = 2.0f*NdotL/(NdotL + Math.Sqrt(alphaSqr + (1.0f - alphaSqr)*NdotL*NdotL));
            double attenuationV = 2.0f*NdotV/(NdotV + Math.Sqrt(alphaSqr + (1.0f - alphaSqr)*NdotV*NdotV));


            Debug.Assert(!Double.IsNaN(attenuationL));
            Debug.Assert(!Double.IsNaN(attenuationV));

            return (float) (attenuationL*attenuationV);
        }


        /*
        private float CalculateG(float NdotL, float NdotV, float HdotL, float HdotV)
        {
            float alpha = Alpha;
            float alphaSql = alpha*alpha;


        }
        */

        private float CalculateV(float NdotL, float NdotV)
        {
//            float denominator = 4.0f*Math.Abs(NdotL)*Math.Abs(NdotV);
            float denominator = 4.0f*NdotL*NdotV;

            Debug.Assert(!Single.IsNaN(denominator));

            if (denominator != 0.0f)
            {
                //float G = CalculateG_Eq4(NdotL, NdotV);
                float G = CalculateG_Ref(NdotL, NdotV);

                return G/denominator;
            }

            return 0.0f;
        }

        // Equation 15
        private float CalculateV_Equ15(float NdotL, float NdotV)
        {
            float alphaSqr = Alpha*Alpha;
            double denom1 = NdotL*Math.Sqrt(NdotV*NdotV*(1.0f - alphaSqr) + alphaSqr);
            double denom2 = NdotV*Math.Sqrt(NdotL*NdotL*(1.0f - alphaSqr) + alphaSqr);

            Debug.Assert(!Double.IsNaN(denom1));
            Debug.Assert(!Double.IsNaN(denom2));

            float denominator = (float) (denom1 + denom2);
            if (denominator > 0)
            {
                return 0.5f/denominator;
            }
            return 0;
        }

        // Equation 16
        // Faster but less accurate
        private float CalculateV_Equ16(float NdotL, float NdotV)
        {
            double denom1 = NdotL*(NdotV*(1.0f - Alpha) + Alpha);
            double denom2 = NdotV*(NdotL*(1.0f - Alpha) + Alpha);

            Debug.Assert(!Double.IsNaN(denom1));
            Debug.Assert(!Double.IsNaN(denom2));

            float denominator = (float)(denom1 + denom2);
            if (denominator != 0)
            {
                return 0.5f/denominator;
            }
            return 0;

        }

        // https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#diffuse-brdf
        private static Colour DiffuseBrdf(Colour c)
        {
            return c*((float)(1.0/Math.PI));
        }

        private Colour SpecularBrdf(float NdotL, float NdotV, float NdotH)
        {
            float V = CalculateV(NdotL, NdotV);
            float D = CalculateD_GGX(NdotH);

            Debug.Assert(!Single.IsNaN(V));
            Debug.Assert(!Single.IsNaN(D));

            float s = V*D;

            return new Colour(s, s, s);
        }

        //private Colour CalculateDiffuse(Colour colour)
        //{
        //    return DiffuseBrdf(colour);
        //}

        // https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#dielectrics
        private Colour DielectricBrdf(Colour baseColour, float NdotL, float NdotV, float NdotH, float VdotH)
        {
            Colour _base = DiffuseBrdf(baseColour);
            Colour layer = SpecularBrdf(NdotL, NdotV, NdotH);

            // Index of refraction hard coded to 1.5.
            return FresnelMix(1.5f, _base, layer, VdotH);
        }

        private Colour MetalBrdf(Colour baseColour, float NdotL, float NdotV, float NdotH)
        {
            Colour bsdf = SpecularBrdf(NdotL, NdotV, NdotH);

            return ConductorFresnel(baseColour, bsdf, Alpha);

            //return Palette.Black;
        }

        public GltfBRDF()
        {
            Name = "Gltf (BRDF)";
        }

        public GltfBRDF(XMLGltfBrdf xml)
        {
            Name = xml.Name;
            Ior = xml.Ior;
            Roughness = xml.Roughness;
            Metallic = xml.Metallic;
        }

        // Calculate Colour at point given in hr based on radiance (Li) coming from direction 'source'.
        // colour   Colour of surface at hit point.
        // source   A vector from hr towards the source of the incoming light.
        // Li       The colour of the incoming light.
        public override Colour GetColour(HitResult hr, Colour colour, SVector source, Colour Li)
        {
            Debug.Assert(!source.IsNull());
            Debug.Assert(!hr.Ray.IsNull());

            //SVector normal = hr.EntryNormal;
            //SVector viewDirection = hr.Ray.Normalise().Negate();
            //SVector halfVector = (viewDirection + source).Normalise();

    source = source.Normalise();

            SVector N = hr.EntryNormal;
            SVector V = hr.Ray.Normalise().Negate();
            SVector L = source;
            SVector H = (V + L).Normalise();


    N = N.Normalise();

            float NdotL = N.DotProductSaturated(L);
            float NdotV = N.DotProductSaturated(V);
            float NdotH = N.DotProductSaturated(H);
            float VdotH = V.DotProductSaturated(H);

     NdotV = Math.Clamp(Math.Abs(N.DotProduct(V)), 0.001f, 1.0f);

            float VdotN = V.DotProductSaturated(N);
            float LdotN = L.DotProductSaturated(N);

            Colour black = Palette.Black;
            Colour c_diff = Colour.LinearBlend(colour, black, _metallic);

            //Colour c = new Colour(0.04f, 0.04f, 0.04f);
            //Colour F0 = Colour.LinearBlend(c, colour, _metallic);

            Colour tint = new(1.0f - _f0.Red, 1.0f - _f0.Green, 1.0f - _f0.Blue);

            Colour diffuseColour = colour*tint;
            diffuseColour *= (1.0f - _metallic);
            Colour specularColour = Colour.LinearBlend(_f0, colour, _metallic);


            //Colour F = new(f, f, f);


            //Colour fDiffuse = tint*DiffuseBrdf(c_diff);

            float reflectance = Math.Max(Math.Max(specularColour.Red, specularColour.Green), specularColour.Blue);

            float reflectance90 = Math.Clamp(reflectance*25.0f, 0.0f, 1.0f);

            Colour specularEnvironmentR0 = specularColour;
            Colour specularEnvironmentR90 = new(reflectance90, reflectance90, reflectance90);

            Colour F = ConductorFresnel(specularEnvironmentR0, specularEnvironmentR90, VdotH);
//            float D = CalculateD_GGX(NdotH);
            float D = CalculateD_Ref(NdotH);
            float G = CalculateG_Ref(NdotL, NdotV);

            Colour tint2 = new(1.0f - F.Red, 1.0f - F.Green, 1.0f - F.Blue);
            Colour diffuseContrib = tint2*DiffuseBrdf(diffuseColour);

            Colour num = F*D*G;
//            float denom = 4.0f*VdotN*LdotN;
            float denom = 4.0f*NdotL*NdotV;
            Colour specularContrib = F*G*D/denom;

            //Debug.Assert(fDiffuse.IsNormal());
            //Debug.Assert(fSpecular.IsNormal());

            Colour material = (diffuseContrib + specularContrib)*Li;// *NdotL;

            //Debug.Assert(material.IsNormal());

            return material;

                /*
            // XXX Force dielectric for testing purposes.
            Colour dielectric = DielectricBrdf(colour, NdotL, NdotV, NdotH, VdotH);
            return dielectric;
                */

/*
            Colour metal = MetalBrdf(colour, NdotL, NdotV, NdotH);
            Colour dielectric = DielectricBrdf(colour, NdotL, NdotV, NdotH, VdotH);

            return dielectric*(1.0f - _metallic) + metal*_metallic;
*/
        }

        public override XMLBrdf ToXML()
        {
            XMLGltfBrdf xml = new();

            xml.Name = Name;
            xml.Ior = Ior;
            xml.Roughness = Roughness;
            xml.Metallic = Metallic;

            return xml;
        }

        //public override XMLBrdf ToXML()
        //{
        //    GltfBRDF xml = new();

        //    xml.Name = Name;
        //    xml.Ior = Ior;
        //    xml.Roughness = Roughness;
        //}
    }
}
