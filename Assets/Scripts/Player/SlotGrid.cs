using System.Collections.Generic;

[System.Serializable]
public class SlotGrid
{
    public SlotRow[] rows = new SlotRow[5];

    public SlotGrid()
    {
        for (int i = 0; i < 5; i++) rows[i] = new SlotRow(i);
    }

    public bool TryAssignResource(int rowIdx, ResourceType t)
    {
        if (rowIdx < 0 || rowIdx >= 5) return false;
        return rows[rowIdx].TryAssign(t);
    }

    public List<SlotRow> GetCompleteRows()
    {
        var list = new List<SlotRow>();
        foreach (var r in rows) if (r.IsComplete()) list.Add(r);
        return list;
    }

    public void ClearCompletedRows()
    {
        foreach (var r in rows) if (r.IsComplete()) r.Clear();
    }

    // 全ロックを即時クリア（テスト用）
    public void ClearAllLocks()
    {
        foreach (var r in rows) r.ClearLocks();
    }

    // Resolve開始時に呼ぶ：カウントダウンして0になった段だけ解除
    public void TickAllLocks()
    {
        foreach (var r in rows) r.TickLock();
    }
}
