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
    class CalculateForces : FluvioFXBlock
    {
        [VFXSetting]
        public bool SurfaceTension = true;
        [VFXSetting]
        public bool Gravity = true;
        [VFXSetting]
        public bool BuoyancyForce = true;

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
                yield return new VFXAttributeInfo(FluvioFXAttribute.NeighborCount, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(FluvioFXAttribute.DensityPressure, VFXAttributeMode.Read);
                if (SurfaceTension)
                {
                    yield return new VFXAttributeInfo(FluvioFXAttribute.Normal, VFXAttributeMode.Read);
                }
                yield return new VFXAttributeInfo(FluvioFXAttribute.Force, VFXAttributeMode.ReadWrite);
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (!Gravity)
                    yield return nameof(BuoyancyForce);
            }
        }

        protected internal override SolverDataParameters solverDataParameters
        {
            get
            {
                var solverDataParams =
                    SolverDataParameters.Fluid_Viscosity |
                    SolverDataParameters.KernelFactors |
                    SolverDataParameters.KernelSize;

                if (SurfaceTension)
                {
                    solverDataParams |= SolverDataParameters.Fluid_SurfaceTension;
                }
                if (Gravity)
                {
                    solverDataParams |= SolverDataParameters.Gravity;

                    if (BuoyancyForce)
                    {
                        solverDataParams |=
                            SolverDataParameters.Fluid_Density |
                            SolverDataParameters.Fluid_BuoyancyCoefficient;
                    }
                }

                return solverDataParams;
            }
        }

        public override string source
        {
            get
            {
                string src =
                    $@"{CheckAlive()}
        float3 dist, f;
        float invNeighborDensity, lenSq, len, len3, diff, diffSq, scalar;
        for (uint j = 0; j < neighborCount; ++j)
        {{
            {LoadNeighbor("neighborIndex", "index", "j")}

            {Load(VFXAttribute.Position, "neighborPosition", "neighborIndex")}
            {Load(VFXAttribute.Mass, "neighborMass", "neighborIndex")}
            {Load(VFXAttribute.Velocity, "neighborVelocity", "neighborIndex")}
            {Load(FluvioFXAttribute.DensityPressure, "neighborDensityPressure", "neighborIndex")}

            dist = position - neighborPosition;

            // For kernels
            lenSq = dot(dist, dist);
            len = sqrt(lenSq);
            len3 = len * len * len;
            diffSq = solverData_KernelSize.y - lenSq;
            diff = solverData_KernelSize.x - len;

            // Pressure term
            scalar = neighborMass
                * (densityPressure.y + neighborDensityPressure.y) / (neighborDensityPressure.x * 2.0f);
            f = SpikyCalculateGradient(dist, diff, len, solverData_KernelFactors.y);
            f *= scalar;

            force -= f;

            invNeighborDensity = 1.0f / neighborDensityPressure.x;

            // Viscosity term
            scalar = neighborMass
                * ViscosityCalculateLaplacian(diff, solverData_KernelSize.z, solverData_KernelFactors.z)
                * invNeighborDensity;

            f = neighborVelocity - velocity;
            f *= scalar * solverData_Fluid_Viscosity;

            force += f;";

                if (SurfaceTension)
                {
                    src += $@"

            // Surface tension term (external)
            if (normal.w > FLUVIO_PI && normal.w < FLUVIO_TAU)
            {{
                scalar = neighborMass
                    * Poly6CalculateLaplacian(lenSq, diffSq, solverData_KernelFactors.x)
                    * solverData_Fluid_SurfaceTension
                    * 1.0f / neighborDensityPressure.x;

                f = normal.xyz * scalar;
                force -= f;
            }}";
                }

                src += @"
        }";

                if (Gravity)
                {
                    src += $@"

        // Gravity term (external)
        force += solverData_Gravity * mass;";

                    if (BuoyancyForce)
                    {
                        src += $@"
        // Buoyancy term (external)
        force += solverData_Gravity * solverData_Fluid_BuoyancyCoefficient * (densityPressure.x - solverData_Fluid_Density);";
                    }
                }

                return src;
            }
        }
    }
}
