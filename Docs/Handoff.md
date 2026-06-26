# 引継ぎドキュメント

このドキュメントは、`FantasyLoveSim` を次の担当者がすぐ触れるようにするための引継ぎメモです。

## プロジェクト概要

本プロジェクトは Unity 製の恋愛シミュレーション試作です。
行動ボタンから会話や日常行動を選び、`Next` ボタンまたはメッセージウィンドウクリックで進行しながら好感度を上げ、一定値に達するとエンディングが解放されます。

### 現在の特徴

- 行動ボタンは `会話` / `休む` / `散歩` / `お茶` / `贈り物`
- 予定パネルから翌日の予定を設定できる
- 予定パネルは戻るボタンで閉じる
- 会話ジャンルは `Daily` / `Food` / `Adventure` / `Love`
- 会話には `Simple` と `Choice` の 2 種類がある
- ヒロイン別会話は `Conversations/<ConversationId>.asset` の個別 `ConversationData` として管理する。旧 `Conversations.asset` container も互換で読めるが、profile の `conversationResourcePath` は `Heroines/<HeroineId>/Conversations` を指し、container と個別 asset を同時に読まない
- 既存互換として、旧 `Conversations.asset` container も読み込める
- `Simple` は `Next` を押すと好感度が増加して会話が終了する
- `Choice` は `Next` で選択肢表示に進み、3 つまでの選択肢から 1 つを選ぶ
- 行動は ScriptableObject ベースで、時間帯・天候・季節・好感度に応じて反応を切り替えられる
- 行動反応は `ActionData.reactions` に入れ、`priority` が高い候補を優先して 1 件選ぶ。条件は好感度・時間帯・天候・季節で切り替えられ、反応専用の `stillSprite` も持てる
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
- タイトルから新規ゲームを開始した直後は、`GameEventData` の `GameStart` イベントを再生してからメイン画面を始める。`GameEventData` はヒロイン別 Resources パスに置き、ページ単位で話者・メッセージ・スチルを持てる
- ヒロイン差し替えは `HeroineProfileData` で管理する。画像、会話、イベント、行動反応、エンディング、朝夜の挨拶などの共通セリフをヒロイン単位で束ね、`Images/Background` は共通背景として扱う。現在は `DefaultHeroineProfile.asset` で `Heroines/DefaultHeroine/Actions` / `Conversations` / `GameEvents` / `Endings` を参照している
- タイトル画面には、将来的にキャラクター選択ボタンを追加する。`Resources.LoadAll<HeroineProfileData>("Heroines")` で候補を列挙し、選択中プロフィールの表示名と立ち絵をプレビューして、決定後に新規ゲーム用の選択ヒロインとして `GameStartSettings` へ渡す。ロード時はセーブデータ内のヒロイン ID を優先する
- `HeroineProfileData` の共通セリフは Unity 側では編集、読み込み可能。`FantasyLoveSimAssetTool` 側の編集 UI と `heroine_profile_export.json` 出力対応は後回しの TODO
- 差し替え確認用として `TestHeroineProfile.asset` と `Heroines/TestHeroine/...` の最小データを追加済み。`GameManager.heroineProfile` に割り当てると、ヒロイン別読み込みの手動確認に使える
- 別リポジトリまたは別フォルダで Stable Diffusion 向けキャラクター素材生成ツールを作る方針。仕様は `Docs/CharacterAssetGenerationToolSpec.md` に整理済み
- Unity Editor で `MainScene` を直接開いて再生した場合は、`GameStartSettings.ShouldPlayGameStartEvent` の初期値が `false` のため開始イベントは発生しない
- `GameEventData.showOnce` はセーブデータの `shownGameEventIds` で管理する
- `GameEventData` の `DayStart` は翌朝メッセージに混ぜて自動再生し、`Manual` は `GameManager.TryStartManualGameEvent(string eventId)` から明示起動する
- `GameManager` にはデバッグ用に `F7` で `debugManualGameEventId` を呼ぶ入口を用意してある
- テスト用の手動イベントとして `TestManualEvent` を用意している。`GameManager.debugManualGameEventId` に `TestManualEvent` を設定すると `F7` で繰り返し再生できる
- イベントIDは `GameStartIntro`、`DayStart_条件_連番`、`Manual_用途_連番`、`Story_章_連番`、`Still_用途_連番` のように用途が分かる名前にする。`eventId` は既読管理に使うため、本番投入後は変更しない
- `GameEventData` には `minDay` / `maxDay` / `minAffection` / `maxAffection` / `requiredShownEventIds` / `blockedShownEventIds` を追加済み。発生可否は `GameManager.CanStartGameEvent(GameEventData gameEvent)` に集約し、日開始イベントと手動イベントの両方で同じ条件判定を使う
- `GameEventData` は衣装条件も持てる。`requiredOutfitIds` / `blockedOutfitIds` の文字列ID指定に加えて、Unity Inspector で `OutfitData` アセットを選べる `requiredOutfits` / `blockedOutfits` を追加済み。判定は現在の `OutfitManager.CurrentOutfit.outfitId` に対して行う
- イベントスチル画像は `Assets/Images/Heroines/<HeroineId>/Event/` に置き、`GameStartIntro_01.png` のようにイベントIDに寄せたファイル名にする
- 将来は JSON から画像ファイルパスを取得し、イベントや回想で表示する画像を差し替えられる仕組みを追加する。画像パスは Unity の `Resources` / `StreamingAssets` / Addressables のどれで読むかを先に決め、JSON 側には `stillId` と画像パスの対応を持たせる。
- 通常背景画像は `Assets/Images/Background/` に置く。ファイル名は `Background_Morning_Sunny.png`、`Background_Noon_Rainy.png`、`Background_Night_Snow.png` のように `Background_時間帯_天候` で統一する。
- 背景は `BackgroundSpriteData` で時間帯と天候に対応する Sprite を管理する。`GameManager.backgroundSpriteData` に割り当てると、`RefreshUI()` 時に現在の時間帯と天候から背景が切り替わる。未設定の組み合わせは従来の `dayBackgroundSprite` / `nightBackgroundSprite` にフォールバックする。
- スチル回想は、イベント既読の `shownGameEventIds` とは別に `SaveData.unlockedStillIds` で保存する。`GameEventPageData.stillId` を追加済みで、スチル表示時に解放済みへ登録する
- 回想 UI は `StillGalleryPanel` で制御する。`StillGalleryAction` から開き、解放済みスチルは押せるボタン、未解放スチルは `???` の無効ボタンとして一覧表示する。初期 UI は Unity 上に手作業配置済み
- スチル回想は後でページングを追加する。`itemsPerPage`、前へ/次へボタン、ページ表示 Text を持たせ、固定件数でページを切り替える案を優先する
- 回想項目のサムネイル Image は、スチル数が増えて一覧の視認性が問題になってから検討する。当面は Text ボタンだけでよい
- `StatusDetailPanel` の画面部品は Unity 上で手作業配置し、Inspector で参照を割り当てる
- 必須参照は `panelRoot`、各 `TextMeshProUGUI`、各 `Button`、`abilityListParent`、`abilityButtonPrefab`、`abilityAcquirePanel` 周辺
- `GameManager.EnsureStatusDetailPanel()` は配置済みの `StatusDetailPanel` を探して初期化するだけで、UI の自動生成は行わない
- 会話や行動のたびに時間が進み、一定数で日付が進む
- 好感度が `100` に達すると `Ending` ボタンが表示される
- 翌朝開始時など複数メッセージが連続発生する場合は、話者付きメッセージキューに積み、`Next` で 1 件ずつ表示する
- `DialogueClickAdvanceArea` を割り当てたメッセージウィンドウは、`Next` ボタンが押せる状態ならクリックでも進行できる
- エンディングは `EndingScene` と `EndingData` で管理する。初期データは `Assets/Resources/Heroines/DefaultHeroine/Endings/GoodEnding.asset`

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
- [`Assets/Scripts/Core/EndingManager.cs`](../Assets/Scripts/Core/EndingManager.cs): エンディングシーンの表示とタイトル復帰
- [`Assets/Scripts/Core/EndingData.cs`](../Assets/Scripts/Core/EndingData.cs): エンディングデータの ScriptableObject 定義
- [`Assets/Scripts/Core/EndingSelectionSettings.cs`](../Assets/Scripts/Core/EndingSelectionSettings.cs): MainScene から EndingScene へ選択エンディングIDを渡す静的設定
- [`Assets/Scripts/Core/BackgroundZoom.cs`](../Assets/Scripts/Core/BackgroundZoom.cs): 背景ズーム演出
- [`Assets/Scripts/Core/BackgroundSpriteData.cs`](../Assets/Scripts/Core/BackgroundSpriteData.cs): 時間帯・天候ごとの背景 Sprite 設定
- [`Assets/Scripts/Core/HeroineProfileData.cs`](../Assets/Scripts/Core/HeroineProfileData.cs): ヒロイン単位の読み込みパスと代表画像設定
- [`Assets/Scripts/Action/`](../Assets/Scripts/Action): 行動データ型の定義
- [`Assets/Scripts/Conversation/`](../Assets/Scripts/Conversation): 会話データ型の定義
- [`Assets/Scripts/Schedule/`](../Assets/Scripts/Schedule): 予定管理と予定パネル制御
- [`Assets/Scripts/Schedule/ScheduledEventData.cs`](../Assets/Scripts/Schedule/ScheduledEventData.cs): 予定イベントの ScriptableObject 定義
- [`Assets/Scripts/Schedule/ScheduledEventDefinition.cs`](../Assets/Scripts/Schedule/ScheduledEventDefinition.cs): 実行時に使う予定イベント定義
- [`Assets/Resources/Heroines/<HeroineId>/ScheduledEvents/`](../Assets/Resources/Heroines): ヒロイン別の予定イベントデータの実体。該当 `ScheduleType` がない場合は [`Assets/Resources/ScheduledEvents/`](../Assets/Resources/ScheduledEvents) を共通フォールバックとして使う
- [`Assets/Resources/Heroines/DefaultHeroine/Actions/`](../Assets/Resources/Heroines/DefaultHeroine/Actions): 現在ヒロインの行動データの実体
- [`Assets/Resources/Heroines/DefaultHeroine/Actions/ScheduleAction.asset`](../Assets/Resources/Heroines/DefaultHeroine/Actions/ScheduleAction.asset): 予定パネルを開く行動アセット
- [`Assets/Resources/Heroines/DefaultHeroine/Endings/`](../Assets/Resources/Heroines/DefaultHeroine/Endings): 現在ヒロインのエンディングデータの実体
- [`Assets/Resources/Heroines/`](../Assets/Resources/Heroines): ヒロインプロフィールデータ
- [`Assets/Resources/Heroines/DefaultHeroine/GameEvents/`](../Assets/Resources/Heroines/DefaultHeroine/GameEvents): 現在ヒロインのゲーム開始、日開始、手動確認用イベントデータ
- [`Assets/Resources/Backgrounds/`](../Assets/Resources/Backgrounds): 背景切り替え用データ
- [`Assets/Resources/Heroines/DefaultHeroine/Conversations/`](../Assets/Resources/Heroines/DefaultHeroine/Conversations): 現在ヒロインの会話データ
- [`Assets/Scenes/MainScene.unity`](../Assets/Scenes/MainScene.unity): メインシーン
- [`Assets/Scenes/EndingScene.unity`](../Assets/Scenes/EndingScene.unity): エンディングシーン
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
- `Assets/Resources/Heroines/DefaultHeroine/Actions/`: 現在ヒロインの行動資産
- `Assets/Resources/Heroines/DefaultHeroine/Conversations/`: 現在ヒロインの会話資産
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
- `stillId` / `stillSprite` を反応ごとに持たせられる
- 同じ `priority` の候補が複数ある場合はランダムで 1 件を選ぶ

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

