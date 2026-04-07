using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// ゲーム開始・フェーズ開始のアナウンス表示を管理する。
/// Start() で自動的にゲーム開始演出を再生する。
/// OnPhaseChanged を購読してフェーズ名を自動表示。
/// </summary>
public class AnnouncementUI : MonoBehaviour
{
    public static AnnouncementUI Instance { get; private set; }

    [Header("Overlay")]
    public CanvasGroup     overlayGroup;
    public RectTransform   mainTextRect;
    public TextMeshProUGUI mainText;
    public TextMeshProUGUI subText;

    /// <summary>アニメーション中は true（外部から待機可能）</summary>
    public bool IsAnimating { get; private set; }

    bool _firstPhaseSkipped = false; // ゲーム開始直後の AcquireP1 をスキップ
    bool _resolveShown     = false; // Resolve アナウンスを1回だけ表示

    static readonly Color Gold  = new Color(1f, 0.85f, 0.1f);
    static readonly Color White = Color.white;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        IsAnimating = true; // Start() でゲーム開始演出が始まるまでブロック
    }

    void Start()
    {
        if (overlayGroup != null)
        {
            overlayGroup.alpha          = 0f;
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable   = false;
        }

        var gm = GameManager.Instance;
        if (gm != null) gm.OnPhaseChanged += OnPhaseChanged;

        // Start() 時点では GameManager はすでに起動済み → ゲーム開始演出を再生
        ShowGameStart();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        if (Instance == this) Instance = null;
    }

    // =====================================================
    // ゲーム開始演出
    // =====================================================
    public void ShowGameStart()
    {
        _firstPhaseSkipped = false;
        _resolveShown      = false;
        StopAllCoroutines();
        StartCoroutine(GameStartCoroutine());
    }

    IEnumerator GameStartCoroutine()
    {
        IsAnimating = true;
        overlayGroup.blocksRaycasts = true;

        mainText.text      = "GAME START";
        mainText.fontSize  = 72;
        mainText.color     = Gold;
        mainText.fontStyle = TMPro.FontStyles.Bold;
        subText.text       = "";

        // スケール 1.3 → 1.0 + フェードイン
        mainTextRect.localScale = Vector3.one * 1.3f;
        yield return Animate(0.4f, t =>
        {
            overlayGroup.alpha      = t;
            mainTextRect.localScale = Vector3.one * (1.3f - 0.3f * t);
        });

        yield return new WaitForSeconds(1.0f);

        yield return Animate(0.35f, t => overlayGroup.alpha = 1f - t);
        overlayGroup.alpha          = 0f;
        overlayGroup.blocksRaycasts = false;
        IsAnimating = false;

        // ゲーム開始後に最初のフェーズ（AcquireP1）を表示
        var gm = GameManager.Instance;
        if (gm != null)
            StartCoroutine(DelayedPhase(gm.turnManager.currentPhase, 0.15f));
    }

    // =====================================================
    // フェーズ変化
    // =====================================================
    void OnPhaseChanged(GamePhase phase)
    {
        // ゲーム開始直後の最初のイベントはゲームスタートコルーチンが処理するのでスキップ
        if (!_firstPhaseSkipped)
        {
            _firstPhaseSkipped = true;
            return;
        }

        // Resolve は1ターン中1回のみ
        if (phase == GamePhase.Resolve)
        {
            if (_resolveShown) return;
            _resolveShown = true;
        }
        else
        {
            _resolveShown = false; // 他フェーズに移ったらリセット
        }

        if (phase == GamePhase.AcquireP2 || phase == GamePhase.AssignP2) return;
        if (phase == GamePhase.GameOver) return;

        StopAllCoroutines();
        overlayGroup.alpha = 0f;
        IsAnimating = false;
        StartCoroutine(PhaseCoroutine(phase));
    }

    IEnumerator DelayedPhase(GamePhase phase, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (phase == GamePhase.AcquireP2 || phase == GamePhase.AssignP2 || phase == GamePhase.GameOver) yield break;
        yield return PhaseCoroutine(phase);
    }

    // =====================================================
    // フェーズアナウンス演出
    // =====================================================
    IEnumerator PhaseCoroutine(GamePhase phase)
    {
        IsAnimating = true;
        overlayGroup.blocksRaycasts = true;
        SetPhaseText(phase);
        mainTextRect.localScale = Vector3.one;

        bool isResolve = phase == GamePhase.Resolve;
        float holdTime = isResolve ? 1.1f : 1.0f;

        yield return Animate(0.18f, t => overlayGroup.alpha = t);
        yield return new WaitForSeconds(holdTime);
        yield return Animate(0.18f, t => overlayGroup.alpha = 1f - t);

        overlayGroup.alpha          = 0f;
        overlayGroup.blocksRaycasts = false;
        IsAnimating = false;
    }

    void SetPhaseText(GamePhase phase)
    {
        var gm   = GameManager.Instance;
        int turn = gm != null ? gm.turnManager.turnCount : 1;
        switch (phase)
        {
            case GamePhase.AcquireP1:
                mainText.text      = "ACQUIRE";
                mainText.fontSize  = 60;
                mainText.color     = White;
                mainText.fontStyle = TMPro.FontStyles.Bold;
                int firstIdx = gm != null ? gm.turnManager.FirstPlayerIndex : 0;
                bool aiFirst = GameSettings.Mode == GameMode.AI && firstIdx == 1;
                subText.text = aiFirst ? "AI のターン" : "あなたのターン";
                break;
            case GamePhase.AssignP1:
                mainText.text      = "ASSIGN";
                mainText.fontSize  = 60;
                mainText.color     = White;
                mainText.fontStyle = TMPro.FontStyles.Bold;
                subText.text       = "";
                break;
            case GamePhase.Resolve:
                mainText.text      = "RESOLVE";
                mainText.fontSize  = 66;
                mainText.color     = Gold;
                mainText.fontStyle = TMPro.FontStyles.Bold;
                subText.text       = "";
                break;
        }
    }

    IEnumerator Animate(float duration, System.Action<float> onUpdate)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            onUpdate(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
    }
}
