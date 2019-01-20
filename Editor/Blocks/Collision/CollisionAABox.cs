using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX;
using UnityEngine;

namespace Thinksquirrel.FluvioFX.Editor.Blocks
{
    [VFXInfo(category = "FluvioFX/Collision")]
    class CollisionAABox : UnityEditor.VFX.Block.CollisionAABox
    {
        [VFXSetting]
        public bool EnableFluidForces = true;

        public override string name
        {
            get
            {
                return "Fluid Collider (AABox)";
            }
        }

        public override IEnumerable<string> includes =>
            FluvioFXCollisionUtility.GetIncludes(base.includes);
        public override IEnumerable<VFXNamedExpression> parameters =>
            FluvioFXCollisionUtility.GetParameters(this, base.parameters);
        protected override IEnumerable<VFXPropertyWithValue> inputProperties =>
            FluvioFXCollisionUtility.GetInputProperties(base.inputProperties, EnableFluidForces);
        public override IEnumerable<VFXAttributeInfo> attributes =>
            FluvioFXCollisionUtility.GetAttributes(base.attributes);

        public override string source =>
            FluvioFXCollisionUtility.GetCollisionSource(
                this,
                EnableFluidForces,
                base.source,
                collisionResponseSource,
                roughSurfaceSource);
    }
}
