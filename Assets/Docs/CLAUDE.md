# CLAUDE.md - Claudeとの作業ガイド（共通テンプレート）
> このファイルはどのUnityプロジェクトでも使い回せる共通ガイドです。
> セッション開始時に必ず読み込んでください。

---

## セッション開始時の読み込み順

```
1. CLAUDE.md（このファイル・共通ルール）
2. PROJECT.md（このプロジェクトの固有情報）
3. 仕様書.md
4. 設計書.md
5. シーン構成書.md（あれば）
```

---

## 新規ゲーム制作フロー

> 仕様書の壁打ちはClaude Desktopのチャットで行い、④以降はUnityMCPが繋がった状態で作業する。

```
① 新規Unityプロジェクトを作成・UnityMCP接続        人
        ↓
② CLAUDE.mdのテンプレートに沿って仕様書を壁打ち    人・AI
        ↓ OK?（人）
③ 仕様書 → 設計書を生成                           AI
   （クラス構成・依存関係・メソッド定義まで）
        ↓ OK?（人）
④ 設計書 → スクリプトを一括生成                   AI
   （コンパイルエラーはその場で自己修正）
        ↓ OK?（人）
⑤ スクリプト → シーン構成書を生成                  AI
   （GameObject名・コンポーネント・Inspector値まで）
        ↓ OK?（人）
⑥ シーン構成書 → シーンを自動構築                  AI
        ↓
⑦ Play実行・動作確認                              AI（人が目視確認）
```

各ステップのNoループは「全部作り直し」ではなく「差分だけ修正」で進めること。

---

## ドキュメントの更新タイミング

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

### 更新の頻度の目安
- 小さな機能追加なら作業セッションの最後にまとめて更新
- 大きな設計変更なら変更直後に更新（次セッションへの引き継ぎのため）
- 迷ったら更新する（古いドキュメントは混乱の元）

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

### 新規ゲームを仕様書から作るとき
```
Assets/Docs/ のドキュメントをすべて読んでください。
制作フローに従って、仕様書をもとにゲームを作ってPlay確認まで行ってください。
各ステップ完了後に確認を求めてください。
```

---

## Unity共通の注意事項

### Input System
新しいInput Systemを使用するプロジェクトでは、
Canvas・EventSystem作成時は StandaloneInputModule ではなく InputSystemUIInputModule を使うこと。
そうしないとクリックが反応しないバグが発生する。

### Canvas再構築時のチェックリスト
1. GraphicRaycaster がCanvasにアタッチされているか
2. EventSystemに InputSystemUIInputModule が使われているか（StandaloneInputModule ではない）
3. Singletonが1つだけシーンに存在するか（重複するとバグの原因になる）

### Unity Muse（AI生成機能）
GenerateMesh / GenerateSprite / GenerateSound / GenerateHumanoidAnimation 等の
AI生成系ツールは Unity Museサブスクリプションが必要。
未契約の場合は3Dモデルをプリミティブ（Cube/Sphere/Capsule/Cylinder）で代用すること。

### GameObjectの重複に注意
Singletonパターンを使うクラス（GameManager等）はシーンに1つだけ存在すること。
重複するとフェーズズレ・二重実行などの原因になる。


---

## TextMeshPro で日本語を使う方法

### 前提
TextMeshPro はデフォルトでラテン文字のみ対応。日本語を表示するには日本語対応フォントアセットが必要。

### 手順

#### ① 日本語フォントを用意する
システムフォント（Windows）を使う場合は `C:\Windows\Fonts\` から選ぶ。
おすすめ：`meiryo.ttc`（メイリオ）/ `NotoSansJP-Regular.ttf`（別途ダウンロード）

`Assets/Fonts/` にコピーしてUnityにインポートする。

#### ② Font Asset Creator で TMP Font Asset を生成する
**Window → TextMeshPro → Font Asset Creator**

| 設定項目 | 推奨値 |
|---|---|
| Source Font File | インポートしたフォント |
| Sampling Point Size | Auto Sizing |
| Padding | 5 |
| Packing Method | **Fast**（Optimumは重い） |
| Atlas Resolution | **512×512** |
| Character Set | **Custom Characters** |
| Render Mode | SDFAA |

> ⚠️ **CJK漢字（4E00-9FFF）を Character Set に含めると２万文字超でフリーズする。絶対に避けること。**

**Custom Characters に貼り付ける文字列（ひらがな・カタカナ・記号）：**
```
 !"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\]^_`abcdefghijklmnopqrstuvwxyz{|}~あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをんアイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲンがぎぐげござじずぜぞだぢづでどばびぶべぼぱぴぷぺぽゃゅょっガギグゲゴザジズゼゾダヂヅデドバビブベボパピプペポャュョッーｰ、。「」・！？：；
