using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RayTrace
{
    /// <summary>
    /// Interaction logic for MaterialEditorDialog.xaml
    /// </summary>
    public partial class MaterialEditorDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly List<MaterialType> materialTypes = new() { MaterialType.Diffuse, MaterialType.Reflective, MaterialType.RefractiveAndReflective, MaterialType.Phong, MaterialType.BRDF_Lambertian, MaterialType.BRDF_CookTorrence, MaterialType.BRDF_Gltf };

        private readonly MaterialTileMaker materialTileMaker = new();

        public MaterialTile MaterialTile { get; set; } = null;

        //private readonly Material material;
        //[NotifyParentProperty(true)]
        public Material material { get; set; } = null;
        public Texture texture { get; set; } = null;

        private TextureType tt;
        public TextureType textureType { get => tt; set { tt = value; Notify(); } }

        public ObservableCollection<Material> Materials { get; set; } = new();

        //public enum TextureType { Solid, Perlin, Squares, Gradient };
        public ObservableCollection<TextureType> Textures { get; set; } = new() { TextureType.Solid, TextureType.Perlin, TextureType.Squares, TextureType.Gradient, TextureType.Bitmap };

        // Orignal Material passed in for Cancelling purposes.
        private XMLMaterial _xmlMaterialOrig;
        private Texture _textureOrig;

        private void Notify([CallerMemberName] String propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        private void UpdatePreview()
        {
            MaterialTile = materialTileMaker.MakeTile(material);
            //WriteableBitmap wb = BitmapToWriteableBitmap(materialTile.Bitmap);

            if (MaterialTile.Bitmap != null)
            {
                WriteableBitmap wb = MaterialTile.Bitmap.ToWriteableBitmap();

                preview.Source = wb;
            }
        }

        //
        // Constructor
        //
        public MaterialEditorDialog(Material mat)
        {
            material = mat;
            texture = material.Texture;

            if (texture != null)
            {
                textureType = material.Texture.Type;

                // Serialse the Material to XML in case the user cancels.
                _textureOrig = mat.Texture;
                //mat.Texture = null;
                _xmlMaterialOrig = mat.ToXML();
                //mat.Texture = _textureOrig;

                InitializeComponent();

                DataContext = this;

                Materials.Add(material);

                comboBoxMaterialType.ItemsSource = materialTypes;
                comboBoxMaterialType.SelectedItem = mat.Type;

                //comboBoxTexture.ItemSource = Textures;

                UpdatePreview();
            }

        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            material.Restore(_xmlMaterialOrig);
            material.Texture = _textureOrig;

            DialogResult = false;
        }

        private void comboBoxMaterialType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MaterialType type = (MaterialType)comboBoxMaterialType.SelectedItem;

            if (material.Type != type)
            {
                material.SetType(type);

                // XXX
                //if (type == MaterialType.BRDF_CookTorrence)
                //{
                //    material.Brdf = new CookTorrence();
                //}
                //else if (type == MaterialType.BRDF_Lambertian)
                //{
                //    material.Brdf = new Lambertian();
                //}

                Materials.Clear();
                Materials.Add(material);
            }

            UpdatePreview();

            //var currentSelector = contentControl.ContentTemplateSelector;
            //contentControl.ContentTemplateSelector = null;
            //contentControl.ContentTemplateSelector = currentSelector;
        }

        private void Control_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            UpdatePreview();
        }

        private void comboBoxTexture_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //TextureType textureType = (TextureType)comboBoxTexture.SelectedItem;

            ComboBox comboBox = (ComboBox)sender;
            if (comboBox.SelectedItem != null)
            {
                TextureType textureType = (TextureType)comboBox.SelectedItem;

                switch (textureType)
                {
                    case TextureType.Solid: material.Texture = new TextureSolid(); break;
                    case TextureType.Squares: material.Texture = new TextureSquares(); break;
                    case TextureType.Perlin: material.Texture = new TextureNoise(); break;
                    case TextureType.Gradient: material.Texture = new TextureGradient(); break;
                    case TextureType.Bitmap: material.Texture = new TextureBitmap(); break;
                }

                texture = material.Texture;

                textureType = texture.Type;

                UpdatePreview();

                //var currentSelector = contentControl.ContentTemplateSelector;
                //contentControl.ContentTemplateSelector = null;
                //contentControl.ContentTemplateSelector = currentSelector;
            }
            else
            {
                comboBox.SelectedItem = material.Texture;
            }
        }

        private void comboBoxGradientType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            if (comboBox.SelectedItem != null)
            {
                TextureGradient.GradientType function = (TextureGradient.GradientType)comboBox.SelectedItem;

                TextureGradient texture = (TextureGradient)material.Texture;

                texture.Gradient = function;

                UpdatePreview();

                //var currentSelector = contentControl.ContentTemplateSelector;
                //contentControl.ContentTemplateSelector = null;
                //contentControl.ContentTemplateSelector = currentSelector;
            }
        }

        private void comboBoxGradientBlendType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            if (comboBox.SelectedItem != null)
            {
                TextureGradient.BlendType function = (TextureGradient.BlendType)comboBox.SelectedItem;

                TextureGradient texture = (TextureGradient)material.Texture;

                texture.Blend = function;

                UpdatePreview();

                //var currentSelector = contentControl.ContentTemplateSelector;
                //contentControl.ContentTemplateSelector = null;
                //contentControl.ContentTemplateSelector = currentSelector;
            }
        }

        private void comboBoxBlendType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            if (comboBox.SelectedItem != null)
            {
                TextureNoise.ColourFunction function = (TextureNoise.ColourFunction)comboBox.SelectedItem;

                TextureNoise texture = (TextureNoise)material.Texture;

                switch (function)
                {
                    case TextureNoise.ColourFunction.Blend:
                        texture.Function = TextureNoise.ColourFunction.Blend;
                        break;
                    case TextureNoise.ColourFunction.EitherOr:
                        texture.Function = TextureNoise.ColourFunction.EitherOr;
                        break;
                }

                UpdatePreview();

                //var currentSelector = contentControl.ContentTemplateSelector;
                //contentControl.ContentTemplateSelector = null;
                //contentControl.ContentTemplateSelector = currentSelector;
            }
        }

        private void comboBoxType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*
                        ComboBox comboBox = (ComboBox)sender;
                        if (comboBox.SelectedItem != null)
                        {
                            CookTorrence.MaterialType materialType = (CookTorrence.MaterialType)comboBox.SelectedItem;
                        }
            */
            UpdatePreview();
        }

        private void TextBox_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            UpdatePreview();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog() { Filter = "Image Files | *.bmp;*.jpg;*.jpeg;*.png;*.tiff", FileName = "" };
            if (dialog.ShowDialog() == true)
            {
                string fileName = dialog.FileName;

                if (material != null)
                {
                    if (material.Texture is TextureBitmap texture)
                    {
                        texture.FileName = fileName;

                        UpdatePreview();
                    }
                }
            }
        }

        private void ButtonNormalMap_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog() { Filter = "Image Files | *.bmp;*.jpg;*.jpeg;*.png;*.tiff", FileName = "" };
            if (dialog.ShowDialog() == true)
            {
                string fileName = dialog.FileName;

                if (material != null)
                {
                    //if (material.NormalMap is null)
                    {
                        material.NormalMap = new(fileName);
                        UpdatePreview();
                    }

                    //if (material.NormalMap != null)
                    //{
                    //    material.NormalMap.FileName = fileName;

                    //    UpdatePreview();
                    //}
                }
            }
        }
    }
}
