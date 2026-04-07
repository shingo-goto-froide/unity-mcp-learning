# PROJECT.md - プロジェクト固有情報
> このファイルはこのプロジェクト専用の情報です。
> CLAUDE.md を読んだ後に読み込んでください。

---

## プロジェクト概要

- **ゲームタイトル**：STRATUM
- **ジャンル**：2人対戦ボードゲーム風ストラテジー
- **対戦モード**：Local / AI（Easy/Normal/Hard） / Online（予定）

---

## ドキュメント一覧

### 各ファイルの役割

| ファイル | 役割 | 主な読者 | 最終更新 |
|---|---|---|---|
| CLAUDE.md | Claudeとの作業フロー・共通ルール・プロンプト集。どのプロジェクトでも使い回せるテンプレート | Claude（セッション開始時に最初に読む） | 2026-04-03 |
| PROJECT.md | このプロジェクト固有の情報。概要・注意事項・TODO・変更履歴 | Claude（CLAUDE.mdの次に読む） | 2026-04-07 |
| 仕様書_v2.0.md | ゲームのルール・フェーズ・バランス数値など「何を作るか」を定義する | Claude・開発者 | 2026-04-03 |
| 設計書_v2.0.md | スクリプト構成・クラス設計・メソッド定義など「どう作るか」を定義する | Claude・開発者 | 2026-04-03 |
| シーン構成書.md | BattleScene・TitleSceneのHierarchy構造とInspector値。シーン変更時は必ず更新 | Claude（シーン作業時） | 2026-04-03 |

> セッション開始時の読み込み順は CLAUDE.md を参照。

---

## プロジェクト固有の注意事項

### タイトル
変更時はTitleSceneのTitleラベルを更新すること。

### 日本語フォント
TitleSceneでは日本語テキストが文字化け（□表示）する。
TitleScene内のUIテキストは英語で記述すること。
BattleScene内はTextMeshProが機能しているため日本語OK。

### 絵文字・特殊文字
TextMeshProでは絵文字（🛡 等）は表示できない。
UIテキストには英数字のみ使用すること。
例：シールド表示は "🛡 5" ではなく "Shield 5" で統一。

### TLS Allocator エラーについて
「TLS Allocator ALLOC_TEMP_TLS has unfreed allocations」はUnity内部の一時メモリ警告。
スクリプト再コンパイル後の初回Play時に出ることがあるが、自作コードとは無関係。
次のPlay起動では消えるため対処不要。

---

## 既知のバグ・TODO

### バグ（未解決）
- なし

### TODO（未実装）
- [x] AI対戦の実装（AIController.cs を1ファイル追加するだけで実装できる見込み）
  - Easy：ランダム選択
  - Normal：グリーディ
  - Hard：先読み
  - AssignはAIが先に内部で決定し、人間の配置を参照しない（同時コミット相当）
  - **実装済み** Assets/Scripts/Core/AIController.cs
- [ ] Online対戦の実装
  - Acquire：全モード共通で順番処理
  - Assign：同時コミット（両者確定後にResolve）
  - Resolve：サーバー側で計算して配信

---

## 変更履歴

