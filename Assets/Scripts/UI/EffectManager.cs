using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }
    RectTransform _canvasRt;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _canvasRt = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    public void PlayAttack(RectTransform srcRowRt, RectTransform dstPanelRt, int damage)
        => StartCoroutine(AttackCoroutine(srcRowRt, dstPanelRt, damage));

    public void PlayDefense(RectTransform defenderPanelRt, RectTransform opponentPanelRt)
        => StartCoroutine(DefenseCoroutine(defenderPanelRt, opponentPanelRt));

    public void PlayDisrupt(RectTransform targetPanelRt)
    {
        Color c = new Color(0.65f, 0.2f, 0.9f);
        StartCoroutine(FlashPanel(targetPanelRt, c, 0.3f));
        StartCoroutine(FloatText("LOCK!", GetPanelCenter(targetPanelRt), c, 36f, 1.2f));
    }

    // atkRowRt=攻撃側Row, atkPanelRt=攻撃側Panel, defPanelRt=防御側Panel, reflectDmg=反射ダメージ
    public void PlayJustGuard(RectTransform atkRowRt, RectTransform atkPanelRt, RectTransform defPanelRt, int reflectDmg)
        => StartCoroutine(JustGuardCoroutine(atkRowRt, atkPanelRt, defPanelRt, reflectDmg));

    // ─── ATK: 光球 → 内側エッジで爆発 ────────────────────────────────────────

    IEnumerator AttackCoroutine(RectTransform srcRt, RectTransform dstRt, int damage)
    {
        if (_canvasRt == null) yield break;
        Color atkColor = new Color(1f, 0.2f, 0.1f);
        Vector2 srcPos  = ToCanvasLocal(srcRt.position);
        Vector2 edgePos = GetInnerEdgePoint(srcRt, dstRt, 22f);

        var orb = CreateOrbGO(atkColor);
        var orbRt = orb.GetComponent<RectTransform>();
        orbRt.anchoredPosition = srcPos;

        for (float t = 0; t < 0.7f; t += Time.deltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / 0.7f);
            orbRt.anchoredPosition = Vector2.Lerp(srcPos, edgePos, p);
            orbRt.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.2f, p);
            yield return null;
        }
        Destroy(orb);

        StartCoroutine(BurstCoroutine(edgePos, atkColor));
        StartCoroutine(FlashPanel(dstRt, atkColor, 0.35f));
        StartCoroutine(FloatText($"DMG -{damage}", GetPanelCenter(dstRt), atkColor, 36f, 1.2f));
    }

    // ─── JUST GUARD: 跳ね返りアニメーション ──────────────────────────────────

    IEnumerator JustGuardCoroutine(RectTransform atkRowRt, RectTransform atkPanelRt, RectTransform defPanelRt, int reflectDmg)
    {
        if (_canvasRt == null) yield break;
        Color atkColor = new Color(1f, 0.2f, 0.1f);
        Color jgColor  = new Color(1f, 0.85f, 0.1f);

        Vector2 atkRowPos = ToCanvasLocal(atkRowRt.position);
        // defPanelの内側エッジ（atkPanel側を向く辺）
        Vector3[] dfc = new Vector3[4]; defPanelRt.GetWorldCorners(dfc);
        Vector2 dfBL = ToCanvasLocal(dfc[0]), dfTR = ToCanvasLocal(dfc[2]);
        Vector2 dfCenter = (dfBL + dfTR) * 0.5f;
        bool atkIsLeft = atkRowPos.x < dfCenter.x;
        float defInnerX = atkIsLeft ? dfBL.x : dfTR.x;
        float defInsetDir = atkIsLeft ? +22f : -22f;
        Vector2 defEdgePos = new Vector2(defInnerX + defInsetDir, dfCenter.y);

        // Phase1: 赤い光球が攻撃側Row → 防御側エッジへ
        var orb1   = CreateOrbGO(atkColor);
        var orb1Rt = orb1.GetComponent<RectTransform>();
        orb1Rt.anchoredPosition = atkRowPos;

        for (float t = 0; t < 0.5f; t += Time.deltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / 0.5f);
            orb1Rt.anchoredPosition = Vector2.Lerp(atkRowPos, defEdgePos, p);
            orb1Rt.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.1f, p);
            yield return null;
        }
        Destroy(orb1);

        // Phase2: 防御側エッジでバースト → JUST GUARD! テキスト（防御側パネル中心）
        StartCoroutine(BurstCoroutine(defEdgePos, jgColor));
        StartCoroutine(FlashPanel(defPanelRt, jgColor, 0.3f));
        StartCoroutine(ShieldVisual(defPanelRt, atkPanelRt));  // ガードエフェクト
        StartCoroutine(FloatText("JUST GUARD!", GetPanelCenter(defPanelRt), jgColor, 36f, 1.3f));

        yield return new WaitForSeconds(0.2f);

        // Phase3: 金色の光球が跳ね返って攻撃側エッジへ
        Vector3[] afc = new Vector3[4]; atkPanelRt.GetWorldCorners(afc);
        Vector2 afBL = ToCanvasLocal(afc[0]), afTR = ToCanvasLocal(afc[2]);
        Vector2 afCenter = (afBL + afTR) * 0.5f;
        bool defIsLeft = dfCenter.x < afCenter.x;
        float atkInnerX = defIsLeft ? afBL.x : afTR.x;
        float atkInsetDir = defIsLeft ? +22f : -22f;
        Vector2 atkEdgePos = new Vector2(atkInnerX + atkInsetDir, afCenter.y);
        var orb2   = CreateOrbGO(jgColor);
        var orb2Rt = orb2.GetComponent<RectTransform>();
        orb2Rt.anchoredPosition = defEdgePos;

        for (float t = 0; t < 0.55f; t += Time.deltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / 0.55f);
            orb2Rt.anchoredPosition = Vector2.Lerp(defEdgePos, atkEdgePos, p);
            orb2Rt.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.85f, p);
            yield return null;
        }
        Destroy(orb2);

        // Phase4: 攻撃側にヒット → バースト + フラッシュ + DMG テキスト
        StartCoroutine(BurstCoroutine(atkEdgePos, jgColor));
        StartCoroutine(FlashPanel(atkPanelRt, jgColor, 0.4f));
        StartCoroutine(FloatText($"DMG -{reflectDmg}", GetPanelCenter(atkPanelRt), new Color(1f, 0.2f, 0.1f), 36f, 1.2f));
    }

    // ─── DEF: 縦の盾エフェクト ────────────────────────────────────────────────

    IEnumerator DefenseCoroutine(RectTransform defPanelRt, RectTransform oppPanelRt)
    {
        if (_canvasRt == null) yield break;
        Color sc = new Color(0.3f, 0.6f, 1f);

        // WorldCornersから位置・サイズを直接計算（宣言を先に）
        Vector3[] dc = new Vector3[4]; defPanelRt.GetWorldCorners(dc);
        Vector2 dBL = ToCanvasLocal(dc[0]), dTL = ToCanvasLocal(dc[1]), dTR = ToCanvasLocal(dc[2]);
        Vector2 dCenter = (dBL + dTR) * 0.5f;
        float h = (dTL - dBL).magnitude * 0.62f;

        // oppPanelが右側 → defPanelの右辺が内側エッジ、左側 → 左辺が内側エッジ
        Vector2 oCenter   = GetPanelCenter(oppPanelRt);
        bool oppIsRight   = oCenter.x > dCenter.x;
        float innerX      = oppIsRight ? dTR.x : dBL.x;
        float shieldInset = oppIsRight ? -30f : +30f;
        Vector2 pos = new Vector2(innerX + shieldInset, dCenter.y);

        var glowGo   = MakeRectAtPos(pos, new Vector2(20f, h), new Color(0.2f, 0.5f, 1f, 0.28f));
        var shieldGo = MakeRectAtPos(pos, new Vector2(7f,  h), new Color(0.55f, 0.85f, 1f, 0.92f));

        for (float t = 0; t < 0.12f; t += Time.deltaTime)
        { float p = t / 0.12f; SetSX(glowGo, p); SetSX(shieldGo, p); yield return null; }
        SetSX(glowGo, 1f); SetSX(shieldGo, 1f);
        StartCoroutine(FloatText("DEF!", GetPanelCenter(defPanelRt), sc, 34f, 1.1f));  // シールド展開と同時に表示

        yield return new WaitForSeconds(0.4f);

        var gi = glowGo.GetComponent<Image>();   Color gc  = gi.color;
        var si = shieldGo.GetComponent<Image>(); Color scc = si.color;
        for (float t = 0; t < 0.22f; t += Time.deltaTime)
        {
            float p = t / 0.22f;
            gi.color = new Color(gc.r,  gc.g,  gc.b,  gc.a  * (1f - p));
            si.color = new Color(scc.r, scc.g, scc.b, scc.a * (1f - p));
            yield return null;
        }
        Destroy(glowGo); Destroy(shieldGo);
        // DEF text at panel center
    }

    // テキストなしの盾ビジュアル（JustGuard用）
    IEnumerator ShieldVisual(RectTransform defPanelRt, RectTransform oppPanelRt)
    {
        if (_canvasRt == null) yield break;
        Vector3[] dc = new Vector3[4]; defPanelRt.GetWorldCorners(dc);
        Vector2 dBL = ToCanvasLocal(dc[0]), dTL = ToCanvasLocal(dc[1]), dTR = ToCanvasLocal(dc[2]);
        Vector2 dCenter = (dBL + dTR) * 0.5f;
        float h = (dTL - dBL).magnitude * 0.62f;
        Vector2 oCenter  = GetPanelCenter(oppPanelRt);
        bool oppIsRight  = oCenter.x > dCenter.x;
        float innerX     = oppIsRight ? dTR.x : dBL.x;
        float shieldInset = oppIsRight ? -30f : +30f;
        Vector2 pos = new Vector2(innerX + shieldInset, dCenter.y);

        var glowGo   = MakeRectAtPos(pos, new Vector2(20f, h), new Color(0.2f, 0.5f, 1f, 0.28f));
        var shieldGo = MakeRectAtPos(pos, new Vector2(7f,  h), new Color(0.55f, 0.85f, 1f, 0.92f));

        for (float t = 0; t < 0.12f; t += Time.deltaTime)
        { float p = t / 0.12f; SetSX(glowGo, p); SetSX(shieldGo, p); yield return null; }
        SetSX(glowGo, 1f); SetSX(shieldGo, 1f);

        yield return new WaitForSeconds(0.4f);

        var gi = glowGo.GetComponent<UnityEngine.UI.Image>(); Color gc = gi.color;
        var si = shieldGo.GetComponent<UnityEngine.UI.Image>(); Color sc = si.color;
        for (float t = 0; t < 0.22f; t += Time.deltaTime)
        {
            float p = t / 0.22f;
            gi.color = new Color(gc.r, gc.g, gc.b, gc.a * (1f - p));
            si.color = new Color(sc.r, sc.g, sc.b, sc.a * (1f - p));
            yield return null;
        }
        Destroy(glowGo); Destroy(shieldGo);
    }

    IEnumerator BurstCoroutine(Vector2 center, Color color)
    {
        if (_canvasRt == null) yield break;
        int cnt = 7;
        var parts = new (GameObject go, RectTransform rt, Vector2 dir)[cnt];
        for (int i = 0; i < cnt; i++)
        {
            float a  = 360f / cnt * i * Mathf.Deg2Rad;
            var go = new GameObject("B", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_canvasRt, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = Vector2.one * 0.5f;
            rt.anchoredPosition = center; rt.sizeDelta = new Vector2(12f, 12f);
            go.GetComponent<Image>().color = color;
            go.GetComponent<Image>().raycastTarget = false;
            parts[i] = (go, rt, new Vector2(Mathf.Cos(a), Mathf.Sin(a)));
        }
        for (float t = 0; t < 0.4f; t += Time.deltaTime)
        {
            float p = t / 0.4f;
            foreach (var (go, rt, dir) in parts)
            {
                if (go == null) continue;
                rt.anchoredPosition = center + dir * (65f * p);
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.15f, p);
                go.GetComponent<Image>().color = new Color(color.r, color.g, color.b, 1f - p);
            }
            yield return null;
        }
        foreach (var (go, _, _) in parts) if (go != null) Destroy(go);
    }

    // ─── 共通ヘルパー ──────────────────────────────────────────────────────────

    Vector2 GetInnerEdgePoint(RectTransform srcRt, RectTransform dstRt, float inset)
    {
        Vector2 srcC  = ToCanvasLocal(srcRt.position);
        Vector2 dstC  = GetPanelCenter(dstRt);   // 視覚的中心を使う（ピボット上端ではない）
        float halfW   = dstRt.rect.width * 0.5f;
        bool srcLeft  = srcC.x < dstC.x;
        float edgeX   = dstC.x + (srcLeft ? -halfW : +halfW);
        float insetX  = inset  * (srcLeft ?  +1f   : -1f);
        return new Vector2(edgeX + insetX, dstC.y);
    }

    Vector2 GetPanelCenter(RectTransform rt)
    {
        Vector3[] c = new Vector3[4];
        rt.GetWorldCorners(c);
        return (ToCanvasLocal(c[0]) + ToCanvasLocal(c[2])) * 0.5f;
    }

    GameObject CreateOrbGO(Color color)
    {
        var orb = new GameObject("Orb", typeof(RectTransform));
        orb.transform.SetParent(_canvasRt, false);
        var rt = orb.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = Vector2.one * 0.5f;
        MakeImg(orb, 40f, new Color(color.r, color.g, color.b, 0.35f));
        MakeImg(orb, 18f, new Color(
            Mathf.Min(1f, color.r * 0.9f + 0.2f),
            Mathf.Min(1f, color.g * 0.9f + 0.2f),
            Mathf.Min(1f, color.b * 0.9f + 0.2f), 1f));
        return orb;
    }

    GameObject MakeImg(GameObject parent, float size, Color color)
    {
        var go = new GameObject("I", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = Vector2.one * 0.5f; rt.sizeDelta = Vector2.one * size;
        var img = go.GetComponent<Image>(); img.color = color; img.raycastTarget = false;
        return go;
    }

    GameObject MakeRectAtPos(Vector2 canvasPos, Vector2 size, Color color)
    {
        var go = new GameObject("R", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(_canvasRt, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = Vector2.one * 0.5f;
        rt.anchoredPosition = canvasPos; rt.sizeDelta = size;
        var img = go.GetComponent<Image>(); img.color = color; img.raycastTarget = false;
        return go;
    }

    void SetSX(GameObject go, float x)
    { var s = go.transform.localScale; go.transform.localScale = new Vector3(x, s.y, s.z); }

    IEnumerator FlashPanel(RectTransform panelRt, Color fc, float dur)
    {
        var img = panelRt?.GetComponent<Image>(); if (img == null) yield break;
        Color orig = img.color;
        img.color = new Color(fc.r, fc.g, fc.b, 0.65f);
        for (float t = 0; t < dur; t += Time.deltaTime)
        { img.color = Color.Lerp(new Color(fc.r, fc.g, fc.b, 0.65f), orig, t / dur); yield return null; }
        img.color = orig;
    }

    IEnumerator FloatText(string text, Vector2 pos, Color color, float fontSize, float dur)
    {
        if (_canvasRt == null) yield break;
        var go = new GameObject("FT", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(_canvasRt, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = Vector2.one * 0.5f;
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(200f, 60f);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color; tmp.raycastTarget = false;

        float riseStart = dur * 0.3f;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float p = Mathf.Max(0f, (t - riseStart) / (dur - riseStart));
            rt.anchoredPosition = pos + Vector2.up * (55f * p);
            tmp.color = new Color(color.r, color.g, color.b, 1f - p * 0.95f);
            yield return null;
        }
        Destroy(go);
    }

    Vector2 ToCanvasLocal(Vector3 worldPos)
    {
        if (_canvasRt == null) return Vector2.zero;
        var sp = RectTransformUtility.WorldToScreenPoint(null, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, sp, null, out Vector2 local);
        return local;
    }
}
