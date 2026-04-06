using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PoolRowUI : MonoBehaviour
{
    public TextMeshProUGUI poolLabel;
    public TextMeshProUGUI[] chipLabels = new TextMeshProUGUI[3];
    public Image[]           chipImages = new Image[3];
    public TextMeshProUGUI   takeButtonLabel;
    public Button            takeButton;

    int _poolIndex;

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
        if (takeButton != null)
            takeButton.interactable = acquirePhase && isHumanTurn;
    }

    public void Initialize(int index)
    {
        _poolIndex = index;
        if (poolLabel != null) poolLabel.text = $"Pool {index}:";

        takeButton?.onClick.RemoveAllListeners();
        takeButton?.onClick.AddListener(() =>
        {
            GameManager.Instance?.SelectPool(_poolIndex);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        });

        // 二重購読を防いでフェーズ変更を購読
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
            GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
            // 現在のフェーズで即時反映
            OnPhaseChanged(GameManager.Instance.turnManager.currentPhase);
        }
    }

    public void Refresh(ResourcePool pool, bool interactable)
    {
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

        if (takeButton != null)    takeButton.interactable = interactable;
        if (takeButtonLabel != null)
            takeButtonLabel.text = res.Count <= 1 ? "Take x1" : "Take x2";
    }
}