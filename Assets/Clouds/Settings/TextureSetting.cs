using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Clouds.Settings
{
    
    [CreateAssetMenu()]
    public class TextureSetting : ScriptableObject
    {

        [Range(8,128)]
        public int resolution = 8;
        
        public NoiseLayer[] noiseLayers = new NoiseLayer[1];
    }

    [System.Serializable]
    public class NoiseLayer
    {
        public bool enable;
        public int mask;
        public NoiseType noiseType;
        public int layerCount = 1;
        public Vector3 offset;
        [Min(0.0001f)]
        public float frequency = 1;
        [Min(0.0001f)]
        public float amplify = 1;
    }

    public enum NoiseType
    {
        Simplex,
        Worley,
    }
    
}

