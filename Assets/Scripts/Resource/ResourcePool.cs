using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ResourcePool
{
    public int poolIndex;
    public List<ResourceType> resources = new List<ResourceType>();

    public ResourcePool(int idx) { poolIndex = idx; Refill(); }

    public List<ResourceType> TakeTwo()
    {
        var taken = new List<ResourceType>();
        int n = Mathf.Min(2, resources.Count);
        for (int i = 0; i < n; i++) { taken.Add(resources[0]); resources.RemoveAt(0); }
        return taken;
    }

    public bool IsEmpty() => resources.Count == 0;

    public void TopUp(int maxCount = 3)
    {
        ResourceType[] types = { ResourceType.Attack, ResourceType.Defense, ResourceType.Disrupt };
        while (resources.Count < maxCount)
            resources.Add(types[Random.Range(0, types.Length)]);
    }
    public int Count => resources.Count;

    public void Refill()
    {
        resources.Clear();
        ResourceType[] types = { ResourceType.Attack, ResourceType.Defense, ResourceType.Disrupt };
        for (int i = 0; i < 3; i++) resources.Add(types[Random.Range(0, types.Length)]);
    }
}
