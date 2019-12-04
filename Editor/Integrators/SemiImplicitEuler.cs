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
    internal class Euler : Integrator
    {
        protected override string source => $@"
velocity += v;
position += velocity * dt;";
    }
}
