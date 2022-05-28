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
        private static readonly int TmpTexId2 = Shader.PropertyToID("_TmpTex2");
        // private static readonly int TmpTexId3 = Shader.PropertyToID("_TmpTex3");
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
            cmd.GetTemporaryRT(TmpTexId,w/2,h/2,0,FilterMode.Point, RenderTextureFormat.Default);
            cmd.GetTemporaryRT(TmpTexId2,w,h,0,FilterMode.Point, RenderTextureFormat.Default);
            // cmd.GetTemporaryRT(TmpTexId3,w,h,0,FilterMode.Point, RenderTextureFormat.Default);
            
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
                var transform = box.transform;
                Vector3 boxcenter = transform.position;
                var localScale = transform.localScale;


                
                
                cmd.SetGlobalFloat("samplerScale",box.samplerScale);
                cmd.SetGlobalVector("samplerOffset",box.samplerOffset);
                cmd.SetGlobalFloat("globalCoverage",box.globalCoverage);
                cmd.SetGlobalFloat("densityMultipler",box.densityMultipler);
                cmd.SetGlobalFloat("densityThreshold",box.densityThreshold);
                cmd.SetGlobalInt("numberStepCloud",box.numberStepCloud);
                
                
                cmd.SetGlobalFloat("lightPhaseValue",box.lightPhaseValue);
                cmd.SetGlobalFloat("lightAbsorptionThroughCloud",box.lightAbsorptionThroughCloud);
                
                
                cmd.SetGlobalFloat("lightAbsorptionTowardSun",box.lightAbsorptionTowardSun);
                cmd.SetGlobalFloat("darknessThreshold",box.darknessThreshold);
                cmd.SetGlobalInt("numberStepLight",box.numberStepLight);
                
                
                
                
                cmd.SetGlobalTexture("shapeNoise",box.textureShape);
                cmd.SetGlobalFloat("debug_shape_z",box.debug_shape_z);
                cmd.SetGlobalInt("debug_rgba",(int)box.debug_rgba);
                
                if (box.debug_shape_noise)
                {
                    cmd.EnableShaderKeyword("DEBUG_SHAPE_NOSE");
                }
                else
                {
                    cmd.DisableShaderKeyword("DEBUG_SHAPE_NOSE");
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
                    float raidu1 = raidu0 * 1.1f;
                    
                    cmd.SetGlobalVector("boxmin", new Vector4(raidu0,0,0,0));
                    cmd.SetGlobalVector("boxmax", new Vector4(raidu1,0,0,0));
                    cmd.SetGlobalVector("sphereCenter", boxcenter);
                }
                

                cmd.Blit(null,TmpTexId,_material,0);
                cmd.SetGlobalTexture(MainTexId,soruce);
                cmd.SetGlobalTexture("_CloudTex",TmpTexId);
                cmd.Blit(soruce,TmpTexId2,_material,1);
                cmd.Blit(TmpTexId2, soruce);
            }
            context.ExecuteCommandBuffer(cmd);

            CommandBufferPool.Release(cmd);

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


