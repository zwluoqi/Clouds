using UnityEngine;

namespace Clouds
{
    public class CloudBox:MonoBehaviour
    {
        [Min(0.01f)]
        public float samplerScale = 1;

        public Vector3 samplerOffset = Vector3.zero;
        [Range(0,1)]
        public float globalCoverage = 1;

        [Min(0.011f)]
        public float densityMultipler = 1;
        [Range(0.001f,1.0f)]
        public float densityThreshold;

        public Texture3D textureShape;

        [Range(1,128)]
        [Tooltip("云层光线步进数量")]
        public int numberStepCloud = 100;
        [Min(0.01f)]
        [Tooltip("光能强度")]
        public float lightPhaseValue = 1;
        [Min(0.01f)]
        [Tooltip("摄像机射向云层使对光线吸收率")]
        public float lightAbsorptionThroughCloud = 1;
        public Color _LightCol = Color.white;

        
        [Tooltip("光源射向云层使对光线吸收率")]
        [Min(0.01f)]
        public float lightAbsorptionTowardSun = 1;
        [Range(0.01f,1.0f)]
        [Tooltip("最低光线穿透阀值")]
        public float darknessThreshold = 0.01f;
        [Range(1,128)]
        [Tooltip("太阳光线步进数量")]
        public int numberStepLight = 100;
        
        public bool debug_shape_noise;
        [Range(0,1)]
        public float debug_shape_z;

        public RGBA_DEBUG debug_rgba = RGBA_DEBUG.ALL;
    }

    public enum RGBA_DEBUG
    {
        R,
        G,
        B,
        A,
        ALL,
    }
}