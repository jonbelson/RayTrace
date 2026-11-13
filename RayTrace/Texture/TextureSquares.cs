// Copyright (c) 2026 Jonathan Belson
// Licensed under the MIT License — use freely, keep this notice.
// SPDX-License-Identifier: MIT
// Full terms: see LICENSE in the project root.

﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RayTrace
{
    public class TextureSquares : Texture, INotifyPropertyChanged
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

        public override TextureType Type { get; } = TextureType.Squares;

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
        } = 20;

        //
        // Constructor.
        //
        public TextureSquares() : this(Palette.Blue, Palette.White) { }

        //
        // Constructor.
        //
        public TextureSquares(Colour colour1, Colour colour2)
        {
            Name = "Squares";

            Colour1 = colour1;
            Colour2 = colour2;
        }

        //
        // Constructor.
        //
        public TextureSquares(XMLTextureSquares xml)
        {
            Colour1 = new Colour(xml.Colour1);
            Colour2 = new Colour(xml.Colour2);
            Scale = xml.Scale;
        }

        public override SColour GetSColour(HitResult hr)
        {
            int x = (int)(hr.u * Scale);
            int y = (int)(hr.v * Scale);

            SColour c1 = Colour1.ToSColour();
            SColour c2 = Colour2.ToSColour();

            if (x % 2 == 1)
            {
                return y % 2 == 1 ? c1 : c2;
            }
            else
            {
                return y % 2 == 0 ? c1 : c2;
            }
        }

        public override Colour GetBaseColour() { return Colour.LinearBlend(Colour1, Colour2, 0.5f); }

        public override XMLTexture ToXML()
        {
            XMLTextureSquares xml = new XMLTextureSquares();
            xml.Colour1 = Colour1.ToXML();
            xml.Colour2 = Colour2.ToXML();
            xml.Scale = Scale;
            return xml;
        }
    }
}
