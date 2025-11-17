using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace RayTrace.Brdfs
{
    // Reference BRDF in glTF specification.
    // V    Normalised vector towards eye.
    // L    Normalised vector to light.
    // N    Surface normal.
    // H    Normalised half vector |L + V|
    public class GltfBrdf : Brdf
    {
        // Multiplier for metal-ness.
        private float _metallicFactor = 1f;

        public float MetallicFactor
        {
            get => _metallicFactor;
            set { _metallicFactor = Math.Clamp(value, 0.0f, 1.0f); }
        }

        public float Ior { get; set; } = 1.5f;

        // Multiplier for Roughness.
        private float _roughnessFactor = 1f;
        public float RoughnessFactor
        {
            get => _roughnessFactor;
            set { _roughnessFactor = Math.Clamp(value, 0f, 1f); }
        }

        // Texture to read Roughness from.
        public TextureBitmap MetallicRoughnessTexture { get; set; }

        // Default Roughness if no texture is present.
        private static readonly Colour _defaultRoughness = new(0f, 1f, 1f);

        // Read Roughness from the texture, else return default.
        private Colour GetRoughness(in HitResult hr)
        {
            if (MetallicRoughnessTexture is not null && MetallicRoughnessTexture.IsValid())
            {
                Colour texColour = MetallicRoughnessTexture.GetColour(hr.u, hr.v);
                return texColour;
                //float roughness = texColour.Green*RoughnessFactor;
                //return new Colour(roughness, roughness, roughness);
            }
            return _defaultRoughness;
        }

        //private float Alpha { get => _roughness*_roughness; }

        private readonly Colour _f0 = new(0.04f, 0.04f, 0.04f);
        private readonly Colour _f90 = new(1.0f, 1.0f, 1.0f);

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

        private static Colour ConductorFresnel(Colour f0, float VdotH)
        {
            //float v = Math.Clamp(1.0f - Math.Abs(VdotH), 0.0f, 1.0f);
            float v = 1.0f - VdotH;
            float v5 = v*v*v*v*v;

            Debug.Assert(v >= 0.0f && v <= 1.0f);

            float r = f0.Red + (1f - f0.Red)*v5;
            float g = f0.Green + (1f - f0.Green)*v5;
            float b = f0.Blue + (1f - f0.Blue)*v5;

            return new Colour(r, g, b);
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

        private static Colour specularReflection(Colour f0, Colour f90, float VdotH)
        {
            float v = Math.Clamp(1.0f - Math.Abs(VdotH), 0.0f, 1.0f);
            //float v = 1.0f - VdotH;
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
        // https://cdn2.unrealengine.com/Resources/files/2013SiggraphPresentationsNotes-26915738.pdf
        // [Equation 3]
        private static float CalculateD_GGX(float NdotH, float roughness)
        {
            float alpha = roughness*roughness;
            float alphaSql = alpha*alpha + 0.000001f;

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

        private static float CalculateD_Ref(float NdotH, float roughness)
        {
            float alpha = roughness*roughness;
            float alphaSqr = alpha*alpha;

            float val = (NdotH*alphaSqr - NdotH)*NdotH + 1.0f;

            return alphaSqr/(MathF.PI*val*val);
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
        private static float CalculateG_Eq4(float NdotL, float NdotV, float roughness)
        {
            float val = roughness + 1;
            float k = val*val/8.0f;

            float Gl = NdotL/(NdotL*(1.0f - k) + k);
            float Gv = NdotV/(NdotV*(1.0f - k) + k);

            Debug.Assert(!float.IsNaN(Gl));
            Debug.Assert(!float.IsNaN(Gv));

            return Gl*Gv;
        }


        private static float CalculateG_Ref(float NdotL, float NdotV, float roughness)
        {
            //float val = _roughness + 1;
            //float k = val*val/8.0f;

            float alpha = roughness*roughness;
            float alphaSqr = alpha*alpha;

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

        private static float CalculateG_Ref2(float NdotL, float NdotV, float roughness)
        {
            //float val = _roughness + 1;
            //float k = val*val/8.0f;

            float alpha = roughness*roughness;
            float alphaSqr = alpha*alpha;

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

        private static float CalculateV(float NdotL, float NdotV, float roughness)
        {
            //            float denominator = 4.0f*Math.Abs(NdotL)*Math.Abs(NdotV);
            float denominator = 4.0f*NdotL*NdotV;

            Debug.Assert(!float.IsNaN(denominator));

            if (denominator != 0.0f)
            {
                //float G = CalculateG_Eq4(NdotL, NdotV);
                float G = CalculateG_Ref(NdotL, NdotV, roughness);

                return G/denominator;
            }

            return 0.0f;
        }

        // Equation 15
        private static float CalculateV_Equ15(float NdotL, float NdotV, float roughness)
        {
            float alpha = roughness*roughness;
            float alphaSqr = alpha*alpha;
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
        private static float CalculateV_Equ16(float NdotL, float NdotV, float roughness)
        {
            float alpha = roughness*roughness;
            double denom1 = NdotL*(NdotV*(1.0f - alpha) + alpha);
            double denom2 = NdotV*(NdotL*(1.0f - alpha) + alpha);

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

        private static Colour SpecularBrdf(float NdotL, float NdotV, float NdotH, float roughness)
        {
            float V = CalculateV(NdotL, NdotV, roughness);
            float D = CalculateD_GGX(NdotH, roughness);

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
        private static Colour DielectricBrdf(Colour baseColour, float NdotL, float NdotV, float NdotH, float VdotH, float roughness)
        {
            Colour _base = DiffuseBrdf(baseColour);
            Colour layer = SpecularBrdf(NdotL, NdotV, NdotH, roughness);

            // Index of refraction hard coded to 1.5.
            return FresnelMix(1.5f, _base, layer, VdotH);
        }

#if false
        private static Colour MetalBrdf(Colour baseColour, float NdotL, float NdotV, float NdotH, float VdotH, float roughness)
        {
            Colour bsdf = SpecularBrdf(NdotL, NdotV, NdotH, roughness);

            float alpha = roughness*roughness;
            return ConductorFresnel(baseColour, bsdf, VdotH /*alpha*/);

            //return Palette.Black;
        }
#endif

        protected override float F(in SVector v)
        {
            return 1f / MathF.PI;
        }

        protected override float Pdf(in SVector v)
        {
            return v.K / MathF.PI;
        }

        private float GetCosinePdf(float cosTheta)
        {
            return cosTheta/MathF.PI;
        }

        //public SVector ImportanceSampleCos()
        //{

        //}

        private float PdfGGX(in SVector N, in SVector H, in SVector V, float roughness)
        {
            float NdotH = MathF.Max(N.DotProduct(H), 0f);
            float VdotH = MathF.Max(V.DotProduct(H), 0f);

            float D = CalculateD_GGX(NdotH, roughness);

            return (D*NdotH)/(4f*VdotH);
        }

        private static float PowerHeuristic(float pdf1, float pdf2)
        {
            float pdf1Sqr = pdf1*pdf1;
            float pdf2Sqr = pdf2*pdf2;

            return pdf1Sqr/(pdf1Sqr + pdf2Sqr);
        }

        // viewDirection (from point to eye).
        // Returns half vector in Y-up space.
        public static SVector ImportanceSampleGGX(Random rand, in SVector viewDirection, float roughness, out float pdf)
        {
            float r1 = rand.NextSingle();
            float r2 = rand.NextSingle();

            float a = roughness*roughness;

            float phi = 2f*MathF.PI*r1;

            float cosTheta = MathF.Sqrt((1f - r2)/(1f + (a*a - 1f)*r2));
            float sinTheta = MathF.Sqrt(1f - cosTheta*cosTheta);

            //float val = (a*a)/(r1*(a*a - 1f) + 1);
            //float val2 = MathF.Sqrt(val);

            //float theta = MathF.Acos(val2);

            float x = sinTheta*MathF.Cos(phi);
            float z = sinTheta*MathF.Sin(phi);
            float y = cosTheta;

            float D = CalculateD_GGX(cosTheta, roughness);
            pdf = D*cosTheta;

            return new SVector(x, y, z);
        }

        // Returns a cosine-weighted vector in local space.
        private static SVector ImportanceSampleCosine(Random rand, out float pdf)
        {
            float r1 = rand.NextSingle();
            float r2 = rand.NextSingle();

            float phi = 2f * MathF.PI * r1;
            float sqrtR2 = MathF.Sqrt(r2);

            float x = MathF.Cos(phi)*sqrtR2;
            float z = MathF.Sin(phi)*sqrtR2;
            float y = MathF.Sqrt(1f - r2);

            pdf = y/MathF.PI;

            return new SVector(x, y, z);
        }

        // Return a sample direction in world space.
        public override SVector GetSample(HitResult hr, LocalCoordinateSystem lcs, Random rand, out float pdf)
        {
            // Decide which lobe to sample (diffuse or specular).
            Colour metallicRoughnessColour = GetRoughness(hr);
            float metalness = metallicRoughnessColour.Blue*MetallicFactor;
            float roughness = metallicRoughnessColour.Green*RoughnessFactor;

            float diffuseWeight = (1f - metalness)*0.5f;
            bool sampleDiffuse = rand.NextSingle() < diffuseWeight;

            SVector wi = hr.Ray.Normalise().Negate();

            // View direction in local coordinate system (towards viewpoint).
            SVector Vlcs = lcs.FromWorld(wi);

            SVector sample = SVector.Null();
            SVector sample_lcs = SVector.Null();

            if (sampleDiffuse)
            {
                /*SVector*/ sample_lcs = ImportanceSampleCosine(rand, out pdf);
            }
            else
            {
                // Sampled half vector.
                SVector HalfVeclcs = ImportanceSampleGGX(rand, Vlcs, roughness, out pdf);

                // Reflect vector-to-viewpoint by the halfVector.
                /*SVector*/ sample_lcs = (HalfVeclcs*(2f*Vlcs.DotProduct(HalfVeclcs)) - Vlcs).Normalise();
            }

            if (sample_lcs.J <= 0f) return SVector.Null();
            
            sample = lcs.Reproject(sample_lcs);  // convert back to world space.

            SVector Hlcs = (Vlcs + lcs.FromWorld(sample)).Normalise();

            float NdotL2 = hr.EntryNormal.DotProduct(sample);
            float pdfDiffuse = GetCosinePdf(NdotL2);
//            float pdfSpecular = PdfGGX(new SVector(0f, 1, 0f), Hlcs, Vlcs, roughness);
            float pdfSpecular = PdfGGX(hr.EntryNormal, Hlcs, Vlcs, roughness);

            float misWeight = PowerHeuristic(pdfDiffuse, pdfSpecular);

            pdf = misWeight/(pdf*(sampleDiffuse ? diffuseWeight : (1f - diffuseWeight)));

            return sample;
        }

        //
        // Constructor
        //
        public GltfBrdf()
        {
            Name = "Gltf (BRDF)";
        }

        //
        // Constructor
        //
        public GltfBrdf(XMLGltfBrdf xml)
        {
            Name = xml.Name;
            Ior = xml.Ior;
            RoughnessFactor = xml.Roughness;
            MetallicFactor = xml.Metallic;
        }

        // Calculate Colour at point given in hr based on radiance (Li) coming from direction 'source'.
        // Based on reference implemenation at:
        //   https://github.com/KhronosGroup/glTF-Sample-Viewer/blob/glTF-WebGL-PBR/shaders/pbr-frag.glsl
        // colour   Colour of surface at hit point.
        // source   A vector from hr towards the source of the incoming light.
        // Li       The colour of the incoming light.
        public override Colour GetColour(HitResult hr, LocalCoordinateSystem lcs, Random rand, Colour colour, SVector source, Colour Li)
        {
            Debug.Assert(!source.IsNull());
            Debug.Assert(!hr.Ray.IsNull());

            //source = source.Normalise();

            SVector N = hr.EntryNormal.Normalise();
            SVector V = hr.Ray.Normalise().Negate();

            // Convert view direction to lcs.
            SVector Vlcs = lcs.FromWorld(V);   

            // Decide which lobe to sample (diffuse or specular).
            Colour metallicRoughnessColour = GetRoughness(hr);
            float metalness = metallicRoughnessColour.Blue*MetallicFactor;
            float roughness = metallicRoughnessColour.Green*RoughnessFactor;

            float diffuseWeight = (1f - metalness)*0.5f;
            bool sampleDiffuse = rand.NextSingle() < diffuseWeight;


            //float NdotL2 = Llcs.J;
            //Debug.Assert(NdotL2 > 0f);
            //if (NdotL2 <= 0f) return Palette.Black;

            SVector L = source;
            //SVector L = lcs.Reproject(Llcs);  // convert back to world space.
            SVector H = (V + L).Normalise();

            //SVector Hlcs = (Vlcs + Llcs).Normalise();

            float NdotL = N.DotProduct(L);

            float pdfDiffuse = GetCosinePdf(NdotL);
            float pdfSpecular = PdfGGX(N, H, V, roughness);

            float pdf = (sampleDiffuse) ? pdfDiffuse : pdfSpecular;
            float misWeight = PowerHeuristic(pdf, sampleDiffuse ? pdfSpecular : pdfDiffuse);

            //float NdotL = N.DotProduct(L);
            //float NdotV = Math.Clamp(MathF.Abs(N.DotProductSaturated(V)), 0.00f, 1f);
            float NdotV = Math.Abs(N.DotProduct(V));
            float NdotH = N.DotProductSaturated(H);
            float VdotH = V.DotProductSaturated(H);

            float VdotN = V.DotProductSaturated(N);
            float LdotN = L.DotProductSaturated(N);

            NdotL = Math.Clamp(NdotL, 0.0001f, 1f);
            NdotV = Math.Clamp(NdotV, 0.0001f, 1f);

            roughness = MathF.Max(roughness, 0.0001f);

            //Colour black = Palette.Black;
            //Colour c_diff = Colour.LinearBlend(colour, black, metalness);

            //Colour f0 = Colour.LinearBlend(_f0, colour, metalness);

            Colour specularColour = Colour.LinearBlend(_f0, colour, metalness);
            Colour diffuseColour = colour*(Palette.White - specularColour /*_f0*/)*(1f - metalness);
 
            float reflectance = Math.Max(Math.Max(specularColour.Red, specularColour.Green), specularColour.Blue);

            float reflectance90 = Math.Clamp(reflectance*25.0f, 0.0f, 1.0f);

            // Reflectance0 (normal incidence angle).
            Colour specularEnvironmentR0 = specularColour;

            // Reflectance at grazing angle.
            Colour specularEnvironmentR90 = new(reflectance90, reflectance90, reflectance90);

            Colour F = specularReflection(specularEnvironmentR0, specularEnvironmentR90, VdotH);
            //float D = CalculateD_GGX(NdotH);
//            float D = CalculateD_Ref(NdotH, roughness);
            float D = CalculateD_GGX(NdotH, roughness);
            float G = CalculateG_Ref2(NdotL, NdotV, roughness);

            Colour diffuseContrib = (Palette.White - F)*diffuseColour/MathF.PI;

            Colour specularContrib = F*G*D/(4.0f*NdotL*NdotV);

            float dp = hr.EntryNormal.DotProduct(source);

            Debug.Assert(dp >= 0f /*&& dp <= 1f*/);

            Colour Lo = (diffuseContrib + specularContrib)*Li*dp;

            //float pdf = dp/MathF.PI;
            //Lo /= pdf;

            Lo *= misWeight/(pdf*(sampleDiffuse ? diffuseWeight : (1f - diffuseWeight)));

            //Debug.Assert(material.IsNormal());

            return Lo;
        }

        public override XMLBrdf ToXML()
        {
            XMLGltfBrdf xml = new();

            xml.Name = Name;
            xml.Ior = Ior;
            xml.Roughness = RoughnessFactor;
            xml.Metallic = MetallicFactor;

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
