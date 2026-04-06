using UnityEngine;

public class GameUI : MonoBehaviour
{
    GameManager gm;
    ResourceType selectedRes = ResourceType.None;

    void Start()
    {
        gm = GameManager.Instance;
        gm.OnPhaseChanged += _ => selectedRes = ResourceType.None;
    }

    void OnGUI()
    {
        if (gm?.players == null) return;
        var p1 = gm.players[0];
        var p2 = gm.players[1];
        var ph = gm.turnManager.currentPhase;

        GUI.Box(new Rect(Screen.width/2-130, 5, 260, 28),
            $"Turn {gm.turnManager.turnCount}  |  {ph}");

        DrawPlayer(p1, 5);
        DrawPlayer(p2, Screen.width - 205);
        DrawPools(Screen.width/2 - 185, 42);
        DrawControls(ph);
    }

    void DrawPlayer(PlayerData p, float x)
    {
        GUI.Box(new Rect(x, 40, 200, 370), p.Name);
        GUI.Label(new Rect(x+10, 65, 180, 22), $"HP: {p.currentHp} / {p.maxHp}");
        GUI.Label(new Rect(x+10, 88, 180, 18), "--- Slots ---");
        for (int i = 0; i < 5; i++)
        {
            var r = p.slotGrid.rows[i];
            string s = $"R{i+1}({r.slotCount}): {r.assignedType} {r.filledCount}/{r.slotCount}";
            if (r.lockedCount > 0) s += $" [L:{r.lockedCount}]";
            GUI.Label(new Rect(x+10, 108+i*22, 180, 20), s);
        }
        GUI.Label(new Rect(x+10, 220, 180, 18), $"--- Res({p.resourceHolder.TotalCount}/6) ---");
        var res = p.resourceHolder.resources;
        for (int i = 0; i < res.Count && i < 6; i++)
            GUI.Label(new Rect(x+10, 240+i*20, 180, 18), $"  {res[i]}");
    }

    void DrawPools(float x, float y)
    {
        float boxW = 370f;
        float rowH = 28f;
        float resW = 80f;
        GUI.Box(new Rect(x, y, boxW, 28 + 3 * rowH), "-- Resource Pools --");
        var ph = gm.turnManager.currentPhase;
        for (int i = 0; i < 3; i++)
        {
            float ry = y + 26 + i * rowH;
            GUI.Label(new Rect(x + 5, ry + 5, 48, 20), $"Pool{i}:");
            var res = gm.poolManager.pools[i].resources;
            for (int j = 0; j < res.Count; j++)
            {
                string label = j < 2 ? $"{j+1}:{res[j]}" : $"({res[j]})";
                GUI.Label(new Rect(x + 55 + j * resW, ry + 5, resW - 4, 20), label);
            }
            if (ph == GamePhase.AcquireP1 || ph == GamePhase.AcquireP2)
            {
                int cnt = gm.poolManager.pools[i].Count;
                string takeLabel = cnt <= 1 ? "Take x1" : "Take x2";
                if (GUI.Button(new Rect(x + 295, ry + 3, 68, 22), takeLabel))
                    gm.SelectPool(i);
            }
        }
    }

    void DrawControls(GamePhase ph)
    {
        float cx = Screen.width/2 - 165, cy = Screen.height - 170;
        if (ph == GamePhase.AssignP1 || ph == GamePhase.AssignP2)
        {
            GUI.Box(new Rect(cx, cy, 330, 160), $"[ {ph} ] - Assign Resources");
            GUI.Label(new Rect(cx+10, cy+28, 310, 22), $"Selected: {selectedRes}");

            int pi = ph == GamePhase.AssignP1 ? 0 : 1;
            var holder = gm.players[pi].resourceHolder;

            Color defaultBg = GUI.backgroundColor;

            GUI.enabled = holder.GetCount(ResourceType.Attack) > 0;
            GUI.backgroundColor = selectedRes == ResourceType.Attack ? Color.yellow : defaultBg;
            if (GUI.Button(new Rect(cx+10,  cy+52, 95, 28), "Attack"))  selectedRes = ResourceType.Attack;

            GUI.enabled = holder.GetCount(ResourceType.Defense) > 0;
            GUI.backgroundColor = selectedRes == ResourceType.Defense ? Color.yellow : defaultBg;
            if (GUI.Button(new Rect(cx+115, cy+52, 95, 28), "Defense")) selectedRes = ResourceType.Defense;

            GUI.enabled = holder.GetCount(ResourceType.Disrupt) > 0;
            GUI.backgroundColor = selectedRes == ResourceType.Disrupt ? Color.yellow : defaultBg;
            if (GUI.Button(new Rect(cx+220, cy+52, 95, 28), "Disrupt")) selectedRes = ResourceType.Disrupt;

            GUI.enabled = true;
            GUI.backgroundColor = defaultBg;

            GUI.Label(new Rect(cx+10, cy+88, 60, 22), "→ Row:");
            for (int i = 0; i < 5; i++)
                if (GUI.Button(new Rect(cx+70+i*52, cy+86, 48, 26), $"R{i+1}") &&
                    selectedRes != ResourceType.None)
                {
                    gm.AssignResource(i, selectedRes);
                    // 所持数が0になったら選択解除
                    if (holder.GetCount(selectedRes) == 0)
                        selectedRes = ResourceType.None;
                }

            GUI.enabled = gm.CanResetAssign;
            GUI.backgroundColor = gm.CanResetAssign ? new Color(1f, 0.4f, 0.4f) : defaultBg;
            if (GUI.Button(new Rect(cx+10, cy+125, 90, 28), "Reset"))
            {
                gm.ResetAssign();
                selectedRes = ResourceType.None;
            }
            GUI.enabled = true;
            GUI.backgroundColor = defaultBg;

            if (GUI.Button(new Rect(cx+115, cy+125, 110, 28), "End Assign"))
                gm.EndAssign();
        }

        if (ph == GamePhase.GameOver)
        {
            GUI.Box(new Rect(Screen.width/2-110, Screen.height/2-35, 220, 85), "=== GAME OVER ===");
            if (GUI.Button(new Rect(Screen.width/2-65, Screen.height/2+15, 130, 32), "Play Again"))
                gm.StartGame();
        }
    }
}
