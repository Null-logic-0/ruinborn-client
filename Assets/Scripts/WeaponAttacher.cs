using UnityEngine;

public class WeaponAttacher : MonoBehaviour
{
    [SerializeField] private GameObject[] weapons;

    [Header("Offset")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    void Start()
    {
        var selector = GetComponent<RandomCharacterSelector>();
        if (selector == null || selector.ActiveVariant == null)
        {
            Debug.LogWarning("[WeaponAttacher] No active variant found");
            return;
        }

        var anim = selector.ActiveVariant.GetComponent<Animator>();
        if (anim == null) return;

        Transform rightHand = anim.GetBoneTransform(HumanBodyBones.RightHand);
        if (rightHand == null)
        {
            Debug.LogWarning("[WeaponAttacher] Right hand not found");
            return;
        }

        foreach (var weapon in weapons)
        {
            if (weapon == null) continue;
            weapon.transform.SetParent(rightHand, false);
            weapon.transform.localPosition = positionOffset;
            weapon.transform.localRotation = Quaternion.Euler(rotationOffset);
        }

        Debug.Log($"[WeaponAttacher] Weapons → {rightHand.name}");
    }
}