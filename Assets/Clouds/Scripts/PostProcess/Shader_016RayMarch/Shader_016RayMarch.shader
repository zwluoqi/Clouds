
Shader "Shader/Shader_016RayMarch"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        shapeNoise ("shapeNoise", 3D) = "white" {}
    }
    
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
    #include "Assets/ShaderLabs/Shaders/RayMarchingIntersection.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Clouds.hlsl"

        #pragma multi_compile _ DEBUG_SHAPE_NOSE
            
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
                
            half4 FragBlurH(Varyings input) : SV_Target
            {

                
              float4 ndcPos = (input.screenPos / input.screenPos.w);
              float deviceDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, ndcPos.xy);                
              float3 colorWorldPos = ComputeWorldSpacePosition(ndcPos.xy, deviceDepth, UNITY_MATRIX_I_VP);

            #ifdef DEBUG_SHAPE_NOSE
            float4 rgba = SAMPLE_TEXTURE3D_LOD(shapeNoise, sampler_shapeNoise, float3(ndcPos.xy,debug_shape_z),0);
                if (debug_rgba<4){
            return rgba[debug_rgba];
                    }else
                    {
                        return rgba;
                    }
            #endif
                
            


             
              float3 cameraToPosDir = colorWorldPos - _WorldSpaceCameraPos.xyz;
              float3 rayDir = normalize(cameraToPosDir);

              float2 distToBox = RayBoxIntersection(_WorldSpaceCameraPos.xyz,rayDir,boxmin,boxmax);
              float rayDst = min(distToBox.y,length(cameraToPosDir)-distToBox.x);


              float totalLightTransmittance = 0;
              float lightEnergy = 0;
              float transmittance = 1;
              if(rayDst>0.01f){

                  float stepDst = rayDst/numberStepCloud;

                  float3 hitPoint = _WorldSpaceCameraPos.xyz + rayDir*(distToBox.x);

                  float curStep=0.0;
                  while (curStep<numberStepCloud)
                  {
                      float3 rayPos = hitPoint + rayDir*(stepDst)*(curStep);
                      float density = sampleDensity(rayPos);
                      if(density > 0.01f )
                      {
                          float lightTransmittance = lightMarching(rayPos);
                          totalLightTransmittance += lightTransmittance;

                          lightEnergy += (density * stepDst * transmittance * lightTransmittance*lightPhaseValue);
                          transmittance *= (exp(-density*stepDst*lightAbsorptionThroughCloud));
                          if(transmittance < 0.001)
                          {
                              break;
                          }
                      }
                      curStep +=1.0;;
                  }
               }

                // Add Cloud To background
              //
              float4 backgroundCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,ndcPos.xy);
              float4 cloudCol = lightEnergy*_MainLightColor;
              // float transmittance = exp(-totalDensity);
              return float4(backgroundCol.rgb*transmittance + cloudCol.rgb,1);
            }

    
            
    ENDHLSL
    
    SubShader
    {
        Tags { "RenderType"="TransParent" "Queue" = "TransParent"}
        LOD 100

        Blend One Zero
        ZWrite Off
        ZTest LEqual
        Cull Off
        
        Pass
        {            
            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment FragBlurH           
            ENDHLSL
        }
    }
}
