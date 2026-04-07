# CLAUDE.md - Claudeとの作業ガイド（共通テンプレート）
> このファイルはどのUnityプロジェクトでも使い回せる共通ガイドです。
> CLIおよびVSCode拡張ではセッション開始時に自動で読み込まれます。
> Claude Desktopでは、セッション開始時に手動で読み込ませてください。

---

## モック制作フロー

```
企画 → 設計 → 実装 → テスト
              ↑______________|（閾値超えで設計フェーズへ戻る）
```

### ドキュメント依存関係
```
仕様書 → 設計書 → スクリプト → シーン構成書 → シーン構築
```

### Gitコミットタイミング
```
企画  : 仕様書完成            → Git commit
設計  : 設計書完成・人確認     → Git commit
実装  : スクリプト・シーン構成書完成・人確認 → Git commit
テスト: 仕様書更新のたびに     → Git commit
```

---

## フェーズ詳細

> ### Claude Codeについて（CLI専用機能）
> **Claude Code** はターミナル（VSCode統合ターミナル・PowerShell等）から起動するCLIツール。
> プロジェクトルートで `claude` コマンドを実行し、対話形式で操作する。
> `.claude/commands/` に置いたMarkdownが `/コマンド名` として使用できる。
> **Claude Desktopでは使用不可。** Claude DesktopはUnityMCPとの接続専用として使い分ける。
>
> ```bash
> cd "C:\Users\s-goto\Unity Projects\unity-mcp-learning"
> claude          # Claude Code起動
> /gen-design     # コマンド実行例
> ```

### 1. 企画フェーズ
**使用ツール:** Claude Desktop + UnityMCP

1. Claude Desktopを起動・UnityMCPと接続
2. 本ファイル（CLAUDE.md）をチャットで手動読み込み
3. Claudeの誘導に従い仕様書を作成
4. チャットを繰り返して仕様書を完成させる
5. Git commit

**セッション開始プロンプト例:**
```
CLAUDE.mdを読みました。企画フェーズを開始します。
仕様書テンプレートに従って、ゲームの仕様を一緒に作ってください。
```

> 💡 **Claude Codeコマンド（CLI）：** `/new-spec`
> 仕様書テンプレートを展開して対話形式で作成する。PROJECT.mdを自動参照。

---

### 2. 設計フェーズ
**使用ツール:** Claude CLI

1. `Assets/Docs/仕様書.md` を読み込ませ、`Assets/Docs/設計書.md` を生成（クラス構成・依存関係・メソッド定義まで）
2. 人が設計書を確認
3. Git commit

> 💡 **Claude Codeコマンド（CLI）：** `/gen-design`
> 仕様書を読んで設計書を自動生成。クラス構成・依存関係・メソッド定義まで含む。

---

### 3. 実装フェーズ
**使用ツール:** Claude CLI

1. `Assets/Docs/設計書.md` を読み込ませ、`Assets/Scripts/` 配下にスクリプトを一括生成（コンパイルエラーはその場で自己修正）
2. 人がスクリプトを確認
3. 仕様書・設計書・スクリプトをもとに `Assets/Docs/シーン構成書.md` を生成（GameObject名・コンポーネント・Inspector値まで）
4. 人がシーン構成書を確認
5. `Assets/Docs/シーン構成書.md` をもとにシーンを構築
6. Git commit

> 💡 **Claude Codeコマンド（CLI）：**
> - `/gen-scripts` ：設計書からスクリプト一括生成（コンパイルエラーは自己修正）
> - `/gen-scene` ：シーン構成書に従いTitleScene・BattleSceneを構築

---

### 4. テストフェーズ
**使用ツール:** Claude Desktop + UnityMCP

1. Claude Desktopを起動・UnityMCPと接続
2. UnityEditorで実行しながら気になった点をClaudeに相談
3. **実装・修正は行わず、仕様書の更新のみ行う**
4. 仕様書の更新がある程度完了したら、設計フェーズへ戻る
5. エラー修正が必要な場合は以下の基準で対応:

| バグの種類 | 対応方法 |
|---|---|
| 軽微なバグ（仕様・設計に影響しない） | CLI即修正 → 修正記録に残す |
| 仕様・設計に影響するバグ | 必ず設計フェーズに戻る |

