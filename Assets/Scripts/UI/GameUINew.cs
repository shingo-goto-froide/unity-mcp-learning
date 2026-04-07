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

    Coroutine _hlPulse;

    void Awake()
    {
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
        int activeIdx = gm.CurrentActorIndex;
        bool anyActive = ph == GamePhase.AcquireP1 || ph == GamePhase.AcquireP2
                      || ph == GamePhase.AssignP1  || ph == GamePhase.AssignP2;
        player1Panel?.SetTurnActive(anyActive && activeIdx == 0);
        player2Panel?.SetTurnActive(anyActive && activeIdx == 1);

        player1Panel?.SetSlotsHidden(false);
        player2Panel?.SetSlotsHidden(false);
        SetPhaseHighlight(ph);
    }

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

    static void ApplyHighlightColor(string panelName, Color c)
    {
        var hl = GameObject.Find(panelName)?.transform.Find("Highlight");
        if (hl == null) return;
        foreach (Transform strip in hl)
        {
            var img = strip.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = c;
        }
    }

    static void SetPanelHighlight(string panelName, bool on)
        => ApplyHighlightColor(panelName, on ? HL_ON : HL_OFF);

    // sin波でalphaをゆっくり上下させるパルス（約1.5秒周期、alpha 0.35〜0.9）
    System.Collections.IEnumerator PulseHighlights(bool pool, bool control)
    {
        while (true)
        {
            float t     = (Mathf.Sin(Time.time * (Mathf.PI * 2f / 1.5f)) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(0.35f, 0.9f, t);
            var c = new Color(1f, 0.85f, 0.1f, alpha);
            if (pool)    ApplyHighlightColor("PoolPanel",    c);
            if (control) ApplyHighlightColor("ControlPanel", c);
            yield return null;
        }
    }

    public void TriggerResolveEffect(int rowIdx, ResourceType p1Type, ResourceType p2Type, int damage, int shieldAmount = 0, int lockAmount = 0)
    {
        var em = EffectManager.Instance;
        if (em == null) return;

        var p1Rt = player1Panel?.GetComponent<RectTransform>();
        var p2Rt = player2Panel?.GetComponent<RectTransform>();

        RectTransform GetRowRt(PlayerPanelUI panel)
        {
            var sc = panel?.transform.Find("SlotsContainer");
            if (sc != null && rowIdx < sc.childCount)
                return sc.GetChild(rowIdx).GetComponent<RectTransform>();
            return panel?.GetComponent<RectTransform>();
        }

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

        bool anyDef = p1Type == ResourceType.Defense || p2Type == ResourceType.Defense;

        if (p1Type == ResourceType.Attack  && p2Rt != null) em.PlayAttack(GetRowRt(player1Panel), p2Rt, damage);
        if (p1Type == ResourceType.Defense && p1Rt != null) em.PlayDefense(p1Rt, p2Rt, shieldAmount);
        if (p1Type == ResourceType.Disrupt && p2Rt != null)
        {
            if (anyDef) em.PlayDisruptDelayed(p2Rt, 0.5f, lockAmount);
            else        em.PlayDisrupt(p2Rt, lockAmount);
        }

        if (p2Type == ResourceType.Attack  && p1Rt != null) em.PlayAttack(GetRowRt(player2Panel), p1Rt, damage);
        if (p2Type == ResourceType.Defense && p2Rt != null) em.PlayDefense(p2Rt, p1Rt, shieldAmount);
        if (p2Type == ResourceType.Disrupt && p1Rt != null)
        {
            if (anyDef) em.PlayDisruptDelayed(p1Rt, 0.5f, lockAmount);
            else        em.PlayDisrupt(p1Rt, lockAmount);
        }
    }

    void SetPhaseHighlight(GamePhase ph)
    {
        var gm2      = GameManager.Instance;
        int actorIdx = gm2 != null ? gm2.CurrentActorIndex : 0;
        bool anyTurn   = ph == GamePhase.AcquireP1 || ph == GamePhase.AcquireP2
                      || ph == GamePhase.AssignP1  || ph == GamePhase.AssignP2;
        bool isAcquire = ph == GamePhase.AcquireP1 || ph == GamePhase.AcquireP2;
        bool isAssign  = ph == GamePhase.AssignP1  || ph == GamePhase.AssignP2;
        bool isAiMode  = GameSettings.Mode == GameMode.AI;

        bool p1On, p2On;
        if (isAssign && isAiMode)
        {
            p1On = actorIdx == 0;
            p2On = false;
        }
        else
        {
            p1On = anyTurn && actorIdx == 0;
            p2On = anyTurn && actorIdx == 1;
        }
        bool poolOn    = isAcquire;
        bool controlOn = isAssign && (!isAiMode || actorIdx == 0);

        // 既存パルスを停止してリセット
        if (_hlPulse != null) { StopCoroutine(_hlPulse); _hlPulse = null; }
        SetPanelHighlight("Player1Panel", false);
        SetPanelHighlight("Player2Panel", false);
        SetPanelHighlight("PoolPanel",    false);
        SetPanelHighlight("ControlPanel", false);

        // Pool・ControlのみパルスON（プレイヤーパネルは枠なし）
        if (poolOn || controlOn)
            _hlPulse = StartCoroutine(PulseHighlights(poolOn, controlOn));
    }

    void OnGameOver(PlayerData winner)
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverText != null)  gameOverText.text = $"{winner.Name} の勝利！";
    }

    void OnDrawGame()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverText != null)  gameOverText.text = "引き分け！";
    }

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
