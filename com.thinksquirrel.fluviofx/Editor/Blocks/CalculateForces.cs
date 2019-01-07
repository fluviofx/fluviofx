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
        class CalculateForces : FluvioFXBlock
            {
                public override string name
                {
                    get
                    {
                        return "Calculate Forces";
                    }
                }
                public override IEnumerable<VFXAttributeInfo> attributes
                {
                    get
                    {
                        yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                        yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                        yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.Read);
                        // yield return new VFXAttributeInfo(FluvioFXAttribute.NeighborCount, VFXAttributeMode.Read);
                        yield return new VFXAttributeInfo(FluvioFXAttribute.DensityPressure, VFXAttributeMode.Read);
                        yield return new VFXAttributeInfo(FluvioFXAttribute.Force, VFXAttributeMode.ReadWrite);
                    }
                }
#pragma warning disable 649
                public class InputProperties
                {
                    [Tooltip("Controls the mass of each fluid particle.")]
                    public float ParticleMass = 7.625f;
                    [Tooltip("Controls the artificial viscosity force of the fluid.")]
                    public float Viscosity = 2.0f;
                    public Vector4 KernelSize = Vector4.one;
                    public Vector4 KernelFactors = Vector4.one;
                    public Texture2D FluvioSolverData;
                    public Vector2 FluvioSolverDataSize;
                }
#pragma warning restore 649
                public override string source => $@"
float3 dist, density, f;
float scalar;
#ifdef FLUVIO_INDEX_GRID
uint neighborIndex;
for (uint j = 0; j < neighborCount; ++j)
{{
    neighborIndex = GetNeighborIndex(FluvioSolverData, FluvioSolverDataSize, index, j);
#else
float distLenSq;
for (uint neighborIndex = 0; neighborIndex < nbMax; ++neighborIndex)
{{
    if (index == neighborIndex) continue;
    {FluvioFXAttribute.GetLoadAttributeCode(this, VFXAttribute.Alive, "neighborAlive", "neighborIndex")}
    if (!neighborAlive) continue;
#endif

    {FluvioFXAttribute.GetLoadAttributeCode(this, VFXAttribute.Position, "neighborPosition", "neighborIndex")}
    {FluvioFXAttribute.GetLoadAttributeCode(this, VFXAttribute.Velocity, "neighborVelocity", "neighborIndex")}
    {FluvioFXAttribute.GetLoadAttributeCode(this, FluvioFXAttribute.DensityPressure, "neighborDensityPressure", "neighborIndex")}

    dist = position - neighborPosition;
#ifndef FLUVIO_INDEX_GRID
    distLenSq = dot(dist, dist);
    if (distLenSq >= KernelSize.y) continue;
#endif

    density = neighborDensityPressure.x;

    // Pressure term
    scalar = ParticleMass * (densityPressure.z + neighborDensityPressure.z) / (max(density, FLUVIO_EPSILON) * 2.0f);
    f = SpikyCalculateGradient(dist, KernelFactors.y, KernelSize.x);
    f *= scalar;

    force -= f;

    // Viscosity term
    scalar = ParticleMass
        * ViscosityCalculateLaplacian(dist, KernelFactors.z, KernelSize.z, KernelSize.x)
        * (1.0f / max(density, FLUVIO_EPSILON));

    f = (neighborVelocity - velocity) / KernelSize.w;
    f *= scalar * Viscosity;

    force += f;
}}";
    }
}
