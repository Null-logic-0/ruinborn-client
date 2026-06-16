using UnityEngine;

public class WeaponAttacher : MonoBehaviour
{
    [SerializeField] private Animator survivalistAnimator;
    [SerializeField] private GameObject[] weapons;

    [Header("Offset")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    void Start()
    {
        if (survivalistAnimator == null)
        {
            Debug.LogError("[WeaponAttacher] No animator assigned");
            return;
        }

        Transform rightHand = survivalistAnimator.GetBoneTransform(HumanBodyBones.RightHand);

        if (rightHand == null)
        {
            Debug.LogWarning("[WeaponAttacher] Right hand bone not found");
            return;
        }

        foreach (var weapon in weapons)
        {
            if (weapon == null) continue;
            weapon.transform.SetParent(rightHand, false);
            weapon.transform.localPosition = positionOffset;
            weapon.transform.localRotation = Quaternion.Euler(rotationOffset);
        }

        Debug.Log($"[WeaponAttacher] Weapons attached to: {rightHand.name}");
    }
}