using TMPro;
using UnityEngine;

public class Credits : MonoBehaviour
{
    public TMP_Text markText;
    public TMP_Text patricText;

    public void DoUnderscoreMark()
    {
        markText.color = Color.red;
    }

    public void DontUnderscoreMark()
    {
        markText.color = Color.white;
    }

    public void DoUnderscorePatric()
    {
        patricText.color = Color.red;
    }

    public void DontUnderscorePatric()
    {
        patricText.color = Color.white;
    }

    public void OpenURL(string url)
    {
        Application.OpenURL(url);
    }
}
