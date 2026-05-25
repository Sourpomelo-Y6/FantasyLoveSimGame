# 引継ぎドキュメント

このドキュメントは、`FantasyLoveSim` を次の担当者がすぐ触れるようにするための引継ぎメモです。

## プロジェクト概要

本プロジェクトは Unity 製の恋愛シミュレーション試作です。
行動ボタンから会話や日常行動を選び、`Next` ボタンで進行しながら好感度を上げ、一定値に達するとエンディングが解放されます。

### 現在の特徴

- 行動ボタンは `会話` / `休む` / `散歩` / `お茶` / `贈り物`
- 予定パネルから翌日の予定を設定できる
- 予定パネルは戻るボタンで閉じる
- 会話ジャンルは `Daily` / `Food` / `Adventure` / `Love`
- 会話には `Simple` と `Choice` の 2 種類がある
- `Simple` は `Next` を押すと好感度が増加して会話が終了する
- `Choice` は `Next` で選択肢表示に進み、3 つまでの選択肢から 1 つを選ぶ
- 行動は ScriptableObject ベースで、時間帯・天候・季節・好感度に応じて反応を切り替えられる
- 衣装には着用後の反応を付けられ、`褒める` / `嫌う` / `退屈` / `着替える` の選択肢で評価を更新できる
- 衣装評価の履歴はセーブデータに保存される
- 予定の状態はセーブデータに保存される
- 予定の保存と復元は確認済み
- 予定が行動制限、会話候補、衣装自動選択に影響する
- 予定を翌日の具体イベントに変換する案2は、準備フェーズ付きで実装済み
- 翌朝は今日の予定と着替え可能な準備メッセージを表示し、予定イベント本体は指定された時間帯に発動する
- 予定イベント本体の直前は、衣装確認モードに応じて `このまま出発` / `着替える` を出し分ける
- 衣装確認モードは `Always` / `Conditional` / `Hidden` を想定しており、`Conditional` のときは今の衣装が予定に対して問題ない場合に確認を省略する
- 衣装確認モードの可否は `GameManager.playerOutfitPromptAbilities` と `HeroineStatus.OutfitPromptAbilities` で制御する
- プレイヤーとヒロインそれぞれに詳細ステータス画面を用意し、その中に能力項目と能力獲得画面への導線を置く方針
- 能力はステータス画面から確認し、必要に応じて獲得画面へ移動して解放する
- 現状の能力は `取得` = `解放` として扱い、取得後の `有効` / `無効` 切り替えはまだ持たせない
- 将来、任意でオンオフできる能力が必要になった場合は `Locked` / `Unlocked` / `Active` のような状態分離を検討する
- 現在の能力項目は衣装確認モード向けが中心
- 能力表示は `StatusAbilityData` の ScriptableObject で管理する
- `Assets/Resources/StatusAbilities/` に能力アセットを置くと、`StatusDetailPanel` が読み込んで `targetRole` ごとに表示する
- `StatusDetailPanel` の `playerAbilities` / `heroineAbilities` を Inspector で設定した場合は、その配列を優先して表示する
- `StatusAbilityData.requiredAffection` と `requiredDay` で解放条件を設定でき、条件未達の場合は解放ボタンを押せない
- 解放条件の現在値表示や詳細な不足理由表示は、現時点では必要性が低いため後回しにする
- タイトルから新規ゲームを開始した直後に、メイン画面へ入る前のゲーム開始イベントを挟み、スチル表示もここで行う方針
- タイトル画面とゲーム開始イベント中は `SaveLoadPanel` を閉じた状態に保ち、`Save` / `Load` ボタンを表示しない
- `StatusAbilityData.effectType` が実際の効果を決める。`UseAbilityKind` は旧来互換として `abilityKind` から効果を推測し、`None` は効果なし能力として汎用の取得済みIDだけを保存する
- 初期データとしてプレイヤー用・ヒロイン用の衣装確認能力をそれぞれ用意している
- テスト用など効果を持たない能力は `StatusAbilityKind.TestJump` のような表示種別にし、`effectType` を `None`、`abilityId` を一意に設定すると汎用の取得済みIDとして保存される
- `ConditionalOutfitPrompt` は初期状態で解放済みのため、何もしない能力のテストには使わない
- 詳細ステータス画面は `StatusDetailPanel`、能力項目は `StatusAbilityKind`、画面の対象切り替えは `StatusDetailRole` で扱う
- 詳細ステータス画面の入口として `StatusDetailAction` を用意し、行動一覧から開けるようにしている
- タイトルから新規ゲームを開始した直後は、`GameEventData` の `GameStart` イベントを再生してからメイン画面を始める。`GameEventData` は `Assets/Resources/GameEvents/` に置き、ページ単位で話者・メッセージ・スチルを持てる
- Unity Editor で `MainScene` を直接開いて再生した場合は、`GameStartSettings.ShouldPlayGameStartEvent` の初期値が `false` のため開始イベントは発生しない
- `GameEventData.showOnce` はセーブデータの `shownGameEventIds` で管理する
- `GameEventData` の `DayStart` は翌朝メッセージに混ぜて自動再生し、`Manual` は `GameManager.TryStartManualGameEvent(string eventId)` から明示起動する
- `GameManager` にはデバッグ用に `F7` で `debugManualGameEventId` を呼ぶ入口を用意してある
- テスト用の手動イベントとして `TestManualEvent` を用意している。`GameManager.debugManualGameEventId` に `TestManualEvent` を設定すると `F7` で繰り返し再生できる
- イベントIDは `GameStartIntro`、`DayStart_条件_連番`、`Manual_用途_連番`、`Story_章_連番`、`Still_用途_連番` のように用途が分かる名前にする。`eventId` は既読管理に使うため、本番投入後は変更しない
- イベントスチル画像は `Assets/Images/Event/` に置き、`GameStartIntro_01.png` のようにイベントIDに寄せたファイル名にする
- スチル回想は、イベント既読の `shownGameEventIds` とは別に `unlockedStillIds` のような保存リストを持たせる方針。`GameEventPageData` に `stillId` を追加し、スチル表示時に解放する案で進める
- 回想 UI は Unity 上で手作業配置し、コード側は回想パネル、一覧コンテナ、項目 Prefab、拡大表示 Image、戻るボタンなどの参照を受け取る
- `StatusDetailPanel` の画面部品は Unity 上で手作業配置し、Inspector で参照を割り当てる
- 必須参照は `panelRoot`、各 `TextMeshProUGUI`、各 `Button`、`abilityListParent`、`abilityButtonPrefab`、`abilityAcquirePanel` 周辺
- `GameManager.EnsureStatusDetailPanel()` は配置済みの `StatusDetailPanel` を探して初期化するだけで、UI の自動生成は行わない
- 会話や行動のたびに時間が進み、一定数で日付が進む
- 好感度が `100` に達すると `Ending` ボタンが表示される
- 翌朝開始時など複数メッセージが連続発生する場合は、話者付きメッセージキューに積み、`Next` で 1 件ずつ表示する

