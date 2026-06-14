using Newtonsoft.Json.Linq;
using UnityEngine;

public class NetworkSender : MonoBehaviour
{
    [SerializeField] private float sendRate = 0.05f;
    private float _timer = 0f;

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= sendRate)
        {
            _timer = 0f;
            SendPosition();
        }
    }

    void SendPosition()
    {
        if (RuinbornNetwork.Instance == null || !RuinbornNetwork.Instance.IsJoined) return;

        var pos = transform.position;
        var rot = transform.eulerAngles.y;

        _ = RuinbornNetwork.Instance.Push("move", new JObject
        {
            ["pos"] = new JObject
            {
                ["x"] = Mathf.Round(pos.x * 100f) / 100f,
                ["y"] = Mathf.Round(pos.y * 100f) / 100f,
                ["z"] = Mathf.Round(pos.z * 100f) / 100f
            },
            ["rot"] = Mathf.Round(rot * 10f) / 10f
        });
    }
}