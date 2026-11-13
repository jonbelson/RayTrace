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
    class TexturePerTriangle : Texture
    {
        public override TextureType Type { get; } = TextureType.PerTriangle;

        private Dictionary<int, Colour> _colourLookup = new Dictionary<int, Colour>();

        public Dictionary<int, Colour> CloneDictionary() => new Dictionary<int, Colour>(_colourLookup);

        public TexturePerTriangle() { }

        public TexturePerTriangle(XMLTexturePerTriangle xmlTexturePerTriangle)
        {
            foreach (KeyPairIntXMLColour item in xmlTexturePerTriangle.ColourList)
            {
                _colourLookup.Add(item.Key, new Colour(item.Value));
            }
        }

        public void AddEntry(int index, Colour colour)
        {
            if (!_colourLookup.ContainsKey(index))
            {
                _colourLookup.Add(index, colour);
            }
            else
            {
                _colourLookup[index] = colour;
            }
        }

        public override SColour GetSColour(HitResult hr)
        {
            Colour colour;
            if (_colourLookup.TryGetValue(hr.Index, out colour))
            {
                return colour.ToSColour();
            }
            return SPalette.Grey;
        }

        public override XMLTexture ToXML()
        {
            XMLTexturePerTriangle xml = new XMLTexturePerTriangle();

            // Nede to convert Dictionary<> to List<> for serialisation.
            List<KeyValuePair<int, Colour>> colourList = _colourLookup.ToList();
            xml.ColourList = new List<KeyPairIntXMLColour>();
            foreach (var item in colourList)
            {
                xml.ColourList.Add(new KeyPairIntXMLColour() { Key = item.Key, Value = item.Value.ToXML() });
            }

            return xml;
        }
    }
}
