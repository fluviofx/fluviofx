using System;
using System.Collections.Generic;
using System.Linq;
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
            [Min(0.0f)]
            public float ParticleMass = 0.0f;

            [Range(1.0f, 16.0f)]
            public float Resolution = 4.0f;
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
            VFXBlock block,
            IEnumerable<VFXNamedExpression> baseParameters)
        {
            var expressions = baseParameters;
            expressions = expressions.Concat(FluvioFXBlock.GetSolverDataExpressions(block));

            foreach (var expression in expressions)
            {
                yield return expression;
            };
        }

        public static IEnumerable<VFXAttributeInfo> GetAttributes(IEnumerable<VFXAttributeInfo> baseAttributes)
        {
            foreach (var attribute in baseAttributes)
            {
                yield return attribute;
            }

            yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(FluvioFXAttribute.DensityPressure, VFXAttributeMode.ReadWrite);
        }

        public static string GetCollisionSource(string originalSource, string collisionResponseSource, bool density)
        {
            if (!density)
            {
                return originalSource;
            }

            var densityTestSource = originalSource
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
    uint colliderIndex;
    float pressure, distLenSq;
    bool collisionTest = false;

    float searchRadius = 2 * solverData_KernelSize.x;
    float step = solverData_KernelSize.x * (1.0f / Resolution);

    // Density calculation
    for(float x = -searchRadius; x < searchRadius; x += step)
    {{
        for(float y = -searchRadius; y < searchRadius; y += step)
        {{
            for(float z = -searchRadius; z < searchRadius; z += step)
            {{
                // Get proxy particle position
                offset = float3(x, y, z);
                proxyPosition = position + offset;

                // Snap to grid to prevent jitter
                proxyPosition = round(proxyPosition / step) * step;
                {densityTestSource}

                if (collisionTest)
                {{
                    // Range check
                    dist = position - proxyPosition;
                    distLenSq = dot(dist, dist);
                    if (distLenSq < solverData_KernelSize.y && distLenSq > FLUVIO_EPSILON)
                    {{
                        // Sum density
                        density += ParticleMass * Poly6Calculate(dist, solverData_KernelFactors.x, solverData_KernelSize.y);
                    }}
                }}
            }}
        }}
    }}

    // Write to density/pressure
    density = max(densityPressure.x + density, solverData_Fluid_MinimumDensity);
    pressure = solverData_Fluid_GasConstant * (density - solverData_Fluid_Density);
    densityPressure = float4(density, densityPressure.y, pressure, densityPressure.w);
}}
{originalSource}";
        }
    };
}
