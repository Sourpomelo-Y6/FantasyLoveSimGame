# 恋愛シミュレーションゲーム作成計画

このドキュメントは、今の実装を踏まえた今後の開発方針をまとめたものです。

## 現在の到達点

実装済みの中心ループは以下です。

```text
行動を選ぶ
↓
会話ジャンルを選ぶ、または日常行動を実行する
↓
反応を表示する
↓
好感度が変化する
↓
時間が進む
↓
条件を満たすとエンディングボタンが出る
```

### いまある機能

| 機能 | 内容 |
| ---- | ---- |
| タイトル画面 | `TitleScene` |
| メイン画面 | `MainScene` |
| 会話 | `Daily` / `Food` / `Adventure` / `Love` |
| 日常行動 | `会話` / `休む` / `散歩` / `お茶` / `贈り物` |
| 予定システム | 予定パネルで翌日の予定を選択し、保存・復元できる |
| 選択肢会話 | 2〜3択で好感度変化 |
| 行動反応 | 天候・時間帯・季節・好感度で差分を切り替え |
| 衣装システム | 着用中の衣装に対する反応と評価を保存 |
| 時間経過 | 朝→昼→夜→翌日 |
| 好感度 | 0〜9999。従来の値を10倍した整数尺度を使い、1000を従来の100相当として扱う |
| 行動HP効果 | `ActionData` / `ActionReactionData` の `playerHpChange` と `heroineHpChange` で HP を増減できる。`休む` はプレイヤーとヒロインの HP を 20 回復する |
| 背景切り替え | 時間帯・天候に応じて背景 Sprite を切り替え |
| ゲームイベント | `GameStart` / `DayStart` / `Manual` の汎用イベント |
| スチル回想 | 解放済み・未解放スチルを一覧表示 |
| メッセージログ | セッション中の直近メッセージを表示 |
| タイトルキャラクター選択 | `Resources.LoadAll<HeroineProfileData>("Heroines")` で候補を列挙し、新規ゲーム開始時のヒロインを選べる |
| 買い物 | `DuoShopping` 予定から `ShopPanel` を開き、所持金消費、購入済み保存、衣装解放を行う |
| 探索・戦闘 | 森、洞窟、湖の探索で敵候補を解決し、簡易戦闘または `BattlePanel` に接続できる |
| 訓練・スキル | `TrainingPanel` で訓練を進め、好感度、熟練度、実績、スキルポイントを保存する。条件を満たしたノードを `SkillTreePanel` で取得し、習得したスキルを戦闘で使える |
| エンディング | 好感度1000で入口を解放し、条件一致する `EndingData` を表示。好感度上限9999とは分離する |

通常の起点は `TitleScene` で、そこから `MainScene` に進む構成を想定しています。

## 今後の方針

最初から全部作ろうとせず、まずは「会話と日常行動で好感度を上げ、条件達成でエンディングを見る」体験を磨くのが優先です。

そのうえで、以下を段階的に追加・整理します。

1. 行動反応と会話データの追加
2. エンディングデータと条件分岐パターンの追加
3. 立ち絵変更と表情差分
4. セーブ/ロード回帰確認とUI補強
5. 訓練、戦闘、ショップのデータ追加と調整

着せ替えと衣装評価は、いまの実装で導線と保存が入っているため、今後は評価の種類追加や UI の整理を中心に詰めるとよいです。

## スチル回想

イベントスチル表示は `GameEventData.pages[].stillSprite` で扱えるため、次の段階では「表示済みスチルを回想画面で見られる」仕組みを追加する。
回想一覧には解放済みスチルだけでなく未解放スチルも枠として表示し、未解放スチルは押せない無効ボタンとして扱う方針にする。

### 保存データ

回想の解放状態は、イベント既読とは別に保存する。
`shownGameEventIds` はイベント再生済み管理であり、スチル単位の解放状態とは用途が違うため、`SaveData.unlockedStillIds` で保存する。

スチルIDは `eventId` とページ番号から自動生成するより、専用の `stillId` を持たせる方が安全。
イベント本文のページ順を後から変えても、回想の解放状態が壊れにくいため。
`GameEventPageData` に `stillId` を追加し、`stillSprite` があるページだけ `stillId` を設定する。

命名は次を基本にする。

- `GameStartIntro_01`: ゲーム開始導入の1枚目
- `Story_Chapter01_01`: 本編1章の1枚目
- `Event_用途_連番`: 汎用イベントスチル

### 解放タイミング

イベント再生中に `stillSprite` と `stillId` を持つページを表示した時点で、その `stillId` を解放済みにする。
イベント完了時ではなく表示時に解放する理由は、複数ページイベントの途中で別スチルに切り替わる場合でも正しく保存できるため。

ただし、セーブタイミングは通常のセーブ操作に任せる。
自動セーブが必要になった場合は、スチル解放時にセーブするのではなく、イベント終了時にまとめて保存する方が安全。

### 回想画面

回想画面は、最初はメイン画面から開ける独立パネルとして作る。
将来的にタイトル画面からも開きたくなった場合は、同じパネルまたは同じデータを使ってタイトル側にも入口を追加する。

Unity UI は手作業配置を基本にする。
コード側では以下の参照だけを `SerializeField` で受け取る。

- 回想パネルのルート
- 一覧を並べるコンテナ
- スチル項目ボタンの Prefab
- 大きく表示する Image
- タイトルまたは説明用 Text
- 戻るボタン

一覧は、全スチル候補を表示し、解放済みの項目だけ押せるようにする。
未解放の項目は `???` などのラベルにして、ボタンを `interactable=false` にする。
未解放項目を押したときの説明表示は不要で、まずは押せないことが分かればよい。
ページングは `StillGalleryPanel` に実装済み。カテゴリ分け、サムネイル生成は後回しにする。
項目ボタンのサムネイル Image は、スチル数が増えて一覧の視認性が問題になってから追加を検討する。
当面は Text ボタンで `stillId` または `???` を表示できれば十分とする。

### データ配置

回想対象のスチル画像は `Assets/Images/Heroines/<HeroineId>/Event/` に置き、ファイル名は `stillId` に合わせる。
`GameEventData` の `stillId` と画像ファイル名を近づけることで、Unity Inspector 上で対応を追いやすくする。
将来は JSON から画像ファイルパスを取得し、イベントや回想で表示する画像を差し替えられるようにする。
JSON には `stillId`、表示名、画像ファイルパスを持たせ、実行時にそのパスから Sprite を読み込む方針にする。
この場合、画像の配置先は `Resources`、`StreamingAssets`、Addressables のどれを使うか先に決める必要がある。

回想一覧に表示する名前や説明が必要になったら、`StillGalleryData` のような ScriptableObject を追加する。
初期実装では `stillId` と Sprite だけで表示し、説明文やカテゴリは後から足す。

### 実装順

1. `SaveData` に `unlockedStillIds` を追加する。実装済み
2. `GameEventPageData` に `stillId` を追加する。実装済み
3. `GameManager` がスチル表示時に `stillId` を解放済みにする。実装済み
4. 回想用パネルの接続ポイントを `StillGalleryPanel` として用意する。実装済み
5. Unity 上で回想 UI を手動配置し、Inspector で参照を割り当てる。初期配置済み
6. メイン画面の行動またはボタンから回想パネルを開けるようにする。`StillGalleryAction` で実装済み
7. 未解放スチルは無効ボタン、解放済みスチルは押せるボタンとして一覧に出るようにする。実装済み
8. 一覧にページング機能を追加する。実装済み
9. スチル数が増えて必要になったら、項目ボタンにサムネイル Image を追加する

最初の実装では、`GameStartIntro_01` を回想解放の確認用に使う。
`TestManualEvent` で同じスチルを表示した場合も解放されるようにすれば、F7 で回想の動作確認がしやすい。

ページングは `StillGalleryPanel` の `itemsPerPage`、前へボタン、次へボタン、ページ表示 Text で制御する。
一覧更新時は全候補から現在ページ分だけを生成し、前後ボタンはページ端で無効化する。
前へボタン、次へボタン、ページ表示 Text は未割り当てでも動くため、Unity UI の配置前でも従来の一覧表示を維持できる。
カテゴリ別ページやフィルタはさらに後回しにする。

## 背景画像

通常背景画像は `Assets/Images/Background/` に置く。
ファイル名は `Background_時間帯_天候.png` で統一し、時間帯と天候から用途が分かるようにする。

例:

- `Background_Morning_Sunny.png`
- `Background_Morning_Cloudy.png`
- `Background_Noon_Rainy.png`
- `Background_Night_Storm.png`
- `Background_Night_Snow.png`

背景の割り当ては `BackgroundSpriteData` で管理する。
Unity の Project ウィンドウで `LoveSim/Background Sprite Data` を作成し、`entries` に `TimeSlot`、`Weather`、`Sprite` の組み合わせを登録する。
作成した `BackgroundSpriteData` を `GameManager.backgroundSpriteData` に割り当てると、`RefreshUI()` 時に現在の時間帯と天候に合う背景へ切り替わる。

未設定の組み合わせは、従来の `dayBackgroundSprite` / `nightBackgroundSprite` にフォールバックする。
そのため、全パターンの背景画像が揃う前でも段階的に差し替えられる。
画像が増えたら、朝・昼・夜・深夜をすべて別画像にするか、朝昼を同じ昼背景、夜深夜を同じ夜背景として扱うかを整理する。

## 画面構成の考え方

`MainScene` に主要導線を集約し、必要に応じてサブシステムを足していくのがよいです。
UI デザイン、Unity シーン編集、Inspector の参照設定は手作業で行い、コード側は必要な接続ポイントを用意する方針です。

今後の拡張では、プレイヤーとヒロインそれぞれに詳細ステータス画面を置き、その中に能力項目と能力獲得画面への導線を作ると、衣装確認モードの解放や各種機能解放を整理しやすくなります。
実装上は `StatusDetailPanel` で詳細表示と能力獲得画面の切り替えをまとめ、対象は `StatusDetailRole`、能力項目は `StatusAbilityKind` で管理するとよいです。
能力の解放状態はセーブデータに保存し、ロード後も同じ解放状況を再現する。
メイン画面の主人公・ヒロイン HP 表示は、まずテキストで現在値と最大値を表示する。
HP バーやゲージ色変更などの視覚的な強化は、戦闘の基本導線や結果反映が落ち着いてから後回しで検討する。
現段階の能力は、取得後に常時利用できる `解放済み` 状態として扱い、取得後の有効/無効切り替えは持たせない。
オンオフ可能な能力が必要になった場合は、能力状態を `Locked` / `Unlocked` / `Active` に分け、解放状態と使用状態を別々に保存する。
能力表示は `StatusAbilityData` の ScriptableObject で管理し、`Assets/Resources/StatusAbilities/` から読み込む。
プレイヤー能力とヒロイン能力は表示枠と保存先を分け、`StatusAbilityData.targetRole` または `StatusDetailPanel.playerAbilities` / `heroineAbilities` で表示一覧を分ける。
解放条件は `StatusAbilityData.requiredAffection` と `requiredDay` で管理し、条件未達の場合は獲得ボタンを無効化して不足条件を表示する。
ただし、現在値付きの詳細条件表示や不足理由の細分化は現時点では優先しない。
タイトルから新規ゲームを始めた直後は、いきなり `MainScene` へ移るのではなく、ゲーム開始イベントを挟んでスチル表示を入れる。
Unity Editor で `MainScene` を直接開いて再生した場合は、開発確認用として開始イベントを発生させない。
ゲーム開始イベント中は `SaveLoadPanel` を閉じた状態に保ち、`Save` / `Load` を表示しない。
`GameEventData` の `DayStart` は翌朝メッセージに混ぜて自動再生し、`Manual` は `GameManager.TryStartManualGameEvent(string eventId)` で明示起動する。
開発時の確認用に、`GameManager` の `debugManualGameEventId` を `F7` で起動する入口もある。
テスト用の手動イベントとして `TestManualEvent` を用意しており、`debugManualGameEventId` に設定すると `F7` で繰り返し再生できる。

