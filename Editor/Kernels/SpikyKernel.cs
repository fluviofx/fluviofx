using System;
using UnityEditor.VFX;
using UnityEngine;

namespace FluvioFX.Editor.Kernels
{
    sealed class SpikyKernel : SmoothingKernel
    {
        public SpikyKernel(VFXExpression kernelSize) : base(kernelSize)
        { }
        protected override void CalculateFactor()
        {
            // Factor
            _factor = VFXValue.Constant(15.0f) / (VFXValue.Constant(Mathf.PI) * _kernelSize6);
        }
    }
}