`OnClickEnding()` は `EndingScene` へ遷移します。
`EndingScene` には `EndingManager` を配置し、`titleButton` と `endingText` を Inspector で割り当てます。
`EndingManager` は `HeroineProfileData.endingResourcePath` のパスから `EndingData` を読み込み、選択された `endingId` の `message` と `stillSprite` を表示します。
初期データとして `GoodEnding.asset` を用意しています。スチル画像ができたら `GoodEnding.asset.stillSprite` に割り当てます。
`TitleButton` で `TitleScene` に戻します。
初期実装は単一エンディングです。分岐が必要になった段階でエンディング条件や表示内容を `EndingData` のような ScriptableObject に切り出します。

### 10. 衣装評価

`OutfitManager` と `OutfitPreferenceManager` が衣装の着用と反応を管理します。

- 衣装を着ると、その `outfitId` の着用回数が増える
- 衣装反応パネルで `褒める` / `嫌う` / `退屈` を選ぶと、衣装ごとの評価値と回数が更新される
- `着替える` を選ぶと衣装選択に戻る
- 衣装評価は `SaveData.outfitPreferences` に保存・復元される
- 今後、衣装を変更したときのヒロインメッセージと、`衣装を見る` 実行後のヒロイン反応メッセージに `expressionId` を持たせ、`HeroineLayeredSpriteView` の表情を切り替えられるようにする

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
- `dialogueClickAdvanceArea`
- `enableDialogueWindowClickAdvance`