## 使用環境

- Unity `2021.3.45f1`
- URP 2D
- TextMeshPro

## 作業分担ルール

- UI デザイン、Unity シーン編集、Inspector の参照設定は手作業で行う
- コード側の実装、データ構造、仕様メモ、ドキュメント更新は Codex が担当してよい
- UI 追加が必要な機能では、先に必要な `SerializeField` や接続ポイントを整理し、Unity 上の配置や見た目は手作業で反映する
- Unity ファイルを書き換える必要がある場合は、事前に作業範囲を確認する
- 既にコードで組んでしまった UI は、シーン上の配置に置き換えるときに対象を洗い出してから削る

## 主要ファイル

- [`Assets/Scripts/Core/GameManager.cs`](../Assets/Scripts/Core/GameManager.cs): ゲーム進行の中心ロジック
- [`Assets/Scripts/Core/BackgroundZoom.cs`](../Assets/Scripts/Core/BackgroundZoom.cs): 背景ズーム演出
- [`Assets/Scripts/Action/`](../Assets/Scripts/Action): 行動データ型の定義
- [`Assets/Scripts/Conversation/`](../Assets/Scripts/Conversation): 会話データ型の定義
- [`Assets/Scripts/Schedule/`](../Assets/Scripts/Schedule): 予定管理と予定パネル制御
- [`Assets/Scripts/Schedule/ScheduledEventData.cs`](../Assets/Scripts/Schedule/ScheduledEventData.cs): 予定イベントの ScriptableObject 定義
- [`Assets/Scripts/Schedule/ScheduledEventDefinition.cs`](../Assets/Scripts/Schedule/ScheduledEventDefinition.cs): 実行時に使う予定イベント定義
- [`Assets/Resources/ScheduledEvents/`](../Assets/Resources/ScheduledEvents): 予定イベントデータの実体
- [`Assets/Resources/Actions/`](../Assets/Resources/Actions): 行動データの実体
- [`Assets/Resources/Actions/ScheduleAction.asset`](../Assets/Resources/Actions/ScheduleAction.asset): 予定パネルを開く行動アセット
- [`Assets/Resources/Conversations/`](../Assets/Resources/Conversations): 会話データの実体
- [`Assets/Scenes/MainScene.unity`](../Assets/Scenes/MainScene.unity): メインシーン
- [`Packages/manifest.json`](../Packages/manifest.json): パッケージ一覧
- [`ProjectSettings/ProjectVersion.txt`](../ProjectSettings/ProjectVersion.txt): Unity バージョン情報

