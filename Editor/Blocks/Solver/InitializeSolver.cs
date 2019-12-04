using System;
using System.Collections.Generic;
using System.Linq;
using FluvioFX.Editor;
using FluvioFX.Editor.Kernels;
using UnityEditor.VFX;
using UnityEngine;

namespace FluvioFX.Editor.Blocks
{
    [VFXInfo(category = "FluvioFX/Solver")]
    class InitializeSolver : FluvioFXBlock
    {
        [VFXSetting] public bool AutomaticMass = true;
        [VFXSetting] public bool AutomaticSize = true;

        public override string name
        {
            get
            {
                return "Initialize Solver";
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                return PropertiesFromType(typeof(InputProperties));
            }
        }

        internal static IEnumerable<VFXNamedExpression> GetExpressions(
            VFXBlock block,
            SolverDataParameters? solverDataParamsOverride = null
        )
        {
            List<VFXNamedExpression> fluid = null;
            VFXExpression h = null;
            VFXExpression mass = null;
            VFXExpression density = null;
            VFXExpression gravity = null;

            var initializeBlock = block as InitializeSolver;

            if (initializeBlock == null)
            {
                var context = block.GetParent();
                var data = context.GetData();
                initializeBlock = data.owners
                    .SelectMany((c) => c.activeChildrenWithImplicit, (_, b) => b)
                    .FirstOrDefault((b) => b.GetType() == typeof(InitializeSolver)) as InitializeSolver;
            }

            if (initializeBlock)
            {
                fluid = new List<VFXNamedExpression>();
                foreach (var expression in GetExpressionsFromSlots(initializeBlock))
                {
                    if (expression.name == $"{nameof(InputProperties.Fluid)}_{nameof(Fluid.SmoothingDistance)}")
                    {
                        // We use solverData_KernelSize instead
                        h = expression.exp;
                        continue;
                    }
                    else if (expression.name == $"{nameof(InputProperties.Fluid)}_{nameof(Fluid.ParticleMass)}")
                    {
                        mass = expression.exp;
                    }
                    else if (expression.name == $"{nameof(InputProperties.Fluid)}_{nameof(Fluid.Density)}")
                    {
                        density = expression.exp;
                    }
                    else if (expression.name == $"{nameof(InputProperties.Gravity)}")
                    {
                        // We'll add solverData_Gravity in separately
                        gravity = expression.exp;
                        continue;
                    }

                    fluid.Add(expression);
                }
            }

            var fluvioFxBlock = block as FluvioFXBlock;

            var expressions = GetExpressionsImpl(
                initializeBlock,
                solverDataParamsOverride ?? fluvioFxBlock?.solverDataParameters ?? SolverDataParameters.All,
                fluid,
                h,
                mass,
                density,
                gravity);

            foreach (var expression in expressions)
            {
                yield return expression;
            }
        }

        private static IEnumerable<VFXNamedExpression> GetExpressionsImpl(
            InitializeSolver initializeBlock,
            SolverDataParameters solverDataParams,
            IEnumerable<VFXNamedExpression> fluid,
            VFXExpression h,
            VFXExpression mass,
            VFXExpression density,
            VFXExpression gravity)
        {
            // Fluid
            if (fluid == null || h == null || mass == null || density == null)
            {
                var defaultFluid = Fluid.defaultValue;
                h = VFXValue.Constant(defaultFluid.SmoothingDistance);

                if (solverDataParams.HasFlag(SolverDataParameters.Fluid_ParticleMass))
                {
                    yield return new VFXNamedExpression(
                        GetParticleMass(
                            initializeBlock,
                            h,
                            VFXValue.Constant(defaultFluid.ParticleMass),
                            VFXValue.Constant(defaultFluid.Density)
                        ), "solverData_Fluid_ParticleMass");
                }
                if (solverDataParams.HasFlag(SolverDataParameters.Fluid_Density))
                {
                    yield return new VFXNamedExpression(
                        VFXValue.Constant(defaultFluid.Density), "solverData_Fluid_Density");
                }
                if (solverDataParams.HasFlag(SolverDataParameters.Fluid_MinimumDensity))
                {
                    yield return new VFXNamedExpression(
                        VFXValue.Constant(defaultFluid.MinimumDensity), "solverData_Fluid_MinimumDensity");
                }
                if (solverDataParams.HasFlag(SolverDataParameters.Fluid_GasConstant))
                {
                    yield return new VFXNamedExpression(
                        VFXValue.Constant(defaultFluid.GasConstant), "solverData_Fluid_GasConstant");
                }
                if (solverDataParams.HasFlag(SolverDataParameters.Fluid_Viscosity))
                {
                    yield return new VFXNamedExpression(
                        VFXValue.Constant(defaultFluid.Viscosity), "solverData_Fluid_Viscosity");
                }
                if (solverDataParams.HasFlag(SolverDataParameters.Fluid_SurfaceTension))
                {
                    yield return new VFXNamedExpression(
                        VFXValue.Constant(defaultFluid.SurfaceTension), "solverData_Fluid_SurfaceTension");
                }
                if (solverDataParams.HasFlag(SolverDataParameters.Fluid_BuoyancyCoefficient))
                {
                    yield return new VFXNamedExpression(
                        VFXValue.Constant(defaultFluid.BuoyancyCoefficient), "solverData_Fluid_BuoyancyCoefficient");
                }
            }
            else
            {
                foreach (var expression in fluid)
                {
                    if (Enum.TryParse(expression.name, out SolverDataParameters solverDataParameter) &&
                        solverDataParams.HasFlag(solverDataParameter))
                    {
                        if (solverDataParameter == SolverDataParameters.Fluid_ParticleMass)
                        {
                            yield return new VFXNamedExpression(
                                GetParticleMass(initializeBlock, h, mass, density), "solverData_Fluid_ParticleMass");
                            continue;
                        }

                        yield return new VFXNamedExpression(expression.exp, $"solverData_{expression.name}");
                    }
                };
            }

            // KernelSize: x - h, y - h^2, z - h^3, w - h / 2
            if (solverDataParams.HasFlag(SolverDataParameters.KernelSize))
            {
                yield return new VFXNamedExpression(new VFXExpressionCombine(new []
                    {
                        h,
                        h * h,
                        h * h * h,
                        h * VFXValue.Constant(0.5f),
                    }),
                    "solverData_KernelSize");
            }

            // KernelFactors: x - poly6, y - spiky, z - viscosity
            if (solverDataParams.HasFlag(SolverDataParameters.KernelFactors))
            {
                var poly6 = new Poly6Kernel(h);
                var spiky = new SpikyKernel(h);
                var viscosity = new ViscosityKernel(h);

                yield return new VFXNamedExpression(new VFXExpressionCombine(new []
                    {
                        poly6.GetFactor(), spiky.GetFactor(), viscosity.GetFactor()
                    }),
                    "solverData_KernelFactors");
            }

            // Gravity
            if (solverDataParams.HasFlag(SolverDataParameters.Gravity))
            {
                if (gravity == null)
                {
                    gravity = new VFXExpressionCombine(new []
                    {
                        VFXValue.Constant(0.0f), VFXValue.Constant(-9.81f), VFXValue.Constant(0.0f),
                    });
                }
                yield return new VFXNamedExpression(gravity, "solverData_Gravity");
            }
        }

