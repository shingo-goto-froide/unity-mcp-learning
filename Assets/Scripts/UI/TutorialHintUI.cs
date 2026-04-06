using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 最初のターンだけ各フェーズのヒントを画面下部に表示するチュートリアルUI。
/// 2ターン目以降は自動で非表示。
/// </summary>
public class TutorialHintUI : MonoBehaviour
{
    public static TutorialHintUI Instance { get; private set; }

    [Header("References")]
    public GameObject    hintPanel;
    public TextMeshProUGUI hintText;

    // フェーズごとのヒントテキスト
    static readonly string HintAcquire =
        "【獲得フェーズ】\n3つのプールからひとつ選んでリソースを受け取ろう。";
    static readonly string HintAssign =
        "【配置フェーズ】\nリソースをスロットに配置。行を埋めると発動！\nATK→ダメージ  DEF→シールド  DIS→相手の行をロック";
    static readonly string HintResolve =
        "【解決フェーズ】\n完成した行の効果が上から順に発動する。演出を見よう！";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.OnPhaseChanged += OnPhaseChanged;
        // 最初のフェーズを反映
        OnPhaseChanged(gm.turnManager.currentPhase);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
    }

    void OnPhaseChanged(GamePhase phase)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // 1ターン目（turnCount=1）のみ表示
        bool isFirstTurn = gm.turnManager.turnCount <= 1;

        string msg = phase switch
        {
            GamePhase.AcquireP1 or GamePhase.AcquireP2 => HintAcquire,
            GamePhase.AssignP1  or GamePhase.AssignP2  => HintAssign,
            GamePhase.Resolve                           => HintResolve,
            _ => null
        };

        bool show = isFirstTurn && msg != null;

        if (hintPanel != null) hintPanel.SetActive(show);
        if (show && hintText != null) hintText.text = msg;
    }
}
