using UnityEngine;
using UnityEngine.InputSystem;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance         = 5f;
    [SerializeField] private float height           = 1.5f;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float smoothSpeed      = 10f;

    private float _yaw;
    private float _pitch = 20f;

    public static FollowCamera Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        Cursor.lockState = CursorLockMode.Locked;  // hide cursor during play
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Rotate camera with mouse
        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            _yaw   += delta.x * mouseSensitivity;
            _pitch -= delta.y * mouseSensitivity;
            _pitch  = Mathf.Clamp(_pitch, -10f, 50f);
        }

        // Position camera behind and above character
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3    offset   = rotation * new Vector3(0f, 0f, -distance);
        Vector3    lookAt   = target.position + Vector3.up * height;

        transform.position = Vector3.Lerp(
            transform.position,
            lookAt + offset,
            smoothSpeed * Time.deltaTime
        );

        transform.LookAt(lookAt);

        // Unlock cursor with Escape
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            Cursor.lockState = CursorLockMode.None;
    }

    // Used by PlayerController to move relative to camera direction
    public Vector3 Forward => new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
    public Vector3 Right   => new Vector3(transform.right.x,   0f, transform.right.z).normalized;
}