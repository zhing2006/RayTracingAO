
float4 _SHAr;
float4 _SHAg;
float4 _SHAb;
float4 _SHBr;
float4 _SHBg;
float4 _SHBb;
float4 _SHC;

// Ref: "Efficient Evaluation of Irradiance Environment Maps" from ShaderX 2
float3 SHEvalLinearL0L1(float3 N, float4 shAr, float4 shAg, float4 shAb)
{
  float4 vA = float4(N, 1.0);

  float3 x1;
  // Linear (L1) + constant (L0) polynomial terms
  x1.r = dot(shAr, vA);
  x1.g = dot(shAg, vA);
  x1.b = dot(shAb, vA);

  return x1;
}

float3 SHEvalLinearL2(float3 N, float4 shBr, float4 shBg, float4 shBb, float4 shC)
{
  float3 x2;
  // 4 of the quadratic (L2) polynomials
  float4 vB = N.xyzz * N.yzzx;
  x2.r = dot(shBr, vB);
  x2.g = dot(shBg, vB);
  x2.b = dot(shBb, vB);

  // Final (5th) quadratic (L2) polynomial
  float vC = N.x * N.x - N.y * N.y;
  float3 x3 = shC.rgb * vC;

  return x2 + x3;
}

float3 SampleSH9(float4 SHCoefficients[7], float3 N)
{
  float4 shAr = SHCoefficients[0];
  float4 shAg = SHCoefficients[1];
  float4 shAb = SHCoefficients[2];
  float4 shBr = SHCoefficients[3];
  float4 shBg = SHCoefficients[4];
  float4 shBb = SHCoefficients[5];
  float4 shCr = SHCoefficients[6];

  // Linear + constant polynomial terms
  float3 res = SHEvalLinearL0L1(N, shAr, shAg, shAb);

  // Quadratic polynomials
  res += SHEvalLinearL2(N, shBr, shBg, shBb, shCr);

  return res;
}

float3 GetSHColor(float3 N)
{
  float4 SHCoefficients[7];
  SHCoefficients[0] = _SHAr;
  SHCoefficients[1] = _SHAg;
  SHCoefficients[2] = _SHAb;
  SHCoefficients[3] = _SHBr;
  SHCoefficients[4] = _SHBg;
  SHCoefficients[5] = _SHBb;
  SHCoefficients[6] = _SHC;
  return SampleSH9(SHCoefficients, N);
}
