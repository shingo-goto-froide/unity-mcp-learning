using System.Collections.Generic;
using System;

public class ResourcePoolManager
{
    public ResourcePool[] pools = new ResourcePool[3];
    public event Action OnPoolsChanged;

    public ResourcePoolManager()
    {
        for (int i = 0; i < 3; i++) pools[i] = new ResourcePool(i);
    }

    public void RefillAll()
    {
        foreach (var p in pools) p.TopUp();
        OnPoolsChanged?.Invoke();
    }

    public List<ResourceType> SelectPool(int poolIdx, PlayerData player)
    {
        if (poolIdx < 0 || poolIdx >= 3) return new List<ResourceType>();
        var taken = pools[poolIdx].TakeTwo();
        foreach (var r in taken) player.resourceHolder.Add(r);
        OnPoolsChanged?.Invoke();
        return taken;
    }
}
