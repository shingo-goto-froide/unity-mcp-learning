using UnityEngine;

public class PoolPanelUI : MonoBehaviour
{
    public PoolRowUI[] poolRows = new PoolRowUI[3];

    public void Initialize()
    {
        for (int i = 0; i < 3; i++)
            if (poolRows[i] != null) poolRows[i].Initialize(i);
    }

    public void Refresh(ResourcePoolManager mgr, GamePhase phase)
    {
        bool canTake = phase == GamePhase.AcquireP1 || phase == GamePhase.AcquireP2;
        for (int i = 0; i < 3; i++)
            poolRows[i]?.Refresh(mgr.pools[i], canTake);
    }
}
