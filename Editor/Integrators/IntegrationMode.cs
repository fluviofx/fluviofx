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
    public enum IntegrationMode
    {
        // Stormer-Verlet
        Verlet,
        // Velocity Verlet
        VelocityVerlet,
        // Semi-implicit Euler
        Euler
    }
}
