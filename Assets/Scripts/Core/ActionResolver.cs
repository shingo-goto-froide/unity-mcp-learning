using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ActionResolver
{
    readonly GameBalanceSO _balance;
    int[] dmgTable    => _balance != null ? _balance.dmgTable    : new[]{ 2, 3, 5, 8, 12 };
    int[] shieldTable => _balance != null ? _balance.shieldTable : new[]{ 1, 2, 3, 4,  6 };
    int[] lockTable   => _balance != null ? _balance.lockTable   : new[]{ 1, 2, 3, 4,  5 };
    int   jgMult      => _balance != null ? _balance.justGuardMultiplier : 2;

    // Resolve前にPrepareResolve()で設定されるコンボ情報
    int _p1AtkCombo = 0;
    int _p2AtkCombo = 0;
    int _p1DisDuration = 1;
    int _p2DisDuration = 1;

    public int P1AtkCombo => _p1AtkCombo;
    public int P2AtkCombo => _p2AtkCombo;

    public ActionResolver(GameBalanceSO balance = null) { _balance = balance; }

    // Resolve開始前に呼ぶ：コンボ数・DIS持続ターンを事前計算
    public void PrepareResolve(PlayerData p1, PlayerData p2)
    {
        _p1AtkCombo    = CountComplete(p1, ResourceType.Attack);
        _p2AtkCombo    = CountComplete(p2, ResourceType.Attack);
        _p1DisDuration = Mathf.Max(1, CountComplete(p1, ResourceType.Disrupt));
        _p2DisDuration = Mathf.Max(1, CountComplete(p2, ResourceType.Disrupt));
        Debug.Log($"[Combo] P1 ATK:{_p1AtkCombo} DIS:{_p1DisDuration}t  P2 ATK:{_p2AtkCombo} DIS:{_p2DisDuration}t");
    }

    int CountComplete(PlayerData p, ResourceType t)
    {
        int n = 0;
        foreach (var row in p.slotGrid.rows)
            if (row.IsComplete() && row.assignedType == t) n++;
        return n;
    }

    // 1段分を処理して演出用テキストを返す
    public string ResolveRowAt(int rowIdx, PlayerData p1, PlayerData p2)
    {
        var r1 = p1.slotGrid.rows[rowIdx];
        var r2 = p2.slotGrid.rows[rowIdx];
        bool p1c = r1.IsComplete(), p2c = r2.IsComplete();
        if (!p1c && !p2c) return "";

        var sb = new StringBuilder($"Row{rowIdx + 1}: ");

        if (p1c && p2c)
        {
            bool p1Atk = r1.assignedType == ResourceType.Attack;
            bool p2Atk = r2.assignedType == ResourceType.Attack;
            bool p1Def = r1.assignedType == ResourceType.Defense;
            bool p2Def = r2.assignedType == ResourceType.Defense;

            if (p1Atk && p2Def)
            {
                ApplyEffect(r2, p2, p1, rowIdx, sb);
                int reflect = dmgTable[rowIdx] * jgMult;
                p1.TakeDamage(reflect);
                sb.Append($"JUST GUARD! P2 reflects {reflect}dmg!");
            }
            else if (p2Atk && p1Def)
            {
                ApplyEffect(r1, p1, p2, rowIdx, sb);
                int reflect = dmgTable[rowIdx] * jgMult;
                p2.TakeDamage(reflect);
                sb.Append($"JUST GUARD! P1 reflects {reflect}dmg!");
            }
            else
            {
                if (p1Def) ApplyEffect(r1, p1, p2, rowIdx, sb);
                if (p2Def) ApplyEffect(r2, p2, p1, rowIdx, sb);
                if (p1Atk && p2Atk) sb.Append("DRAW! ");
                if (!p1Def) ApplyEffect(r1, p1, p2, rowIdx, sb);
                if (!p2Def) ApplyEffect(r2, p2, p1, rowIdx, sb);
            }
        }
        else if (p1c) ApplyEffect(r1, p1, p2, rowIdx, sb);
        else          ApplyEffect(r2, p2, p1, rowIdx, sb);

        return sb.ToString().TrimEnd();
    }

    void ApplyEffect(SlotRow row, PlayerData owner, PlayerData opp, int i, StringBuilder sb)
    {
        switch (row.assignedType)
        {
            case ResourceType.Attack:
                opp.TakeDamage(dmgTable[i]);
                sb.Append($"{owner.Name} ATK -{dmgTable[i]}  ");
                break;
            case ResourceType.Defense:
                owner.AddShield(shieldTable[i]);
                sb.Append($"{owner.Name} DEF +{shieldTable[i]}Shield  ");
                break;
            case ResourceType.Disrupt:
                int dur = owner.playerIndex == 0 ? _p1DisDuration : _p2DisDuration;
                ApplyLocks(opp, lockTable[i], dur);
                sb.Append($"{owner.Name} DIS x{lockTable[i]}Lock({dur}T)  ");
                break;
        }
    }

    public void ClearAfterResolve(PlayerData p1, PlayerData p2)
    {
        p1.slotGrid.ClearCompletedRows();
        p2.slotGrid.ClearCompletedRows();
    }

    // Resolve開始時：カウントダウン（0になった段だけ解除）
    public void TickLocksBeforeResolve(PlayerData p1, PlayerData p2)
    {
        p1.slotGrid.TickAllLocks();
        p2.slotGrid.TickAllLocks();
    }

    // テスト用同期版
    public void Resolve(PlayerData p1, PlayerData p2)
    {
        PrepareResolve(p1, p2);
        TickLocksBeforeResolve(p1, p2);
        for (int i = 0; i < 5; i++) ResolveRowAt(i, p1, p2);
        ClearAfterResolve(p1, p2);
    }

    void ApplyLocks(PlayerData target, int count, int duration = 1)
    {
        var avail = new List<int>();
        for (int i = 0; i < 5; i++)
            if (target.slotGrid.rows[i].GetAvailableCount() > 0) avail.Add(i);
        for (int i = 0; i < count && avail.Count > 0; i++)
        {
            int pick = Random.Range(0, avail.Count);
            target.slotGrid.rows[avail[pick]].AddLock(1, duration);
            avail.RemoveAt(pick);
        }
    }
}
