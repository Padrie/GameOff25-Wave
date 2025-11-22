using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class ForceDirectX11 : MonoBehaviour
{
    [MenuItem("Build/Force DirectX 11")]
    static void SetDirectX11()
    {
        // For 64-bit Windows builds
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });

        // For 32-bit Windows builds (if needed)
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows, new[] { GraphicsDeviceType.Direct3D11 });

        Debug.Log("DirectX 11 has been set as the only graphics API for Windows builds");
    }
}