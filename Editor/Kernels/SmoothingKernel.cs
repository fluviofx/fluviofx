using System;
using UnityEditor.VFX;
using UnityEngine;

namespace FluvioFX.Editor.Kernels
{
    abstract class SmoothingKernel
    {
        protected VFXExpression _factor;
        protected VFXExpression _kernelSize;
        protected VFXExpression _kernelSizeSq;
        protected VFXExpression _kernelSize3;
        protected VFXExpression _kernelSize6;
        protected VFXExpression _kernelSize9;

        private void Initialize(VFXExpression kernelSize)
        {
            if (kernelSize == this._kernelSize)
                return;

            this._kernelSize = kernelSize;
            _kernelSizeSq = kernelSize * kernelSize;
            _kernelSize3 = kernelSize * kernelSize * kernelSize;
            _kernelSize6 = kernelSize * kernelSize * kernelSize * kernelSize * kernelSize * kernelSize;
            _kernelSize9 =
                kernelSize *
                kernelSize *
                kernelSize *
                kernelSize *
                kernelSize *
                kernelSize *
                kernelSize *
                kernelSize *
                kernelSize;

            CalculateFactor();
        }
        public VFXExpression GetFactor()
        {
            return _factor;
        }

        protected SmoothingKernel(VFXExpression kernelSize)
        {
            Initialize(kernelSize);
        }

        protected abstract void CalculateFactor();
    }
}
