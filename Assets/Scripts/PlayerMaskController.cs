using UnityEngine;
using UnityEngine.UI; // UI iþlemleri için þart
using StarterAssets;
using UnityEngine.Rendering;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
[RequireComponent(typeof(AudioSource))]
public class PlayerMaskController : MonoBehaviour
{
    [Header("Mask Speed Settings")]
    public float thiefSpeed = 6.0f;
    public float guardSpeed = 3.0f;

    [Header("Mask Models (3D Objeler)")]
    public GameObject thiefMaskObject;
    public GameObject guardMaskObject;
    public GameObject statueMaskObject;

    [Header("UI Icons (Normal & Outlier)")]
    public Image thiefIconNormal;    // mask1
    public Image thiefIconOutlier;   // mask1outlier
    public Image guardIconNormal;    // mask2
    public Image guardIconOutlier;   // mask2outlier
    public Image statueIconNormal;   // mask3
    public Image statueIconOutlier;  // mask3outlier
    public float uiFadeSpeed = 0.2f; // Geçiþ hýzý (saniye)

    [Header("Guard Mask Duration")]
    public float maxGuardDuration = 5.0f;
    public float guardCooldownSpeed = 1.0f;
    public Slider guardDurationSlider;
    public GameObject guardBarContainer;

    [Range(0f, 1f)] public float audioVolume = 0.3f;

    [Header("Audio Settings")]
    public AudioClip guardWarningClip;
    public AudioClip guardRechargeClip;
    public AudioClip statueTransformClip;
    public float warningThreshold = 5.0f; // Ses kaç saniye kala çalsýn? (5 sn dediðin için 5.0f)
    private AudioSource _audioSource;
    private bool _hasPlayedWarningSound = false;

    [Header("Blackout Settings")]
    public Light sunLight;
    public Light statueSpotLight;
    public Camera mainCamera;

    public enum MaskType { Thief, Guard, Statue }
    public MaskType currentMask { get; private set; }

    private ThirdPersonController _starterController;
    private StarterAssetsInputs _input;
    private Animator _animator;

    private float _currentGuardTime;
    private bool _isGuardDepleted = false;

    // Render Ayarlarý
    private Material _defaultSkybox;
    private float _defaultAmbientIntensity;
    private Color _defaultAmbientLight;
    private AmbientMode _defaultAmbientMode;
    private float _defaultReflectionIntensity;
    private bool _defaultFog;
    private Color _defaultFogColor;
    private CameraClearFlags _defaultClearFlags;
    private Color _defaultBackgroundColor;

    private void Start()
    {
        _starterController = GetComponent<ThirdPersonController>();
        _input = GetComponent<StarterAssetsInputs>();
        _animator = GetComponent<Animator>();

        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.volume = audioVolume;

        if (mainCamera == null) mainCamera = Camera.main;

        if (mainCamera != null)
        {
            _defaultClearFlags = mainCamera.clearFlags;
            _defaultBackgroundColor = mainCamera.backgroundColor;
        }


        _defaultSkybox = RenderSettings.skybox;
        _defaultAmbientMode = RenderSettings.ambientMode;
        _defaultAmbientLight = RenderSettings.ambientLight;
        _defaultAmbientIntensity = RenderSettings.ambientIntensity;
        _defaultReflectionIntensity = RenderSettings.reflectionIntensity;
        _defaultFog = RenderSettings.fog;
        _defaultFogColor = RenderSettings.fogColor;

        _currentGuardTime = maxGuardDuration;

        if (guardBarContainer) guardBarContainer.SetActive(false);

        if (guardDurationSlider != null)
        {
            guardDurationSlider.maxValue = maxGuardDuration;
            guardDurationSlider.value = maxGuardDuration;
        }

        // UI Baþlangýç Ayarý: Outlier'larýn Alphasýný 0 yapalým (Görünmez ama aktif)
        InitUIAlpha(thiefIconOutlier);
        InitUIAlpha(guardIconOutlier);
        InitUIAlpha(statueIconOutlier);

        SwitchMask(MaskType.Thief);
    }

    // Baþlangýçta outlierlarý görünmez yapar (SetActive false yapmadan)
    void InitUIAlpha(Image img)
    {
        if (img != null)
        {
            img.canvasRenderer.SetAlpha(0f);
        }
    }