タイトル画面にはキャラクター選択 UI を追加済み。
実行時にファイルシステム上のフォルダを直接探索するのではなく、`Resources.LoadAll<HeroineProfileData>("Heroines")` で存在する `HeroineProfileData` を列挙する方式にしているため、Editor とビルド後の両方で扱いやすい。
選択画面では、候補リスト、選択中ヒロインの `displayName`、`defaultHeroineSprite`、必要なら説明文や口調メモを表示する。
決定ボタンを押したら選択した `heroineId` または profile resource path をゲーム開始設定へ保存し、新規ゲーム開始時に `GameManager` がその `HeroineProfileData` を読み込む。
ロード時はセーブデータに保存されたヒロイン ID を優先し、タイトル画面で選んだヒロインで既存セーブを上書きしない。
`GameStartSettings` は選択中ヒロイン ID を持ち、`SaveData` にも保存済みヒロイン ID を保存する。

解放後の実効果は `StatusAbilityData.effectType` で分岐し、`StatusAbilityKind` は表示種別や旧データ互換の分類として使う。
`effectType` が `UseAbilityKind` の場合は従来通り `StatusAbilityKind` から衣装確認能力を推測し、`None` の場合は効果なし能力として `abilityId` の取得済み状態だけを保存する。
これにより、テスト用能力や将来の表示カテゴリを追加しても、実効果の有無をデータ側で明示できる。
入口は `StatusDetailAction` を行動一覧に置くと、既存の行動導線に自然に混ぜられます。
`StatusDetailPanel` の UI は Unity の Hierarchy 上に手で配置し、`GameManager` から参照する形で扱う。
`GameManager.EnsureStatusDetailPanel()` は配置済みの `StatusDetailPanel` を探して初期化するだけで、UI のランタイム生成は行わない。

```text
Canvas
├── BackgroundImage
├── HeroineImage
├── StatusPanel
│   ├── DayText
│   ├── TimeText
│   ├── AffectionText
│   ├── WeatherText
│   └── SeasonText
├── SchedulePanel
├── PlayerStatusDetailPanel
│   ├── AbilityList
│   └── AbilityAcquireButton
├── HeroineStatusDetailPanel
│   ├── AbilityList
│   └── AbilityAcquireButton
├── DialoguePanel
│   ├── SpeakerNameText
│   ├── DialogueText
│   ├── ChoiceButtonArea
│   └── NextButton
└── CommandPanel
    ├── ActionButtonArea
    ├── GenreButtonArea
    └── EndingButton
```

## データ設計の方針

### HeroineProfileData

ヒロイン差し替えは、画像だけを差し替えるのではなく、ヒロイン単位で画像・会話・イベント・行動反応・エンディングを束ねる方針にする。
`Sprites` フォルダと `Images` フォルダのうち `Background` 以外は、基本的にヒロインに紐づく素材として扱う。
背景は共通素材として残し、ヒロイン別差し替えの対象から外す。

`HeroineProfileData` の ScriptableObject を追加済みで、以下を持たせる。

- `heroineId`: セーブや将来の切り替えで使う一意 ID
- `displayName`: 表示名
- `conversationResourcePath`: 会話データの読み込みパス
- `gameEventResourcePath`: 汎用イベントデータの読み込みパス
- `actionResourcePath`: 行動・行動反応データの読み込みパス
- `endingResourcePath`: エンディングデータの読み込みパス
- `defaultHeroineSprite`: 代表立ち絵
- `initialDialogueMessage`: MainScene 直接開始時などの初期セリフ
- `nextActionPrompt`: 行動や会話後に次の行動を促すセリフ
- `morningGreeting`: 翌朝開始時の挨拶
- `goodNightGreeting`: 夜から翌日へ進む前の挨拶
- `gameStartFallbackMessage`: GameStart イベントがない場合の開始メッセージ
- `gameStartFollowUpMessage`: GameStart fallback 後の次セリフ

現在のヒロインは `DefaultHeroine` として扱い、`Assets/Resources/Heroines/DefaultHeroineProfile.asset` に `Heroines/DefaultHeroine` / `Heroines/DefaultHeroine/GameEvents` / `Heroines/DefaultHeroine/Actions` / `Heroines/DefaultHeroine/Endings` を参照させている。
`GameManager` は profile から会話・イベント・行動の読み込みパスを適用し、`EndingScene` へ遷移するときに profile の `endingResourcePath` を `EndingSelectionSettings` へ渡す。
`EndingManager` は渡された `endingResourcePath` があればそのパスから `EndingData` を読み込む。
コード直書きだった朝夜の挨拶や行動後プロンプトは `HeroineProfileData` へ移し、ヒロイン差し替え時に口調も変えられるようにする。
将来ヒロインを増やす段階で、Resources 配下を次のように分ける。

```text
Assets/Resources/Heroines/DefaultHeroine/Conversations/
Assets/Resources/Heroines/DefaultHeroine/HeroineAssetCatalog.asset
Assets/Resources/Heroines/DefaultHeroine/HeroineLayeredSpriteData.asset
Assets/Resources/Heroines/DefaultHeroine/GameEvents/
Assets/Resources/Heroines/DefaultHeroine/Actions/
Assets/Resources/Heroines/DefaultHeroine/Endings/
```

画像素材は参照切れを避けるため、Resources パスの切り替えが動いた後で整理する。
現在は次のように、背景以外を `DefaultHeroine` 配下へ移動済み。

```text
Assets/Images/Heroines/DefaultHeroine/Sprites/
Assets/Images/Heroines/DefaultHeroine/Event/
Assets/Images/Heroines/DefaultHeroine/Actions/
Assets/Images/Heroines/DefaultHeroine/Ending/
```

実装順は、`HeroineProfileData` の追加、`DefaultHeroineProfile.asset` の作成、`GameManager` の読み込みパス差し替え、`EndingManager` の読み込みパス差し替えまでは完了。
`Resources/Heroines/DefaultHeroine/...` へのデータ移動と profile のパス切り替えは実施済み。
画像フォルダ整理も `DefaultHeroine` については実施済み。

差し替え確認用として `Assets/Resources/Heroines/TestHeroineProfile.asset` を追加済み。
`Heroines/TestHeroine/Actions` / `Conversations` / `GameEvents` / `Endings` には最小確認用データだけを置く。
`MainScene` の `GameManager.heroineProfile` に `TestHeroineProfile` を割り当てると、ヒロイン名、開始イベント、会話、エンディングの読み込み元が切り替わるか確認できる。
`HeroineProfileData.defaultHeroineSprite` は通常衣装 `Normal` の立ち絵として `OutfitManager` に渡し、通常衣装以外は衣装側の `heroineSprite` を優先する。
AssetTool の `assets_export.json` は importer で `HeroineAssetCatalog.asset` に変換し、画像の `assetId`、用途、Unity asset path、Sprite 参照を保持する。
AssetTool の `sprite_layers_export.json` は importer で `HeroineLayeredSpriteData.asset` に変換し、表情、衣装、ベース、小物の透過レイヤー定義を保持する。
実際の表示に使う `HeroineLayeredSpriteView` は追加済み。
現在衣装の `costumeId` と会話行の `expressionId` から `BaseBody`、`Costume`、`Expression`、条件一致 `Accessory` を選び、指定がない場合は `Default` 衣装と `Neutral` 表情へ fallback する。
会話 import は `lines[]` を保持し、実行時に `expression` を表情レイヤー切り替えへ渡す。
今後は会話とイベントだけでなく、衣装変更時のヒロインメッセージと `衣装を見る` 実行後のヒロイン反応にも `expressionId` を持たせる。
`HeroineProfileData.outfitMessageOverrides` と `outfitReactionMessageOverrides` に表情指定を追加し、衣装変更成功時、未解放時、褒める/嫌う/退屈/着替える反応時に `HeroineLayeredSpriteView` の表情を切り替えられるようにする。
本番用ヒロインを追加する前に、まずこの profile で差し替え導線を手動確認する。

新しいヒロインを追加するときは、次のチェックリストを使う。

- `HeroineProfileData` を作成し、`heroineId` と `displayName` を設定する
- `conversationResourcePath` に、そのヒロイン用の root パス `Heroines/<HeroineId>` を設定する
- `gameEventResourcePath` に、そのヒロイン用の `GameEvents` フォルダを設定する
- `actionResourcePath` に、そのヒロイン用の `Actions` フォルダを設定する
- `endingResourcePath` に、そのヒロイン用の `Endings` フォルダを設定する
- `defaultHeroineSprite` に代表立ち絵を設定する
- `HeroineAssetCatalog.asset` に画像の `assetId` と Sprite 参照が入っているか確認する
- 透過レイヤー方式を使う場合は `HeroineLayeredSpriteData.asset` に `BaseBody`、`Default` 衣装、`Neutral` 表情が入っているか確認する
- `HeroineLayeredSpriteView` の `BaseBodyImage`、`CostumeImage`、`ExpressionImage`、`AccessoryImage` が同じ親の下にあり、表情会話で `Neutral`、`Smile`、`Sad` などが切り替わるか確認する
- `Actions` には行動名、行動結果、行動反応、行動スチルを用意する
- `Conversations/` にはジャンル会話、好感度条件会話、天候・時間帯・季節条件会話を個別 `ConversationData` として用意する
- `GameEvents` には `GameStart`、`DayStart`、`Manual` 確認用イベントを用意する
- `Endings` には少なくとも `defaultEndingId` と一致する `EndingData` を用意する
- `Images/Heroines/<HeroineId>/Sprites/` には通常立ち絵と、必要なら衣装・表情差分を用意する
- `Images/Heroines/<HeroineId>/Event/` にはイベントスチルを用意し、`stillId` とファイル名を対応させる
- `Images/Heroines/<HeroineId>/Actions/` には行動スチルを用意し、`ActionData.stillId` または `ActionReactionData.stillId` と対応させる
- `Images/Heroines/<HeroineId>/Ending/` にはエンディングスチルを用意し、`EndingData.stillSprite` に割り当てる
- `Images/Background` は共通背景なので、ヒロイン別素材とは分けて扱う
- `MainScene` の `GameManager.heroineProfile` を新しい profile に切り替えて、行動・会話・イベント・エンディングが読み込めるか確認する

### ConversationData

