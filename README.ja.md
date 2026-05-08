# Run — Windows 「ファイル名を指定して実行」代替アプリ

Win+R を横取りして起動するカスタム実行ダイアログ。ダークモード対応・コマンド履歴・SYSTEM 権限実行に対応。

## 機能

| ショートカット | 動作 |
|---|---|
| Win+R | ダイアログを開く |
| Enter / OK | コマンド実行 |
| Ctrl+Shift+Enter | 管理者として実行（UAC） |
| Ctrl+Alt+Enter | SYSTEM 権限で実行（psexec 使用） |
| ↑ / ↓ | コマンド履歴を移動 |
| Escape | 閉じる |

- **ダーク / ライトモード** — Windows のシステムテーマに自動追従
- **コマンド履歴** — セッションをまたいで保持
- **システムトレイ常駐** — ダブルクリックまたは右クリックメニューから開く；スタートアップ登録オプションあり
- **参照ボタン** — ファイルピッカーで実行ファイルを選択
- **即時フォーカス** — 開いた瞬間からクリック不要でキー入力可能

## 動作環境

- Windows 10 / 11
- .NET 8 (Windows)
- [PsExec](https://learn.microsoft.com/sysinternals/downloads/psexec) を `PATH` に配置 *(省略可 — Ctrl+Alt+Enter を使う場合のみ必須)*

## ビルド

```
dotnet build -c Release
```

出力先: `bin\Release\net8.0-windows\Run.exe`

## インストール

1. `Run.exe` を任意の場所に配置（例: `%LOCALAPPDATA%\Run\`）
2. 起動するとシステムトレイに常駐
3. トレイメニューの **Run at Startup** からスタートアップ登録

> `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` に自身を登録します。

## 備考

- ミューテックスにより二重起動を防止
- Win+R 横取り時にスタートメニューが開く副作用を抑制
- キーボードフックは `WH_KEYBOARD_LL` でシステム全体に適用
