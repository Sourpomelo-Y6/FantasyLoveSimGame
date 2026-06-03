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
| 背景切り替え | 時間帯・天候に応じて背景 Sprite を切り替え |
| ゲームイベント | `GameStart` / `DayStart` / `Manual` の汎用イベント |
| スチル回想 | 解放済み・未解放スチルを一覧表示 |
| メッセージログ | セッション中の直近メッセージを表示 |
| エンディング | 好感度100で `EndingScene` に遷移し、`EndingData` を表示 |

通常の起点は `TitleScene` で、そこから `MainScene` に進む構成を想定しています。

## 今後の方針

最初から全部作ろうとせず、まずは「会話と日常行動で好感度を上げ、条件達成でエンディングを見る」体験を磨くのが優先です。

そのうえで、以下を段階的に追加・整理します。

1. 行動反応と会話データの追加
2. エンディング分岐条件の自動選択
3. 立ち絵変更と表情差分
4. スチル回想のページング
5. セーブ/ロードの補強
6. ミニゲーム

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
ページング、カテゴリ分け、サムネイル生成は後回しにする。
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
8. スチル数が増えたら、一覧にページング機能を追加する
9. スチル数が増えて必要になったら、項目ボタンにサムネイル Image を追加する

最初の実装では、`GameStartIntro_01` を回想解放の確認用に使う。
`TestManualEvent` で同じスチルを表示した場合も解放されるようにすれば、F7 で回想の動作確認がしやすい。

ページングを追加する場合は、`StillGalleryPanel` に `itemsPerPage`、`currentPageIndex`、前へボタン、次へボタン、ページ表示 Text を持たせる。
一覧更新時は全候補から現在ページ分だけを生成し、前後ボタンはページ端で無効化する。
最初は固定件数のページングでよく、カテゴリ別ページやフィルタはさらに後回しにする。

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

現在のヒロインは `DefaultHeroine` として扱い、`Assets/Resources/Heroines/DefaultHeroineProfile.asset` に `Heroines/DefaultHeroine/Conversations` / `GameEvents` / `Actions` / `Endings` を参照させている。
`GameManager` は profile から会話・イベント・行動の読み込みパスを適用し、`EndingScene` へ遷移するときに profile の `endingResourcePath` を `EndingSelectionSettings` へ渡す。
`EndingManager` は渡された `endingResourcePath` があればそのパスから `EndingData` を読み込む。
将来ヒロインを増やす段階で、Resources 配下を次のように分ける。