### Ending

- `endingButton`
- `endingSceneName`
- `defaultEndingId`

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
- エンディング表示は `EndingData` 化済みだが、現在の選択処理は `GameManager.defaultEndingId` の固定選択
- エンディング分岐条件の自動選択は未実装。必要になったら `HeroineProfileData.endingResourcePath` から条件一致する `EndingData` を選ぶ
- エンディング到達済みの永続フラグや回想はまだない
- セーブスロット UI は prefab 化済みで、`TitleScene` と `MainScene` に配置済み
- 話者ラベルは `SYSTEM` / `予定` / `衣装` / ヒロイン名に分けている
- 話者タイプごとに `speakerNameText` と `dialogueText` の色を変え、ヒロイン発話・システム通知・予定通知・衣装通知を見分けやすくしている
- ただしヒロイン発話とシステム通知は同じメッセージボックスに出るため、今後はシステム通知専用パネルなどの追加分離を検討する
- 翌朝開始時のメッセージは話者付きキューで順番に表示する
- 汎用ログ画面は `MessageLogPanel` で実装済み。`GameManager.SetDialogueText()` で表示した会話、行動結果、予定、衣装通知を直近 20 件までセッション内ログとして保持し、セーブデータには入れない
- ログ画面は `MessageLogAction` から開く。UI は手作業配置した `MessageLogPanel` / `MessageLogList` / `MessageLogRowPrefab` を使う
- ログ画面をスクロール表示にする場合、Unity 上の階層は `MessageLogPanel > Scroll View > Viewport > MessageLogList` を基本にし、`ScrollRect.content` に `MessageLogList` の RectTransform を割り当てる。`MessageLogList` の中身は実行時に `MessageLogRowPrefab` から生成するため、編集時は空でよい
- Unity Editor で UI を手作業変更した後は、Codex にシーン編集を依頼する前に必ず `Ctrl+S` で `MainScene.unity` を保存する。未保存の `Scroll View` / `Viewport` などはディスク上の `MainScene.unity` に存在しないため、Codex 側の scene patch と食い違って消えたように見えることがある
- 衣装確認モードの解放条件は、`GameManager.playerOutfitPromptAbilities` と `HeroineStatus.OutfitPromptAbilities` の組み合わせで管理する

