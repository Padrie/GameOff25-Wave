using EasyPeasyFirstPersonController;
using UnityEngine;

public class BillboardText : MonoBehaviour
{
    private FirstPersonController firstPersonController;

    private Camera playerCamera;

    private void Start()
    {
        playerCamera = Camera.main;
    }

    void LateUpdate()
    {
        transform.LookAt(transform.position + playerCamera.transform.forward);
    }
}
