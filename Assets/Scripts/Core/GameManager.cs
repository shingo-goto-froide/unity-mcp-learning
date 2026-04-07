using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Balance")]
    [SerializeField] GameBalanceSO balanceSO;

    public PlayerData[] players { get; private set; }
    public TurnManager turnManager { get; private set; }
    public ResourcePoolManager poolManager { get; private set; }
    public ActionResolver actionResolver { get; private set; }

    public event Action<GamePhase> OnPhaseChanged;
    public event Action<PlayerData> OnGameOver;
    public event Action OnDraw;
    public string ResolveMessage { get; private set; } = "";

    PlayerData _pendingWinner = null; // Resolve終了後にGameOverを発火するために保存
    bool _isDraw = false;
    int[] snapFilled = new int[5];
    ResourceType[] snapTypes = new ResourceType[5];
    System.Collections.Generic.List<ResourceType> snapResources;
    int assignedThisTurn = 0;
    public bool CanResetAssign => assignedThisTurn > 0;
    public int JustGuardMultiplier => balanceSO != null ? balanceSO.justGuardMultiplier : 2;

    // 現在フェーズで操作中のプレイヤーインデックスを返す
    public int CurrentActorIndex
    {
        get
        {
            var ph = turnManager.currentPhase;
            if (ph == GamePhase.AcquireP1 || ph == GamePhase.AssignP1) return turnManager.FirstPlayerIndex;
            if (ph == GamePhase.AcquireP2 || ph == GamePhase.AssignP2) return turnManager.SecondPlayerIndex;
            return 0;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameManager] Duplicate detected and destroyed.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        StartGame();
    }

    void Start() { }

    public void StartGame()
    {
        int hp     = balanceSO != null ? balanceSO.initialHP            : 20;
        int maxRes = balanceSO != null ? balanceSO.maxResourceCapacity : 6;
        _pendingWinner = null;
        _isDraw = false;
        players = new PlayerData[] { new PlayerData(0, hp, maxRes), new PlayerData(1, hp, maxRes) };
        turnManager = new TurnManager();
        poolManager = new ResourcePoolManager();
        actionResolver = new ActionResolver(balanceSO);

        // 先手をランダム決定
        turnManager.Initialize(UnityEngine.Random.Range(0, 2));

        foreach (var p in players) p.OnDefeated += OnPlayerDefeated;

        bool isFirstPhase = true;
        turnManager.OnPhaseChanged += ph =>
        {
            isFirstPhase = false;

            OnPhaseChanged?.Invoke(ph);
            if (ph == GamePhase.AssignP1) BeginAssign(turnManager.FirstPlayerIndex);
            if (ph == GamePhase.AssignP2) BeginAssign(turnManager.SecondPlayerIndex);
        };

        poolManager.RefillAll();
        Debug.Log("Game Started!");
        OnPhaseChanged?.Invoke(turnManager.currentPhase);
    }

    // リソース満杯のとき、取得をスキップしてフェーズを進める
    public void SkipAcquire()
    {
        var ph = turnManager.currentPhase;
        if (ph != GamePhase.AcquireP1 && ph != GamePhase.AcquireP2) return;
        turnManager.NextPhase();
    }

    public void SelectPool(int poolIdx)
    {
        var ph = turnManager.currentPhase;
        if (ph != GamePhase.AcquireP1 && ph != GamePhase.AcquireP2) return;
        int pi = (ph == GamePhase.AcquireP1) ? turnManager.FirstPlayerIndex : turnManager.SecondPlayerIndex;
        // 満杯のときはプールから取得しない（リソース消滅バグ防止）
        if (players[pi].resourceHolder.IsFull()) return;
        if (players[pi].resourceHolder.IsFull()) return; // 満杯なら取得しない
        poolManager.SelectPool(poolIdx, players[pi]);
        turnManager.NextPhase();
    }

    public bool AssignResource(int rowIdx, ResourceType t)
    {
        var ph = turnManager.currentPhase;
        if (ph != GamePhase.AssignP1 && ph != GamePhase.AssignP2) return false;
        int pi = (ph == GamePhase.AssignP1) ? turnManager.FirstPlayerIndex : turnManager.SecondPlayerIndex;
        var p = players[pi];
        if (!p.resourceHolder.resources.Contains(t)) return false;
        if (!p.slotGrid.TryAssignResource(rowIdx, t)) return false;
        p.resourceHolder.Remove(t);
        assignedThisTurn++;
        return true;
    }

    public void EndAssign()
    {
        var ph = turnManager.currentPhase;
        if (ph != GamePhase.AssignP1 && ph != GamePhase.AssignP2) return;
        turnManager.NextPhase();
        if (turnManager.currentPhase == GamePhase.Resolve)
        {
            // AI先手の場合、人間のEndAssignタイミングで保留アサインを適用
            if (GameSettings.Mode == GameMode.AI)
                AIController.Instance?.ApplyPendingPlan();
            StartCoroutine(ResolveCoroutine());
        }
    }

    void BeginAssign(int pi)
    {
        var p = players[pi];
        for (int i = 0; i < 5; i++)
        {
            snapFilled[i] = p.slotGrid.rows[i].filledCount;
            snapTypes[i]  = p.slotGrid.rows[i].assignedType;
        }
        snapResources = new System.Collections.Generic.List<ResourceType>(p.resourceHolder.resources);
        assignedThisTurn = 0;
    }

    public void ResetAssign()
    {
        var ph = turnManager.currentPhase;
        if (ph != GamePhase.AssignP1 && ph != GamePhase.AssignP2) return;
        int pi = (ph == GamePhase.AssignP1) ? turnManager.FirstPlayerIndex : turnManager.SecondPlayerIndex;
        var p = players[pi];
        for (int i = 0; i < 5; i++)
        {
            p.slotGrid.rows[i].filledCount  = snapFilled[i];
            p.slotGrid.rows[i].assignedType = snapTypes[i];
        }
        p.resourceHolder.resources = new System.Collections.Generic.List<ResourceType>(snapResources);
        assignedThisTurn = 0;
    }

    System.Collections.IEnumerator ResolveCoroutine()
    {
        // 双方のAssign確定状態を一瞬表示してからResolveへ
        GameUINew.Instance?.RefreshAll();
        yield return new WaitForSeconds(0.4f);

        // Resolve アナウンス演出が終わるまで待機
        while (AnnouncementUI.Instance != null && AnnouncementUI.Instance.IsAnimating)
            yield return null;

        // コンボ数・DIS持続ターンを事前計算
        actionResolver.PrepareResolve(players[0], players[1]);
        // ロックをカウントダウン（0になった段だけ解除）
        actionResolver.TickLocksBeforeResolve(players[0], players[1]);
        // ロック減数をUIに反映して0.3秒見せる
        GameUINew.Instance?.RefreshAll();
        yield return new WaitForSeconds(0.3f);

        for (int i = 0; i < 5; i++)
        {
            var row0 = players[0].slotGrid.rows[i];
            var row1 = players[1].slotGrid.rows[i];
            var p1EffType = (row0.IsComplete() && row0.lockedCount == 0) ? row0.assignedType : ResourceType.None;
            var p2EffType = (row1.IsComplete() && row1.lockedCount == 0) ? row1.assignedType : ResourceType.None;

            string desc = actionResolver.ResolveRowAt(i, players[0], players[1]);
            if (desc != "")
            {
                ResolveMessage = desc;
                OnPhaseChanged?.Invoke(GamePhase.Resolve);
                GameUINew.Instance?.RefreshAll();
                GameUINew.Instance?.HighlightResolveRow(i);
                int rowDmg = (balanceSO != null && balanceSO.dmgTable != null && i < balanceSO.dmgTable.Length)
                    ? balanceSO.dmgTable[i] : new[]{1,2,4,6,10}[i];
                GameUINew.Instance?.TriggerResolveEffect(i, p1EffType, p2EffType, rowDmg);
                // エフェクトが全部終わるまで待機（最低0.3秒）
                yield return new UnityEngine.WaitForSeconds(0.3f);
                while (EffectManager.Instance != null && EffectManager.Instance.IsPlaying)
                    yield return null;
                yield return new UnityEngine.WaitForSeconds(0.15f);
                GameUINew.Instance?.ClearResolveHighlights(); // 段を見終わったら色を戻す
            }
            if (players[0].IsDefeated() || players[1].IsDefeated()) break;
        }

        // ATKコンボボーナス（両者同時適用 → DRAW判定を正しく行うため）
        int p1Combo = actionResolver.P1AtkCombo;
        int p2Combo = actionResolver.P2AtkCombo;
        // ① ダメージを先に両者同時適用（P1コンボでP2が死んでもP2コンボは有効）
        if (p1Combo >= 2) players[1].TakeDamage(p1Combo);
        if (p2Combo >= 2) players[0].TakeDamage(p2Combo);
        GameUINew.Instance?.RefreshAll();
        // ② 演出を同時起動（P1は上寄り、P2は下寄りにテキストをずらす）
        float comboWait = 0f;
        if (p1Combo >= 2)
        {
            var p1Rt = GameUINew.Instance?.player1Panel?.GetComponent<RectTransform>();
            var p2Rt = GameUINew.Instance?.player2Panel?.GetComponent<RectTransform>();
            float dur1 = EffectManager.Instance != null
                ? EffectManager.Instance.PlayCombo(p1Rt, p2Rt, p1Combo, p1Combo)
                : 1.5f;
            comboWait = Mathf.Max(comboWait, dur1);
        }
        if (p2Combo >= 2)
        {
            var p2Rt2 = GameUINew.Instance?.player2Panel?.GetComponent<RectTransform>();
            var p1Rt2 = GameUINew.Instance?.player1Panel?.GetComponent<RectTransform>();
            float dur2 = EffectManager.Instance != null
                ? EffectManager.Instance.PlayCombo(p2Rt2, p1Rt2, p2Combo, p2Combo)
                : 1.5f;
            comboWait = Mathf.Max(comboWait, dur2);
        }
        // ATKコンボ演出が完全に終わるまで待機
        if (comboWait > 0f)
        {
            yield return new UnityEngine.WaitForSeconds(0.2f);
            while (EffectManager.Instance != null && EffectManager.Instance.IsPlaying)
                yield return null;
            yield return new UnityEngine.WaitForSeconds(0.15f);
        }

        actionResolver.ClearAfterResolve(players[0], players[1]);
        // シールド半減（半減前の値を記録してフィードバック表示）
        int[] shieldBefore = { players[0].shield, players[1].shield };
        foreach (var p in players) p.HalfShield();
        Debug.Log($"Turn {turnManager.turnCount}: Shield halved after Resolve.");
        // シールドが変化したパネルにフィードバック演出
        if (shieldBefore[0] > 0 || shieldBefore[1] > 0)
        {
            GameUINew.Instance?.TriggerShieldHalved(shieldBefore[0], shieldBefore[1]);
            yield return new UnityEngine.WaitForSeconds(0.3f);
            while (EffectManager.Instance != null && EffectManager.Instance.IsPlaying)
                yield return null;
            yield return new UnityEngine.WaitForSeconds(0.15f);
        }
        ResolveMessage = "";
        GameUINew.Instance?.ClearResolveHighlights();

        if (!players[0].IsDefeated() && !players[1].IsDefeated())
        {
            poolManager.RefillAll();
            turnManager.NextPhase();
        }
        else
        {
            GameUINew.Instance?.RefreshAll();
            // 全演出（Announcement・EffectManager）が終わるまで待機
            while (AnnouncementUI.Instance != null && AnnouncementUI.Instance.IsAnimating)
                yield return null;
            yield return new WaitForSeconds(0.6f);
            // GameOver イベントを発火
            if (_isDraw)
            {
                OnDraw?.Invoke();
                _isDraw = false;
            }
            else if (_pendingWinner != null)
            {
                OnGameOver?.Invoke(_pendingWinner);
                _pendingWinner = null;
            }
        }
    }

    void OnPlayerDefeated(PlayerData p)
    {
        turnManager.SetGameOver();
        var other = players[1 - p.playerIndex];
        if (other.IsDefeated())
        {
            // 両者同時にHP0 → DRAW
            _isDraw = true;
            _pendingWinner = null;
            Debug.Log("DRAW!");
        }
        else
        {
            _isDraw = false;
            _pendingWinner = other;
            Debug.Log($"{other.Name} Wins!");
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
