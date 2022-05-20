using UnityEditor;
using UnityEngine;

namespace Clouds
{
    [CustomEditor(typeof(TestRayBox))]
    public class TestRayBoxEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            
            base.OnInspectorGUI();
            TestRayBox t = target as TestRayBox;
            var transform = t.transform;
            var boxTrans = t.boxcollider.transform;
            var boxPos = boxTrans.position;
            var distance = RayIntersection.RayBoxIntersection(transform.position,
                transform.forward,
                t.boxmin,
                t.boxmax
            );

            t.hitInfo = distance;
        }
    }
}