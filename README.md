# RedmineWinFormsStarter

`RedmineWinFormsStarter` は、Redmine のチケット CSV を読み込み、内容確認、編集、コメント確認、チケット新規追加を行うための WinForms アプリです。

**`使用する際は「RedmineWinFormsStarter\bin\Release」のReleaseフォルダをダウンロードし、RedmineWinFormsStarter.exeを起動してください。`**

## 連携タグ

README と実コードの対応を追いやすくするため、以下の連携タグを使います。

| タグ | 用途 | 主な対応ファイル |
| --- | --- | --- |
| `RMWIN001` | WinForms メイン画面と操作起点 | `RedmineWinFormsStarter/MainForm.cs` |
| `RMCSV001` | CSV 読込と CP932/UTF-8 判定 | `RedmineWinFormsStarter/CsvLoader.cs` |
| `RMCSV002` | 内部管理 CSV ワークスペース | `RedmineWinFormsStarter/CsvWorkspaceSession.cs` |
| `RMCSV003` | Redmine からの CSV ダウンロード | `RedmineWinFormsStarter/RedmineFieldOptionsService.cs` |
| `RMBAT001` | 検証実行バッチ | `RedmineWinFormsStarter/run_dryrun.bat` |
| `RMBAT002` | 更新実行バッチ | `RedmineWinFormsStarter/run_update.bat` |
| `RMBAT003` | WinForms から BAT 起動 | `RedmineWinFormsStarter/BatRunner.cs` |
| `RMPY001` | Python による CSV 一括更新本体 | `RedmineWinFormsStarter/Scripts/redmine_update_from_csv.py` |
| `RMPS001` | 旧 PowerShell GUI ツール | `RedmineWinFormsStarter/Scripts/RedmineTicketTool_FullGUI.ps1` |
| `RMCMT001` | コメント一覧取得と追記更新 | `RedmineWinFormsStarter/MainForm.cs`, `RedmineWinFormsStarter/RedmineFieldOptionsService.cs` |
| `RMCRE001` | 新規チケット追加 | `RedmineWinFormsStarter/MainForm.cs`, `RedmineWinFormsStarter/RedmineFieldOptionsService.cs` |
| `RMMETA001` | プロジェクト依存の候補取得 | `RedmineWinFormsStarter/RedmineFieldOptionsService.cs` |

## 主な機能

- ローカル CSV の読込 `RMCSV001` `RMCSV002` `RMWIN001`
- Redmine からの CSV 直接ダウンロード `RMCSV003` `RMWIN001`
- プロジェクト指定でのチケット一覧読込 `RMCSV003` `RMMETA001` `RMWIN001`
- CSV の編集と保存 `RMCSV002` `RMWIN001`
- 列ごとのフィルタ `RMWIN001`
- チケットの全コメント一覧表示 `RMCMT001`
- コメント更新内容の追記反映 `RMCMT001`
- チケットの一括新規追加 `RMCRE001` `RMMETA001`

## 事前準備

### 1. `RedmineURL`
Redmine のベース URL を入力します。

例:

- `https://redmine.example.com`
- `https://redmine.example.com/redmine`

### 2. `API Key`
Redmine の個人用 API キーを入力します。

取得例:

1. Redmine にログイン
2. 右上の個人設定を開く
3. API アクセスキーを確認

## 画面説明

### 上部入力欄

#### `RedmineURL`
- Redmine サーバーの URL
- CSV ダウンロード、コメント取得、チケット作成時に使用

#### `API Key`
- Redmine API への接続に使用
- プロジェクト読込、候補取得、コメント取得、チケット作成に必要

### ボタン

#### `CSV読込`
CSV の読み込み方法を選択します。

連携タグ: `RMWIN001` `RMCSV001` `RMCSV002` `RMCSV003`

選択肢:

1. ローカルの CSV を取り込む
2. Redmine から CSV を直接ダウンロード
3. プロジェクト指定で CSV を直接ダウンロード

#### `CSV保存`
現在画面で編集している内容を、アプリ内部で管理している CSV に保存します。

連携タグ: `RMCSV002` `RMWIN001`

#### `検証実行`
現在の CSV を使って更新内容をドライラン実行します。

連携タグ: `RMBAT001` `RMBAT003` `RMPY001`

- 実際の更新は行いません
- 送信内容やエラー確認に使います

#### `Redmine更新`
現在の CSV を使って Redmine へ更新を反映します。

連携タグ: `RMBAT002` `RMBAT003` `RMPY001`

## タブ説明

### `チケット`
チケット一覧を表示します。

連携タグ: `RMWIN001` `RMCSV002` `RMMETA001`

