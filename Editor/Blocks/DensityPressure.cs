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
    class DensityPressure : FluvioFXBlock
        {
            public override string name
            {
                get
                {
                    return "Calculate Density and Pressure";
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
                    yield return new VFXAttributeInfo(FluvioFXAttribute.DensityPressure, VFXAttributeMode.Write);
                }
            }
            public override string source => $@"
float3 dist;
float density = 0;
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

    dist = position - neighborPosition;
#ifndef FLUVIO_INDEX_GRID
    distLenSq = dot(dist, dist);
    if (distLenSq >= solverData_KernelSize.y) continue;
#endif

    density += neighborMass * Poly6Calculate(dist, solverData_KernelFactors.x, solverData_KernelSize.y);
}}

// Write to density/pressure texture
density = max(density, solverData_Fluid_MinimumDensity);
densityPressure = float4(density, density, solverData_Fluid_GasConstant * (density - solverData_Fluid_Density), 0);";
    }
}
