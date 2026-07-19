# スケジュール UI 拡張計画

## 目的

現在の `ScheduleManager` は今日と明日の予定だけを保持し、`SchedulePanel` は明日の予定を1件選ぶ画面になっている。
これを、1週間または1か月分の予定を確認・設定できるカレンダー型UIへ変更する。

あわせて、よく使う予定の並びを複数のテンプレートとして保存し、別のゲームセーブデータからも呼び出せるようにする。
予定イベントが開始する前であれば、設定済みの予定をプレイヤーがキャンセルできるようにする。

## 画面構成

`SchedulePanel` は週間表示と月間表示を持つ。

- 週間表示: 現在日を含む7日分
- 月間表示: 現在日を起点とする30日分

暦月ではなく `TimeManager.Day` を基準にし、`Day 12` のようなゲーム内日数を表示する。

```text
SchedulePanel
├── Header（期間移動、週間／月間切り替え）
├── CalendarGrid（7枠または30枠）
├── SelectedDayPanel
│   ├── 日付と現在予定
│   ├── 予定選択ボタン
│   ├── 設定／変更ボタン
│   └── キャンセルボタン
├── TemplatePanel
│   ├── テンプレート一覧と名前入力
│   ├── 保存／適用／削除ボタン
│   └── 適用結果表示
└── CloseButton
```

日付枠にはゲーム内日数、曜日、予定名、状態を表示する。
予定なし、設定済み、実行済み、キャンセル済みを色やアイコンで区別し、詳細は選択日パネルへ表示する。

## 予定データ

`todaySchedule` / `tomorrowSchedule` の2フィールドから、日数をキーにした予定一覧へ移行する。

```csharp
[Serializable]
public class ScheduleEntry
{
    public int day;
    public ScheduleType scheduleType;
    public ScheduleEntryState state;
    public string cancelReason;
}
```

状態は `Planned` / `Executed` / `Cancelled` を基本とする。
同じ日に設定できる主要予定は当面1件とし、複数時間帯の予定は後から検討する。

`ScheduleManager` は次の操作を提供する。

- 指定日の予定を取得
- 指定日に予定を設定・変更
- 指定日の予定をキャンセル
- 指定期間の予定一覧を取得
- 実行開始時に状態を `Executed` へ変更
- 今日・明日の表示を一覧から解決

初期実装では現在日から30日先までを編集可能にする。
過去日は閲覧専用とし、古い履歴を残す上限は実績集計との関係を確認して決める。

## キャンセル

予定はイベント本体が開始する前だけキャンセル可能にする。

キャンセル可能:

- 明日以降の設定済み予定
- 今日の予定でイベント本体が始まっていないもの

キャンセル不可:

- 実行済みの予定
- 予定イベントまたは戦闘が進行中
- 過去日の予定

準備メッセージ表示後でも本体開始前ならキャンセル可能とする。
キャンセル後は短いシステムメッセージを表示し、予定を削除せず `Cancelled` として履歴へ残す。
同じ日に新しい予定を設定する場合は、確認後に新しい `Planned` エントリへ置き換える。

天候による自動中止とプレイヤー操作によるキャンセルは、`cancelReason` などで区別できるようにする。

## スケジュールテンプレート

週間・月間の予定配置を名前付きテンプレートとして複数保存する。

例:

- 訓練中心の週
- 探索中心の週
- ヒロイン交流週
- 休日中心

テンプレートはゲームの `SaveData` とは別に、`Application.persistentDataPath` 配下の `schedule_templates.json` へ保存する。
同じ端末の別セーブスロットから共有できるが、Git管理対象にはしない。

```csharp
[Serializable]
public class ScheduleTemplateData
{
    public string templateId;
    public string displayName;
    public int lengthDays;
    public List<ScheduleTemplateEntry> entries;
}

[Serializable]
public class ScheduleTemplateEntry
{
    public int dayOffset;
    public ScheduleType scheduleType;
}
```

絶対日付ではなく開始日からの `dayOffset` を保存し、選択日を開始日として展開する。
同名保存と削除には確認を表示し、予定が1件もない空テンプレートは保存しない。

適用方法は次の2種類を想定する。

- 空いている日にだけ追加
- 対象期間を上書き

初期実装では「空いている日にだけ追加」を既定にし、追加件数と衝突によるスキップ件数を一度だけ結果表示する。
日ごとの確認ダイアログは出さない。

## 別セーブデータとの共有

セーブスロットごとの予定本体は各 `SaveData` に保存し、テンプレートだけをスロット外の共通ファイルへ保存する。
これにより、現在日、実行済み状態、キャンセル履歴、ヒロイン固有の物語進行は別セーブへ混ざらない。