会話は引き続き `ConversationData` で管理します。
新規 import では、対象ヒロインの `Assets/Resources/Heroines/<HeroineId>/Conversations/<ConversationId>.asset` に会話を個別保存する。
`GameManager` は個別の `ConversationData` アセットを会話候補として読み込む。
既存互換として、旧 `Conversations.asset` container の `ConversationData.items` も展開できる。
会話IDは `カテゴリ_条件_連番` を基本にし、`showOnce` は一度だけ見せる会話に限って使う。
`minAffection` / `maxAffection` は会話の入口条件、`allowedTimeSlots` / `allowedWeathers` / `allowedSeasons` は必要な場合だけ使う。条件が重なる会話は `priority` で解決する。
同じ条件の会話が複数ある場合でも、`conversationId` から用途が分かるようにしておく。

### GameEventData

タイトルから新規ゲームを開始した直後の導入や、今後の汎用イベントは `GameEventData` で管理する。
対象ヒロインの `gameEventResourcePath` 配下に置き、`triggerType` で `GameStart` / `DayStart` / `Manual` を分ける。
ページごとに話者、メッセージ、必要ならスチルを持ち、`showOnce` は `shownGameEventIds` でセーブデータに保存する。
`TestManualEvent` は `Manual` / `showOnce=false` の確認用イベントで、システム・ヒロイン・予定・衣装の話者表示とスチル表示をまとめて確認するために使う。

イベントIDは用途が分かるように、以下の規則を基本にする。

- `GameStartIntro`: タイトルから新規開始した直後の導入イベント
- `DayStart_条件_連番`: 翌朝メッセージに混ぜる日開始イベント
- `Manual_用途_連番`: デバッグ、確認用、任意起動イベント
- `Story_章_連番`: 将来の本編イベント
- `Still_用途_連番`: スチル表示を主目的にしたイベント

`eventId` はセーブデータの `shownGameEventIds` に入るため、後から名前を変えると既読管理が崩れる。
本番用イベントは追加後に ID を変更しない方針にする。
表示文だけの修正は同じ `eventId` のまま行い、イベントの意味や発生条件が変わる場合は新しい `eventId` を作る。

トリガー別の運用は以下にする。

- `GameStart`: 新規開始時に一度だけ見る導入イベント。基本は `showOnce=true`
- `DayStart`: 翌朝に自動で混ぜるイベント。条件付きイベントを増やす場合は発生条件フィールドを追加する
- `Manual`: デバッグ確認、テスト再生、将来の任意起動イベントに使う。確認用は `showOnce=false`

`GameEventData` には発生条件フィールドを追加済み。
イベント数が増えても「何日目以降」「好感度いくつ以上」「特定イベントを見た後」などを ScriptableObject 側で指定できる。
現在使える条件フィールドは以下。

- `minDay`: この日数以上で発生する。`0` または `1` 以下なら制限なし
- `maxDay`: この日数以下で発生する。`0` 以下なら制限なし
- `minAffection`: この好感度以上で発生する。`0` なら制限なし
- `maxAffection`: この好感度以下で発生する。`0` 以下なら制限なし
- `requiredShownEventIds`: 指定イベントをすべて見ている場合だけ発生する
- `blockedShownEventIds`: 指定イベントを1つでも見ている場合は発生しない
- `requiredOutfitIds`: 現在の衣装IDが指定一覧のどれかに一致したときだけ発生する
- `blockedOutfitIds`: 現在の衣装IDが指定一覧のどれかに一致した場合は発生しない
- `requiredOutfits`: 指定した `OutfitData` アセットの衣装を着ている場合だけ発生する
- `blockedOutfits`: 指定した `OutfitData` アセットの衣装を着ている場合は発生しない
- `requiredSkillIds`: 指定した主人公スキルをすべて取得している場合だけ発生する

条件判定は `GameManager.CanStartGameEvent(GameEventData gameEvent)` に集約し、`GetGameEventsForTrigger()` と `TryStartManualGameEvent()` の両方から使う。
既存の `GameStartIntro` と `TestManualEvent` は条件未設定なら今まで通り発生するようにし、既存データの互換を壊さない。
条件フィールドを追加した後も、`showOnce` は既読管理、条件フィールドは発生可否という役割分担にする。
将来、予定やスチル解放状態を条件にしたくなった場合は、同じ `CanStartGameEvent` に条件を追加していく。
衣装条件は現在の `OutfitManager.CurrentOutfit.outfitId` を見て判定する。
文字列ID欄の `requiredOutfitIds` / `blockedOutfitIds` も残しているが、通常は Unity Inspector で `OutfitData` アセットを選べる `requiredOutfits` / `blockedOutfits` を使う。
衣装の種類や属性ではなく、実際に着ている衣装を直接指定する方が、表示するスチルとの対応を崩しにくい。
汎用スキルのイベント条件接続は実装済み。`requiredSkillIds` は取得済み主人公ノードから再構築されるスキル ID を参照し、条件のためだけのセーブ項目は追加しない。空または未取得の ID が含まれるイベントは開始不可として安全に扱う。`GameEventDataValidator` は存在しない ID と同一イベント内の重複・空 ID を検出し、`FantasyLoveSim > Validate Game Event Data` から全ヒロインのイベントを検証できる。Editor Play / Development Build の起動時にも全ヒロインのイベントを検証する。
確認用として汎用スキル「気配り」と主人公ノード `Player_Consideration`、TestHeroine 専用手動イベント `Manual_Consideration_01` を追加する。訓練を1回完了して訓練回数と SP を獲得し、1 SP で「気配り」を取得すると、F7 からイベントを開始できる。取得前は同じ操作をしても開始しない。イベントは繰り返し確認できるよう `showOnce=false` とする。

イベントスチル画像は `Assets/Images/Heroines/<HeroineId>/Event/` に置き、ファイル名はイベントIDに寄せる。
例として、`GameStartIntro` で使う画像は `GameStartIntro_01.png` のようにする。
複数ページで同じスチルを維持したい場合は、最初のページだけでなく必要なページにも `stillSprite` を設定するか、現在スチルを維持する仕様を明文化してから実装する。
将来、イベント定義を外部 JSON 化する場合は、ページごとに画像ファイルパスを持たせ、JSON の内容だけで表示画像を差し替えられるようにする。
ScriptableObject の `stillSprite` は手動設定向け、JSON の画像パスは外部データ差し替え向けとして役割を分ける。

イベントが増えてきたら、対象ヒロインの `gameEventResourcePath` 内で ScriptableObject 名と `eventId` を一致させる。
Unity 上の一覧で見つけやすくするため、`sortOrder` は同種イベント内の表示順や発生順に使い、同じトリガー内で重複しないようにする。

### ActionData

日常行動は `ActionData` で管理します。

```csharp
public class ActionData : ScriptableObject
{
    public string actionId;
    public string displayName;
    public ActionExecutionType executionType;
    public string resultMessage;
    public string unavailableMessage;
    public bool useHeroineNameAsSpeaker;
    public int affectionChange;
    public bool advanceTime;
    public List<ActionReactionData> reactions;
    public ActionButtonColumn displayColumn;
    public int sortOrder;
    public bool isEnabled;
}
```

行動ボタンは `ActionButtonArea` を左・中央・右の列に分けて配置する。
`ActionData.displayColumn` で `Left` / `Center` / `Right` を指定した場合は、その列に配置する。
`Auto` の場合や指定列が存在しない場合は、旧データ互換として `sortOrder` 順の有効な `ActionData` を列数で均等に分配する。
列内の順序は既存の `sortOrder` を使う。
初期アクションは基本行動を左列、交流・状態系を中央列、衣装・予定などの補助系を右列に置く。
列見出しの追加や分類の微調整は現時点では優先しない。
必要になった場合は、列見出しは Unity UI 上に Text を手動配置し、分類変更は `ActionData.displayColumn` の設定で行う。

### ActionReactionData

条件分岐は `ActionReactionData` に分けると、次の拡張がしやすくなります。

- 時間帯ごとの反応
- 天候ごとの反応
- 季節ごとの反応
- 好感度帯ごとの反応

今の実装では、`ActionData.reactions` から条件一致する候補を集め、`priority` が高いものを優先して選ぶ。
同じ `priority` が複数ある場合はランダムに 1 件を選ぶ。
条件欄は `minAffection` / `maxAffection`、`anyTimeSlot` / `allowedTimeSlots`、`anyWeather` / `allowedWeathers`、`anySeason` / `allowedSeasons` で構成する。
`stillId` / `stillSprite` を持たせると、反応専用のスチルも差し替えられる。
当面は衣装や予定種別まで条件に入れず、まずは時間帯・天候・季節・好感度で反応の厚みを増やす。

## 買い物・探索・戦闘の拡張予定

二人で買い物に行く予定では、アイテムや衣服を購入できる機能を追加済み。
`DuoShopping` は単なるスチル表示イベントではなく、商品一覧、所持金、購入結果、衣装解放を扱う専用フローとして動く。
購入した衣服は `OutfitData` の解放状態と連動し、購入済み ID と解放済み衣装 ID はセーブデータに保存する。

買い物以外のお出かけ、たとえば森、洞窟、湖などは、探索・戦闘要素を持つ行動へ拡張中。
簡易戦闘は、敵、HP、攻撃、防御、素早さ、報酬、敗北時の処理を最小単位として実装済み。
探索や戦闘の結果は、好感度、入手アイテム、衣装解放、イベント発生条件、エンディング条件へつなげられるようにする。

ステータス画面には、買い物や戦闘で使うパラメータを表示する。
プレイヤーとヒロインの両方に、戦闘用パラメータとして HP、攻撃、防御、素早さを持たせ、所持金はプレイヤー側で管理する。
`SaveData` はプレイヤー/ヒロインの戦闘ステータス、プレイヤー所持金、購入済み商品、解放済み衣装を保存する。
UI は既存の `StatusDetailPanel` を拡張し、能力項目とは別に基本能力、戦闘能力、所持品または購入済み衣服を確認できる欄を追加する。

買い物・探索・戦闘で共通利用する基礎ステータスは実装済み。
次は商品データ、敵データ、戦闘結果イベント、UI 表示を増やしながらバランスを調整する。

実装順は次を基本にする。

1. プレイヤーとヒロインの戦闘・買い物用ステータスを追加する。最小項目は HP、最大 HP、攻撃、防御、素早さ、所持金。実装済み
2. 追加したステータスを `StatusDetailPanel` に表示する。実装済み
3. 追加したステータスを `SaveData` に保存し、セーブ/ロードで復元する。実装済み
4. 所持金を増減するテスト処理を用意する。実装済み。デフォルトでは F8 で +100、F9 で -100
5. `DuoShopping` などの予定から簡易買い物イベントを起動する。実装済み。DuoShopping 実行時に商品カタログを開く
6. 買い物でアイテムまたは衣装を購入し、所持金消費と解放状態を反映する。`ShopCatalogData` と `ShopItemData` による季節衣装 4 商品の購入済み保存と衣装解放まで実装済み
7. 森、洞窟、湖などの簡易探索フローを追加する。最小版として、該当予定の完了時に探索結果メッセージ、所持金報酬、HP 変化を反映する処理を実装済み
8. 敵、HP、攻撃、防御、素早さ、報酬、敗北時処理を持つ簡易戦闘を追加する。最小版として探索時の敵候補解決と UI なしの自動戦闘計算を実装済み

