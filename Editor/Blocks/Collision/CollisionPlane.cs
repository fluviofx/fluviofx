using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX;
using UnityEngine;

namespace Thinksquirrel.FluvioFX.Editor.Blocks
{
    [VFXInfo(category = "FluvioFX/Collision")]
    class CollisionPlane : UnityEditor.VFX.Block.CollisionPlane
    {
        [VFXSetting]
        public bool EnableDensity = true;

        public override string name
        {
            get
            {
                return "Fluid Collider (Plane)";
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters =>
            FluvioFXCollisionUtility.GetParameters(this, base.parameters);
        protected override IEnumerable<VFXPropertyWithValue> inputProperties =>
            FluvioFXCollisionUtility.GetInputProperties(base.inputProperties, EnableDensity);
        public override IEnumerable<VFXAttributeInfo> attributes =>
            FluvioFXCollisionUtility.GetAttributes(base.attributes);

        public override string source =>
            FluvioFXCollisionUtility.GetCollisionSource(
                base.source,
                collisionResponseSource,
                EnableDensity);
    }
}
