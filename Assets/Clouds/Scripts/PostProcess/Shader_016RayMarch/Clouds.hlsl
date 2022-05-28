#ifndef QINGZHU_CLOUDS
#define QINGZHU_CLOUDS
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "Assets/ShaderLabs/Shaders/RayMarchingIntersection.hlsl"


TEXTURE3D(shapeNoise);
SAMPLER(sampler_shapeNoise);

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
           


// CBUFFER_START(UnityPerMaterial) // Required to be compatible with SRP Batcher
float4 _MainTex_ST;
float4 sphereCenter;
float4 boxmin;
float4 boxmax;

float samplerScale;
float4 samplerOffset;
float densityMultipler;
float densityThreshold;//云层密度阀值
int numberStepCloud;//云层密度检测步进

float lightPhaseValue;//光线穿透能力
float lightAbsorptionThroughCloud;//云层对光的吸收率


int numberStepLight;//管线传播步进
float lightAbsorptionTowardSun;//云层对光的吸收率
float darknessThreshold;//最低光线穿透阀值

float globalCoverage;



float debug_shape_z;
float debug_rgba;
// CBUFFER_END


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

float lightMarching(float3 rayPos)
{
    // return 0.0f;
    float3 dir = _MainLightPosition.xyz;
    
    float3 dirToLight = normalize(dir.xyz);
    
    float distInsideBox = RayBoxIntersection(rayPos,dirToLight,boxmin,boxmax).y;
    // return exp(distInsideBox);
    // if (distInsideBox<=0.001)
    // {
    //     return 1;
    // }
    // 
    float stepSize = distInsideBox/numberStepLight;
    float totalDensity = 0;
    for (int step = 0;step <numberStepLight;step++)
    {
        rayPos += dirToLight*stepSize;
        float density = sampleDensity(rayPos);
        totalDensity += density*stepSize;
    }
    // return totalDensity;

    //密度越大,穿透率越低
    float transmittance = exp(-totalDensity*lightAbsorptionTowardSun);
    
    return darknessThreshold + transmittance*(1-darknessThreshold);
}




#endif