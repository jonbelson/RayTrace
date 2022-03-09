using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    public class Entity : INotifyPropertyChanged
    {
        private bool enabled = true;

        public bool Enabled {
            get => enabled;
            set { if (enabled != value) { enabled = value; Notify("Enabled"); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName)); 
        }

        public Guid UID { get; /*set;*/ } = Guid.NewGuid();

        public Scene Scene { get; set; }

        public string Name { get; set; }
        
        //
        // Constructor
        //
        public Entity()
        {
            Name = GetType().Name;
        }

        //
        // Constructor
        //
        public Entity(XMLEntity xmlEntity)
        {
            UID = xmlEntity.UID;
            Name = xmlEntity.Name;
        }

        // Allow Shape to pre-transform data using the Camera's WorldToView Matrix.
        public virtual void CreateCache(Camera camera) { }

        protected virtual void AsXMLImpl(XMLEntity xml)
        {
            xml.UID = UID;
            xml.Enabled = Enabled;
            xml.Name = Name;
        }

        public virtual XMLEntity ToXML()
        {
            XMLEntity xml = new XMLEntity();
            AsXMLImpl(xml);
            return xml;
        }
    }
}
