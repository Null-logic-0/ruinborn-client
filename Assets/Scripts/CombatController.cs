using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class CombatController : MonoBehaviour
{
    private Animator _animator;
    private int      _locomotionHash = 0;
    private bool     _isDead         = false;

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
            animations = new[] { "1Hand_Up_Attack_A_1", "1Hand_Up_Attack_A_2" }
        },
        new Weapon {
            name       = "Bat",
            animations = new[] { "1Hand_Up_Attack_A_1", "1Hand_Up_Attack_A_2" }
        }
    };

    private int      _currentWeapon = 0;
    private int      _comboIndex    = 0;
    private float    _cooldown      = 0f;

    private string[] hitAnimations = { "Hit_F_1_InPlace" };
    private bool     _isCrouching  = false;

    [SerializeField] private string crouchAnimation = "1Hand_Up_Crouch_F";
    [SerializeField] private string deathAnimation  = "HumanM@Death01";

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip   _swingClip;

   

    void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        HideAllWeapons();
        ShowCurrentWeapon();
    }

    void OnEnable()
    {
        RuinbornNetwork.OnPlayerDied += HandlePlayerDied;
    }

    void OnDisable()
    {
        RuinbornNetwork.OnPlayerDied -= HandlePlayerDied;
    }

    void HandlePlayerDied(string playerId, string killerId)
    {
        if (playerId != RuinbornNetwork.Instance?.PlayerId) return;
        Debug.Log("[Combat] Local player died — calling Die()");
        Die();
    }

    // Input

    void Update()
    {
        if (_isDead) return;

        _cooldown -= Time.deltaTime;

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
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0f) CycleWeapon(1);
            if (scroll < 0f) CycleWeapon(-1);

            if (mouse.leftButton.wasPressedThisFrame && _cooldown <= 0f && !_isCrouching)
            {
                _cooldown = 0.8f;
                Attack();
            }
        }
    }

    // Weapons

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

    // Attack

    void Attack()
    {
        StopAllCoroutines();

        if (_audioSource != null && _swingClip != null)
        _audioSource.PlayOneShot(_swingClip);  

        var    anims   = weapons[_currentWeapon].animations;
        string anim    = anims[_comboIndex % anims.Length];
        _comboIndex++;

        float duration = GetAnimationLength(anim);
        _cooldown      = duration * 0.6f;

        _animator.CrossFade(anim, 0.1f, 0);
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

    // Hit reaction

    public void OnHit()
    {
        if (_isDead) return;

        string anim = hitAnimations[Random.Range(0, hitAnimations.Length)];
        int    hash = Animator.StringToHash(anim);

        if (_animator.HasState(0, hash))
        {
            _animator.CrossFade(anim, 0.1f, 0);
            StartCoroutine(ReturnToMovement(0.5f));
        }
    }

    // Death

    public void Die()
    {

    if (_isDead) return;

    StopAllCoroutines();
    _isDead      = true;
    _cooldown    = 999f;
    _isCrouching = false;

    var tpc = GetComponent<StarterAssets.ThirdPersonController>();
    if (tpc != null) tpc.enabled = false; 

    // Disable CharacterController so it stops fighting gravity
    var cc = GetComponent<CharacterController>();
    if (cc != null) cc.enabled = false;

    if (_animator == null)
        _animator = GetComponentInChildren<Animator>();

    if (_animator == null)
    {
        StartCoroutine(FallDown());
        return;
    }

    int  hash     = Animator.StringToHash(deathAnimation);
    bool hasState = _animator.HasState(0, hash);

    Debug.Log($"[Combat] Die() — anim:'{deathAnimation}' hasState:{hasState}");

    if (hasState)
        _animator.CrossFade(deathAnimation, 0.2f, 0);
    else
        StartCoroutine(FallDown());
}

IEnumerator FallDown()
{
    // Freeze animation in current pose
    if (_animator != null)
        _animator.speed = 0f;

    float elapsed  = 0f;
    float duration = 0.2f;

    Vector3 startPos = transform.position;
    Vector3 endPos   = startPos - Vector3.up * 0.01f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
        transform.position = Vector3.Lerp(startPos, endPos, t);
        yield return null;
    }
}
    // Return to locomotion

    IEnumerator ReturnToMovement(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (_isDead) yield break;

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