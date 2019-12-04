using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace FluvioFX.Editor.Integrators
{
    internal class Verlet : Integrator
    {
        protected override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(FluvioFXAttribute.PreviousPosition, VFXAttributeMode.ReadWrite);
            }
        }

        protected override string source => $@"
float3 x = position;
position = (x * 2) - previousPosition + ((v + velocity) * dt);
velocity += v;";
    }
}
