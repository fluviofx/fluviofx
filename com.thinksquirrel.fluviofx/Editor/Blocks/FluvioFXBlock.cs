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
            }
        }
    }
}