> 💡 **Claude Codeコマンド（CLI）：**
> - `/check-diff` ：実装とドキュメントの差分をレポート形式で出力
> - `/debug` ：バグ調査・修正。軽微バグはCLI即修正、仕様変更は設計フェーズへ戻る判断も含む
> - `/update-docs` ：実装に合わせてDocs一式を更新しPROJECT.mdの変更履歴も追記

---

## Assets/Docs/ の構成

ドキュメントはすべて `Assets/Docs/` に配置する。

| ファイル名 | 内容 | 作成タイミング |
|---|---|---|
| 仕様書.md | ゲームの仕様 | 企画フェーズ |
| 設計書.md | クラス設計・依存関係 | 設計フェーズ |
| シーン構成書.md | GameObject・コンポーネント構成 | 実装フェーズ |
| 修正記録.md | バグ修正の記録 | テストフェーズ |

---

## Claudeへのドキュメント更新ルール

### 仕様書を更新すべき時
- ゲームルールを変更したとき（効果量・フェーズ順序など）
- 新しい要素を追加したとき（新リソース種類・新フェーズ・新モードなど）
- UIレイアウトや操作フローを変更したとき

### 設計書を更新すべき時
- 新しいスクリプト・クラスを追加したとき
- 既存クラスのメソッド・変数を変更・追加・削除したとき
- クラス間の依存関係が変わったとき

### 更新不要なケース
- バグ修正のみでインターフェースが変わらない場合
- コメントの追記・整理のみの場合
- パラメータ調整（数値チューニング）のみの場合

---

## Claude Code コマンド一覧

> **Claude Code（CLI）専用。** ターミナルでプロジェクトルートに移動後 `claude` を起動し、`/コマンド名` で実行。
> Claude Desktopでは使用不可（Claude DesktopはUnityMCPとの連携専用）。
> コマンドファイルは `.claude/commands/` に配置されており、Gitで管理される。

| コマンド | 使用フェーズ | ツール | 内容 |
|---|---|---|---|
| `/new-spec` | 企画 | Claude Code CLI | 仕様書テンプレートを展開して対話形式で作成 |
| `/gen-design` | 設計 | Claude Code CLI | 仕様書を読んで設計書を自動生成 |
| `/gen-scripts` | 実装 | Claude Code CLI | 設計書からスクリプト一括生成（エラー自己修正） |
| `/gen-scene` | 実装 | Claude Code CLI | シーン構成書に従いTitleScene・BattleSceneを構築 |
| `/check-diff` | テスト | Claude Code CLI | 実装とドキュメントの差分をレポート形式で出力 |
| `/debug` | テスト | Claude Code CLI | バグ調査・修正（軽微バグ即修正 / 仕様変更は設計フェーズへ） |
| `/update-docs` | 共通 | Claude Code CLI | 実装に合わせてDocs一式を更新・変更履歴を追記 |

### コマンドとツールの使い分け

```
企画フェーズ  → Claude Desktop（UnityMCP接続）+ /new-spec（CLI）
設計フェーズ  → Claude Code CLI（/gen-design）
実装フェーズ  → Claude Code CLI（/gen-scripts → /gen-scene）
テストフェーズ → Claude Desktop（UnityMCP）+ Claude Code CLI（/check-diff / /debug）
```

---

## セッション開始時のプロンプト集

### 基本の引き継ぎ
```
Assets/Docs/ のドキュメントをすべて読んでプロジェクトの内容を把握した上で、〇〇を作ってください
```

### 機能を追加したいとき
```
Assets/Docs/ のドキュメントを読んでください。
その後、〇〇という機能を追加したいです。
実装方針を提案してから、コードを書いてください。
```

### バグを直したいとき
```
Assets/Docs/ のドキュメントとコンソールログを確認してください。
〇〇をすると△△というエラーが出ます。原因と修正方法を教えてください。
```

### 実装と仕様書の差分確認
```
Assets/Docs/ のドキュメントを読み、Assets/Scripts/ の実際のコードと比較して
仕様書・設計書と食い違っている箇所があれば教えてください。
```

### ドキュメントの更新
```
Assets/Scripts/ のコードを読み、Assets/Docs/ の仕様書と設計書を
最新の実装に合わせて更新してください。
```


---

## テンプレートリポジトリ運用（②方式）

> 新規プロジェクト開始時はこの手順に従う。
> CLAUDE.md・PROJECT.md・`.claude/commands/` をテンプレートとして使い回す。

### テンプレートに含まれるもの