        private static VFXExpression GetParticleMass(
            InitializeSolver initializeBlock,
            VFXExpression h,
            VFXExpression mass,
            VFXExpression density)
        {
            if (initializeBlock?.AutomaticMass ?? true)
            {
                var hCubed = h * h * h;
                var hParticleFactor = VFXValue.Constant(FluvioFXSettings.kAutoParticleSizeFactor);
                return
                VFXValue.Constant(4.0f / 3.0f) *
                    VFXValue.Constant(Mathf.PI) *
                    hCubed *
                    hParticleFactor *
                    density;
            }
            else
            {
                return mass;
            }
        }
#pragma warning disable 649
        public class InputProperties
        {
            public Fluid Fluid;
            public Vector Gravity;
        }
#pragma warning restore 649

        private bool hasPreviousPosition => GetData().IsCurrentAttributeWritten(FluvioFXAttribute.PreviousPosition);

        protected internal override SolverDataParameters solverDataParameters =>
            SolverDataParameters.Fluid_ParticleMass |
            SolverDataParameters.KernelSize;

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(FluvioFXAttribute.Force, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Write);

                // For the define
                if (hasLifetime)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.Lifetime, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.ReadWrite);
                }

                foreach (var attr in FluvioFXAttribute.Buckets)
                {
                    yield return new VFXAttributeInfo(attr, VFXAttributeMode.ReadWrite);
                }
            }
        }

        public override string source
        {
            get
            {
                var str = $@"// Clear forces
force = 0;

// Set mass
mass = solverData_Fluid_ParticleMass;";
                if (AutomaticSize)
                {
                    str += $@"

// Set size
size = solverData_KernelSize.x * FLUVIO_AUTO_PARTICLE_SIZE_FACTOR;";
                }

                if (hasLifetime)
                {
                    str += $@"

// Reap particles
if (age > lifetime)
{{
    alive = false;
}}";
                }

                return str;
            }
        }

        // Custom kernel here since we need to access dead particle indices,
        // But still cull particles
        public override string replacementKernel
        {
            get
            {
                var str = $@"
    uint id = groupThreadId.x + groupId.x * NB_THREADS_PER_GROUP + groupId.y * dispatchWidth * NB_THREADS_PER_GROUP;
	uint index = id;
	if (id < nbMax)
	{{
        // Clear buckets
        for (uint bucketIndex = 0; bucketIndex < FLUVIO_MAX_BUCKET_COUNT; ++bucketIndex)
        {{
            {StoreBucket("0", "index", "bucketIndex")}
        }}
";
                if (hasLifetime)
                {
                    str += $@"
        {Load(VFXAttribute.Age, "age", "index")}
        {Load(VFXAttribute.Lifetime, "lifetime", "index")}
        {Load(VFXAttribute.Alive, "alive", "index")}

        if (alive)";
                }
                str += $@"
        {{
            {Load(VFXAttribute.Mass, "mass", "index")}
            {Load(VFXAttribute.Size, "size", "index")}
            {Load(FluvioFXAttribute.Force, "force", "index")}
";
                if (hasPreviousPosition)
                {
                    str += $@"
            // Set previous position
            {Load(VFXAttribute.Position, "position", "index")}
            {Store(FluvioFXAttribute.PreviousPosition, "position", "index")}
";
                }
                str += $@"
            {CallFunction()}
            {Store(FluvioFXAttribute.Force, "force", "index")}
            {Store(VFXAttribute.Mass, "mass", "index")}
            {Store(VFXAttribute.Size, "size", "index")}
";
                str += $@"
#if VFX_HAS_INDIRECT_DRAW";
                if (hasLifetime)
                {
                    str += $@"
            if (alive)";
                }
                str += $@"
            {{
                uint indirectIndex = indirectBuffer.IncrementCounter();
			    indirectBuffer[indirectIndex] = index;
            }}";
                if (hasLifetime)
                {
                    str += $@"
            else
            {{
                {Store(VFXAttribute.Alive, "false", "index")}
                uint deadIndex = deadListOut.IncrementCounter();
                deadListOut[deadIndex] = index;
            }}";
                }
                str += $@"
#endif
        }}
    }}";
                return str;
            }
        }
    }
}
