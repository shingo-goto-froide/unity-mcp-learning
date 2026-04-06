using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerPanelUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI shieldText;   // HPバー横のシールド表示
    public Slider hpSlider;
    public Transform slotsContainer;
    public Transform resContainer;
    public TextMeshProUGUI resLabel;

    [Header("Prefabs (optional)")]
    public GameObject resourceChipPrefab;

    PlayerData _player;
    int _highlightedRow = -1;  // Resolve演出中のハイライト行（-1=なし）
    bool _slotsHidden = false;   // Assignフェーズ中の相手スロット隠蔽フラグ

    static readonly Color[] rowColors =
    {
        new Color(0.32f, 0.2f,  0.1f),
        new Color(0.28f, 0.18f, 0.12f),
        new Color(0.18f, 0.2f,  0.28f),
        new Color(0.14f, 0.18f, 0.3f),
        new Color(0.1f,  0.14f, 0.32f),
    };

    public void Initialize(PlayerData player, GameObject chipPrefab)
    {
        _player = player;
        resourceChipPrefab = chipPrefab;
        if (playerNameText != null) playerNameText.text = player.Name;
        if (hpSlider != null) hpSlider.maxValue = player.maxHp;

        // HP・シールド変化を購読
        player.OnHpChanged     += _ => RefreshHP();
        player.OnShieldChanged += _ => RefreshHP();

        Refresh();
    }

    public void Refresh()
    {
        if (_player == null) return;
        RefreshHP();
        RefreshSlots();
        RefreshResources();
    }

    void RefreshHP()
    {
        if (hpText != null)
            hpText.text = $"HP:{_player.currentHp}/{_player.maxHp}";
        if (hpSlider != null)
            hpSlider.value = _player.currentHp;
        if (shieldText != null)
        {
            shieldText.text = $"Shield {_player.shield}";
            shieldText.gameObject.SetActive(true);
        }
    }

    void RefreshSlots()
    {
        if (slotsContainer == null) return;
        for (int row = 0; row < 5; row++)
        {
            if (row >= slotsContainer.childCount) break;
            var rowTf   = slotsContainer.GetChild(row);
            var slotRow = _player.slotGrid.rows[row];

            for (int s = 0; s <= row; s++)
            {
                if (s >= rowTf.childCount) break;
                var slotTf  = rowTf.GetChild(s);
                var lbl     = slotTf.Find("Label")?.GetComponent<TextMeshProUGUI>()
                           ?? slotTf.Find("Txt")?.GetComponent<TextMeshProUGUI>();
                var lockOvl = slotTf.Find("LockedOverlay")?.gameObject;
                var img     = slotTf.GetComponent<Image>();

                // スロット隠蔽中は中身を隠す
                if (_slotsHidden)
                {
                    if (lbl != null) lbl.text = "";
                    if (img != null) img.color = new Color(0.1f, 0.1f, 0.14f, 1f);
                    if (lockOvl != null) lockOvl.SetActive(false);
                    continue;
                }
                bool filled = s < slotRow.filledCount;
                // 行にロックがあれば未充填スロット全体にオーバーレイ表示
                bool locked = slotRow.lockedCount > 0 && !filled;

                if (lbl != null)     lbl.text = filled ? Short(slotRow.assignedType) : "";
                // ハイライト中はスロット背景色を黄色に上書き
                Color slotColor = filled ? ResColor(slotRow.assignedType) : rowColors[row];
                if (row == _highlightedRow && !locked)
                    slotColor = Color.Lerp(slotColor, new Color(1f, 0.85f, 0.1f), 0.7f);
                if (img != null)     img.color = slotColor;
                if (lockOvl != null) lockOvl.SetActive(locked);
            }
        }
    }

    void RefreshResources()
    {
        if (resContainer == null) return;
        // SetParent(null)で即座に親から切り離してから破棄（Destroyの遅延でチップが残るのを防ぐ）
        var toDestroy = new System.Collections.Generic.List<GameObject>();
        foreach (Transform c in resContainer) toDestroy.Add(c.gameObject);
        foreach (var go in toDestroy) { go.transform.SetParent(null); Object.Destroy(go); }

        var res = _player.resourceHolder.resources;
        if (resLabel != null) resLabel.text = $"Res ({res.Count}/6)";

        foreach (var r in res)
        {
            GameObject chip;
            if (resourceChipPrefab != null)
            {
                chip = Object.Instantiate(resourceChipPrefab, resContainer);
            }
            else
            {
                chip = new GameObject("Chip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                chip.transform.SetParent(resContainer, false);
                var rt = chip.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(48, 28);
                var lblGo = new GameObject("Txt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                lblGo.transform.SetParent(chip.transform, false);
                var lblRt = lblGo.GetComponent<RectTransform>();
                lblRt.anchorMin = Vector2.zero;
                lblRt.anchorMax = Vector2.one;
                lblRt.offsetMin = Vector2.zero;
                lblRt.offsetMax = Vector2.zero;
                var tmp = lblGo.GetComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize  = 14;
                tmp.color     = Color.white;
            }

            var lbl2 = chip.GetComponentInChildren<TextMeshProUGUI>();
            var img2 = chip.GetComponent<Image>();
            if (lbl2 != null) lbl2.text = Short(r);
            if (img2 != null) img2.color = ResColor(r);
        }
    }

    // Resolve演出用：指定行のスロット全体をハイライト
    public void HighlightRow(int rowIdx, bool on)
    {
        _highlightedRow = on ? rowIdx : -1;
        RefreshSlots();  // スロット再描画でハイライト反映
    }

    // Assignフェーズ中に相手スロットを隠す（全スロットを暗色・テキスト非表示）
    public void SetSlotsHidden(bool hidden)
    {
        _slotsHidden = hidden;
        RefreshSlots();
    }

    public void SetTurnActive(bool isActive)
    {
        var img = GetComponent<Image>();
        if (img != null)
            img.color = new Color(0.11f, 0.11f, 0.17f, 0.97f);

        if (playerNameText != null)
        {
            playerNameText.fontSize = isActive ? 24 : 18;
            playerNameText.color    = isActive ? new Color(1f, 0.92f, 0.3f) : new Color(0.6f, 0.6f, 0.6f);
        }

        var turnLbl = transform.Find("TurnLabel");
        if (turnLbl != null) turnLbl.gameObject.SetActive(isActive);
    }

    public static string Short(ResourceType t) => t switch
    {
        ResourceType.Attack  => "ATK",
        ResourceType.Defense => "DEF",
        ResourceType.Disrupt => "DIS",
        _ => ""
    };

    public static Color ResColor(ResourceType t) => t switch
    {
        ResourceType.Attack  => new Color(0.75f, 0.2f,  0.2f),
        ResourceType.Defense => new Color(0.2f,  0.4f,  0.8f),
        ResourceType.Disrupt => new Color(0.6f,  0.2f,  0.8f),
        _ => new Color(0.2f, 0.22f, 0.32f)
    };
}
