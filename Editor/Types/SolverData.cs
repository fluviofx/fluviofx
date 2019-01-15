using System;
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
    }
}
