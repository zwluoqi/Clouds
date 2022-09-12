
Shader "Shader/RayMarchCloud"
{
    Properties
    {
//        _MainTex ("Texture", 2D) = "white" {}
//        shapeNoise ("shapeNoise", 3D) = "white" {}


//        boxmin("boxmin",vector)=(0,0,0,0)
//        boxmax("boxmax",vector)=(1,1,1,0)        
//
//        samplerScale("samplerScale",float) = 1
//        samplerOffset("samplerOffset",vector) = (0,0,0,0)
//        densityMultipler("densityMultipler",float) = 1
//        densityThreshold("densityThreshold",float) = 1
//        numberStepCloud("numberStepCloud",int) = 1
//        
//        lightPhaseValue("lightPhaseValue",float) = 1
//        lightAbsorptionThroughCloud("lightAbsorptionThroughCloud",float) = 1
//        
//        numberStepLight("numberStepLight",int) = 1
//        lightAbsorptionTowardSun("lightAbsorptionTowardSun",float) = 1
//        darknessThreshold("darknessThreshold",float) = 1
//
//        globalCoverage("globalCoverage",float) = 1
//        debug_shape_z("debug_shape_z",float) = 1
//        debug_rgba("debug_rgba",vector) = (1,1,1,1)
    }
    
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
    #include "Assets/ShaderLabs/Shaders/RayMarchingIntersection.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Clouds.hlsl"

        #pragma multi_compile _ DEBUG_SHAPE_NOSE DEBUG_DETAIL_NOSE DEBUG_SHAPE_DENSITY_UV
        #pragma multi_compile SHAPE_BOX SHAPE_SPHERE
        #pragma multi_compile _ FRAME_DIVIDE

    
            
            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float4 screenPos :TEXCOORD1;
            };
            
            Varyings FullscreenVert(Attributes input)
            {
                Varyings o;
                o.vertex = TransformObjectToHClip(input.vertex.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                o.color = input.color;
                               //vertex
                o.screenPos = ComputeScreenPos(o.vertex);//o.vertex是裁剪空间的顶点
                return o;
            }
            
            half4 DepthShow(Varyings input) : SV_Target
            {
              float4 ndcPos = (input.screenPos / input.screenPos.w);

              float deviceDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, ndcPos.xy);                

              float sceneDepth =  LinearEyeDepth(deviceDepth,_ZBufferParams)*0.001;
              return float4(sceneDepth,sceneDepth,sceneDepth,1);
            }
                
            half4 FragCloud(Varyings input) : SV_Target
            {
                float4 ndcPos = (input.screenPos / input.screenPos.w);
                float deviceDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, ndcPos.xy);                

                
                #ifdef FRAME_DIVIDE
                    int frameOrder = GetIndex(ndcPos.xy, _TargetWidth, _TargetHeight, _FrameIterationCount);

                    //判断当帧是否渲染该片元
                    if (frameOrder != _FrameCount)
                    {
                        return 0;
                    }
                #endif
                
                float3 colorWorldPos = ComputeWorldSpacePosition(ndcPos.xy, deviceDepth, UNITY_MATRIX_I_VP);
                
                #ifdef DEBUG_SHAPE_NOSE
                    float4 rgba = SAMPLE_TEXTURE3D_LOD(shapeNoise, sampler_shapeNoise, float3(ndcPos.xy,debug_shape_z),0);
                    if (debug_rgba<4){
                        return rgba[debug_rgba];
                    }else
                    {
                        return rgba;
                    }
                #elif DEBUG_DETAIL_NOSE
                    float4 rgba = SAMPLE_TEXTURE3D_LOD(detailNoise, sampler_detailNoise, float3(ndcPos.xy,debug_shape_z),0);
                    if (debug_rgba<4){
                        return rgba[debug_rgba];
                    }else
                    {
                        return rgba;
                    }
                #endif
                
            
                float3 cameraToPosDir = colorWorldPos - _WorldSpaceCameraPos.xyz;
                float3 rayDir = normalize(cameraToPosDir);


                #ifdef SHAPE_BOX
                    float2 distToBox = RayBoxIntersection(_WorldSpaceCameraPos.xyz,rayDir,boxmin,boxmax);
  
                    float distToBoxHit = distToBox.x;
                    float rayDst = min(distToBox.y,length(cameraToPosDir)-distToBoxHit);
                #elif  SHAPE_SPHERE
                    //optimize
                    //float2 radius = GetOptimizeRadius(boxmin.x,boxmax.x);
                    float2 radius = float2(boxmin.x,boxmax.x);
                    float2 distToOuter = RaySphereIntersection(sphereCenter.xyz,radius.y,_WorldSpaceCameraPos.xyz,rayDir);
                    float2 distToInner = RaySphereIntersection(sphereCenter.xyz,radius.x,_WorldSpaceCameraPos.xyz,rayDir);
  
  
                    float distToBoxHit = min(distToOuter.x,distToInner.x);
                    float rayDst = min(distToInner.x- distToOuter.x,distToOuter.y-distToInner.y);
                    rayDst = min(rayDst,length(cameraToPosDir)-distToBoxHit);
                #else
                    float rayDst  = 0;
                    float distToBoxHit = 0;
                #endif

  
                float totalLightTransmittance = 0;
                float lightEnergy = 0;
                float transmittance = 1;
                float cloudHeight = 1;
                float totalDensity = 0;
                #ifdef SHAPE_BOX
                    cloudHeight = (boxmax.y-boxmin.y);
                #elif SHAPE_SPHERE
                    cloudHeight = (boxmax.x-boxmin.x);
                #endif


                if(rayDst>0.001f){

                    float3 dirToLight = normalize(_MainLightPosition.xyz);
                    float cosTheta = dot(-rayDir,dirToLight);
                    // return cosTheta;
                    // float isex = ISextra(cosTheta);
                    // return isex;
                    // float hgInsValue = hg(cosTheta,lightPhaseIns);
                    // return hgInsValue;
                    // float hgOutValue = hg(cosTheta,-lightPhaseOuts);
                    // return hgOutValue;
                    float lightPhaseValue = lightPhase(cosTheta)*lightPhaseStrength;
                    // return lightPhaseValue;

                    
                    
                    #ifdef DEBUG_SHAPE_DENSITY_UV
                    
                    float3 textureCoord = GetUVWH(colorWorldPos,cloudHeight);
                    // textureCoord = frac(textureCoord);
                    // return float4(textureCoord,0);

                    // float4 weatherColor = SAMPLE_TEXTURE2D_LOD(weatherMap, sampler_weatherMap,float2(textureCoord.x,textureCoord.z),0);
                    //
                    // float wc0 = weatherColor.r;
                    // float wc1 = weatherColor.g;
                    // float wh = weatherColor.b;
                    // float wd = weatherColor.a;
                    // return float4(wd,0,0,0);
                    float4 rgba = SAMPLE_TEXTURE3D_LOD(shapeNoise, sampler_shapeNoise, textureCoord,0);
                    return float4(rgba.rgb,0);
                    #endif
                    
                    float stepDst = rayDst/numberStepCloud;
                    float4 rayMarchOffset = SAMPLE_TEXTURE2D_LOD(rayMarchOffsetMap, sampler_rayMarchOffsetMap,offsetMapUVScale*float2(rayDir.x+rayDir.y,rayDir.y+rayDir.z),0);

                    float startOffset = offsetMapValueScale*stepDst*rayMarchOffset.x;//TAA TODO
                    rayDst -= startOffset;
                    stepDst = rayDst/numberStepCloud;
                    float curStep = stepDst*2;//开始步进大,遇到云后步进变小
                    float3 hitPoint = _WorldSpaceCameraPos.xyz + rayDir*(distToBoxHit);
                    float marchLength = startOffset*0.5f;
                    for (int step = 0;step <numberStepCloud;step++)
                    {
                        float3 rayPos = hitPoint + rayDir*marchLength;
  
                        float density = sampleDensity(rayPos);
                        // totalDensity+=density;
                        float lightEnergyFactor = 1;
                        if(density > 0 )
                        {
                            curStep = stepDst;
                            float lengthToLightCould = 0;
                            #ifdef SHAPE_BOX
                                  lengthToLightCould = RayBoxIntersection(rayPos,dirToLight,boxmin,boxmax).y;
                            #elif SHAPE_SPHERE
                                  lengthToLightCould = RaySphereIntersection(sphereCenter.xyz,radius.y,rayPos,dirToLight).y;
                            #endif
                            
                        
                            float lightDensity = lightMarchingDensity(rayPos,dirToLight,lengthToLightCould);
                            float d = lightDensity*lightAbsorptionTowardSun/cloudHeight;
                            //2*exp(-d)*(1-exp(-2*d))
                            float lightTransmittance = (exp(-d))*(1-darknessThreshold)+darknessThreshold;
                            totalLightTransmittance += lightTransmittance;
  
                            #ifdef SHAPE_SPHERE
                                  float ditFacotr = lengthToLightCould/(2*radius.x);
                                  lightEnergyFactor = 1-ditFacotr*ditFacotr*ditFacotr;
                            #endif                   
                            
                            lightEnergy += (density * curStep * transmittance * lightTransmittance*lightPhaseValue)*lightEnergyFactor;
                            transmittance *= (exp(-density*curStep*lightAbsorptionThroughCloud/cloudHeight));
                            if(transmittance < 0.001)
                            {
                                break;
                            }
                        }
                        marchLength += (curStep);
                        if(marchLength>rayDst)
                        {
                            break;
                        }
                    }
                }
                float4 backgroundCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,ndcPos.xy);
                float4 cloudCol = lightEnergy*_MainLightColor;
                
                return float4(backgroundCol.xyz*transmittance+cloudCol.rgb,1);
                // return float4(cloudCol.rgb,transmittance);
            }

            half4 FragBlend(Varyings input) : SV_Target{
                float4 ndcPos = (input.screenPos / input.screenPos.w);
                float4 backgroundCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,ndcPos.xy);
                float4 cloudCol = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex,ndcPos.xy);
                return float4(backgroundCol.xyz*cloudCol.a+cloudCol.xyz,1);
            }

            float4 GetPreClipFun(float2 uv,float4x4 _InverseProjection,float4x4 _InverseRotation,float4x4 _PreviousRotation,float4x4 _Projection)
            {
                float4 screenPos = float4(uv*2.0-1.0,1.0,1.0);
                float4 cameraPos = mul(_InverseProjection,screenPos);
                cameraPos = cameraPos/cameraPos.w;
                float3 worldPos = mul((float3x3)_InverseRotation,cameraPos.xyz);
                float3 preCameraPos = mul((float3x3)_PreviousRotation,worldPos.xyz);
                float4 pre_clip = mul(_Projection,preCameraPos);
                pre_clip /= pre_clip.w;
                pre_clip.xy = pre_clip.xy*0.5+0.5;
                return float4(pre_clip.xy,0,1);
            }

            half4 FragBlendPreFrame(Varyings input) : SV_Target{
                float4 ndcPos = (input.screenPos / input.screenPos.w);
                float4 cloudCol;
                
                #ifndef  FRAME_DIVIDE
                    cloudCol = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex,ndcPos.xy);
                    return cloudCol;
                #endif

                

                int frameOrder = GetIndex(ndcPos.xy, _TargetWidth, _TargetHeight, _FrameIterationCount);

                // return float4(pre_clip.xy,0,0);                
                // return float4(abs(pre_clip.xy- ndcPos.xy),0,0);

                
                //判断当帧是否渲染该片元
                if (frameOrder == _FrameCount)
                {
                    cloudCol = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex,ndcPos.xy);
                    return cloudCol;
                }

                
                float4 pre_clip = GetPreClipFun(ndcPos.xy,_InverseProjection,_InverseRotation,_PreviousRotation,_Projection);
                // return float4(pre_clip.xy,0,1);
                if(pre_clip.y<0.0 || pre_clip.y>1.0 ||
                    pre_clip.x <0.0 || pre_clip.x >1.0)
                {
                    cloudCol = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex,ndcPos.xy); 
                }else
                {
                    cloudCol = SAMPLE_TEXTURE2D(_PreTex, sampler_PreTex,pre_clip.xy);
                    // if(_TotalFrameCount > 15){
                    //     float2  offs = 0.5/float2(_TargetWidth,_TargetHeight);
                    //     float4 s1 = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex,ndcPos.xy+offs*float2(-1,-1));
                    //     float4 s2 = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex,ndcPos.xy+offs*float2(1,1));
                    //     float4 smin = min(s1,s2);
                    //     float4 smax = max(s1,s2);
                    //     cloudCol = clamp(cloudCol,smin,smax);
                    // }
                    // return 0;
                }
                return cloudCol;
            }
    
            
    ENDHLSL
    
    SubShader
    {
        Tags { "RenderType"="TransParent" "Queue" = "TransParent"}
        LOD 100

        ZWrite Off
        ZTest LEqual
        Cull Off
        
        Pass
        {        
            Blend One Zero
    
            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment FragCloud           
            ENDHLSL
        }

        Pass
        {        
            Blend One Zero
    
            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment FragBlend           
            ENDHLSL
        }
        
        Pass
        {        
            Blend One Zero
    
            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment FragBlendPreFrame           
            ENDHLSL
        }
    }
}
