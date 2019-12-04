using System;

namespace FluvioFX.Editor
{
    [Flags]
    internal enum SolverDataParameters : uint
    {
        None = 0,

        Fluid_ParticleMass = 1 << 0,
        Fluid_Density = 1 << 1,
        Fluid_MinimumDensity = 1 << 2,
        Fluid_GasConstant = 1 << 3,
        Fluid_Viscosity = 1 << 4,
        Fluid_SurfaceTension = 1 << 5,
        Fluid_BuoyancyCoefficient = 1 << 6,

        KernelSize = 1 << 7,
        KernelFactors = 1 << 8,
        Gravity = 1 << 9,

        All = ~0u,
    }
}
