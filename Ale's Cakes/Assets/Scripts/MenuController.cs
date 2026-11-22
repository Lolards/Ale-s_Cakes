using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [SerializeField] string gameScene = "Game";      // nombre exacto de tu escena de juego
    [SerializeField] string menuScene = "MainMenu";  // nombre exacto de la escena del menú

    public void PlayEasy()   { Play(GameMode.Easy); }
    public void PlayNormal() { Play(GameMode.Normal); }
    public void PlayHard()   { Play(GameMode.Hard); }
    public void PlayFree()   { Play(GameMode.Free); }

    void Play(GameMode mode)
    {
        GameSettings.Mode = mode;
        SceneManager.LoadScene(gameScene);
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(menuScene);
    }

    public void Retry()
    {
        SceneManager.LoadScene(gameScene); // el modo elegido se conserva en GameSettings
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}