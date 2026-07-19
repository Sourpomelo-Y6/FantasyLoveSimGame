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
- 行動反応は `ActionData.reactions` に入れ、`priority` が高い候補を優先して 1 件選ぶ。条件は好感度・時間帯・天候・季節・衣装・表示済みイベント・取得済みスキルで切り替えられ、`showOnce` と反応専用の `stillSprite` も持てる。主要行動には priority 0 の無条件フォールバックを残す
- 通常行動と行動反応は `playerHpChange` / `heroineHpChange` を持ち、実行時に HP を増減できる。現在の `休む` はプレイヤーとヒロインの HP を 20 回復する
- 衣装には着用後の反応を付けられ、`褒める` / `嫌う` / `退屈` / `着替える` の選択肢で評価を更新できる
- 衣装評価の履歴はセーブデータに保存される
- 予定の状態はセーブデータに保存される
- 予定の保存と復元は確認済み
- 予定が行動制限、会話候補、衣装自動選択に影響する
- 予定を翌日の具体イベントに変換する案2は、準備フェーズ付きで実装済み
- 翌朝は今日の予定と着替え可能な準備メッセージを表示し、予定イベント本体は指定された時間帯に発動する
- 予定イベント本体の直前は、衣装確認モードに応じて `このまま出発` / `着替える` を出し分ける
- 予定画面は将来、今日・明日だけでなく7日／30日のカレンダー表示へ変更する。実行前キャンセルと、`Application.persistentDataPath` に保存する複数の名前付きテンプレートを追加し、テンプレートだけを別セーブスロットから共有する。詳細は `Docs/ScheduleUiExpansionPlan.md` を参照する
- 衣装確認モードは `Always` / `Conditional` / `Hidden` を想定しており、`Conditional` のときは今の衣装が予定に対して問題ない場合に確認を省略する
- 衣装確認モードの利用可否は取得済み主人公スキルツリーノードから導出し、現在モードだけを `GameManager.playerOutfitPromptAbilities` に保持する
- タイトルから新規ゲームを開始した直後に、メイン画面へ入る前のゲーム開始イベントを挟み、スチル表示もここで行う方針
- タイトル画面とゲーム開始イベント中は `SaveLoadPanel` を閉じた状態に保ち、`Save` / `Load` ボタンを表示しない
- 詳細ステータス画面は `StatusDetailPanel`、画面の対象切り替えは `StatusDetailRole` で扱う
- 状態画面の独自アビリティ取得は廃止し、衣装確認モードの解放を主人公スキルツリーへ統合する方針。状態画面は閲覧専用、`StatusProgressPanel` は実績集計専用として整理する。段階移行と削除対象は `Docs/StatusAndAchievementUiReorganizationPlan.md` を参照する
- 衣装確認モードのスキルツリー統合は実装済み。主人公ノード `Player_ConditionalOutfitPrompt` / `Player_HiddenOutfitPrompt` で解放し、取得済みノードのボタンから使用／解除する。旧解放フラグの移行確認は完了し、旧移行コードは完全削除工程で撤去した
- 旧 `StatusAbilityData`、関連Resourcesアセット、`unlockedStatusAbilityIds`、ヒロイン側の旧能力保存、Scene上の旧取得UIは削除済み。`SaveData` はversion 19で、状態画面は読み取り専用
- 詳細ステータス画面の入口として `StatusDetailAction` を用意し、行動一覧から開けるようにしている
- タイトルから新規ゲームを開始した直後は、`GameEventData` の `GameStart` イベントを再生してからメイン画面を始める。`GameEventData` はヒロイン別 Resources パスに置き、ページ単位で話者・メッセージ・スチルを持てる
- ヒロイン差し替えは `HeroineProfileData` で管理する。画像、会話、イベント、行動反応、エンディング、朝夜の挨拶などの共通セリフをヒロイン単位で束ね、`Images/Background` は共通背景として扱う。現在は `DefaultHeroineProfile.asset` で `Heroines/DefaultHeroine/Actions` / `Conversations` / `GameEvents` / `Endings` を参照している
- タイトル画面にはキャラクター選択 UI を追加済み。`Resources.LoadAll<HeroineProfileData>("Heroines")` で候補を列挙し、選択中プロフィールの表示名と立ち絵をプレビューして、決定後に新規ゲーム用の選択ヒロインとして `GameStartSettings` へ渡す。ロード時はセーブデータ内のヒロイン ID を優先する
- `HeroineProfileData` の共通セリフ、衣装メッセージ、Resources path、ヒロイン戦闘スキルは `FantasyLoveSimAssetTool` のプロフィール画面で編集でき、`heroine_profile_export.json` と Unity の `heroine_profile_from_unity.json` の往復同期に対応済み。旧JSONで省略された戦闘スキルは既存値を維持し、明示された空配列だけを削除として扱う
- ヒロイン固有の訓練スキルとスキルツリーノードはAssetToolのプロフィール画面で編集し、`heroine_skills_export.json` / `heroine_skills_from_unity.json` で往復できる。Unity Importerはヒロイン別フォルダだけを更新し、主人公ノード、他ヒロイン、共通 `TrainingData` を変更しない。前提ノード、解放訓練、実績条件、ツリー座標も同期対象
- ヒロイン別の戦闘後イベントと戦闘パネル結果メッセージはAssetToolの「戦闘メッセージ」タブで編集し、`battle_result_events_export.json` / `battle_panel_result_messages_export.json` と対応するFromUnity JSONで往復できる。結果種別、`battleContextId`、本文、`stillId`、好感度、解放衣装を保持し、Unity Importerはプロフィールで指定されたヒロイン別Resources pathだけを更新する
- AssetToolの戦闘メッセージ結果種別は候補選択式で、スチル・衣装IDも登録済み候補を参照できる。Export時に空値、未知の結果種別、重複条件、未登録参照を検証し、Unity Import結果には戦闘メッセージの追加・更新・削除・スキップ件数を表示する
- 戦闘後イベントは話者種別、任意話者名、表情IDを持ち、既存の話者・表情付きメッセージキューで表示する。話者名が空なら種別に応じた既定名を使い、表情IDが空なら現在の表情を維持する。AssetToolでは話者と登録済み表情を候補選択でき、JSON往復にも対応する
- 戦闘後イベントの表示方式は `Auto / StillOnly / StillWithPortrait / PortraitOnly`。`Auto` はカタログから専用スチルを解決できれば立ち絵を隠し、解決できなければ探索画像の上へ半透明の黒い暗幕と立ち絵を表示する。暗幕は参照未設定時に立ち絵の背面へ実行時生成される。TestHeroineの同行勝利は探索画像を残す `StillWithPortrait` を使用する
- AssetToolの `Unity Profile読込` は、同じフォルダにある戦闘結果・戦闘パネル文JSONの追加、更新、削除、維持件数と、話者・表情・表示方式の変更件数を戦闘メッセージタブへ表示する。画面下の `戦闘メッセージへ` で直接移動できる
- AssetToolの `制作状況` タブは、現在選択中のヒロインについて基本情報、戦闘メッセージ、訓練画像、訓練セリフ、キャラクター画像、会話、イベント、行動反応、表情、衣装、戦闘スキル、スキルツリー、Export準備を既存データから `○ / △ / × / ―` で都度集計する。訓練セリフは登録済み訓練ごとの5状態について枠・重複・本文を確認し、詳細クリックで対応Assetとセリフ候補を選択する。キャラクター画像は登録済み・参照中・基本立ち絵・標準戦闘枠を必須としてAcceptedと実ファイルを確認し、未参照候補は `―` にする。行動反応は主要5行動、ID、本文、条件、優先度、重複、各参照を確認し、詳細から `ActionReactions` の該当行へ移動する。イベントとExport前検査も個別に確認でき、未完成のみ表示と再集計に対応する
- 通常会話の分類ルールは `Docs/ConversationClassificationRules.md` を正本とする。categoryはUnityの `ConversationGenre` に合わせて `Daily / Food / Adventure / Love` の4値を使い、各ジャンルへpriority 0・条件なし・反復可能なフォールバックを最低1件置く。IDは `Conv_<Genre>_<Context>_<NN>` とし、本番公開後は変更しない
- 制作状況の詳細行は対象IDを保持し、イベント、Asset、戦闘・訓練スキル、ツリーノード、表情、衣装、レイヤーの該当一覧行まで選択する。戦闘スキルとスキルツリーの編集欄は移動時に自動展開する
- AssetToolのExport前検証は制作状況と実Exportで共通化され、`Error / Warning / Information` と修正対象を返す。Accepted画像の実ファイル、prompt、会話・イベント条件と各参照、戦闘メッセージ参照を読み取り専用で確認する。Error時はExport前に確認し、キャンセルすると制作状況へ移動する
- 差し替え確認用として `TestHeroineProfile.asset` と `Heroines/TestHeroine/...` の最小データを追加済み。`GameManager.heroineProfile` に割り当てると、ヒロイン別読み込みの手動確認に使える
- devでのTestHeroine切り替えは、Scene上の`GameManager.heroineProfile`を書き換えず、`Tools > FantasyLoveSim > Development Heroine Override`を使用する。設定は端末ローカルのEditorPrefsへ保存され、Gitやmainブランチへ混入しない。新規ゲーム時だけ指定プロフィールを優先し、ロード時はセーブデータのヒロインを優先する
- 別リポジトリまたは別フォルダで Stable Diffusion 向けキャラクター素材生成ツールを作る方針。仕様は `Docs/CharacterAssetGenerationToolSpec.md` に整理済み
- Unity Editor で `MainScene` を直接開いて再生した場合は、`GameStartSettings.ShouldPlayGameStartEvent` の初期値が `false` のため開始イベントは発生しない
- `GameEventData.showOnce` はセーブデータの `shownGameEventIds` で管理する
- `GameEventData` の `DayStart` は翌朝メッセージに混ぜて自動再生し、`Manual` は `GameManager.TryStartManualGameEvent(string eventId)` から明示起動する
- `GameManager` にはデバッグ用に `F7` で `debugManualGameEventId` を呼ぶ入口を用意してある
- テスト用の手動イベントとして `TestManualEvent` を用意している。`GameManager.debugManualGameEventId` に `TestManualEvent` を設定すると `F7` で繰り返し再生できる
- イベントIDは `GameStartIntro`、`DayStart_条件_連番`、`Manual_用途_連番`、`Story_章_連番`、`Still_用途_連番` のように用途が分かる名前にする。`eventId` は既読管理に使うため、本番投入後は変更しない
- `GameEventData` には `minDay` / `maxDay` / `minAffection` / `maxAffection` / `requiredShownEventIds` / `blockedShownEventIds` を追加済み。発生可否は `GameManager.CanStartGameEvent(GameEventData gameEvent)` に集約し、日開始イベントと手動イベントの両方で同じ条件判定を使う
- `GameEventData` は衣装条件も持てる。`requiredOutfitIds` / `blockedOutfitIds` の文字列ID指定に加えて、Unity Inspector で `OutfitData` アセットを選べる `requiredOutfits` / `blockedOutfits` を追加済み。判定は現在の `OutfitManager.CurrentOutfit.outfitId` に対して行う
- `GameEventData.requiredSkillIds` に指定した主人公スキルをすべて取得済みの場合だけイベントを開始できる。取得状態は取得済み主人公ノードから再構築されるため、イベント用のセーブ項目は持たない。存在しない・重複・空のスキル ID は `FantasyLoveSim > Validate Game Event Data` で検出でき、Editor Play / Development Build の起動時にも全ヒロインのイベントを検証する
- 確認用の汎用スキル「気配り」は、訓練を1回完了して訓練回数と SP を獲得した後、1 SPで主人公ノード `Player_Consideration` から取得する。TestHeroine使用時はスキルツリーを閉じると `Manual_Consideration_01` が一度だけ自動開始する。`SkillTreeNodeData.unlockEventId` と `unlockEventHeroineId` で接続し、取得済み・未表示状態からロード後も発生待ちを復元する。F7のデバッグ起動も利用できる
- `FantasyLoveSim > Validate Skill Tree Data` は取得時イベントが対象ヒロインのイベントパスに存在し、Manual・Once・有効状態であることを確認する。イベント必須スキルが対象ノードまたは前提ノードの取得で保証されない場合も警告する。AssetToolの制作状況では取得時イベントIDとOnceを事前確認できる
- ヒロイン固有スキル／ノードの正規配置は `Resources/Skills/Heroines/<HeroineId>/` と `Resources/SkillTreeNodes/Heroines/<HeroineId>/`。IDはResources全体で一意になるよう `<HeroineId>_<用途>` とし、TestHeroineの訓練スキル3件は名前空間付きIDへ移行済み。旧ルート配置のTestHeroineノード7件は削除し、DefaultHeroine参照中の共通スキルは残している
- AssetToolの制作状況・Export前検査とUnityの `HeroineSkillTreeAssetSync` は、ヒロイン固有SkillId／NodeIdの `<HeroineId>_` 接頭辞を検査する。Unity Import前にはResources全体の同一IDとアセットパスも確認し、別アセットとの衝突時はパスをConsoleへ出して処理を中止する。自動改名や既存アセット上書きはしない
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

