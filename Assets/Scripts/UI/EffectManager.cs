using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }
    RectTransform _canvasRt;

    int _activeEffects = 0;
    public bool IsPlaying => _activeEffects > 0;

    void StartTracked(System.Collections.IEnumerator coroutine)
        => StartCoroutine(TrackCoroutine(coroutine));

    System.Collections.IEnumerator TrackCoroutine(System.Collections.IEnumerator inner)
    {
        _activeEffects++;
        yield return StartCoroutine(inner);
        _activeEffects--;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _canvasRt = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    public void PlayAttack(RectTransform srcRowRt, RectTransform dstPanelRt, int damage)
        => StartTracked(AttackCoroutine(srcRowRt, dstPanelRt, damage));

    public void PlayDefense(RectTransform defenderPanelRt, RectTransform opponentPanelRt, int shieldAmount = 1)
        => StartTracked(DefenseCoroutine(defenderPanelRt, opponentPanelRt, shieldAmount));

    public void PlayDisruptDelayed(RectTransform targetPanelRt, float delay, int lockAmount = 1)
        => StartTracked(DelayedDisrupt(targetPanelRt, delay, lockAmount));

    System.Collections.IEnumerator DelayedDisrupt(RectTransform targetPanelRt, float delay, int lockAmount = 1)
    {
        yield return new WaitForSeconds(delay);
        StartTracked(DisruptCoroutine(targetPanelRt, lockAmount));
    }

    public void PlayDisrupt(RectTransform targetPanelRt, int lockAmount = 1)
    {
        StartTracked(DisruptCoroutine(targetPanelRt, lockAmount));
    }

    public void PlayJustGuard(RectTransform atkRowRt, RectTransform atkPanelRt, RectTransform defPanelRt, int reflectDmg)
        => StartTracked(JustGuardCoroutine(atkRowRt, atkPanelRt, defPanelRt, reflectDmg));

    public float PlayCombo(RectTransform srcPanelRt, RectTransform dstPanelRt, int comboCount, int comboDamage)
    {
        StartTracked(ComboCoroutine(srcPanelRt, dstPanelRt, comboCount, comboDamage));
        return 0.5f + comboCount * 0.35f + 0.5f;
    }

    System.Collections.IEnumerator ComboCoroutine(RectTransform srcRt, RectTransform dstRt, int comboCount, int comboDmg)
    {
        if (_canvasRt == null) yield break;
        Color comboColor = new Color(1f, 0.6f, 0.1f);
        string comboLabel = "COMBO x" + comboCount + "!";
        // P1（左）なら右辺の外、P2（右）なら左辺の外、高さはHP付近
        Vector3[] _corners = new Vector3[4];
        srcRt.GetWorldCorners(_corners);
        Vector2 _bl = ToCanvasLocal(_corners[0]);
        Vector2 _tl = ToCanvasLocal(_corners[1]);
        Vector2 _tr = ToCanvasLocal(_corners[2]);
        float _panelH = _tl.y - _bl.y;
        float _hpY   = _bl.y + _panelH * 0.78f; // 上から約22%＝HP付近
        Vector2 _ctr = (_bl + _tr) * 0.5f;
        bool _isLeft = _ctr.x < 0f; // キャンバス中心より左ならP1
        float _textX = _isLeft ? _tr.x + 20f : _bl.x - 20f;
        Vector2 _textPos = new Vector2(_textX, _hpY);
        StartTracked(FloatTextWide(comboLabel, _textPos, comboColor, 44f, 1.2f, 280f));
        yield return new WaitForSeconds(0.3f);
        for (int i = 0; i < comboCount; i++)
        {
            StartTracked(ComboOrbCoroutine(srcRt, dstRt, i, comboCount));
            yield return new WaitForSeconds(0.35f);
        }
        yield return new WaitForSeconds(0.4f);
    }

    System.Collections.IEnumerator ComboOrbCoroutine(RectTransform srcRt, RectTransform dstRt, int orbIdx, int total)
    {
        Color orbColor = new Color(1f, 0.5f, 0.1f);
        Vector2 srcPos  = GetPanelCenter(srcRt);
        Vector2 dstEdge = GetInnerEdgePoint(srcRt, dstRt, 22f);
        float vOffset   = (orbIdx - (total - 1) * 0.5f) * 18f;
        Vector2 startPos = srcPos + Vector2.up * vOffset;
        var orb   = CreateOrbGO(orbColor);
        var orbRt = orb.GetComponent<RectTransform>();
        orbRt.anchoredPosition = startPos;
        for (float t = 0; t < 0.45f; t += Time.deltaTime)
        {
            if (orb == null) yield break;
            float p = Mathf.SmoothStep(0f, 1f, t / 0.45f);
            orbRt.anchoredPosition = Vector2.Lerp(startPos, dstEdge, p);
            orbRt.localScale       = Vector3.one * Mathf.Lerp(0.6f, 1.0f, p);
            yield return null;
        }
        if (orb != null) Destroy(orb);
        StartTracked(BurstCoroutine(dstEdge, orbColor));
        StartTracked(FlashPanel(dstRt, orbColor, 0.25f));
        Vector2 textPos = GetPanelCenter(dstRt) + Vector2.up * (10f + orbIdx * 22f);
        StartTracked(FloatText("DMG -1", textPos, orbColor, 28f, 0.8f));
    }


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

        StartTracked(BurstCoroutine(edgePos, atkColor));
        StartTracked(FlashPanel(dstRt, atkColor, 0.35f));
        StartTracked(FloatText($"DMG -{damage}", GetPanelCenter(dstRt), atkColor, 36f, 1.2f));
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
        StartTracked(BurstCoroutine(defEdgePos, jgColor));
        StartTracked(FlashPanel(defPanelRt, jgColor, 0.3f));
        StartTracked(ShieldVisual(defPanelRt, atkPanelRt));  // ガードエフェクト
        StartTracked(FloatText("JUST GUARD!", GetPanelCenter(defPanelRt), jgColor, 36f, 1.3f));

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
        StartTracked(BurstCoroutine(atkEdgePos, jgColor));
        StartTracked(FlashPanel(atkPanelRt, jgColor, 0.4f));
        StartTracked(FloatText($"DMG -{reflectDmg}", GetPanelCenter(atkPanelRt), new Color(1f, 0.2f, 0.1f), 36f, 1.2f));
    }

    // ─── DEF: 縦の盾エフェクト ────────────────────────────────────────────────

    IEnumerator DefenseCoroutine(RectTransform defPanelRt, RectTransform oppPanelRt, int shieldAmount = 1)
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
        StartTracked(FloatText("DEF!", GetPanelCenter(defPanelRt), sc, 34f, 1.1f));  // シールド展開と同時に表示

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

    // サイズ指定版 orb 生成（威力別演出用）
    GameObject CreateOrbGOSized(Color color, float innerSize, float glowSize)
    {
        var orb = new GameObject("Orb", typeof(RectTransform));
        orb.transform.SetParent(_canvasRt, false);
        var rt = orb.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = Vector2.one * 0.5f;
        MakeImg(orb, glowSize,  new Color(color.r, color.g, color.b, 0.35f));
        MakeImg(orb, innerSize, new Color(
            Mathf.Min(1f, color.r * 0.9f + 0.2f),
            Mathf.Min(1f, color.g * 0.9f + 0.2f),
            Mathf.Min(1f, color.b * 0.9f + 0.2f), 1f));
        return orb;
    }

    // 粒子数指定版バースト（威力別演出用）
    IEnumerator BurstCoroutineSized(Vector2 center, Color color, int cnt)
    {
        if (_canvasRt == null) yield break;
        float spread = Mathf.Lerp(60f, 95f, Mathf.InverseLerp(6, 13, cnt));
        var parts = new (GameObject go, RectTransform rt, Vector2 dir)[cnt];
        for (int i = 0; i < cnt; i++)
        {
            float a  = 360f / cnt * i * Mathf.Deg2Rad;
            var go = new GameObject("B", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(_canvasRt, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = Vector2.one * 0.5f;
            rt.anchoredPosition = center;
            float pSize = Mathf.Lerp(10f, 16f, Mathf.InverseLerp(6, 13, cnt));
            rt.sizeDelta = new Vector2(pSize, pSize);
            go.GetComponent<UnityEngine.UI.Image>().color = color;
            go.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            parts[i] = (go, rt, new Vector2(Mathf.Cos(a), Mathf.Sin(a)));
        }
        for (float t = 0; t < 0.45f; t += Time.deltaTime)
        {
            float p = t / 0.45f;
            foreach (var (go, rt, dir) in parts)
            {
                if (go == null) continue;
                rt.anchoredPosition = center + dir * (spread * p);
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.1f, p);
                go.GetComponent<UnityEngine.UI.Image>().color = new Color(color.r, color.g, color.b, 1f - p);
            }
            yield return null;
        }
        foreach (var (go, _, _) in parts) if (go != null) Destroy(go);
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

    // DIS: ロック量に応じてフラッシュ強度・回数・テキストを変化
    IEnumerator DisruptCoroutine(RectTransform targetPanelRt, int lockAmount = 1)
    {
        if (_canvasRt == null) yield break;

        float t01      = Mathf.InverseLerp(1f, 5f, lockAmount);
        Color disColor = Color.Lerp(new Color(0.55f, 0.15f, 0.8f), new Color(0.8f, 0.1f, 1f), t01);
        float flashDur = Mathf.Lerp(0.25f, 0.45f, t01);
        int   pulses   = lockAmount >= 4 ? 3 : lockAmount >= 2 ? 2 : 1; // lock量で点滅回数
        float textSize = Mathf.Lerp(28f, 46f, t01);

        var img = targetPanelRt?.GetComponent<Image>();
        if (img == null) yield break;
        Color orig = img.color;

        for (int p = 0; p < pulses; p++)
        {
            img.color = new Color(disColor.r, disColor.g, disColor.b, 0.7f);
            yield return new WaitForSeconds(flashDur * 0.4f);
            img.color = orig;
            if (p < pulses - 1) yield return new WaitForSeconds(0.08f);
        }
        img.color = orig;
        StartTracked(FloatText("LOCK!", GetPanelCenter(targetPanelRt), disColor, textSize, 1.0f));
    }

        IEnumerator FlashPanel(RectTransform panelRt, Color fc, float dur)
    {
        var img = panelRt?.GetComponent<Image>(); if (img == null) yield break;
        Color orig = img.color;
        img.color = new Color(fc.r, fc.g, fc.b, 0.65f);
        for (float t = 0; t < dur; t += Time.deltaTime)
        { img.color = Color.Lerp(new Color(fc.r, fc.g, fc.b, 0.65f), orig, t / dur); yield return null; }
        img.color = orig;
    }

    IEnumerator FloatTextWide(string text, Vector2 pos, Color color, float fontSize, float dur, float width)
    {
        if (_canvasRt == null) yield break;
        var go = new GameObject("FT", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(_canvasRt, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = Vector2.one * 0.5f;
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(width, 80f);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
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
