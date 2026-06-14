using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class CombatController : MonoBehaviour
{
    private Animator _animator;
    private int      _locomotionHash = 0;

    [System.Serializable]
    public class Weapon
    {
        public string     name;
        public GameObject model;
        public string[]   animations;
    }

    [SerializeField] private Weapon[] weapons = {
        new Weapon {
            name       = "Axe",
            animations = new[] { "1Hand_Up_Attack_A_1_InPlace", "1Hand_Up_Attack_A_2_InPlace" }
        },
        new Weapon {
            name       = "Bat",
            animations = new[] { "1Hand_Up_Attack_A_1", "1Hand_Up_Attack_A_2" }
        }
    };

    private int   _currentWeapon = 0;
    private int   _comboIndex    = 0;
    private float _cooldown      = 0f;

    private string[] hitAnimations = { "Hit_F_1_InPlace", "Hit_F_2_InPlace" };

    private bool   _isCrouching   = false;
    [SerializeField] private string crouchAnimation = "1Hand_Up_Crouch_F";


    void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        HideAllWeapons();
        ShowCurrentWeapon();
    }

    void Update()
    {
        _cooldown -= Time.deltaTime;

        // Continuously capture locomotion state hash when not attacking
        if (!_isCrouching)
        {
            var info = _animator.GetCurrentAnimatorStateInfo(0);
            if (!IsAttackState(info))
                _locomotionHash = info.shortNameHash;
        }

        var kb    = Keyboard.current;
        var mouse = Mouse.current;

        if (kb != null)
        {
            // C = crouch toggle
            if (kb.cKey.wasPressedThisFrame)
            {
                _isCrouching = !_isCrouching;
                if (_isCrouching)
                    _animator.CrossFade(crouchAnimation, 0.2f, 0);
                else
                    StartCoroutine(ReturnToMovement(0f));
            }
        }

        if (mouse != null)
        {
            // Scroll wheel = switch weapon
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0f) CycleWeapon(1);
            if (scroll < 0f) CycleWeapon(-1);

            // Left click = attack
            if (mouse.leftButton.wasPressedThisFrame && _cooldown <= 0f && !_isCrouching)
            {
                _cooldown = 0.8f;
                Attack();
            }
        }
    }


    void CycleWeapon(int direction)
    {
        _currentWeapon = (_currentWeapon + direction + weapons.Length) % weapons.Length;
        _comboIndex    = 0;
        HideAllWeapons();
        ShowCurrentWeapon();
        Debug.Log($"[Combat] Weapon: {weapons[_currentWeapon].name}");
    }

    void HideAllWeapons()
    {
        foreach (var w in weapons)
            if (w.model != null) w.model.SetActive(false);
    }

    void ShowCurrentWeapon()
    {
        var w = weapons[_currentWeapon];
        if (w.model != null)
            w.model.SetActive(true);
        else
            Debug.LogWarning($"[Combat] No model assigned for: {w.name}");
    }


    void Attack()
    {
        var    anims = weapons[_currentWeapon].animations;
        string anim  = anims[_comboIndex % anims.Length];
        _comboIndex++;

        _animator.CrossFade(anim, 0.1f, 0);

        float duration = GetAnimationLength(anim);
        StartCoroutine(ReturnToMovement(duration));

        if (RuinbornNetwork.Instance?.IsJoined == true)
        {
            var pos = transform.position;
            _ = RuinbornNetwork.Instance.Push("attack", new JObject
            {
                ["weapon"] = _currentWeapon,
                ["pos"]    = new JObject
                {
                    ["x"] = pos.x,
                    ["y"] = pos.y,
                    ["z"] = pos.z
                }
            });
        }
    }

    float GetAnimationLength(string animName)
    {
        if (_animator.runtimeAnimatorController == null) return 0.8f;

        foreach (var clip in _animator.runtimeAnimatorController.animationClips)
            if (clip.name == animName)
                return Mathf.Max(0.1f, clip.length - 0.2f);

        Debug.LogWarning($"[Combat] Clip '{animName}' not found — using 0.8s");
        return 0.8f;
    }

    bool IsAttackState(AnimatorStateInfo info)
    {
        foreach (var w in weapons)
            if (w.animations != null)
                foreach (var anim in w.animations)
                    if (info.IsName(anim)) return true;
        return false;
    }


    public void OnHit()
    {
        _animator.CrossFade(hitAnimations[Random.Range(0, hitAnimations.Length)], 0.1f, 0);
        StartCoroutine(ReturnToMovement(0.5f));
    }

   

    IEnumerator ReturnToMovement(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (_isCrouching)
        {
            _animator.CrossFade(crouchAnimation, 0.2f, 0);
            yield break;
        }

        if (_locomotionHash != 0)
            _animator.Play(_locomotionHash, 0, 0f);
        else
            Debug.LogWarning("[Combat] No locomotion hash captured yet");
    }
}