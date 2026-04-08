# トラブル対処・セッションプロンプト集

## トラブル対処

### UnityMCP接続が切れた場合

1. Unityエディタが起動しているか確認
2. Claude Desktopを再起動
3. 再起動後に接続確認：「Unity MCPに繋がってる？」と聞く

### Hooks（settings.json）のエラーが出た場合

Claude Code CLIが起動時に `.claude/settings.json` のエラーを表示する場合：

1. エラーメッセージにある選択肢で「Continue without these settings」を選んで起動
2. 「settings.jsonを確認して修正して」とClaude Codeに依頼する
3. 修正後に再起動して確認

### git MCPが動かない場合

1. `claude mcp list` で `git` が表示されているか確認
2. `uvx --version` でuvxが入っているか確認
3. 未インストールの場合は `winget install astral-sh.uv` でインストール
4. `~/.claude.json` に git MCPの設定が入っているか確認（04-setup.md参照）

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
