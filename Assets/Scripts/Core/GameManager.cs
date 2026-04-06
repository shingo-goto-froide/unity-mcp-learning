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
    public string ResolveMessage { get; private set; } = "";

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

    public void SelectPool(int poolIdx)
    {
        var ph = turnManager.currentPhase;
        if (ph != GamePhase.AcquireP1 && ph != GamePhase.AcquireP2) return;
        int pi = (ph == GamePhase.AcquireP1) ? turnManager.FirstPlayerIndex : turnManager.SecondPlayerIndex;
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
            StartCoroutine(ResolveCoroutine());
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
        // コンボ数・DIS持続ターンを事前計算
        actionResolver.PrepareResolve(players[0], players[1]);
        // ロックをカウントダウン（0になった段だけ解除）
        actionResolver.TickLocksBeforeResolve(players[0], players[1]);

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
                yield return new UnityEngine.WaitForSeconds(1.5f);
                GameUINew.Instance?.ClearResolveHighlights(); // 段を見終わったら色を戻す
            }
            if (players[0].IsDefeated() || players[1].IsDefeated()) break;
        }

        // ATKコンボボーナス（2段以上完成でコンボ数分の追加ダメージ）
        int p1Combo = actionResolver.P1AtkCombo;
        int p2Combo = actionResolver.P2AtkCombo;
        if (p1Combo >= 2 && !players[1].IsDefeated())
        {
            players[1].TakeDamage(p1Combo);
            ResolveMessage = $"P1 COMBO x{p1Combo}! +{p1Combo} bonus dmg!";
            OnPhaseChanged?.Invoke(GamePhase.Resolve);
            GameUINew.Instance?.RefreshAll();
            yield return new UnityEngine.WaitForSeconds(1.5f);
            GameUINew.Instance?.ClearResolveHighlights();
        }
        if (p2Combo >= 2 && !players[0].IsDefeated())
        {
            players[0].TakeDamage(p2Combo);
            ResolveMessage = $"P2 COMBO x{p2Combo}! +{p2Combo} bonus dmg!";
            OnPhaseChanged?.Invoke(GamePhase.Resolve);
            GameUINew.Instance?.RefreshAll();
            yield return new UnityEngine.WaitForSeconds(1.5f);
            GameUINew.Instance?.ClearResolveHighlights();
        }

        actionResolver.ClearAfterResolve(players[0], players[1]);
        // Resolve後にシールド半減（案A：積んだターンは全額有効、次ターンから半減）
        foreach (var p in players) p.HalfShield();
        Debug.Log($"Turn {turnManager.turnCount}: Shield halved after Resolve.");
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
        }
    }

    void OnPlayerDefeated(PlayerData p)
    {
        turnManager.SetGameOver();
        var winner = players[p.playerIndex == 0 ? 1 : 0];
        Debug.Log($"{winner.Name} Wins!");
        OnGameOver?.Invoke(winner);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
