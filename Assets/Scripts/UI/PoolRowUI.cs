using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PoolRowUI : MonoBehaviour
{
    public TextMeshProUGUI poolLabel;
    public TextMeshProUGUI[] chipLabels = new TextMeshProUGUI[3];
    public Image[]           chipImages = new Image[3];
    public TextMeshProUGUI   takeButtonLabel;
    public Button            takeButton;

    [Header("B: ボタン内プレビューチップ")]
    public Image[] previewChipImages = new Image[2];

    int _poolIndex;
    ResourcePool _currentPool;

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
    }

    void OnPhaseChanged(GamePhase phase)
    {
        bool acquirePhase = phase == GamePhase.AcquireP1 || phase == GamePhase.AcquireP2;
        bool isHumanTurn = GameSettings.Mode != GameMode.AI
            || GameManager.Instance?.CurrentActorIndex != 1;
        bool playerFull = false;
        if (acquirePhase && isHumanTurn && GameManager.Instance != null)
        {
            var gm = GameManager.Instance;
            playerFull = gm.players[gm.CurrentActorIndex].resourceHolder.IsFull();
        }
        if (takeButton != null)
            takeButton.interactable = acquirePhase && isHumanTurn && !playerFull;
    }

    public void Initialize(int index)
    {
        _poolIndex = index;
        if (poolLabel != null) poolLabel.text = $"プール {index}:";

        takeButton?.onClick.RemoveAllListeners();
        takeButton?.onClick.AddListener(() =>
        {
            GameManager.Instance?.SelectPool(_poolIndex);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        });

        // A: TakeButtonにホバーイベントを追加
        if (takeButton != null)
        {
            var trigger = takeButton.GetComponent<EventTrigger>()
                       ?? takeButton.gameObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => HighlightChips(true));
            trigger.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => HighlightChips(false));
            trigger.triggers.Add(exitEntry);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
            GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
            OnPhaseChanged(GameManager.Instance.turnManager.currentPhase);
        }
    }

    public void Refresh(ResourcePool pool, bool interactable)
    {
        _currentPool = pool;
        var res = pool.resources;

        for (int i = 0; i < 3; i++)
        {
            bool exists = i < res.Count;
            if (chipLabels[i] != null)
            {
                chipLabels[i].text = exists
                    ? (i < 2 ? $"{i+1}:{PlayerPanelUI.Short(res[i])}" : $"({PlayerPanelUI.Short(res[i])})")
                    : "-";
            }
            if (chipImages[i] != null)
                chipImages[i].color = exists ? PlayerPanelUI.ResColor(res[i]) : new Color(0.15f, 0.15f, 0.2f);
        }

        // B: ボタン内プレビューチップの色を更新
        for (int i = 0; i < 2; i++)
        {
            if (previewChipImages[i] == null) continue;
            bool exists = i < res.Count;
            previewChipImages[i].color = exists
                ? PlayerPanelUI.ResColor(res[i])
                : new Color(0.2f, 0.2f, 0.25f, 0.5f);
            previewChipImages[i].gameObject.SetActive(exists);
        }

        if (takeButton != null)    takeButton.interactable = interactable;
        if (takeButtonLabel != null)
            takeButtonLabel.text = res.Count <= 1 ? "取得 x1" : "取得 x2";
    }

    // A: ホバー時にチップの明暗を切り替える
    void HighlightChips(bool highlight)
    {
        if (_currentPool == null) return;
        var _gm = GameManager.Instance;
        if (_gm == null) return;

        // highlight=false の場合は常に色をリセット（フェーズ問わず）
        if (!highlight)
        {
            var res0 = _currentPool.resources;
            for (int i = 0; i < 3; i++)
            {
                if (chipImages[i] == null) continue;
                bool exists = i < res0.Count;
                chipImages[i].color = exists ? PlayerPanelUI.ResColor(res0[i]) : new Color(0.15f, 0.15f, 0.2f);
            }
            return;
        }

        // highlight=true はAcquireフェーズかつ人間のターンのみ
        var _ph = _gm.turnManager.currentPhase;
        if (_ph != GamePhase.AcquireP1 && _ph != GamePhase.AcquireP2) return;
        if (GameSettings.Mode == GameMode.AI && _gm.CurrentActorIndex == 1) return;
        var res = _currentPool.resources;
        for (int i = 0; i < 3; i++)
        {
            if (chipImages[i] == null) continue;
            bool exists = i < res.Count;
            if (!exists) continue;
            Color baseColor = PlayerPanelUI.ResColor(res[i]);
            if (highlight)
                chipImages[i].color = i < 2
                    ? Color.Lerp(baseColor, Color.white, 0.45f)  // 取得される2枚：明るく
                    : Color.Lerp(baseColor, Color.black, 0.55f); // 残る1枚：暗く
            else
                chipImages[i].color = baseColor; // 通常に戻す
        }
    }
}