現在は `BattleStatusData` と `PlayerStatus` を追加し、所持金は案Aとしてプレイヤーだけが持つ。
`HeroineStatus` は戦闘用ステータスのみを持ち、所持金は持たない。
`GameManager` は `PlayerStatus` がシーンに未配置の場合、実行時に自分の GameObject へ自動追加する。
Unity 上で明示配置したい場合は、`GameManager.playerStatus` に `PlayerStatus` を割り当てる。
`GameManager.playerStatus` が未設定の場合は、まずシーン内の既存 `PlayerStatus` を探し、見つからなければ `GameManager` と同じ GameObject に `PlayerStatus` を自動追加する。
このため、未配置のままでもゲームは動くが、Hierarchy に `PlayerStatus` という専用オブジェクトは表示されない。
Inspector で Player の初期値やバランスを調整したい場合は、空の GameObject を作成して `PlayerStatus` コンポーネントを追加し、そのオブジェクトを `GameManager.playerStatus` に割り当てる。
今後バランス調整を行う場合は、値の所在を明確にするため手動配置を推奨する。
現在の初期戦闘パラメータは次の通り。

| 対象 | HP | 攻撃 | 防御 | 素早さ | 所持金 | 備考 |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Player | 100/100 | 10 | 5 | 5 | 1000 | `PlayerStatus` で明示定義 |
| Heroine | 80/80 | 8 | 4 | 6 | - | `HeroineStatus` で定義 |
| ForestSlime | 24/24 | 5 | 2 | 3 | - | 報酬 12、勝利時好感度 +1 |
| CaveBat | 55/55 | 11 | 3 | 8 | - | 報酬 45、勝利時好感度 +1 |
| LakeSpirit | 28/28 | 4 | 4 | 5 | - | 報酬 10、勝利時好感度 +2 |

探索先ごとの初期バランス方針は次の通り。

| 探索先 | 想定難度 | 方針 |
| --- | --- | --- |
| 森 | 低 | Player 単独でもほぼ勝てる。探索と戦闘ログ確認用の安全な入口にする |
| 洞窟 | 高 | Player 単独では危険で HP を削られやすい。Duo 探索なら安定寄りにする |
| 湖 | 低 | 報酬より好感度寄り。危険度は低めにして回復/交流系イベントへつなげやすくする |

戦闘バランスを調整する場合は、まず敵の `attack`、次に敵の `maxHp`、その後に Player / Heroine の初期値、最後に報酬や好感度変化を調整する。
プレイヤー側の初期値を頻繁に変えると全探索先の難度が同時に動くため、単一探索先の調整では敵側の値を優先する。
敗北時は報酬なし、HP 1、予定消費済みを基本挙動とし、敗北イベント分岐を追加する場合もこの結果を基準にする。

所持金のデバッグ確認用に、`GameManager` は `debugAddMoneyKey`、`debugSpendMoneyKey`、`debugMoneyAmount` を持つ。
デフォルト設定では F8 で所持金を 100 増やし、F9 で 100 消費する。
テスト商品は `ShopItemData` で定義し、`ShopCatalogData` に並べて `GameManager.duoShoppingShopCatalog` に設定する。
現在は `Assets/Resources/ShopItems/SpringOutfitItem.asset`、`SummerOutfitItem.asset`、`AutumnOutfitItem.asset`、`WinterOutfitItem.asset` が価格 100 の季節衣装商品として定義済み。
`Assets/Resources/ShopItems/DuoShoppingCatalog.asset` は季節衣装商品 4 件を持つ。
簡易買い物イベントとして、`DuoShopping` 予定の実行時に専用 `ShopPanel` を開き、カタログの商品を一覧表示する。
商品を選ぶとその商品の価格を消費し、選択前に `ShopPanel` を閉じた場合は予定を消費しない。
商品ボタンは `商品名 / 価格G / 解放: 衣装ID` の形式で表示し、購入済み商品と所持金不足の商品は状態ラベルを付けて無効化する。表示順は未購入商品を上、購入済み商品を下に並べる。
`ShopItemData` は購入条件として `requiredAffection`、`requiredDay`、`requiredPurchasedItemIds` を持つ。条件未達の商品は `条件未達` と表示して無効化し、購入処理側でも条件を再確認する。現在の季節衣装商品は条件なしとして扱う。
`ShopPanel` は他のパネルと同じく自動生成せず、Canvas 直下に手動配置して `GameManager.shopPanel` に割り当てる。
必要な UI は `ShopPanel` ルート、`TitleText`、`EmptyText`、`ShopItemList`、`ShopItemButtonPrefab`、`CloseButton`。
所持金が足りない場合は、買い物できなかった旨を予定イベント本文に追記する。
`GameManager.duoShoppingShopCatalog` が未設定または空の場合は `duoShoppingShopItem` を使い、それも未設定の場合は従来の `duoShoppingTestItemId` / `duoShoppingTestCost` / `duoShoppingUnlockedOutfitIds` にフォールバックする。
購入済み ID は `SaveData.purchasedItemIds` に保存し、ロード時に復元する。
同じ商品が購入済みの場合は再購入せず、所持金も消費しない。
季節衣装は `SpringOutfitItem` / `SummerOutfitItem` / `AutumnOutfitItem` / `WinterOutfitItem` の複数商品として `DuoShoppingCatalog.asset` に登録済み。
各商品は商品データの解放衣装 ID を `SaveData.unlockedOutfitIds` に保存する。
既存の `ShoppingTestItem_01` は互換用の単体テスト商品として残している。
`OutfitManager.CanWearOutfit()` は `isUnlockedByDefault=false` の衣装について、`unlockedOutfitIds` に含まれていれば着用可能とする。
購入で解放された衣装は好感度条件を無視して着用できる方針にする。
未購入の衣装は「好感度不足」ではなく「まだ所持していない」状態として扱う。
春夏秋冬など `isUnlockedByDefault=false` かつ `unlockedOutfitIds` に含まれない衣装は、DressUp の衣装ボタン自体を表示しない方針にする。
`lockedMessage` は好感度やイベント条件など、存在は見えているが条件不足で着られない場合の文言に限定する。
衣装ボタン生成側では、`OutfitManager.IsOutfitVisibleInDressUp()` により未所持衣装を一覧から除外する判定を実装済み。
ショップ UI を拡張する段階では、商品説明をボタン内に長く表示するのではなく、商品一覧と詳細表示を分ける。商品ボタンを選択すると説明用テキストボックスに用途、雰囲気、解放内容、購入条件を表示し、購入確定は別ボタンで行う構成を検討する。商品数が増えたら `ShopItemList` を Scroll View 化し、衣装、消耗品、イベント用アイテムなどのカテゴリ分けやフィルタを追加する。これらは Unity UI の追加配置と Inspector 参照設定が必要になるため、現在は後回しにする。
簡易探索フローは `SoloForest` / `SoloCave` / `SoloLake` / `DuoForest` / `DuoCave` / `DuoLake` を対象にし、予定イベント本文へ探索結果、所持金報酬、HP 変化を追記する。
HP 増減は `PlayerStatus` / `HeroineStatus` の `DamageHp()` / `RecoverHp()` で行い、`BattleStatusData.Clamp()` により 0 から最大 HP の範囲へ丸める。
敵データは `EnemyData` ScriptableObject として用意し、敵 ID、表示名、`BattleStatusData`、報酬所持金、勝利時好感度変化、勝利/敗北メッセージを持つ。
探索用の初期敵アセットは `Assets/Resources/Enemies/` に置き、森は `ForestSlime`、洞窟は `CaveBat`、湖は `LakeSpirit` を使う。
初期バランスは、森は安全でほぼ勝てる、洞窟は HP を削られやすいが報酬高め、湖は回復/好感度寄りで危険度低めにする。
`GameManager.ResolveExplorationEnemy()` は探索予定から敵候補を読み込む。
`GameManager.ResolveSimpleBattle()` は敵とプレイヤー、Duo 探索時はヒロインを含めて最大 20 ターンの自動戦闘を解決する。
勝利時は `EnemyData.rewardMoney` と `affectionChangeOnWin` を反映し、敗北時は HP 1 で撤退する。
戦闘結果の後続メッセージとして、勝利/敗北に応じた戦闘後イベントを表示する。
Duo 探索ではヒロイン同行時の反応文に切り替え、将来の好感度イベント、探索分岐、敗北イベントへ接続する入口にする。
現在の戦闘後イベント文は `GameManager` 内の固定文だが、将来的には `BattleResultEventData` のようなデータへ切り出す。
最初のデータ化単位は、戦闘側の `battleContextId`、勝敗、Solo/Duo の組み合わせとする。
探索予定から発生した戦闘は、`ScheduleType` を直接データへ持たせず、`Forest`、`Cave`、`Lake` などの `battleContextId` に変換して扱う。
データ化前の整理として、固定文生成は勝敗と Solo/Duo から内部イベント種別を解決してから文面を返す形にする。
`BattleResultEventData` は、`BattleResultEventType`、`battleContextId`、メッセージ本文、将来用の `stillId`、`affectionChange`、`unlockedOutfitIds` を持つ。
`GameManager.battleResultEvents` に一致するデータが設定されていればそのメッセージを使い、未設定または空の場合は従来の固定文へフォールバックする。
`battleContextId` が一致するデータを優先し、空の `battleContextId` は Solo/Duo 勝敗だけで使える共通フォールバックデータとして扱う。
`GameManager.battleResultEvents` が未設定の場合は、`Resources/BattleResultEvents` から `BattleResultEventData` を自動読み込みする。
初期データとして `SoloVictory`、`DuoVictory`、`SoloDefeat`、`DuoDefeat` を用意する。
`DuoVictory` はデータ参照確認用に固定文とは異なる文面にし、表示されれば `Resources/BattleResultEvents` のデータが使われていると判断できる。
context 付きデータは、実際に該当する戦闘結果を確認できる条件から追加する。
データ本文では `{heroineName}` を使うと現在のヒロイン名に置換できる。
データ化後は、メッセージ本文だけでなく、スチル表示、好感度変化、衣装解放、追加イベント条件、敗北時専用分岐へ接続できるようにする。
当面はメッセージ本文だけをデータ参照し、スチル表示や好感度変化などの追加効果は次段階で扱う。
専用戦闘 UI の最初の入口として、通常メニューの `DebugBattleAction` から `BattlePanel` を開けるようにする。
このデバッグ戦闘は既存の予定探索とは切り離し、まずは固定敵 `ForestSlime` を相手に、攻撃、敵反撃、逃げる、閉じるを確認する。
デバッグ戦闘中の HP は `PlayerStatus` / `HeroineStatus` / `EnemyData` のコピーを使い、通常プレイ中の実ステータスや予定消費、報酬、戦闘後イベントには反映しない。
`BattlePanel` の UI は Unity 上で手動配置し、初期状態では `panelRoot` を非アクティブにしておく。
`panelRoot`、敵名、敵HP、プレイヤーHP、ヒロインHP、ログ Text、攻撃ボタン、逃げるボタン、閉じるボタンを Inspector で割り当てる。
`GameManager.battlePanel` に配置済みの `BattlePanel` を割り当てると、`OpenDebugBattlePanel` から開ける。
予定探索に組み込むのは、デバッグ戦闘 UI の基本操作と見た目が固まってからにする。
暫定の自動戦闘ログは、探索結果メッセージの後続ページとして敵名、勝敗、ターン数、被ダメージ、現在 HP、報酬、好感度変化を表示する。
戦闘結果サマリと主要行動ログは、1つの戦闘ログページ列にまとめる。
戦闘ログは、見出し 1 行と本文最大 3 行のページに分割して、本文 4 行程度の表示枠を超えないようにする。
バランス確認用の暫定戦闘ログとして、主要行動を探索結果メッセージの後続ページに 3 行ずつ分けて表示する。
戦闘ログの後続ページは `Next` でページ送りし、話者は `戦闘ログ` とする。予定メッセージとは別の専用色で表示し、表示中のスチルや立ち絵状態を変更せず、表情指定がないメッセージでは `GameManager` が表情更新を行わない。
敗北時は探索報酬と戦闘報酬を付与せず、HP 1 で撤退、予定は消費済みであることを表示する。
デバッグ戦闘 UI の次段階では、スキルや防御などのコマンド、詳細ログ、勝敗結果の返却、予定探索への接続、敗北時イベント分岐を扱う。

