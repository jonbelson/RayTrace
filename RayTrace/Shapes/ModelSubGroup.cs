using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    // Class to represent a group/object of a Model, required to handle the case
    //  where it has a different Material.
    public class ModelSubGroup : Entity
    {
        private Model _model;
        private ModelData _modelData;

        private ModelData.SubGroup _group;

        protected override void EnabledStateChanged()
        {
            //_model.SetGroupEnabled();
            _group.Enabled = Enabled;
        }

        public Material Material { get; set; }

        //
        // Constructor
        //
        public ModelSubGroup(Model owner, ModelData.SubGroup group)
        {
            _model = owner;
            _modelData = owner.GetModelData();
            _group = group;
            Name = group.Name;
        }

        public override HitResult HitTest(Camera camera, Ray ray)
        {
            throw new NotImplementedException();
        }
    }
}