## 全体構成

### ルート

- `Assets/`: ゲーム本体のアセット
- `Packages/`: Unity パッケージ設定
- `ProjectSettings/`: プロジェクト設定
- `Docs/`: この引継ぎ資料

### `Assets` の役割

- `Assets/Scripts/Core/`: 進行管理の本体
- `Assets/Scripts/Action/`: 行動データの型
- `Assets/Scripts/Conversation/`: 会話データの型
- `Assets/Resources/Actions/`: 行動資産
- `Assets/Resources/Conversations/`: 会話資産
- `Assets/Scenes/`: シーン
- `Assets/Fonts/`: 日本語表示用フォント資産
- `Assets/Settings/`: URP 関連設定
- `Assets/TextMesh Pro/`: TextMeshPro 標準アセット群

## GameManager の役割

[`Assets/Scripts/Core/GameManager.cs`](../Assets/Scripts/Core/GameManager.cs) がゲーム進行をほぼすべて管理しています。

### 管理している状態

- `day`
- `affection`
- `timeIndex`
- `flowState`
- `currentConversation`
- 会話データの一覧
- 行動データの一覧
- 衣装評価の保存データ
- 予定の保存データ

### 管理している UI

- 日付表示
- 時刻表示
- 好感度表示
- 会話文表示
- 行動ボタン
- ジャンルボタン
- 選択肢ボタン
- 衣装反応パネル
- 予定パネル
- 予定表示テキスト
- `Next` ボタン
- エンディングボタン

## ゲームの流れ

### 1. Start 時

`Start()` で以下を実行します。

- 会話データと行動データを読み込む
- 各ボタンにイベントを設定する
- 行動ボタン、ジャンルボタン、選択肢領域を初期化する
- 衣装反応パネルを閉じる
- 予定の表示を UI に反映する
- `Next` ボタンを非表示にする
- 初期メッセージを表示する
- UI を更新する
- 必要ならロード処理を行う

### 2. 行動ボタン押下

`ExecuteAction(ActionData action)` が呼ばれます。

