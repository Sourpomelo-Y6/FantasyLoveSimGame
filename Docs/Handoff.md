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
- 通常行動と行動反応は `playerHpChange` / `heroineHpChange` を持ち、実行時に HP を増減できる。現在の `休む` はプレイヤーとヒロインの HP を 20 回復する
- 衣装には着用後の反応を付けられ、`褒める` / `嫌う` / `退屈` / `着替える` の選択肢で評価を更新できる
- 衣装評価の履歴はセーブデータに保存される
- 予定の状態はセーブデータに保存される
- 予定の保存と復元は確認済み
- 予定が行動制限、会話候補、衣装自動選択に影響する
- 予定を翌日の具体イベントに変換する案2は、準備フェーズ付きで実装済み
- 翌朝は今日の予定と着替え可能な準備メッセージを表示し、予定イベント本体は指定された時間帯に発動する
- 予定イベント本体の直前は、衣装確認モードに応じて `このまま出発` / `着替える` を出し分ける
- 衣装確認モードは `Always` / `Conditional` / `Hidden` を想定しており、`Conditional` のときは今の衣装が予定に対して問題ない場合に確認を省略する
- 衣装確認モードはプレイヤー側の便利機能として扱い、可否と現在の使用モードは `GameManager.playerOutfitPromptAbilities` で制御する
- プレイヤーとヒロインそれぞれに詳細ステータス画面を用意し、その中に能力項目と能力獲得画面への導線を置く方針
- 能力はステータス画面から確認し、必要に応じて獲得画面へ移動して解放する
- 現状の能力は基本的に `取得` = `解放` として扱う。ただし衣装確認モードは、解放済みの `Conditional` / `Hidden` を「使用する」ことで現在モードに設定し、選択中のモードを「解除する」と `Always` に戻せる
- 将来、任意でオンオフできる能力がさらに必要になった場合は `Locked` / `Unlocked` / `Active` のような状態分離を検討する
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
- タイトル画面にはキャラクター選択 UI を追加済み。`Resources.LoadAll<HeroineProfileData>("Heroines")` で候補を列挙し、選択中プロフィールの表示名と立ち絵をプレビューして、決定後に新規ゲーム用の選択ヒロインとして `GameStartSettings` へ渡す。ロード時はセーブデータ内のヒロイン ID を優先する
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
- スチル回想のページングは `StillGalleryPanel` に実装済み。`itemsPerPage`、前へ/次へボタン、ページ表示 Text を持ち、固定件数でページを切り替える。前へ/次へボタンとページ表示 Text は未割り当てでも従来の一覧表示として動く
- 回想項目のサムネイル Image は、スチル数が増えて一覧の視認性が問題になってから検討する。当面は Text ボタンだけでよい
- `StatusDetailPanel` の画面部品は Unity 上で手作業配置し、Inspector で参照を割り当てる
- 必須参照は `panelRoot`、各 `TextMeshProUGUI`、各 `Button`、`abilityListParent`、`abilityButtonPrefab`、`abilityAcquirePanel` 周辺
- `GameManager.EnsureStatusDetailPanel()` は配置済みの `StatusDetailPanel` を探して初期化するだけで、UI の自動生成は行わない
- メイン画面の主人公・ヒロイン HP はテキスト表示を優先する。HP バー化や残量に応じた色変更は後回し
- 会話や行動のたびに時間が進み、一定数で日付が進む
- 好感度が `1000` に達すると `Ending` ボタンが表示される。最大値は `9999` で、入口解放値とは分離されている
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
- `playerHpChange` / `heroineHpChange` でプレイヤーとヒロインの HP 増減を指定できる。実際に増減した値は行動結果メッセージに追記される
- 同じ `priority` の候補が複数ある場合はランダムで 1 件を選ぶ

### 7. 時間経過

`AdvanceTime()` によって `timeIndex` が進みます。

- 時間は 4 段階
- 最終段階に到達すると日付が 1 日進む
- `timeNames` 配列の内容を UI に出している

### 8. エンディング解放

`ClampAffection()` 内で好感度をチェックしています。好感度は従来値を10倍した整数尺度です。

- `affection < 0` の場合は `0` に丸める
- `affection > 9999` の場合は `9999` に丸める
- `endingUnlockAffection` の `1000` に到達すると `Ending` ボタンを表示する
- ランク境界は `200` / `400` / `600` / `800` / `1000`

