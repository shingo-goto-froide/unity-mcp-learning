using System.Collections.Generic;

[System.Serializable]
public class ResourceHolder
{
    public int maxCapacity = 6;
    public List<ResourceType> resources = new List<ResourceType>();

    public ResourceHolder(int cap = 6) { maxCapacity = cap; }

    public bool Add(ResourceType t) { if (IsFull()) return false; resources.Add(t); return true; }
    public bool Remove(ResourceType t) => resources.Remove(t);
    public bool IsFull() => resources.Count >= maxCapacity;
    public int TotalCount => resources.Count;
    public int GetCount(ResourceType t) { int c = 0; foreach (var r in resources) if (r == t) c++; return c; }
}
