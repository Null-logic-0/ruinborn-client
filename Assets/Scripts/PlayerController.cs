using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed    = 5f;
    [SerializeField] private float sendRate = 0.05f;

    private CharacterController _cc;
    private Animator            _animator;
    private float               _sendTimer = 0f;

    void Start()
    {
        _cc       = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        float h = 0f;
        float v = 0f;

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h =  1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)  h = -1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)    v =  1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)  v = -1f;

        // Move relative to where the camera is facing
        Vector3 direction = Vector3.zero;
        if (FollowCamera.Instance != null)
            direction = (FollowCamera.Instance.Forward * v + FollowCamera.Instance.Right * h).normalized;
        else
            direction = new Vector3(h, 0f, v).normalized;

        // Apply movement
        Vector3 velocity = direction * speed;
        velocity.y       = -9.81f;
        _cc.Move(velocity * Time.deltaTime);

        // Smoothly rotate character to face movement direction
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation   = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }

        _animator?.SetFloat("Speed", direction.magnitude);

        _sendTimer += Time.deltaTime;
        if (_sendTimer >= sendRate)
        {
            _sendTimer = 0f;
            SendPosition();
        }
    }

    void SendPosition()
    {
        if (RuinbornNetwork.Instance == null || !RuinbornNetwork.Instance.IsJoined) return;

        var pos = transform.position;
        _ = RuinbornNetwork.Instance.Push("move", new JObject
        {
            ["pos"] = new JObject
            {
                ["x"] = Mathf.Round(pos.x * 100f) / 100f,
                ["y"] = Mathf.Round(pos.y * 100f) / 100f,
                ["z"] = Mathf.Round(pos.z * 100f) / 100f
            }
        });
    }
}