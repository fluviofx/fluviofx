using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace Thinksquirrel.FluvioFX.Editor.Integrators
{
    internal class Verlet : Integrator
    {
        protected override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.OldPosition, VFXAttributeMode.Read);
            }
        }

        protected override string source => $@"
float3 x = position;
position = (x * 2) - oldPosition + ((v + velocity) * dt);
velocity += v;";
    }
}
