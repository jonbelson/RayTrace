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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName)); 
        }

        public bool Enabled {
            get { return enabled; }
            set { if (enabled != value) { enabled = value; Notify("Enabled"); } }
        }

        public string Name { get; set; }
        
        public Entity()
        {
            Name = GetType().Name;
        }

        protected virtual void AsXMLImpl(XMLEntity xml)
        {
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
