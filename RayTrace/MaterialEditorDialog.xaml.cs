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

        private readonly List<MaterialType> materialTypes = new() { MaterialType.Diffuse, MaterialType.Reflective, MaterialType.RefractiveAndReflective, MaterialType.Phong };

        private readonly MaterialTileMaker materialTileMaker = new();

        public MaterialTile MaterialTile { get; set; } = null;

        //private readonly Material material;
        //[NotifyParentProperty(true)]
        public Material material { get; set; } = null;
        public Texture texture { get; set; } = null;

        private TextureType tt;
        public TextureType textureType { get { return tt; } set { tt = value; Notify(); }  }

        public ObservableCollection<Material> Materials { get; set; } = new();

        //public enum TextureType { Solid, Perlin, Squares, Gradient };
        public ObservableCollection<TextureType> Textures { get; set; } = new() { TextureType.Solid, TextureType.Perlin, TextureType.Squares, TextureType.Gradient };

        private void Notify([CallerMemberName] String propName="")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

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
            texture = material.Texture;
            textureType = material.Texture.Type;

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
                    case TextureType.Gradient: material.Texture = new TextureGradient(); break;
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
    }
}
