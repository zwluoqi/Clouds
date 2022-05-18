using UnityEditor;
using UnityEngine;

namespace Clouds
{
    [CustomEditor(typeof(ShowTexture))]
    public class ShowTextureEditor : Editor
    {
        private SettingEditor<ShowTexture> shapeEdirot;

        private void OnEnable()
        {
            shapeEdirot = new SettingEditor<ShowTexture>();
            shapeEdirot.OnEnable(this);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            shapeEdirot.OnInspectorGUI(this);
            
            if (GUILayout.Button("生成图片"))
            {
                (target as ShowTexture).GenerateTexture();
            }
        }
        
    }
}