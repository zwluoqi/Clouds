using System.Collections;
using System.Collections.Generic;
using Clouds.Settings;
using Unity.Mathematics;
using UnityEngine;
using UnityTools.Algorithm.Noise;


namespace Clouds
{
    public class GenerateTexture
    {
        public static Texture2D Generate(TextureSetting textureSetting)
        {
            Texture2D texture2D = new Texture2D(textureSetting.resolution, textureSetting.resolution);
            Color[] colors = new Color[textureSetting.resolution*textureSetting.resolution];
            int i = 0;
            for (int x = 0; x < textureSetting.resolution; x++)
            {
                for (int z = 0; z < textureSetting.resolution; z++)
                {
                    var pos = Vector3.right*(x-textureSetting.resolution/2)
                              +  Vector3.up
                              +  Vector3.forward*(z-textureSetting.resolution/2);
                    float v = 0;
                    for (int layer = 0; layer < textureSetting.noiseLayers.Length; layer++)
                    {
                        v += NoiseGenerate(pos,textureSetting.noiseLayers[layer]);
                    }
                    colors[i++] = new Color(v, v, v, v);
                }
            }
            texture2D.SetPixels(colors);
            texture2D.Apply();
            return texture2D;
        }

        private static float NoiseGenerate(Vector3 pos,NoiseLayer textureSettingNoiseLayer)
        {
            pos += textureSettingNoiseLayer.offset;
            float v = 0;
            if (textureSettingNoiseLayer.noiseType == NoiseType.Simplex)
            {
                v = SimplexNoise3D.snoise(textureSettingNoiseLayer.frequency * pos);
            }
            else
            {
                v = WorleyNoise3D.worley(textureSettingNoiseLayer.frequency * pos).x;
            }

            v = Mathf.Abs(v);
            v *= textureSettingNoiseLayer.amplify;
            return v;
        }
    }
}