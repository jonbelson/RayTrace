using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
    public partial class MaterialEditorDialog : Window
    {
        private readonly List<MaterialType> materialTypes = new() { MaterialType.Diffuse, MaterialType.Reflective, MaterialType.RefractiveAndReflective, MaterialType.Phong };

        private readonly MaterialTileMaker materialTileMaker = new();

        public MaterialTile MaterialTile { get; set; } = null;

        //private readonly Material material;
        //[NotifyParentProperty(true)]
        public Material material { get; set; } = null;

        public ObservableCollection<Material> Materials { get; set; } = new();

        public enum TextureType { Solid, Perlin, Squares };
        public ObservableCollection<TextureType> Textures { get; set; } = new() { TextureType.Solid, TextureType.Perlin, TextureType.Squares };

        private void UpdatePreview()
        {
            MaterialTile = materialTileMaker.MakeTile(material);
            //WriteableBitmap wb = BitmapToWriteableBitmap(materialTile.Bitmap);

            WriteableBitmap wb = MaterialTile.Bitmap.ToWriteableBitmap();

            preview.Source = wb;
        }

        public MaterialEditorDialog(Material mat)
        {
            material = mat;

            InitializeComponent();

            this.DataContext = this;

            Materials.Add(material);

            comboBoxMaterialType.ItemsSource = materialTypes;
            comboBoxMaterialType.SelectedItem = mat.Type;

            //comboBoxTexture.ItemSource = Textures;

            UpdatePreview();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void comboBoxMaterialType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MaterialType type = (MaterialType)comboBoxMaterialType.SelectedItem;

            material.Type = type;

            Materials.Clear();
            Materials.Add(material);

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
                }

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
    }
}
