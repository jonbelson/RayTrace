// Copyright (c) 2026 Jonathan Belson
// Licensed under the MIT License — use freely, keep this notice.
// SPDX-License-Identifier: MIT
// Full terms: see LICENSE in the project root.

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class TextureNoise : Texture, INotifyPropertyChanged
    {
        private void Notify([CallerMemberName] String propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        private void Colour_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Notify(e.PropertyName);
        }


        private double noiseRange = Perlin.GetRange();

        public override TextureType Type { get; } = TextureType.Perlin;

        public event PropertyChangedEventHandler PropertyChanged;

        public enum ColourFunction
        {
            EitherOr, Blend
        };

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

        public int Scale
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    Notify();
                }
            }
        } = 10;

        public ColourFunction Function { get; set; } = ColourFunction.EitherOr;

        public TextureNoise() : this(Palette.Green, Palette.Red) { }

        public TextureNoise(Colour col1, Colour col2)
        {
            Name = "Perlin Noise";

            Colour1 = col1;
            Colour2 = col2;
        }

        public override SColour GetSColour(HitResult hr)
        {
            double x = hr.u * Scale;
            double y = hr.v * Scale;

            double val = Perlin.Noise(x, y, 0.0);

            Debug.Assert(val >= - noiseRange);
            Debug.Assert(val <= noiseRange);

            //Console.WriteLine(val);

            SColour result = SPalette.Grey;

            SColour c1 = Colour1.ToSColour();
            SColour c2 = Colour2.ToSColour();

            switch (Function)
            {
                case ColourFunction.EitherOr:
                    result = (val < 0.0) ? c1 : c2;
                    break;
                case ColourFunction.Blend:
                    {
                        float ratio = (float) ((val + noiseRange) / (2.0 * noiseRange));

                        //Debug.Assert(ratio >= 0.0);
                        //Debug.Assert(ratio <= 1.0);

                        result = SColour.SmoothBlend(c1, c2, ratio);

                        Debug.Assert(result.Red >= 0.0 && result.Red <= 1.0);
                        Debug.Assert(result.Green >= 0.0 && result.Green <= 1.0);
                        Debug.Assert(result.Blue >= 0.0 && result.Blue <= 1.0);
                    }
                    break;
            }

            return result;
        }

    }
}
