using UnityEngine;

public class GameUINew : MonoBehaviour
{
    public static GameUINew Instance { get; private set; }

    [Header("Player Panels")]
    public PlayerPanelUI player1Panel;
    public PlayerPanelUI player2Panel;

    [Header("Pool Panel")]
    public PoolPanelUI poolPanel;

    [Header("Control Panel")]
    public ControlPanelUI controlPanel;

    [Header("Prefabs")]
    public GameObject resourceChipPrefab;

    [Header("Game Over")]
    public GameObject gameOverPanel;
    public TMPro.TextMeshProUGUI gameOverText;
    public UnityEngine.UI.Button restartBtn;
    public UnityEngine.UI.Button toTitleBtn;

    void Awake()
    {
        // 重複していたら自分自身を削除して終了
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameUINew] Duplicate detected and destroyed.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        var gm = GameManager.Instance;
        if (gm == null) { Debug.LogError("[GameUINew] GameManager not found!"); return; }

        gm.OnPhaseChanged += _ => RefreshAll();
        gm.OnGameOver += OnGameOver;
        gm.OnDraw += OnDrawGame;

        InitializeAllPanels();

        restartBtn?.onClick.AddListener(() =>
        {
            gm.StartGame();
            InitializeAllPanels();
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            RefreshAll();
        });

