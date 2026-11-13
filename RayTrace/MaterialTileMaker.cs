// Copyright (c) 2026 Jonathan Belson
// Licensed under the MIT License — use freely, keep this notice.
// SPDX-License-Identifier: MIT
// Full terms: see LICENSE in the project root.

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

using RayTrace.Skydomes;

namespace RayTrace
{
    public class MaterialTile
    {
        [NotifyParentProperty(true)]
        public string Name { get; set; }
        public Guid Guid { get; set; }
        public Material Material { get; set; }
        public Bitmap Bitmap { get; set; }

        public WriteableBitmap WriteableBitmap { get; set; }

        public MaterialTile()
        {

        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class MaterialTileMaker
    {
        private Scene scene;
        private Sphere sphere;

        public int Size { get; set; } = 200;

        private async Task<bool> Render(Renderer renderer, Canvas canvas)
        {
            await DoRender(renderer, canvas);

            return true;
        }

        private struct Tile
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;
        }

        private Task<bool> DoRender(Renderer renderer, Canvas canvas)
        {
            CancellationTokenSource cancellation = new();
            CancellationToken token = cancellation.Token;

            List<Tile> tiles = [];

            // Define chunk sizes for rendering, expressed as 1/n.
            int[] steps = [4, 8, 8, 8, 8, 4];

            int yOffset = 0;
            for (int y = 0; y<steps.Length; y++)
            {
                int xOffset = 0;
                for (int x = 0; x<steps.Length; x++)
                {
                    tiles.Add(new Tile { X = xOffset, Y = yOffset, Width = Size/steps[x], Height = Size/steps[y] });

                    xOffset += Size/steps[x];
                }
                yOffset += Size/steps[y];
            }

            List<Task> tasks = [];
            foreach (Tile tile in tiles)
            {
                Task task = Task.Factory.StartNew(() => { renderer.Render(scene, canvas, tile.X, tile.Y, tile.Width, tile.Height, token); });
                tasks.Add(task);
            }

            Task.WaitAll(tasks);

            return Task.FromResult(true);
        }

        //
        // Constructor
        //
        public MaterialTileMaker()
        {
            scene = new Scene();
            scene.Ambient = Palette.Blue;
            //_scene.Background = Palette.Red;
            scene.Camera.Pos = new Point(0, 0, 0);
            scene.Camera.Target = new Point(0, 0, -1);

            sphere = new Sphere(scene, new Point(0, 0, -5f), 2, Palette.White);
            //scene.AddShape(sphere);

            PointLight pl = new PointLight(scene, new Point(-6, 4, 0), 500, Palette.White);
            //scene.AddLight(scene, new PointLight(new Point(-6, 4, 0), 500, Palette.White));

            //Plane plane = new Plane(new Vector(0, 0, -1), new Point(0, 0, 20));
            //plane.Material.Texture = new TextureSquares(new Colour(System.Windows.Media.Colors.DarkGray), new Colour(System.Windows.Media.Colors.Gray)) { Scale = 50 };

            Material matSquares = new(MaterialType.BRDF_Lambertian) { Name="Squares" };
            //matSquares.SetType(MaterialType.BRDF_Lambertian);
            matSquares.Texture = new TextureSquares(Palette.DarkGrey, Palette.White /*Palette.LightGrey*/)
            {
                Scale = 50
            };

            //Disc disc = new Disc(scene, new Point(0, 0, -20), new Vector(0, 0, 1), 18)
            //{
            //    Material = matSquares
            //};

            scene.AddMaterial(matSquares);

            scene.Skydome = new SkydomeBitmap("Data\\skydomes\\immenstadter_horn_1024.jpg");

            scene.Camera.UpdateMatrix();    // XXX Needed?
        }

//        public async Task<MaterialTile> MakeTile(Material material)
        public MaterialTile MakeTile(Material material)
        {
            Logger.Instance.Debug($"Rendering Tile for Material {material.Name}");

            sphere.Material = material;
            scene.AddMaterial(sphere.Material);
            scene.CreateCaches();

            Renderer renderer = new();
            renderer.AntiAlias = Renderer.AntiAliasType.NoiseGrid;
            //renderer.AntiAlias = Renderer.AntiAliasType.None;
            renderer.SampleDepth = 10;
            ////renderer.UseAmbient = true;
            renderer.UseShadows = false;
            //scene.Ambient = new Colour(0.5f, 0.5f, 0.5f);
            renderer.NumBRDFSamples = 100;
            renderer.RecursionDepth = 2;
            Canvas canvas = new(Size, Size, SPalette.Red);
            //renderer.Render(scene, canvas);

            //await DoRender(renderer, canvas);

            bool b = DoRender(renderer, canvas).Result;

            MaterialTile materialTile = new()
            {
                Name = material.ToString(),// material.Name;
                Guid = material.Guid,
                Bitmap = canvas.GetAsBitmap(),
                Material = material
            };

            //string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            //canvas.Save(documents + "/" + material.Guid.ToString());

            return materialTile;
        }
    }
}
