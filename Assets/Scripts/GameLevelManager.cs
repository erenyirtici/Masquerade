using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic; // Listeler için gerekli
using System.Linq; // Sýralama (Sort) iþlemleri için gerekli

public class GameLevelManager : MonoBehaviour
{
    [Header("Settings")]
    public float totalTime = 60.0f;

    [Header("UI References")]
    public TextMeshProUGUI timerText; // Süre yazýsý
    public GameObject nameInputPanel; // Kazanma paneli
    public TMP_InputField nameInputField;
    public GameObject losePanel; // Kaybetme paneli

    private float _currentTime;
    private bool _isGameOver = false;

    void Start()
    {
        _currentTime = totalTime;

        // Baþlangýçta panelleri gizle
        if (nameInputPanel != null) nameInputPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    void Update()
    {
        if (_isGameOver) return;

        _currentTime -= Time.deltaTime;

        if (timerText != null)
        {
            float timeDisplay = _currentTime < 0 ? 0 : _currentTime;
            float minutes = Mathf.FloorToInt(timeDisplay / 60);
            float seconds = Mathf.FloorToInt(timeDisplay % 60);
            float milliseconds = Mathf.FloorToInt((timeDisplay * 100) % 100);

            timerText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
        }

        if (_currentTime <= 0)
        {
            _currentTime = 0;
            LoseGame();
        }
    }

    // --- KAYBETME FONKSÝYONU ---
    void LoseGame()
    {
        if (_isGameOver) return;
        _isGameOver = true;
        StartCoroutine(LoseRoutine());
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

    // --- KAZANMA FONKSÝYONU ---
    public void WinGame()
    {
        if (_isGameOver) return;
        _isGameOver = true;
        StartCoroutine(AnimateTimerToCenter());
    }

    // --- TOP 10 KAYIT SÝSTEMÝ (JSON) ---
    public void SubmitScore()
    {
        // Kutu baðlý deðilse hata vermesin
        if (nameInputField == null) return;

        string playerName = nameInputField.text;
        if (string.IsNullOrEmpty(playerName)) playerName = "Unknown Thief";

        Debug.Log("Skor Kaydediliyor: " + playerName + " - " + _currentTime);

        // 1. MEVCUT LÝSTEYÝ YÜKLE
        // PlayerPrefs'ten JSON verisini çek, yoksa boþ "{}" getir.
        string jsonString = PlayerPrefs.GetString("LeaderboardTable", "{}");
        LeaderboardData data = JsonUtility.FromJson<LeaderboardData>(jsonString);

        // Eðer veri hiç yoksa veya bozuksa yenisini oluþtur
        if (data == null) data = new LeaderboardData();
        if (data.entryList == null) data.entryList = new List<ScoreEntry>();

        // 2. YENÝ SKORU EKLE
        ScoreEntry newEntry = new ScoreEntry { name = playerName, score = _currentTime };
        data.entryList.Add(newEntry);

        // 3. SIRALA (Büyükten Küçüðe)
        // OrderByDescending kullanýyoruz çünkü "Kalan Süre" ne kadar çoksa o kadar iyidir.
        data.entryList = data.entryList.OrderByDescending(x => x.score).ToList();

        // 4. LÝSTEYÝ 10 KÝÞÝYLE SINIRLA (KIRP)
        if (data.entryList.Count > 10)
        {
            // 10. sýradan sonrasýný sil
            data.entryList.RemoveRange(10, data.entryList.Count - 10);
        }

        // 5. KAYDET
        string newJson = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("LeaderboardTable", newJson);
        PlayerPrefs.Save();

        Debug.Log("Leaderboard Güncellendi!");

        // 6. MENÜYE DÖN
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
