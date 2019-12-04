using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace FluvioFX.Editor.Blocks
{
    abstract class FluvioFXBlock : VFXBlock
    {
        static FluvioFXBlock()
        {
            VFXCodeGenerator.OnGenerateCode += ShaderPostprocessor.ModifyShader;
        }

        public override VFXContextType compatibleContexts
        {
            get
            {
                return VFXContextType.Update;
            }
        }

        public override VFXDataType compatibleData
        {
            get
            {
                return VFXDataType.Particle;
            }
        }

        public override IEnumerable<string> includes
        {
            get
            {
                yield return $"{PackageInfo.assetPackagePath}/Shaders/FluvioCompute.cginc";
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                if (solverDataParameters != SolverDataParameters.None)
                {
                    return InitializeSolver.GetExpressions(this);
                }

                return Enumerable.Empty<VFXNamedExpression>();
            }
        }

        public virtual string replacementKernel => null;

        protected internal virtual SolverDataParameters solverDataParameters => SolverDataParameters.All;

        protected bool hasLifetime => GetData().IsCurrentAttributeWritten(VFXAttribute.Alive);

        protected string Load(VFXAttribute attribute, string name, string index)
        {
            return GetData().GetLoadAttributeCode(attribute, name, index);
        }

        protected string Store(VFXAttribute attribute, string value, string index)
        {
            return GetData().GetStoreAttributeCode(attribute, value, index);
        }

        protected string LoadNeighbor(string name, string index, string neighborIndex)
        {
            return GetData().GetLoadArrayCode(FluvioFXAttribute.Neighbors.First(), name, index, neighborIndex);
        }

        protected string StoreNeighbor(string value, string index, string neighborIndex)
        {
            return GetData().GetStoreArrayCode(FluvioFXAttribute.Neighbors.First(), value, index, neighborIndex);
        }

        protected string LoadBucket(string name, string index, string bucketIndex)
        {
            return GetData().GetLoadArrayCode(FluvioFXAttribute.Buckets.First(), name, index, bucketIndex);
        }

        protected string StoreBucket(string value, string index, string bucketIndex)
        {
            return GetData().GetStoreArrayCode(FluvioFXAttribute.Buckets.First(), value, index, bucketIndex);
        }

        protected string GetMethodName()
        {
            var settings = GetSettings(true).ToArray();
            if (settings.Length > 0)
            {
                int hash = 0;
                foreach (var setting in settings)
                {
                    var value = setting.value;
                    hash = (hash * 397) ^ value.GetHashCode();
                }
                return string.Format("{0}_{1}", GetType().Name, hash.ToString("X"));
            }
            else
            {
                return GetType().Name;
            }
        }

        // HACK: This is very implementation-specific :(
        protected string CallFunction()
        {
            var data = GetData();
            var context = (VFXContext) GetParent();
            var expressionGraph = GetGraph()
                .GetPropertyValue<VFXGraphCompiledData>("compiledData")
                .GetFieldValue<VFXExpressionGraph>("m_ExpressionGraph");

            var gpuMapper = expressionGraph.BuildGPUMapper(context);
            var uniformMapper = new VFXUniformMapper(gpuMapper, context.doesGenerateShader);
            var blockIndex = context.activeChildrenWithImplicit
                .Select((value, index) => new
                {
                    value,
                    index
                })
                .Where(pair => pair.value == this)
                .Select(pair => pair.index + 1)
                .FirstOrDefault() - 1;

            var writer = new VFXShaderWriter();
            var expressionToName = data
                .GetAttributes()
                .ToDictionary(o =>
                    new VFXAttributeExpression(o.attrib) as VFXExpression,
                    o => (new VFXAttributeExpression(o.attrib)).GetCodeString(null));

            expressionToName = expressionToName
                .Union(uniformMapper.expressionToCode).ToDictionary(s => s.Key, s => s.Value);

            var blockParameters = mergedAttributes.Select(o => new VFXShaderWriter.FunctionParameter
            {
                name = o.attrib.name, expression = new VFXAttributeExpression(o.attrib) as VFXExpression, mode = o.mode
            }).ToList();

            foreach (var blockParameter in parameters)
            {
                var expReduced = gpuMapper.FromNameAndId(blockParameter.name, blockIndex);
                if (VFXExpression.IsTypeValidOnGPU(expReduced.valueType))
                {
                    blockParameters.Add(new VFXShaderWriter.FunctionParameter
                    {
                        name = blockParameter.name, expression = expReduced, mode = VFXAttributeMode.None
                    });
                }
            }

            var scoped = blockParameters.Any(o => !expressionToName.ContainsKey(o.expression));
            if (scoped)
            {
                expressionToName = new Dictionary<VFXExpression, string>(expressionToName);
                writer.EnterScope();
                foreach (var exp in blockParameters.Select(o => o.expression))
                {
                    if (expressionToName.ContainsKey(exp))
                    {
                        continue;
                    }
                    writer.WriteVariable(exp, expressionToName);
                }
            }

            writer.WriteCallFunction(GetMethodName(), blockParameters, gpuMapper, expressionToName);

            if (scoped)
            {
                writer.ExitScope();
            }

            return writer.builder.ToString();
        }

        protected string CompareExchangeBucket(
            string index,
            string bucketIndex,
            string compareValue,
            string value,
            string original)
        {
            return GetData()
                .GetCompareExchangeCode(
                    FluvioFXAttribute.Buckets.First(),
                    index,
                    bucketIndex,
                    compareValue,
                    value,
                    original);
        }

        internal new static IEnumerable<VFXPropertyWithValue> PropertiesFromType(Type type)
        {
            if (type == null)
            {
                return Enumerable.Empty<VFXPropertyWithValue>();
            }

            var instance = System.Activator.CreateInstance(type);
            return type.GetFields()
                .Where(f => !f.IsStatic)
                .Select(f =>
                {
                    var p = new VFXPropertyWithValue();
                    p.property = new VFXProperty(f);
                    p.value = f.GetValue(instance);
                    return p;
                });
        }

        internal static string CheckAlive(VFXData data)
        {
            var hasLifetime = data?.IsCurrentAttributeWritten(VFXAttribute.Alive) == true;
            if (hasLifetime)
            {
                return $@"{data.GetLoadAttributeCode(VFXAttribute.Alive, "isAlive", "index")}
if (!isAlive) return;
";
            }

            return "";
        }

        protected string CheckAlive()
        {
            return CheckAlive(GetData());
        }
    }
}
