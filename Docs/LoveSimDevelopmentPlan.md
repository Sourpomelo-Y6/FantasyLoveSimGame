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
| エンディング | 好感度100でボタン表示 |

通常の起点は `TitleScene` で、そこから `MainScene` に進む構成を想定しています。

## 今後の方針

最初から全部作ろうとせず、まずは「会話と日常行動で好感度を上げ、条件達成でエンディングを見る」体験を磨くのが優先です。

そのうえで、以下を段階的に追加します。

1. スチル表示
2. スチル回想
3. 立ち絵変更
4. セーブスロット UI の調整
5. ミニゲーム
6. エンディング分岐

着せ替えと衣装評価は、いまの実装で導線と保存が入っているため、今後は評価の種類追加や UI の整理を中心に詰めるとよいです。

## スチル回想

イベントスチル表示は `GameEventData.pages[].stillSprite` で扱えるため、次の段階では「表示済みスチルを回想画面で見られる」仕組みを追加する。
まずはゲーム内で一度表示したイベントスチルだけを解放済みにし、未解放スチルは一覧に出さない、または空枠として表示する方針にする。

### 保存データ

回想の解放状態は、イベント既読とは別に保存する。
`shownGameEventIds` はイベント再生済み管理であり、スチル単位の解放状態とは用途が違うため、将来的には `unlockedStillIds` のようなリストを `SaveData` に追加する。

スチルIDは `eventId` とページ番号から自動生成するより、専用の `stillId` を持たせる方が安全。
イベント本文のページ順を後から変えても、回想の解放状態が壊れにくいため。
実装時は `GameEventPageData` に `stillId` を追加し、`stillSprite` があるページだけ `stillId` を設定する。

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

一覧は、最初は解放済みスチルだけを表示する。
未解放枠の表示、ページング、カテゴリ分け、サムネイル生成は後回しにする。

### データ配置

回想対象のスチル画像は `Assets/Images/Event/` に置き、ファイル名は `stillId` に合わせる。
`GameEventData` の `stillId` と画像ファイル名を近づけることで、Unity Inspector 上で対応を追いやすくする。

回想一覧に表示する名前や説明が必要になったら、`StillGalleryData` のような ScriptableObject を追加する。
初期実装では `stillId` と Sprite だけで表示し、説明文やカテゴリは後から足す。

### 実装順

1. `SaveData` に `unlockedStillIds` を追加する
2. `GameEventPageData` に `stillId` を追加する
3. `GameManager` がスチル表示時に `stillId` を解放済みにする
4. 回想用パネルの接続ポイントを `StillGalleryPanel` として用意する
5. Unity 上で回想 UI を手動配置し、Inspector で参照を割り当てる
6. メイン画面の行動またはボタンから回想パネルを開けるようにする
7. 解放済みスチルだけが一覧に出ることを確認する

最初の実装では、`GameStartIntro_01` を回想解放の確認用に使う。
`TestManualEvent` で同じスチルを表示した場合も解放されるようにすれば、F7 で回想の動作確認がしやすい。

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

### ConversationData

会話は引き続き `ConversationData` で管理します。
会話の追加は `Assets/Resources/Conversations/` にアセットを置くだけで済むようにするのが目標です。
会話IDは `カテゴリ_条件_連番` を基本にし、`showOnce` は一度だけ見せる会話に限って使う。
`minAffection` / `maxAffection` は会話の入口条件、`allowedTimeSlots` / `allowedWeathers` / `allowedSeasons` は必要な場合だけ使う。条件が重なる会話は `priority` で解決する。
同じ条件の会話が複数ある場合でも、`conversationId` から用途が分かるようにしておく。

### GameEventData

タイトルから新規ゲームを開始した直後の導入や、今後の汎用イベントは `GameEventData` で管理する。
`Assets/Resources/GameEvents/` に置き、`triggerType` で `GameStart` / `DayStart` / `Manual` を分ける。
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

イベントスチル画像は `Assets/Images/Event/` に置き、ファイル名はイベントIDに寄せる。
例として、`GameStartIntro` で使う画像は `GameStartIntro_01.png` のようにする。
複数ページで同じスチルを維持したい場合は、最初のページだけでなく必要なページにも `stillSprite` を設定するか、現在スチルを維持する仕様を明文化してから実装する。

イベントが増えてきたら、`Assets/Resources/GameEvents/` 内で ScriptableObject 名と `eventId` を一致させる。
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
10. 汎用ログ画面を追加する場合は、セッション中の直近ログだけを保持する。対象は会話、行動結果、予定、衣装通知とし、話者タイプ付きで最大 20 件程度を表示する。セーブデータには含めない

## 優先度の高い改善候補

1. 行動の反応パターン追加
2. 会話データの整理と命名規則の統一
3. スチル表示と回想の導線追加
4. 立ち絵切り替えと表情差分の整理
5. セーブスロット UI の調整
6. セーブ/ロードの補強
7. UI の視認性改善
8. エンディングの分岐追加

## 補足

- 旧文書に残っていた `SampleScene` や `Assets/Data/Conversations/` は現在の構成とずれているので、今後は `MainScene` と `Assets/Resources/...` を基準にする
- 実装済みの機能を増やす時は、まずデータ追加で済むかを優先して考える
