using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX;
using UnityEngine;

namespace FluvioFX.Editor.Blocks
{
    [VFXInfo(category = "FluvioFX/Collision")]
    class CollisionSphere : UnityEditor.VFX.Block.CollisionSphere, ICollisionSettings
    {
        public override string name
        {
            get
            {
                return "Fluid Collider (Sphere)";
            }
        }

        [VFXSetting]
        public bool boundaryPressure = true;
        [VFXSetting]
        public bool boundaryViscosity = true;
        [VFXSetting]
        public bool repulsionForce = false;

        public bool BoundaryPressure => boundaryPressure;

        public bool BoundaryViscosity => boundaryViscosity;

        public bool RepulsionForce => repulsionForce;

        public bool RoughSurface => roughSurface;

        public override IEnumerable<string> includes =>
            FluvioFXCollisionUtility.GetIncludes(base.includes);
        public override IEnumerable<VFXNamedExpression> parameters =>
            FluvioFXCollisionUtility.GetParameters(this, base.parameters);
        protected override IEnumerable<VFXPropertyWithValue> inputProperties =>
            FluvioFXCollisionUtility.GetInputProperties(this, base.inputProperties);
        public override IEnumerable<VFXAttributeInfo> attributes =>
            FluvioFXCollisionUtility.GetAttributes(this, base.attributes);

        public override string source =>
            FluvioFXCollisionUtility.GetCollisionSource(
                this,
                base.source,
                collisionResponseSource,
                roughSurfaceSource);
    }
}