テンプレート適用時は、現在のセーブで利用できない予定や未解放コンテンツを検査する。
利用不可の予定はスキップし、件数と理由を表示する。
別ヒロインのイベントIDや進行フラグをテンプレートへ含めない。

## セーブデータ移行

`SaveData` に `scheduleEntries` を追加する。
旧セーブの `todaySchedule`、`tomorrowSchedule`、`todayScheduleEventExecuted` は移行元として読み込む。

- `todaySchedule != None` を現在日のエントリへ変換
- `tomorrowSchedule != None` を現在日+1のエントリへ変換
- `todayScheduleEventExecuted` を今日の `Executed` 状態へ変換

新形式を保存できるようになった後もしばらく旧フィールドを読み、移行確認後に旧フィールドの書き込みと定義を削除する。

## 結果表示

設定、変更、キャンセル、テンプレート適用後は画面内に結果を表示する。

```text
Day 12の予定を「二人で湖へ」に設定しました。
Day 13の予定をキャンセルしました。
テンプレート「探索中心の週」を適用しました。追加5件、スキップ2件。
```

大量設定時は予定ごとのダイアログを出さず、最後に集計結果を1回表示する。

## 実装順

1. `ScheduleEntry` と日数キーの予定一覧を追加する（実装済み）
2. 旧今日・明日データとの互換変換を追加する（実装済み）
3. 指定日の設定・変更・キャンセルAPIを追加する（実装済み）
4. 週間表示を実装する（実装・Scene配置・動作確認済み）
5. 月間表示へ拡張する（実装・Scene配置・動作確認済み）
6. 複数テンプレートの保存・読込・削除を実装する
7. テンプレート適用時の衝突・利用可否検査を追加する
8. メイン画面、翌朝処理、予定イベント発動を新一覧へ接続する
9. セーブ／ロード、日付進行、キャンセル、別スロット共有を回帰確認する

最初からドラッグ操作や同日複数予定は導入せず、日付選択とボタン設定で確実に動作させる。

第1段階では `SaveData.scheduleEntries` と `ScheduleEntryState` を追加し、セーブバージョン19から日付別一覧を正本とする。`ScheduleManager` は現在日から30日先までの設定・変更、実行前キャンセル、期間取得、実行済み化を提供する。既存の `TodaySchedule` / `TomorrowSchedule`、明日設定UI、予定イベント処理は互換窓口として一覧を参照するため、週間UIの導入前も従来どおり利用できる。バージョン18以前のロード時は旧今日・明日フィールドを現在日と翌日のエントリへ変換し、新形式の保存時もしばらく旧フィールドを併記する。

週間UIのコードは `SchedulePanel` に実装済み。7個の `dayButtons` / `dayButtonTexts`、期間表示、前週・次週・今日へ戻るボタン、選択日詳細、キャンセルボタンをInspectorで接続すると有効になる。`DayButton0`～`DayButton6` の命名で同じパネルの子階層に配置した場合は、未設定のButtonと子TMP Textを起動時に自動検出する。Inspectorのコンテキストメニュー `Auto Assign Calendar UI References` から手動実行も可能。既存の予定選択ボタンは選択中の日付へ予定を設定し、`scheduleChoiceButtons` に登録すると過去日・実行済み日・31日以上先で自動的に無効になる。7個の日付ボタンを検出できない場合は週間モードを使わず、従来どおり予定選択ボタンで明日の予定を設定する。

月間表示コードも実装済み。`WeeklyViewButton` / `MonthlyViewButton` で表示を切り替え、`MonthGrid` の非アクティブな `ScheduleDayCell` テンプレート1個から最大36セルを生成し、その中へ30日分の日付枠を表示する。日付枠は週間と同じ選択日、予定状態、色、編集可否を使い、表示を切り替えても選択日を維持する。月間の現在期間は現在日から29日先までを表示し、前期間では過去30日を閲覧できる。次期間への移動は現在日開始までに制限し、編集可能範囲外の予定を増やさない。

月間グリッドは日曜を左端、土曜を右端とする。表示開始日の前へ曜日に応じた0～6個の操作不可な空白セルを挿入し、その後へ30日分を配置する。末尾の未使用セルも空白にし、最大36個のセルを生成・再利用する。曜日見出しは後から追加する。