```
{テンプレートリポジトリ}/
├── CLAUDE.md                     ← 本ファイル（共通フロー・テンプレート集）
└── .claude/
    └── commands/
        ├── new-spec.md           ← 設計に依存しない（そのまま使える）
        ├── gen-design.md         ← 設計に依存しない（そのまま使える）
        ├── gen-scripts.md        ← ⚠️ 設計書完成後に並列グループを更新が必要
        ├── gen-scene.md          ← 設計に依存しない（そのまま使える）
        ├── check-diff.md         ← 設計に依存しない（そのまま使える）
        ├── update-docs.md        ← 設計に依存しない（そのまま使える）
        └── debug.md              ← 設計に依存しない（そのまま使える）
```

> `PROJECT.md` はプロジェクト固有情報のため、テンプレートには含めない。
> 新規プロジェクト開始時に新たに作成する。

---

### 新規プロジェクト開始手順

```
ステップ1：テンプレートをコピー
  テンプレートリポジトリをコピーして新規プロジェクトのルートに配置
  CLAUDE.md・.claude/commands/ がそのまま使える状態になる

ステップ2：PROJECT.md を新規作成
  プロジェクト固有情報（タイトル・ジャンル・注意事項）を記載する
  Assets/Docs/PROJECT.md として保存

ステップ3：企画フェーズ（/new-spec）
  → 仕様書完成・Git commit

ステップ4：設計フェーズ（/gen-design）
  → 設計書完成・人の確認

ステップ5：gen-scripts.md の並列グループを更新 ← ⚠️ 重要
  設計書のクラス構成・依存関係ツリーをもとに
  /gen-scripts の並列実行グループを書き換える
  （詳細は下記「gen-scripts.md の更新ルール」参照）
  → Git commit

ステップ6：実装フェーズ（/gen-scripts → /gen-scene）
  → スクリプト生成・シーン構築・人の確認・Git commit

ステップ7：テストフェーズ（Claude Desktop + UnityMCP）
  → /check-diff / /debug でバグ修正・仕様書更新
```

---

### gen-scripts.md の更新ルール

`/gen-scripts` の並列グループは**設計書の依存関係ツリーを読んで更新する**。
並列化の判断基準は「互いのクラスを using / 参照していないか」。

**基本的な並列化パターン（Unityプロジェクト共通）：**

```
ステップ1（並列）：Enum・ScriptableObject・静的クラス
  → 誰にも依存しないため必ず最初・全グループ並列OK

ステップ2（並列）：純粋C#クラス（MonoBehaviour非依存）
  → Enumにのみ依存するため、ステップ1完了後に並列実行

ステップ3（並列）：Core MonoBehaviour（GameManager等）
  → 純粋C#クラスを参照するため、ステップ2完了後

ステップ4（並列）：UI・AI・その他MonoBehaviour
  → Coreを参照するため、ステップ3完了後
  → UIとAIは互いに独立していれば並列OK
```

> 設計書の依存関係が上記パターンと異なる場合は、
> Claude Codeに「設計書を読んで並列グループを提案して」と依頼すればよい。

---

## Unity MCP接続が切れたときの対処

1. Unityエディタが起動しているか確認
2. Claude Desktopを再起動
3. 再起動後に接続確認：「Unity MCPに繋がってる？」と聞く

---

## 仕様書テンプレート

> 新規ゲーム制作時はこの構成に従って仕様書を作成すること。
> 不要なセクションは削除してよい。項目が足りない場合は追加する。


# [ゲームタイトル] 仕様書 v1.0


## ゲーム概要

ゲームのコンセプトを2〜3行で記述する。

- ジャンル：
- プレイ人数：
- 対戦形式：（ローカル / AI / オンライン 等）
- 想定プレイ時間：

---

## プレイヤー

- 人数：
- 初期ステータス：（HP・所持数上限 等）
- 勝利条件：
- 敗北条件：

---

## ゲーム要素

> ゲームの中心となるリソース・カード・ユニット等を記載する。

| 名前 | 種類 | 効果・説明 |
|---|---|---|
|  |  |  |

### 各要素の詳細ルール
（効果量・発動条件・特殊挙動等、挙動の細かいルールをここに記載）

---

## フェーズ構成（1ターン）

```
フェーズ1：〇〇
フェーズ2：〇〇
フェーズ3：〇〇
次のターンへ
```

