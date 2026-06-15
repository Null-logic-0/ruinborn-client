using System;
using System.Collections;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class RuinbornNetwork : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] private string serverUrl = "ws://localhost:4000/socket/websocket";
    [SerializeField] private string matchId   = "room_1";

    [Header("Player")]
    [SerializeField] private string playerId  = "";

    //  Events 
    public static event Action<string, int>     OnPlayerJoined;
    public static event Action<string, int>     OnPlayerLeft;
    public static event Action<string, Vector3> OnPlayerMoved;
    public static event Action<string, int>     OnHpUpdate;
    public static event Action<string, string>  OnPlayerDied;
    public static event Action<int>             OnCountdown;
    public static event Action                  OnMatchStart;
    public static event Action<string, string>  OnMatchEnded;

    public static RuinbornNetwork Instance { get; private set; }

    private WebSocket _ws;
    private int       _ref           = 0;
    private float     _heartbeatTime = 0f;
    private bool      _joined        = false;

    const float HeartbeatInterval = 30f;

   

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    async void Start()
    {
        // Persist player ID across scene reloads
        if (string.IsNullOrEmpty(playerId))
        {
            playerId = PlayerPrefs.GetString("ruinborn_player_id", "");

            if (string.IsNullOrEmpty(playerId))
            {
                playerId = "player_" + UnityEngine.Random.Range(1000, 9999);
                PlayerPrefs.SetString("ruinborn_player_id", playerId);
                PlayerPrefs.Save();
            }
        }

        await Connect();
    }

    async Task Connect()
    {
        var url = $"{serverUrl}?player_id={Uri.EscapeDataString(playerId)}&vsn=2.0.0";
        Debug.Log($"[Network] Connecting as '{playerId}'");

        _ws           = new WebSocket(url);
        _ws.OnOpen    += HandleOpen;
        _ws.OnMessage += HandleMessage;
        _ws.OnError   += e    => Debug.LogError($"[Network] Error: {e}");
        _ws.OnClose   += code => Debug.Log($"[Network] Closed ({code})");

        await _ws.Connect();
    }

    void Update()
    {
        _ws?.DispatchMessageQueue();

        if (!_joined) return;
        _heartbeatTime += Time.deltaTime;
        if (_heartbeatTime >= HeartbeatInterval)
        {
            _heartbeatTime = 0f;
            _ = SendRaw(null, "phoenix", "heartbeat", new JObject());
        }
    }

    async void OnDestroy()
    {
        if (_ws != null)
        {
            _ws.OnOpen    -= HandleOpen;
            _ws.OnMessage -= HandleMessage;
            await _ws.Close();
            _ws = null;
        }
    }

    async void OnApplicationQuit()
    {
        if (_ws != null) await _ws.Close();
    }

    // Connection
    void HandleOpen()
    {
        Debug.Log("[Network] WebSocket open — joining match...");
        _ = JoinMatch();
    }

    async Task JoinMatch()
    {
        _ref = 0;
        var topic = $"match:{matchId}";
        var msg   = new JArray { "1", NextRef(), topic, "phx_join", new JObject() };
        await _ws.SendText(msg.ToString(Newtonsoft.Json.Formatting.None));
    }

    IEnumerator RetryJoin(float delay)
    {
        Debug.Log($"[Network] Retrying join in {delay}s...");
        yield return new WaitForSeconds(delay);
        Debug.Log("[Network] Retrying join now");
        _ = JoinMatch();
    }

    // Messages 

    void HandleMessage(byte[] bytes)
    {
        var json = System.Text.Encoding.UTF8.GetString(bytes);
        try
        {
            var msg       = JArray.Parse(json);
            var eventName = msg[3]?.ToString();
            var payload   = msg[4] as JObject ?? new JObject();

            switch (eventName)
            {
                case "phx_reply":
                    if (payload["status"]?.ToString() == "ok")
                    {
                        _joined = true;
                        Debug.Log($"[Network] ✓ Joined match:{matchId} as {playerId}");
                    }
                    else if (payload["response"]?["reason"]?.ToString() == "room_full")
                    {
                        Debug.LogWarning("[Network] Room full — retrying in 3s...");
                        StartCoroutine(RetryJoin(3f));
                    }
                    break;

                case "player_joined":
                    var joinedId    = payload["player_id"]?.ToString();
                    var joinedCount = payload["player_count"]?.Value<int>() ?? 0;
                    Debug.Log($"[Network] player_joined → {joinedId} ({joinedCount} in room)");
                    OnPlayerJoined?.Invoke(joinedId, joinedCount);
                    break;

                case "player_left":
                    var leftId    = payload["player_id"]?.ToString();
                    var leftCount = payload["player_count"]?.Value<int>() ?? 0;
                    Debug.Log($"[Network] player_left → {leftId} ({leftCount} in room)");
                    OnPlayerLeft?.Invoke(leftId, leftCount);
                    break;

                case "player_moved":
                    var movedId = payload["player_id"]?.ToString();
                    var p       = payload["pos"];
                    if (movedId != null && p != null)
                    {
                        var position = new Vector3(
                            p["x"]?.Value<float>() ?? 0f,
                            p["y"]?.Value<float>() ?? 0f,
                            p["z"]?.Value<float>() ?? 0f
                        );
                        OnPlayerMoved?.Invoke(movedId, position);
                    }
                    break;

                case "hp_update":
                    var hpPlayerId = payload["player_id"]?.ToString();
                    var hp         = payload["hp"]?.Value<int>() ?? 0;
                    OnHpUpdate?.Invoke(hpPlayerId, hp);
                    break;

                case "player_died":
                    var diedId   = payload["player_id"]?.ToString();
                    var killerId = payload["killer_id"]?.ToString();
                    OnPlayerDied?.Invoke(diedId, killerId);
                    break;

                case "countdown":
                    var seconds = payload["seconds"]?.Value<int>() ?? 0;
                    Debug.Log($"[Network] Countdown: {seconds}");
                    OnCountdown?.Invoke(seconds);
                    break;

                case "match_start":
                    Debug.Log("[Network] FIGHT!");
                    OnMatchStart?.Invoke();
                    break;

                case "match_ended":
                    var winnerId = payload["winner_id"]?.ToString();
                    var loserId  = payload["loser_id"]?.ToString();
                    Debug.Log($"[Network] Match ended — winner: {winnerId}");
                    OnMatchEnded?.Invoke(winnerId, loserId);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Network] Parse error: {e.Message}\nRaw: {json}");
        }
    }

    //  Public API
    public async Task Push(string eventName, JObject payload = null)
    {
        if (_ws == null || !_joined)
        {
            Debug.LogWarning("[Network] Push called before joining");
            return;
        }
        await SendRaw(null, $"match:{matchId}", eventName, payload ?? new JObject());
    }

    public IEnumerator CloseConnection()
    {
        if (_ws != null)
        {
            _ws.OnOpen    -= HandleOpen;
            _ws.OnMessage -= HandleMessage;

            var   task    = _ws.Close();
            float timeout = 2f;
            float elapsed = 0f;

            while (!task.IsCompleted && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            _ws     = null;
            _joined = false;
            Debug.Log("[Network] Connection closed cleanly");
        }
    }

    public string PlayerId => playerId;
    public bool   IsJoined => _joined;

    // Private 

    async Task SendRaw(string joinRef, string topic, string eventName, JObject payload)
    {
        var msg = new JArray { joinRef, NextRef(), topic, eventName, payload };
        await _ws.SendText(msg.ToString(Newtonsoft.Json.Formatting.None));
    }

    string NextRef() => (++_ref).ToString();
}