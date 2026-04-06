using UnityEngine;
using System;

public class PlayerData
{
    public int playerIndex;
    public int maxHp = 20;
    public int currentHp;
    public int shield;
    public SlotGrid slotGrid;
    public ResourceHolder resourceHolder;

    public event Action<PlayerData> OnDefeated;
    public event Action<PlayerData> OnHpChanged;
    public event Action<PlayerData> OnShieldChanged;

    public PlayerData(int index, int hp = 20, int maxRes = 6)
    {
        playerIndex = index;
        maxHp = hp;
        currentHp = maxHp;
        shield = 0;
        slotGrid = new SlotGrid();
        resourceHolder = new ResourceHolder(maxRes);
    }

    public void TakeDamage(int amount)
    {
        // シールドが先に吸収
        if (shield > 0)
        {
            int absorbed = Mathf.Min(shield, amount);
            shield -= absorbed;
            amount -= absorbed;
            OnShieldChanged?.Invoke(this);
        }
        if (amount > 0)
        {
            currentHp = Mathf.Max(0, currentHp - amount);
            OnHpChanged?.Invoke(this);
            if (currentHp <= 0) OnDefeated?.Invoke(this);
        }
    }

    public void AddShield(int amount)
    {
        shield += amount;
        OnShieldChanged?.Invoke(this);
    }

    // ターン開始時に呼ぶ。シールドを半減（切り捨て）
    public void HalfShield()
    {
        shield = shield / 2;
        OnShieldChanged?.Invoke(this);
    }

    public bool IsDefeated() => currentHp <= 0;
    public string Name => playerIndex == 0 ? "Player1" : "Player2";
}
