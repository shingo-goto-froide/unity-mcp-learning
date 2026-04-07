using UnityEngine;
using UnityEngine.UI;

public class PoolPanelUI : MonoBehaviour
{
    public PoolRowUI[] poolRows = new PoolRowUI[3];
    public Button skipButton;

    public void Initialize()
    {
        for (int i = 0; i < 3; i++)
            if (poolRows[i] != null) poolRows[i].Initialize(i);
        skipButton?.onClick.RemoveAllListeners();
        skipButton?.onClick.AddListener(() => GameManager.Instance?.SkipAcquire());
        if (skipButton != null) skipButton.gameObject.SetActive(false);
    }

    public void Refresh(ResourcePoolManager mgr, GamePhase phase)
    {
        bool acquirePhase = phase == GamePhase.AcquireP1 || phase == GamePhase.AcquireP2;
        bool isHumanTurn  = GameSettings.Mode != GameMode.AI
            || GameManager.Instance?.CurrentActorIndex != 1;
        for (int i = 0; i < 3; i++)
            poolRows[i]?.Refresh(mgr.pools[i], acquirePhase);
        if (skipButton != null)
        {
            // Acquireフェーズの人間ターン中は常に表示
            skipButton.gameObject.SetActive(acquirePhase && isHumanTurn);
            // 満杯のときはオレンジ強調、通常はグレー
            if (acquirePhase && isHumanTurn && GameManager.Instance != null)
            {
                var g = GameManager.Instance;
                bool full = g.players[g.CurrentActorIndex].resourceHolder.IsFull();
                var img = skipButton.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                    img.color = full
                        ? new Color(0.9f, 0.5f, 0.1f, 1f)   // 満杯: オレンジ
                        : new Color(0.20f, 0.50f, 0.30f, 1f); // 通常: 緑（TakeButtonと同色）
            }
        }
    }
}
