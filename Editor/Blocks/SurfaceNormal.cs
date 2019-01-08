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
        class SurfaceNormal : FluvioFXBlock
            {
                public override string name
                {
                    get
                    {
                        return "Surface Normal";
                    }
                }
                public override IEnumerable<VFXAttributeInfo> attributes
                {
                    get
                    {
                        yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                        yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                        // yield return new VFXAttributeInfo(FluvioFXAttribute.NeighborCount, VFXAttributeMode.Read);
                        yield return new VFXAttributeInfo(FluvioFXAttribute.DensityPressure, VFXAttributeMode.Read);
                        yield return new VFXAttributeInfo(FluvioFXAttribute.Normal, VFXAttributeMode.Write);
                    }
                }
#pragma warning disable 649
                public class InputProperties
                {
                    [Tooltip("Controls the mass of each fluid particle.")]
                    public float ParticleMass = 7.625f;
                    public Vector4 KernelSize = Vector4.one;
                    public Vector4 KernelFactors = Vector4.one;
                    public Texture2D FluvioSolverData;
                    public Vector2 FluvioSolverDataSize;
                }
#pragma warning restore 649
                public override string source => $@"
float3 dist;
float3 n = 0;
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
    {FluvioFXAttribute.GetLoadAttributeCode(this, FluvioFXAttribute.DensityPressure, "neighborDensityPressure", "neighborIndex")}

    dist = position - neighborPosition;
#ifndef FLUVIO_INDEX_GRID
    distLenSq = dot(dist, dist);
    if (distLenSq >= KernelSize.y) continue;
#endif

    n += (ParticleMass / neighborDensityPressure.y) * Poly6CalculateGradient(dist, KernelFactors.x, KernelSize.y);
}}

// Write to normal texture
float normalLen = length(n);
normal = float4(n / normalLen, normalLen);";
    }
}
