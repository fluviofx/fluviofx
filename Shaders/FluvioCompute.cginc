#ifndef FLUVIO_COMPUTE_INCLUDED
#define FLUVIO_COMPUTE_INCLUDED
#define FLUVIO_PI 3.14159265359f
#define FLUVIO_TAU FLUVIO_PI * 2.0f

// ---------------------------------------------------------------------------------------
// Space partitioning
// ---------------------------------------------------------------------------------------

uint mod_pos(int a, uint b)
{
    //return (a % b + b) % b;
    float q = (float)a / (float)b;
    uint p = (uint)(q * b);
    return a - p;
}

// This isn't perfect, since we can't control our grid size in memory very well
inline uint GetLocation(int3 location3, uint count)
{
    return mod_pos(
        location3.x + FLUVIO_MAX_GRID_SIZE * (location3.y + FLUVIO_MAX_GRID_SIZE * location3.z),
        count);
}

inline int3 GetLocation3(float3 position, float cellSpace)
{
    return (int3)((position + float3(1000000, 1000000, 1000000)) / cellSpace);
}

// ---------------------------------------------------------------------------------------
// Poly6 SPH Kernel (general)
// ---------------------------------------------------------------------------------------
inline float Poly6Calculate(float diffSq, float poly6Factor)
{
    return poly6Factor * diffSq * diffSq * diffSq;
}
inline float3 Poly6CalculateGradient(float diffSq, float3 dist, float poly6Factor)
{
    float f = -poly6Factor * 6.0f * diffSq * diffSq; // 6.0 to convert 315/64 to 945/32
    return dist * f;
}
inline float Poly6CalculateLaplacian(float lenSq, float diffSq, float poly6Factor)
{
    float f = lenSq - 0.75f * diffSq;
    return poly6Factor * 24.0f * diffSq * diffSq * f; // 24.0 to convert 315/64 to 945/8
}

// ---------------------------------------------------------------------------------------
// Spiky SPH Kernel (pressure)
// ---------------------------------------------------------------------------------------
inline float SpikyCalculate(float diff, float spikyFactor)
{
    return spikyFactor * diff * diff * diff;
}
inline float3 SpikyCalculateGradient(float3 dist, float diff, float len, float spikyFactor)
{
    float f = -spikyFactor * 3.0f * diff * diff / len; // 3.0 to convert 15/1 to 45/1
    return dist * f;
}

// ---------------------------------------------------------------------------------------
// Viscosity SPH Kernel (viscosity)
// ---------------------------------------------------------------------------------------
inline float ViscosityCalculate(
    float lenSq,
    float len,
    float len3,
    float kernelSize3,
    float kernelSizeSq,
    float kernelSize,
    float viscosityFactor)
{
    return
        viscosityFactor *
        (((-len3/(2.0f * kernelSize3)) + (lenSq / kernelSizeSq) + (kernelSize / (2.0f * len))) - 1.0f);
}
inline float3 ViscosityCalculateGradient(
    float3 dist,
    float lenSq,
    float len,
    float len3,
    float kernelSize3,
    float kernelSizeSq,
    float kernelSize,
    float viscosityFactor)
{
    float f = viscosityFactor *
        ((-3.0f * len / (2.0f * kernelSize3)) + (2.0f / kernelSizeSq) + (kernelSize / (2.0f * len3)));
    return dist * f;
}
inline float ViscosityCalculateLaplacian(float diff, float kernelSize3, float viscosityFactor)
{
    return viscosityFactor * (6.0f / kernelSize3) * diff;
}

#endif // FLUVIO_COMPUTE_INCLUDED
