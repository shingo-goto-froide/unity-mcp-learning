# 環境構築

> 新規PCでこのフローを使い始める際に必要なセットアップ。
> 一度設定すれば全プロジェクトで使い回せる。

## 必要なツール一覧

| ツール | 用途 | 必須 |
|---|---|---|
| Unity Hub + Unity Editor | ゲーム開発 | ✅ |
| Claude Desktop | UnityMCPとの接続・企画・テストフェーズ | ✅ |
| UnityMCP | Claude DesktopからUnityを操作 | ✅ |
| Claude Code CLI | 設計・実装フェーズのCLI操作 | ✅ |
| uv | git MCPの実行に必要なPythonパッケージマネージャー | ✅ |
| git MCP | Claude CodeからGitを操作 | ✅ |

---

## 1. Claude Desktop

https://claude.ai/download からインストール。

---

## 2. UnityMCP

Claude DesktopとUnityを接続するパッケージ。

**Unityへのインストール：**
Unity Package Manager → Add package from git URL：
```
https://github.com/justinpbarnett/unity-mcp.git
```

**Claude Desktopへの設定：**
インストール後にUnityエディタ上でセットアップ画面が表示される。
指示に従い `claude_desktop_config.json` にMCPサーバーを追加する。

**接続確認：**
Claude Desktopで「Unity MCPに繋がってる？」と聞いてエディタ状態が返ってくればOK。

---

## 3. Claude Code CLI

```powershell
npm install -g @anthropic-ai/claude-code
```

Node.js が必要。未インストールの場合は https://nodejs.org/ からインストール。

**動作確認：**
```powershell
claude --version
```

---

## 4. uv（git MCP の前提条件）

```powershell
winget install astral-sh.uv
```

インストール後ターミナルを再起動して確認：
```powershell
uv --version
uvx --version
```

---

## 5. git MCP

Claude Code CLIからGit操作を可能にする。**ユーザースコープ**で設定する（全プロジェクト共通）。

`~/.claude.json` の `projects` キーの直前に以下を追加：
```json
"mcpServers": {
  "git": {
    "command": "cmd",
    "args": ["/c", "uvx", "mcp-server-git"]
  }
},
```

**動作確認：**
```powershell
claude mcp list
# git が表示されればOK
```

---

## 環境構築完了後のチェックリスト

```
[ ] Claude Desktop が起動する
[ ] Unity + UnityMCP が接続できる（「繋がってる？」で確認）
[ ] claude --version が表示される
[ ] uvx --version が表示される
[ ] claude mcp list に git が表示される
```