- `isEnabled` や好感度、時間帯、天候、季節の条件を確認する
- 条件に合う場合は `ExecuteActionData()` で結果を表示する
- `OpenConversationGenres()` の場合はジャンルボタンを表示する
- `OpenOutfitReactionPanel()` の場合は衣装反応パネルを表示する
- `OpenSchedulePanel()` の場合は予定パネルを表示する
- 条件に合わない場合は `unavailableMessage` を表示する

### 3. 会話開始

`StartTalk(ConversationGenre genre)` が呼ばれます。

- 行動ボタンとジャンルボタンを閉じる
- 指定ジャンルに一致する会話データを抽出する
- ランダムで 1 件を選ぶ
- `Simple` なら会話文を表示して `Next` で終了する
- `Choice` なら会話文を表示して `Next` で選択肢表示に進む

### 4. `Next` 押下

`OnClickNext()` が現在の状態に応じて分岐します。

- `ShowingQuestion` の場合は選択肢を表示する
- `ShowingSimple` の場合は好感度を加算して会話を終了する
- `ShowingResponse` の場合は時間を進めて会話を終了する
- `ShowingActionResult` の場合は行動結果を閉じて通常状態に戻る

### 5. 選択肢選択

`SelectChoice(ConversationChoice choice)` が呼ばれます。

- 選択肢表示を閉じる
- ヒロインの返答文を表示する
- 好感度を選択肢の値だけ増減する
- `Next` ボタンを表示する

### 6. 行動反応

`ActionData` には `reactions` があるため、条件に合う反応を優先度順に選べます。

- 条件一致する反応があれば、その内容を優先して表示する
- 条件一致しない場合はデフォルト結果を使う
- `useHeroineNameAsSpeaker` で話者を切り替えられる

### 7. 時間経過

`AdvanceTime()` によって `timeIndex` が進みます。

- 時間は 4 段階
- 最終段階に到達すると日付が 1 日進む
- `timeNames` 配列の内容を UI に出している

### 8. エンディング解放

`ClampAffection()` 内で好感度をチェックしています。

- `affection < 0` の場合は `0` に丸める
- `affection >= 100` の場合は `100` に丸める
- このとき `Ending` ボタンを表示する

### 9. Ending ボタン

`OnClickEnding()` は現状、固定のエンド文を表示するだけです。

### 10. 衣装評価

`OutfitManager` と `OutfitPreferenceManager` が衣装の着用と反応を管理します。

- 衣装を着ると、その `outfitId` の着用回数が増える
- 衣装反応パネルで `褒める` / `嫌う` / `退屈` を選ぶと、衣装ごとの評価値と回数が更新される
- `着替える` を選ぶと衣装選択に戻る
- 衣装評価は `SaveData.outfitPreferences` に保存・復元される

## Inspector で必要な参照

`GameManager` の `SerializeField` は、Unity の Inspector で必ず割り当てる必要があります。

### Status

- `dayText`
- `timeText`
- `weekdayText`
- `seasonText`
- `weatherText`
- `affectionText`
- `affectionRankText`

### Dialogue

- `speakerNameText`
- `dialogueText`
- `backgroundZoom`

### Action Buttons

- `actionButtonArea`
- `actionButtonAreaColumnLeft`
- `actionButtonAreaColumnCenter`
- `actionButtonAreaColumnRight`
- `actionButtonParent`
- `actionButtonPrefab`
- `ActionData.displayColumn` が `Left` / `Center` / `Right` の場合は指定列へ配置する
- `displayColumn` が `Auto` の場合や指定列が存在しない場合は、`sortOrder` 順の有効な `ActionData` を利用可能な列数で均等に分配する
- 列内の並び順は引き続き `sortOrder` を使う
- 初期アクションは左列に `Rest` / `Walk` / `Tea`、中央列に `Talk` / `Gift` / `StatusDetail`、右列に `DressUp` / `OutfitReaction` / `Schedule` を配置している
- 列見出しや列分類の微調整は現時点では後回し。必要になったら Unity UI 上で見出し Text を手動配置し、表示分類は `ActionData.displayColumn` の設定で調整する

