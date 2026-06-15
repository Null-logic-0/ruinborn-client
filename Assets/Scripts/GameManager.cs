using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject remotePlayerPrefab;

    private Dictionary<string, RemotePlayer> _remotePlayers = new();

    void OnEnable()
    {
        RuinbornNetwork.OnPlayerJoined += HandlePlayerJoined;
        RuinbornNetwork.OnPlayerLeft   += HandlePlayerLeft;
        RuinbornNetwork.OnPlayerMoved  += HandlePlayerMoved;
        RuinbornNetwork.OnHpUpdate     += HandleHpUpdate;
        RuinbornNetwork.OnPlayerDied   += HandlePlayerDied;
    }

    void OnDisable()
    {
        RuinbornNetwork.OnPlayerJoined -= HandlePlayerJoined;
        RuinbornNetwork.OnPlayerLeft   -= HandlePlayerLeft;
        RuinbornNetwork.OnPlayerMoved  -= HandlePlayerMoved;
        RuinbornNetwork.OnHpUpdate     -= HandleHpUpdate;
        RuinbornNetwork.OnPlayerDied   -= HandlePlayerDied;
    }

    void HandlePlayerJoined(string playerId, int count)
    {
        if (playerId == RuinbornNetwork.Instance?.PlayerId) return;
        if (_remotePlayers.ContainsKey(playerId)) return;

        var go = Instantiate(remotePlayerPrefab, new Vector3(2f, 1f, 0f), Quaternion.identity);
        go.name = $"Remote_{playerId}";
        _remotePlayers[playerId] = go.GetComponent<RemotePlayer>();

        Debug.Log($"[Game] Spawned remote player: {playerId}");
    }

    void HandlePlayerLeft(string playerId, int count)
    {
        if (_remotePlayers.TryGetValue(playerId, out var rp))
        {
            Destroy(rp.gameObject);
            _remotePlayers.Remove(playerId);
            Debug.Log($"[Game] Removed remote player: {playerId}");
        }
    }

    void HandlePlayerMoved(string playerId, Vector3 position)
    {
        if (_remotePlayers.TryGetValue(playerId, out var rp))
            rp.SetTargetPosition(position);
    }

    void HandleHpUpdate(string playerId, int hp)
    {
        bool isLocal = playerId == RuinbornNetwork.Instance?.PlayerId;

        if (isLocal)
            FindAnyObjectByType<CombatController>()?.OnHit();
    }

    void HandlePlayerDied(string playerId, string killerId)
    {
        if (playerId == RuinbornNetwork.Instance?.PlayerId)
        {
            var tpc = FindAnyObjectByType<StarterAssets.ThirdPersonController>(); 
            if (tpc != null) tpc.enabled = false;

            var combat = FindAnyObjectByType<CombatController>();
                    Debug.Log($"[Game] CombatController found: {combat != null}");  

            if (combat != null) combat.enabled = false;

            Debug.Log("[Game] YOU DIED");
        }
        else
        {
            HandlePlayerLeft(playerId, 0);
            Debug.Log("[Game] ENEMY DEFEATED");
        }
    }
}