### 模擬戦闘とスキルシステム

将来的に、主人公とヒロインが互いに戦う模擬戦闘を追加する。
模擬戦闘は探索戦闘や敵との本戦闘とは別枠にし、訓練、関係性イベント、スキル習得、戦闘バランス確認に使う。
模擬戦闘は `BattlePanel` の流用ではなく、訓練専用画面として作る方針にする。
画面はヒロインと一対一で訓練メニューをこなす構成にし、表示されるビジュアルはヒロインの一枚絵を基本にする。
敵キャラ表示や探索戦闘風の敵 HP UI は主役にせず、ヒロインの状態、訓練メニュー、結果ログ、必要なら主人公側の簡易ステータスを表示する。
訓練メニューは、攻撃練習、防御練習、回避練習、連携練習、スキル練習のような選択肢から始める。
内部的にはプレイヤーとヒロインの戦闘ステータスのコピーを使って結果を計算し、終了後に本来の HP を直接減らさない。
ただし、訓練結果として経験値、好感度、訓練用スキル熟練度などを反映する余地は残す。
勝敗の扱いは本戦闘より軽くし、敗北ペナルティではなく会話や成長につなげる方針にする。

訓練開始時は、複数の訓練メニューから 1 つを選ぶ。
各訓練はターンまたはステップ単位で進行し、進めるたびに主人公とヒロインの訓練用 HP が減っていく。
訓練中は途中でやめるボタンを用意し、任意のタイミングで中断できるようにする。
中断時はその時点までの軽い成果だけを反映し、終了条件到達時のボーナスとは分ける。

訓練専用の耐久リソースとして、主人公とヒロインに LP を設定する。
LP は本戦闘では使わず、訓練専用の続行ポイントとして扱う。
訓練中にどちらかの HP が 0 になった場合、その対象に LP が残っていれば LP を 1 消費し、HP を全回復して訓練を続行する。
どちらかの HP が 0 になり、かつその対象の LP も 0 の場合、訓練を終了する。
両者の HP が同時に 0 になった場合は、訓練の噛み合いが良かった扱いとしてボーナスを付ける。
同時 0 の時点で LP が残っている場合は、両者が LP を消費して続行できるが、同時 0 ボーナスはそのステップの成果として記録する。
同時 0 で片方または両方の LP が足りず終了する場合も、終了結果に同時 0 ボーナスを加える。

LP と訓練用 HP は訓練画面内の一時値から始める。
将来、訓練成果やスキルで LP 最大値を伸ばす場合は、通常戦闘 HP とは別の `TrainingStatus` のような保存領域を検討する。
最初の実装では、主人公 LP、ヒロイン LP、訓練用 HP、選択中訓練 ID、経過ステップ数、同時 0 回数、途中中断フラグを持てばよい。
訓練メニューの定義は `TrainingData` ScriptableObject で扱う。
`TrainingData` には `trainingId`、表示名、説明、1 ステップごとの主人公 HP 減少、1 ステップごとのヒロイン HP 減少、初期主人公 LP、初期ヒロイン LP、好感度報酬、訓練熟練度報酬、同時 0 ボーナス、訓練セッションの最大ステップ数を持たせる。最大ステップ数は最初に選択した訓練からセッション開始時に確定し、途中で訓練メニューを切り替えてもリセットまたは延長しない。互換性を考慮して `0` 以下は無制限、正の値はそのステップを処理した時点で通常完了とする。上限到達は途中中断として扱わず、各ステップ報酬に加えて完了報酬とスキルポイントの対象にする。HP / LP による終了と上限到達が同じステップに起きた場合も、そのステップの LP 消費、同時 0、報酬を記録してから一度だけ結果を確定する。UI には「現在ステップ / 最大ステップ」を表示し、無制限時は最大値の代わりに「制限なし」と表示する。
訓練中の一時状態は `TrainingSessionState` で扱い、主人公 HP、ヒロイン HP、主人公 LP、ヒロイン LP、経過ステップ数、同時 0 回数、中断フラグ、終了フラグを持つ。
`TrainingSessionState.AdvanceStep()` は 1 ステップ分の HP 減少、HP 0 時の LP 消費、同時 0 カウント、終了判定をまとめて処理する。
`TrainingPanel` は UI 配置前の接続用スクリプトとして用意する。
`TrainingPanel.Open(IReadOnlyList<TrainingData>, BattleStatusData, BattleStatusData)` で訓練データ一覧と主人公/ヒロインの戦闘ステータスを渡し、訓練ボタン生成、訓練選択、1 ステップ進行、中断、閉じる、HP/LP/結果ログ更新を担当する。
必要な UI 参照は `panelRoot`、`heroineImage`、`trainingListParent`、`trainingButtonPrefab`、`trainingNameText`、`playerHpText`、`heroineHpText`、`playerLpText`、`heroineLpText`、`resultLogText`、`advanceButton`、`quitButton`、`closeButton`、`emptyText`。
初期確認用の訓練データは `Assets/Resources/Training` に `LightPractice`、`SparringPractice`、`EnduranceTraining` を用意する。
`GameManager.OpenTrainingPanel()` から `Resources.LoadAll<TrainingData>("Training")` を読み込み、主人公とヒロインの `BattleStatusData` を渡して `TrainingPanel` を開く。
行動ボタン用に `ActionExecutionType.OpenTrainingPanel` と `TrainingAction` を用意する。
訓練終了結果は `TrainingResult` で扱い、`trainingId`、訓練名、経過ステップ数、同時 0 回数、中断フラグ、終了フラグを持たせる。
`TrainingPanel` は HP/LP 終了時、途中終了時、進行中に閉じた時に一度だけ `GameManager.OnTrainingPanelResult(...)` へ結果を通知する。
`TrainingData.affectionRewardPerStep` は進行ボタンを押して有効な 1 ステップを進めるたびに訓練セッションへ累積する。初期値は全訓練 `1` とし、中断しても進めたステップ分の好感度は反映する。`GameManager.OnTrainingPanelResult(...)` は完了かつ非中断の場合だけ `TrainingData.affectionReward` と同時 0 ボーナスを追加し、中断時は完了報酬を与えない。
訓練結果は `ShowSystemMessage(...)` で画面に表示し、メッセージログにも残す。1 ステップ以上進めた訓練は、完了/中断に関わらず時間を 1 段階進める。
訓練熟練度は `SaveData.trainingProficiencies` に `trainingId` ごとの値として保存する。有効な 1 ステップごとに `trainingProficiencyRewardPerStep` を加算し、初期訓練はすべて `1`。中断してもステップ分は保持し、完了かつ非中断の場合だけ従来の `trainingProficiencyReward` を倍率変更なしで完了ボーナスとして追加する。完了ボーナスは軽い稽古 `1`、実戦形式 `2`、持久訓練 `3`。訓練を途中で切り替えた場合は、各ステップで実際に選択していた `trainingId` へ熟練度を加算する。訓練ごとの熟練度上限は `999999`。熟練度と訓練実績はスキルを直接解放せず、スキルツリーノードの取得条件として評価する。
`TrainingPanel` は訓練ボタンと選択中タイトルに現在の熟練度を表示する。
訓練の最大ステップ数は実装済み。`TrainingData.maxSteps` をセッション開始時に `TrainingSessionState.maxSteps` へ固定し、`TrainingEndReason` で HP / LP 終了、最大ステップ到達、途中終了を区別する。初期3訓練はすべて最大20ステップ。`StepCountText` が配置されていれば専用欄へ、未配置なら訓練名欄へ現在値と上限を表示する。結果メッセージにも終了理由を表示し、最大ステップ到達は通常完了として完了報酬とスキルポイントを付与する。
訓練画面の画像切替を将来追加する。訓練ボタンを押した時点で、現在の訓練画面を開いてから `elapsedSteps == 0` なら開始前画像、`elapsedSteps > 0` なら進行後画像を表示する。途中で訓練を切り替えてもステップ数はリセットしない。ステップ実行時に主人公だけ、ヒロインだけ、双方同時のいずれかで LP を消費した場合は、それぞれ別の画像へ切り替える。同時消費を個別消費より優先し、次の通常ステップでは進行後画像へ戻す。判定は累計実績ではなく、そのステップ直前・直後の LP 差分を使う。
画像は共通 `TrainingData` に直接持たせず、ヒロイン別 `HeroineTrainingImageData` で `trainingId` と表示状態を Sprite に対応させ、既存の `TrainingPanel.heroineImage` を更新する。訓練別画像、状態別共通画像、現在画像の順にフォールバックし、未設定や参照切れでも訓練処理を停止しない。画像状態、最初に作る9枚、AssetToolの `usage = Training` と `training_images_export.json` は `Docs/Extra_FantasyLoveSimAssetTool/TrainingImagePlan.md` を正とする。これまでのUnity変更をToolへ同期する範囲と順序は `CurrentFeatureSyncPlan.md` にまとめる。この機能は現時点では未実装である。
訓練メニューの追加解放は、熟練度や日数だけで自動的に段階変化させる方式ではなく、スキルツリーで取得する「訓練解放ノード」の効果として扱う将来案とする。この機能の実装は後回しとし、当面の初期訓練は従来どおり利用可能にする。実装時は `SkillTreeNodeData` に `unlockedTrainingIds` のような解放対象を持たせ、戦闘・訓練補正用の `SkillData` を必須にせず、訓練を解放すること自体をノード取得効果として表現する。解放状態は新しい保存リストを正本にせず、取得済み主人公・ヒロインノード ID から導出する。ヒロインノードの場合は `targetHeroineId` と現在ヒロインを照合し、そのヒロインでのみ有効な解放を表現できるようにする。
`TrainingPanel` は初期解放訓練と取得済みノードが解放した訓練だけを選択可能にし、未解放項目は非表示または「条件未達」の無効表示から選べる設計にする。スキルツリー詳細には「取得すると○○訓練を解放」と表示する。`SkillTreeDataValidator` は存在しない・重複した訓練 ID、同一ノード内の重複指定を検出する。さらに、訓練解放ノードの取得条件へ、そのノードで初めて解放される訓練の熟練度や実績を要求すると取得不能になるため、前提ノードを含む解放経路を考慮した循環・到達不能条件を警告対象にする。ノードや対象訓練が削除された場合は不正 ID を安全に無視し、例外で画面を停止させない。
シーン配置の細かい見た目調整も後回しとする。

