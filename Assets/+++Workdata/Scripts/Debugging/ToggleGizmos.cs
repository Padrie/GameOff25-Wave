using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ToggleGizmos : MonoBehaviour
{
    public bool show3DIcons = true;
#if UNITY_EDITOR
    private void OnValidate()
    {
        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            sceneView.drawGizmos = show3DIcons;
            sceneView.Repaint();
        }
    }
#endif
}