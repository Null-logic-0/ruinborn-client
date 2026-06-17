using UnityEngine;

public class AnimatorSync : MonoBehaviour
{
    [SerializeField] private Animator source;
    private Animator _target;

    void Start()
    {
        _target = GetComponent<Animator>();
    }

    void LateUpdate()
{
    if (source == null || _target == null) return;

    foreach (var param in source.parameters)
    {
        switch (param.type)
        {
            case AnimatorControllerParameterType.Float:
                _target.SetFloat(param.nameHash, source.GetFloat(param.nameHash));
                break;
            case AnimatorControllerParameterType.Bool:
                _target.SetBool(param.nameHash, source.GetBool(param.nameHash));
                break;
            case AnimatorControllerParameterType.Int:
                _target.SetInteger(param.nameHash, source.GetInteger(param.nameHash));
                break;
        }
    }

    var srcState = source.GetCurrentAnimatorStateInfo(0);
    var dstState = _target.GetCurrentAnimatorStateInfo(0);

    if (srcState.shortNameHash != dstState.shortNameHash)
    {
        // Use Play instead of CrossFade for immediate sync
        _target.Play(srcState.shortNameHash, 0, srcState.normalizedTime);
        Debug.Log($"[Sync] Force state: {srcState.shortNameHash}");
    }
}
}