`SelectedDayArea` の位置と大きさは表示別に変更できる。`SchedulePanel` と同じ親の下に表示されないUI用RectTransformとして `WeeklySelectedDayAnchor` / `MonthlySelectedDayAnchor` を置き、それぞれのアンカー、Pivot、位置、サイズを設定する。表示切替時に対象アンカーのRectTransform設定を `SelectedDayArea` へコピーする。3オブジェクトは同じ親を使用し、Layout Groupの自動配置対象には含めない。参照はInspectorまたはオブジェクト名から自動解決する。

## スケジュールテンプレート

テンプレートのデータ基盤は `ScheduleTemplateManager` に実装する。テンプレートは7日または30日の相対日数と `ScheduleType` だけを保持し、`Executed` / `Cancelled` などセーブ固有の状態は保持しない。保存先は `Application.persistentDataPath/schedule_templates.json` とし、通常の `SaveData` から分離するため、同じ端末内なら別のセーブスロットからも利用できる。JSONは一時ファイルを経由して保存し、読み込み不能なファイルがあっても空の一覧へフォールバックする。

`GetTemplates()` は編集されないようコピーを返す。`TrySaveTemplate(...)` は指定した開始日から7日または30日を新規保存し、既存の `templateId` を渡した場合は上書きする。`TryDeleteTemplate(...)` はID指定で削除する。適用前は `TryPreviewTemplateApplication(...)` で適用予定・競合・範囲外などのスキップ件数を確認し、確認後に同じ条件で `TryApplyTemplate(...)` を呼ぶ。適用は必ず `ScheduleManager.CanEditScheduleForDay(...)` と `TrySetScheduleForDay(...)` を通すため、過去、30日を超える未来、実行済みの日は変更されない。既存予定を上書きしない設定では、その日を競合として残す。空の予定もテンプレートに含まれ、上書きを許可した場合は適用先の既存予定を削除できる。

### UIの推奨構成

`SchedulePanel` と同じCanvas配下へ、最初は非アクティブな `ScheduleTemplatePanel` を作る。背景Panelの内側は次の構成にする。

```text
ScheduleTemplatePanel
├─ TitleText                     「スケジュールテンプレート」
├─ TemplateNameInput             TMP_InputField
├─ PeriodDropdown                TMP_Dropdown（7日 / 30日）
├─ SaveNewButton                 「新規保存」
├─ OverwriteButton               「選択中へ上書き」
├─ TemplateListScrollView
│  └─ Viewport
│     └─ Content                 VerticalLayoutGroup
│        └─ TemplateRowTemplate  非アクティブなButton
├─ SelectedTemplateText          選択名と期間
├─ ApplyStartDayText             選択中のDayから適用する旨
├─ OverwriteExistingToggle       「既存予定を上書き」
├─ ApplyButton                   「選択日から適用」
├─ DeleteButton                  「削除」
├─ ResultText                    件数・エラー表示
└─ CloseButton                   「閉じる」
```

通常のスケジュール画面には `OpenTemplateButton`（「テンプレート」）を1個追加し、押したときだけ `ScheduleTemplatePanel` を開く。テンプレート一覧は行を選択してから上書き・適用・削除する方式とし、誤操作を避けるため未選択時は3ボタンを無効にする。保存元と適用先の開始日は、カレンダーで現在選択している `selectedDay` を使う。期間Dropdownの値は0を7日、1を30日に対応させる。

適用ボタンでは、まずプレビュー結果を次のような確認ダイアログへ表示する。

```text
「平日探索」をDay 12から適用します。
適用予定 5件 / 競合 2件 / スキップ予定 0件
既存予定を上書き：しない
```

確認後だけ実適用し、完了後はカレンダーと `ResultText` を更新する。上書きToggleが有効な場合は、空枠による既存予定の削除も起こることを確認文へ明記する。削除にもテンプレート名を含む確認ダイアログを出す。`ScheduleTemplateManager` は専用の空GameObject `ScheduleTemplateManager` へ追加してもよいが、`SchedulePanel` と同じGameObjectへ追加すると参照設定が分かりやすい。JSONは実行時に生成されるユーザーデータなのでGitには追加しない。

`ScheduleTemplatePanelController` にUI制御を実装済み。配置済みUIはオブジェクト名から自動検出し、`SchedulePanel` は `OpenTemplateButton` から現在の選択日を渡して開く。Scene上にControllerを手動追加していない場合も、`ScheduleTemplateManager` と同じGameObjectへ実行時に追加する。一覧行の生成、選択内容の入力欄への反映、新規保存、確認付き上書き・削除、適用前プレビュー、確認後の一括適用、結果表示、適用後のカレンダー更新まで接続する。`PeriodDropdown` はValue 0を7日、Value 1を30日として扱い、表示文言には依存しない。
