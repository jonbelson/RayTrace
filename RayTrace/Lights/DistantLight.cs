// Copyright (c) 2026 Jonathan Belson
// Licensed under the MIT License — use freely, keep this notice.
// SPDX-License-Identifier: MIT
// Full terms: see LICENSE in the project root.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class DistantLight : Light
    {
//        public Vector Direction { get; set; }
        public Vector Normal { get; set; }

        //
        // Constructor
        //
        public DistantLight(Scene scene, Vector direction, float intensity, Colour c) : base(scene, intensity, c)
        {
            Normal = direction.Normalise();
        }

        //
        // Constructor
        //
        public DistantLight(Scene scene, XMLDistantLight xmlDistantLight) : base(scene, xmlDistantLight)
        {
            Normal = new Vector(xmlDistantLight.Direction);
        }

        private SVector _direction = new SVector();

        // Allow Shape to pre-transform data using the Camera's WorldToView Matrix.
        public override void CreateCache(Camera camera)
        {
            _direction = Normal.ToSVector();
        }

        public override SVector GetDirection(Camera camera, SPoint p)
        {
            return _direction * 10000;
        }

        public override float GetAttenuation(Camera camera, SPoint p)
        {
            return 1.0f;
        }

        public override SColour GetIntensityAtPoint(Camera camera, SPoint p)
        {
            return (Output / MathF.PI).ToSColour();
        }

        protected override void AsXMLImpl(XMLEntity xml)
        {
            base.AsXMLImpl(xml);

            if (xml is XMLDistantLight xmlDistantLight)
            {
                xmlDistantLight.Direction = Normal.ToXML();
            }
        }

        public override XMLEntity ToXML()
        {
            XMLDistantLight xml = new XMLDistantLight();
            AsXMLImpl(xml);
            return xml;
        }

        public override HitResult HitTest(Camera camera, Ray ray)
        {
            return new HitResult(null);
        }
    }
}
