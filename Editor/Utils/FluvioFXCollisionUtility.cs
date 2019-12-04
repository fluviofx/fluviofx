using System;
using System.Collections.Generic;
using System.Linq;
using FluvioFX;
using FluvioFX.Editor.Blocks;
using UnityEditor.VFX;
using UnityEditor.VFX.Block;
using UnityEngine;

namespace FluvioFX.Editor
{
    internal static class FluvioFXCollisionUtility
    {
        public class BoundaryPressureProperties
        {
            [Min(FluvioFXSettings.kEpsilon), Tooltip("Gas constant of the collision shape")]
            public float GasConstant = 0.1f;
        }

        public class BoundaryViscosityProperties
        {
            [Min(0.0f), Tooltip("Artificial viscosity force of the collision shape")]
            public float Viscosity = 0.03f;

            [Tooltip("Velocity of the collider, in meters per second")]
            public Vector Velocity = Vector3.zero;

            [Tooltip("Angular velocity of the collider, in radians per second")]
            public Vector3 AngularVelocity = Vector3.zero;
            [Tooltip("Center of gravity of the collider")]
            public Vector CenterOfGravity = Vector3.zero;
        }

        public class RepulsionForceProperties
        {
            [Min(0.0f), Tooltip("An additional repulsion force added on collision")]
            public float RepulsionForce = 0.0f;
        }

        public static IEnumerable<string> GetIncludes(
            IEnumerable<string> baseIncludes)
        {
            foreach (var include in baseIncludes)
            {
                yield return include;
            }
            yield return $"{PackageInfo.assetPackagePath}/Shaders/FluvioCompute.cginc";
        }

        public static IEnumerable<VFXPropertyWithValue> GetInputProperties(
            ICollisionSettings collisionSettings,
            IEnumerable<VFXPropertyWithValue> baseProperties)
        {
            var properties = baseProperties;
            if (collisionSettings.BoundaryPressure)
            {
                properties = properties
                    .Concat(FluvioFXBlock.PropertiesFromType(typeof(BoundaryPressureProperties)));
            }
            if (collisionSettings.BoundaryViscosity)
            {
                properties = properties
                    .Concat(FluvioFXBlock.PropertiesFromType(typeof(BoundaryViscosityProperties)));
            }
            if (collisionSettings.RepulsionForce)
            {
                properties = properties
                    .Concat(FluvioFXBlock.PropertiesFromType(typeof(RepulsionForceProperties)));
            }

            return properties;
        }

        public static IEnumerable<VFXNamedExpression> GetParameters(
            CollisionBase block,
            IEnumerable<VFXNamedExpression> baseParameters)
        {
            var expressions = baseParameters;
            expressions = expressions.Concat(InitializeSolver.GetExpressions(
                block,
                SolverDataParameters.Fluid_Density |
                SolverDataParameters.Fluid_MinimumDensity |
                SolverDataParameters.Fluid_GasConstant |
                SolverDataParameters.KernelSize |
                SolverDataParameters.KernelFactors
            ));

            foreach (var expression in expressions)
            {
                yield return expression;
            };

            yield return new VFXNamedExpression(VFXValue.Constant(1.0f) / VFXBuiltInExpression.DeltaTime, "invDt");
        }

