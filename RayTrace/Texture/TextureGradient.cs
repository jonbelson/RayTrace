// Copyright (c) 2026 Jonathan Belson
// Licensed under the MIT License — use freely, keep this notice.
// SPDX-License-Identifier: MIT
// Full terms: see LICENSE in the project root.

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    class TextureGradient : Texture, INotifyPropertyChanged
    {
        private void Notify([CallerMemberName] String propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        private void Colour_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Notify(e.PropertyName);
        }

        public event PropertyChangedEventHandler PropertyChanged;


        public override TextureType Type { get; } = TextureType.Gradient;

        public Colour Colour1
        {
            get;
            set
            {
                if (field != value)
                {
                    field?.PropertyChanged -= Colour_PropertyChanged;

                    field = value;

                    field?.PropertyChanged += Colour_PropertyChanged;

                    Notify();
                }
            }
        }
        public Colour Colour2
        {
            get;
            set
            {
                if (field != value)
                {
                    field?.PropertyChanged -= Colour_PropertyChanged;

                    field = value;

                    field?.PropertyChanged += Colour_PropertyChanged;

                    Notify();
                }
            }
        }
        public enum GradientType
        {
            Vertical, Horizontal, Diagonal, Radial
        }

        public enum BlendType
        {
            Linear, Smooth, Step
        }

        public GradientType Gradient { get; set; } = GradientType.Vertical;

        public BlendType Blend { get; set; } = TextureGradient.BlendType.Smooth;

        //
        // Constructor.
        //
        public TextureGradient() : this(Palette.Blue, Palette.Green) { }

        //
        // Constructor.
        //
        public TextureGradient(Colour colour1, Colour colour2)
        {
            Name = "Gradient";

            Colour1 = colour1;
            Colour2 = colour2;
        }

        //public TextureGradient(XMLTextureGradient xml)
        //{
        //    Colour1 = new Colour(xml.Colour1);
        //    Colour2 = new Colour(xml.Colour2);
        //      Type = xml.Type;
        //}

        public delegate SColour Blender(SColour c1, SColour c2, float ratio);

        public override SColour GetSColour(HitResult hr)
        {
            Blender handler = null;

            switch (Blend)
            {
                case BlendType.Linear: handler = SColour.LinearBlend;    break;
                case BlendType.Smooth: handler = SColour.SmoothBlend; break;
                case BlendType.Step: handler = SColour.StepBlend; break;
            }

            SColour result = SPalette.Grey;

            if (handler is not null)
            {
                SColour c1 = Colour1.ToSColour();
                SColour c2 = Colour2.ToSColour();

                switch (Gradient)
                {
                    case GradientType.Vertical:
                        result = handler.Invoke(c1, c2, hr.v);
                        break;

                    case GradientType.Horizontal:
                        result = handler.Invoke(c1, c2, hr.u);
                        break;

                    case GradientType.Diagonal:
                        // Find shortest distance from 0,0 to 1,1 and use 'm' as blend.
                        throw new NotImplementedException();

                    case GradientType.Radial:
                        Vector v = new Point(0.5f, 0.5f, 0) - new Point(hr.u, hr.v, 0);
                        double distance = v.Magnitude();
                        return handler.Invoke(c1, c2, (float)distance);

                }
            }

            return result;
        }

        public override Colour GetBaseColour()
        {
            return Colour.SmoothBlend(Colour1, Colour2, 0.5f);

        }

        //public override XMLTexture ToXML()
        //{
        //    XMLTextureGradient xml = new();
        //    xml.Colour1 = Colour1.ToXML();
        //    xml.Colour2 = Colour2.ToXML();
        //    xml.Type = Type;
        //    return xml;
        //}
    }
}
