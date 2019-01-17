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
    [VFXInfo(category = "FluvioFX/Solver")]
    class SurfaceNormal : FluvioFXBlock
        {
            public override string name
            {
                get
                {
                    return "Calculate Surface Normal";
                }
            }

            public override IEnumerable<VFXAttributeInfo> attributes
            {
                get
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                    if (hasLifetime)
                    {
                        yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                    }
                    yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);

                    // yield return new VFXAttributeInfo(FluvioFXAttribute.NeighborCount, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(FluvioFXAttribute.DensityPressure, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(FluvioFXAttribute.Normal, VFXAttributeMode.Write);
                }
            }

            public override string source => $@"
float3 dist;
float3 n = 0;
#ifdef FLUVIO_INDEX_GRID
uint neighborIndex;
for (uint j = 0; j < neighborCount; ++j)
{{
    neighborIndex = GetNeighborIndex(solverData_Tex, solverData_TexSize, index, j);
#else
float distLenSq;
for (uint neighborIndex = 0; neighborIndex < nbMax; ++neighborIndex)
{{
    if (index == neighborIndex) continue;
    {(hasLifetime ? FluvioFXAttribute.GetLoadAttributeCode(this, VFXAttribute.Alive, "neighborAlive", "neighborIndex") : "")}
    {(hasLifetime ? "if (!neighborAlive) continue;" : "")}
#endif

    {FluvioFXAttribute.GetLoadAttributeCode(this, VFXAttribute.Position, "neighborPosition", "neighborIndex")}
    {FluvioFXAttribute.GetLoadAttributeCode(this, VFXAttribute.Mass, "neighborMass", "neighborIndex")}
    {FluvioFXAttribute.GetLoadAttributeCode(this, FluvioFXAttribute.DensityPressure, "neighborDensityPressure", "neighborIndex")}

    dist = position - neighborPosition;
#ifndef FLUVIO_INDEX_GRID
    distLenSq = dot(dist, dist);
    if (distLenSq >= solverData_KernelSize.y) continue;
#endif

    n += (neighborMass / neighborDensityPressure.y) * Poly6CalculateGradient(dist, solverData_KernelFactors.x, solverData_KernelSize.y);
}}

// Write to normal texture
float normalLen = length(n);
normal = float4(n / normalLen, normalLen);";
    }
}
