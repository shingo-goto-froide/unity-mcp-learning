using UnityEngine;

[System.Serializable]
public class SlotRow
{
    public int rowIndex;
    public int slotCount;
    public ResourceType assignedType;
    public int filledCount;
    public int lockedCount;
    public int lockedTurnsRemaining { get; private set; } = 0;

    public SlotRow(int idx)
    {
        rowIndex   = idx;
        slotCount  = idx + 1;
        assignedType = ResourceType.None;
    }

    public bool CanAssign(ResourceType t)
    {
        if (t == ResourceType.None) return false;
        if (lockedCount > 0) return false;
        if (assignedType != ResourceType.None && assignedType != t) return false;
        return GetAvailableCount() > 0;
    }

    public bool TryAssign(ResourceType t)
    {
        if (!CanAssign(t)) return false;
        assignedType = t;
        filledCount++;
        return true;
    }

    public bool IsComplete() => filledCount >= slotCount;
    public int GetAvailableCount() => Mathf.Max(0, slotCount - lockedCount - filledCount);

    /// <summary>
    /// DISロックを追加する。
    /// 対象段がすでに埋まっている場合は空振り（何も起きない）。
    /// </summary>
    public void AddLock(int n = 1, int duration = 1)
    {
        int available = slotCount - filledCount;
        if (available <= 0) return; // 埋まり済みは空振り
        lockedCount = Mathf.Min(lockedCount + n, available);
        lockedTurnsRemaining += duration;
    }

    // Resolve開始時に呼ぶ。残りターンを1減らし、0になったらロック解除
    public void TickLock()
    {
        if (lockedCount == 0) return;
        lockedTurnsRemaining = Mathf.Max(0, lockedTurnsRemaining - 1);
        if (lockedTurnsRemaining <= 0) ClearLocks();
    }

    public void Clear()
    {
        assignedType = ResourceType.None;
        filledCount  = 0;
    }

    public void ClearLocks()
    {
        lockedCount          = 0;
        lockedTurnsRemaining = 0;
    }
}