### 9. Ending ボタン

`OnClickEnding()` は `EndingScene` へ遷移します。
`EndingScene` には `EndingManager` を配置し、`titleButton` と `endingText` を Inspector で割り当てます。
`EndingManager` は `HeroineProfileData.endingResourcePath` のパスから `EndingData` を読み込み、選択された `endingId` の `message` と `stillSprite` を表示します。
初期データとして `GoodEnding.asset` を用意しています。スチル画像ができたら `GoodEnding.asset.stillSprite` に割り当てます。
`TitleButton` で `TitleScene` に戻します。
`GameManager` は `HeroineProfileData.endingResourcePath` から `EndingData` を読み込み、現在の好感度、衣装条件、表示済みイベント条件に合うものを選びます。
複数の `EndingData` が条件に一致する場合は、`requiredAffection` が高いものを優先します。
条件に一致する `EndingData` がない場合は、`defaultEndingId` を使います。

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
- セーブデータとヒロインは連動済み。`SaveData.heroineId` / `heroineDisplayName` に保存時のヒロインを記録し、ロード時はセーブデータ側の `heroineId` を優先して `HeroineProfileData`、会話、行動、イベント、予定データを切り替える
- セーブ画面サムネイルは実装済み。`SaveData.thumbnailFileName` にスロット別 PNG 名を保存し、画像本体は git 管理外の `Application.persistentDataPath` に置く。`SaveLoadPanel.OpenSave()` 時点で、セーブ画面を開く直前の画面を縮小キャプチャし、保存時にスロット別サムネイルとして書き出す
- タイトル画面にはギャラリーモードを追加する。キャラクター選択から対象ヒロインを選び、そのヒロインの解放済みスチル一覧へ移動して表示する

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
- エンディング表示は `EndingData` 化済みで、`GameManager` が `HeroineProfileData.endingResourcePath` から条件一致する `EndingData` を選ぶ
- エンディング分岐条件は `requiredAffection`、`costumeId`、`requiredShownEventIds` を使う。複数一致時は `requiredAffection` が高いものを優先する
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
- 衣装確認モードの解放条件と現在の使用モードは、プレイヤー側の便利機能として `GameManager.playerOutfitPromptAbilities` で管理する

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