- Unity `2021.3.45f2`
- URP 2D
- TextMeshPro

### Unity Editorバージョン更新

- 2026年7月17日にUnity Editorを`2021.3.45f1`から`2021.3.45f2`へ更新した
- 正式なプロジェクトバージョンは`ProjectSettings/ProjectVersion.txt`の`2021.3.45f2 (88f88f591b2e)`とする
- CloneまたはPull後はUnity Hubから同じEditorバージョンを指定して開く
- Editorバージョンを変更した場合は、`ProjectSettings/ProjectVersion.txt`も関連変更としてGitへコミットする
- バージョン更新後はスクリプトの再コンパイルとEditMode Testを確認する。直近のテスト構成は20件なので、`2021.3.45f2`で初回起動した環境でも全20件の成功を確認する

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
- `Assets/Fonts/Local/`: 各開発環境で用意する日本語フォントと生成TMP Font Asset。Git管理しない
- `Assets/Settings/`: URP 関連設定
- `Assets/TextMesh Pro/`: TextMeshPro 標準アセット群

### TextMeshPro日本語フォント

日本語フォント本体と生成TMP Font Assetはリポジトリに含めない。新しい環境では`Tools > TextMeshPro > Japanese Font Setup`を開き、`Assets/Fonts/Local`へ配置した`.ttf`または`.otf`からFont Assetを生成して、`Assets/Resources/JapaneseFontSettings.asset`へ設定する。詳細手順は[`Docs/JapaneseFontSetup.md`](JapaneseFontSetup.md)を参照する。