        toTitleBtn?.onClick.AddListener(() =>
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
        });

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        RefreshAll();

        // 旧OnGUI UIを無効化
        var oldUI = GetComponent<GameUI>();
        if (oldUI != null) oldUI.enabled = false;
    }

    void InitializeAllPanels()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        player1Panel?.Initialize(gm.players[0], resourceChipPrefab);
        player2Panel?.Initialize(gm.players[1], resourceChipPrefab);
        poolPanel?.Initialize();
        controlPanel?.Initialize();
    }

    public void RefreshAll()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.players == null) return;

        player1Panel?.Refresh();
        player2Panel?.Refresh();
        poolPanel?.Refresh(gm.poolManager, gm.turnManager.currentPhase);
        controlPanel?.Refresh();

        var ph = gm.turnManager.currentPhase;
        // 先手後手が入れ替わるため CurrentActorIndex で判定
        int activeIdx = gm.CurrentActorIndex;
        bool anyActive = ph == GamePhase.AcquireP1 || ph == GamePhase.AcquireP2
                      || ph == GamePhase.AssignP1  || ph == GamePhase.AssignP2;
        player1Panel?.SetTurnActive(anyActive && activeIdx == 0);
        player2Panel?.SetTurnActive(anyActive && activeIdx == 1);

        // Assignフェーズ中は相手のスロットを隠す
        bool isAssignPhase = ph == GamePhase.AssignP1 || ph == GamePhase.AssignP2;
        if (isAssignPhase)
        {
            // 操作中のプレイヤーは自分のスロットが見える・相手は隠れる
            player1Panel?.SetSlotsHidden(activeIdx != 0);
            player2Panel?.SetSlotsHidden(activeIdx != 1);
        }
        else
        {
            // Assign以外（Acquire・Resolve等）は両方表示
            player1Panel?.SetSlotsHidden(false);
            player2Panel?.SetSlotsHidden(false);
        }
        SetPhaseHighlight(ph);
    }

    // Resolve演出：指定行をハイライト（両パネル同時）
    public void HighlightResolveRow(int rowIdx)
    {
        player1Panel?.HighlightRow(rowIdx, true);
        player2Panel?.HighlightRow(rowIdx, true);
    }

    public void ClearResolveHighlights()
    {
        player1Panel?.HighlightRow(-1, false);
        player2Panel?.HighlightRow(-1, false);
    }

    static readonly Color HL_ON  = new Color(1f, 0.85f, 0.1f, 0.9f);
    static readonly Color HL_OFF = new Color(0f, 0f, 0f, 0f);

    // Highlightコンテナ配下の4本帯の色をまとめて設定
    static void SetPanelHighlight(string panelName, bool on)
    {
        var hl = GameObject.Find(panelName)?.transform.Find("Highlight");
        if (hl == null) return;
        var c = on ? HL_ON : HL_OFF;
        foreach (Transform strip in hl)
        {
            var img = strip.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = c;
        }
    }

    // Resolve演出: rowIdx/タイプを渡して各エフェクトを起動
    public void TriggerResolveEffect(int rowIdx, ResourceType p1Type, ResourceType p2Type, int damage)
    {
        var em = EffectManager.Instance;
        if (em == null) return;

        var p1Rt = player1Panel?.GetComponent<RectTransform>();
        var p2Rt = player2Panel?.GetComponent<RectTransform>();

        // 攻撃Row中心のRt取得
        RectTransform GetRowRt(PlayerPanelUI panel)
        {
            var sc = panel?.transform.Find("SlotsContainer");
            if (sc != null && rowIdx < sc.childCount)
                return sc.GetChild(rowIdx).GetComponent<RectTransform>();
            return panel?.GetComponent<RectTransform>();
        }

        // Just Guard判定
        bool jg = (p1Type == ResourceType.Attack && p2Type == ResourceType.Defense) ||
                  (p2Type == ResourceType.Attack && p1Type == ResourceType.Defense);
        if (jg)
        {
            bool p1Attacks = p1Type == ResourceType.Attack;
            var atkRow   = GetRowRt(p1Attacks ? player1Panel : player2Panel);
            var atkPanel = p1Attacks ? p1Rt : p2Rt;
            var defPanel = p1Attacks ? p2Rt : p1Rt;
            int mult     = GameManager.Instance?.JustGuardMultiplier ?? 2;
            em.PlayJustGuard(atkRow, atkPanel, defPanel, damage * mult);
            return;
        }

        // P1エフェクト
        if (p1Type == ResourceType.Attack  && p2Rt != null) em.PlayAttack(GetRowRt(player1Panel), p2Rt, damage);
        if (p1Type == ResourceType.Defense && p1Rt != null) em.PlayDefense(p1Rt, p2Rt);
        if (p1Type == ResourceType.Disrupt && p2Rt != null) em.PlayDisrupt(p2Rt);

        // P2エフェクト（ATK同士DRAW含む）
        if (p2Type == ResourceType.Attack  && p1Rt != null) em.PlayAttack(GetRowRt(player2Panel), p1Rt, damage);
        if (p2Type == ResourceType.Defense && p2Rt != null) em.PlayDefense(p2Rt, p1Rt);
        if (p2Type == ResourceType.Disrupt && p1Rt != null) em.PlayDisrupt(p1Rt);
    }

    void SetPhaseHighlight(GamePhase ph)
    {
        var gm2 = GameManager.Instance;
        int actorIdx  = gm2 != null ? gm2.CurrentActorIndex : 0;
        bool anyTurn  = ph == GamePhase.AcquireP1 || ph == GamePhase.AcquireP2
                     || ph == GamePhase.AssignP1  || ph == GamePhase.AssignP2;
        bool isP1Turn = anyTurn && actorIdx == 0;
        bool isP2Turn = anyTurn && actorIdx == 1;
        bool isAcquire = ph == GamePhase.AcquireP1 || ph == GamePhase.AcquireP2;
        bool isAssign  = ph == GamePhase.AssignP1  || ph == GamePhase.AssignP2;

        SetPanelHighlight("Player1Panel", isP1Turn);
        SetPanelHighlight("Player2Panel", isP2Turn);
        SetPanelHighlight("PoolPanel",    isAcquire);
        SetPanelHighlight("ControlPanel", isAssign);
    }

    void OnGameOver(PlayerData winner)
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverText != null)  gameOverText.text = $"{winner.Name} Wins!";
    }

    void OnDrawGame()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverText != null)  gameOverText.text = "DRAW!";
    }

    // シールド半減フィードバック：両パネルにフラッシュ演出を依頼
    public void TriggerShieldHalved(int shield0Before, int shield1Before)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        int after0 = gm.players[0].shield;
        int after1 = gm.players[1].shield;
        player1Panel?.FlashShieldHalved(shield0Before, after0);
        player2Panel?.FlashShieldHalved(shield1Before, after1);
    }

        void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}