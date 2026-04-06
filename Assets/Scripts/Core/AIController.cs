using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AI対戦コントローラー。
/// GameSettings.Mode == AI のときのみ動作し、Player2（index 1）を自動操作する。
///
/// 【公平性の保証】
/// Hard AIが参照する相手スロット情報は AssignP1 開始時点のスナップショット。
/// 人間が後から置いた内容は一切参照しない（同時コミット相当）。
/// </summary>
public class AIController : MonoBehaviour
{
    public static AIController Instance { get; private set; }

    const int aiPlayerIndex = 1;

    static readonly int[] DmgTable    = { 2, 3, 5, 8, 12 };
    static readonly int[] ShieldTable = { 1, 2, 3, 4,  6 };

    // AssignP1 開始時点の相手スロットスナップショット（Hard AI 用）
    ResourceType[] oppSnapTypes  = new ResourceType[5];
    int[]          oppSnapFilled = new int[5];

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (GameSettings.Mode != GameMode.AI) { enabled = false; return; }
        GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
        OnPhaseChanged(GameManager.Instance.turnManager.currentPhase);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
    }

    void OnPhaseChanged(GamePhase phase)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // AssignP1 開始 = まだ誰も置いていない瞬間にスナップショットを取る
        if (phase == GamePhase.AssignP1 && GameSettings.Difficulty == AIDifficulty.Hard)
            CaptureOppSnapshot(gm.players[1 - aiPlayerIndex]);

        if (gm.CurrentActorIndex != aiPlayerIndex) return;

        if (phase == GamePhase.AcquireP1 || phase == GamePhase.AcquireP2)
            StartCoroutine(DoAcquire());
        else if (phase == GamePhase.AssignP1 || phase == GamePhase.AssignP2)
            StartCoroutine(DoAssign());
    }

    /// AssignP1 開始時点の相手スロットを記録する
    void CaptureOppSnapshot(PlayerData opp)
    {
        for (int r = 0; r < 5; r++)
        {
            oppSnapTypes[r]  = opp.slotGrid.rows[r].assignedType;
            oppSnapFilled[r] = opp.slotGrid.rows[r].filledCount;
        }
    }

    // =====================================================
    // Acquire フェーズ
    // =====================================================

    IEnumerator DoAcquire()
    {
        // 1フレーム待ってAnnouncementUIがIsAnimating=trueをセットするのを確実に待つ
        yield return null;
        // アナウンス演出が終わるまで待機
        while (AnnouncementUI.Instance != null && AnnouncementUI.Instance.IsAnimating)
            yield return null;
        yield return new WaitForSeconds(GameSettings.Difficulty == AIDifficulty.Hard ? 0.6f : 0.8f);
        var gm    = GameManager.Instance;
        var pools = gm.poolManager.pools;
        int chosen = GameSettings.Difficulty switch
        {
            AIDifficulty.Easy   => ChoosePoolEasy(pools),
            AIDifficulty.Normal => ChoosePoolGreedy(pools),
            AIDifficulty.Hard   => ChoosePoolHard(pools, gm),
            _                   => 0
        };
        gm.SelectPool(chosen);
    }

    int ChoosePoolEasy(ResourcePool[] pools)
    {
        var nonEmpty = pools.Select((p, i) => (p, i)).Where(x => !x.p.IsEmpty()).ToList();
        return nonEmpty.Count > 0 ? nonEmpty[Random.Range(0, nonEmpty.Count)].i : 0;
    }

    int ChoosePoolGreedy(ResourcePool[] pools)
    {
        return pools.Select((p, i) => (p, i))
            .Where(x => !x.p.IsEmpty())
            .OrderByDescending(x => x.p.Count)
            .Select(x => x.i)
            .DefaultIfEmpty(0).First();
    }

    // Hard: 相手HP低→ATK多いプール / 自分HP低→DEF多いプール / 通常→枚数最大
    int ChoosePoolHard(ResourcePool[] pools, GameManager gm)
    {
        var ai  = gm.players[aiPlayerIndex];
        var opp = gm.players[1 - aiPlayerIndex];
        if (opp.currentHp <= DmgTable[4])
        {
            int best = BestPoolByType(pools, ResourceType.Attack);
            if (best >= 0) return best;
        }
        if (ai.currentHp <= 6)
        {
            int best = BestPoolByType(pools, ResourceType.Defense);
            if (best >= 0) return best;
        }
        return ChoosePoolGreedy(pools);
    }

    int BestPoolByType(ResourcePool[] pools, ResourceType type)
    {
        return pools.Select((p, i) => (p, i))
            .Where(x => !x.p.IsEmpty())
            .OrderByDescending(x => x.p.resources.Count(r => r == type))
            .ThenByDescending(x => x.p.Count)
            .Select(x => x.i)
            .DefaultIfEmpty(-1).First();
    }

    // =====================================================
    // Assign フェーズ
    // =====================================================

    IEnumerator DoAssign()
    {
        // AIアサインは即座に実行（1フレームだけ待って確実にフェーズ切り替えを反映）
        yield return null;
        var gm  = GameManager.Instance;
        var ai  = gm.players[aiPlayerIndex];
        var opp = gm.players[1 - aiPlayerIndex];
        switch (GameSettings.Difficulty)
        {
            case AIDifficulty.Easy:   AssignEasy(gm, ai);         break;
            case AIDifficulty.Normal: AssignNormal(gm, ai);       break;
            case AIDifficulty.Hard:   AssignHard(gm, ai, opp);    break;
        }
        yield return new WaitForSeconds(0.3f);
        gm.EndAssign();
    }

    // =====================================================
    // Easy
    // =====================================================

    void AssignEasy(GameManager gm, PlayerData ai)
    {
        var resources = new List<ResourceType>(ai.resourceHolder.resources);
        Shuffle(resources);
        foreach (var res in resources)
        {
            var rows = AvailableRows(ai, res);
            if (rows.Count == 0) continue;
            gm.AssignResource(rows[Random.Range(0, rows.Count)], res);
        }
    }

    // =====================================================
    // Normal
    // =====================================================

    void AssignNormal(GameManager gm, PlayerData ai)
    {
        foreach (var preferred in new[] { ResourceType.Attack, ResourceType.Defense, ResourceType.Disrupt })
            PlaceAll(gm, ai, preferred, closestFirst: true);
        AssignEasy(gm, ai);
    }

    // =====================================================
    // Hard: スナップショットのみ参照・公平AI
    // =====================================================

    void AssignHard(GameManager gm, PlayerData ai, PlayerData opp)
    {
        int aiHp  = ai.currentHp;
        int oppHp = opp.currentHp;

        // Step 1: 相手が瀕死 → ATK全振りでキルを狙う
        if (oppHp <= 8)
        {
            TryCompleteAtkGreedy(gm, ai);
            PlaceAll(gm, ai, ResourceType.Attack, closestFirst: true);
        }

        // Step 2: Just Guard狙い（スナップショットの相手ATK段と同じ行にDEF）
        for (int r = 4; r >= 0; r--)
        {
            if (oppSnapTypes[r] != ResourceType.Attack || oppSnapFilled[r] == 0) continue;
            if (!ai.resourceHolder.resources.Contains(ResourceType.Defense)) break;
            if (!ai.slotGrid.rows[r].CanAssign(ResourceType.Defense)) continue;
            if (DmgTable[r] > ai.shield || ai.currentHp <= DmgTable[r])
                gm.AssignResource(r, ResourceType.Defense);
        }

        // Step 3: ATKコンボ狙い
        TryCompleteAtkGreedy(gm, ai);

        // Step 4: 自分HP危険域 → DEF優先で生存
        if (aiHp <= 6)
        {
            for (int r = 4; r >= 0; r--)
            {
                while (ai.resourceHolder.resources.Contains(ResourceType.Defense)
                       && ai.slotGrid.rows[r].CanAssign(ResourceType.Defense))
                {
                    if (!gm.AssignResource(r, ResourceType.Defense)) break;
                }
            }
        }

        // Step 5: DISで相手の最脅威ATK段（スナップショット）を妨害
        if (ai.resourceHolder.resources.Contains(ResourceType.Disrupt))
        {
            int targetRow = MostThreateningOppRowFromSnapshot();
            if (targetRow >= 0)
            {
                for (int r = 4; r >= 0; r--)
                {
                    if (!ai.slotGrid.rows[r].CanAssign(ResourceType.Disrupt)) continue;
                    if (!ai.resourceHolder.resources.Contains(ResourceType.Disrupt)) break;
                    gm.AssignResource(r, ResourceType.Disrupt);
                    break;
                }
            }
            PlaceAll(gm, ai, ResourceType.Disrupt, closestFirst: true);
        }

        // Step 6: 残りをNormalで処理
        AssignNormal(gm, ai);
    }

    void TryCompleteAtkGreedy(GameManager gm, PlayerData ai)
    {
        var rows = Enumerable.Range(0, 5)
            .Where(r => ai.slotGrid.rows[r].CanAssign(ResourceType.Attack))
            .Select(r => (rowIdx: r,
                          need: ai.slotGrid.rows[r].GetAvailableCount(),
                          value: DmgTable[r]))
            .OrderBy(x => x.need)
            .ThenByDescending(x => x.value)
            .ToList();

        foreach (var (rowIdx, _, _) in rows)
        {
            var row = ai.slotGrid.rows[rowIdx];
            while (ai.resourceHolder.resources.Contains(ResourceType.Attack)
                   && row.CanAssign(ResourceType.Attack))
            {
                if (!gm.AssignResource(rowIdx, ResourceType.Attack)) break;
            }
        }
    }

    // スナップショットから脅威度最大の相手ATK段を返す
    int MostThreateningOppRowFromSnapshot()
    {
        int bestRow = -1, bestScore = 0;
        for (int r = 0; r < 5; r++)
        {
            if (oppSnapTypes[r] != ResourceType.Attack || oppSnapFilled[r] == 0) continue;
            int score = oppSnapFilled[r] * DmgTable[r];
            if (score > bestScore) { bestScore = score; bestRow = r; }
        }
        return bestRow;
    }

    // =====================================================
    // ユーティリティ
    // =====================================================

    void PlaceAll(GameManager gm, PlayerData ai, ResourceType res, bool closestFirst)
    {
        while (ai.resourceHolder.resources.Contains(res))
        {
            var rows = AvailableRows(ai, res);
            if (rows.Count == 0) break;
            int row = closestFirst
                ? rows.OrderBy(r => ai.slotGrid.rows[r].GetAvailableCount())
                      .ThenByDescending(r => DmgTable[r]).First()
                : rows[Random.Range(0, rows.Count)];
            if (!gm.AssignResource(row, res)) break;
        }
    }

    List<int> AvailableRows(PlayerData ai, ResourceType res)
    {
        return Enumerable.Range(0, 5)
            .Where(r => ai.slotGrid.rows[r].CanAssign(res))
            .ToList();
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
