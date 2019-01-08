#ifndef FLUVIO_COMPUTE_INCLUDED
#define FLUVIO_COMPUTE_INCLUDED
#define FLUVIO_PI 3.14159265359f

// Simulation constants - these cannot be changed at this time
#define FLUVIO_EPSILON 9.99999944e-11f // FluvioSettings.kEpsilon
#define FLUVIO_MAX_SQR_VELOCITY_CHANGE 100.0f // FluvioSettings.kMaxSqrVelocityChange
#define FLUVIO_TURBULENCE_CONSTANT 10.0f // FluvioSettings.kTurbulenceConstant

inline float3 clamp_len(float3 v, float len)
{
    return (dot(v, v) > (len * len)) ? (normalize(v) * len) : v;
}

// ---------------------------------------------------------------------------------------
// Index Grid and Neighbor search
// ---------------------------------------------------------------------------------------

// TODO
// The index grid is not functional/very experimental at the moment due to VFX limitations.
// This, it is disabled by default
// #define FLUVIO_INDEX_GRID 1

// Grid constants
#define FLUVIO_MAX_GRID_SIZE 64
#define FLUVIO_MAX_NEIGHBORS 1727

inline uint2 to_2(uint i, uint max_x, uint max_y)
{
    return uint2
    (
        i % max_x,
        i / max_x
    );
}
inline uint mod_pos(uint a, uint b)
{
    return (a % b + b) % b;
}
inline uint3 GetGridIndexVector(float3 position, float cellSpace)
{
    return (uint3)(position / cellSpace);
}
inline uint GetGridIndex(uint3 indexVector)
{
    return mod_pos(indexVector.x + FLUVIO_MAX_GRID_SIZE * (indexVector.y + FLUVIO_MAX_GRID_SIZE * indexVector.z), FLUVIO_MAX_GRID_SIZE * FLUVIO_MAX_GRID_SIZE * FLUVIO_MAX_GRID_SIZE) + 1;
}
inline uint GetGridIndexFromPosition(float3 position, float cellSpace)
{
    return GetGridIndex(GetGridIndexVector(position, cellSpace));
}
inline uint GetNeighborIndex(RWTexture2D<uint> solverData, float2 w, uint i, uint j)
{
    uint2 d = to_2(i * FLUVIO_MAX_NEIGHBORS + j, w.x, w.y);
    return solverData[d.xy];
}
inline void SetNeighborIndex(RWTexture2D<uint> solverData, float2 w, uint i, uint j, uint value)
{
    uint2 d = to_2(i * FLUVIO_MAX_NEIGHBORS + j, w.x, w.y);
    solverData[d.xy] = value;
}

// ---------------------------------------------------------------------------------------
// Poly6 SPH Kernel (general)
// ---------------------------------------------------------------------------------------
inline float Poly6Calculate(float3 dist, float poly6Factor, float kernelSizeSq)
{
    float lenSq = dot(dist, dist);
    float diffSq = kernelSizeSq - lenSq;
    return poly6Factor*diffSq*diffSq*diffSq;
}
inline float3 Poly6CalculateGradient(float3 dist, float poly6Factor, float kernelSizeSq)
{
    float lenSq = dot(dist, dist);
    float diffSq = kernelSizeSq - lenSq;
    float f = -poly6Factor*6.0f*diffSq*diffSq; // 6.0 to convert 315/64 to 945/32
    return dist*f;
}
inline float Poly6CalculateLaplacian(float3 dist, float poly6Factor, float kernelSizeSq)
{
    float lenSq = dot(dist, dist);
    float diffSq = kernelSizeSq - lenSq;
    float f = lenSq - (0.75f*diffSq);
    return poly6Factor*24.0f*diffSq*diffSq*f; // 24.0 to convert 315/64 to 945/8
}

// ---------------------------------------------------------------------------------------
// Spiky SPH Kernel (pressure)
// ---------------------------------------------------------------------------------------
inline float SpikyCalculate(float3 dist, float spikyFactor, float kernelSize)
{
    float lenSq = dot(dist, dist);
    float f = kernelSize - sqrt(lenSq);
    return spikyFactor*f*f*f;
}
inline float3 SpikyCalculateGradient(float3 dist, float spikyFactor, float kernelSize)
{
    float lenSq = dot(dist, dist);
    float len = sqrt(lenSq);
    float f = -spikyFactor*3.0f*(kernelSize - len)*(kernelSize - len)/len; // 3.0 to convert 15/1 to 45/1
    return dist*f;
}

// ---------------------------------------------------------------------------------------
// Viscosity SPH Kernel (viscosity)
// ---------------------------------------------------------------------------------------
inline float ViscosityCalculate(float3 dist, float viscosityFactor, float kernelSize3, float kernelSizeSq, float kernelSize)
{
    float lenSq = dot(dist, dist);
    float len = sqrt(lenSq);
    float len3 = len*len*len;
    return viscosityFactor*(((-len3/(2.0f*kernelSize3)) + (lenSq/kernelSizeSq) + (kernelSize/(2.0f*len))) - 1.0f);
}
inline float3 ViscosityCalculateGradient(float3 dist, float viscosityFactor, float kernelSize3, float kernelSizeSq, float kernelSize)
{
    float lenSq = dot(dist, dist);
    float len = sqrt(lenSq);
    float len3 = len*len*len;
    float f = viscosityFactor*((-3.0f*len/(2.0f*kernelSize3)) + (2.0f/kernelSizeSq) + (kernelSize/(2.0f*len3)));
    return dist*f;
}
inline float ViscosityCalculateLaplacian(float3 dist, float viscosityFactor, float kernelSize3, float kernelSize)
{
    float lenSq = dot(dist, dist);
    float len = sqrt(lenSq);
    return viscosityFactor*(6.0f/kernelSize3)*(kernelSize - len);
}

#endif // FLUVIO_COMPUTE_INCLUDED
