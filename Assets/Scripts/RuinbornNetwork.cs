using System;
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

    public static event Action<string, int> OnPlayerJoined;
    public static event Action<string, int> OnPlayerLeft;
    public static event Action<string, Vector3> OnPlayerMoved;


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
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        if (string.IsNullOrEmpty(playerId))
            playerId = "player_" + UnityEngine.Random.Range(1000, 9999);

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

    async void OnApplicationQuit()
    {
        if (_ws != null) await _ws.Close();
    }

    void HandleOpen()
    {
        Debug.Log("[Network] WebSocket open — joining match...");
        _ = JoinMatch();
    }

    async Task JoinMatch()
    {
        var topic = $"match:{matchId}";
        var msg   = new JArray { "1", NextRef(), topic, "phx_join", new JObject() };
        await _ws.SendText(msg.ToString(Newtonsoft.Json.Formatting.None));
    }

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
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Network] Parse error: {e.Message}\nRaw: {json}");
        }
    }

    public async Task Push(string eventName, JObject payload = null)
    {
        if (_ws == null || !_joined)
        {
            Debug.LogWarning("[Network] Push called before joining");
            return;
        }
        await SendRaw(null, $"match:{matchId}", eventName, payload ?? new JObject());
    }

    public string PlayerId => playerId;
    public bool   IsJoined => _joined;

    async Task SendRaw(string joinRef, string topic, string eventName, JObject payload)
    {
        var msg = new JArray { joinRef, NextRef(), topic, eventName, payload };
        await _ws.SendText(msg.ToString(Newtonsoft.Json.Formatting.None));
    }

    string NextRef() => (++_ref).ToString();
}
