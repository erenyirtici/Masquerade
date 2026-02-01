using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// SFX için bir AudioSource zorunlu kýlýyoruz
[RequireComponent(typeof(AudioSource))]
public class GameLevelManager : MonoBehaviour
{
    [Header("Settings")]
    public float totalTime = 60.0f;

    [Header("Audio Settings")]
    public AudioClip countdownClip;   // 10 saniyelik geri sayým sesi
    public AudioClip backgroundMusic; // YENÝ: Arka plan müziði
    [Range(0f, 1f)] public float musicVolume = 0.6f; // YENÝ: Müzik sesi (Varsayýlan %60)

    private AudioSource _sfxSource;   // Efektler için (Geri sayým)
    private AudioSource _musicSource; // Müzik için (Loop)
    private bool _hasPlayedCountdown = false;

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public GameObject nameInputPanel;
    public TMP_InputField nameInputField;
    public GameObject losePanel;

    private float _currentTime;
    private bool _isGameOver = false;

    void Start()
    {
        _currentTime = totalTime;

        // 1. SFX Kaynaðýný Al (Mevcut olan)
        _sfxSource = GetComponent<AudioSource>();
        _sfxSource.playOnAwake = false;

        // 2. Müzik Kaynaðýný Kodla Oluþtur (Yeni)
        // Ayný objeye ikinci bir AudioSource ekliyoruz ki müzik ve efekt karýþmasýn
        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.clip = backgroundMusic;
        _musicSource.volume = musicVolume;
        _musicSource.loop = true; // Döngüye al
        _musicSource.playOnAwake = false;

        // Müziði Baþlat
        if (backgroundMusic != null)
        {
            _musicSource.Play();
        }

        if (nameInputPanel != null) nameInputPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    void Update()
    {
        // Oyun esnasýnda sesi Inspector'dan deðiþtirirsen anlýk güncellensin
        if (_musicSource != null && _musicSource.volume != musicVolume)
        {
            _musicSource.volume = musicVolume;
        }

        if (_isGameOver) return;

        _currentTime -= Time.deltaTime;

        // --- SON 10 SANÝYE KONTROLÜ (SFX Source kullanýr) ---
        if (_currentTime <= 10.0f && !_hasPlayedCountdown)
        {
            if (countdownClip != null)
            {
                _sfxSource.PlayOneShot(countdownClip);
            }
            _hasPlayedCountdown = true;
        }
        // ----------------------------------------------------

        if (timerText != null)
        {
            float timeDisplay = _currentTime < 0 ? 0 : _currentTime;
            float minutes = Mathf.FloorToInt(timeDisplay / 60);
            float seconds = Mathf.FloorToInt(timeDisplay % 60);
            float milliseconds = Mathf.FloorToInt((timeDisplay * 100) % 100);

            timerText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);

            if (_currentTime <= 10.0f) timerText.color = Color.red;
            else timerText.color = Color.white;
        }

        if (_currentTime <= 0)
        {
            _currentTime = 0;
            LoseGame();
        }
    }

    void LoseGame()
    {
        if (_isGameOver) return;
        _isGameOver = true;

        // Kaybedince müziði kýsarak bitirebiliriz veya direkt durdurabiliriz
        if (_musicSource != null) _musicSource.Stop();

        StartCoroutine(LoseRoutine());
    }

    public void WinGame()
    {
        if (_isGameOver) return;
        _isGameOver = true;

        // Kazanýnca hem müziði hem sireni sustur
        if (_sfxSource.isPlaying) _sfxSource.Stop();
        if (_musicSource != null) _musicSource.Stop();

        StartCoroutine(AnimateTimerToCenter());
    }

    // --- (Geri kalan fonksiyonlar ayný) ---
    public void SubmitScore()
    {
        if (nameInputField == null) return;
        string playerName = nameInputField.text;
        if (string.IsNullOrEmpty(playerName)) playerName = "Unknown Thief";

        string jsonString = PlayerPrefs.GetString("LeaderboardTable", "{}");
        LeaderboardData data = JsonUtility.FromJson<LeaderboardData>(jsonString);
        if (data == null) data = new LeaderboardData();
        if (data.entryList == null) data.entryList = new List<ScoreEntry>();

        ScoreEntry newEntry = new ScoreEntry { name = playerName, score = _currentTime };
        data.entryList.Add(newEntry);
        data.entryList = data.entryList.OrderByDescending(x => x.score).ToList();

        if (data.entryList.Count > 10) data.entryList.RemoveRange(10, data.entryList.Count - 10);

        string newJson = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("LeaderboardTable", newJson);
        PlayerPrefs.Save();
        SceneManager.LoadScene("MenuScene");
    }

    IEnumerator LoseRoutine()
    {
        if (losePanel != null)
        {
            losePanel.SetActive(true);
            Transform txtTransform = losePanel.transform.GetChild(0);
            if (txtTransform != null)
            {
                Vector3 originalScale = txtTransform.localScale;
                Vector3 targetScale = originalScale * 1.5f;
                float duration = 0.5f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    txtTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                    yield return null;
                }
            }
        }
        yield return new WaitForSeconds(3.0f);
        SceneManager.LoadScene("MenuScene");
    }

    IEnumerator AnimateTimerToCenter()
    {
        if (timerText == null) yield break;
        RectTransform rect = timerText.GetComponent<RectTransform>();
        Vector3 startPos = rect.position;
        Vector3 startScale = rect.localScale;
        Vector3 targetPos = new Vector3(Screen.width / 2, (Screen.height / 2) + 100, 0);
        Vector3 targetScale = new Vector3(3f, 3f, 3f);
        float duration = 1.0f;
        float elapsed = 0f;
        timerText.color = Color.yellow;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);
            rect.position = Vector3.Lerp(startPos, targetPos, t);
            rect.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        rect.position = targetPos;
        rect.localScale = targetScale;
        if (nameInputPanel != null)
        {
            nameInputPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}