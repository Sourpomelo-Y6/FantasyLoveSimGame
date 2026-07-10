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
| 好感度 | 0〜100 |
| 行動HP効果 | `ActionData` / `ActionReactionData` の `playerHpChange` と `heroineHpChange` で HP を増減できる。`休む` はプレイヤーとヒロインの HP を 20 回復する |
| 背景切り替え | 時間帯・天候に応じて背景 Sprite を切り替え |
| ゲームイベント | `GameStart` / `DayStart` / `Manual` の汎用イベント |
| スチル回想 | 解放済み・未解放スチルを一覧表示 |
| メッセージログ | セッション中の直近メッセージを表示 |
| タイトルキャラクター選択 | `Resources.LoadAll<HeroineProfileData>("Heroines")` で候補を列挙し、新規ゲーム開始時のヒロインを選べる |
| 買い物 | `DuoShopping` 予定から `ShopPanel` を開き、所持金消費、購入済み保存、衣装解放を行う |
| 探索・戦闘 | 森、洞窟、湖の探索で敵候補を解決し、簡易戦闘または `BattlePanel` に接続できる |
| 訓練・スキル | `TrainingPanel` で訓練を進め、好感度報酬と訓練熟練度を保存する。条件達成スキルを自動解放し、`SkillPanel` と `BattlePanel` で使える |
| エンディング | 好感度100で `EndingScene` に遷移し、条件一致する `EndingData` を表示 |

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

条件判定は `GameManager.CanStartGameEvent(GameEventData gameEvent)` に集約し、`GetGameEventsForTrigger()` と `TryStartManualGameEvent()` の両方から使う。
既存の `GameStartIntro` と `TestManualEvent` は条件未設定なら今まで通り発生するようにし、既存データの互換を壊さない。
条件フィールドを追加した後も、`showOnce` は既読管理、条件フィールドは発生可否という役割分担にする。
将来、予定やスチル解放状態を条件にしたくなった場合は、同じ `CanStartGameEvent` に条件を追加していく。
衣装条件は現在の `OutfitManager.CurrentOutfit.outfitId` を見て判定する。
文字列ID欄の `requiredOutfitIds` / `blockedOutfitIds` も残しているが、通常は Unity Inspector で `OutfitData` アセットを選べる `requiredOutfits` / `blockedOutfits` を使う。
衣装の種類や属性ではなく、実際に着ている衣装を直接指定する方が、表示するスチルとの対応を崩しにくい。

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
`TrainingData` には `trainingId`、表示名、説明、1 ステップごとの主人公 HP 減少、1 ステップごとのヒロイン HP 減少、初期主人公 LP、初期ヒロイン LP、好感度報酬、訓練熟練度報酬、同時 0 ボーナスを持たせる。
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
`GameManager.OnTrainingPanelResult(...)` は完了時のみ `TrainingData.affectionReward` と同時 0 ボーナスを好感度へ反映し、途中終了時は報酬なしにする。
訓練結果は `ShowSystemMessage(...)` で画面に表示し、メッセージログにも残す。1 ステップ以上進めた訓練は、完了/中断に関わらず時間を 1 段階進める。
訓練熟練度は `SaveData.trainingProficiencies` に `trainingId` ごとの値として保存し、完了時のみ `trainingProficiencyReward` を加算する。訓練完了後は `GameManager` が `SkillData` の好感度、日数、訓練熟練度、前提スキル条件を確認し、条件を満たしたスキルを `SaveData.unlockedSkillIds` へ自動解放する。
`TrainingPanel` は訓練ボタンと選択中タイトルに現在の熟練度を表示する。
まだ熟練度によるスキル解放、訓練メニューの段階変化、シーン配置の細かい見た目調整は次段階で扱う。

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
それぞれ `LightPractice` 熟練度 3、`EnduranceTraining` 熟練度 3、`SparringPractice` 熟練度 5 を解放条件にする。
戦闘用スキルは `BattlePanel` のコマンドとして表示し、汎用スキルはステータスやイベント条件で参照できるようにする。
訓練用スキルは模擬戦闘で優先的に使い、勝敗だけでなく「うまく防げた」「連携できた」などの訓練結果に接続する。
取得済みスキル ID は `SaveData.unlockedSkillIds` に保存し、`GameManager.IsSkillUnlocked(...)` / `UnlockSkill(...)` / `GetUnlockedSkillIds()` で扱う。
スキル熟練度や装備中スキルを保存する場合は、将来 `skillProficiencies`、`equippedSkillIds` のような保存領域を追加する。

スキル取得と装備は、`StatusDetailPanel` に直接詰め込まず、別の `SkillPanel` として扱う。
`SkillPanel` は実装済みで、全スキルの未取得/取得済み状態、説明、解放条件を表示し、戦闘中は解放済みの戦闘用スキルを選択できる。`PowerStrike`（Damage）、`GuardStance`（Guard）、`FirstAid`（Heal）は `BattlePanel` で実行できる。
UI は Canvas 配下へ手動配置し、`panelRoot`、一覧親、ボタン Prefab、必要に応じてタイトル・説明・閉じるボタンを Inspector で割り当てる。通常一覧はヒロイン別 `Actions/SkillAction.asset` の `ActionExecutionType.OpenSkillPanel` から開く。戦闘中は `BattleSkillPanel` を開き、現在 MP、スキル一覧、選択中説明、使用/戻るを表示する。`BattleSkillPanel` が未配置の場合だけ従来の `SkillPanel` を戦闘選択にフォールバックする。
`BattleStatusData` は HP に加えて MP を持つ。MP は戦闘開始時に最大値まで回復する戦闘内リソースで、`SkillData.cost` を使うと減少する。`SkillEffectType.Buff` / `Debuff` は `SkillData.affectedStat`（Attack / Defense / Speed）を戦闘終了まで増減する。初期データとして `BattleFocus`（攻撃 Buff）と `ArmorBreak`（敵防御 Debuff）を追加済み。
敵側も同じ `BattleStatusData` の MP を使う。敵スキルは `EnemyData.battleSkills` の `EnemyBattleSkillData` で定義し、MP が足りる候補から `useChancePercent` と `priority` により選ばれる。`maxUsesPerBattle` で同じ補助スキルの連続使用を防げる。初期敵では ForestSlime が酸液吹きと硬質化、CaveBat が急降下攻撃と翼の加護、LakeSpirit が水弾と冷気の霧を使う。
ヒロイン用スキル、カテゴリタブ、装備枠、複数ターンで切れる Buff/Debuff、MP 回復アイテムは次段階で追加する。

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