## 変更しやすいポイント

### ヒロインを追加する

新しいヒロインを追加する場合は、`HeroineProfileData` とヒロイン別 Resources フォルダをセットで作る。
`Images/Background` は共通背景として残し、立ち絵・イベントスチル・行動スチル・エンディングスチルはヒロイン別素材として扱う。
差し替え確認用に `Assets/Resources/Heroines/TestHeroineProfile.asset` を追加済み。
`GameManager.heroineProfile` に割り当てると、最小データでヒロイン別読み込みを確認できる。
`HeroineProfileData.defaultHeroineSprite` は通常衣装 `Normal` の立ち絵として使う。
通常衣装以外は衣装側の `heroineSprite` を優先し、衣装画像がない場合だけ profile の代表立ち絵へフォールバックする。
AssetTool の `assets_export.json` を importer で取り込むと、`Assets/Resources/Heroines/<HeroineId>/HeroineAssetCatalog.asset` に `assetId` と Sprite 参照の対応が保存される。
Importer は完了時に copied images、catalog assets、layers、conversations、game events、warning 件数を summary 表示する。
`sprite_layers_export.json` がある場合は、`HeroineLayeredSpriteData.asset` に `BaseBody` / `Costume` / `Expression` / `Accessory` の各レイヤーを import する。
レイヤーの実表示を行う `HeroineLayeredSpriteView` は実装済み。
`OutfitManager` が現在衣装を `costumeId` として渡し、会話表示時は `ConversationData.lines[].expressionId` を表情切り替えに使う。
指定表情がない場合は `Neutral`、指定衣装がない場合は `Default` へ fallback する。
`game_events_export.json` がある場合は、`GameEvents/<EventId>.asset` を作成、更新し、`lines[]` を `GameEventData.pages` に変換する。
イベントページの `expressionId` も会話と同じ表情切り替えに使う。
追加の `TestHeroine` 画像は容量節約のためコミットしない。
画像が必要な場合は `FantasyLoveSimAssetTool` の export サンプルから importer で取り込み、ローカル確認用として扱う。

