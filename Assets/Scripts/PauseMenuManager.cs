using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private Button     resumeButton;
    [SerializeField] private Button     mainMenuButton;
    [SerializeField] private Button     exitButton;

    private bool _isPaused = false;

    void Start()
    {
        if (pauseMenu != null) pauseMenu.SetActive(false);

        if (resumeButton   != null) resumeButton.onClick.AddListener(Resume);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
        if (exitButton     != null) exitButton.onClick.AddListener(ExitGame);
    }

    void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (_isPaused) Resume();
            else           Pause();
        }
    }

    void Pause()
    {
        _isPaused = true;
        if (pauseMenu != null) pauseMenu.SetActive(true);
        Time.timeScale = 0f; // freeze game
    }

    void Resume()
    {
        _isPaused = false;
        if (pauseMenu != null) pauseMenu.SetActive(false);
        Time.timeScale = 1f; // unfreeze game
    }

    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        StartCoroutine(GoToMenuWithCleanup());
    }

    System.Collections.IEnumerator GoToMenuWithCleanup()
    {
        if (RuinbornNetwork.Instance != null)
            yield return StartCoroutine(RuinbornNetwork.Instance.CloseConnection());

        SceneManager.LoadScene("MainMenu");
    }

    void ExitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}