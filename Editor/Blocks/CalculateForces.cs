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
    class CalculateForces : FluvioFXBlock
    {
        [VFXSetting]
        public bool SurfaceTension = true;
        [VFXSetting]
        public bool Turbulence = true;
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
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                if (hasLifetime)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                }
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
                // yield return new VFXAttributeInfo(FluvioFXAttribute.NeighborCount, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(FluvioFXAttribute.DensityPressure, VFXAttributeMode.Read);
                if (SurfaceTension)
                {
                    yield return new VFXAttributeInfo(FluvioFXAttribute.Normal, VFXAttributeMode.Read);
                }
                if (Turbulence)
                {
                    yield return new VFXAttributeInfo(
                        FluvioFXAttribute.VorticityTurbulence,
                        VFXAttributeMode.ReadWrite);
                }
                yield return new VFXAttributeInfo(FluvioFXAttribute.Force, VFXAttributeMode.ReadWrite);
            }
        }

        private IEnumerable<string> defines
        {
            get
            {
                if (SurfaceTension) yield return "#define FLUVIO_SURFACE_TENSION_ENABLED 1";
                if (Turbulence) yield return "#define FLUVIO_TURBULENCE_ENABLED 1";
                if (Gravity)
                {
                    yield return "#define FLUVIO_GRAVITY_ENABLED 1";
                    if (BuoyancyForce) yield return "#define FLUVIO_BUOYANCY_FORCE_ENABLED 1";
                }
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

        public override string source => $@"
{string.Join("\n", defines)}
float3 dist, f, invNeighborDensity;
float scalar;

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
    {FluvioFXAttribute.GetLoadAttributeCode(this, VFXAttribute.Velocity, "neighborVelocity", "neighborIndex")}
    {FluvioFXAttribute.GetLoadAttributeCode(
        this,
        FluvioFXAttribute.DensityPressure,
        "neighborDensityPressure",
        "neighborIndex")}

    dist = position - neighborPosition;
#ifndef FLUVIO_INDEX_GRID
    distLenSq = dot(dist, dist);
    if (distLenSq >= solverData_KernelSize.y) continue;
#endif

    // Pressure term
    scalar = neighborMass
        * (densityPressure.y + neighborDensityPressure.y) / (neighborDensityPressure.x * 2.0f);
    f = SpikyCalculateGradient(dist, solverData_KernelFactors.y, solverData_KernelSize.x);
    f *= scalar;

    force -= f;

    invNeighborDensity = 1.0f / neighborDensityPressure.x;

    // Viscosity term
    scalar = neighborMass
        * ViscosityCalculateLaplacian(
            dist,
            solverData_KernelFactors.z,
            solverData_KernelSize.z,
            solverData_KernelSize.x)
        * invNeighborDensity;

    f = neighborVelocity - velocity;
    f *= scalar * solverData_Fluid_Viscosity;

    force += f;

#ifdef FLUVIO_SURFACE_TENSION_ENABLED
    // Surface tension term (external)
    if (normal.w > FLUVIO_PI && normal.w < FLUVIO_TAU)
    {{
        scalar = neighborMass
            * Poly6CalculateLaplacian(dist, solverData_KernelFactors.x, solverData_KernelSize.y)
            * solverData_Fluid_SurfaceTension
            * 1.0f / neighborDensityPressure.x;

        f = normal.xyz * scalar;
        force -= f;
    }}
#endif

#ifdef FLUVIO_TURBULENCE_ENABLED
    // Turbulence term (external)
    {(Turbulence
        ? FluvioFXAttribute.GetLoadAttributeCode(
            this,
            FluvioFXAttribute.VorticityTurbulence,
            "neighborVorticityTurbulence",
            "neighborIndex")
        : "")}
    if (vorticityTurbulence.w >= solverData_Fluid_TurbulenceProbability && neighborVorticityTurbulence.w < solverData_Fluid_TurbulenceProbability)
    {{
        scalar = neighborMass
            * ViscosityCalculateLaplacian(
                dist,
                solverData_KernelFactors.z,
                solverData_KernelSize.z,
                solverData_KernelSize.x)
            * invNeighborDensity;

        vorticityTurbulence = scalar * (neighborVorticityTurbulence - vorticityTurbulence);
        f = clamp_len(FLUVIO_TURBULENCE_CONSTANT * cross(dist, vorticityTurbulence.xyz), FLUVIO_MAX_SQR_VELOCITY_CHANGE * mass);

        force += f;
    }}
#endif
}}

#ifdef FLUVIO_GRAVITY_ENABLED
// Gravity term (external)
force += solverData_Gravity * mass;
#ifdef FLUVIO_BUOYANCY_FORCE_ENABLED
// Buoyancy term (external)
force += solverData_Gravity * solverData_Fluid_BuoyancyCoefficient * (densityPressure.x - solverData_Fluid_Density);
#endif
#endif
";
    }
}
