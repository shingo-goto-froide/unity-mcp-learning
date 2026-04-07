using UnityEngine;

/// <summary>
/// RulesPanel の開閉を自己完結で管理するコンポーネント。
/// CloseBtn の onClick から Close() を呼ぶことで Prefab 内で完結する。
/// </summary>
public class RulesPanelController : MonoBehaviour
{
    public void Open()  => gameObject.SetActive(true);
    public void Close() => gameObject.SetActive(false);
}
