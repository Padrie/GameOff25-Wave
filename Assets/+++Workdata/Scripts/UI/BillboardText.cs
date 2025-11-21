using EasyPeasyFirstPersonController;
using UnityEngine;

public class BillboardText : MonoBehaviour
{
    private FirstPersonController firstPersonController;

    private Camera playerCamera;


    private void Awake()
    {
        firstPersonController = FindFirstObjectByType<FirstPersonController>();
    }

    void Start()
    {
        playerCamera = firstPersonController.cam;
    }

    void LateUpdate()
    {
        transform.LookAt(transform.position + playerCamera.transform.forward);
    }
}
