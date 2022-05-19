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


        public bool debug_shape_noise;
        [Range(0,1)]
        public float debug_shape_z;
    }
}