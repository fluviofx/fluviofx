using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace Thinksquirrel.FluvioFX.Editor.Blocks
{
    [VFXInfo(category = "FluvioFX")]
    class ExternalForces : FluvioFXBlock
    {
        public override string name
        {
            get
            {
                return "External Forces";
            }
        }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(FluvioFXAttribute.DensityPressure, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(FluvioFXAttribute.Normal, VFXAttributeMode.ReadWrite);
            }
        }
#pragma warning disable 649
        public class InputProperties
        {
            [Tooltip("Controls the mass of each fluid particle.")]
            public float ParticleMass = 7.625f;
            [Tooltip("Controls the surface tension of the fluid (intended for liquids only). FluvioFX uses a simplified surface tension model that is ideal for realtime use.")]
            public float SurfaceTension = 0.728f;
            [Tooltip("Controls the buoyancy coefficient of a fluid. This controls the buoyancy force, which is a density-dependent force in the opposite direction of gravity. This should be used when simulating gas like smoke or fire, or fluids that are less dense than the surrounding material.")]
            public float BuoyancyCoefficient = 0.0f;
            [Tooltip("Gravity constant for fluid simulation.")]
            public Vector3 Gravity = new Vector3(0.0f, -9.81f, 0.0f);
            public Vector4 KernelSize = Vector4.one;
            public Vector4 KernelFactors = Vector4.one;
            public Texture2D FluvioSolverData;
            public Vector2 FluvioSolverDataSize;
        }
#pragma warning restore 649

        public override string source
        {
            get
            {
                return @"";
            }
        }
    }
}
