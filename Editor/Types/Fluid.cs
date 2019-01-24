using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace Thinksquirrel.FluvioFX.Editor
{
    [VFXType, Serializable]
    internal struct Fluid
    {
        [Min(FluvioFXSettings.kEpsilon), Tooltip(
            "Controls the smoothing distance of fluid particles. This changes the overall simulation resolution " +
            "and greatly affects all other properties.")]
        public float SmoothingDistance;
        [Min(FluvioFXSettings.kEpsilon), Tooltip("Controls the mass of each fluid particle.")]
        public float ParticleMass;
        [Min(FluvioFXSettings.kEpsilon), Tooltip("Controls the overall density of the fluid.")]
        public float Density;
        [Min(FluvioFXSettings.kEpsilon), Tooltip(
            "Controls the minimum density of any fluid particle. This can be used to help stabilize a " +
            "low-viscosity fluid.")]
        public float MinimumDensity;

        [Min(FluvioFXSettings.kEpsilon), Tooltip(
            "Controls the gas constant of the fluid, which in turn affects the pressure forces applied " +
            "to particles.")]
        public float GasConstant;
        [Min(FluvioFXSettings.kEpsilon), Tooltip("Controls the artificial viscosity force of the fluid.")]
        public float Viscosity;
        [Min(0.0f), Tooltip(
            "Controls the surface tension of the fluid (intended for liquids only). FluvioFX uses a " +
            "simplified surface tension model that is ideal for realtime use.")]
        public float SurfaceTension;
        [Min(0.0f), Tooltip(
            "Controls the buoyancy coefficient of a fluid. This controls the buoyancy force, which is a " +
            "density-dependent force in the opposite direction of gravity. This should be used when " +
            "simulating gas like smoke or fire, or fluids that are less dense than the surrounding " +
            "material.")]
        public float BuoyancyCoefficient;

        public static Fluid defaultValue = new Fluid
        {
            SmoothingDistance = 0.38125f,
            ParticleMass = 7.625f,
            Density = 998.29f,
            MinimumDensity = 9.9829f,
            GasConstant = 0.01f,
            Viscosity = 0.3f,
            SurfaceTension = 0.728f,
            BuoyancyCoefficient = 0.0f,
        };

        internal IEnumerable<VFXNamedExpression> defaultExpressions
        {
            get
            {
                yield return new VFXNamedExpression(VFXValue.Constant(SmoothingDistance), nameof(SmoothingDistance));
                yield return new VFXNamedExpression(VFXValue.Constant(ParticleMass), nameof(ParticleMass));
                yield return new VFXNamedExpression(VFXValue.Constant(Density), nameof(Density));
                yield return new VFXNamedExpression(VFXValue.Constant(MinimumDensity), nameof(MinimumDensity));
                yield return new VFXNamedExpression(VFXValue.Constant(GasConstant), nameof(GasConstant));
                yield return new VFXNamedExpression(VFXValue.Constant(Viscosity), nameof(Viscosity));
                yield return new VFXNamedExpression(VFXValue.Constant(SurfaceTension), nameof(SurfaceTension));
                yield return new VFXNamedExpression(VFXValue.Constant(BuoyancyCoefficient), nameof(BuoyancyCoefficient));
            }
        }
    }
}
