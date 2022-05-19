#ifndef QINGZHU_CLOUDS
#define QINGZHU_CLOUDS
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

TEXTURE3D(shapeNoise);
SAMPLER(sampler_shapeNoise);

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
           


CBUFFER_START(UnityPerMaterial) // Required to be compatible with SRP Batcher
float4 _MainTex_ST;
float3 boxmin;
float3 boxmax;

float samplerScale;
float3 samplerOffset;
float densityMultipler;
float densityThreshold;

float globalCoverage;

float debug_shape_z;
CBUFFER_END


float SAT(float v)
{
    return clamp(0,1,v);
}

float sampleDensity(float3 worldpos)
{
    float3 texCoord = worldpos*samplerScale*0.001+samplerOffset*0.01;
    float4 rgba = SAMPLE_TEXTURE3D_LOD(shapeNoise, sampler_shapeNoise, texCoord,0);

    float wc0=rgba.r;
    float wc1=rgba.g;
    float wh=rgba.b;
    float wd=rgba.a;
    
    // R(wc0) G(wc1) coverage ,
    // B(wh) cloud height
    // A(wd) cloud density
    // 4 WM = max(wc0,STA(gc-0.5)*wc1*2) 云出现率
    float density = max(0,wc0-densityThreshold)*densityMultipler;
    // float density = max(wc0,SAT(globalCoverage-0.5)*wc1*2)*densityMultipler;
    return density;
}




#endif