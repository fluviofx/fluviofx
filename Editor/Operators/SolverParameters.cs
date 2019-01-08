using System;
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
    class SolverParameters : VFXOperator
    {
#pragma warning disable 649
        [VFXSetting(VFXSettingAttribute.VisibleFlags.None), Delayed]
        private Texture2D FluvioSolverData;

        public class InputProperties
        {
            [Tooltip("Controls the smoothing distance of fluid particles. This changes the overall simulation resolution and greatly affects all other properties.")]
            public float SmoothingDistance = 0.38125f;
            public float SimulationScale = 1.0f;
        }

        public class OutputProperties
        {
            public Vector4 KernelSize = Vector3.zero;
            public Vector4 KernelFactors = Vector3.zero;
            public Texture2D FluvioSolverData;
            public Vector2 FluvioSolverDataSize;
        }
#pragma warning restore 649

        override public string name
        {
            get
            {
                return "Solver Parameters";
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            // UpdateSolverTexture(true);

            // KernelSize: x - h, y - h^2, z - h^3, w - simulation scale
            // KernelFactors: x - poly6, y - spiky, z - viscosity, w - unused
            VFXExpression h = inputExpression[0];
            VFXExpression simulationScale = inputExpression[1];
            var poly6 = new Poly6Kernel(h);
            var spiky = new SpikyKernel(h);
            var viscosity = new ViscosityKernel(h);

            return new VFXExpression[]
            {
                new VFXExpressionCombine(new []
                    {
                        h,
                        h * h,
                        h * h * h,
                        simulationScale
                    }),
                    new VFXExpressionCombine(new []
                    {
                        poly6.GetFactor(), spiky.GetFactor(), viscosity.GetFactor(), VFXValue.Constant(0.0f)
                    }),
                    new VFXTexture2DValue(FluvioSolverData ? FluvioSolverData : null),
                    new VFXExpressionCombine(new []
                    {
                        VFXValue.Constant((float) (FluvioSolverData? FluvioSolverData.width : 0)), VFXValue.Constant((float) (FluvioSolverData ? FluvioSolverData.height : 0))
                    }),
            };
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
                if (!FluvioSolverData)
                {
                    var asset = GetGraph()?.GetPropertyValue("visualEffectResource")?.GetPropertyValue<UnityObject>("asset");
                    if (asset == null)
                    {
                        return;
                    }
                    var assetPath = AssetDatabase.GetAssetPath(asset);
                    var directory = $"{Path.GetDirectoryName(assetPath)}/Textures/{asset.name}/";
                    var fileName = $"{directory}FluvioSolverData.asset";

                    if (File.Exists(fileName))
                    {
                        if (VFXViewPreference.advancedLogs)
                        {
                            Debug.Log("[FluvioFX] Loading solver texture");
                        }
                        FluvioSolverData = AssetDatabase.LoadAssetAtPath<Texture2D>(fileName);
                    }
                    else
                    {
                        if (VFXViewPreference.advancedLogs)
                        {
                            Debug.Log("[FluvioFX] Creating solver texture");
                        }
                        FluvioSolverData = new Texture2D(width, height, TextureFormat.RFloat, false);
                        FluvioSolverData.filterMode = FilterMode.Point;
                        FluvioSolverData.wrapMode = TextureWrapMode.Clamp;
                        FluvioSolverData.anisoLevel = 0;
                        Directory.CreateDirectory(directory);
                        AssetDatabase.CreateAsset(FluvioSolverData, fileName);
                        if (!buildExpression)
                        {
                            updateExpressions = true;
                        }
                    }
                }

                FluvioSolverData.hideFlags |= HideFlags.NotEditable;

                if (FluvioSolverData.width != width
                    || FluvioSolverData.height != height
                    || FluvioSolverData.format != TextureFormat.RFloat)
                {
                    if (VFXViewPreference.advancedLogs)
                    {
                        Debug.Log("[FluvioFX] Resizing/reformatting solver texture");
                    }
                    FluvioSolverData.Resize(width, height, TextureFormat.RFloat, false);
                    if (!buildExpression)
                    {
                        updateExpressions = true;
                    }
                }
                if (FluvioSolverData.filterMode != FilterMode.Point)
                {
                    if (VFXViewPreference.advancedLogs)
                    {
                        Debug.Log("[FluvioFX] Setting solver texture filter mode");
                    }
                    FluvioSolverData.filterMode = FilterMode.Point;
                }
                if (FluvioSolverData.wrapMode != TextureWrapMode.Clamp)
                {
                    if (VFXViewPreference.advancedLogs)
                    {
                        Debug.Log("[FluvioFX] Setting solver texture clamp");
                    }
                    FluvioSolverData.wrapMode = TextureWrapMode.Clamp;
                }
                if (FluvioSolverData.anisoLevel != 0)
                {
                    if (VFXViewPreference.advancedLogs)
                    {
                        Debug.Log("[FluvioFX] Setting solver aniso level");
                    }
                    FluvioSolverData.anisoLevel = 0;
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
