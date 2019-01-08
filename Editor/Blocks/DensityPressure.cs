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
        class DensityPressure : FluvioFXBlock
            {
                public override string name
                {
                    get
                    {
                        return "Density/Pressure";
                    }
                }
                public override IEnumerable<VFXAttributeInfo> attributes
                {
                    get
                    {
                        yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                        yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                        // yield return new VFXAttributeInfo(FluvioFXAttribute.NeighborCount, VFXAttributeMode.Read);
                        yield return new VFXAttributeInfo(FluvioFXAttribute.DensityPressure, VFXAttributeMode.Write);
                    }
                }
#pragma warning disable 649
                public class InputProperties
                {
                    [Tooltip("Controls the mass of each fluid particle.")]
                    public float ParticleMass = 7.625f;
                    [Tooltip("Controls the overall density of the fluid.")]
                    public float Density = 998.29f;
                    [Tooltip("Controls the minimum density of any fluid particle. This can be used to help stabilize a low-viscosity fluid.")]
                    public float MinimumDensity = 499.145f;
                    [Tooltip("Controls the gas constant of the fluid, which in turn affects the pressure forces applied to particles.")]
                    public float GasConstant = 0.01f;
                    public Vector4 KernelSize = Vector4.one;
                    public Vector4 KernelFactors = Vector4.one;
                    public Texture2D FluvioSolverData;
                    public Vector2 FluvioSolverDataSize;
                }
#pragma warning restore 649
                public override string source => $@"
float3 dist;
float density = 0;
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

    dist = position - neighborPosition;
#ifndef FLUVIO_INDEX_GRID
    distLenSq = dot(dist, dist);
    if (distLenSq >= KernelSize.y) continue;
#endif

    density += ParticleMass * Poly6Calculate(dist, KernelFactors.x, KernelSize.y);
}}

// Write to density/pressure texture
density = max(density, MinimumDensity);
densityPressure = float4(density, density, GasConstant * (density - Density), 0);";
    }
}
