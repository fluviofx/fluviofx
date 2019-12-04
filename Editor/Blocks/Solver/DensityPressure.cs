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
                yield return new VFXAttributeInfo(FluvioFXAttribute.NeighborCount, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(FluvioFXAttribute.DensityPressure, VFXAttributeMode.Write);
            }
        }
        protected internal override SolverDataParameters solverDataParameters =>
            SolverDataParameters.Fluid_Density |
            SolverDataParameters.Fluid_MinimumDensity |
            SolverDataParameters.Fluid_GasConstant |
            SolverDataParameters.KernelFactors |
            SolverDataParameters.KernelSize;

        public override string source => $@"{CheckAlive()}
float3 dist;
float density = 0;
float pressure, diffSq;
for (uint j = 0; j < neighborCount; ++j)
{{
    {LoadNeighbor("neighborIndex", "index", "j")}
    {Load(VFXAttribute.Position, "neighborPosition", "neighborIndex")}
    {Load(VFXAttribute.Mass, "neighborMass", "neighborIndex")}

    dist = position - neighborPosition;
    diffSq = solverData_KernelSize.y - dot(dist, dist);
    density += neighborMass * Poly6Calculate(diffSq, solverData_KernelFactors.x);
}}

// Write to density/pressure
density = max(density, solverData_Fluid_MinimumDensity);
pressure = solverData_Fluid_GasConstant * (density - solverData_Fluid_Density);
densityPressure.xy = float2(density, pressure);";
    }
}