### フェーズ1：〇〇
- 処理内容・操作内容を具体的に記載

### フェーズ2：〇〇
- 処理内容・操作内容を具体的に記載

### フェーズ3：〇〇
- 処理内容・操作内容を具体的に記載

---

## 画面構成

| シーン名 | 役割 |
|---|---|
| TitleScene | タイトル・モード選択 |
| GameScene | ゲーム本体 |

### UI・操作フロー

#### TitleScene
| ボタン・要素 | 動作 |
|---|---|
|  |  |

#### GameScene
| ボタン・要素 | 動作 |
|---|---|
|  |  |

---

## パラメータ一覧

> 数値で定義される要素をここにまとめる。調整しやすくするために分離しておく。

| パラメータ名 | 値 | 説明 |
|---|---|---|
|  |  |  |

---

## 未決事項・注意事項

（後回しにした仕様・実装時に注意すべきルールをここに残す）


## 設計書テンプレート

# [ゲームタイトル] 設計書 v1.0

## スクリプト一覧・依存関係

> クラスの親子関係をツリー形式で記載する。

```
ManagerClass（MonoBehaviour・Singleton）
├── SubManagerA          役割の説明
├── SubManagerB          役割の説明
└── DataClass × N        役割の説明
    └── ChildClass       役割の説明
```

---

## Enum・静的クラス・ScriptableObject

### ScriptableObject
> バランス調整値はScriptableObjectで管理する。
> アセットパス: `Assets/ScriptableObjects/Xxx.asset`

### Enum
（ゲーム状態・リソース種別等、ゲーム全体で使うEnumをここに列挙）

### 静的クラス
（シーンをまたいで共有する設定値・グローバル状態をここに記載）

---

## クラス詳細

> 各クラスの変数・イベント・メソッドを記載する。
> pure C# class / MonoBehaviour / Singleton の区別を明記する。

### [ClassName]（pure C# class / MonoBehaviour・Singleton）

| 変数 / イベント | 型 | 説明 |
|---|---|---|
|  |  |  |

| メソッド | 説明 |
|---|---|
|  |  |

---

（クラスごとに上記ブロックを繰り返す）

---

## フォルダ構成

```
Assets/
├── Scripts/
│   ├── Core/
│   ├── [カテゴリ]/
│   └── UI/
├── Docs/
├── Prefabs/
├── Scenes/
└── ScriptableObjects/
```

---

## シーン構成書テンプレート

> 実装フェーズでシーン構成書を作成する際はこの構成に従うこと。
> 不要なセクションは削除してよい。項目が足りない場合は追加する。
> シーンを変更したら必ず更新すること。

# [ゲームタイトル] シーン構成書 v1.0

## シーン一覧

| シーン名 | 役割 | Build Index | パス |
|---|---|---|---|
| TitleScene | タイトル・モード選択 | 0 | Assets/Scenes/TitleScene.unity |
| GameScene  | ゲーム本体          | 1 | Assets/Scenes/GameScene.unity  |

---

## [シーン名]

### Hierarchy

```
SceneName
├── EventSystem
├── [MainCanvas]
│   ├── Background
│   ├── [PanelA]              ※起動時 activeSelf=false の場合は明記
│   │   └── [ChildObject]
│   └── [PanelB]
├── [Manager]                 ← MonoBehaviour をアタッチするオブジェクト
└── Main Camera
```

### コンポーネント一覧

| GameObject | コンポーネント | 備考 |
|---|---|---|
| EventSystem | EventSystem, InputSystemUIInputModule | StandaloneInputModuleは使わない |
| [MainCanvas] | Canvas, CanvasScaler, GraphicRaycaster | |
| [PanelA] | Image | 起動時非表示 |
| [Manager] | [ScriptName] | 役割の説明 |
| Main Camera | Camera, AudioListener | |

### Inspector参照設定

> Inspectorで手動設定が必要なフィールドをここに明記する。

| コンポーネント | フィールド | 参照先 |
|---|---|---|
|  |  |  |

---

（シーンごとに上記ブロックを繰り返す）


## 修正記録テンプレート（テストフェーズ用）


## 修正記録

| 日付 | 種別 | 内容 | 対応 |
|---|---|---|---|
| YYYY-MM-DD | 軽微バグ / 仕様変更 |  | CLI修正 / 設計フェーズ戻り |

## 未解決の技術課題

（設計時点で判明しているリスク・後回しにした設計判断）

