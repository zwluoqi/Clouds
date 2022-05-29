#ifndef QINGZHU_CLOUDS
#define QINGZHU_CLOUDS
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "Assets/ShaderLabs/Shaders/RayMarchingIntersection.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

TEXTURE2D(_SkyTex);
SAMPLER(sampler_SkyTex);


// CBUFFER_START(UnityPerMaterial) // Required to be compatible with SRP Batcher
float4 _MainTex_ST;
float4 sphereCenter;
float radiusTerrain;
float radiusAtoms;


float atomDensityFalloff;
int numberStepSky;
float lightPhaseValue;

int numberStepLight;
float lightAbsorptionTowardSun;
float darknessThreshold;

float4 waveRGBScatteringCoefficients;
// CBUFFER_END



float sampleDensity(float3 worldpos)
{
    float3 ditToCenter = worldpos - sphereCenter;
    float size = (radiusAtoms-radiusTerrain);
    float height01 = saturate( (length(ditToCenter) - radiusTerrain)/size);
    float density = exp(-height01*atomDensityFalloff)*(1-height01);
    return density;
}


float4 marchingTransmittance(float3 rayPos,float3 rayDir,float rayLength)
{
    float stepSize = rayLength/numberStepLight;
    float totalDensity = 0;
    for (int step = 0;step <numberStepLight;step++)
    {
        rayPos += rayDir*stepSize;
        float density = sampleDensity(rayPos);
        totalDensity += density*stepSize;
    }

    //密度越大,穿透率越低
    float4 transmittance = exp(-totalDensity*lightAbsorptionTowardSun*waveRGBScatteringCoefficients);
    
    return darknessThreshold + transmittance*(1-darknessThreshold);
}




#endif