チェックリスト:

- `HeroineProfileData` を作成する
- `heroineId` と `displayName` を設定する
- `conversationResourcePath` / `gameEventResourcePath` / `actionResourcePath` / `endingResourcePath` を設定する
- `defaultHeroineSprite` に代表立ち絵を設定する
- `HeroineAssetCatalog.asset` に画像の `assetId` と Sprite 参照が入っているか確認する
- `Actions` に行動データと行動反応を用意する
- `Conversations` にジャンル会話と条件付き会話を用意する
- `GameEvents` に `GameStart` / `DayStart` / `Manual` イベントを用意する
- `Endings` に `defaultEndingId` と一致する `EndingData` を用意する
- 立ち絵は `Assets/Images/Heroines/<HeroineId>/Sprites/` に置く
- イベントスチルは `Assets/Images/Heroines/<HeroineId>/Event/` に置き、`GameEventData` に割り当てる
- 行動スチルは `Assets/Images/Heroines/<HeroineId>/Actions/` に置き、`ActionData` または `ActionReactionData` に割り当てる
- エンディングスチルは `Assets/Images/Heroines/<HeroineId>/Ending/` に置き、`EndingData` に割り当てる
- `MainScene` の `GameManager.heroineProfile` を切り替えて読み込み確認する
- Unity Editor の `FantasyLoveSim > Validate Heroine Data` を実行し、ResourcePath、ID 重複、別ヒロイン画像参照、衣装セリフ override の重複 warning を確認する

### 行動を増やす

`ActionData` アセットを `Assets/Resources/Heroines/DefaultHeroine/Actions/` など、対象ヒロインの `actionResourcePath` 配下に追加すればよいです。
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

予定イベントを増やす場合は、対象ヒロインの `HeroineProfileData.scheduledEventResourcePath` 配下に `ScheduledEventData` アセットを追加します。
ヒロイン別データがない `ScheduleType` は、共通 `Assets/Resources/ScheduledEvents/` のデータで補完されます。
同じ `ScheduleType` のアセットがある場合、`GameManager` はそのデータを優先し、アセットがない場合だけコード内の既定定義へフォールバックします。
予定イベント本文の話者は `ScheduledEventData.eventSpeakerType` で指定でき、`Heroine` / `System` / `Schedule` / `Outfit` から選べます。

今後の大きな拡張として、`DuoShopping` など二人で買い物へ行く予定では、アイテムや衣服を購入できる専用フローを追加する。
買い物で購入した衣服は `OutfitData` の解放状態と連動させ、購入アイテムはイベント条件や将来の戦闘、ステータスに使えるようにする。
買い物以外のお出かけは、現在のスチル表示中心のイベントから、RPG でよくある探索・戦闘要素を持つ行動へ拡張する予定。
そのため、プレイヤーとヒロインの状態には HP、攻撃、防御、素早さ、所持金、装備または衣装補正などの戦闘用パラメータを追加し、`StatusDetailPanel` に表示できるようにする。

