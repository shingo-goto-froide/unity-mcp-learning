# PROJECT.md - プロジェクト固有情報
> このファイルはこのプロジェクト専用の情報です。
> CLAUDE.md を読んだ後に読み込んでください。

---

## プロジェクト概要

- **ゲームタイトル**：STRATUM
- **ジャンル**：2人対戦ボードゲーム風ストラテジー
- **対戦モード**：AI対戦（Easy/Normal/Hard） / Online（予定）

---

## ドキュメント一覧

| ファイル | 役割 | 最終更新 |
|---|---|---|
| CLAUDE.md | Claudeとの作業フロー・共通ルール | 2026-04-07 |
| PROJECT.md | プロジェクト固有情報・注意事項・変更履歴 | 2026-04-07 |
| 仕様書_v2.0.md | ゲームのルール・フェーズ・バランス数値 | 2026-04-07 |
| 設計書_v2.0.md | スクリプト構成・クラス設計・メソッド定義 | 2026-04-07 |
| シーン構成書.md | BattleScene・TitleSceneのHierarchy構造とInspector値 | 2026-04-07 |

---

## プロジェクト固有の注意事項

### タイトル
変更時はTitleSceneのTitleラベルを更新すること。

### 日本語フォント
TextMeshProで日本語を表示する場合は `Assets/Fonts/NotoSansJP-Regular SDF.asset`（Dynamic モード）を使用すること。
日本語テキストを追加・変更した際は、そのTMPコンポーネントにフォントを手動またはUnityMCP経由で割り当てること。
TitleText / SubTitle は英語テキストのみ（タイトル・サブタイトルは英語固定）。

### 絵文字・特殊文字
TextMeshProでは絵文字（🛡 等）は表示できない。
UIテキストには英数字・ひらがな・カタカナ・漢字を使用すること。

### TLS Allocator エラーについて
「TLS Allocator ALLOC_TEMP_TLS has unfreed allocations」はUnity内部の一時メモリ警告。
スクリプト再コンパイル後の初回Play時に出ることがあるが、自作コードとは無関係。

---

## 既知のバグ・TODO

### バグ（未解決）
- なし

### TODO（未実装）
- [ ] Online対戦の実装
  - Acquire：全モード共通で順番処理
  - Assign：同時コミット（両者確定後にResolve）
  - Resolve：サーバー側で計算して配信

---

## 変更履歴

| 日付 | 対象 | 内容 |
|---|---|---|
| 2026-04-02 | 仕様書・設計書 | 初版作成 |
| 2026-04-02 | 仕様書・設計書 | プール補充タイミング変更・妨害ランダム化・Reset機能追加・AIモード追加・UIをGameUINewに刷新 |
| 2026-04-03 | 全体 | シールド・JustGuard・ATKコンボ・DISコンボ実装。EffectManager追加。バランス調整 |
| 2026-04-06 | 全体 | AI対戦実装（Easy/Normal/Hard）。BattleMenuUI・AnnouncementUI追加。ローカル対戦廃止 |
| 2026-04-07 | 仕様書・実装 | DISコンボ廃止（lockTable復元・duration=1固定）。同時コミット方式確立 |
| 2026-04-07 | 実装・シーン | UI全面日本語化（TitleScene・BattleScene）。NotoSansJPフォント適用 |
| 2026-04-07 | 実装 | EffectManager：ATK/DEF/DIS演出を効果量でスケール対応 |
| 2026-04-07 | 実装 | PoolRowUI：TakeButtonにプレビューチップ追加・ホバー時チップハイライト |
| 2026-04-07 | 実装 | PlayerPanelUI：HoverSlot追加（行ボタンホバーで次アサインスロットをハイライト） |
| 2026-04-07 | 実装 | ControlPanelUI：行ボタンをローマ数字（Ⅰ〜Ⅴ）に変更。フェーズ名日本語化（獲得/配置/発動フェーズ） |
| 2026-04-07 | 実装 | AnnouncementUI：獲得フェーズのサブテキストを「あなたのターン/AIのターン」に変更 |
| 2026-04-07 | 実装 | GameUINew：パネルボーダーパルスをPool・ControlPanelのみに変更。プレイヤーパネルは消灯 |
| 2026-04-07 | 実装 | AIController：アナウンス演出終了後に0.25s追加待機 |
| 2026-04-07 | シーン | BattleScene UIレイアウト調整：プレイヤーパネル280px・SlotsContainer258px・ControlPanel600px |
| 2026-04-07 | 実装・シーン | ローマ数字（Ⅰ〜Ⅴ）導入：ControlPanelの行ボタン＋PlayerPanelの各行にラベル追加（P1左・P2右） |
| 2026-04-07 | 実装 | フェーズ名日本語化：獲得フェーズ / 配置フェーズ / 発動フェーズ |
| 2026-04-07 | 実装 | AnnouncementUI：獲得フェーズのサブテキストをターン所有者名（あなたのターン/AIのターン）に変更 |
| 2026-04-07 | 実装 | PoolRowUI / GameUINew：Assignフェーズ中のホバーライト・AIフェーズ中の誤点灯を修正 |
| 2026-04-07 | シーン | BattleScene：各行にローマ数字ラベル追加（P1左・P2右）。TextBackground追加（アナウンス演出） |
| 2026-04-07 | CLAUDE.md | 制作フロー・テンプレート全面更新 |