```

**Generate Font Atlas → Save** で `.asset` ファイルを保存する。

#### ③ Font Asset を Dynamic モードに変更する（重要）
右クリックで作成した場合 **Static モードになっている**。
Static だと登録外の文字が白い□で表示される。

Project ウィンドウで生成した `.asset` を選択 → Inspector で **Atlas Population Mode を Dynamic に変更**。

UnityMCP 経由で自動設定する場合：
```csharp
var so = new SerializedObject(fontAsset);
so.FindProperty("m_AtlasPopulationMode").intValue = 1; // Dynamic
so.FindProperty("m_IsMultiAtlasTexturesEnabled").boolValue = true;
so.ApplyModifiedProperties();
EditorUtility.SetDirty(fontAsset);
AssetDatabase.SaveAssets();
```

#### ④ TextMeshPro コンポーネントにフォントを割り当てる
Inspector の Font Asset フィールドに作成した `.asset` をドラッグ、または UnityMCP で自動設定。

---

### トラブルシューティング

| 症状 | 原因 | 対処 |
|---|---|---|
| 文字が白い□で表示される | Static モードで未登録文字 | Atlas Population Mode を Dynamic に変更 |
| Font Asset Creator がフリーズ | CJK漢字（素4E00-9FFFの約２万文字）を含めている | Character Set から CJK範囲を外す |
| Generate Font Atlas が進まない | 同上 | キャンセルできない場合はUnityを強制終了 |
| 白い四角がテキストを隐す | UIのパディング・レイアウト設定の問題 | ContentPanelのパディングを調整する |

### フォントアセットの保存場所
`Assets/Fonts/` に配置するのを推奨。

---

## Unity MCP接続が切れたときの対処

1. Unityエディタが起動しているか確認
2. Claude Desktopを再起動
3. 再起動後に接続確認：「Unity MCPに繋がってる？」と聞く

---

## 仕様書テンプレート

> 新規ゲーム制作時はこの構成に従って仕様書を作成すること。
> 不要なセクションは削除してよい。項目が足りない場合は追加する。

```markdown
# [ゲームタイトル] 仕様書

---

## ゲーム概要

- ジャンル：
- プレイ人数：
- 対戦形式：（ローカル / AI / オンライン 等）
- ゲームの目的・コンセプト（2〜3行で）

---

## プレイヤー

- 人数：
- 初期ステータス：（HP・所持数上限 等）
- 勝利条件：
- 敗北条件：

---

## ゲーム要素

> リソース・カード・ユニット等、ゲームの中心となる要素を記載する。

| 名前 | 種類 | 効果・説明 |
|---|---|---|
| 例：攻撃 | リソース | 相手にダメージを与える |

---

## フェーズ構成（1ターン）

```
フェーズ1：〇〇
フェーズ2：〇〇
フェーズ3：〇〇
次のターンへ
```

### フェーズ1：〇〇
- 操作内容・処理内容を具体的に記載

### フェーズ2：〇〇
- 操作内容・処理内容を具体的に記載

---

## 画面構成

| シーン名 | 役割 |
|---|---|
| TitleScene | タイトル・モード選択 |
| GameScene | ゲーム本体 |

---

## UI・操作フロー

> 各画面で何が表示され、どう操作するかを記載する。

### TitleScene
| ボタン・要素 | 動作 |
|---|---|
| 例：Startボタン | ゲームシーンへ遷移 |

### GameScene
| ボタン・要素 | 動作 |
|---|---|
| 例：Endボタン | ターン終了 |

---

## パラメータ一覧

> 数値で定義される要素をここにまとめる。調整しやすくするために分離しておく。

| パラメータ名 | 値 | 説明 |
|---|---|---|
| 例：初期HP | 20 | 各プレイヤーの開始HP |
```