    void Update()
    {
        if (_audioSource.volume != audioVolume)
        {
            _audioSource.volume = audioVolume;
        }
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchMask(MaskType.Thief);

        if (Input.GetKeyDown(KeyCode.Alpha2) && !_isGuardDepleted) SwitchMask(MaskType.Guard);

        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchMask(MaskType.Statue);

        if (currentMask != MaskType.Thief)
        {
            _input.jump = false;
        }

        HandleGuardDuration();
    }

    void HandleGuardDuration()
    {
        // --- DURUM 1: MASKE TAKILI (Süre Azalýyor) ---
        if (currentMask == MaskType.Guard)
        {
            if (guardBarContainer) guardBarContainer.SetActive(true);

            _currentGuardTime -= Time.deltaTime;

            // 1. Uyarý Sesi (Warning)
            if (_currentGuardTime <= warningThreshold && !_hasPlayedWarningSound)
            {
                if (guardWarningClip != null)
                {
                    _audioSource.loop = false; // Uyarý sesi loop olmamalý
                    _audioSource.clip = guardWarningClip;
                    _audioSource.Play();
                }
                _hasPlayedWarningSound = true;
            }

            if (_currentGuardTime <= 0)
            {
                _currentGuardTime = 0;
                _isGuardDepleted = true;
                SwitchMask(MaskType.Thief);
            }
        }
        // --- DURUM 2: MASKE TAKILI DEÐÝL (Bar Doluyor) ---
        else
        {
            if (_currentGuardTime < maxGuardDuration)
            {
                if (guardBarContainer) guardBarContainer.SetActive(true);

                _currentGuardTime += Time.deltaTime * guardCooldownSpeed;

                // 2. YENÝ: Þarj Olma Sesi (Recharge Loop)
                // Eðer þarj sesi tanýmlýysa VE þu an çalmýyorsa baþlat
                if (guardRechargeClip != null)
                {
                    // Eðer hiç ses çalmýyorsa VEYA çalan ses uyarý sesiyse (onu kesip þarja geç)
                    if (!_audioSource.isPlaying || _audioSource.clip == guardWarningClip)
                    {
                        _audioSource.clip = guardRechargeClip;
                        _audioSource.loop = true; // Þarj sesi sürekli dönsün
                        _audioSource.Play();
                    }
                }

                // Uyarý sesi kilidini sýfýrla (tekrar maskeyi takýnca çabuk çalsýn diye deðil, mantýk sýfýrlansýn diye)
                _hasPlayedWarningSound = false;

                if (_currentGuardTime >= maxGuardDuration)
                {
                    // DOLUM BÝTTÝ
                    _currentGuardTime = maxGuardDuration;
                    _isGuardDepleted = false;
                    if (guardBarContainer) guardBarContainer.SetActive(false);

                    // Sesi Durdur
                    if (_audioSource.clip == guardRechargeClip)
                    {
                        _audioSource.Stop();
                        _audioSource.loop = false;
                    }
                }
            }
            else
            {
                if (guardBarContainer) guardBarContainer.SetActive(false);

                // Garanti olsun: Doluysa ve hala þarj sesi çalýyorsa sustur
                if (_audioSource.clip == guardRechargeClip && _audioSource.isPlaying)
                {
                    _audioSource.Stop();
                    _audioSource.loop = false;
                }
            }
        }

        if (guardDurationSlider != null)
        {
            guardDurationSlider.value = _currentGuardTime;
            if (guardDurationSlider.fillRect != null)
            {
                Image fillImage = guardDurationSlider.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = _isGuardDepleted ? Color.red : Color.blue;
                }
            }
        }
    }

    public void SwitchMask(MaskType newMask)
    {
        if (newMask == MaskType.Guard && _isGuardDepleted) return;

        // --- SES KONTROLÜ (Geçiþlerde) ---

        // 1. Eðer Guard maskesine geçiyorsak: Þarj sesini (loop) hemen kesmeliyiz.
        if (newMask == MaskType.Guard)
        {
            if (_audioSource.clip == guardRechargeClip && _audioSource.isPlaying)
            {
                _audioSource.Stop();
                _audioSource.loop = false;
            }
        }
        // 2. Eðer Guard maskesinden çýkýyorsak: Uyarý sesini kesmeliyiz.
        else if (currentMask == MaskType.Guard)
        {
            if (_audioSource.clip == guardWarningClip && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
        }
        // --------------------------------

        currentMask = newMask;

        ResetWorld();
        ActivateMaskModel(currentMask);
        UpdateUI(currentMask);

        switch (currentMask)
        {
            case MaskType.Thief:
                SetMovement(thiefSpeed, thiefSpeed + 2.0f, 1.2f, -15.0f);
                break;

            case MaskType.Guard:
                SetMovement(guardSpeed, guardSpeed, 0.0f, -25.0f);
                break;

            case MaskType.Statue:
                SetMovement(0.0f, 0.0f, 0.0f, -15.0f);
                GoPitchBlack();
                if (statueTransformClip != null)
                {
                    _audioSource.loop = false; // Loop'u kapat (Guard'dan geliyorsak açýk kalmýþ olabilir)
                    _audioSource.PlayOneShot(statueTransformClip);
                }
                break;
        }
    }

    // --- UI GEÇÝÞ SÝSTEMÝ (FADE EFFECT) ---
    void UpdateUI(MaskType type)
    {
        // 1. Önce hepsini "Seçilmemiþ" moduna al (Normali göster, Outlier'ý gizle)
        SetIconState(thiefIconNormal, thiefIconOutlier, false);
        SetIconState(guardIconNormal, guardIconOutlier, false);
        SetIconState(statueIconNormal, statueIconOutlier, false);

        // 2. Sadece seçili olaný "Seçilmiþ" moduna al (Normali gizle, Outlier'ý göster)
        switch (type)
        {
            case MaskType.Thief:
                SetIconState(thiefIconNormal, thiefIconOutlier, true);
                break;
            case MaskType.Guard:
                SetIconState(guardIconNormal, guardIconOutlier, true);
                break;
            case MaskType.Statue:
                SetIconState(statueIconNormal, statueIconOutlier, true);
                break;
        }
    }

    // Yardýmcý Fonksiyon: Ýkonlar arasý yumuþak geçiþ yapar
    void SetIconState(Image normalIcon, Image outlierIcon, bool isSelected)
    {
        if (normalIcon == null || outlierIcon == null) return;

        if (isSelected)
        {
            // Seçiliyse: Normal ikon gitsin (0), Outlier gelsin (1)
            normalIcon.CrossFadeAlpha(0f, uiFadeSpeed, true);
            outlierIcon.CrossFadeAlpha(1f, uiFadeSpeed, true);
        }
        else
        {
            // Seçili deðilse: Normal ikon gelsin (1), Outlier gitsin (0)
            normalIcon.CrossFadeAlpha(1f, uiFadeSpeed, true);
            outlierIcon.CrossFadeAlpha(0f, uiFadeSpeed, true);
        }
    }

    void ActivateMaskModel(MaskType type)
    {
        if (thiefMaskObject) thiefMaskObject.SetActive(false);
        if (guardMaskObject) guardMaskObject.SetActive(false);
        if (statueMaskObject) statueMaskObject.SetActive(false);

        switch (type)
        {
            case MaskType.Thief:
                if (thiefMaskObject) thiefMaskObject.SetActive(true);
                break;
            case MaskType.Guard:
                if (guardMaskObject) guardMaskObject.SetActive(true);
                break;
            case MaskType.Statue:
                if (statueMaskObject) statueMaskObject.SetActive(true);
                break;
        }
    }

    void SetMovement(float move, float sprint, float jump, float grav)
    {
        _starterController.MoveSpeed = move;
        _starterController.SprintSpeed = sprint;
        _starterController.JumpHeight = jump;
        _starterController.Gravity = grav;
        if (_animator) _animator.speed = 1;
    }

    void GoPitchBlack()
    {
        if (sunLight != null) sunLight.enabled = false;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.ambientIntensity = 0f;
        RenderSettings.skybox = null;
        RenderSettings.reflectionIntensity = 0f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.5f;

        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = Color.black;
        }

        if (statueSpotLight != null) statueSpotLight.enabled = true;
    }

    void ResetWorld()
    {
        if (sunLight != null) sunLight.enabled = true;
        RenderSettings.ambientMode = _defaultAmbientMode;
        RenderSettings.ambientLight = _defaultAmbientLight;
        RenderSettings.ambientIntensity = _defaultAmbientIntensity;
        RenderSettings.skybox = _defaultSkybox;
        RenderSettings.reflectionIntensity = _defaultReflectionIntensity;
        RenderSettings.fog = _defaultFog;
        RenderSettings.fogColor = _defaultFogColor;

        if (mainCamera != null)
        {
            mainCamera.clearFlags = _defaultClearFlags;
            mainCamera.backgroundColor = _defaultBackgroundColor;
        }
        RenderSettings.fogDensity = 0.025f;

        if (statueSpotLight != null) statueSpotLight.enabled = false;
    }
}