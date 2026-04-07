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

    // 完成行へのDISロックを「予約」として保存し、Clear()時に適用する
    int _pendingLockCount    = 0;
    int _pendingLockDuration = 0;
    public bool HasPendingLock => _pendingLockCount > 0;

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
    /// 完成行の場合は予約ロックとして保存し、このターンのResolveは通常発動させる。
    /// 予約ロックはClear()（ClearCompletedRows経由）のタイミングで本適用される。
    /// 未完成行の場合は即時ロックを付与する。
    /// </summary>
    public void AddLock(int n = 1, int duration = 1)
    {
        if (IsComplete())
        {
            // 完成行 → 予約ロック（Resolve後のClear時に適用）
            _pendingLockCount    += n;
            _pendingLockDuration += duration;
        }
        else
        {
            // 未完成行 → 即時ロック
            int available = slotCount - filledCount;
            if (available <= 0) return;
            lockedCount = Mathf.Min(lockedCount + n, available);
            lockedTurnsRemaining += duration;
        }
    }

    // Resolve開始時に呼ぶ。残りターンを1減らし、0になったらロック解除
    public void TickLock()
    {
        if (lockedCount == 0) return;
        lockedTurnsRemaining = Mathf.Max(0, lockedTurnsRemaining - 1);
        if (lockedTurnsRemaining <= 0) ClearLocks();
    }

    /// <summary>
    /// 完成行をクリアする。予約ロックがあれば本適用する。
    /// </summary>
    public void Clear()
    {
        assignedType = ResourceType.None;
        filledCount  = 0;

        // 予約ロックを本適用（行が空になったのでそのままロック数をセット）
        if (_pendingLockCount > 0)
        {
            lockedCount          = Mathf.Min(_pendingLockCount, slotCount);
            lockedTurnsRemaining = _pendingLockDuration;
            _pendingLockCount    = 0;
            _pendingLockDuration = 0;
        }
    }

    public void ClearLocks()
    {
        lockedCount          = 0;
        lockedTurnsRemaining = 0;
        _pendingLockCount    = 0;
        _pendingLockDuration = 0;
    }
}