Runtimeでは`JapaneseFontApplier`が起動時に自動生成され、設定済みの場合だけ非アクティブを含むロード済みSceneの`TMP_Text`へ適用する。各CanvasやGameObjectへの手動追加は不要。新規TMP向けのDefault Font Assetは`Edit > Project Settings > TextMesh Pro`から手動設定する。

EditorWindowのScene・Prefab一括適用はローカル確認専用。適用後のScene・Prefabと、ローカルFont Assetを割り当てた`JapaneseFontSettings.asset`には非Git管理アセットのGUIDが入るため、その差分をコミットしない。

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
- `requiredShownEventIds` は表示済みイベント、`requiredSkillIds` は取得済みスキルをすべて満たす場合だけ候補になる
- `showOnce` は `reactionId` を通常会話と共通の表示履歴へ保存するため、会話IDと重複しないIDを使う
- 条件反応を増やしても、priority 0・一度限りOFF・好感度0～9999・その他条件なしの反応をフォールバックとして残す

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
訓練メニュー定義は `TrainingData` ScriptableObject、訓練中の一時状態は `TrainingSessionState` で扱う。`TrainingData` は `trainingId`、表示名、説明、ステップごとの HP 減少、初期 LP、報酬、同時 0 ボーナス、`maxSteps` を持つ。`TrainingSessionState` は訓練用 HP、LP、経過ステップ数、固定した最大ステップ数、同時 0 回数、中断フラグ、終了フラグ、`TrainingEndReason` を持ち、`AdvanceStep()` で HP 減少、LP 消費、同時 0 カウント、終了判定を行う。最大ステップは最初に選択した訓練からセッション開始時に固定し、途中の訓練切り替えではリセット・延長しない。`0` 以下は無制限、正の上限到達は通常完了として各ステップ報酬、完了報酬、スキルポイントを付与する。HP / LP 終了と同時に上限へ達した場合は HP / LP 終了を優先し、そのステップの LP 消費、同時 0、報酬を反映してから結果を一度だけ確定する。初期3訓練はすべて最大20ステップ。UI は `StepCountText` があれば専用表示し、なければ訓練名欄へ表示する。
`TrainingPanel` は訓練専用画面の接続用スクリプトとして用意済み。`Open(IReadOnlyList<TrainingData>, BattleStatusData, BattleStatusData)` で訓練一覧と主人公/ヒロインの戦闘ステータスを受け取り、訓練選択、1 ステップ進行、中断、閉じる、HP/LP/ログ更新を行う。UI の必要参照は `panelRoot`、`heroineImage`、`trainingListParent`、`trainingButtonPrefab`、各 HP/LP Text、`resultLogText`、`advanceButton`、`quitButton`、`closeButton`。好感度報酬反映と訓練熟練度保存は実装済みで、Scene 配置の細かい見た目調整は未実装。
訓練画像の自動切替を実装済みで、設計を `Docs/Extra_FantasyLoveSimAssetTool/TrainingImagePlan.md` に記録している。訓練選択時は現在の訓練画面で `elapsedSteps == 0` なら開始前、1以上なら進行後の画像を使う。ステップごとのLP差分から主人公のみ、ヒロインのみ、双方同時の3状態を判定し、同時状態を優先する。画像はヒロイン別 `HeroineTrainingImageData` に分離し、既存 `heroineImage` を使い、未設定時は現在画像を維持する。初期素材は3訓練それぞれに開始前、進行後、主人公LP消費、ヒロインLP消費、同時LP消費を用意する標準15枚とする。旧共通LP画像は互換用フォールバックとして利用できる。
訓練中のヒロインセリフは実装済み。画像と同じ `trainingId + visualState` をキーにして訓練ごとに変え、標準15画像枠の各組み合わせへ最低1件ずつ設定できる。各枠には複数候補を登録でき、直前と同じセリフを避けて選択する。セリフデータは画像参照から分離した `HeroineTrainingDialogueData` で管理し、訓練進行時に一度決定した `TrainingVisualState` を画像とセリフへ共有する。TestHeroineには標準15セリフを作成済み。UIは `HeroineNameText` と `TrainingMessageText` を結果ログとは別に配置し、同名子GameObjectの自動検出またはInspector参照を利用する。
AssetToolの訓練画像タブから各枠のセリフ候補を編集し、`training_dialogues_export.json` へ出力できる。Unityの `HeroineAssetImporter` はこのJSONが存在する場合だけ `HeroineTrainingDialogueData.asset` を生成・更新し、旧ExportでJSONがない場合は既存セリフを維持する。
UnityからToolへ戻す経路も実装済み。`FantasyLoveSim/Export Heroine Unity Data` は `training_dialogues_from_unity.json` を生成し、AssetToolの訓練画像タブにある `Unity訓練セリフ読込` で取り込める。同じ `trainingId + visualState` の既存枠を維持し、未登録のセリフ候補だけを追加する。ヒロインID、schemaVersion、表示状態を検証し、旧表示状態名は現行名へ正規化する。
AssetTool側は `usage = Training`、`Images/Training/`、`training_images_export.json` を追加し、Unity側の `requiredSkillIds`、共通セリフ、`battleSkills`、ヒロイン別スキル・ノードなども段階的に往復対応する。同期範囲、旧JSONで新規フィールドを消さない更新規則、実装順は `Docs/Extra_FantasyLoveSimAssetTool/CurrentFeatureSyncPlan.md` を参照する。
スキルツリーノード取得による訓練メニュー解放は実装済み。`TrainingData.unlockedByDefault` が初期利用可否、`SkillTreeNodeData.unlockedTrainingIds` が取得時に解放する訓練IDを持つ。戦闘・訓練スキルを持たない解放専用ノードも取得でき、解放状態は新しいセーブ項目へ二重保存せず既存の取得済みノードIDから導出する。ヒロインノードは `targetHeroineId` と現在ヒロインが一致する場合だけ有効。初期3訓練は常時利用可能を維持する。
`TrainingPanel` は現在ヒロインに解放経路がある未解放訓練を無効ボタンとして表示し、解放ノード名も併記する。解放経路自体がないヒロインにはその訓練を表示しない。スキルツリー詳細には解放する訓練名を表示する。Validatorは存在しない・空・重複した訓練ID、初期解放済み訓練の冗長な指定、自身が初めて解放する訓練の実績を要求する直接的な到達不能条件を警告する。確認用としてTestHeroineの「連携演習の心得」ノードと、未解放訓練 `CooperativeDrill`（連携演習）を追加した。
追加訓練のAssetTool対応も実装済み。UnityのFromUnity Exportは `training_catalog_from_unity.json` に現在ヒロインで制作対象となる訓練ID、表示名、カテゴリー、初期解放状態、解放ノードを出力する。Toolの訓練画像タブで `Unity訓練一覧読込` を実行し、`登録済み訓練の不足枠を準備` を押すと、固定3訓練ではなくカタログ内の各訓練×5状態を不足分だけ作成する。既存15枠は維持され、TestHeroineでは `CooperativeDrill` の5枠だけを追加できる。旧プロフィールはカタログが空なら初期3訓練を補完する。
初期確認用の訓練データは `Assets/Resources/Training` に `LightPractice`、`SparringPractice`、`EnduranceTraining` として追加済み。
限定対象の確認用スキルとして、主人公「実戦感覚」は `Combat` カテゴリーだけで熟練度を1増加し、「集中訓練」の取得、実戦カテゴリー10回、2 SPを条件にする。ヒロイン「持久の支え」は `EnduranceTraining` だけでヒロインHP消費を1軽減し、「応援」の取得、持久訓練10回、2 SPを条件にして DefaultHeroine / TestHeroine の両ツリーへ追加する。
訓練終了結果は、実際に軽減した双方のHP消費、スキルによる好感度・熟練度の増減、効果を発揮したスキル名をセッション全体で集計して表示する。訓練切替時も実適用ステップだけを含める。会話欄とメッセージログには同じ5行以内の要約を渡し、長い訓練名・発動スキル名は省略して行数超過を防ぐ。`TrainingPanel` のログは TextMesh Pro の折り返し後の実表示行数で `maxLogLines` を守り、古い行を削除したうえで `maxVisibleLines` / `Truncate` により領域外表示を防ぐ。
`GameManager.OpenTrainingPanel()`、`ActionExecutionType.OpenTrainingPanel`、ヒロイン別 `TrainingAction` は追加済み。`GameManager` は `Resources.LoadAll<TrainingData>("Training")` で訓練データを読み、主人公/ヒロインの `BattleStatusData` を `TrainingPanel.Open(...)` に渡す。`TrainingPanel.Close()` は `GameManager.OnTrainingPanelClosed()` に戻り通知する。
訓練終了結果は `TrainingResult` で扱う。`TrainingPanel` は HP/LP 終了時、途中終了時、進行中に閉じた時に一度だけ `GameManager.OnTrainingPanelResult(...)` へ結果を通知する。進行ボタンで有効な 1 ステップを進めるたびに `TrainingData.affectionRewardPerStep` と `trainingProficiencyRewardPerStep` を累積し、初期訓練はどちらもすべて `1`。中断してもステップ分の好感度と熟練度は反映する。完了かつ非中断の場合だけ `TrainingData.affectionReward`、同時 0 ボーナス、倍率変更しない `trainingProficiencyReward` を完了報酬として追加する。熟練度の完了ボーナスは軽い稽古 `1`、実戦形式 `2`、持久訓練 `3`。途中で訓練を切り替えた場合も、ステップ熟練度は実際に進めた訓練 ID へ個別加算する。結果は `ShowSystemMessage(...)` で画面に表示し、メッセージログにも残す。1 ステップ以上進めた訓練は、完了/中断に関わらず時間を 1 段階進める。訓練熟練度は `SaveData.trainingProficiencies` に保存し、訓練ごとの上限は `999999`。熟練度と実績はスキルツリーの取得条件として使い、訓練結果からスキルを自動解放しない。`TrainingPanel` は訓練ボタンと選択中タイトルに現在の熟練度を表示する。
訓練用スキルは各1個を開始前に選択せず、取得済みかつ有効な主人公・ヒロインのスキルをすべて各ステップへ適用する。有効状態は所有者別に保存し、同一スキル ID の重複適用を防ぐ。固定値補正を合計した後、元の HP 消費が1以上なら軽減後も最低1、元が0なら0を維持する。好感度・熟練度補正は最低0。複数の軽減スキルによって HP / LP 終了条件が無効にならないようにし、ログには個々の発動行を並べず補正合計を表示する。
訓練スキルの有効状態管理とステップ効果は実装済み。主人公は `SaveData.activePlayerTrainingSkillIds`、ヒロインは `SaveData.heroineTrainingSkillActivations` にヒロイン ID 別で保存する。取得済み訓練ノードでは `SkillTreePanel` の既存ボタンを「有効にする／無効にする」として使い、有効数に上限はない。ロード時は存在しない・未取得・`SkillData` 側で無効なスキル ID を除去する。セーブバージョン15以前は取得済み訓練スキルを自動的に有効化し、16以降は空または無効の選択も維持する。`SkillData` の訓練用HP消費軽減・好感度補正・熟練度補正を訓練開始時に合計し、同一スキル ID は所有者をまたいでも一度だけ適用する。適用範囲は `trainingApplicationScope` で全訓練・カテゴリー・訓練IDから選び、対象指定時は `trainingApplicationTargetId` を使う。訓練選択・切替時に対象スキルだけで再集計し、対象外スキルは有効スキル表示にも含めない。`SkillTreeDataValidator` は適用対象IDも検証する。`TrainingPanel` は訓練選択時に有効スキル名、補正後HP消費、好感度・熟練度の予定値を既存ログへ表示する。プレビューとステップ実処理は同じ計算を使い、ステップ後は個別のスキル名ではなく実際に効いた補正合計を表示し、熟練度集計には補正後の値を記録する。初期ノードは主人公「深呼吸」、DefaultHeroine / TestHeroine「呼吸合わせ」で、総訓練回数1回と1 SPを条件にし、それぞれ主人公・ヒロインのHP消費を1軽減する。派生ノードは主人公「集中訓練」が熟練度を1、各ヒロインの「応援」が好感度を1増加し、それぞれ初期ノードの取得、累計訓練5回、2 SPを条件にする。既存4スキルは全訓練対象。
スキル取得は、訓練などで得るスキルポイントをスキルツリーで消費する。熟練度や実績条件はツリーノードの購入可否を決め、条件達成だけでは自動取得させない。累計訓練回数、主人公の LP 消費発生回数、訓練相手の LP 消費発生回数、モンスター撃破数は `SaveData` に永続化済み。訓練は 1 ステップ以上進めて結果確定した場合に中断を含め 1 回、LP は HP 0 後に実際に LP を消費した側へ 1 回、撃破は勝利と報酬が確定した敵だけを加算する。同時 LP 消費は両者へ各 1 回加算し、訓練相手、逃走、敗北、未確定のデバッグ結果はモンスター撃破数に含めない。
保存構造は `SkillProgressStats` のような全体集計を基本とし、訓練 ID ごとにも訓練回数、主人公 LP 消費回数、相手 LP 消費回数を記録する。`TrainingData` には安定した `trainingCategoryId` または `TrainingCategory` を追加し、攻撃、防御、持久、連携などのカテゴリー単位でも同じ 3 種類を累計する。訓練結果確定時は、全体、選択中訓練 ID、所属カテゴリーの集計を同時に更新する。将来は敵 ID、モンスター分類 ID ごとの撃破カウンターも追加できる形にする。
スキルツリー側は `SkillData` と取得条件を分離した `SkillTreeNodeData` のようなデータを用意し、必要ポイント、前提ノード、熟練度、訓練回数、双方の LP 消費回数、撃破数を基本 AND 条件で評価する。訓練条件の集計範囲は「全訓練」「特定の訓練 ID」「特定のカテゴリー ID」から選べるようにする。UI には対象訓練またはカテゴリー名と、各条件の現在値・必要値を表示する。旧セーブに存在しないカウンターとカテゴリー集計は 0 として扱う。
状態確認 UI でも訓練・戦闘実績を確認できるよう、`StatusDetailPanel` に「実績」タブまたは実績詳細パネルを将来追加する。基本能力欄へ全件を常時表示せず、全体集計を先に表示し、訓練カテゴリー別、訓練 ID 別、敵別の内訳を折りたたみまたは切り替えで表示する。累計訓練回数、双方の LP 消費回数、モンスター撃破数を `SkillProgressStats` から読み、訓練・戦闘結果確定後とロード後に更新する。未記録値は 0 として表示する。
実績集計基盤は実装済み。`TrainingData.trainingCategoryId` と `SaveData.skillProgressStats` を追加し、全体・訓練 ID 別・カテゴリー別の訓練回数と双方の LP 消費回数、全体・敵 ID 別の撃破数を保存・復元する。訓練メニューを途中で切り替えても、LP 消費は実際に進めた訓練 ID とカテゴリーへ記録される。全体訓練回数はセッションごと、個別訓練とカテゴリーはセッション内で 1 ステップ以上実行した対象ごとに 1 回加算する。初期カテゴリーは `Fundamentals`、`Combat`、`Endurance`。撃破は予定戦闘の勝利だけを数え、デバッグ戦闘、逃走、敗北は除外する。表示側は `GameManager.GetSkillProgressStats()` でコピーを取得でき、確認用に `[SkillProgress]` ログを出す。
`StatusProgressPanel` は実装済みで、全体、カテゴリー別、訓練別、敵別の表示を切り替える。内訳は表示名順のコピーを並べ替えるため保存済みリストの順序を変更せず、実績も熟練度も 0 の項目を省略する。内訳タイトルには表示件数、訓練別には熟練度、スキルツリーノードの条件に使われる集計には「スキル解放条件」を表示する。本文の高さは TextMesh Pro の内容に合わせて更新し、表示切替時は先頭へ戻す。`StatusDetailPanel.progressButton` から開き、`progressPanel` は状態詳細の子に配置した場合は自動検出可能。`MainScene` にはパネルルート、タイトル Text、スクロール可能な本文 Text、全体・カテゴリー・訓練・敵 Button、閉じる Button を配置し、`StatusProgressPanel` の各参照と `StatusDetailPanel.progressButton` / `progressPanel` を割り当て済み。
スケジュール拡張のデータ基盤は実装済み。セーブバージョン19から `SaveData.scheduleEntries` を正本とし、`ScheduleEntry` はゲーム内日数、予定種別、`Planned` / `Executed` / `Cancelled`、キャンセル理由を保持する。`ScheduleManager` は日付指定の取得・設定・変更・キャンセル、期間一覧、実行済み化を提供し、編集可能範囲は現在日から30日先まで。既存の今日・明日APIと画面は一覧を参照する互換窓口として残す。バージョン18以前のロード時は旧今日・明日フィールドを現在日と翌日へ移行し、新形式の保存にも移行期間中は旧フィールドを併記する。次はこの基盤を使う週間UIを実装する。
スキルポイント基盤は実装済み。主人公とヒロインのポイントは `SaveData.playerSkillPoints` / `heroineSkillPoints` に別々に保存する。完了かつ非中断の訓練だけが `TrainingData.playerSkillPointReward` / `heroineSkillPointReward` を付与し、軽い稽古は双方 1、実戦形式と持久訓練は双方 2。訓練結果メッセージと `StatusProgressPanel` の全体表示に獲得量・現在値を表示する。参照は `GameManager.PlayerSkillPoints` / `HeroineSkillPoints`、安全な消費は `TrySpendPlayerSkillPoints(...)` / `TrySpendHeroineSkillPoints(...)` を使う。消費成功時は状態詳細表示も更新する。旧セーブは 0。`EnemyData.playerSkillPointReward` / `heroineSkillPointReward` は予定探索の勝利時だけ付与し、敗北、逃走、デバッグ戦闘は除外する。森スライムと湖の精霊は双方1、洞窟コウモリは双方2。`BattlePanel` とフォールバック簡易戦闘の両方で加算し、結果メッセージとログに獲得量を表示する。
`SkillTreeNodeData` と条件評価基盤は実装済み。ノードは固定 ID、所有者、主人公用 `SkillData`、ヒロイン用 `targetHeroineId` / `grantedHeroineSkillId`、ポイントコスト、前提ノード、条件一覧、`treePosition` を持つ。条件種類は訓練熟練度、訓練回数、双方の LP 消費回数、モンスター撃破数、好感度、日数。集計範囲は全体、訓練 ID、訓練カテゴリー ID、敵 ID に対応し、全条件を AND で評価する。`GameManager.GetSkillTreeNodes()`、`EvaluateSkillTreeNode(...)`、`TryAcquireSkillTreeNode(...)` を使用する。評価結果は `Locked` / `Available` / `InsufficientPoints` / `Acquired`、現在値・必要値、未取得前提ノードを含む。
取得済みノードは主人公・ヒロイン別に `SaveData.acquiredPlayerSkillTreeNodeIds` / `acquiredHeroineSkillTreeNodeIds` へ保存する。主人公ノード取得時だけ対応スキルを使用可能にし、ヒロインノードは主人公スキルへ混入させない。起動時とロード時は取得済み主人公ノードから `unlockedSkillIds` を再構築するため、旧来のスキル ID だけでは使用可能にならない。初期主人公ツリーは `PowerStrike` から `BattleFocus`、`GuardStance` から `FirstAid` / `ArmorBreak` へ分岐する。上位ノードは熟練度に加えて、総モンスター撃破数、実戦カテゴリーの相手 LP 消費回数、主人公 LP 消費回数を条件に使う。`SkillTreePanel` と通常メニューからの取得導線は実装済みで、条件達成による自動解放は廃止済み。ヒロインノードは現在プロフィールと一致するものだけを表示し、取得済みの `grantedHeroineSkillId` だけを `BattlePanel` の自動スキル候補にする。DefaultHeroine は `RadiantSlash` から回復・防御へ、TestHeroine は `SharpThrust` から回復・防御へ分岐し、未取得時は通常攻撃を行う。
`SkillTreePanel` は各ノードの `treePosition` を使って二次元配置し、前提ノード間の接続線を実行時に生成する。Content の表示サイズを座標範囲から自動計算し、必要な方向だけスクロールを有効にする。ノード状態は灰色・黄色・緑・青、接続線は接続先ノードの状態に合わせて色分けし、選択中ボタンへ白い Outline を付ける。詳細欄には取得可否、不足理由、取得結果を表示する。訓練・カテゴリー・敵の条件対象 ID は表示名へ解決し、ヒロインタブでは現在ヒロイン名と所持ポイントを表示する。接続線用 Prefab を含む追加の Scene UI 部品は不要。
`SkillTreeDataValidator` はノード ID、前提関係、循環参照、所有者・対象ヒロイン、主人公・ヒロインスキル、条件対象 ID、条件重複、座標重複を検証する。Unity Editor の `FantasyLoveSim > Validate Skill Tree Data` で全ノードを検証でき、Editor Play と Development Build の開始時にも一度実行する。警告は `[SkillTreeValidation]` で始まるため Console で絞り込み可能。通常リリースビルドでは起動時検証を呼ばない。
好感度尺度は整数 `0〜9999`。既存の値・増減・条件は10倍へ移行し、従来の100相当は1000、ランク境界は200、400、600、800、1000とする。`HeroineStatus.maxAffection = 9999` と `endingUnlockAffection = 1000` を分離済み。旧 `maxAffection = 100` は通常コンテンツの終端でもあったため、移行後の会話・行動の既定上限は9999にして1000以降も利用可能にする。1000以降は単純な累積値として扱える。現在の `SaveData.saveVersion` は19で、旧好感度・熟練度尺度とのセーブ互換性は保証しない。
スキルシステムは `SkillData` 系ScriptableObjectと `SkillTreeNodeData` で管理する。スキルは汎用スキル、戦闘用スキル、訓練用スキルに分類する。汎用スキルは探索、会話、買い物、イベント条件、ステータス補正などに使い、戦闘用スキルは攻撃、防御、回復、バフ、デバフなど `BattlePanel` のコマンドに使う。訓練用スキルは模擬戦闘や稽古、熟練度上げに使う。
`SkillData`、`SkillCategory`、`SkillEffectType`、`SkillTargetType` は追加済み。スキルデータには `skillId`、表示名、カテゴリ、説明、消費コスト、対象、効果種別、威力または回復量、解放条件、使用可能な戦闘種別を持たせる。使用可能スキル ID は `SaveData.unlockedSkillIds` に派生情報として保存するが、取得済み主人公ノードを正本として起動・ロード時に再構築する。参照には `GameManager.IsSkillUnlocked(...)` / `GetUnlockedSkillIds()` を使い、外部から直接解放しない。主人公の装備中戦闘スキルは順序付きの `SaveData.equippedPlayerBattleSkillIds` に最大4件保存する。ヒロインの編成中戦闘スキルは `SaveData.heroineBattleSkillLoadouts` にヒロイン ID ごとの順序付きリストとして最大3件保存する。敵側は `EnemyData.battleSkills` の `EnemyBattleSkillData` を使い、敵ごとのスキル、MP コスト、対象、使用確率、優先度、戦闘中の最大使用回数を設定する。敵 MP も戦闘開始時に全回復し、MP が足りる候補から priority の高いスキルを確率で選ぶ。将来必要になったら `skillProficiencies` のような保存領域を追加する。
初期確認用のスキルデータは `Assets/Resources/Skills` に `PowerStrike`、`GuardStance`、`FirstAid` として追加済み。熟練度尺度の10倍化後の解放条件は順に `LightPractice` 30、`EnduranceTraining` 30、`SparringPractice` 50。`BattleFocus` は `LightPractice` 50、`ArmorBreak` は `EnduranceTraining` 50とする。
通常メニューのスキル導線は `SkillTreePanel` を開く。取得済みの主人公戦闘ノードを選ぶと既存の取得ボタンが「装備する」または「外す」に変わり、詳細欄とノード表示にも装備状態と使用中枠数を表示する。ヒロインノードでは同じボタンを「編成する」または「外す」として使い、現在ヒロインの3枠を編集する。取得時は空きがあれば自動編成する。旧セーブのバージョン14以前は取得済みスキルを最大3件まで補完し、バージョン15以降は空編成も維持する。ロード時は存在しない・未取得・重複・4件目以降のヒロインスキルを除去する。戦闘中は専用の `BattleSkillPanel` を優先して開き、装備中の最大4スキルだけを表示する。未配置の場合だけ従来の `SkillPanel` をフォールバックとして使う。`PowerStrike`（Damage）、`GuardStance`（Guard）、`FirstAid`（Heal）、`BattleFocus`（プレイヤー攻撃 Buff）、`ArmorBreak`（敵防御 Debuff）を実行できる。MP は戦闘開始時に最大値まで回復し、各スキルの `cost` を消費する。Buff / Debuff は `statusDurationTurns` の対象ターン数だけ `affectedStat` を変化させ、期限が切れると戦闘ログへ解除を出す。敵 Speed がプレイヤー Speed より 4 以上高い場合は 30% で追加行動する。ヒロイン用スキルは `HeroineProfileData.battleSkills` でヒロイン別に定義する。MP、確率、優先度、最大使用回数を持ち、編成中の候補から自動選択され、使用可能な候補がなければ通常攻撃する。初期設定は DefaultHeroine / TestHeroine ともに攻撃、HP が減った味方への回復、主人公への防御 Buff の 3 種類である。`BattlePanel` は主人公・ヒロイン・敵の HP と MP を常時表示し、`StatusButton` から `BattleStatusEffectPanel` を開く。状態パネルでは対象ごとに攻撃/防御/素早さの増減値と残りターンを一覧表示し、Buff と Debuff を色分けする。主人公・ヒロインの編成操作に追加の Scene UI は不要。カテゴリタブと MP 回復アイテムは後回しにする。
基礎ステータスは実装済み。共通の `BattleStatusData`、プレイヤー用の `PlayerStatus`、ヒロイン側の `HeroineStatus.BattleStatus` を使う。
所持金は案Aとして `PlayerStatus` だけが持つ。`SaveData` は `playerBattleStatus`、`playerMoney`、`heroineBattleStatus` を保存し、ロード時に復元する。
`StatusDetailPanel` はプレイヤー詳細にHP、MP、攻撃、防御、素早さ、所持金、装備中戦闘スキル、有効な訓練スキル、購入済み商品、解放衣装、衣装確認設定を表示する。ヒロイン詳細にはHP、MP、攻撃、防御、素早さ、好感度、現在衣装、編成中戦闘スキル、有効な訓練スキルを表示する。スキルはIDではなく表示名を使い、空欄は「なし」とする。
状態詳細の本文は `Scroll View > Viewport > Content > StatusDetailSummaryText` に配置する。`StatusDetailPanel` がTMPの `preferredHeight` に合わせて本文とContentの高さを更新し、画面を開いたときと対象切り替え時は先頭へ戻す。通常のステータス更新では現在のスクロール位置を維持する。
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
- Unity バージョンが `2021.3.45f2` か

## 備考

- 背景演出やアクション反応は拡張しやすいので、必要になった時点でデータを追加するのがよい
