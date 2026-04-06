using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TitleScreenUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject aiLevelPanel;

    [Header("Main Menu Buttons")]
    public Button aiBtn;
    public Button onlineBtn;

    [Header("Rules")]
    public GameObject rulesPanel;
    public UnityEngine.UI.Button rulesBtn;

    [Header("AI Level Buttons")]
    public Button easyBtn;
    public Button normalBtn;
    public Button hardBtn;
    public Button backBtn;

    [Header("Scene")]
    public string gameSceneName = "BattleScene";

    void Start()
    {
        aiBtn?.onClick.AddListener(OnAI);
        onlineBtn?.onClick.AddListener(OnOnline);
        easyBtn?.onClick.AddListener(() => OnAILevel(AIDifficulty.Easy));
        normalBtn?.onClick.AddListener(() => OnAILevel(AIDifficulty.Normal));
        hardBtn?.onClick.AddListener(() => OnAILevel(AIDifficulty.Hard));
        backBtn?.onClick.AddListener(OnBack);
        rulesBtn?.onClick.AddListener(OnRules);

        ShowMainMenu();
    }

    void OnAI()
    {
        mainMenuPanel?.SetActive(false);
        aiLevelPanel?.SetActive(true);
    }

    void OnOnline()
    {
        GameSettings.Mode = GameMode.Online;
        LoadGame();
    }

    void OnAILevel(AIDifficulty diff)
    {
        GameSettings.Mode = GameMode.AI;
        GameSettings.Difficulty = diff;
        LoadGame();
    }

    void OnRules()
    {
        if (rulesPanel != null) rulesPanel.SetActive(true);
    }

    void OnBack() => ShowMainMenu();

    void ShowMainMenu()
    {
        mainMenuPanel?.SetActive(true);
        aiLevelPanel?.SetActive(false);
        if (rulesPanel != null) rulesPanel.SetActive(false);
    }

    void LoadGame() => SceneManager.LoadScene(gameSceneName);
}
