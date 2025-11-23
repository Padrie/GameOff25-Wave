using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private static Dictionary<uiid, UIElement> uiElements = new();

    private void Awake()
    {
        Instance = this;
    }

    public static void RegisterUI(UIElement element)
    {
        uiElements[element.id] = element;
        if(element.id != uiid.MainMenuScene)
            element.gameObject.SetActive(false);
    }

    public void ShowUI(uiid id)
    {
        if (uiElements.TryGetValue(id, out var ui))
        {
            ui.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError($"UI {id} not found!");
        }
    }
    
    public void HideUI(uiid id)
    {
        if (uiElements.TryGetValue(id, out var ui))
        {
            ui.gameObject.SetActive(false);
        }
    }

    public bool IsUIActive(uiid id)
    {
        if (uiElements.TryGetValue(id, out var ui))
        {
            return ui.gameObject.activeSelf;
        }
        else
            return false;
    }
}
