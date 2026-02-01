using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic; // Listeler için
using TMPro; // TextMeshPro için

public class MenuManager : MonoBehaviour
{
    [Header("GEREKLÝ OBJELER")]
    public GameObject uiButtons;       // Play, Quit vs. butonlarýn olduðu obje
    public GameObject background;      // Arkadaki sabit resim
    public GameObject videoScreenObj;  // VideoScreen (RawImage)
    public VideoPlayer videoPlayer;    // VideoScreen üzerindeki Player
    public CanvasGroup perdeObjesi;    // Siyah Perde (Canvas Group)

    [Header("AYARLAR")]
    public float videoSuresi = 5.0f;   // DÝKKAT: Videon kaç saniyeyse buraya yaz!
    public float kararmaSuresi = 1.0f; // Son kaç saniyede ekran kararsýn?

    [Header("LEADERBOARD")]
    public GameObject leaderboardPanel;
    public Transform scoreContainer;
    public GameObject rowPrefab;

    void Start()
    {
        Time.timeScale = 1f; // Zamanýn aktýðýndan emin ol
        if (perdeObjesi != null) perdeObjesi.alpha = 0;
        if (videoScreenObj != null) videoScreenObj.SetActive(false);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
    }

    // PLAY BUTONU BUNU ÇAÐIRACAK
    public void PlayGame()
    {
        StartCoroutine(SinematikGecis());
    }

    IEnumerator SinematikGecis()
    {
        // 1. Butonlarý yok et
        if (uiButtons != null) uiButtons.SetActive(false);

        // 2. Videoyu Aç ve Oynat
        if (videoScreenObj != null && videoPlayer != null)
        {
            videoScreenObj.SetActive(true); // Ekraný aç
            if (background != null) background.SetActive(false); // Arkadaki resmi kapat

            videoPlayer.Play(); // Videoyu baþlat

            // 3. Bekleme (Manuel Süre ile)
            // Kararma süresini düþüyoruz ki video bitmeden ekran kararmaya baþlasýn
            float bekleme = videoSuresi - kararmaSuresi;
            if (bekleme < 0) bekleme = 0;

            yield return new WaitForSeconds(bekleme);
        }
        else
        {
            // Video yoksa bekleme yapma
            yield return new WaitForSeconds(0.1f);
        }

        // 4. Kararma Efekti (Fade Out)
        if (perdeObjesi != null)
        {
            float gecenSure = 0f;
            while (gecenSure < kararmaSuresi)
            {
                gecenSure += Time.deltaTime;
                perdeObjesi.alpha = gecenSure / kararmaSuresi;
                yield return null;
            }
            perdeObjesi.alpha = 1;
        }

        // 5. Oyuna Geç
        SceneManager.LoadScene("SampleScene");
    }

    // --- LEADERBOARD FONKSÝYONLARI (Ayný kaldý) ---
    public void OpenLeaderboard()
    {
        leaderboardPanel.SetActive(true);
        LoadScores();
    }

    public void CloseLeaderboard()
    {
        leaderboardPanel.SetActive(false);
    }

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