#### 使い方
- セルを直接編集できます
- 選択肢がある項目はプルダウン表示になります
- 上部のフィルタ欄で各列を絞り込みできます
- `フィルタ解除` で全列フィルタをクリアします

#### よく使う列

##### `#`
- チケット番号
- 既存チケット更新時のキーです

##### `プロジェクト`
- Redmine のプロジェクト名
- 例: `開発部共通`, `営業支援` など

##### `トラッカー`
- チケット種別
- 例: `バグ`, `機能`, `サポート` など

##### `ステータス`
- チケット状態
- 例: `新規`, `進行中`, `完了` など

##### `優先度`
- Redmine の優先度名

##### `題名`
- チケットの件名

##### `担当者`
- 対象プロジェクトで選択可能なメンバー名

##### `カテゴリ`
- プロジェクトに設定されているカテゴリ名

##### `対象バージョン`
- プロジェクトに設定されている対象バージョン名

##### `開始日`
- `YYYY-MM-DD` または `YYYY/MM/DD`

##### `期日`
- `YYYY-MM-DD` または `YYYY/MM/DD`

##### `進捗率`
- `0` から `100` の整数

##### `プライベート`
- `はい` または `いいえ`

##### `説明`
- チケット本文

##### カスタムフィールド列
- Redmine 側のカスタムフィールド名そのままで表示されます
- 候補付きの項目はプルダウンまたは候補制御されます

### `コメント`
CSV に含まれるチケット番号に対するコメントを一覧表示します。

連携タグ: `RMCMT001`

#### `コメント再読込`
Redmine から最新のコメント一覧を再取得します。

#### `コメント追記`
ポップアップ画面から `チケット番号` と `コメント` を入力して、新しいコメントを追加します。

#### 既存コメントについて
- 一覧表示される既存コメントは参照専用です
- 標準 Redmine API の制約により、既存コメント ID を保ったまま本文だけを直接編集することはしていません
- コメントが 0 件のチケットにも、`コメント追記` から新規コメントを追加できます

### `新規追加`
新規チケットを作成する画面です。

連携タグ: `RMCRE001` `RMMETA001`

#### 上部の `プロジェクト`
- ここでプロジェクトを選ぶと、作成グリッドの `プロジェクト` 列へ反映されます
- プロジェクトに応じて `トラッカー` や候補項目が変化します

#### `親チケット`
- 空白なら通常の新規チケットとして作成されます
- 親チケット番号を入力すると、そのチケットの子チケットとして作成されます

#### `入力クリア`
作成用の入力行をリセットします。

#### `チケット作成`
入力された複数行をまとめて新規作成します。

### 新規作成時の入力項目

#### 必須
- `プロジェクト`
- `トラッカー`
- `題名`

#### 任意
- `親チケット`
- `ステータス`
- `優先度`
- `担当者`
- `カテゴリ`
- `対象バージョン`
- `開始日`
- `期日`
- `進捗率`
- `プライベート`
- `説明`

## CSV 読込について

### 対応エンコーディング
- UTF-8
- CP932

連携タグ: `RMCSV001`

Redmine のエクスポート CSV が CP932 の場合でも読込できます。

### 内部管理 CSV
このアプリは、読み込んだ CSV をそのまま直接編集せず、内部で扱いやすい作業用 CSV に変換して管理します。

連携タグ: `RMCSV002`

そのため:

- 元のダウンロードファイルの文字化けを避けやすい
- ローカルファイル読込時も内部で統一形式に変換される
- 保存や実行は内部 CSV を使って行われる

## よくある使い方

### 既存チケットを更新したい
1. `RedmineURL` を入力
2. `API Key` を入力
3. `CSV読込` で CSV を読み込む
4. `チケット` タブで内容を編集
5. `CSV保存` で保存
6. `検証実行` で確認
7. 問題なければ `Redmine更新`

連携タグ: `RMWIN001` `RMCSV001` `RMCSV002` `RMBAT001` `RMBAT002` `RMPY001`

### 新規チケットをまとめて登録したい
1. `新規追加` タブを開く
2. 上部の `プロジェクト` を選ぶ
3. 各行に `トラッカー`、`題名` などを入力
4. `チケット作成` を押す

連携タグ: `RMCRE001` `RMMETA001`

## 補足

- 候補付きの列は Redmine の設定内容に応じて制御されます
- 読み込んだチケット一覧とコメント一覧は Redmine 接続情報に依存します
- コメント機能は一覧表示と追記反映に対応しています
- 旧 PowerShell 版 GUI も同梱しています `RMPS001`