限定対象の確認用スキルとして、主人公「実戦感覚」は `Combat` カテゴリーだけでステップ熟練度を1増加し、「集中訓練」の取得、実戦カテゴリーの訓練回数10回、2 SPを条件にする。ヒロイン「持久の支え」は `EnduranceTraining` だけでヒロインHP消費を1軽減し、「応援」の取得、持久訓練10回、2 SPを条件にして DefaultHeroine / TestHeroine の両ツリーへ追加する。
訓練終了結果には、セッション全体で実際に軽減した主人公・ヒロインのHP消費、スキルによる好感度・熟練度の増減、効果を発揮したスキル名を表示する。途中で訓練を切り替えた場合も各ステップの実適用分だけを集計する。会話欄とメッセージログへ保存する結果本文は5行以内の共通要約とし、長い訓練名・発動スキル名は先頭部分と残件数へ省略して折り返しを抑える。`TrainingPanel` の結果ログは登録件数ではなく TextMesh Pro の折り返し後の実表示行数を測り、`maxLogLines` を超えた古い行を削除し、単一項目が上限を超える場合も `maxVisibleLines` と `Truncate` で領域外表示を防ぐ。

訓練用スキルは、訓練開始前に各1個を選ぶ方式ではなく、取得済みかつ有効に設定している主人公・ヒロインの訓練スキルをすべて適用する。有効状態は所有者別に保存し、同じスキル ID は1回だけ適用する。複数スキルの固定値補正は合計してから最終値を制限し、元のステップ HP 消費が1以上なら、軽減後も主人公・ヒロインとも最低1を消費する。元の消費が0の対象をスキルによって1へ増やすことはしない。これにより、軽減スキルを重ねても HP / LP による訓練終了条件を完全には無効化しない。好感度と熟練度の補正結果は最低0とし、訓練ログには各スキルを毎行表示せず、そのステップへ適用した補正合計をまとめて表示する。
訓練スキルの有効状態管理とステップ効果は実装済み。主人公は `SaveData.activePlayerTrainingSkillIds`、ヒロインは `heroineId` ごとの `SaveData.heroineTrainingSkillActivations` に保存し、取得済みで有効な `SkillData` だけを維持する。スキルツリーの取得済み訓練ノードでは既存ボタンを「有効にする／無効にする」として使い、有効数に上限は設けない。セーブバージョン15以前は取得済み訓練スキルをすべて自動的に有効化し、バージョン16以降は無効にした状態も維持する。`SkillData` の `trainingPlayerHpCostReduction`、`trainingHeroineHpCostReduction`、`trainingAffectionRewardModifier`、`trainingProficiencyRewardModifier` を訓練開始時に有効スキル間で合計し、各ステップの実消費・実報酬と訓練別熟練度へ反映する。同一スキル ID は主人公・ヒロイン間でも一度だけ集計する。`trainingApplicationScope` は全訓練、カテゴリー指定、訓練ID指定から選び、後者2つは `trainingApplicationTargetId` に対象IDを設定する。訓練を選択・切替するたびに対象スキルだけで補正を再集計し、対象外スキルはプレビューにも表示しない。`SkillTreeDataValidator` は対象カテゴリー・訓練IDの存在も検証する。`TrainingPanel` は訓練選択時に主人公・ヒロインの有効スキル名、補正後のHP消費予定値、好感度・熟練度の予定値を既存ログへ表示し、プレビューと実処理で同じ計算を使用する。初期スキルは主人公「深呼吸」が主人公HP消費を1、DefaultHeroine / TestHeroine「呼吸合わせ」がヒロインHP消費を1軽減する。派生スキルは主人公「集中訓練」がステップ熟練度を1、各ヒロインの「応援」がステップ好感度を1増加する。派生ノードはそれぞれ初期スキルの取得、累計訓練5回、2 SPを条件にする。既存4スキルは全訓練対象とする。

スキル取得は、訓練などで得たスキルポイントをスキルツリー上で消費して取得する。熟練度や各種実績はスキルを直接取得させるものではなく、ツリーノードを購入可能にする解放条件として利用する。従来の条件達成による自動解放は廃止済み。
スキル解放条件に利用するため、累計訓練回数、訓練中に主人公の LP が減った回数、訓練相手の LP を減らした回数、モンスター撃破数を永続的に記録する。訓練回数は、訓練を 1 ステップ以上進めて結果を確定した時点で 1 回として数え、途中終了も回数には含める。LP の減少回数は失った LP のポイント数ではなく、HP 0 により LP 消費が発生した回数として数える。同時に両者が LP を消費した場合は、主人公側と相手側をそれぞれ 1 回ずつ加算する。モンスター撃破数は勝利が確定して報酬処理へ進んだ敵のみを数え、逃走、敗北、デバッグ上の未確定結果、訓練相手は含めない。
どの訓練で実績を達成したか判定できるよう、全体累計だけでなく `trainingId` ごとにも、訓練回数、主人公 LP 消費回数、相手 LP 消費回数を記録する。さらに `TrainingData` に `trainingCategoryId` または `TrainingCategory` を追加し、攻撃、防御、持久、連携などの訓練カテゴリーを定義する。同じカテゴリーに属する全訓練についても、訓練回数、主人公 LP 消費回数、相手 LP 消費回数をカテゴリー別に集計する。1 回の結果確定時には、全体、選択中 `trainingId`、所属カテゴリーの各カウンターを同時に更新する。
保存領域は、全体集計を持つ `SkillProgressStats` のような構造を `SaveData` に追加し、`totalTrainingCount`、`playerLpConsumedCount`、`opponentLpConsumedCount`、`totalMonsterDefeatCount` を保持する方針にする。加えて、`trainingId` 別と `trainingCategoryId` 別に `trainingCount`、`playerLpConsumedCount`、`opponentLpConsumedCount` を持つ集計リストを保存する。将来「特定の敵や種族を何体倒したか」を条件にできるよう、敵 ID、モンスター分類 ID ごとのカウンターも追加できる構成にする。ロード互換性のため、未保存のカウンターと新設カテゴリーは 0 として扱う。カテゴリー ID は表示名と分離した変更されにくい識別子を使う。
スキルツリーのノード定義は戦闘効果を持つ `SkillData` から分離し、`SkillTreeNodeData` のような取得条件用データに、必要スキルポイント、前提ノード、必要熟練度、必要訓練回数、主人公 LP 消費回数、相手 LP 消費回数、モンスター撃破数などを持たせる。訓練実績条件には集計範囲として「全訓練」「特定の `trainingId`」「特定の `trainingCategoryId`」を指定できるようにし、例えば「防御カテゴリーを 10 回」「持久訓練で自分の LP を 5 回消費」「攻撃訓練で相手の LP を 3 回消費させる」を表現可能にする。複数条件は基本的にすべて達成する AND 条件とし、将来 OR 条件が必要になった場合は条件グループとして明示的に追加する。UI では条件の対象訓練またはカテゴリー名と現在値を「防御訓練 3 / 10」のように表示し、条件達成済みでもポイント不足なら取得できないことが分かるようにする。
状態確認 UI からも `SkillProgressStats` の訓練・戦闘実績を閲覧できるようにする。`StatusDetailPanel` の基本能力表示へ全件を常時並べず、「実績」タブまたは実績詳細パネルへ分離し、全体集計、訓練カテゴリー別、訓練 ID 別、モンスター撃破実績の順に確認できる構成を基本とする。全体には累計訓練回数、主人公 LP 消費回数、相手 LP 消費回数、総モンスター撃破数を表示し、カテゴリーや個別訓練、敵別の内訳は折りたたみまたは切り替え表示にする。値は保存済み集計から読み出し、訓練・戦闘結果確定後とセーブデータのロード後に更新する。未記録項目は 0 として表示し、将来はスキル解放条件の現在値確認画面から同じ集計表示へ移動できるようにする。
実績集計基盤は実装済み。`TrainingData.trainingCategoryId`、`SaveData.skillProgressStats`、全体・訓練 ID 別・カテゴリー別の訓練回数と双方の LP 消費回数、全体・敵 ID 別のモンスター撃破数を保存する。訓練中にメニューを切り替えた場合も、各ステップで使用した訓練 ID とカテゴリーへ LP 消費を記録する。全体訓練回数は 1 セッションにつき 1 回、訓練 ID とカテゴリー別の回数は、そのセッションで 1 ステップ以上実行した対象ごとに各 1 回加算する。初期カテゴリーは `Fundamentals`、`Combat`、`Endurance`。予定戦闘の勝利時だけ撃破数を加算し、デバッグ戦闘、逃走、敗北は除外する。`GameManager.GetSkillProgressStats()` から表示用コピーを取得でき、集計時は `[SkillProgress]` ログを出力する。
状態確認 UI のコード接続として `StatusProgressPanel` を追加済み。全体、カテゴリー別、訓練別、敵別の 4 表示をボタンで切り替え、訓練名と敵名は Resources 内のデータから表示名を解決する。`StatusDetailPanel.progressButton` から開き、`progressPanel` は状態詳細の子に置けば非アクティブ状態でも自動検出できる。`MainScene` には `StatusProgressPanel`、タイトル Text、スクロール可能な本文 Text、全体・カテゴリー・訓練・敵の切り替え Button、閉じる Button を配置し、Inspector 参照を割り当て済み。
スキルポイント基盤は実装済み。主人公とヒロインのポイントを `SaveData.playerSkillPoints` / `heroineSkillPoints` に分けて保存し、旧セーブでは 0 として扱う。`TrainingData.playerSkillPointReward` / `heroineSkillPointReward` を完了かつ非中断の訓練結果にだけ加算し、結果メッセージと `StatusProgressPanel` の全体表示へ現在値を出す。初期報酬は軽い稽古が双方 1、実戦形式と持久訓練が双方 2。スキルツリーは `GameManager.PlayerSkillPoints` / `HeroineSkillPoints` を参照し、`TrySpendPlayerSkillPoints(...)` / `TrySpendHeroineSkillPoints(...)` で不足・不正値を防いで消費する。`EnemyData.playerSkillPointReward` / `heroineSkillPointReward` は予定探索の勝利時だけ付与し、敗北、逃走、デバッグ戦闘では付与しない。森スライムと湖の精霊は双方1、洞窟コウモリは双方2。通常の `BattlePanel` とフォールバックの簡易戦闘の両経路で、獲得量と現在値を結果へ表示する。
スキルツリーノードのデータ基盤は実装済み。`SkillTreeNodeData` は固定 `nodeId`、表示名、`Player` / `Heroine` の所有者、主人公用 `SkillData`、ヒロイン用 `targetHeroineId` / `grantedHeroineSkillId`、必要スキルポイント、前提ノード、条件一覧、将来の UI 用 `treePosition` を持つ。条件は訓練熟練度、訓練回数、主人公 LP 消費回数、相手 LP 消費回数、モンスター撃破数、好感度、日数に対応し、集計範囲として全体、訓練 ID、訓練カテゴリー ID、敵 ID を指定できる。条件はすべて AND で評価する。
`GameManager.GetSkillTreeNodes()` は `Assets/Resources/SkillTreeNodes` からノードを読み、`EvaluateSkillTreeNode(...)` は `Locked` / `Available` / `InsufficientPoints` / `Acquired` と、条件ごとの現在値・必要値、未取得の前提ノード ID を返す。`TryAcquireSkillTreeNode(...)` は取得直前に再評価し、所有者側のポイントを安全に消費して取得済みノード ID を保存する。主人公ノードは対応する既存スキル ID も解放するが、ヒロインノードは主人公用 `unlockedSkillIds` へ混入させない。取得済みノードは `SaveData.acquiredPlayerSkillTreeNodeIds` / `acquiredHeroineSkillTreeNodeIds` に分けて保存する。起動時とロード時には取得済み主人公ノードから使用可能スキルを再構築する。
初期主人公ノードは `PowerStrike`、`GuardStance`、`FirstAid` が各 1 ポイント、`BattleFocus` と `ArmorBreak` が各 2 ポイント。`PowerStrike` から `BattleFocus`、`GuardStance` から `FirstAid` と `ArmorBreak` へ分岐する。上位条件には訓練熟練度だけでなく、総モンスター撃破数、実戦カテゴリーで相手の LP を減らした回数、主人公の LP 消費回数を使用する。`SkillTreePanel` は主人公・ヒロインの切替、所持ポイント、ノード状態、前提ノード、条件進捗、取得操作に対応済み。通常メニューのスキル導線はこの画面を開き、既存 `SkillPanel` は戦闘選択のフォールバックとして残す。取得済み主人公ノードを使用可能スキルの正本とし、従来の自動解放は廃止済み。ヒロイン側は現在の `HeroineProfileData.heroineId` と一致するノードだけを表示・取得でき、取得済みノードの `grantedHeroineSkillId` と一致する `HeroineBattleSkillData` だけを戦闘の自動行動候補にする。未取得時は通常攻撃へフォールバックする。DefaultHeroine は `RadiantSlash` から `GentlePrayer` / `ProtectionBlessing`、TestHeroine は `SharpThrust` から `ReluctantAid` / `GuardedStance` へ分岐する。
`SkillTreePanel` は `SkillTreeNodeData.treePosition` に従ってノードを二次元配置し、表示中ノード同士の前提関係を接続線で描画する。表示範囲はノード座標から自動計算し、必要な場合だけ縦横スクロールを有効にする。ノードは未解放を灰色、ポイント不足を黄色、取得可能を緑、取得済みを青で表示し、接続線も接続先の状態に合わせる。選択中ノードには白い Outline を付け、詳細欄には取得可否と不足理由、取得結果を表示する。条件対象の訓練 ID、カテゴリー ID、敵 ID は可能な限り表示名へ解決し、ヒロインタブのポイント表示には現在ヒロイン名を併記する。接続線は実行時に生成するため、専用 Prefab や追加の Scene UI 部品は不要。
スキルツリーデータ検証は `SkillTreeDataValidator` に集約する。空または重複したノード ID、前提ノードの Missing・重複・自己参照・循環参照、所有者や対象ヒロインをまたぐ前提関係、存在しない主人公・ヒロインスキル、訓練 ID・カテゴリー ID・敵 ID、重複条件、同一ツリー内の座標重複を検出する。Unity Editor の `FantasyLoveSim > Validate Skill Tree Data` から任意に実行でき、Editor Play または Development Build の起動時にも一度検証する。結果は `[SkillTreeValidation]` ログへノード ID とともに出力し、通常リリースビルドでは起動時検証を省略する。
好感度は小数型を使わず整数で管理し、上限を `9999` とする。従来の好感度、増減値、条件値はすべて10倍へ移行し、従来の100相当を1000とする。ランク境界は200、400、600、800、1000。`HeroineStatus.endingUnlockAffection` は1000とし、`maxAffection` から分離する。従来の `maxAffection = 100` は当時の全体上限を表していたため、通常会話や行動が1000以降で消えないよう新しい既定値は9999とする。1000以降は新しい解放条件を必須とせず、好感度の累積値として9999まで増加できる。現在のセーブバージョンは16とし、旧尺度の好感度・熟練度セーブとの互換性は保証しない。

