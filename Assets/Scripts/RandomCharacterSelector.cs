using UnityEngine;

public class RandomCharacterSelector : MonoBehaviour
{
    [SerializeField] private GameObject[] variants;

    [SerializeField] private int forceIndex = 3; // -1 = random, 0-3 = specific variant

    public GameObject ActiveVariant { get; private set; }

    // void Awake()
    // {
    //     if (variants == null || variants.Length == 0) return;

    //     foreach (var v in variants)
    //         if (v != null) v.SetActive(false);

    //     int idx = Random.Range(0, variants.Length);
    //     variants[idx].SetActive(true);
    //     ActiveVariant = variants[idx];

    //     Debug.Log($"[Character] Variant {idx + 1} selected");
    // }
    void Awake()
{
    if (variants == null || variants.Length == 0) return;

    foreach (var v in variants)
        if (v != null) v.SetActive(false);

    int idx = forceIndex >= 0 && forceIndex < variants.Length
        ? forceIndex
        : Random.Range(0, variants.Length);

    variants[idx].SetActive(true);
    ActiveVariant = variants[idx];

    Debug.Log($"[Character] Variant {idx + 1} selected");
}
}