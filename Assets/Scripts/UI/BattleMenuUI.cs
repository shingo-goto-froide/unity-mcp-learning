using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// BattleScene 右上のハンバーガーメニューを管理する。
/// MenuBtn     → ドロップダウン + バックドロップ表示
/// バックドロップクリック → メニューを閉じる
/// Rules       → RulesPanel 表示
/// Title       → TitleScene へ遷移
/// </summary>
public class BattleMenuUI : MonoBehaviour
{
    public static BattleMenuUI Instance { get; private set; }

    [Header("Menu")]
    public GameObject menuDropdown;
    public GameObject menuBackdrop;   // 半透明の黒オーバーレイ

    [Header("Rules Panel")]
    public GameObject rulesPanel;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (menuDropdown != null) menuDropdown.SetActive(false);
        if (menuBackdrop != null) menuBackdrop.SetActive(false);
        if (rulesPanel   != null) rulesPanel.SetActive(false);
    }

    // MenuBtn から呼ぶ（トグル）
    public void ToggleMenu()
    {
        bool next = !(menuDropdown != null && menuDropdown.activeSelf);
        if (menuDropdown != null) menuDropdown.SetActive(next);
        if (menuBackdrop != null) menuBackdrop.SetActive(next);
    }

    // バックドロップクリック → メニューを閉じる
    public void CloseMenu()
    {
        if (menuDropdown != null) menuDropdown.SetActive(false);
        if (menuBackdrop != null) menuBackdrop.SetActive(false);
    }

    // MenuDropdown → Rules ボタンから呼ぶ
    public void OpenRules()
    {
        CloseMenu();
        if (rulesPanel != null) rulesPanel.SetActive(true);
    }

    // RulesPanel → 閉じるボタンから呼ぶ
    public void CloseRules()
    {
        if (rulesPanel != null) rulesPanel.SetActive(false);
    }

    // MenuDropdown → タイトルへ ボタンから呼ぶ
    public void GoToTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