スキルシステムは、現在の `StatusAbilityData` とは別の `SkillData` 系 ScriptableObject として拡張する。
`StatusAbilityData` は画面機能や衣装確認モードなどの能力解放に使い、戦闘・訓練で選択する技や効果はスキルとして分ける。
スキルは次のカテゴリに分ける。

| カテゴリ | 用途 |
| --- | --- |
| 汎用スキル | 探索、会話、買い物、イベント条件、ステータス補正など、戦闘以外でも使える効果 |
| 戦闘用スキル | 攻撃、防御、回復、バフ、デバフ、逃走補助など、敵との戦闘や模擬戦闘で使うコマンド |
| 訓練用スキル | 主人公とヒロインの模擬戦闘、稽古、成長イベント、熟練度上げに使う効果 |

`SkillData`、`SkillCategory`、`SkillEffectType`、`SkillTargetType` は追加済み。
スキルデータには、`skillId`、表示名、カテゴリ、説明、消費コスト、対象、効果種別、威力または回復量、解放条件、使用可能な戦闘種別を持たせる。
初期確認用のスキルデータは `Assets/Resources/Skills` に `PowerStrike`、`GuardStance`、`FirstAid` を用意する。
熟練度尺度の10倍化後は、`PowerStrike` が `LightPractice` 30、`GuardStance` が `EnduranceTraining` 30、`FirstAid` が `SparringPractice` 50を解放条件にする。`BattleFocus` は `LightPractice` 50、`ArmorBreak` は `EnduranceTraining` 50とする。
戦闘用スキルは `BattlePanel` のコマンドとして表示し、汎用スキルはステータスやイベント条件で参照できるようにする。
訓練用スキルは模擬戦闘で優先的に使い、勝敗だけでなく「うまく防げた」「連携できた」などの訓練結果に接続する。
使用可能スキル ID は `SaveData.unlockedSkillIds` に派生情報として保存し、取得済み主人公ノードから起動・ロード時に再構築する。参照には `GameManager.IsSkillUnlocked(...)` / `GetUnlockedSkillIds()` を使い、外部から直接解放しない。
主人公の戦闘スキル装備は4枠とし、`SaveData.equippedPlayerBattleSkillIds` に順序付きで保存する。スキルツリーで主人公スキルを取得したときは空き枠があればそのスキルだけを自動装備し、枠が埋まっている場合は既存装備を変更しない。ロード時は未取得、無効、戦闘使用不可、重複、5件目以降の ID を除去する。セーブバージョン13以前は取得済み戦闘スキルを最大4件まで自動装備し、バージョン14以降は空の装備枠もプレイヤーの選択として維持する。将来スキル熟練度が必要になった場合は `skillProficiencies` のような保存領域を別途追加する。
ヒロインの戦闘スキル編成はヒロインごとに3枠とし、`SaveData.heroineBattleSkillLoadouts` に `heroineId` と順序付きスキル ID を保存する。スキルツリーでヒロインスキルを取得したときは、対象ヒロインの空き枠があれば取得したスキルだけを自動編成する。ロード時は対象プロフィールに存在しないスキル、未取得スキル、重複、4件目以降を除去する。セーブバージョン14以前は取得済みスキルをプロフィールの定義順で最大3件まで自動編成し、バージョン15以降は空の編成も選択結果として維持する。

スキル取得と装備は、`StatusDetailPanel` に直接詰め込まず、別の `SkillPanel` として扱う。
`SkillPanel` は実装済みで、全スキルの未取得/取得済み状態、説明、解放条件を表示し、戦闘中は解放済みの戦闘用スキルを選択できる。`PowerStrike`（Damage）、`GuardStance`（Guard）、`FirstAid`（Heal）は `BattlePanel` で実行できる。
UI は Canvas 配下へ手動配置し、`panelRoot`、一覧親、ボタン Prefab、必要に応じてタイトル・説明・閉じるボタンを Inspector で割り当てる。通常一覧はヒロイン別 `Actions/SkillAction.asset` の `ActionExecutionType.OpenSkillPanel` から開く。戦闘中は `BattleSkillPanel` を開き、現在 MP、スキル一覧、選択中説明、使用/戻るを表示する。`BattleSkillPanel` が未配置の場合だけ従来の `SkillPanel` を戦闘選択にフォールバックする。
`BattleStatusData` は HP に加えて MP を持つ。MP は戦闘開始時に最大値まで回復する戦闘内リソースで、`SkillData.cost` を使うと減少する。`SkillEffectType.Buff` / `Debuff` は `SkillData.affectedStat`（Attack / Defense / Speed）を `statusDurationTurns` の対象ターン数だけ増減する。効果の付与、残りターン、解除は戦闘ログに表示する。初期データとして `BattleFocus`（攻撃 Buff）と `ArmorBreak`（敵防御 Debuff）を追加済み。
敵側も同じ `BattleStatusData` の MP を使う。敵スキルは `EnemyData.battleSkills` の `EnemyBattleSkillData` で定義し、MP が足りる候補から `useChancePercent` と `priority` により選ばれる。`maxUsesPerBattle` で同じ補助スキルの連続使用を防げる。敵 Speed がプレイヤー Speed より 4 以上高い場合は、30% の確率で敵が追加行動する。初期敵では ForestSlime が酸液吹きと硬質化、CaveBat が急降下攻撃と翼の加護、LakeSpirit が水弾と冷気の霧を使う。
`BattlePanel` では主人公・ヒロイン・敵の MP を HP と並べて常時表示する。Buff / Debuff の詳細は常時表示せず、`StatusButton` から開く `BattleStatusEffectPanel` に対象ごとの増減値と残りターンを一覧表示する。行は `StatusEffectRowPrefab` から生成し、Buff と Debuff を色分けする。主人公の戦闘スキル一覧には装備中スキルだけを表示する。ヒロイン用スキルは `HeroineProfileData.battleSkills` でキャラクター別に定義し、編成中のスキルだけを通常攻撃の代わりに自動使用する。既存の MP、使用確率、優先度、最大使用回数、対象条件は維持し、使用可能な編成スキルがなければ通常攻撃へ戻る。初期データには攻撃、HP が減った味方への回復、主人公への防御 Buff を用意する。編成操作には `SkillTreePanel` の取得ボタンを兼用し、取得済みヒロインノードでは「編成する」または「外す」と編成数を表示するため、追加の Scene UI は不要。カテゴリタブと MP 回復アイテムは次段階で追加する。