| 日付 | バージョン | 対象 | 内容 |
|---|---|---|---|
| 2026-04-02 | v1.0 | 仕様書・設計書 | 初版作成 |
| 2026-04-02 | v2.0 | 仕様書・設計書 | プール補充タイミング変更・妨害ランダム化・Reset機能追加・AIモード追加・タイトル画面仕様追加・UIをGameUINewに刷新・pure C#クラス化 |
| 2026-04-03 | - | CLAUDE.md・PROJECT.md | CLAUDE.mdを共通テンプレートとPROJECT.mdに分割 |
| 2026-04-03 | - | シーン構成書 | 新規作成（UnityMCPで実シーンから自動生成） |
| 2026-04-03 | - | PROJECT.md | フェーズズレバグをクローズ（Play確認で修正済みを確認） |
| 2026-04-03 | - | 設計書・仕様書 | Resolve処理を「P1全段→P2全段」から「Row1〜5順に両者同時発動（DRAW対応）」に変更 |
| 2026-04-03 | - | 仕様書 | Defense仕様をHP回復からシールドカウンター方式に変更。ターン開始時半減・攻撃を先に吸収 |
| 2026-04-03 | - | 設計書・実装 | shield実装（PlayerData/ActionResolver/GameManager/PlayerPanelUI更新） |
| 2026-04-03 | - | 仕様書・設計書・実装 | Disruptロックのクリアタイミング修正（Resolve直後→次Resolve開始時に変更） |
| 2026-04-03 | - | 仕様書・設計書・実装 | Disruptロック段数をOption Bに変更（発動段=ロック段数、lockTable {1,2,3,4,5}） |
| 2026-04-03 | - | 仕様書・実装 | 同時発動時のDefense優先処理を実装（シールド付与→Attack/Disruptの順） |
| 2026-04-03 | - | 仕様書・設計書・実装 | Just Guard実装（ATK vs DEF 同時発動時に2倍ダメージ反射） |
| 2026-04-03 | - | 設計書・実装 | GameBalanceSO作成（効果量・HP・リソース上限をInspectorから調整可能に） |
| 2026-04-03 | - | 実装 | SlotRow.CanAssign修正：ロック中の段へのアサインをブロック |
| 2026-04-03 | - | 実装・シーン | パネルフェーズボーダーハイライト追加（Acquire→Pool、Assign→Control、PlayerPanel点灯） |
| 2026-04-03 | - | 実装・シーン | HPBar非表示化・HP/ShieldをテキストのみのUIに変更 |
| 2026-04-03 | - | 実装・シーン | LockedOverlayに赤X追加・ロック行全体にオーバーレイ表示 |
| 2026-04-03 | - | 実装・シーン | EffectManager追加（ATK光球・DEF盾・DISフラッシュ・JustGuard跳ね返り演出） |
| 2026-04-03 | - | 設計書・シーン構成書 | Docsを実装に合わせて更新 |
| 2026-04-03 | - | 実装 | EffectManager調整：DMGテキスト位置をパネル中央に変更・DEFテキストを盾展開と同時表示 |
| 2026-04-03 | - | 実装 | JustGuard調整：DMGテキスト色を赤に統一・盾エフェクト（ShieldVisual）をJustGuard時にも表示 |
| 2026-04-03 | - | 仕様書 | シールド表示をHPバー横 → HP右隣テキスト常時表示に記述修正（実装と同期） |
| 2026-04-03 | - | 仕様書・設計書・実装 | 先手後手入れ替え実装：ゲーム開始ランダム・奇数ターン初期先手・偶数ターン後手が先手に |
| 2026-04-03 | - | 仕様書・実装 | シールド半減タイミングをターン開始時→Resolve終了後に変更（案A：積んだターンは全額有効） |
| 2026-04-03 | - | 仕様書・実装 | Assignフェーズ中に相手スロットを隠蔽（全モード共通）。Resolve時に一斉公開 |
| 2026-04-03 | - | 仕様書・実装 | バランス調整：HP 20→15、DMG {1,2,4,6,10}→{2,3,5,8,12}、Resolve待機 2.0s→1.5s（目標5〜10分） |
| 2026-04-03 | - | 仕様書・設計書・実装 | ATKコンボ実装（2段以上完成でコンボ数分の追加ダメージ） |
| 2026-04-03 | - | 仕様書・設計書・実装 | DISコンボ実装（2段以上完成でロック持続ターン=コンボ数）・ロックカウントダウン方式に変更 |
| 2026-04-06 | - | PROJECT.md | 読み込み順の重複をCLAUDE.md参照に整理 |
| 2026-04-06 | - | 設計書・実装・シーン | AI対戦実装（AIController.cs追加・BattleSceneに配置）Easy/Normal/Hard対応 |
| 2026-04-06 | - | 実装 | Hard AI強化：Just Guard狙い・ATKコンボ狙い・DISターゲティング・HP連動戦略 |
| 2026-04-06 | - | 実装 | Hard AI公平性修正：AssignP1開始時スナップショットで相手の現ターン配置を参照しないよう修正 |
| 2026-04-06 | - | 実装 | AIターン中UIロック：PoolRowUI・ControlPanelUIでAIターン中のボタンを無効化 |
| 2026-04-06 | - | 実装 | PlayerData.Name：AIモード時にYou / AI(Easy/Normal/Hard)表示に対応 |
| 2026-04-06 | - | 実装 | LockedOverlay：残りロックターン数を動的表示（X → X2, X3...） |
| 2026-04-06 | - | 実装 | SlotRow.AddLock：durationをMax採用から加算方式に変更 |
| 2026-04-06 | - | 実装・シーン | GameOverPanelにToTitleBtn追加（TitleSceneへ戻るボタン） |
| 2026-04-06 | - | シーン | TitleSceneにRulesBtn・RulesPanel追加（日本語ルール説明オーバーレイ） |
| 2026-04-06 | - | 実装 | TitleScreenUI.cs：RulesPanel開閉ロジック追加 |
| 2026-04-06 | - | 実装 | TextMeshPro日本語対応：NotoSansJP-Regular SDF追加（Dynamic モード） |
| 2026-04-06 | - | 仕様・実装・シーン | ローカル対戦廃止：LocalBtnを削除・TitleScreenUI.csからLocal関連コード削除・ボタン3個に再配置 |
| 2026-04-06 | - | 実装・シーン | BattleMenuUI.cs追加：右上ハンバーガーメニュー（ルール確認・タイトルへ）+ MenuBackdrop（半透明オーバーレイ・操作ブロック） |
| 2026-04-06 | - | 実装・シーン | AnnouncementUI.cs追加：ゲーム開始（GAME START）・各フェーズ（ACQUIRE/ASSIGN/RESOLVE）アナウンス演出 |
| 2026-04-06 | - | 実装 | AIController・GameManager(Resolve)：アナウンス演出終了まで待機するよう修正 |
| 2026-04-06 | - | シーン | BattleScene UIサイズ調整：ControlPanel高さ210→240・PoolPanel560×178・各フォントサイズ拡大 |
| 2026-04-07 | - | 実装 | SlotRow.AddLock：完成行への予約ロック機構追加（Clear時に適用・今ターン発動・次Assignからロック） |
| 2026-04-07 | - | 実装・シーン | RulesPanel Prefab化（Assets/Prefabs/RulesPanel.prefab）・RulesPanelController.cs追加 |
| 2026-04-07 | - | 実装・シーン | RulesPanelにScrollRect+Scrollbar追加・閉じるボタンをContentPanel外へ移動・ラベル「閉じる」統一 |
| 2026-04-07 | - | 実装 | EffectManager：DEF+DIS同時発動時にDISを0.5秒遅延（PlayDisruptDelayed追加） |
| 2026-04-07 | - | 仕様書 | 同時発動の処理順序（全9パターン）追記・Just Guard成立条件の明文化 |
| 2026-04-07 | - | 実装 | ATKコンボ：両者同時ダメージ適用でDRAW判定を正確に。演出も同時起動・テキストをパネル側面HP付近に配置 |
| 2026-04-07 | - | 実装・仕様書 | DISコンボ廃止：lockTable {1,2,3,4,5}に復元・duration常時1固定・関連コード削除 |
| 2026-04-07 | - | 実装 | Resolveロックカウントダウン：TickLocksBeforeResolve直後にRefreshAll+0.3秒待機を追加 |
| 2026-04-07 | - | 実装・仕様書 | Assignフェーズ同時コミット化：AI先手時に計画を保留し人間EndAssign時に一括適用（ComputePlan/ApplyPendingPlan機構） |
| 2026-04-07 | - | 実装・仕様書 | Assignフェーズのスロット隠蔽を撤廃（同時コミット方式で不要。前ターン配置は常時表示） |
| 2026-04-07 | - | 実装 | EffectManager：IsPlaying/StartTracked追跡機構追加。全サブ演出含めた完全待機を実現 |
| 2026-04-07 | - | 実装 | GameManager ResolveCoroutine：行ループ・ATKコンボ・シールド半減をIsPlaying完全待機に変更 |
| 2026-04-07 | - | 実装 | 満杯時のTake防止：PoolRowUIにIsFull()チェック追加・GameManager SelectPoolに満杯ガード追加 |
| 2026-04-07 | - | 実装・シーン | SkipボタンをAcquireフェーズ中常時表示（満杯時オレンジ・通常は緑）・PoolPanel高さ178→224 |
