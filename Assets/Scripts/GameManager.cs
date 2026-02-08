using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class PanelConfig
{
    [Header("Panel Configuration")]
    public string panelName;
    public GameObject panelObject;

    [Header("Button Controls")]
    public Button openButton;
    public Button closeButton;

    [Header("Pause Options")]
    public bool pauseOnOpen = true;
    public bool unpauseOnClose = true;
}

public class GameManager : MonoBehaviour
{
    [Header("Upgrade Buffs")]
    public bool isImmune = false;
    public bool isDoubleCoin = false;
    public static GameManager Instance;
    public CustomerSpawner customerSpawner;
    [Header("Canvas References")]
    public GameObject mainMenuCanvas;
    public GameObject gameplayCanvas;
    public GameObject endGamePanel;

    [Header("Hearts UI")]
    public Image[] heartImages;
    public Sprite heartFull;
    public Sprite heartEmpty;

    [Header("End Game UI")]
    public TMP_Text timeText;
    public TMP_Text moneyEarnedText;

    [Header("Money & Timer")]
    public TMP_Text moneyText;
    private int totalMoney = 0;
    private int earnedMoneyThisRound = 0;
    private float playTime = 0f;
    private bool isPlaying = false;
    private bool isTiming = false;
    private float playStartTime = 0f;
    private int lives = 5;

    [Header("Custom Panels Configuration")]
    public List<PanelConfig> panelConfigs = new List<PanelConfig>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        totalMoney = PlayerPrefs.GetInt("TotalMoney", 0);
        UpdateMoneyUI();
        UpdateHeartsUI();
        ShowMainMenu();

        // Gắn sự kiện cho tất cả panel
        foreach (var config in panelConfigs)
        {
            if (config.openButton != null)
                config.openButton.onClick.AddListener(() => TogglePanel(config, true));

            if (config.closeButton != null)
                config.closeButton.onClick.AddListener(() => TogglePanel(config, false));

            if (config.panelObject != null)
                config.panelObject.SetActive(false); // tắt tất cả panel lúc đầu
        }
    }

    private void Update()
    {
        if (isTiming)
            playTime = Time.time - playStartTime;
    }

    //================= LIVES =================
    public void LoseLife()
    {
        if (isImmune) return; 

        if (lives <= 0) return;

        lives--;
        UpdateHeartsUI();

        if (lives <= 0)
            EndGame();
    }

    private void UpdateHeartsUI()
    {
        for (int i = 0; i < heartImages.Length; i++)
            heartImages[i].sprite = i < lives ? heartFull : heartEmpty;
    }

    //================= MONEY =================
    public void AddMoney(int amount)
    {
        if (isDoubleCoin)
            amount *= 2;

        earnedMoneyThisRound += amount;
        totalMoney += amount;
        PlayerPrefs.SetInt("TotalMoney", totalMoney);
        UpdateMoneyUI();
    }

    private void UpdateMoneyUI()
    {
        if (moneyText != null)
            moneyText.text = totalMoney.ToString("N0");
    }

    //================= GAME FLOW =================
    public void StartGame()
    {
        playTime = 0f;
        earnedMoneyThisRound = 0;
        lives = 5;
        UpdateHeartsUI();
        isPlaying = true;
        StartTiming();

        gameplayCanvas.SetActive(true);
        mainMenuCanvas.SetActive(false);
        endGamePanel.SetActive(false);
        Time.timeScale = 1f;

        foreach (var config in panelConfigs)
        {
            if (config.panelObject != null)
                config.panelObject.SetActive(false);
        }

        CustomerController customerMgr = FindObjectOfType<CustomerController>();
        if (customerMgr != null)
            customerMgr.ResetCustomers();
        if (customerSpawner != null)
            customerSpawner.ResetDifficulty();

        isImmune = false;
        isDoubleCoin = false;

        UpgradeManager upgradeManager = FindObjectOfType<UpgradeManager>();
        if (upgradeManager != null)
        {
            upgradeManager.ResetAllUpgrades();
            Debug.Log("🔁 Tất cả buff và cooldown đã được làm mới!");
        }
    }

    public void EndGame()
    {
        StopTiming();
        isPlaying = false;
        Time.timeScale = 0f;
        endGamePanel.SetActive(true);

        if (timeText != null)
            timeText.text = $"{FormatTime(playTime)}";
        if (moneyEarnedText != null)
            moneyEarnedText.text = $"+{earnedMoneyThisRound:N0}";
        if (customerSpawner != null)
            customerSpawner.StopAllCoroutines();
    }

    public void BackToMenu() => ShowMainMenu();

    //================= CANVAS CONTROL =================
    public void ShowMainMenu()
    {
        isPlaying = false;
        StopTiming();
        playTime = 0f;
        earnedMoneyThisRound = 0;
        lives = 5;
        UpdateHeartsUI();

        mainMenuCanvas.SetActive(true);
        gameplayCanvas.SetActive(false);
        endGamePanel.SetActive(false);
        Time.timeScale = 0f;

        CustomerController customerMgr = FindObjectOfType<CustomerController>();
        if (customerMgr != null)
            customerMgr.ResetCustomers();
    }

    //================= CUSTOM PANEL CONTROL =================
    private void TogglePanel(PanelConfig config, bool open)
    {
        if (config.panelObject == null) return;

        config.panelObject.SetActive(open);

        if (open && config.pauseOnOpen)
            Time.timeScale = 0f;
        else if (!open && config.unpauseOnClose)
            Time.timeScale = 1f;
    }

    //================= TIMER =================
    public void StartTiming()
    {
        playStartTime = Time.time;
        playTime = 0f;
        isTiming = true;
    }

    public void StopTiming() => isTiming = false;

    private string FormatTime(float t)
    {
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}
