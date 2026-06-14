using UnityEngine;

public class RemotePlayer : MonoBehaviour
{
    private Vector3 _targetPos;
    private bool    _hasTarget = false;

    const float InterpolationSpeed = 12f;

    void Start()
    {
        _targetPos = transform.position;
    }

    void Update()
    {
        if (!_hasTarget) return;

        // Smoothly move toward the server-confirmed position
        transform.position = Vector3.Lerp(
            transform.position,
            _targetPos,
            Time.deltaTime * InterpolationSpeed
        );

        // Face movement direction
        var dir = _targetPos - transform.position;
        if (dir.magnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    public void SetTargetPosition(Vector3 pos)
    {
        _targetPos  = pos;
        _hasTarget  = true;
    }
}