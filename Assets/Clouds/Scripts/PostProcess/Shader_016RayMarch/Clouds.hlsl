#ifndef QINGZHU_CLOUDS
#define QINGZHU_CLOUDS
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "Assets/ShaderLabs/Shaders/RayMarchingIntersection.hlsl"


TEXTURE3D(shapeNoise);
SAMPLER(sampler_shapeNoise);

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

TEXTURE2D(_CloudTex);
SAMPLER(sampler_CloudTex);


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

float remap(float v,float l0,float h0,float ln,float hn)
{
    return ln+(v-l0)*(hn-ln)/(h0-l0);
}


float sampleDensityBox(float3 worldPos)
{
    float3 texCoord = worldPos*samplerScale*0.001+samplerOffset*0.01;
    float4 rgba = SAMPLE_TEXTURE3D_LOD(shapeNoise, sampler_shapeNoise, texCoord,0);

    float3 size = boxmax - boxmin;
    float wc0=rgba.r;
    float wc1=rgba.g;
    float wh=rgba.b;
    float wd=rgba.a;
    
    
    float gMin = .2;
    float gMax = .7;
    float heightPercent = (worldPos.y - boxmin.y) / size.y;
    
    float heightGradient = saturate(remap(heightPercent, 0.0, gMin, 0, 1))
    * saturate(remap(heightPercent, 1, gMax, 0, 1));
    // heightGradient *= edgeWeight;
    
    float density = max(0,wc0-densityThreshold)*densityMultipler;

    
    return density*heightGradient;
}


float sampleDensitySphere(float3 worldPos)
{
    float3 texCoord = worldPos*samplerScale*0.001+samplerOffset*0.01;
    float4 rgba = SAMPLE_TEXTURE3D_LOD(shapeNoise, sampler_shapeNoise, texCoord,0);

    float3 size = boxmax - boxmin;
    float height = length(worldPos-sphereCenter);

    float wc0=rgba.r;
    float wc1=rgba.g;
    float wh=rgba.b;
    float wd=rgba.a;

    
    float heightPercent =saturate( (height - boxmin.x-size.x*0.5) / (size.x*0.5) );
    

//     5 SRb = SAT(R(ph, 0, 0.07, 0, 1)) 向下映射<br>
//     6 SRt = SAT(R(ph, wh ×0.2, wh, 1, 0)) 向上映射<br>
//     7 SA = SRb × SRt <br>
    float SRb = SAT(remap(heightPercent, 0, 0.07, 0, 1));
    float SRt = SAT(remap(heightPercent, 1*0.2, 1, 1, 0));
    float SA = SRb*SRt;
    
    // 8 DRb = ph ×SAT(R(ph, 0, 0.15, 0, 1)) 向底部降低密度<br>
    // 9 DRt = SAT(R(ph, 0.9, 1.0, 1, 0))) 向顶部的更柔和的过渡降低密度<br>
    // 10 DA = gd × DRb × DRt × wd × 2 密度融合<br>
    float DRb = SAT(remap(heightPercent, 0,  0.15, 0, 1));
    float DRt = SAT(remap(heightPercent, 0.9, 1, 1, 0));
    float DA = 0.5*SRb*SRt*1*2;

    float density = max(0,wc0-densityThreshold)*densityMultipler;

    return density*SA*DA;
}



float sampleDensity(float3 worldpos)
{
    #if SHAPE_BOX
    return sampleDensityBox(worldpos);
    #elif SHAPE_SPHERE
    return sampleDensitySphere(worldpos);
    #else
    return 0;
    #endif
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