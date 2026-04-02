Redmine WinForms Starter (.NET 10)

- Visual Studio 2026向け: .slnx + net10.0-windows + WinForms
- run_dryrun.bat / run_update.bat を出力フォルダへコピーしてボタンから実行します。

配置
- あなたの既存 run_dryrun.bat / run_update.bat を Scripts フォルダの同名ファイルに上書きするか、
  ビルド出力先(bin\Debug
et10.0-windows\)へ配置してください。

機能
- RedmineURL / APIキーはユーザー設定に保存され、次回起動時に復元されます。
- Load CSV でCSVを読み込み、DataGridViewで編集できます。
- Dry Run / Update でbatを起動し、stdout/stderrをログ欄に表示します。
