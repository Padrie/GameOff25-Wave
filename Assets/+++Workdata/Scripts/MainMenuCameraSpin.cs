using UnityEngine;

public class MainMenuCameraSpin : MonoBehaviour
{
    [SerializeField] private GameObject cameraParent;

    [SerializeField] private float rotationSpeed = 5f;

    private float randomInt;

    void Start()
    {
        if (cameraParent == null)
        {
            Debug.LogError("Camera Parent is not assigned in MainMenuCameraSpin script.");
        }
        else
        {
            // Random starting rotation
            randomInt = Random.Range(0f, 359f);
            cameraParent.transform.rotation = Quaternion.Euler(0f, randomInt, 0f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        cameraParent.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
}
