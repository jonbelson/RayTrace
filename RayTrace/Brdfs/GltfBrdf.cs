using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace.Brdfs
{
    // Reference BRDF in glTF specification.
    // V    Normalised vector towards eye.
    // L    Normalised vector to light.
    // N    Surface normal.
    // H    Normalised half vector |L + V|
    public class GltfBrdf : Brdf
    {
        public float _metallic { get; set; }

        public float Metallic
        {
            get => _metallic;
            set { Math.Clamp(_metallic, 0.0f, 1.0f); _metallic = value; }
        }

        public float Ior { get; set; } = 1.5f;

        private float _roughness = 0.5f;
        public float Roughness
        {
            get => _roughness;
            set { _roughness = Math.Clamp(value, 0f, 1f); }
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
            return x > 0.0f ? 1.0f : 0.0f;
        }

        // Calculate GGX microfactet distribution, or 'D'.
        // https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#specular-brdf
        private float CalculateD_GGX(float NdotH)
        {
            float alpha = Alpha;
            float alphaSql = alpha*alpha;

            float val = NdotH*NdotH*(alphaSql - 1.0f) + 1.0f;
            float denominator = MathF.PI*val*val;

            //float NdotH = N.DotProductSaturated(H);

            if (denominator != 0f)
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

            return alphaSqr*(MathF.PI*val*val);
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

            Debug.Assert(!float.IsNaN(Gl));
            Debug.Assert(!float.IsNaN(Gv));

            return Gl*Gv;
        }


        private float CalculateG_Ref(float NdotL, float NdotV)
        {
            //float val = _roughness + 1;
            //float k = val*val/8.0f;

            float alphaSqr = Alpha*Alpha;

            float denom1 = NdotL*MathF.Sqrt(NdotV*NdotV*(1.0f - alphaSqr) + alphaSqr);
            float denom2 = NdotV*MathF.Sqrt(NdotL*NdotL*(1.0f - alphaSqr) + alphaSqr);

            Debug.Assert(!float.IsNaN(denom1));
            Debug.Assert(!float.IsNaN(denom2));

            float denominator = denom1 + denom2;
            if (denominator > 0f)
            {
                return 0.5f/denominator;
            }
            return 0f;
        }

        private float CalculateG_Ref2(float NdotL, float NdotV)
        {
            //float val = _roughness + 1;
            //float k = val*val/8.0f;

            float alphaSqr = Alpha*Alpha;

            float attenuationL = 2.0f*NdotL/(NdotL + MathF.Sqrt(alphaSqr + (1.0f - alphaSqr)*NdotL*NdotL));
            float attenuationV = 2.0f*NdotV/(NdotV + MathF.Sqrt(alphaSqr + (1.0f - alphaSqr)*NdotV*NdotV));


            Debug.Assert(!float.IsNaN(attenuationL));
            Debug.Assert(!float.IsNaN(attenuationV));

            return attenuationL*attenuationV;
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

            Debug.Assert(!float.IsNaN(denominator));

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
            float denom1 = NdotL*MathF.Sqrt(NdotV*NdotV*(1.0f - alphaSqr) + alphaSqr);
            float denom2 = NdotV*MathF.Sqrt(NdotL*NdotL*(1.0f - alphaSqr) + alphaSqr);

            Debug.Assert(!float.IsNaN(denom1));
            Debug.Assert(!float.IsNaN(denom2));

            float denominator = denom1 + denom2;
            if (denominator > 0f)
            {
                return 0.5f/denominator;
            }
            return 0f;
        }

        // Equation 16
        // Faster but less accurate
        private float CalculateV_Equ16(float NdotL, float NdotV)
        {
            double denom1 = NdotL*(NdotV*(1.0f - Alpha) + Alpha);
            double denom2 = NdotV*(NdotL*(1.0f - Alpha) + Alpha);

            Debug.Assert(!double.IsNaN(denom1));
            Debug.Assert(!double.IsNaN(denom2));

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
            return c*(1.0f/MathF.PI);
        }

        private Colour SpecularBrdf(float NdotL, float NdotV, float NdotH)
        {
            float V = CalculateV(NdotL, NdotV);
            float D = CalculateD_GGX(NdotH);

            Debug.Assert(!float.IsNaN(V));
            Debug.Assert(!float.IsNaN(D));

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

        public GltfBrdf()
        {
            Name = "Gltf (BRDF)";
        }

        public GltfBrdf(XMLGltfBrdf xml)
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
            diffuseColour *= 1.0f - _metallic;
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
