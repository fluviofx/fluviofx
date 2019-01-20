using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace Thinksquirrel.FluvioFX.Editor
{
    // HACK: Reflection stuffs
    internal static class FluvioFXAttribute
    {
        public static VFXAttribute DensityPressure => new VFXAttribute("densityPressure", VFXValue.Constant<Vector4>());
        public static VFXAttribute Normal => new VFXAttribute("normal", VFXValue.Constant<Vector4>());
        public static VFXAttribute VorticityTurbulence =>
            new VFXAttribute("vorticityTurbulence", VFXValue.Constant<Vector4>());
        public static VFXAttribute Force => new VFXAttribute("force", VFXValue.Constant<Vector3>());
        public static VFXAttribute GridIndex => new VFXAttribute("gridIndex", VFXValue.Constant<uint>());
        public static VFXAttribute NeighborCount => new VFXAttribute("neighborCount", VFXValue.Constant<uint>());

        public static string GetLoadAttributeCode(VFXBlock block, VFXAttribute attribute, string name, string index)
        {
            var r = new VFXShaderWriter();
            var layout = new StructureOfArrayProvider();
            r.WriteVariable(
                attribute.type,
                name,
                block.GetData().GetLoadAttributeCode(attribute, VFXAttributeLocation.Current));
            var result = r.builder.ToString().Replace("(index *", $"({index} *");
            return result;
        }
    }
}
