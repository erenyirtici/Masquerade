using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("UI Objects")]
    public GameObject uiButtons;       // Play, Quit buttons container
    public GameObject staticBackground; // The static image background
    public GameObject videoScreen;     // The RawImage object showing the video
    public VideoPlayer videoPlayer;    // The VideoPlayer component
    public CanvasGroup blackoutPanel;  // The black fade panel

    [Header("Settings")]
    public float videoDuration = 5.0f; // MANUAL TIME: Write your video length here (e.g., 12)
    public float fadeDuration = 1.0f;

    [Header("Leaderboard")]
    public GameObject leaderboardPanel;
    public Transform scoreContainer;
    public GameObject rowPrefab;

    void Start()
    {
        // Reset everything on start
        Time.timeScale = 1f;
        if (blackoutPanel != null) blackoutPanel.alpha = 0;
        //if (videoScreen != null) videoScreen.SetActive(false); // Hide video initially
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
    }

    public void PlayGame()
    {
        StartCoroutine(PlayTransitionSequence());
    }

    IEnumerator PlayTransitionSequence()
    {
        // 1. Butonlarý Gizle
        if (uiButtons != null) uiButtons.SetActive(false);

        // 2. VÝDEO EKRANINI AÇ (Ama Arka Planý Henüz Kapatma!)
        if (videoScreen != null)
        {
            videoScreen.SetActive(true);
        }

        // Objenin aktifleþmesi için minik bir nefes aldýr
        yield return null;

        // 3. VÝDEOYU BAÞLAT
        if (videoPlayer != null)
        {
            videoPlayer.Play();

            // --- KRÝTÝK DÜZELTME BURASI ---
            // Video oynatmaya baþlasa bile ilk görüntünün gelmesi 0.1-0.2 saniye sürebilir.
            // Bu yüzden arka plan resmini (staticBackground) hemen kapatmýyoruz.
            // Video resmin üstüne tam binene kadar 0.2 saniye bekliyoruz.
            yield return new WaitForSeconds(0.2f);

            // Görüntü artýk geldi, þimdi arkadakini silebiliriz (Performans için)
            if (staticBackground != null) staticBackground.SetActive(false);

            // 4. KALAN SÜREYÝ BEKLE
            // 0.2 saniye beklediðimiz için onu toplam süreden düþmeliyiz
            float waitTime = videoDuration - fadeDuration - 0.2f;
            if (waitTime < 0) waitTime = 0;

            yield return new WaitForSeconds(waitTime);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        // 5. KARARMA EFEKTÝ
        if (blackoutPanel != null)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                blackoutPanel.alpha = timer / fadeDuration;
                yield return null;
            }
            blackoutPanel.alpha = 1;
        }

        // 6. SAHNE YÜKLEME
        SceneManager.LoadScene("Playground");
    }

    // --- Leaderboard Methods (No Changes) ---
    public void OpenLeaderboard() { leaderboardPanel.SetActive(true); LoadScores(); }
    public void CloseLeaderboard() { leaderboardPanel.SetActive(false); }

    void LoadScores()
    {
        foreach (Transform child in scoreContainer) Destroy(child.gameObject);
        string jsonString = PlayerPrefs.GetString("LeaderboardTable", "{}");
        LeaderboardData data = JsonUtility.FromJson<LeaderboardData>(jsonString);
        if (data == null || data.entryList == null) return;
        int rank = 1;
        foreach (ScoreEntry entry in data.entryList)
        {
            GameObject newRow = Instantiate(rowPrefab, scoreContainer);
            TextMeshProUGUI[] texts = newRow.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 2)
            {
                texts[0].text = rank + ". " + entry.name;
                float t = entry.score;
                texts[1].text = string.Format("{0:00}:{1:00}:{2:00}", Mathf.FloorToInt(t / 60), Mathf.FloorToInt(t % 60), Mathf.FloorToInt((t * 100) % 100));
            }
            rank++;
        }
    }
}