`DuoShopping` など二人で買い物へ行く予定では、アイテムや衣服を購入できる専用フローを追加済み。
買い物で購入した衣服は `OutfitData` の解放状態と連動し、購入済み ID と解放済み衣装 ID はセーブデータに保存される。
買い物以外のお出かけは、スチル表示中心のイベントから、探索・戦闘要素を持つ行動へ拡張中。
共通基盤として、プレイヤーとヒロインの HP、攻撃、防御、素早さ、所持金は `StatusDetailPanel` に表示し、`SaveData` でセーブ/ロード復元できる。
所持金の増減テスト、`DuoShopping` 予定からの簡易買い物、衣装またはアイテム購入、簡易探索、簡易戦闘は実装済み。
`BattlePanel` は通常メニューの `DebugBattleAction` と、森・洞窟・湖の探索予定の両方で使う共通戦闘画面である。`GameManager` は探索予定では導入メッセージの後に対応する `EnemyData` を渡して開き、勝敗・HP/MP・報酬を予定結果へ反映する。
`BattlePanel` は Unity 上で手動配置し、`GameManager.battlePanel` に参照を割り当てる。
`DebugBattleAction` は固定敵 `ForestSlime` を使う確認用入口である。戦闘計算はコピーから開始するが、終了時には現在実装では主人公・ヒロインの HP/MP を実ステータスへ反映する。デバッグ入口でこの反映を行わない仕様へ変更する場合は、予定戦闘かどうかを明示するフラグを追加して分離する。
UI の必須参照は `panelRoot`、敵名、敵HP、プレイヤーHP、ヒロインHP、戦闘ログ Text、攻撃ボタン、逃げるボタン、閉じるボタン。
初期状態では `panelRoot` を非アクティブにしておく。`BattlePanel.Awake()` では初回表示を打ち消さないように panel root を閉じない。
将来的に、主人公とヒロインが互いに戦う模擬戦闘を追加する。模擬戦闘は探索戦闘とは別枠にし、訓練、関係性イベント、スキル習得、戦闘バランス確認に使う。`BattlePanel` の流用ではなく訓練専用画面として作り、ヒロインと一対一で訓練メニューをこなす構成にする。表示されるビジュアルはヒロインの一枚絵を基本とし、訓練メニュー、結果ログ、必要なら主人公側の簡易ステータスを併置する。内部計算は戦闘ステータスのコピーを使い、終了後に本来の HP を直接減らさない方針にする。訓練開始時は複数の訓練メニューから選択し、進行ごとに主人公とヒロインの訓練用 HP が減る。途中でやめることもできる。HP が 0 になった対象に LP が残っていれば LP を 1 消費して HP 全回復で続行し、どちらかの HP と LP が両方 0 になったら終了する。同時に両者の HP が 0 になった場合はボーナスを付ける。LP は本戦闘では使わない訓練専用リソースとして扱う。
訓練メニュー定義は `TrainingData` ScriptableObject、訓練中の一時状態は `TrainingSessionState` で扱う。`TrainingData` は `trainingId`、表示名、説明、ステップごとの HP 減少、初期 LP、報酬、同時 0 ボーナスを持つ。`TrainingSessionState` は訓練用 HP、LP、経過ステップ数、同時 0 回数、中断フラグ、終了フラグを持ち、`AdvanceStep()` で HP 減少、LP 消費、同時 0 カウント、終了判定を行う。今後 `TrainingData.maxSteps` を追加し、最初に選択した訓練の値をセッション全体の上限として固定する。途中の訓練切り替えでは上限をリセット・延長しない。`0` 以下は無制限、正の上限到達は通常完了として各ステップ報酬、完了報酬、スキルポイントを付与する。HP / LP 終了と同時に上限へ達した場合も、そのステップの LP 消費、同時 0、報酬を反映してから結果を一度だけ確定する。UI には現在値と最大ステップ数を表示する。
`TrainingPanel` は訓練専用画面の接続用スクリプトとして用意済み。`Open(IReadOnlyList<TrainingData>, BattleStatusData, BattleStatusData)` で訓練一覧と主人公/ヒロインの戦闘ステータスを受け取り、訓練選択、1 ステップ進行、中断、閉じる、HP/LP/ログ更新を行う。UI の必要参照は `panelRoot`、`heroineImage`、`trainingListParent`、`trainingButtonPrefab`、各 HP/LP Text、`resultLogText`、`advanceButton`、`quitButton`、`closeButton`。好感度報酬反映と訓練熟練度保存は実装済みで、Scene 配置の細かい見た目調整は未実装。
初期確認用の訓練データは `Assets/Resources/Training` に `LightPractice`、`SparringPractice`、`EnduranceTraining` として追加済み。
`GameManager.OpenTrainingPanel()`、`ActionExecutionType.OpenTrainingPanel`、ヒロイン別 `TrainingAction` は追加済み。`GameManager` は `Resources.LoadAll<TrainingData>("Training")` で訓練データを読み、主人公/ヒロインの `BattleStatusData` を `TrainingPanel.Open(...)` に渡す。`TrainingPanel.Close()` は `GameManager.OnTrainingPanelClosed()` に戻り通知する。
訓練終了結果は `TrainingResult` で扱う。`TrainingPanel` は HP/LP 終了時、途中終了時、進行中に閉じた時に一度だけ `GameManager.OnTrainingPanelResult(...)` へ結果を通知する。進行ボタンで有効な 1 ステップを進めるたびに `TrainingData.affectionRewardPerStep` と `trainingProficiencyRewardPerStep` を累積し、初期訓練はどちらもすべて `1`。中断してもステップ分の好感度と熟練度は反映する。完了かつ非中断の場合だけ `TrainingData.affectionReward`、同時 0 ボーナス、倍率変更しない `trainingProficiencyReward` を完了報酬として追加する。熟練度の完了ボーナスは軽い稽古 `1`、実戦形式 `2`、持久訓練 `3`。途中で訓練を切り替えた場合も、ステップ熟練度は実際に進めた訓練 ID へ個別加算する。結果は `ShowSystemMessage(...)` で画面に表示し、メッセージログにも残す。1 ステップ以上進めた訓練は、完了/中断に関わらず時間を 1 段階進める。訓練熟練度は `SaveData.trainingProficiencies` に保存し、訓練ごとの上限は `999999`。熟練度と実績はスキルツリーの取得条件として使い、訓練結果からスキルを自動解放しない。`TrainingPanel` は訓練ボタンと選択中タイトルに現在の熟練度を表示する。
訓練用スキルは各1個を開始前に選択せず、取得済みかつ有効な主人公・ヒロインのスキルをすべて各ステップへ適用する。有効状態は所有者別に保存し、同一スキル ID の重複適用を防ぐ。固定値補正を合計した後、元の HP 消費が1以上なら軽減後も最低1、元が0なら0を維持する。好感度・熟練度補正は最低0。複数の軽減スキルによって HP / LP 終了条件が無効にならないようにし、ログには個々の発動行を並べず補正合計を表示する。
スキル取得は、訓練などで得るスキルポイントをスキルツリーで消費する。熟練度や実績条件はツリーノードの購入可否を決め、条件達成だけでは自動取得させない。累計訓練回数、主人公の LP 消費発生回数、訓練相手の LP 消費発生回数、モンスター撃破数は `SaveData` に永続化済み。訓練は 1 ステップ以上進めて結果確定した場合に中断を含め 1 回、LP は HP 0 後に実際に LP を消費した側へ 1 回、撃破は勝利と報酬が確定した敵だけを加算する。同時 LP 消費は両者へ各 1 回加算し、訓練相手、逃走、敗北、未確定のデバッグ結果はモンスター撃破数に含めない。
保存構造は `SkillProgressStats` のような全体集計を基本とし、訓練 ID ごとにも訓練回数、主人公 LP 消費回数、相手 LP 消費回数を記録する。`TrainingData` には安定した `trainingCategoryId` または `TrainingCategory` を追加し、攻撃、防御、持久、連携などのカテゴリー単位でも同じ 3 種類を累計する。訓練結果確定時は、全体、選択中訓練 ID、所属カテゴリーの集計を同時に更新する。将来は敵 ID、モンスター分類 ID ごとの撃破カウンターも追加できる形にする。
スキルツリー側は `SkillData` と取得条件を分離した `SkillTreeNodeData` のようなデータを用意し、必要ポイント、前提ノード、熟練度、訓練回数、双方の LP 消費回数、撃破数を基本 AND 条件で評価する。訓練条件の集計範囲は「全訓練」「特定の訓練 ID」「特定のカテゴリー ID」から選べるようにする。UI には対象訓練またはカテゴリー名と、各条件の現在値・必要値を表示する。旧セーブに存在しないカウンターとカテゴリー集計は 0 として扱う。
状態確認 UI でも訓練・戦闘実績を確認できるよう、`StatusDetailPanel` に「実績」タブまたは実績詳細パネルを将来追加する。基本能力欄へ全件を常時表示せず、全体集計を先に表示し、訓練カテゴリー別、訓練 ID 別、敵別の内訳を折りたたみまたは切り替えで表示する。累計訓練回数、双方の LP 消費回数、モンスター撃破数を `SkillProgressStats` から読み、訓練・戦闘結果確定後とロード後に更新する。未記録値は 0 として表示する。
実績集計基盤は実装済み。`TrainingData.trainingCategoryId` と `SaveData.skillProgressStats` を追加し、全体・訓練 ID 別・カテゴリー別の訓練回数と双方の LP 消費回数、全体・敵 ID 別の撃破数を保存・復元する。訓練メニューを途中で切り替えても、LP 消費は実際に進めた訓練 ID とカテゴリーへ記録される。全体訓練回数はセッションごと、個別訓練とカテゴリーはセッション内で 1 ステップ以上実行した対象ごとに 1 回加算する。初期カテゴリーは `Fundamentals`、`Combat`、`Endurance`。撃破は予定戦闘の勝利だけを数え、デバッグ戦闘、逃走、敗北は除外する。表示側は `GameManager.GetSkillProgressStats()` でコピーを取得でき、確認用に `[SkillProgress]` ログを出す。
`StatusProgressPanel` は実装済みで、全体、カテゴリー別、訓練別、敵別の表示を切り替える。`StatusDetailPanel.progressButton` から開き、`progressPanel` は状態詳細の子に配置した場合は自動検出可能。`MainScene` にはパネルルート、タイトル Text、スクロール可能な本文 Text、全体・カテゴリー・訓練・敵 Button、閉じる Button を配置し、`StatusProgressPanel` の各参照と `StatusDetailPanel.progressButton` / `progressPanel` を割り当て済み。
スキルポイント基盤は実装済み。主人公とヒロインのポイントは `SaveData.playerSkillPoints` / `heroineSkillPoints` に別々に保存する。完了かつ非中断の訓練だけが `TrainingData.playerSkillPointReward` / `heroineSkillPointReward` を付与し、軽い稽古は双方 1、実戦形式と持久訓練は双方 2。訓練結果メッセージと `StatusProgressPanel` の全体表示に獲得量・現在値を表示する。参照は `GameManager.PlayerSkillPoints` / `HeroineSkillPoints`、安全な消費は `TrySpendPlayerSkillPoints(...)` / `TrySpendHeroineSkillPoints(...)` を使う。消費成功時は状態詳細表示も更新する。旧セーブは 0。`EnemyData.playerSkillPointReward` / `heroineSkillPointReward` は予定探索の勝利時だけ付与し、敗北、逃走、デバッグ戦闘は除外する。森スライムと湖の精霊は双方1、洞窟コウモリは双方2。`BattlePanel` とフォールバック簡易戦闘の両方で加算し、結果メッセージとログに獲得量を表示する。
`SkillTreeNodeData` と条件評価基盤は実装済み。ノードは固定 ID、所有者、主人公用 `SkillData`、ヒロイン用 `targetHeroineId` / `grantedHeroineSkillId`、ポイントコスト、前提ノード、条件一覧、`treePosition` を持つ。条件種類は訓練熟練度、訓練回数、双方の LP 消費回数、モンスター撃破数、好感度、日数。集計範囲は全体、訓練 ID、訓練カテゴリー ID、敵 ID に対応し、全条件を AND で評価する。`GameManager.GetSkillTreeNodes()`、`EvaluateSkillTreeNode(...)`、`TryAcquireSkillTreeNode(...)` を使用する。評価結果は `Locked` / `Available` / `InsufficientPoints` / `Acquired`、現在値・必要値、未取得前提ノードを含む。
取得済みノードは主人公・ヒロイン別に `SaveData.acquiredPlayerSkillTreeNodeIds` / `acquiredHeroineSkillTreeNodeIds` へ保存する。主人公ノード取得時だけ対応スキルを使用可能にし、ヒロインノードは主人公スキルへ混入させない。起動時とロード時は取得済み主人公ノードから `unlockedSkillIds` を再構築するため、旧来のスキル ID だけでは使用可能にならない。初期主人公ツリーは `PowerStrike` から `BattleFocus`、`GuardStance` から `FirstAid` / `ArmorBreak` へ分岐する。上位ノードは熟練度に加えて、総モンスター撃破数、実戦カテゴリーの相手 LP 消費回数、主人公 LP 消費回数を条件に使う。`SkillTreePanel` と通常メニューからの取得導線は実装済みで、条件達成による自動解放は廃止済み。ヒロインノードは現在プロフィールと一致するものだけを表示し、取得済みの `grantedHeroineSkillId` だけを `BattlePanel` の自動スキル候補にする。DefaultHeroine は `RadiantSlash` から回復・防御へ、TestHeroine は `SharpThrust` から回復・防御へ分岐し、未取得時は通常攻撃を行う。
`SkillTreePanel` は各ノードの `treePosition` を使って二次元配置し、前提ノード間の接続線を実行時に生成する。Content の表示サイズを座標範囲から自動計算し、必要な方向だけスクロールを有効にする。ノード状態は灰色・黄色・緑・青、接続線は接続先ノードの状態に合わせて色分けし、選択中ボタンへ白い Outline を付ける。詳細欄には取得可否、不足理由、取得結果を表示する。訓練・カテゴリー・敵の条件対象 ID は表示名へ解決し、ヒロインタブでは現在ヒロイン名と所持ポイントを表示する。接続線用 Prefab を含む追加の Scene UI 部品は不要。
`SkillTreeDataValidator` はノード ID、前提関係、循環参照、所有者・対象ヒロイン、主人公・ヒロインスキル、条件対象 ID、条件重複、座標重複を検証する。Unity Editor の `FantasyLoveSim > Validate Skill Tree Data` で全ノードを検証でき、Editor Play と Development Build の開始時にも一度実行する。警告は `[SkillTreeValidation]` で始まるため Console で絞り込み可能。通常リリースビルドでは起動時検証を呼ばない。
好感度尺度は整数 `0〜9999`。既存の値・増減・条件は10倍へ移行し、従来の100相当は1000、ランク境界は200、400、600、800、1000とする。`HeroineStatus.maxAffection = 9999` と `endingUnlockAffection = 1000` を分離済み。旧 `maxAffection = 100` は通常コンテンツの終端でもあったため、移行後の会話・行動の既定上限は9999にして1000以降も利用可能にする。1000以降は単純な累積値として扱える。現在の `SaveData.saveVersion` は15で、旧好感度・熟練度尺度とのセーブ互換性は保証しない。
スキルシステムは `StatusAbilityData` とは分け、`SkillData` 系 ScriptableObject として拡張する。スキルは汎用スキル、戦闘用スキル、訓練用スキルに分類する。汎用スキルは探索、会話、買い物、イベント条件、ステータス補正などに使い、戦闘用スキルは攻撃、防御、回復、バフ、デバフなど `BattlePanel` のコマンドに使う。訓練用スキルは模擬戦闘や稽古、熟練度上げに使う。
`SkillData`、`SkillCategory`、`SkillEffectType`、`SkillTargetType` は追加済み。スキルデータには `skillId`、表示名、カテゴリ、説明、消費コスト、対象、効果種別、威力または回復量、解放条件、使用可能な戦闘種別を持たせる。使用可能スキル ID は `SaveData.unlockedSkillIds` に派生情報として保存するが、取得済み主人公ノードを正本として起動・ロード時に再構築する。参照には `GameManager.IsSkillUnlocked(...)` / `GetUnlockedSkillIds()` を使い、外部から直接解放しない。主人公の装備中戦闘スキルは順序付きの `SaveData.equippedPlayerBattleSkillIds` に最大4件保存する。ヒロインの編成中戦闘スキルは `SaveData.heroineBattleSkillLoadouts` にヒロイン ID ごとの順序付きリストとして最大3件保存する。敵側は `EnemyData.battleSkills` の `EnemyBattleSkillData` を使い、敵ごとのスキル、MP コスト、対象、使用確率、優先度、戦闘中の最大使用回数を設定する。敵 MP も戦闘開始時に全回復し、MP が足りる候補から priority の高いスキルを確率で選ぶ。将来必要になったら `skillProficiencies` のような保存領域を追加する。
初期確認用のスキルデータは `Assets/Resources/Skills` に `PowerStrike`、`GuardStance`、`FirstAid` として追加済み。熟練度尺度の10倍化後の解放条件は順に `LightPractice` 30、`EnduranceTraining` 30、`SparringPractice` 50。`BattleFocus` は `LightPractice` 50、`ArmorBreak` は `EnduranceTraining` 50とする。
通常メニューのスキル導線は `SkillTreePanel` を開く。取得済みの主人公戦闘ノードを選ぶと既存の取得ボタンが「装備する」または「外す」に変わり、詳細欄とノード表示にも装備状態と使用中枠数を表示する。ヒロインノードでは同じボタンを「編成する」または「外す」として使い、現在ヒロインの3枠を編集する。取得時は空きがあれば自動編成する。旧セーブのバージョン14以前は取得済みスキルを最大3件まで補完し、バージョン15以降は空編成も維持する。ロード時は存在しない・未取得・重複・4件目以降のヒロインスキルを除去する。戦闘中は専用の `BattleSkillPanel` を優先して開き、装備中の最大4スキルだけを表示する。未配置の場合だけ従来の `SkillPanel` をフォールバックとして使う。`PowerStrike`（Damage）、`GuardStance`（Guard）、`FirstAid`（Heal）、`BattleFocus`（プレイヤー攻撃 Buff）、`ArmorBreak`（敵防御 Debuff）を実行できる。MP は戦闘開始時に最大値まで回復し、各スキルの `cost` を消費する。Buff / Debuff は `statusDurationTurns` の対象ターン数だけ `affectedStat` を変化させ、期限が切れると戦闘ログへ解除を出す。敵 Speed がプレイヤー Speed より 4 以上高い場合は 30% で追加行動する。ヒロイン用スキルは `HeroineProfileData.battleSkills` でヒロイン別に定義する。MP、確率、優先度、最大使用回数を持ち、編成中の候補から自動選択され、使用可能な候補がなければ通常攻撃する。初期設定は DefaultHeroine / TestHeroine ともに攻撃、HP が減った味方への回復、主人公への防御 Buff の 3 種類である。`BattlePanel` は主人公・ヒロイン・敵の HP と MP を常時表示し、`StatusButton` から `BattleStatusEffectPanel` を開く。状態パネルでは対象ごとに攻撃/防御/素早さの増減値と残りターンを一覧表示し、Buff と Debuff を色分けする。主人公・ヒロインの編成操作に追加の Scene UI は不要。カテゴリタブと MP 回復アイテムは後回しにする。
基礎ステータスは実装済み。共通の `BattleStatusData`、プレイヤー用の `PlayerStatus`、ヒロイン側の `HeroineStatus.BattleStatus` を使う。
所持金は案Aとして `PlayerStatus` だけが持つ。`SaveData` は `playerBattleStatus`、`playerMoney`、`heroineBattleStatus` を保存し、ロード時に復元する。
`StatusDetailPanel` はプレイヤー詳細に HP、攻撃、防御、素早さ、所持金を表示し、ヒロイン詳細に HP、攻撃、防御、素早さを表示する。
所持金を増減するテスト処理は実装済み。`GameManager.debugAddMoneyKey` は F8、`debugSpendMoneyKey` は F9、`debugMoneyAmount` は 100 がデフォルト。
`DuoShopping` 予定からの簡易買い物イベントも実装済み。`DuoShopping` 実行時に専用 `ShopPanel` を開き、`GameManager.duoShoppingShopCatalog` の商品を一覧表示する。
商品を選ぶとその商品の価格を所持金から消費し、結果を予定イベント本文に追記する。`ShopPanel` を閉じた場合は予定を消費しない。
`ShopPanel` の商品ボタンは `商品名 / 価格G / 解放: 衣装ID` を表示する。購入済み商品は `購入済み`、所持金不足の商品は `所持金不足` を表示し、どちらもボタンを押せない。表示順は未購入商品を上、購入済み商品を下に並べる。
`ShopItemData` には購入条件として `requiredAffection`、`requiredDay`、`requiredPurchasedItemIds` を持たせている。条件未達の商品は `条件未達` を表示し、ボタンを押せない。現在の季節衣装商品は条件なしで、各条件フィールドは 0 または空リストにしている。
`ShopPanel` は他のパネルと同じく自動生成せず、Canvas 直下に手動配置して `GameManager.shopPanel` に割り当てる。必要な UI は `ShopPanel` ルート、`TitleText`、`EmptyText`、`ShopItemList`、`ShopItemButtonPrefab`、`CloseButton`。
カタログが未設定または空の場合は `GameManager.duoShoppingShopItem`、それも未設定の場合は従来の固定テスト値へフォールバックする。
季節衣装の商品は `Assets/Resources/ShopItems/SpringOutfitItem.asset`、`SummerOutfitItem.asset`、`AutumnOutfitItem.asset`、`WinterOutfitItem.asset` の `ShopItemData` で定義済み。各商品は価格 100 で、対応する `Spring` / `Summer` / `Autumn` / `Winter` の衣装を 1 つずつ解放する。
購入済み ID は `SaveData.purchasedItemIds` に保存し、`StatusDetailPanel` のプレイヤー詳細に表示する。
`DuoShoppingCatalog.asset` には季節衣装の商品 4 件を登録済み。既存の `ShoppingTestItem_01` は互換用の単体テスト商品として残している。
商品購入時に解放された衣装 ID は `SaveData.unlockedOutfitIds` に保存し、`OutfitManager` の着用可否判定に渡す。
春夏秋冬の衣装アセットは手作業で `isUnlockedByDefault=false` に変更済み。購入解放された衣装は好感度条件を無視して着用できる。
未購入の衣装は好感度不足ではなく未所持として扱う。購入前の春夏秋冬など `isUnlockedByDefault=false` かつ `unlockedOutfitIds` に含まれない衣装は、DressUp の衣装ボタンを表示しない方針にする。
`lockedMessage` は好感度やイベント条件など、存在は見えているが条件不足で着られない場合に限定する。
ショップ UI を充実させる場合は、単純なボタン一覧に説明文を詰め込むのではなく、商品一覧と詳細表示を分ける。商品ボタンを選択すると説明用テキストボックスに用途や雰囲気、解放衣装、購入条件を表示し、別の購入ボタンで確定する構成を検討する。商品数が増えた段階で `ShopItemList` は Scroll View 化し、衣装、消耗品、イベント用アイテムなどのカテゴリ分けやフィルタも追加する。UI 部品の追加と Inspector 参照設定が必要になるため、現時点では後回しにする。
次に進める場合は、商品ごとの価格差、購入条件、ショップの在庫表示を検討する。

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
- `requiredAffection`: エンディング選択条件。複数条件に一致する場合は高い値のものを優先する
- `costumeId`: 現在衣装が一致する場合だけ選択候補にする
- `requiredShownEventIds`: 特定イベントを見た場合だけ出すエンディングに使う