### 会話を増やす

新規 import では、対象ヒロインの `Assets/Resources/Heroines/<HeroineId>/Conversations/<ConversationId>.asset` に会話を個別保存します。
旧 `Conversations.asset` container も互換として読めますが、重複を避けるため profile の `conversationResourcePath` は `Heroines/<HeroineId>/Conversations` を指します。
会話IDは `Daily_Morning_01` のように `カテゴリ_条件_連番` を基本にし、`showOnce` は一度見せたら再出現させたくない会話だけに使う。
`minAffection` / `maxAffection` は会話の入口条件、`allowedTimeSlots` / `allowedWeathers` / `allowedSeasons` は必要なときだけ絞り込む。条件が重なる会話が複数ある場合は `priority` で並び替える。
同じ条件で複数の会話を置く場合は、まず `priority`、次に `conversationId` の命名で識別できるようにする。

### エンディングを増やす

エンディング内容は `EndingData` アセットで管理します。
新しいエンディングを追加する場合は、Unity の Project ウィンドウで `Create > LoveSim > Ending Data` を選び、対象ヒロインの `endingResourcePath` 配下に保存します。
ファイル名と `endingId` は一致させます。例: `GoodEnding.asset` / `GoodEnding`。

設定する項目:

- `endingId`: エンディング選択に使う一意ID。本番投入後は変更しない
- `displayName`: Unity 上で見分けるための表示名
- `message`: `EndingScene` の `EndingText` に表示する本文
- `stillSprite`: エンディングスチル。画像が未完成なら空でよい
- `requiredAffection`: 将来の分岐判定用。現状は `GoodEnding` の `100` を基準にする
- `requiredShownEventIds`: 将来の分岐判定用。特定イベントを見た場合だけ出すエンディングに使う

現状の `GameManager` は `defaultEndingId = GoodEnding` を選んで `EndingScene` に渡します。
分岐を増やす段階では、`GameManager` に `HeroineProfileData.endingResourcePath` を読み込んで条件一致する `EndingData` を選ぶ処理を追加します。
複数条件に一致する可能性が出たら、`EndingData` に `priority` を追加して優先順位を決めます。

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
- 現在の複数メッセージ進行は `Next` ボタンとメッセージ表示ウィンドウクリックに対応済み。選択肢や Save/Load、ログ、ステータス画面などの UI 操作と競合しないよう、クリック進行はメッセージ表示ウィンドウに限定する。
- メッセージ表示ウィンドウのクリック進行は `DialogueClickAdvanceArea` で実装済み。Unity 上でメッセージウィンドウの Panel などに追加し、`GameManager.dialogueClickAdvanceArea` に割り当てる。クリック対象の Image は `Raycast Target` を有効にしておく。
- クリック進行は好みが分かれるため、オプション画面を追加し、ON/OFF を切り替えられるようにする候補として扱う。

## 追加開発の優先候補

1. 行動データの反応パターン追加
2. 会話データの分類ルール整理
3. エンディング分岐条件の自動選択
4. 立ち絵切り替えと表情差分の整理
5. スチル回想のページング
6. セーブ/ロードの強化
7. メッセージ表示ウィンドウのクリック進行 ON/OFF オプション
8. UI の見た目調整

## デバッグ時の確認項目

- `GameManager` がシーン上のオブジェクトにアタッチされているか
- すべての `SerializeField` が割り当て済みか
- `choiceButtonArea` の初期状態が想定どおりか
- `nextButton` が割り当て済みか
- `actionButtonParent` と `actionButtonPrefab` が割り当て済みか
- `scheduleManager` と `schedulePanel` が割り当て済みか
- `GameManager.heroineProfile` または `defaultHeroineProfileResourcePath` が正しく設定されているか
- `DefaultHeroineProfile.asset` の `actionResourcePath` / `conversationResourcePath` / `gameEventResourcePath` / `endingResourcePath` が `Heroines/DefaultHeroine/...` を指しているか
- `Assets/Scenes/MainScene.unity` を開いているか
- Unity バージョンが `2021.3.45f1` か

## 備考

- 背景演出やアクション反応は拡張しやすいので、必要になった時点でデータを追加するのがよい
