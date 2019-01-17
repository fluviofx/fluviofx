using System.IO;
using UnityEditor;
using UnityEditor.VFX;
using UnityEngine;
using UnityObject = UnityEngine.Object;
using System;
using System.Linq;
using System.Text;
using Thinksquirrel.FluvioFX.Editor;
using Thinksquirrel.FluvioFX.Editor.Blocks;
using UnityEngine.Experimental.VFX;

namespace Thinksquirrel.FluvioFX.Editor
{
    internal static class ShaderPostprocessor
    {
        // private const string solverData = "Texture2D Tex";
        // private const string rwSolverData = "globallycoherent RWTexture2D<uint> Tex";
        // private const string samplerState = "SamplerState samplerFluvioSolverData_a;";
        // private const string sampler = "VFXSampler2D Tex";
        // private const string rwSampler = "RWTexture2D<uint> Tex";
        // private const string getSampler = "GetVFXSampler(FluvioSolverData_a, samplerFluvioSolverData_a)";
        // private const string rwGetSampler = "FluvioSolverData_a";
        private const string arg1 = "(float3 position,";
        private const string arg2 = "(inout float3 position,";
        private const string call1 = "(position,";
        private const string call2 = "( /*inout */position,";
        private const string indexArg1 = "(uint index, float3 position,";
        private const string indexArg2 = "(uint index, inout float3 position,";
        private const string indexCall1 = "(index, /*inout */position,";
        private const string indexCall2 = "(index, position,";

        public static StringBuilder ModifyShader(VFXContext context, StringBuilder source)
        {
            if (context.activeChildrenWithImplicit.Any((b) => typeof(FluvioFXBlock).IsAssignableFrom(b.GetType())))
            {
                source
                    // .Replace(rwSolverData, solverData)
                    // .Replace(rwSampler, sampler)
                    // .Replace(solverData, rwSolverData)
                    // .Replace(samplerState, "")
                    // .Replace(sampler, rwSampler)
                    // .Replace(getSampler, rwGetSampler)
                    .Replace(arg1, indexArg1)
                    .Replace(arg2, indexArg2)
                    .Replace(call1, indexCall1)
                    .Replace(call2, indexCall2);
            }

            return source;
        }
    }
}
