// Copyright (c) 2026 Jonathan Belson
// Licensed under the MIT License — use freely, keep this notice.
// SPDX-License-Identifier: MIT
// Full terms: see LICENSE in the project root.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace.Brdfs
{
    public class Lambertian : Brdf
    {
        public float Albedo { get; set; } = 1f;

        protected override float F(in SVector v)
        {
            return Albedo / MathF.PI;   
        }

        protected override float Pdf(in SVector v)
        {
            return v.K / MathF.PI;
        }

        //
        // Constructor.
        //
        public Lambertian()
        {
            Name = "Lambertian (BRDF)";
        }

        //
        // Constructor.
        //
        public Lambertian(XMLBrdf xml)
        {
            Name = xml.Name;
        }

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

            //pdf = y / MathF.PI;

            SVector sample_lcs = new SVector(x, y, z, 0f);

            // convert back to world space.
            SVector sample = lcs.Reproject(sample_lcs).Normalise();
            float NdotL = hr.EntryNormal.DotProductSaturated(sample);

            pdf = NdotL / MathF.PI;

            return sample;
        }

        public override SColour GetBrdfReflectance(HitResult hr, LocalCoordinateSystem lcs, Random rand, SColour colour, SVector source)
        {
            float diffuse = F(source);

            return colour * diffuse;
        }

        // Calculate Colour at point given in hr based on radiance (L) coming from direction source.
        // colour   Colour of surface at bit point.
        // source   Vector pointing outwards from hit point.
        // Li       Colour received from hit point.
        //public override Colour GetColour(HitResult hr, Colour colour, SVector source, Colour Li)
        public override SColour GetColour(HitResult hr, LocalCoordinateSystem lcs, Random rand, SColour colour, SVector source, SColour Li)
        {
            // Calculate wo (L towards viewer)
            /*            SVector up = new(0, 0, 1);
                        float coswo = up.DotProduct(hr.Ray.Normalise());
                        float wo = (float) Math.Acos(coswo);

                        // Calculate wi (L from source)
                        float coswi = up.DotProduct(source.Normalise().Negate());
                        float wi = (float)Math.Acos(coswi);
            */

            float dp = hr.EntryNormal.DotProduct(source);
            if (dp <= 0f)
            {
                return SPalette.Black;
            }

            dp = Math.Clamp(dp, 0.0001f, 1f);

            SColour Lo = GetBrdfReflectance(hr, lcs, rand, colour, source) * Li * dp; ;// colour * diffuse * Li * dp;

            return Lo;
        }

        public override XMLBrdf ToXML()
        {
            XMLLambertian xml = new();

            xml.Name = Name;

            return xml;
        }
    }
}
