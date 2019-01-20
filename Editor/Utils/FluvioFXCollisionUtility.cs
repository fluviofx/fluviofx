using System;
using System.Collections.Generic;
using System.Linq;
using Thinksquirrel.FluvioFX;
using Thinksquirrel.FluvioFX.Editor.Blocks;
using UnityEditor.VFX;
using UnityEditor.VFX.Block;
using UnityEngine;

namespace Thinksquirrel.FluvioFX.Editor
{
    internal static class FluvioFXCollisionUtility
    {
        public class BoundaryPressureProperties
        {
            [Min(FluvioFXSettings.kEpsilon), Tooltip("Gas constant of the collision shape")]
            public float GasConstant = 2.0f;
        }

        public class BoundaryViscosityProperties
        {
            [Min(0.0f), Tooltip("Artificial viscosity force of the collision shape")]
            public float Viscosity = 0.3f;

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
            expressions = expressions.Concat(FluvioFXBlock.GetSolverDataExpressions(block));

            foreach (var expression in expressions)
            {
                yield return expression;
            };

            yield return new VFXNamedExpression(VFXValue.Constant(1.0f) / VFXBuiltInExpression.DeltaTime, "invDt");
        }

        public static IEnumerable<VFXAttributeInfo> GetAttributes(IEnumerable<VFXAttributeInfo> baseAttributes)
        {
            foreach (var attribute in baseAttributes)
            {
                yield return attribute;
            }

            yield return new VFXAttributeInfo(VFXAttribute.ParticleId, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(VFXAttribute.OldPosition, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(FluvioFXAttribute.DensityPressure, VFXAttributeMode.ReadWrite);
            yield return new VFXAttributeInfo(FluvioFXAttribute.Force, VFXAttributeMode.ReadWrite);
        }

        private static string GetFluidResponseSource(
            ICollisionSettings settings,
            string roughSurfaceSource)
        {
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
    oldPosition = position;
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

            return $@"
{{
    float3 offset, proxyPosition, dummyPosition, dist;
    float density = 0;
    float pressure, distLenSq;
    bool collisionTest = false;

    float searchRadius = solverData_KernelSize.x;
    float step = solverData_KernelSize.x * 0.5f;

    {(settings.RoughSurface ? @"float3 gridJitter = float3(FIXED_RAND3(0x5f7b48e2))
        * (1.0f / 4294967296.0f)
        * step
        * Roughness" : "")};

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
                    distLenSq = dot(dist, dist);
                    if (distLenSq < solverData_KernelSize.y)
                    {{
                        // Sum density
                        density += mass * Poly6Calculate(dist, solverData_KernelFactors.x, solverData_KernelSize.y);
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

    float scalar, bodyRadiusSq;
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
                    distLenSq = dot(dist, dist);
                    if (distLenSq < solverData_KernelSize.y)
                    {{
                        {(settings.BoundaryPressure ? @"// Pressure term
                        scalar = mass
                            * (pressure + neighborDensityPressure.y) / (neighborDensityPressure.x * 2.0f);
                        f = SpikyCalculateGradient(dist, solverData_KernelFactors.y, solverData_KernelSize.x);
                        f *= scalar;

                        force -= f;" : "")}

                        {(settings.BoundaryViscosity ? @"// Viscosity term
                        bodyDist = proxyPosition - CenterOfGravity;
                        bodyRadiusSq = sqrt(dot(bodyDist, bodyDist));
                        neighborVelocity = Velocity + (AngularVelocity / FLUVIO_TAU) * FLUVIO_TAU * bodyRadiusSq;

                        scalar = mass
                            * ViscosityCalculateLaplacian(
                                dist,
                                solverData_KernelFactors.z,
                                solverData_KernelSize.z,
                                solverData_KernelSize.x)
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