### Genre Buttons

- `genreButtonArea`
- `genreButtonParent`
- `genreButtonPrefab`

### Choice Buttons

- `choiceButtonArea`
- `choiceButton1`
- `choiceButton2`
- `choiceButton3`

### Control Buttons

- `nextButton`

### Ending

- `endingButton`

### Data

- `actionResourcePath`
- `conversationResourcePath`

### Outfit

- `outfitManager`
- `outfitPreferenceManager`
- `outfitReactionPanel`
- `praiseOutfitButton`
- `dislikeOutfitButton`
- `boredOutfitButton`
- `changeOutfitButton`

### Schedule

- `scheduleManager`
- `schedulePanel`
- `todayScheduleText`
- `tomorrowScheduleText`

### Save / Load Buttons

- `saveButton`
- `loadButton`
- `MainScene` では `SaveButton` から `SaveLoadPanel.OpenSave()`、`LoadButton` から `SaveLoadPanel.OpenLoad()` を呼ぶ
- `GameManager.Start()` の即セーブ・即ロード接続は使わず、セーブロードパネル経由で操作する

### Save Slots

- `SaveManager.saveSlotCount` で利用するスロット数を設定する
- 現在の `TitleScene` / `MainScene` は `saveSlotCount = 4`
- `GameManager.SaveGameToSlot(int slotIndex)` で指定スロットに保存する
- `GameManager.LoadGameFromSlot(int slotIndex)` で指定スロットから読み込む
- `GameManager.SelectSaveSlot(int slotIndex)` で現在の対象スロットを切り替える
- `GameManager.HasSaveDataInSlot(int slotIndex)` で指定スロットにデータがあるか確認する
- `TitleManager.SelectSaveSlot(int slotIndex)` はタイトル画面のスロット選択 UI から呼ぶ想定
- `TitleManager.ContinueFromSelectedSlot(int slotIndex)` はタイトル画面のロード用スロットボタンから呼ぶ想定
- `TitleManager.HasSaveDataInSlot(int slotIndex)` でタイトル画面からスロット状態を確認する
- `TitleManager.GetSaveSlotCount()` で実際のスロット数を取得する
- `slot 0` は従来の `save.json` を使い、既存セーブとの互換を保つ
- `SaveLoadPanel` は `TitleScene` と `MainScene` に同じ prefab を置いて使う共通 UI 制御用スクリプト
- `SaveLoadPanel` は開いたモードに応じて背景色とタイトルを切り替える
- `SaveLoadPanel` は起動時に閉じた状態で始まる
- 保存済みスロットのラベルは `Slot 1 / Day 3 / Affection 42` のように日数と好感度を表示する
- スロット一覧表示では `SaveManager.LoadPreview(int slotIndex)` でセーブデータを読み、現在選択中スロットは変更しない
- `MainScene` でスロットからロードした後は `SaveLoadPanel.Close()` でパネルを閉じる

### 参照漏れ時の症状

- ボタンを押しても反応しない
- UI 更新時に `NullReferenceException` が出る
- 選択肢表示が出ない
- `Next` ボタンが出ない、または進行しない
- スロットラベルが変わらない場合は `SaveLoadPanel.slotLabels` の割り当て漏れを確認する
- 4つ目のスロットボタンが押せない場合は `SaveManager.saveSlotCount` が `4` になっているか確認する

## UI 実装上の前提

- 行動ボタンは `ActionButtonPrefab`
- ジャンルボタンは `GenreButtonPrefab`
- テキスト表示は `TMP_Text`
- 選択肢ボタンは `GetComponentInChildren<TMP_Text>()` でラベルを書き換える
- `choiceButtonArea` はまとめて表示・非表示を切り替える

## 既知の実装制約