`GameManager` は `HeroineProfileData.endingResourcePath` を読み込み、条件一致する `EndingData` を選んで `EndingScene` に渡します。
条件に一致する `EndingData` がない場合は `defaultEndingId` を使います。
現状の優先順位は `requiredAffection` が高いものを優先する方式です。

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
- セーブサムネイルを表示する場合は、各スロットにサムネイル Image を追加し、`SaveLoadPanel` の `Slot Thumbnail Images` にスロット順で割り当てる

`TitleScene` 側の接続:

- `Title Manager` にシーン上の `TitleManager` を割り当てる
- `Game Manager` は空のままでよい
- `ContinueButton` から `SaveLoadPanel.OpenLoad()` を呼ぶ
- 将来のギャラリーモードでは、タイトル画面に `GalleryButton` とキャラクター選択 UI を追加し、選択したヒロインの解放済みスチルを表示する画面へ遷移する
- キャラクター選択 UI を使う場合は、`TitleManager` の `Heroine Select Button`、`Heroine Select Panel`、`Previous Heroine Button`、`Next Heroine Button`、`Confirm Heroine Button`、`Close Heroine Select Button`、`Heroine Preview Image`、`Heroine Name Text`、`Heroine Info Text` を Inspector で割り当てる。未選択時に優先するキャラクターは `Default Heroine Id` で指定する
- `TitleManager` は `Resources.LoadAll<HeroineProfileData>("Heroines")` で候補を列挙し、決定した `heroineId` を `GameStartSettings.SelectedHeroineId` に渡す。`NewGameButton` で開始した場合のみ `GameManager` がこの選択を使う
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
- スキル取得状態がロード後に戻る
- 衣装確認モードの解放状態と現在の使用モードがロード後に戻る
- 表示列 `ActionData.displayColumn` はアセット設定なので、セーブ/ロードで変化しない
- 翌朝メッセージキューと将来の汎用ログはセーブ対象外の方針なので、ロード後に復元されなくてよい
- タイトル画面からロードした場合も、MainScene のロード後 UI が閉じた状態で始まる
- 現在の複数メッセージ進行は `Next` ボタンとメッセージ表示ウィンドウクリックに対応済み。選択肢や Save/Load、ログ、ステータス画面などの UI 操作と競合しないよう、クリック進行はメッセージ表示ウィンドウに限定する。
- メッセージ表示ウィンドウのクリック進行は `DialogueClickAdvanceArea` で実装済み。Unity 上でメッセージウィンドウの Panel などに追加し、`GameManager.dialogueClickAdvanceArea` に割り当てる。クリック対象の Image は `Raycast Target` を有効にしておく。
- クリック進行は好みが分かれるため、オプション画面を追加し、ON/OFF を切り替えられるようにする候補として扱う。

## 追加開発の優先候補

1. 行動データの反応パターン追加
2. 会話データの分類ルール整理
3. エンディングデータと条件分岐パターンの追加
4. 立ち絵切り替えと表情差分の整理
5. セーブ/ロードの強化
6. メッセージ表示ウィンドウのクリック進行 ON/OFF オプション
7. UI の見た目調整

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
