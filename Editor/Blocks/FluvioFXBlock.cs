using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace Thinksquirrel.FluvioFX.Editor.Blocks
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
                return VFXContextType.kUpdate;
            }
        }

        public override VFXDataType compatibleData
        {
            get
            {
                return VFXDataType.kParticle;
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
                foreach (var p in GetExpressionsFromSlots(this))
                {
                    yield return p;
                }

                if (findSolverData)
                {
                    foreach (var exp in GetSolverDataExpressions(this))
                    {
                        yield return exp;
                    }
                }
            }
        }

        protected virtual bool solverDataProperty => false;
        protected virtual bool findSolverData => true;

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                if (solverDataProperty)
                {
                    return PropertiesFromType(typeof(SolverDataProperties));
                }

                return Enumerable.Empty<VFXPropertyWithValue>();
            }
        }

        protected bool hasLifetime => GetData().IsCurrentAttributeWritten(VFXAttribute.Alive);

        internal static IEnumerable<VFXNamedExpression> GetSolverDataExpressions(VFXBlock block)
        {
            var context = block.GetParent();
            var data = context.GetData();
            var initializeBlock = data.owners
                .SelectMany((c) => c.activeChildrenWithImplicit, (_, b) => b)
                .FirstOrDefault((b) => b.GetType() == typeof(InitializeSolver));

            if (initializeBlock)
            {
                return initializeBlock.parameters.Where((exp) => !exp.name.Contains("_Tex"));
            }
            else
            {
                var solverData = SolverData.defaultValue;
                return solverData.defaultExpressions.Select((expression) =>
                {
                    expression.name = $"solverData_{expression.name}";
                    return expression;
                });
            }
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

#pragma warning disable 649
        public class SolverDataProperties
        {
            public SolverData solverData;
        }
#pragma warning restore 649
    }
}
