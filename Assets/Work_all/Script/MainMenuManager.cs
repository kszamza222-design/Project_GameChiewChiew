using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Name")]
    public string firstLevelName = "Map1";

    // °¥ªÿË¡ Play
    public void PlayGame()
    {
        SceneManager.LoadScene(firstLevelName);
    }

    // °¥ªÿË¡ Exit
    public void ExitGame()
    {
        Debug.Log("Exit Game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}