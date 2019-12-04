using UnityEditor.VFX;
using UnityEngine;

namespace FluvioFX.Editor.Kernels
{
    sealed class ViscosityKernel : SmoothingKernel
    {
        public ViscosityKernel(VFXExpression kernelSize) : base(kernelSize)
        { }

        protected override void CalculateFactor()
        {
            _factor = VFXValue.Constant(15.0f) / (VFXValue.Constant(2.0f) * VFXValue.Constant(Mathf.PI) * _kernelSize3);
        }
    }
}
