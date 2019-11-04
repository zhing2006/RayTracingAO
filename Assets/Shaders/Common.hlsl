#include "UnityRaytracingMeshUtils.cginc"

#define M_PI            3.14159265359f
#define M_TWO_PI        6.28318530718f
#define M_FOUR_PI       12.56637061436f
#define M_INV_PI        0.31830988618f
#define M_INV_TWO_PI    0.15915494309f
#define M_INV_FOUR_PI   0.07957747155f
#define M_HALF_PI       1.57079632679f
#define M_INV_HALF_PI   0.636619772367f

#define CBUFFER_START(name) cbuffer name {
#define CBUFFER_END };

// Macro that interpolate any attribute using barycentric coordinates
#define INTERPOLATE_RAYTRACING_ATTRIBUTE(A0, A1, A2, BARYCENTRIC_COORDINATES) (A0 * BARYCENTRIC_COORDINATES.x + A1 * BARYCENTRIC_COORDINATES.y + A2 * BARYCENTRIC_COORDINATES.z)

#define SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod) textureName.SampleLevel(samplerName, coord2, lod)
#define TEXTURE2D(textureName) Texture2D textureName
#define SAMPLER(samplerName) SamplerState samplerName

#define LOAD_TEXTURE2D(textureName, unCoord2)          textureName.Load(int3(unCoord2, 0))
#define LOAD_TEXTURE2D_LOD(textureName, unCoord2, lod) textureName.Load(int3(unCoord2, lod))

#define UNITY_RAW_FAR_CLIP_VALUE (0.0)

#define SIGN_IGNORE_NEGATIVE_NUMVERS 1

inline float sqr(float value)
{
  return value * value;
}

inline float gaussian(float radius, float sigma)
{
  return exp(-sqr(radius / sigma));
}

// Inserts the bits indicated by 'mask' from 'src' into 'dst'.
inline uint BitFieldInsert(uint mask, uint src, uint dst)
{
  return (src & mask) | (dst & ~mask);
}

float CopySign(float x, float s)
{
#ifdef SIGN_IGNORE_NEGATIVE_NUMVERS
  return (s >= 0) ? abs(x) : -abs(x);
#else
  uint negZero = 0x80000000u;
  uint signBit = negZero & asuint(s);
  return asfloat(BitFieldInsert(negZero, signBit, asuint(x)));
#endif
}

// Returns -1 for negative numbers and 1 for positive numbers.
// 0 can be handled in 2 different ways.
// The IEEE floating point standard defines 0 as signed: +0 and -0.
// However, mathematics typically treats 0 as unsigned.
// Therefore, we treat -0 as +0 by default: FastSign(+0) = FastSign(-0) = 1.
// If (ignoreNegZero = false), FastSign(-0, false) = -1.
// Note that the sign() function in HLSL implements signum, which returns 0 for 0.
inline float sgn(float s)
{
  return CopySign(1.0, s);
}

CBUFFER_START(CameraBuffer)
float4x4 _CameraViewProj;
float4x4 _InvCameraViewProj;
float3 _WorldSpaceCameraPos;
float _CameraFarDistance;
float4 _ZBufferParams;
CBUFFER_END

struct RayIntersection
{
  int remainingDepth;
  uint4 PRNGStates;
  float4 color;
  float hitT;
};

struct RayIntersectionAO
{
  float ao;
};

struct AttributeData
{
  float2 barycentrics;
};

// Z buffer to linear 0..1 depth
inline float Linear01Depth(float z)
{
  return 1.0f / (_ZBufferParams.x * z + _ZBufferParams.y);
}

// Z buffer to linear depth
inline float LinearEyeDepth(float z)
{
  return 1.0f / (_ZBufferParams.z * z + _ZBufferParams.w);
}

// Assumes that (0 <= x <= Pi).
float SinFromCos(float cosX)
{
  return sqrt(saturate(1 - cosX * cosX));
}

float3 SphericalToCartesian(float cosPhi, float sinPhi, float cosTheta)
{
  float sinTheta = SinFromCos(cosTheta);

  return float3(float2(cosPhi, sinPhi) * sinTheta, cosTheta);
}

float3 SphericalToCartesian(float phi, float cosTheta)
{
  float sinPhi, cosPhi;
  sincos(phi, sinPhi, cosPhi);

  return SphericalToCartesian(cosPhi, sinPhi, cosTheta);
}
