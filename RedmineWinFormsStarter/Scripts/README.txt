# Redmine 4.2.x 既存チケットをCSVから一括更新（Python未インストール環境向け）

このフォルダは **インストール不要**で動くように作っています。
Python本体は「Windows embeddable package」を同じフォルダへ展開して使います。

## 1) 準備
- Redmine URL (例: https://redmine.example.com)
- Redmine APIキー（個人設定で確認）
- 更新したいCSV（このフォルダの issues.csv）

## 2) ポータブルPythonを置く（インストール不要）
1. https://www.python.org/downloads/windows/ を開く
2. **Windows embeddable package (64-bit)** をダウンロード
3. ZIPを展開し、中の `python.exe` など一式をこのフォルダにコピー

## 3) 設定
run_dryrun.bat / run_update.bat をメモ帳で開いて、
- REDMINE_BASE_URL
- REDMINE_API_KEY
を自分の環境に合わせて書き換え。

## 4) 実行
1. まず `run_dryrun.bat`（更新しない・安全確認）
2. 問題なければ `run_update.bat`（本番更新）

## 5) 仕様
- 更新キーはCSV列 `#`（チケット番号）
- 空欄の列は更新しない（既存値を壊さない）
- 標準項目 + 同名のカスタムフィールドを更新

