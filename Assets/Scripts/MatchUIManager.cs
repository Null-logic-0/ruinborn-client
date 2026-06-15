using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MatchUIManager : MonoBehaviour
{
    public static MatchUIManager Instance { get; private set; }

    [Header("Waiting Screen")]
    [SerializeField] private GameObject waitingScreen;
    [SerializeField] private TMP_Text   waitingText;

    [Header("Countdown")]
    [SerializeField] private GameObject countdownScreen;
    [SerializeField] private TMP_Text   countdownText;

    [Header("End Screen")]
    [SerializeField] private GameObject endScreen;
    [SerializeField] private TMP_Text   endText;
    [SerializeField] private TMP_Text   subtitleText;
    [SerializeField] private Button     restartButton;

   

    void Awake() => Instance = this;

    void OnEnable()
    {
        RuinbornNetwork.OnPlayerJoined += HandlePlayerJoined;
        RuinbornNetwork.OnCountdown    += HandleCountdown;
        RuinbornNetwork.OnMatchStart   += HandleMatchStart;
        RuinbornNetwork.OnMatchEnded   += HandleMatchEnded;
    }

    void OnDisable()
    {
        RuinbornNetwork.OnPlayerJoined -= HandlePlayerJoined;
        RuinbornNetwork.OnCountdown    -= HandleCountdown;
        RuinbornNetwork.OnMatchStart   -= HandleMatchStart;
        RuinbornNetwork.OnMatchEnded   -= HandleMatchEnded;
    }

    void Start()
    {
        ShowWaiting();
        if (restartButton != null)
            restartButton.onClick.AddListener(Restart);
    }

    // Event Handlers 

    void HandlePlayerJoined(string playerId, int count)
    {
        if (waitingText != null)
            waitingText.text = count >= 2
                ? "Match starting..."
                : "Waiting for opponent...";
    }

    void HandleCountdown(int seconds)
    {
        Debug.Log($"[UI] HandleCountdown — {seconds}");
        if (waitingScreen   != null) waitingScreen.SetActive(false);
        if (countdownScreen != null) countdownScreen.SetActive(true);
        if (countdownText   != null) countdownText.text = seconds.ToString();
    }

    void HandleMatchStart()
    {
        Debug.Log("[UI] HandleMatchStart — FIGHT!");
        if (countdownText   != null) countdownText.text = "FIGHT!";
        if (countdownScreen != null) countdownScreen.SetActive(true);
        StartCoroutine(HideCountdownAfter(1f));
    }

    void HandleMatchEnded(string winnerId, string loserId)
    {
        if (endScreen != null) endScreen.SetActive(true);

        bool iWon = winnerId == RuinbornNetwork.Instance?.PlayerId;

        if (endText != null)
        {
            endText.text  = iWon ? "YOU WIN" : "YOU LOSE";
            endText.color = iWon
                ? new Color(1f, 0.84f, 0f)
                : new Color(0.8f, 0.1f, 0.1f);
        }

        if (subtitleText != null)
        {
            subtitleText.text = iWon
                ? $"You defeated {loserId}"
                : $"{winnerId} defeated you";
        }
    }

    // UI Helpers 

    void ShowWaiting()
    {
        if (waitingScreen   != null) waitingScreen.SetActive(true);
        if (countdownScreen != null) countdownScreen.SetActive(false);
        if (endScreen       != null) endScreen.SetActive(false);
    }

    IEnumerator HideCountdownAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (countdownScreen != null) countdownScreen.SetActive(false);
    }

    //  Restart

    void Restart()
    {
        StartCoroutine(RestartWithCleanup());
    }

    IEnumerator RestartWithCleanup()
    {
           Debug.Log("[UI] RestartWithCleanup started");

        if (RuinbornNetwork.Instance != null)
            yield return StartCoroutine(RuinbornNetwork.Instance.CloseConnection());

           Debug.Log("[UI] Connection closed — loading scene");

        yield return new WaitForSeconds(0.5f);

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}