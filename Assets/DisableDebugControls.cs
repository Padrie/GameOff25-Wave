using UnityEngine;

public class DisableDebugControls : MonoBehaviour
{
    [SerializeField] private bool debugControls = false;
    public static bool debugControlsEnabled = false;
    
    
    void Start()
    {
        debugControlsEnabled = debugControls;
    }
}
