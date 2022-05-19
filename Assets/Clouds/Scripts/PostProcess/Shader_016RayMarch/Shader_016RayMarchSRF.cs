using System;
using Clouds;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public class Shader_016RayMarchSRF : ScriptableRendererFeature
{
    class Shader_016RayMarchPass : ScriptableRenderPass
    {

        public static string k_RenderTag = "Shader_016RayMarch";
        static string shaderName = "Shader/Shader_016RayMarch";
        // private Shader_016RayMarchVolume volume;
        private RenderTargetIdentifier _renderTargetIdentifier;
        private Material _material;

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int TmpTexId = Shader.PropertyToID("_TmpTex");
        //
        // private static readonly int boxminId = Shader.PropertyToID("boxmin");
        // private static readonly int boxmaxId = Shader.PropertyToID("boxmax");
        // private static readonly int _alphaMultiplier = Shader.PropertyToID("_alphaMultiplier");
        // private static readonly int _colorMultiplier = Shader.PropertyToID("_colorMultiplier");
        // private static readonly int _fogMultiplier = Shader.PropertyToID("_fogMultiplier");
        //
        // private static readonly int _waterSmoothness = Shader.PropertyToID("_waterSmoothness");
        //
        // private static readonly int depthColor = Shader.PropertyToID("depthColor");
        // private static readonly int surfaceColor = Shader.PropertyToID("surfaceColor");
        //
        // private static readonly int waveLen = Shader.PropertyToID("waveLen");
        //
        // private static readonly int waves = Shader.PropertyToID("waves");
        //
        //
        //
        // private static readonly int mouseFocusPoint = Shader.PropertyToID("mouseFocusPoint");

        // private RenderTextureDescriptor _cameraTextureDescriptor;


        public Shader_016RayMarchPass()
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
            
            var soruce = _renderTargetIdentifier;
            cmd.GetTemporaryRT(TmpTexId,w,h,0,FilterMode.Point, RenderTextureFormat.Default);

            
            for (int i = 0; i < cloudBoxes.Length; i++)
            {
                var box = cloudBoxes[i];
                // if (!planetMesh.ColorSettting.postProcessOcean)
                // {
                //     continue;
                // }
                //
                // _material = planetMesh.WaterRenderSettting.postMaterial;
                // if (_material == null)
                // {
                //     continue;
                // }
                // cmd.SetGlobalVector(centerPos,planetMesh.transform.position);
                // cmd.SetGlobalFloat(radius,planetMesh.ShapeSettting.radius);
                // cmd.SetGlobalFloat(_alphaMultiplier,planetMesh.WaterRenderSettting.alphaMultiplier);
                // cmd.SetGlobalFloat(_colorMultiplier,planetMesh.WaterRenderSettting.colorMultiplier);
                // cmd.SetGlobalFloat(_fogMultiplier,planetMesh.WaterRenderSettting.fogMultiplier);
                //
                // cmd.SetGlobalFloat(_waterSmoothness,planetMesh.WaterRenderSettting.waterSmoothness);
                //
                // cmd.SetGlobalColor(depthColor,planetMesh.ColorSettting.ocean.Evaluate(0));
                // cmd.SetGlobalColor(surfaceColor,planetMesh.ColorSettting.ocean.Evaluate(1));
                //
                // cmd.SetGlobalFloat(waveLen,planetMesh.WaterRenderSettting.waterLayers.Length);
                // cmd.SetGlobalVectorArray(waves,planetMesh.WaterRenderSettting.ToWaveVec4s());
                var transform = box.transform;
                Vector3 boxcenter = transform.position;
                var localScale = transform.localScale;
                Vector3 boxmin = boxcenter - localScale*0.5f;
                Vector3 boxmax = boxcenter + localScale*0.5f;

                cmd.SetGlobalVector("boxmin", boxmin);
                cmd.SetGlobalVector("boxmax", boxmax);
                cmd.SetGlobalFloat("samplerScale",box.samplerScale);
                cmd.SetGlobalVector("samplerOffset",box.samplerOffset);
                cmd.SetGlobalFloat("globalCoverage",box.globalCoverage);
                cmd.SetGlobalFloat("densityMultipler",box.densityMultipler);
                cmd.SetGlobalFloat("densityThreshold",box.densityThreshold);
                
                cmd.SetGlobalTexture("shapeNoise",box.textureShape);
                cmd.SetGlobalFloat("debug_shape_z",box.debug_shape_z);
                if (box.debug_shape_noise)
                {
                    _material.EnableKeyword("DEBUG_SHAPE_NOSE");
                }
                else
                {
                    _material.DisableKeyword("DEBUG_SHAPE_NOSE");
                }

                cmd.SetGlobalTexture(MainTexId,soruce);
                cmd.Blit(soruce,TmpTexId);
                cmd.Blit(TmpTexId,soruce,_material,0);
                
            }
            context.ExecuteCommandBuffer(cmd);

            CommandBufferPool.Release(cmd);

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

    Shader_016RayMarchPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new Shader_016RayMarchPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_ScriptablePass.SetUp(renderer.cameraColorTarget);
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


