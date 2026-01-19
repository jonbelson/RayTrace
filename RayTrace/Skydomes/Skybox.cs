using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace.Skydomes
{
    // Represent the environment via a cube map.
    // Sky bitmap can either be a single texture or six separate textures.
    // http://www.humus.name/index.php?page=Textures
    public class Skybox : Skydome
    {
        private string _directory;

        public string Directory
        {
            get
            {
                return new DirectoryInfo(_directory).Name;
            }
        }

        private string _displayName;

        public string DisplayName { get => _displayName; }

        // private Colour[] faces = new Colour[6];
        private BitmapUtils.RgbTexture[] faces = new BitmapUtils.RgbTexture[6];

        private const int Right = 0;
        private const int Left = 1;
        private const int Top = 2;
        private const int Bottom = 3;
        private const int Front = 4;
        private const int Back = 5;

        private class /*struct*/ Fileset
        {
            public List<string> faces = new List<string>();
        };

        private readonly Fileset fileset1 = new Fileset() { faces = { "posx.jpg", "negx.jpg", "posy.jpg", "negy.jpg", "negz.jpg", "posz.jpg" } };

        // Attempt to load skybox textures by trying a few common formats.
        private bool LoadTextures(string directory)
        {
            bool ok = true;

            // Figure out which filenames are being used.
            for (int i = 0; i < 6; i++)
            {
                string fileName = directory + "\\" + fileset1.faces[i];

                if (!File.Exists(fileName))
                {
                    ok = false;
                    break;
                }
            }

            if (ok)
            {
                for (int i = 0; i < 6; i++)
                {
                    string fileName = directory + "\\" + fileset1.faces[i];

                    if (BitmapUtils.LoadTexture(fileName, out BitmapUtils.RgbTexture colourTexture))
                    {
                        faces[i] = colourTexture;
                    }
                }
            }

            return ok;
        }

        //
        // Extract the specified zip file to a new directory.
        //
        private static string ExtractZip(string zipFile)
        {
            try
            {
                using ZipArchive archive = ZipFile.OpenRead(zipFile);

                //string tempDir = System.IO.Path.GetTempPath();
                string appDataDir = Storage.DataDir;
                string dirName = Guid.NewGuid().ToString();
                string dir = Path.Combine(appDataDir, dirName);

                archive.ExtractToDirectory(dir, true);

                return dir;
            }
            catch
            {
            }
            return string.Empty;
        }

        //
        // Constructor
        // name     Name of skybox directory. If not found, look for a matching ZIP file containing the skybox files.
        //
        public Skybox(string name)
        {
            if (!System.IO.Directory.Exists(name))
            {
                // Not matching directory, so look for a ZIP.
                string zipFile = name + ".zip";
                if (File.Exists(zipFile))
                {
                    string extractionDir = ExtractZip(zipFile);

                    if (string.IsNullOrEmpty(extractionDir))
                    {
                        throw new ArgumentException($"Failed to extract zipfile '{zipFile}'");
                    }

                    _directory = extractionDir;
                    _displayName = Path.GetFileNameWithoutExtension(name);
                }
                else
                {
                    throw new ArgumentException($"Invalid skybox '{name}'");
                }
            }
            else
            {
                _directory = name;

                _displayName = Path.GetFileNameWithoutExtension(name);
            }

            _ = LoadTextures(_directory);
        }

        //
        // Constructor.
        //
        public Skybox(XMLSkybox xmlSkybox)
        {
            this._directory = xmlSkybox.Directory;

            _ = LoadTextures(_directory);
        }

        // Return colour in linear RGB format.
        private static SColour GetColour(BitmapUtils.RgbTexture texture, float u, float v)
        {
            if (texture != null)
            {
                u = Math.Clamp(u, 0, 1);
                v = Math.Clamp(v, 0, 1);

                int x = (int)(u * (texture.Width - 1));
                int y = (int)(v * (texture.Height - 1));

                Debug.Assert(y * texture.Width + x < texture.Texture.Length);

                try
                {
                    uint rgb = (uint)texture.Texture[y * texture.Width + x];
                    return SColour.FromRGB(rgb).ToLinearRGB();
                    //Colour c = texture.Texture[y * texture.Width + x];
                    //return c;
                }
                catch (Exception e)
                {
                    Logger.Instance.Warn($"{e.Message}");

                    //MessageBox.Show(e.Message);
                }
            }

            return SPalette.Grey;
        }

        public override SColour GetColour(Camera camera, SVector ray)
        {
            ray = ray.Normalise();

            Matrix m = camera.NormalMatrixT;

            Matrix rot = Matrix.GetRotationY(MathF.PI);

            m = rot*m;

            SVector v = m * ray;

            float iAbs = Math.Abs(v.I);
            float jAbs = Math.Abs(v.J);
            float kAbs = Math.Abs(v.K);

            bool iMajor = false;
            bool jMajor = false;
            bool kMajor = false;

            // Find major axis.
            if (iAbs > jAbs)
            {
                if (iAbs > kAbs)
                {
                    // I is major axis.
                    iMajor = true;
                }
                else
                {
                    // K is major axis.
                    kMajor = true;
                }
            }
            else
            {
                if (jAbs > kAbs)
                {
                    // J is major axis.
                    jMajor = true;
                }
                else
                {
                    // K is major axis.
                    kMajor = true;
                }
            }

            if (iMajor)
            {
                if (v.I < 0)
                {
                    // Left face.
                    float U = ((v.K / iAbs) + 1) / 2;
                    float V = ((v.J / iAbs) + 1) / 2;
                    return GetColour(faces[Left], U, V);
                }
                else
                {
                    // Right face.
                    float U = ((-v.K / iAbs) + 1) / 2;
                    float V = ((v.J / iAbs) + 1) / 2;
                    return GetColour(faces[Right], U, V);
                }
            }

            if (jMajor)
            {
                if (v.J < 0)
                {
                    // Bottom face.
                    float U = ((v.I / jAbs) + 1) / 2;
                    float V = ((-v.K / jAbs) + 1) / 2;
                    return GetColour(faces[Bottom], U, 1.0f - V);
                }
                else
                {
                    // Top face.
                    float U = ((v.I / jAbs) + 1) / 2;
                    float V = ((v.K / jAbs) + 1) / 2;
                    return GetColour(faces[Top], U, 1.0f - V);
                }
            }

            if (kMajor)
            {
                if (v.K < 0)
                {
                    // Back face.
                    float U = ((v.I / kAbs) + 1) / 2;
                    float V = ((v.J / kAbs) + 1) / 2;
                    return GetColour(faces[Front], 1.0f - U, V);
                }
                else
                {
                    // Front face.
                    float U = ((v.I / kAbs) + 1) / 2;
                    float V = ((v.J / kAbs) + 1) / 2;
                    return GetColour(faces[Back], U, V);
                }
            }

            return SPalette.Grey;
        }

        public override XMLSkydome ToXML()
        {
            XMLSkybox xml = new();

            xml.Directory = _directory;

            return xml;
        }

        public override HitResult HitTest(Camera camera, Ray ray)
        {
            throw new NotImplementedException();
        }
    }
}
