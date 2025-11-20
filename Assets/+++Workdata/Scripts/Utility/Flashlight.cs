using UnityEngine;

public class Flashlight : MonoBehaviour
{
    public GameObject[] flashlightGameobjects;
    public KeyCode toggleKey = KeyCode.F;

    void Start()
    {
        for (int i = 0; i < flashlightGameobjects.Length; i++)
        {
            if (flashlightGameobjects[i] != null)
            {
                flashlightGameobjects[i].SetActive(false);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            for (int i = 0; i < flashlightGameobjects.Length; i++)
            {
                if (flashlightGameobjects[i] != null)
                {
                    flashlightGameobjects[i].SetActive(!flashlightGameobjects[i].activeSelf);
                }
            }
        }
    }
}
