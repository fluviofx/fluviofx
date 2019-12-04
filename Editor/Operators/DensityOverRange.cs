using System.Linq;
using FluvioFX.Editor;
using UnityEditor.VFX;
using UnityEngine;

namespace FluvioFX.Editor.Operators
{
    [VFXInfo(category = "FluvioFX")]
    class DensityOverRange : VFXOperator
    {
#pragma warning disable 649
        public class InputProperties
        {
            public Vector2 Range = new Vector2(499.145f, 9982.9f);
        }
        public class OutputProperties
        {
            public float t = 0;
        }
#pragma warning restore 649

        public override string name
        {
            get
            {
                return "Density Over Range [0..1]";
            }
        }
        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var density = VFXOperatorUtility
                .ExtractComponents(new VFXAttributeExpression(FluvioFXAttribute.DensityPressure))
                .ToArray() [0];

            var range = VFXOperatorUtility.ExtractComponents(inputExpression[0]).ToArray();

            return new VFXExpression[]
            {
                InverseLerp(density, range[0], range[1])
            };
        }

        // TODO: Might be implemented by VFX graph in the future
        private static VFXExpression InverseLerp(VFXExpression x, VFXExpression a, VFXExpression b)
        {
            return (x - a) / (b - a);
        }
    }
}