```text
Assets/Resources/Heroines/DefaultHeroine/Conversations/
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
`Heroines/TestHeroine/Actions` / `Conversations` / `GameEvents` / `Endings` には最小確認用データだけを置いている。
`MainScene` の `GameManager.heroineProfile` に `TestHeroineProfile` を割り当てると、ヒロイン名、開始イベント、会話、エンディングの読み込み元が切り替わるか確認できる。
本番用ヒロインを追加する前に、まずこの profile で差し替え導線を手動確認する。

新しいヒロインを追加するときは、次のチェックリストを使う。

- `HeroineProfileData` を作成し、`heroineId` と `displayName` を設定する
- `conversationResourcePath` に、そのヒロイン用の `Conversations` フォルダを設定する
- `gameEventResourcePath` に、そのヒロイン用の `GameEvents` フォルダを設定する
- `actionResourcePath` に、そのヒロイン用の `Actions` フォルダを設定する
- `endingResourcePath` に、そのヒロイン用の `Endings` フォルダを設定する
- `defaultHeroineSprite` に代表立ち絵を設定する
- `Actions` には行動名、行動結果、行動反応、行動スチルを用意する
- `Conversations` にはジャンル会話、好感度条件会話、天候・時間帯・季節条件会話を用意する
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
会話の追加は、対象ヒロインの `conversationResourcePath` 配下にアセットを置くだけで済むようにするのが目標です。
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
- `TitleScene` の `ContinueButton` は `SaveLoadPanel.OpenLoad()` でロード用スロット選択を開く
- `MainScene` の `SaveButton` / `LoadButton` は `SaveLoadPanel.OpenSave()` / `OpenLoad()` でスロット選択を開く
- `SaveLoadPanel` はセーブ時に青背景・`セーブ`、ロード時にオレンジ背景・`ロード` に切り替える
- 保存済みスロットは `Day` と `Affection` をラベルに表示する
- `MainScene` でロードした後は `SaveLoadPanel` を閉じる
- セーブ/ロード回帰確認では、好感度、日付、時間帯、曜日、季節、天気、現在衣装、衣装評価、今日/明日の予定、当日予定イベントの発動済み状態、能力取得状態、衣装確認モード解放状態を確認する
- 翌朝メッセージキューと将来の汎用ログはセーブ対象外の方針なので、ロード後に復元されなくてよい

### 案2: `ScheduleType -> ScheduledEventData` 変換表

これは「予定を翌日に自動実行する」ための仕組みで、現在は準備フェーズ付きで実装済み。
予定イベントは `Assets/Resources/ScheduledEvents/` の `ScheduledEventData` アセットで管理する。
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
8. 衣装確認モードの解放条件は、`GameManager.playerOutfitPromptAbilities` と `HeroineStatus.OutfitPromptAbilities` で管理する
9. 話者タイプごとに表示色を変え、同じメッセージボックス内でも発話種別が分かるようにする
10. 汎用ログ画面は実装済み。セッション中の直近ログだけを保持し、対象は会話、行動結果、予定、衣装通知とする。話者名とメッセージを `MessageLogPanel` に表示し、セーブデータには含めない

ログ画面をスクロールビューで表示する場合は、Unity 上で `MessageLogPanel > Scroll View > Viewport > MessageLogList` の階層を作り、`ScrollRect.content` に `MessageLogList` を指定する。
`MessageLogList` の中身は実行時に `MessageLogRowPrefab` から生成されるため、編集時点では空でよい。
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
現状は `GameManager.defaultEndingId` の `GoodEnding` を固定選択しているため、分岐を入れる段階で `HeroineProfileData.endingResourcePath` から条件に合う `EndingData` を選ぶ処理を追加する。
複数エンディングが同時に条件を満たす場合に備えて、必要になったら `priority` を追加する。

### メッセージのクリック進行

複数メッセージを読むときは、`Next` ボタンまたはメッセージ表示ウィンドウのクリックで次のメッセージへ進む。
選択肢、Save/Load、メッセージログ、ステータス詳細などの UI 操作と競合しないよう、クリック進行はメッセージ表示ウィンドウに限定する。
`DialogueClickAdvanceArea` をメッセージ表示ウィンドウの Panel などに追加し、`GameManager.dialogueClickAdvanceArea` に割り当てると、`Next` ボタンが表示中かつ押せる状態のときだけクリックで次へ進む。
クリック対象の Image は `Raycast Target` を有効にする必要がある。
この操作はプレイヤーの好みによって必要性が変わるため、オプション画面を追加して ON/OFF を切り替えられるようにする案を残す。

## 優先度の高い改善候補

1. 行動の反応パターン追加
2. 会話データの整理と命名規則の統一
3. エンディング分岐条件の自動選択
4. 立ち絵切り替えと表情差分の整理
5. スチル回想のページング
6. セーブ/ロードの補強
7. UI の視認性改善
8. メッセージ表示ウィンドウのクリック進行 ON/OFF オプション
9. ミニゲーム

## 補足

- 旧文書に残っていた `SampleScene` や `Assets/Data/Conversations/` は現在の構成とずれているので、今後は `MainScene` と `Assets/Resources/...` を基準にする
- 実装済みの機能を増やす時は、まずデータ追加で済むかを優先して考える
