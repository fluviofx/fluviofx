using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace Thinksquirrel.FluvioFX.Editor
{
    [VFXType, Serializable]
    internal struct SolverData
    {
        public Fluid Fluid;
        public Vector Gravity;
        public Vector4 KernelSize;
        public Vector4 KernelFactors;
        public Texture2D Tex;
        public Vector2 TexSize;

        public static SolverData defaultValue = new SolverData
        {
            Fluid = Fluid.defaultValue,
            Gravity = new Vector3(0.0f, -9.81f, 0.0f),
            KernelSize = new Vector4(0.38125f, 0.38125f * 0.38125f, 0.38125f * 0.38125f * 0.38125f, 1.0f),
            KernelFactors = new Vector4(9206.448f, 1554.828f, 43.08061f, 0.0f),
            Tex = null,
            TexSize = Vector2.zero,
        };

        internal IEnumerable<VFXNamedExpression> defaultExpressions
        {
            get
            {
                var fluidExpressions =
                    Fluid.defaultExpressions.Select((expression) =>
                    {
                        expression.name = $"Fluid_{expression.name}";
                        return expression;
                    });

                foreach (var exp in fluidExpressions)
                {
                    yield return exp;
                }

                yield return new VFXNamedExpression(
                    new VFXExpressionCombine(new []
                    {
                        VFXValue.Constant(Gravity.vector.x),
                            VFXValue.Constant(Gravity.vector.y),
                            VFXValue.Constant(Gravity.vector.z)
                    }),
                    nameof(Gravity));

                yield return new VFXNamedExpression(
                    new VFXExpressionCombine(new []
                    {
                        VFXValue.Constant(KernelSize.x),
                            VFXValue.Constant(KernelSize.y),
                            VFXValue.Constant(KernelSize.z),
                            VFXValue.Constant(KernelSize.w),
                    }),
                    nameof(KernelSize));

                yield return new VFXNamedExpression(
                    new VFXExpressionCombine(new []
                    {
                        VFXValue.Constant(KernelFactors.x),
                            VFXValue.Constant(KernelFactors.y),
                            VFXValue.Constant(KernelFactors.z),
                            VFXValue.Constant(KernelFactors.w),
                    }),
                    nameof(KernelFactors));

                // yield return new VFXNamedExpression(VFXValue.Constant(Tex), nameof(Tex));
                // yield return new VFXNamedExpression(
                //     new VFXExpressionCombine(new[]
                //     {
                //         VFXValue.Constant(TexSize.x),
                //         VFXValue.Constant(TexSize.y),
                //     }),
                //     nameof(TexSize));
            }
        }
    }
}
