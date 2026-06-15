using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("Player HP")]
    [SerializeField] private Image playerFill;

    [Header("Enemy HP")]
    [SerializeField] private Image enemyFill;

    private const int MaxHP = 100;

    void Awake() => Instance = this;

    void OnEnable()
    {
        RuinbornNetwork.OnHpUpdate   += HandleHpUpdate;
        RuinbornNetwork.OnPlayerDied += HandlePlayerDied;
    }

    void OnDisable()
    {
        RuinbornNetwork.OnHpUpdate   -= HandleHpUpdate;
        RuinbornNetwork.OnPlayerDied -= HandlePlayerDied;
    }

    void HandleHpUpdate(string playerId, int hp)
    {
        bool isLocal = playerId == RuinbornNetwork.Instance?.PlayerId;
        if (isLocal) SetPlayerHP(hp);
        else         SetEnemyHP(hp);
    }

    void HandlePlayerDied(string playerId, string killerId)
    {
        bool isLocal = playerId == RuinbornNetwork.Instance?.PlayerId;

        if (isLocal)
        {
            SetPlayerHP(0);
            SetBarGray(playerFill);   //  gray on death
        }
        else
        {
            SetEnemyHP(0);
            SetBarGray(enemyFill);    // gray on death
        }
    }

    public void SetPlayerHP(int hp)
    {
        if (playerFill == null) return;
        playerFill.fillAmount = Mathf.Clamp01((float)hp / MaxHP);
        playerFill.color      = hp > 30
            ? new Color(0.8f, 0.1f, 0.1f)
            : new Color(1.0f, 0.4f, 0.0f);
    }

    public void SetEnemyHP(int hp)
    {
        if (enemyFill == null) return;
        enemyFill.fillAmount = Mathf.Clamp01((float)hp / MaxHP);
        enemyFill.color      = hp > 30
            ? new Color(0.8f, 0.1f, 0.1f)
            : new Color(1.0f, 0.4f, 0.0f);
    }

    void SetBarGray(Image fill)
    {
        if (fill == null) return;
        fill.fillAmount = 1f;                          // full bar but gray
        fill.color      = new Color(0.4f, 0.4f, 0.4f); // dark gray
    }
}