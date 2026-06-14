using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject remotePlayerPrefab;

    private Dictionary<string, RemotePlayer> _remotePlayers = new();

    // Subscribe to network events when this object is active
    void OnEnable()
    {
        RuinbornNetwork.OnPlayerJoined += HandlePlayerJoined;
        RuinbornNetwork.OnPlayerLeft   += HandlePlayerLeft;
        RuinbornNetwork.OnPlayerMoved  += HandlePlayerMoved;
    }

    void OnDisable()
    {
        RuinbornNetwork.OnPlayerJoined -= HandlePlayerJoined;
        RuinbornNetwork.OnPlayerLeft   -= HandlePlayerLeft;
        RuinbornNetwork.OnPlayerMoved  -= HandlePlayerMoved;
    }

    void HandlePlayerJoined(string playerId, int count)
    {
        // Skip ourselves
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
}