        public static IEnumerable<VFXAttributeInfo> GetAttributes(
            CollisionBase block,
            IEnumerable<VFXAttributeInfo> baseAttributes)
        {
            var hasPreviousPosition = block.GetData().IsCurrentAttributeWritten(FluvioFXAttribute.PreviousPosition);
            if (hasPreviousPosition)
            {
                yield return new VFXAttributeInfo(FluvioFXAttribute.PreviousPosition, VFXAttributeMode.Read);
            }

            foreach (var attribute in baseAttributes)
            {
                yield return attribute;
            }

            yield return new VFXAttributeInfo(VFXAttribute.ParticleId, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(FluvioFXAttribute.DensityPressure, VFXAttributeMode.ReadWrite);
            yield return new VFXAttributeInfo(FluvioFXAttribute.Force, VFXAttributeMode.ReadWrite);
        }

        private static string GetFluidResponseSource(
            ICollisionSettings settings,
            string roughSurfaceSource)
        {
            var hasPreviousPosition = false;
            var hasLifetime = false;
            var block = settings as VFXBlock;
            if (block != null)
            {
                hasPreviousPosition = block.GetData().IsCurrentAttributeWritten(FluvioFXAttribute.PreviousPosition);
                hasLifetime = block.GetData().IsCurrentAttributeWritten(VFXAttribute.Alive);
            }

            var src = "";
            if (settings.RoughSurface)
            {
                src += roughSurfaceSource;
            }

            src += $@"
    float projVelocity = dot(n, velocity);

    float3 normalVelocity = projVelocity * n;
    float3 tangentVelocity = velocity - normalVelocity;

    float3 v = 0;
    if (projVelocity < 0)
    {{
        v -= ((1 + Elasticity) * projVelocity) * n;
    }}
    v -= Friction * tangentVelocity;

    // Repulsion force
    {(settings.RepulsionForce ? "force += v * (1.0f / mass) * RepulsionForce * invDt;" : "")}

    velocity += v;
    {(hasPreviousPosition ? "previousPosition = position - v;" : "")}
    age += (LifetimeLoss * lifetime);";

            return src;
        }

        public static string GetCollisionSource(
            ICollisionSettings settings,
            string baseSource,
            string collisionResponseSource,
            string roughSurfaceSource)
        {
            if (!settings.BoundaryPressure && !settings.BoundaryViscosity)
            {
                return baseSource.Replace(collisionResponseSource, GetFluidResponseSource(settings, roughSurfaceSource));
            }

            var proxyTestSource = baseSource
                .Replace("float3 nextPos = position + velocity * deltaTime;", "")
                .Replace("nextPos", "proxyPosition")
                .Replace("position", "dummyPosition")
                .Replace(collisionResponseSource, "    collisionTest = true;");

            var split = proxyTestSource.Split(new []
            {
                "\r\n",
                "\r",
                "\n"
            }, StringSplitOptions.None);
            proxyTestSource = string.Join("\n                ", split);

            var data = (settings as VFXBlock)?.GetData();
            return $@"{FluvioFXBlock.CheckAlive(data)}
{{
    float3 offset, proxyPosition, dummyPosition, dist;
    float density = 0;
    float pressure, lenSq, diffSq;
    bool collisionTest = false;

    float searchRadius = solverData_KernelSize.x;
    float step = solverData_KernelSize.w;

    float3 gridJitter = float3(FIXED_RAND3(0x5f7b48e2)) * step;

    float x, y, z;

    // Fluid density calculation
    for(x = -searchRadius; x < searchRadius; x += step)
    {{
        for(y = -searchRadius; y < searchRadius; y += step)
        {{
            for(z = -searchRadius; z < searchRadius; z += step)
            {{
                // Get proxy particle position
                offset = float3(x, y, z);
                proxyPosition = position + offset;

                // Snap to random grid, but keep the grid consistent per particle
                proxyPosition = (round(proxyPosition / step) * step) + gridJitter;
                collisionTest = false;
                {proxyTestSource}

                if (collisionTest)
                {{
                    // Range check
                    // TODO: We can optimize this away since we know the precise step size
                    dist = position - proxyPosition;
                    lenSq = dot(dist, dist);
                    if (lenSq < solverData_KernelSize.y)
                    {{
                        // Sum density
                        diffSq = solverData_KernelSize.y - lenSq;
                        density += mass * Poly6Calculate(diffSq, solverData_KernelFactors.x);
                    }}
                }}
            }}
        }}
    }}

    density = max(density, solverData_Fluid_MinimumDensity);
    pressure = solverData_Fluid_GasConstant * (density - solverData_Fluid_Density);

    float2 neighborDensityPressure = float2(
        solverData_Fluid_Density,
        {(settings.BoundaryPressure ? "GasConstant * solverData_Fluid_Density" : "0")});

    float len, len3, diff, bodyRadiusSq, scalar;
    float3 f, neighborVelocity, bodyDist;
    for(x = -searchRadius; x < searchRadius; x += step)
    {{
        for(y = -searchRadius; y < searchRadius; y += step)
        {{
            for(z = -searchRadius; z < searchRadius; z += step)
            {{
                if (abs(x) < step && abs(y) < step && abs(z) < step)
                {{
                    continue;
                }}

                // Get proxy particle position
                offset = float3(x, y, z);
                proxyPosition = position + offset;

                // Snap to random grid, but keep the grid consistent per particle
                proxyPosition = (round(proxyPosition / step) * step) + gridJitter;
                collisionTest = false;
                {proxyTestSource}

                if (collisionTest)
                {{
                    // Range check
                    // TODO: We can optimize this away since we know the precise step size
                    dist = position - proxyPosition;
                    lenSq = dot(dist, dist);
                    if (lenSq < solverData_KernelSize.y)
                    {{
                        // For kernels
                        lenSq = dot(dist, dist);
                        len = sqrt(lenSq);
                        len3 = len * len * len;
                        diffSq = solverData_KernelSize.y - lenSq;
                        diff = solverData_KernelSize.x - len;

                        {(settings.BoundaryPressure ? @"// Pressure term
                        scalar = mass
                            * (pressure + neighborDensityPressure.y) / (neighborDensityPressure.x * 2.0f);
                        f = SpikyCalculateGradient(dist, diff, len, solverData_KernelFactors.y);
                        f *= scalar;

                        force -= f;" : "")}

                        {(settings.BoundaryViscosity ? @"// Viscosity term
                        bodyDist = proxyPosition - CenterOfGravity;
                        bodyRadiusSq = sqrt(dot(bodyDist, bodyDist));
                        neighborVelocity = Velocity + (AngularVelocity / FLUVIO_TAU) * FLUVIO_TAU * bodyRadiusSq;

                        scalar = mass
                            * ViscosityCalculateLaplacian(
                                diff,
                                solverData_KernelSize.z,
                                solverData_KernelFactors.z)
                            * (1.0f / neighborDensityPressure.x);

                        f = neighborVelocity - velocity;
                        f *= scalar * Viscosity;

                        force += f;" : "")}
                    }}
                }}
            }}
        }}
    }}

    // Write totals to density/pressure (zw)
    density = max(densityPressure.z + density, solverData_Fluid_MinimumDensity);
    pressure = solverData_Fluid_GasConstant * (density - solverData_Fluid_Density);
    densityPressure.zw = float2(density, pressure);
}}

{baseSource.Replace(collisionResponseSource, GetFluidResponseSource(settings,roughSurfaceSource))}";
        }
    };
}
