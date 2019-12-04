using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace FluvioFX.Editor.Blocks
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
                yield return new VFXAttributeInfo(FluvioFXAttribute.NeighborCount, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(FluvioFXAttribute.DensityPressure, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(FluvioFXAttribute.Normal, VFXAttributeMode.Write);
            }
        }

        protected internal override SolverDataParameters solverDataParameters =>
            SolverDataParameters.KernelSize |
            SolverDataParameters.KernelFactors;

        public override string source => $@"{CheckAlive()}
float3 dist;
float3 n = 0;
float lenSq, diffSq;
for (uint j = 0; j < neighborCount; ++j)
{{
    {LoadNeighbor("neighborIndex", "index", "j")}

    {Load(VFXAttribute.Position, "neighborPosition", "neighborIndex")}
    {Load(VFXAttribute.Mass, "neighborMass", "neighborIndex")}
    {Load(FluvioFXAttribute.DensityPressure, "neighborDensityPressure", "neighborIndex")}

    dist = position - neighborPosition;
    lenSq = dot(dist, dist);
    diffSq = solverData_KernelSize.y - lenSq;
    n +=
        (neighborMass / neighborDensityPressure.x) *
        Poly6CalculateGradient(diffSq, solverData_KernelFactors.x, solverData_KernelSize.y);
}}

// Write to normal
float normalLen = max(length(n), FLUVIO_EPSILON);
normal = float4(n / normalLen, normalLen);";
    }
}
