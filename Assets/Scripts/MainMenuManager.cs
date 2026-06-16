using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button exitButton;

    void Start()
    {
        if (playButton != null)
            playButton.onClick.AddListener(PlayGame);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
    }

    void PlayGame()
    {
        SceneManager.LoadScene("SampleScene");
    }

    void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}