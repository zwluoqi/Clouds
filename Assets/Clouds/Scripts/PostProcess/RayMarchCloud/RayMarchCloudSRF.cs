using System;
using System.Collections.Generic;
using Clouds;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public class RayMarchCloudSRF : ScriptableRendererFeature
{
    class RayMarchCloudPass : ScriptableRenderPass
    {

        public static string k_RenderTag = "RayMarchCloud";
        static string shaderName = "Shader/RayMarchCloud";

        private RenderTargetIdentifier _renderTargetIdentifier;
        private Material _material;

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int TmpTexId = Shader.PropertyToID("_CloudTex");
        private static readonly int TmpTexId2 = Shader.PropertyToID("_CloudTex2");


        // private int TmpWidth;
        // private int TmpHeight;
        public Dictionary<int, CameraFrameData> _CloudCameraDatas = new Dictionary<int, CameraFrameData>();

        public class CameraFrameData
        {
            public RenderTexture subTex;
            public RenderTexture[] buffTexs = new RenderTexture[2];
            public int curIndex;
            public Matrix4x4 previousRotation;
            public int frameCount = 0;
        }
        public RayMarchCloudPass()
        {
            
        }
        
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {

        }

        public void SetUp(RenderTargetIdentifier targetIdentifier)
        {
            this._renderTargetIdentifier = targetIdentifier;
            
            //需要存储法线
            ConfigureInput(ScriptableRenderPassInput.Normal|ScriptableRenderPassInput.Depth|ScriptableRenderPassInput.Color);

        }
        
        bool EnsureRenderTarget(ref RenderTexture rt, int width, int height, RenderTextureFormat format, FilterMode filterMode, int depthBits = 0, int antiAliasing = 1)
        {
            if (rt != null && (rt.width != width || rt.height != height || rt.format != format || rt.filterMode != filterMode || rt.antiAliasing != antiAliasing))
            {
                RenderTexture.ReleaseTemporary(rt);
                rt = null;
            }
            if (rt == null)
            {
                rt = RenderTexture.GetTemporary(width, height, depthBits, format, RenderTextureReadWrite.Default, antiAliasing);
                rt.filterMode = filterMode;
                rt.wrapMode = TextureWrapMode.Clamp;
                return true;// new target
            }
            return false;// same target
        }
        
        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

            CloudBox[] cloudBoxes = Object.FindObjectsOfType<CloudBox>();
            if (cloudBoxes.Length == 0)
            {
                return;
            }
            
            
            if (_material == null)
            {
                CreateMaterial(shaderName);
            }

            var cmd = CommandBufferPool.Get(k_RenderTag);

            var w = renderingData.cameraData.camera.scaledPixelWidth;
            var h = renderingData.cameraData.camera.scaledPixelHeight;
            var cameraKey = renderingData.cameraData.camera.GetInstanceID();
            
            this._CloudCameraDatas.TryGetValue(cameraKey,out var _CloudData);
            if (_CloudData == null)
            {
                _CloudData = new CameraFrameData();
                this._CloudCameraDatas[cameraKey] = _CloudData;
            }

            _CloudData.frameCount++;
            var soruce = _renderTargetIdentifier;
            bool newRt = EnsureRenderTarget(ref _CloudData.buffTexs[0], w / 2, h / 2, RenderTextureFormat.ARGB32, FilterMode.Bilinear);
            bool newRt2 = EnsureRenderTarget(ref _CloudData.buffTexs[1], w / 2, h / 2, RenderTextureFormat.ARGB32, FilterMode.Bilinear);
            bool newRt3 = EnsureRenderTarget(ref _CloudData.subTex, w / 2, h / 2, RenderTextureFormat.ARGB32, FilterMode.Bilinear);
            if (newRt || newRt2 || newRt3)
            {
                // Matrix4x4 initWorldToCameraMatrix = Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)) * renderingData.cameraData.GetViewMatrix();
                // _CloudData.previousView2WorldMat = initWorldToCameraMatrix.inverse;//Matrix4x4.Inverse();
                _CloudData.previousRotation = renderingData.cameraData.camera.nonJitteredProjectionMatrix;
            }
            // cmd.GetTemporaryRT(TmpTexId2,w,h,0,FilterMode.Bilinear, RenderTextureFormat.ARGB32);

            var projection = renderingData.cameraData.camera.nonJitteredProjectionMatrix;
            var rotation = renderingData.cameraData.camera.worldToCameraMatrix;
            var inverseRotation = renderingData.cameraData.camera.cameraToWorldMatrix;
            for (int i = 0; i < cloudBoxes.Length; i++)
            {
                var box = cloudBoxes[i];
                if (!box.enabled)
                {
                    continue;
                }
                var transform = box.transform;
                Vector3 boxcenter = transform.position;
                var localScale = transform.localScale;


                
                
                cmd.SetGlobalFloat("samplerScale",box.samplerScale);
                cmd.SetGlobalFloat("samplerHeightScale",box.samplerHeightScale);
                
                cmd.SetGlobalVector("samplerOffset",box.samplerOffset);
                cmd.SetGlobalFloat("globalCoverage",box.globalCoverage);
                cmd.SetGlobalFloat("globalDensity",box.globalDensity);
                cmd.SetGlobalFloat("globalThickness",box.globalThickness);
                cmd.SetGlobalFloat("globalStarHeight",box.globalStarHeight);
                
                
                cmd.SetGlobalFloat("densityMultipler",box.densityMultipler);
                cmd.SetGlobalFloat("densityThreshold",box.densityThreshold);
                cmd.SetGlobalInt("numberStepCloud",box.numberStepCloud);
                
                
                cmd.SetGlobalFloat("lightPhaseStrength",box.lightPhaseStrength);
                cmd.SetGlobalFloat("lightPhaseIns",box.lightPhaseIns);
                cmd.SetGlobalFloat("lightPhaseOuts",box.lightPhaseOuts);
                cmd.SetGlobalFloat("lightPhaseBlend",box.lightPhaseBlend);
                
                cmd.SetGlobalFloat("lightPhaseCsi",box.lightPhaseCsi);
                cmd.SetGlobalFloat("lightPhaseCse",box.lightPhaseCse);
                
                
                
                cmd.SetGlobalFloat("lightAbsorptionThroughCloud",box.lightAbsorptionThroughCloud);
                
                
                cmd.SetGlobalFloat("lightAbsorptionTowardSun",box.lightAbsorptionTowardSun);
                cmd.SetGlobalFloat("darknessThreshold",box.darknessThreshold);
                cmd.SetGlobalInt("numberStepLight",box.numberStepLight);
                
                
                
                
                cmd.SetGlobalTexture("shapeNoise",box.textureShape);
                cmd.SetGlobalTexture("detailNoise",box.detailShape);
                cmd.SetGlobalTexture("weatherMap",box.weatherMap);
                cmd.SetGlobalTexture("rayMarchOffsetMap",box.rayMarchOffsetMap);
                
                cmd.SetGlobalFloat("offsetMapUVScale",box.offsetMapUVScale);
                cmd.SetGlobalFloat("offsetMapValueScale",box.offsetMapValueScale);

                if (box._DebugFrameCount != 0)
                {
                    
                    cmd.SetGlobalInt("_FrameCount", box._DebugFrameCount-1);
                }
                else
                {
                    var f = _CloudData.frameCount % 16;
                    cmd.SetGlobalInt("_FrameCount", f);
                }
                cmd.SetGlobalInt("_TotalFrameCount", _CloudData.frameCount);
                
                cmd.SetGlobalInt("_FrameIterationCount", box.FrameIterationMode == 0 ? 2 : 4);
                cmd.SetGlobalInt("_TargetWidth",w/2);
                cmd.SetGlobalInt("_TargetHeight",h/2);
                // cmd.SetGlobalMatrix("PRE_UNITY_MATRIX_I_V",_CloudData.previousView2WorldMat);
                
                cmd.SetGlobalMatrix("_InverseProjection",projection.inverse);
                cmd.SetGlobalMatrix("_InverseRotation",inverseRotation);
                cmd.SetGlobalMatrix("_PreviousRotation",_CloudData.previousRotation);
                cmd.SetGlobalMatrix("_Projection",projection);
        
                cmd.SetGlobalFloat("debug_shape_z",box.debug_shape_z);
                cmd.SetGlobalInt("debug_rgba",(int)box.debug_rgba);


                if (box._FrameDivide)
                {
                    cmd.EnableShaderKeyword("FRAME_DIVIDE");
                }
                else
                {
                    cmd.DisableShaderKeyword("FRAME_DIVIDE");
                }
                
                if (box.debug_shape_noise == DEBUG_SHAPE.SHAPE)
                {
                    EnableDebugShapeKeyWord(cmd,"DEBUG_SHAPE_NOSE");
                }
                else if(box.debug_shape_noise == DEBUG_SHAPE.DETAIL)
                {
                    EnableDebugShapeKeyWord(cmd,"DEBUG_DETAIL_NOSE");
                }
                else if(box.debug_shape_noise == DEBUG_SHAPE.DEBUG_SHAPE_DENSITY_UV)
                {
                    EnableDebugShapeKeyWord(cmd,"DEBUG_SHAPE_DENSITY_UV");
                }
                else
                {
                    EnableDebugShapeKeyWord(cmd,"");
                }


                if (box.cloudShape == CloudBox.CloudShape.BOX)
                {
                    EnableShapeKeyWord(cmd,"SHAPE_BOX");
                    
                    Vector3 boxmin = boxcenter - localScale*0.5f;
                    Vector3 boxmax = boxcenter + localScale*0.5f;
                    cmd.SetGlobalVector("boxmin", boxmin);
                    cmd.SetGlobalVector("boxmax", boxmax);
                }
                else
                {
                    EnableShapeKeyWord(cmd,"SHAPE_SPHERE");

                    float raidu0 = localScale.x*0.5f;
                    float raidu1 = raidu0 * 1.0f;
                    float raidu2 = raidu1 * 1.2f;
                    
                    cmd.SetGlobalVector("boxmin", new Vector4(raidu1,0,0,0));
                    cmd.SetGlobalVector("boxmax", new Vector4(raidu2,0,0,0));
                    cmd.SetGlobalVector("sphereCenter", boxcenter);
                }

                _CloudData.curIndex ^= 1;
                //render current
                cmd.SetGlobalTexture("_MainTex",soruce);
                cmd.Blit(null,_CloudData.subTex,_material,0);

                
                //combine pre
                cmd.SetGlobalTexture("_CloudTex",_CloudData.subTex);
                cmd.SetGlobalTexture("_PreTex",_CloudData.buffTexs[_CloudData.curIndex^1]);
                cmd.Blit(null,_CloudData.buffTexs[_CloudData.curIndex],_material,2);
                
                //copy
                cmd.Blit(_CloudData.buffTexs[_CloudData.curIndex], soruce);
            }
            
            // cmd.ReleaseTemporaryRT(TmpTexId2);
            
            context.ExecuteCommandBuffer(cmd);

            CommandBufferPool.Release(cmd);

            // previousView2WorldMat = renderingData.cameraData.camera.cameraToWorldMatrix;
            // Matrix4x4 worldToCameraMatrix = Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)) * renderingData.cameraData.GetViewMatrix();
            // _CloudData.previousView2WorldMat = worldToCameraMatrix.inverse;//Matrix4x4.Inverse();
            _CloudData.previousRotation = rotation;
        }

        private void EnableDebugShapeKeyWord(CommandBuffer cmd, string debugShapeNose)
        {
            cmd.DisableShaderKeyword("DEBUG_SHAPE_NOSE");
            cmd.DisableShaderKeyword("DEBUG_DETAIL_NOSE");
            cmd.DisableShaderKeyword("DEBUG_SHAPE_DENSITY_UV");
            if (!string.IsNullOrEmpty(debugShapeNose))
            {
                cmd.EnableShaderKeyword(debugShapeNose);    
            }
            
        }

        private void EnableShapeKeyWord(CommandBuffer cmd,string shapeBox)
        {
            cmd.DisableShaderKeyword("SHAPE_BOX");
            cmd.DisableShaderKeyword("SHAPE_SPHERE");
            cmd.EnableShaderKeyword(shapeBox);
        }


        private void CreateMaterial(string shaderName)
        {
            if (_material != null)
            {
                CoreUtils.Destroy(_material);
            }
        
            var shader = Shader.Find(shaderName);
            _material = CoreUtils.CreateEngineMaterial(shader);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    RayMarchCloudPass _mScriptableCloudPass;

    /// <inheritdoc/>
    public override void Create()
    {
        _mScriptableCloudPass = new RayMarchCloudPass();

        // Configures where the render pass should be injected.
        _mScriptableCloudPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents-10;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _mScriptableCloudPass.SetUp(renderer.cameraColorTarget);
        renderer.EnqueuePass(_mScriptableCloudPass);
    }
}


