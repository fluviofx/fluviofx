using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace FluvioFX.Editor
{
    // HACK: Reflection stuffs
    internal static class FluvioFXAttribute
    {
        public static VFXAttribute DensityPressure => new VFXAttribute("densityPressure", VFXValue.Constant<Vector4>());
        public static VFXAttribute Normal => new VFXAttribute("normal", VFXValue.Constant<Vector4>());
        public static VFXAttribute Force => new VFXAttribute("force", VFXValue.Constant<Vector3>());
        public static VFXAttribute PreviousPosition => new VFXAttribute("previousPosition", VFXValue.Constant<Vector3>());

        public static IEnumerable<VFXAttribute> Buckets
        {
            get
            {
                for (var i = 0; i < FluvioFXSettings.kMaxBucketCount; ++i)
                {
                    yield return new VFXAttribute($"buckets_{i}", VFXValue.Constant<uint>());
                }
            }
        }

        public static IEnumerable<VFXAttribute> Neighbors
        {
            get
            {
                for (var i = 0; i < FluvioFXSettings.kMaxNeighborCount; ++i)
                {
                    yield return new VFXAttribute($"neighbors_{i}", VFXValue.Constant<uint>());
                }
            }
        }

        public static VFXAttribute NeighborCount => new VFXAttribute("neighborCount", VFXValue.Constant<uint>());

        public static string GetCodeOffset(this VFXData data, VFXAttribute attribute, string index)
        {
            // HACK: Reflection
            var layout = data.GetFieldValue<StructureOfArrayProvider>("m_layoutAttributeCurrent");
            return layout.GetCodeOffset(attribute, index);
        }

        public static string GetLoadAttributeCode(this VFXData data, VFXAttribute attribute, string name, string index)
        {
            var r = new VFXShaderWriter();
            r.WriteVariable(
                attribute.type,
                name,
                data.GetLoadAttributeCode(attribute, VFXAttributeLocation.Current));
            var result = r.builder.ToString().Replace("(index *", $"({index} *");
            return result;
        }

        public static string GetStoreAttributeCode(this VFXData data, VFXAttribute attribute, string value, string index)
        {
            var storeAttributeCode = data.GetStoreAttributeCode(attribute, value);
            var result = $"{storeAttributeCode.Replace("(index *", $"({index} *")};";
            return result;
        }

        public static string GetLoadArrayCode(
            this VFXData data,
            VFXAttribute array0,
            string name,
            string index,
            string subIndex)
        {
            var array0Code = GetLoadAttributeCode(data, array0, name, "index");
            var result = array0Code
                .Replace("(index *", $"({index} *")
                .Replace(") << ", $" + ({subIndex})) << ");
            return result;
        }

        public static string GetStoreArrayCode(
            this VFXData data,
            VFXAttribute array0,
            string value,
            string index,
            string subIndex)
        {
            var array0Code = GetStoreAttributeCode(data, array0, value, "index");
            var result = array0Code
                .Replace("(index *", $"({index} *")
                .Replace(") << ", $" + ({subIndex})) << ");
            return result;
        }

        public static string GetCompareExchangeCode(
            this VFXData data,
            VFXAttribute array0,
            string index,
            string dest,
            string compareValue,
            string value,
            string original
        )
        {
            var codeOffset = GetCodeOffset(data, array0, index)
                .Replace(") << ", $" + ({dest})) << ");
            return $@"attributeBuffer.InterlockedCompareExchange({codeOffset}, {compareValue}, {value}, {original});";
        }
    }
}