実装順は、まずスキルデータ定義と表示だけを作り、次に戦闘用スキルを `BattlePanel` へ接続し、その後に訓練専用画面、模擬戦闘、訓練用スキルを扱う。
汎用スキルはイベント条件やステータス補正への影響範囲が広いため、戦闘用スキルの動作が固まってから段階的に接続する。

## 実装メモ

- `GameManager` は状態管理と UI 反映をまとめている
- `BackgroundZoom` は会話開始時の演出用
- 行動の一部は `OpenConversationGenres` のように会話導線へ分岐する
- 予定パネルは `OpenSchedulePanel` で開く
- 予定パネルは戻るボタンで閉じる運用にしている
- 衣装反応は `OpenOutfitReactionPanel` で専用パネルに切り替える
- `Next` ボタンは会話結果、行動結果、選択肢表示の進行を兼ねる
- セーブデータは複数スロット対応済みで、`slot 0` は従来の `save.json` を使う
- セーブロード UI は `Assets/Prefabs/SaveLoadPanel.prefab` として作成済み
- `TitleScene` と `MainScene` の `SaveManager.saveSlotCount` は `4`
- 今後は `SaveData` に `heroineId` を保存し、セーブデータをキャラクター個別に扱う。タイトルで選んだキャラクターは新規ゲーム開始時だけ使い、ロード時は保存済みの `heroineId` を優先する
- セーブ/ロード画面のサムネイルは実装済み。保存時にセーブ画面を開く直前のスクリーンショットを縮小保存し、スロットサムネイルとして表示する。画像は `Application.persistentDataPath` に保存し、git には含めない
- タイトル画面にはギャラリーモードを追加し、キャラクター選択から対象ヒロインの解放済みスチル一覧へ移動できるようにする
- `TitleScene` の `ContinueButton` は `SaveLoadPanel.OpenLoad()` でロード用スロット選択を開く
- `MainScene` の `SaveButton` / `LoadButton` は `SaveLoadPanel.OpenSave()` / `OpenLoad()` でスロット選択を開く
- `SaveLoadPanel` はセーブ時に青背景・`セーブ`、ロード時にオレンジ背景・`ロード` に切り替える
- `MessageLogPanel` / `StillGalleryPanel` / `ShopPanel` / `SaveLoadPanel` は、非アクティブ状態から初回表示したときに `Awake()` が `Open()` を打ち消さないよう、`Awake()` 内で panel root を閉じない方針に統一済み
- 保存済みスロットは `Day` と `Affection` をラベルに表示する
- `MainScene` でロードした後は `SaveLoadPanel` を閉じる
- セーブ/ロード回帰確認では、好感度、日付、時間帯、曜日、季節、天気、現在衣装、衣装評価、今日/明日の予定、当日予定イベントの発動済み状態、能力取得状態、スキル取得状態、衣装確認モードの解放状態と現在の使用モードを確認する
- 翌朝メッセージキューと将来の汎用ログはセーブ対象外の方針なので、ロード後に復元されなくてよい

### 案2: `ScheduleType -> ScheduledEventData` 変換表

これは「予定を翌日に自動実行する」ための仕組みで、現在は準備フェーズ付きで実装済み。
予定イベントは `Assets/Resources/Heroines/<HeroineId>/ScheduledEvents/` の `ScheduledEventData` アセットで管理する。
`HeroineProfileData.scheduledEventResourcePath` を優先して読み、該当 `ScheduleType` がない場合だけ `Assets/Resources/ScheduledEvents/` の共通データをフォールバックとして使う。
`ActionId` は既存の `ActionData` と分けて、予約実行専用の内部 ID として扱っている。
予定イベント本文の話者は `eventSpeakerType` で `Heroine` / `System` / `Schedule` / `Outfit` から選べる。
翌朝は予定の準備メッセージだけを表示し、イベント本体は `triggerTimeSlot` に到達した時点で発動する。
イベント直前には、必要に応じて `このまま出発` / `着替える` を選べる。

| `ScheduleType` | `ActionId` | 現在の発動時間 | 用途 |
| ---- | ---- | ---- | ---- |
| `None` | `None` | なし | 自動実行しない |
| `SoloForest` | `AutoWalkForest` | 昼 | 森への散歩や探索 |
| `SoloCave` | `AutoWalkCave` | 昼 | 洞窟への探索 |
| `SoloLake` | `AutoWalkLake` | 昼 | 湖への散歩 |
| `SoloShopping` | `AutoWalkShopping` | 昼 | 街への買い物や外出 |
| `DuoForest` | `AutoDuoForest` | 昼 | 二人で森林デート |
| `DuoCave` | `AutoDuoCave` | 昼 | 二人で洞窟デート |
| `DuoLake` | `AutoDuoLake` | 昼 | 二人で湖デート |
| `DuoShopping` | `AutoDuoShopping` | 昼 | 二人で買い物デート |
| `StayHome` | `AutoStayHome` | 昼 | 家で過ごす日 |

この方式では、翌日の開始時に `ScheduleType` を見て準備メッセージを表示し、指定時間帯にイベント本体へ変換する。
現在の実装は次の構成。

1. `ScheduledEventData` を Resources から読み込む
2. 翌日開始時に準備メッセージを表示する
3. `triggerTimeSlot` に到達したら衣装確認を挟む
4. 発動済み状態をセーブデータに保存し、ロード後の二重発火を避ける
5. アセットがない予定はコード内の既定定義へフォールバックする
6. 今後は `triggerTimeSlot` を昼・夜で分けたり、予定ごとの専用演出を追加できる
7. 複数メッセージが同時に発生したら、話者付きキューで管理し、`Next` で 1 件ずつ読めるようにする
8. 衣装確認モードの解放条件と現在の使用モードは、プレイヤー側の便利機能として `GameManager.playerOutfitPromptAbilities` で管理する
9. 話者タイプごとに表示色を変え、同じメッセージボックス内でも発話種別が分かるようにする
10. 汎用ログ画面は実装済み。セッション中の直近ログだけを保持し、対象は会話、行動結果、予定、衣装通知とする。話者名とメッセージを `MessageLogPanel` に表示し、セーブデータには含めない

ログ画面をスクロールビューで表示する場合は、Unity 上で `MessageLogPanel > Scroll View > Viewport > MessageLogList` の階層を作り、`ScrollRect.content` に `MessageLogList` を指定する。
`MessageLogList` の中身は実行時に `MessageLogRowPrefab` から生成されるため、編集時点では空でよい。
メッセージログ 1 行の高さは、`MessageLogRowPrefab` ルートの `LayoutElement.preferredHeight` が基準になる。
`DialogueText` の RectTransform 高さだけを広げても、親行の `preferredHeight` が 200 のままだと表示領域は広がらない。
ログ行を広げる場合は、`MessageLogRowPrefab` ルートの `Preferred Height` と `DialogueText` の高さをセットで調整する。
`MainScene` 側に `MessageLogRowPrefab` の prefab override が残っている場合は、prefab 側の変更だけでは反映されないため、override を同じ値にするか Apply/Revert で揃える。
朝開始メッセージ、イベント会話、戦闘ログなどの会話シーケンス中は、Save/Load を開かない。
会話中に Save/Load の通常メッセージを表示すると、表示中の会話キューを消して進行不能になるため、ブロック時は通常メッセージを出さず `Debug.LogWarning` に留める。
Save/Load ボタンは会話中に `interactable=false` へ切り替え、無効時の色を `GameManager.saveLoadDisabledColor` で表示する。
Save/Load ボタン配下の `TMP_Text` も無効時は `GameManager.saveLoadDisabledTextColor` に切り替え、有効時は起動時に保持した元の文字色へ戻す。
同じ理由で、会話シーケンス中に `ShopPanel`、`StatusDetailPanel`、`MessageLogPanel`、`StillGalleryPanel` などの操作パネルを開く場合も、会話キューを壊さないか確認する。
新しいパネルやボタンを追加するときは、`CanOpenSaveLoadPanel()` と同種のガードを用意するか、会話中に開いても `ShowDialogue()` / `ShowSystemDialogue()` を呼ばない非破壊 UI にする。
ブロック理由をプレイヤーへ表示したい場合も通常メッセージボックスは使わず、`Debug.LogWarning`、ボタンの無効表示、または会話キューに触れない専用通知 UI を使う。
Unity Editor 上で UI を手作業変更した後は、必ず `Ctrl+S` でシーン保存してから Codex にコードや scene patch を依頼する。
未保存の UI 変更はディスク上の `MainScene.unity` に存在しないため、Codex 側でシーンを編集したときに `Scroll View` / `Viewport` が消えたように見える原因になる。

## 操作・エンディング

### エンディングシーン

Ending ボタンを押すと、専用の `EndingScene` へ遷移する。
`EndingScene` には `EndingManager` を配置し、`HeroineProfileData.endingResourcePath` の `EndingData` からエンディング用テキストとスチルを表示した後に `TitleButton` から `TitleScene` へ戻れるようにする。
初期データは `GoodEnding.asset` で、スチル画像ができたら `stillSprite` に割り当てる。
シーンと表示データは分離済みで、複数エンディングが必要になった場合も `EndingData` を追加する方針にする。

エンディングを増やす場合は、対象ヒロインの `endingResourcePath` 配下に `EndingData` アセットを追加する。
`endingId` はセーブや分岐で参照する可能性があるため、ファイル名と一致させ、本番投入後は変更しない。
設定項目は `displayName`、`message`、`stillSprite`、`requiredAffection`、`requiredShownEventIds` を基本にする。
`GameManager` は `HeroineProfileData.endingResourcePath` から条件に合う `EndingData` を選ぶ。
複数エンディングが同時に条件を満たす場合は、`requiredAffection` が高いものを優先する。
条件に合う `EndingData` がない場合は `defaultEndingId` を使う。

### メッセージのクリック進行

複数メッセージを読むときは、`Next` ボタンまたはメッセージ表示ウィンドウのクリックで次のメッセージへ進む。
選択肢、Save/Load、メッセージログ、ステータス詳細などの UI 操作と競合しないよう、クリック進行はメッセージ表示ウィンドウに限定する。
`DialogueClickAdvanceArea` をメッセージ表示ウィンドウの Panel などに追加し、`GameManager.dialogueClickAdvanceArea` に割り当てると、`Next` ボタンが表示中かつ押せる状態のときだけクリックで次へ進む。
クリック対象の Image は `Raycast Target` を有効にする必要がある。
この操作はプレイヤーの好みによって必要性が変わるため、オプション画面を追加して ON/OFF を切り替えられるようにする案を残す。

## 優先度の高い改善候補

1. 行動の反応パターン追加
2. 会話データの整理と命名規則の統一
3. エンディングデータと条件分岐パターンの追加
4. 立ち絵切り替えと表情差分の整理
5. セーブ/ロード回帰確認とUI補強
6. UI の視認性改善
7. メッセージ表示ウィンドウのクリック進行 ON/OFF オプション
8. 訓練、戦闘、ショップのデータ追加と調整

## 補足

- 旧文書に残っていた `SampleScene` や `Assets/Data/Conversations/` は現在の構成とずれているので、今後は `MainScene` と `Assets/Resources/...` を基準にする
- 実装済みの機能を増やす時は、まずデータ追加で済むかを優先して考える
