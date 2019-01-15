using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Thinksquirrel.FluvioFX.Editor.Kernels;
using UnityEditor;
using UnityEditor.VFX;
using UnityEditor.VFX.Operator;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Thinksquirrel.FluvioFX.Editor.Operators
{
    [VFXInfo(category = "FluvioFX")]
    class FluidToSolverData : VFXOperator
    {
#pragma warning disable 649
        [VFXSetting(VFXSettingAttribute.VisibleFlags.None), Delayed]
        private Texture2D Tex;

        public class InputProperties
        {
            public Fluid Fluid = Fluid.defaultValue;
            public Vector Gravity = new Vector3(0.0f, -9.81f, 0.0f);
        }

        public class OutputProperties
        {
            public SolverData SolverData;
        }
#pragma warning restore 649

        public override string name => "Fluid to Solver Data";

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            // UpdateSolverTexture(true);

            // KernelSize: x - h, y - h^2, z - h^3, w - simulation scale
            // KernelFactors: x - poly6, y - spiky, z - viscosity, w - unused
            VFXExpression h = inputExpression[0];
            VFXExpression simulationScale = VFXValue.Constant(1.0f);
            var poly6 = new Poly6Kernel(h);
            var spiky = new SpikyKernel(h);
            var viscosity = new ViscosityKernel(h);

            var outputExpression = new List<VFXExpression>();
            outputExpression.AddRange(inputExpression);

            outputExpression.Add(new VFXExpressionCombine(new []
            {
                h,
                h * h,
                h * h * h,
                simulationScale
            }));
            outputExpression.Add(new VFXExpressionCombine(new []
            {
                poly6.GetFactor(), spiky.GetFactor(), viscosity.GetFactor(), VFXValue.Constant(0.0f)
            }));
            outputExpression.Add(new VFXTexture2DValue(Tex ? Tex : null));
            outputExpression.Add(new VFXExpressionCombine(new []
            {
                VFXValue.Constant((float) (Tex? Tex.width : 0)),
                    VFXValue.Constant((float) (Tex ? Tex.height : 0))
            }));

            return outputExpression.ToArray();
        }

        /*
        // TODO: Disable these for now (non-functional)
        private VFXDataParticle GetData()
        {
            var graph = GetParent() as VFXGraph;
            if (graph != null)
            {
                var context = graph.children?.FirstOrDefault((c) => c is VFXBasicUpdate) as VFXBasicUpdate;
                if (context != null)
                {
                    return context.GetData() as VFXDataParticle;
                }
            }
            return null;
        }

        public override void OnEnable()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        protected void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
        }

        private void OnUpdate()
        {
            UpdateSolverTexture(false);
        }
        private void UpdateSolverTexture(bool buildExpression)
        {
            var data = GetData();
            var updateExpressions = false;

            // Set/create solver data texture
            var capacity = data?.capacity ?? 0u;

            // Limitation due to texture sizes
            var totalCapacity = capacity * 1727u;
            const uint maxTexDimension = 16384u;
            const uint maxCapacityPixels = maxTexDimension * maxTexDimension;
            if (totalCapacity > maxCapacityPixels)
            {
                Debug.LogError($"[FluvioFX] VFX particle capacity is larger than allowed by FluvioFX ({capacity} > ~{maxCapacityPixels / 1727u}).");
                totalCapacity = maxCapacityPixels;
            }

            // Get closest pow2 texture that fits, square preferred
            var width = 2;
            var height = 2;
            while (width * height < totalCapacity && (width < maxTexDimension || height < maxTexDimension))
            {
                if (width < maxTexDimension) width *= 2;
                if (height < maxTexDimension) height *= 2;

                // Sanity check
                if (width >= maxTexDimension && height >= maxTexDimension) break;
            }

            try
            {
                if (!Tex)
                {
                    var asset = GetGraph()?.GetPropertyValue("visualEffectResource")?.GetPropertyValue<UnityObject>("asset");
                    if (asset == null)
                    {
                        return;
                    }
                    var assetPath = AssetDatabase.GetAssetPath(asset);
                    var directory = $"{Path.GetDirectoryName(assetPath)}/Textures/{asset.name}/";
                    var fileName = $"{directory}Tex.asset";

                    if (File.Exists(fileName))
                    {
                        if (VFXViewPreference.advancedLogs)
                        {
                            Debug.Log("[FluvioFX] Loading solver texture");
                        }
                        Tex = AssetDatabase.LoadAssetAtPath<Texture2D>(fileName);
                    }
                    else
                    {
                        if (VFXViewPreference.advancedLogs)
                        {
                            Debug.Log("[FluvioFX] Creating solver texture");
                        }
                        Tex = new Texture2D(width, height, TextureFormat.RFloat, false);
                        Tex.filterMode = FilterMode.Point;
                        Tex.wrapMode = TextureWrapMode.Clamp;
                        Tex.anisoLevel = 0;
                        Directory.CreateDirectory(directory);
                        AssetDatabase.CreateAsset(Tex, fileName);
                        if (!buildExpression)
                        {
                            updateExpressions = true;
                        }
                    }
                }

                Tex.hideFlags |= HideFlags.NotEditable;

                if (Tex.width != width
                    || Tex.height != height
                    || Tex.format != TextureFormat.RFloat)
                {
                    if (VFXViewPreference.advancedLogs)
                    {
                        Debug.Log("[FluvioFX] Resizing/reformatting solver texture");
                    }
                    Tex.Resize(width, height, TextureFormat.RFloat, false);
                    if (!buildExpression)
                    {
                        updateExpressions = true;
                    }
                }
                if (Tex.filterMode != FilterMode.Point)
                {
                    if (VFXViewPreference.advancedLogs)
                    {
                        Debug.Log("[FluvioFX] Setting solver texture filter mode");
                    }
                    Tex.filterMode = FilterMode.Point;
                }
                if (Tex.wrapMode != TextureWrapMode.Clamp)
                {
                    if (VFXViewPreference.advancedLogs)
                    {
                        Debug.Log("[FluvioFX] Setting solver texture clamp");
                    }
                    Tex.wrapMode = TextureWrapMode.Clamp;
                }
                if (Tex.anisoLevel != 0)
                {
                    if (VFXViewPreference.advancedLogs)
                    {
                        Debug.Log("[FluvioFX] Setting solver aniso level");
                    }
                    Tex.anisoLevel = 0;
                }

                if (updateExpressions)
                {
                    this.UpdateOutputExpressions();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
                Debug.LogWarning("Unable to make/fetch solver data texture!");
            }
        }
        */
    }
}
