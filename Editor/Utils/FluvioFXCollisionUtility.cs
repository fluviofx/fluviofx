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
        public class DensityProperties
        {
            [Min(0.0f)] public float RepulsionForce = 1.0f;

            [Min(FluvioFXSettings.kEpsilon)] public float ParticleMass = 7.625f;

            [Min(FluvioFXSettings.kEpsilon)] public float Density = 1996.5f;

            [Min(FluvioFXSettings.kEpsilon)] public float GasConstant = 0.1f;

            [Min(FluvioFXSettings.kEpsilon)] public float Viscosity = 0.03f;

            public Vector Velocity = Vector3.zero;
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
            IEnumerable<VFXPropertyWithValue> baseProperties,
            bool density)
        {
            var properties = baseProperties;
            if (density)
            {
                properties = properties
                    .Concat(FluvioFXBlock.PropertiesFromType(typeof(DensityProperties)));
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

            yield return new VFXAttributeInfo(VFXAttribute.OldPosition, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(FluvioFXAttribute.DensityPressure, VFXAttributeMode.ReadWrite);
            yield return new VFXAttributeInfo(FluvioFXAttribute.Force, VFXAttributeMode.ReadWrite);
        }

        private static string GetFluidResponseSource(
            CollisionBase block,
            bool fluidForces,
            string roughSurfaceSource)
        {
            var src = "";
            if (block.roughSurface)
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

{(fluidForces ? "force += v * (1.0f / mass) * RepulsionForce * invDt;" : "")}

velocity += v;
oldPosition = position;
age += (LifetimeLoss * lifetime);";

            return src;
        }

        public static string GetCollisionSource(
            CollisionBase block,
            bool fluidForces,
            string baseSource,
            string collisionResponseSource,
            string roughSurfaceSource)
        {
            if (!fluidForces)
            {
                return baseSource.Replace(collisionResponseSource, GetFluidResponseSource(block, false, roughSurfaceSource));
            }

            var densityTestSource = baseSource
                .Replace("float3 nextPos = position + velocity * deltaTime;", "")
                .Replace("nextPos", "proxyPosition")
                .Replace("position", "dummyPosition")
                .Replace(collisionResponseSource, "    collisionTest = true;");

            var split = densityTestSource.Split(new []
            {
                "\r\n",
                "\r",
                "\n"
            }, StringSplitOptions.None);
            densityTestSource = string.Join("\n            ", split);

            return $@"
{{
    float3 offset, proxyPosition, dummyPosition, dist;
    float density = 0;
    float pressure, distLenSq;
    bool collisionTest = false;

    float searchRadius = solverData_KernelSize.x;
    float step = solverData_KernelSize.x * 0.5f;

    float x, y, z;

    // Density calculation
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

                // Snap to grid to prevent jitter
                proxyPosition = round(proxyPosition / step) * step;
                collisionTest = false;
                {densityTestSource}

                if (collisionTest)
                {{
                    // Range check
                    dist = position - proxyPosition;
                    distLenSq = dot(dist, dist);
                    if (distLenSq < solverData_KernelSize.y)
                    {{
                        // Sum density
                        density += ParticleMass * Poly6Calculate(dist, solverData_KernelFactors.x, solverData_KernelSize.y);
                    }}
                }}
            }}
        }}
    }}

    density = max(density, solverData_Fluid_MinimumDensity);
    pressure = solverData_Fluid_GasConstant * (density - solverData_Fluid_Density);

    float2 neighborDensityPressure = float2(Density, GasConstant * Density);
    float scalar;
    float3 f;
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

                // Snap to grid to prevent jitter
                proxyPosition = round(proxyPosition / step) * step;
                collisionTest = false;
                {densityTestSource}

                if (collisionTest)
                {{
                    // Range check
                    dist = position - proxyPosition;
                    distLenSq = dot(dist, dist);
                    if (distLenSq < solverData_KernelSize.y)
                    {{
                        // Pressure term
                        scalar = ParticleMass
                            * (pressure + neighborDensityPressure.y) / (neighborDensityPressure.x * 2.0f);
                        f = SpikyCalculateGradient(dist, solverData_KernelFactors.y, solverData_KernelSize.x);
                        f *= scalar;

                        force -= f;

                        // Viscosity term
                        scalar = ParticleMass
                            * ViscosityCalculateLaplacian(
                                dist,
                                solverData_KernelFactors.z,
                                solverData_KernelSize.z,
                                solverData_KernelSize.x)
                            * (1.0f / neighborDensityPressure.x);

                        f = Velocity - velocity;
                        f *= scalar * Viscosity;

                        force += f;
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

{baseSource.Replace(collisionResponseSource, GetFluidResponseSource(block, fluidForces, roughSurfaceSource))}";
        }
    };
}
