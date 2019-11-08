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
    internal class VelocityVerlet : Integrator
    {
        protected override string source => $@"
position += velocity * dt + 0.5 * v * dt;
velocity += v;";
    }
}
