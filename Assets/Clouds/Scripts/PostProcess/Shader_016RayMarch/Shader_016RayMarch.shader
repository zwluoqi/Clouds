
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
                return rgba.r;
                #endif
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,ndcPos.xy);


             
              float3 cameraToPosDir = colorWorldPos - _WorldSpaceCameraPos.xyz;
              float3 rayDir = normalize(cameraToPosDir);

              float2 distToBox = RayBoxIntersection(_WorldSpaceCameraPos.xyz,rayDir,boxmin,boxmax);
              float rayDst = min(distToBox.y,length(cameraToPosDir)-distToBox.x);


              float totalDensity = 0;
              if(rayDst>0){
                  float NumSteps = 100.0f;
                  // float3 rayPos = _WorldSpaceCameraPos.xyz + rayDir*(distToBox.x);
                  float stepDst = rayDst/NumSteps;
                  
                  
                  float rayDstAdd = 0;
                  float stepVal = 0.0;
                  while (stepVal < NumSteps)
                  {
                      float3 rayPos = _WorldSpaceCameraPos.xyz + rayDir*(distToBox.x+rayDstAdd);
                  
                      totalDensity += sampleDensity(rayPos)*stepDst;
                  
                      rayDstAdd += stepDst;
                      stepVal += 1.0f;
                  }

               }

              float transmittance = exp(-totalDensity);
              return transmittance*col;
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
