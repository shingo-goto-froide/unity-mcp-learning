using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ControlPanelUI : MonoBehaviour
{
    [Header("Phase")]
    public TextMeshProUGUI phaseLabel;

    [Header("Resource Buttons")]
    public Button attackBtn;
    public Button defenseBtn;
    public Button disruptBtn;

    [Header("Row Buttons")]
    public Button[] rowBtns = new Button[5];

    [Header("Actions")]
    public Button endAssignBtn;
    public Button resetBtn;

    [Header("Selected Indicator")]
    public TextMeshProUGUI selectedLabel;

    ResourceType _selected = ResourceType.None;

    static readonly Color normalBtnColor   = new Color(0.22f, 0.32f, 0.52f);
    static readonly Color selectedColor    = new Color(0.8f,  0.7f,  0.1f);
    static readonly Color disabledColor    = new Color(0.18f, 0.18f, 0.22f);
    static readonly Color resetNormalColor = new Color(0.5f,  0.18f, 0.18f);

    public void Initialize()
    {
        attackBtn? .onClick.RemoveAllListeners();
        defenseBtn?.onClick.RemoveAllListeners();
        disruptBtn?.onClick.RemoveAllListeners();
        endAssignBtn?.onClick.RemoveAllListeners();
        resetBtn?.onClick.RemoveAllListeners();
        for (int i = 0; i < 5; i++)
            rowBtns[i]?.onClick.RemoveAllListeners();

        attackBtn? .onClick.AddListener(() => SelectResource(ResourceType.Attack));
        defenseBtn?.onClick.AddListener(() => SelectResource(ResourceType.Defense));
        disruptBtn?.onClick.AddListener(() => SelectResource(ResourceType.Disrupt));

        endAssignBtn?.onClick.AddListener(() =>
        {
            GameManager.Instance?.EndAssign();
            _selected = ResourceType.None;
            Refresh();
        });

        resetBtn?.onClick.AddListener(() =>
        {
            GameManager.Instance?.ResetAssign();
            _selected = ResourceType.None;
            Refresh();
            GameUINew.Instance?.RefreshAll();
        });

        for (int i = 0; i < 5; i++)
        {
            int row = i;
            rowBtns[i]?.onClick.AddListener(() =>
            {
                if (_selected == ResourceType.None) return;
                GameManager.Instance?.AssignResource(row, _selected);
                var gm = GameManager.Instance;
                if (gm != null && gm.players != null)
                {
                    int pi = gm.CurrentActorIndex;
                    if (gm.players[pi].resourceHolder.GetCount(_selected) == 0)
                        _selected = ResourceType.None;
                }
                Refresh();
                GameUINew.Instance?.RefreshAll();
                // マウスが同じ行上にある場合、次のスロットを再ハイライト
                HoverRow(row, true);
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
            });

            // ホバーイベント追加
            if (rowBtns[i] == null) continue;
            var trigger = rowBtns[i].GetComponent<EventTrigger>()
                       ?? rowBtns[i].gameObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => HoverRow(row, true));
            trigger.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => HoverRow(row, false));
            trigger.triggers.Add(exitEntry);
        }
    }

    // 行ボタンホバー時：次アサイン予定スロット単体をハイライト
    void HoverRow(int rowIdx, bool on)
    {
        var ui = GameUINew.Instance;
        var gm = GameManager.Instance;
        if (ui == null || gm == null) return;

        // リソース未選択 or 配置不可行はハイライトしない
        if (on && (_selected == ResourceType.None
            || !gm.players[gm.CurrentActorIndex].slotGrid.rows[rowIdx].CanAssign(_selected)))
            return;

        var panel = gm.CurrentActorIndex == 0 ? ui.player1Panel : ui.player2Panel;
        int slotIdx = gm.players[gm.CurrentActorIndex].slotGrid.rows[rowIdx].filledCount;
        panel?.HoverSlot(rowIdx, slotIdx, on);
    }

    void SelectResource(ResourceType t)
    {
        _selected = (_selected == t) ? ResourceType.None : t;
        Refresh();
    }

    public void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.players == null) return;
        var ph = gm.turnManager.currentPhase;

        if (phaseLabel != null)
        {
            var rMsg = GameManager.Instance?.ResolveMessage;
            phaseLabel.text = string.IsNullOrEmpty(rMsg)
                ? $"Turn {gm.turnManager.turnCount}  |  {ToJP(ph)}"
                : rMsg;
        }

        bool isAssign = ph == GamePhase.AssignP1 || ph == GamePhase.AssignP2;
        int pi = gm.CurrentActorIndex;
        bool isHumanTurn = GameSettings.Mode != GameMode.AI || pi != 1;
        isAssign = isAssign && isHumanTurn;

        if (isAssign)
        {
            var h = gm.players[pi].resourceHolder;
            SetResBtn(attackBtn,  ResourceType.Attack,  h.GetCount(ResourceType.Attack)  > 0);
            SetResBtn(defenseBtn, ResourceType.Defense, h.GetCount(ResourceType.Defense) > 0);
            SetResBtn(disruptBtn, ResourceType.Disrupt, h.GetCount(ResourceType.Disrupt) > 0);
        }

        if (selectedLabel != null)
            selectedLabel.text = $"選択中: {(_selected == ResourceType.None ? "-" : _selected.ToString())}";

        Color rowNormal = new Color(0.22f, 0.32f, 0.48f);
        for (int i = 0; i < 5; i++)
        {
            if (rowBtns[i] == null) continue;
            bool canPlace = isAssign
                && _selected != ResourceType.None
                && gm.players[pi].slotGrid.rows[i].CanAssign(_selected);
            rowBtns[i].interactable = canPlace;
            var img = rowBtns[i].GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = canPlace ? rowNormal : disabledColor;
        }

        if (endAssignBtn != null) endAssignBtn.interactable = isAssign;
        if (resetBtn != null)
        {
            bool canReset = isAssign && gm.CanResetAssign;
            resetBtn.interactable = canReset;
            var img = resetBtn.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = canReset ? resetNormalColor : disabledColor;
        }

        attackBtn?.gameObject.SetActive(isAssign);
        defenseBtn?.gameObject.SetActive(isAssign);
        disruptBtn?.gameObject.SetActive(isAssign);
        if (selectedLabel != null) selectedLabel.gameObject.SetActive(isAssign);
        for (int i = 0; i < 5; i++) rowBtns[i]?.gameObject.SetActive(isAssign);
        endAssignBtn?.gameObject.SetActive(isAssign);
        resetBtn?.gameObject.SetActive(isAssign);
    }

    static string ToJP(GamePhase ph)
    {
        switch (ph)
        {
            case GamePhase.AcquireP1:
            case GamePhase.AcquireP2: return "獲得フェーズ";
            case GamePhase.AssignP1:
            case GamePhase.AssignP2:  return "配置フェーズ";
            case GamePhase.Resolve:   return "発動フェーズ";
            default:                  return ph.ToString();
        }
    }

    void SetResBtn(Button btn, ResourceType t, bool hasResource)
    {
        if (btn == null) return;
        btn.interactable = hasResource;
        var img = btn.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
            img.color = !hasResource ? disabledColor
                      : _selected == t ? selectedColor
                      : PlayerPanelUI.ResColor(t);
    }
}