- 会話データと行動データは ScriptableObject 化されているが、一覧の登録は Inspector 依存
- エンディングは 1 パターンのみ
- 分岐による永続状態はない
- セーブスロット UI は prefab 化済みで、`TitleScene` と `MainScene` に配置済み
- 話者ラベルは `SYSTEM` / `予定` / `衣装` / ヒロイン名に分けている
- 話者タイプごとに `speakerNameText` と `dialogueText` の色を変え、ヒロイン発話・システム通知・予定通知・衣装通知を見分けやすくしている
- ただしヒロイン発話とシステム通知は同じメッセージボックスに出るため、今後はシステム通知専用パネルなどの追加分離を検討する
- 翌朝開始時のメッセージは話者付きキューで順番に表示する。汎用ログ画面はまだ未実装
- 汎用ログ画面を追加する場合は、まずセッション中の直近ログだけを対象にする。会話、行動結果、予定、衣装通知を話者タイプ付きで保持し、セーブデータには入れない方針
- ログ件数は固定上限を持たせる。初期案は直近 20 件程度
- 衣装確認モードの解放条件は、`GameManager.playerOutfitPromptAbilities` と `HeroineStatus.OutfitPromptAbilities` の組み合わせで管理する

## 変更しやすいポイント

### 行動を増やす

`ActionData` アセットを `Assets/Resources/Actions/` に追加すればよいです。
行動の反応を増やすなら `ActionReactionData` を使います。
行動ボタンの列を固定したい場合は、`ActionData.displayColumn` を `Left` / `Center` / `Right` に設定します。

### 予定を増やす

予定の種類を増やすなら `ScheduleType` を更新し、`SchedulePanel` と `ScheduleManager` の表示や選択肢を合わせて修正します。

### 予定を行動へ変換する

案2は `ScheduledEventData -> ScheduledEventDefinition` の変換として実装済みです。
翌日開始時は準備メッセージだけを表示し、予定イベント本体は `triggerTimeSlot` に到達した後で発動します。
イベント直前には `allowOutfitChangeBeforeStart` と `outfitPromptMode` を見て、衣装確認モードに応じて `このまま出発` / `着替える` を表示します。
`Conditional` は今の衣装が予定に対して問題ない場合は確認を省略し、`Hidden` は確認を出しません。
現在は昼発動の固定メッセージ中心ですが、今後は予定ごとに昼・夜などの発動時間を拡張できます。

予定イベントを増やす場合は、`Assets/Resources/ScheduledEvents/` に `ScheduledEventData` アセットを追加します。
同じ `ScheduleType` のアセットがある場合、`GameManager` はそのデータを優先し、アセットがない場合だけコード内の既定定義へフォールバックします。
予定イベント本文の話者は `ScheduledEventData.eventSpeakerType` で指定でき、`Heroine` / `System` / `Schedule` / `Outfit` から選べます。

### 会話を増やす

`ConversationData` アセットを追加して、`Assets/Resources/Conversations/` に配置すればよいです。
会話IDは `Daily_Morning_01` のように `カテゴリ_条件_連番` を基本にし、`showOnce` は一度見せたら再出現させたくない会話だけに使う。
`minAffection` / `maxAffection` は会話の入口条件、`allowedTimeSlots` / `allowedWeathers` / `allowedSeasons` は必要なときだけ絞り込む。条件が重なる会話が複数ある場合は `priority` で並び替える。
同じ条件で複数の会話を置く場合は、まず `priority`、次に `conversationId` の命名で識別できるようにする。

### エンディングを増やす

現在は好感度 `100` の単一条件です。
条件分岐を増やすなら、好感度だけでなく以下も判定対象にできます。

- 選択したジャンルの回数
- 特定会話の達成有無
- 日数

### セーブスロット UI

UI デザインは手作業で行っています。
シーンを増やさず、同じ `Assets/Prefabs/SaveLoadPanel.prefab` を `TitleScene` と `MainScene` の両方に置く構成です。

共通 prefab の構成:

- ルートに `SaveLoadPanel` をアタッチする
- `Save Manager` に同じシーンの `SaveManager` を割り当てる
- `Panel Root` に表示・非表示したいパネル本体を割り当てる
- `Close Button` に閉じるボタンを割り当てる
- `Background Image` に背景色を切り替える `Image` を割り当てる
- `Title Text` に `SaveLoadTitleText` を割り当てる
- `Save Background Color` は青系、`Load Background Color` はオレンジ系
- `Save Title` は `セーブ`、`Load Title` は `ロード`
- `Slot Buttons` にスロットボタンを順番に割り当てる
- `Slot Labels` に各スロットの TMP ラベルを順番に割り当てる
- `Auto Wire Slot Buttons` を有効にすると、配列順で `SelectSlot(0..)` が自動接続される
- 現在は `SlotButton_0` から `SlotButton_3` までの4スロット

`TitleScene` 側の接続:

- `Title Manager` にシーン上の `TitleManager` を割り当てる
- `Game Manager` は空のままでよい
- `ContinueButton` から `SaveLoadPanel.OpenLoad()` を呼ぶ
- タイトルでは保存しないため `OpenSave()` は使わない
- `TitleManager.Start()` の `continueButton.onClick.AddListener(OnClickContinue)` は使わず、Inspector の OnClick でロードパネルを開く

`MainScene` 側の接続:

- `Game Manager` にシーン上の `GameManager` を割り当てる
- `Title Manager` は空のままでよい
- セーブ表示ボタンから `SaveLoadPanel.OpenSave()` を呼ぶ
- ロード表示ボタンから `SaveLoadPanel.OpenLoad()` を呼ぶ
- `SaveManager.saveSlotCount` は `4`
- ロード後は `SaveLoadPanel` が自動で閉じる

この構成にすると、見た目と階層は同じ prefab で管理し、動作だけを `TitleManager` / `GameManager` の参照で切り替えられます。

### セーブ/ロード回帰確認チェックリスト

手動確認時は、次の状態が保存・復元されるかを見る。

- 好感度、日付、時間帯、曜日、季節、天気がロード後に戻る
- 現在の衣装がロード後に戻る
- 衣装評価の履歴がロード後に戻り、同じ衣装への反応が継続する
- 今日/明日の予定がロード後に戻る
- 当日予定イベントの発動済み状態がロード後に戻り、同じ日に二重発火しない
- 能力取得状態がロード後に戻る
- 衣装確認モードの解放状態がロード後に戻る
- 表示列 `ActionData.displayColumn` はアセット設定なので、セーブ/ロードで変化しない
- 翌朝メッセージキューと将来の汎用ログはセーブ対象外の方針なので、ロード後に復元されなくてよい
- タイトル画面からロードした場合も、MainScene のロード後 UI が閉じた状態で始まる

## 追加開発の優先候補

1. 会話データの分類ルール整理
2. 行動データの反応パターン追加
3. スチル表示と回想の導線追加
4. 立ち絵切り替えと表情差分の整理
5. セーブスロット UI の調整
6. 予定を行動へ変換する
7. セーブ/ロードの強化
8. エンディング分岐の追加
9. UI の見た目調整

## デバッグ時の確認項目

- `GameManager` がシーン上のオブジェクトにアタッチされているか
- すべての `SerializeField` が割り当て済みか
- `choiceButtonArea` の初期状態が想定どおりか
- `nextButton` が割り当て済みか
- `actionButtonParent` と `actionButtonPrefab` が割り当て済みか
- `scheduleManager` と `schedulePanel` が割り当て済みか
- `actionResourcePath` が `Actions` になっているか
- `conversationResourcePath` が `Conversations` になっているか
- `Assets/Scenes/MainScene.unity` を開いているか
- Unity バージョンが `2021.3.45f1` か

## 備考

- 背景演出やアクション反応は拡張しやすいので、必要になった時点でデータを追加するのがよい
