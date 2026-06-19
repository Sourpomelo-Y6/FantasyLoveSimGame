# キャラクター素材生成ツール仕様

このドキュメントは、別リポジトリまたは別フォルダで作成する、ヒロインキャラクター用データと Stable Diffusion 画像素材を管理・生成するためのツール仕様です。
Unity プロジェクト本体とは分けて運用し、生成した成果物だけを `FantasyLoveSim` に取り込む前提にします。

## 目的

- ヒロインごとの立ち絵、衣装差分、イベントスチル、行動スチル、エンディングスチルをまとめて生成・管理する
- Stable Diffusion 用のプロンプト、ネガティブプロンプト、Seed、モデル、LoRA、ControlNet 設定を記録する
- 画像だけでなく、ヒロイン設定、口調、会話方針、イベント案、行動反応案も同じキャラクター単位で管理する
- Unity 側の `HeroineProfileData` と `Assets/Images/Heroines/<HeroineId>/...` に取り込みやすい形で出力する

## 想定する運用

1. 別リポジトリにキャラクターごとの素材生成プロジェクトを作る
2. ツール上で `HeroineId`、名前、外見設定、性格、口調、衣装、生成プロンプトを登録する
3. Stable Diffusion で画像を生成し、用途別に採用画像を選ぶ
4. 採用画像を Unity 向けのフォルダ構成とファイル名へ整形して export する
5. Unity 側で `Assets/Images/Heroines/<HeroineId>/...` と `Assets/Resources/Heroines/<HeroineId>/...` に取り込む

## 管理対象

### キャラクター基本情報

- `heroineId`
- 表示名
- 年齢・身長などの任意プロフィール
- 性格
- 口調
- 一人称・二人称
- 好きなもの、苦手なもの
- 行動反応の方向性
- エンディング方針

### 画像用途

- `Sprites`: 通常立ち絵、衣装差分、表情差分
- `Event`: ゲーム開始、日常イベント、予定イベントなどのイベントスチル
- `Actions`: 行動結果や行動反応用のスチル
- `Ending`: エンディング用スチル

Unity 側の取り込み先は次を基本にする。

```text
Assets/Images/Heroines/<HeroineId>/Sprites/
Assets/Images/Heroines/<HeroineId>/Event/
Assets/Images/Heroines/<HeroineId>/Actions/
Assets/Images/Heroines/<HeroineId>/Ending/
```

### 会話・イベント案

画像生成ツール側では、Unity の ScriptableObject を直接作る必要はない。
ただし、次の下書きをキャラクター単位で持てるようにする。

- ジャンル会話案
- 好感度条件会話案
- 天候・季節・時間帯条件会話案
- ゲーム開始イベント案
- 日開始イベント案
- 行動反応案
- エンディング本文案

最終的には Unity 側で `ConversationData`、`GameEventData`、`ActionReactionData`、`EndingData` に手動または変換ツールで反映する。

## 出力ファイル命名

ファイル名は Unity 側の ID と対応しやすいように、用途と連番を含める。

### 立ち絵

```text
Heroine_Normal.png
Heroine_Smile.png
Heroine_Spring.png
Heroine_Summer.png
Heroine_Autumn.png
Heroine_Winter.png
Heroine_Dress.png
Heroine_NightDress.png
Heroine_Raincoat.png
```

### イベントスチル

```text
GameStartIntro_01.png
DayStart_Routine_01.png
DayStart_Rainy_01.png
WithForest_01.png
WithLake_01.png
WithCave_01.png
```

### 行動スチル

```text
Tea_01.png
Rest_01.png
Walk_01.png
Gift_01.png
```

### エンディングスチル

```text
GoodEnding_01.png
NormalEnding_01.png
BadEnding_01.png
```

## Stable Diffusion 設定の記録

採用画像ごとに次を保存する。

- positive prompt
- negative prompt
- model
- VAE
- LoRA
- sampler
- steps
- CFG scale
- seed
- image size
- ControlNet / reference image の有無
- upscale 設定
- inpaint / img2img の履歴
- 採用理由と修正メモ

画像ファイルとは別に、同名の JSON または YAML を置くと追跡しやすい。

```text
GameStartIntro_01.png
GameStartIntro_01.prompt.json
```

## ツールの画面案

### キャラクター一覧

- 登録済みキャラクターを一覧表示する
- `HeroineId`、表示名、作成状況、採用済み画像数を確認できる
- 新規キャラクター作成、編集、export を実行できる

### キャラクター詳細

- 基本設定
- 口調・会話方針
- 衣装一覧
- 画像用途別リスト
- Stable Diffusion プロンプトテンプレート
- Unity 出力設定

### 画像生成・採用画面

- 用途を選ぶ
- プロンプトテンプレートから生成用プロンプトを作る
- 生成結果を登録する
- 採用・保留・没を管理する
- 採用画像のファイル名を Unity 用に決める

### Export 画面

- Unity 向けフォルダ構成で出力する
- 採用画像だけを出力する
- prompt 記録を同梱するか選べる
- `HeroineProfileData` 作成用のメモまたは JSON を出力する

## 出力フォルダ例

ツール側の export 結果は次のようにする。

```text
Export/
  TestHeroine/
    Images/
      Sprites/
      Event/
      Actions/
      Ending/
    Data/
      heroine_profile_note.md
      heroine_profile_export.json
      assets_export.json
      conversations_draft.md
      game_events_draft.md
      action_reactions_draft.md
      endings_draft.md
      conversations_export.json
      game_events_export.json
      action_reactions_export.json
      endings_export.json
    Prompts/
      GameStartIntro_01.prompt.json
```

Unity に取り込むときは、`Images` 配下を次へコピーする。

```text
Assets/Images/Heroines/<HeroineId>/
```

`Data` 配下の `*_draft.md` は人間が内容を確認するための下書きとして残す。
`*_export.json` は Unity Editor 拡張が読み込み、Unity Editor 内で ScriptableObject を生成、更新するための中間データとして使う。

## Unity 取り込み方針

`FantasyLoveSimAssetTool` は Unity の ScriptableObject `.asset` を直接生成しない。
ツール側は画像と中間 JSON を出力し、Unity Editor 拡張が Unity Editor 内で JSON を読み込んで `.asset` を生成、更新する。

この分担にする理由は、`.meta`、GUID、fileID、ScriptableObject 型情報を Unity Editor に管理させるためです。
外部ツール側で Unity YAML を直接生成すると、参照切れや GUID 重複の原因になりやすい。

基本方針:

- ツール側は `Export/<HeroineId>/` を出力する
- Unity 側は `Export/<HeroineId>/Images/` の画像を `Assets/Images/Heroines/<HeroineId>/` へ取り込む
- Unity 側は `Export/<HeroineId>/Data/heroine_profile_export.json` と `Export/<HeroineId>/Data/assets_export.json` を読む
- Unity 側の `.asset` 生成、更新は Unity Editor 拡張が担当する
- `Prompts/` 配下の prompt JSON は、生成条件の参照資料として保持する

### WPF Export 構成

将来の export 構成は次を基本にする。

```text
Export/
  <HeroineId>/
    Images/
      Sprites/
      Event/
      Actions/
      Ending/
    Data/
      heroine_profile_note.md
      heroine_profile_export.json
      assets_export.json
      conversations_draft.md
      game_events_draft.md
      action_reactions_draft.md
      endings_draft.md
      conversations_export.json
      game_events_export.json
      action_reactions_export.json
      endings_export.json
    Prompts/
      <AssetId>.prompt.json
```

画像の取り込み先は次を基本にする。

```text
Assets/Images/Heroines/<HeroineId>/
  Sprites/
  Event/
  Actions/
  Ending/
```

ScriptableObject の保存先は、Unity プロジェクト側の既存設計に合わせる。
未確定の場合は次を候補にする。

```text
Assets/Resources/Heroines/<HeroineId>/
  HeroineProfileData.asset
  HeroineAssetCatalog.asset
```

画像は `assets_export.json` の `unityImagePath` を基準に参照する。
ツール側の export では `unityImagePath` を `Assets/Images/Heroines/<HeroineId>/<Usage>/<FileName>` として出力する。

### heroine_profile_export.json

`heroine_profile_export.json` は、Unity 側で `HeroineProfileData` などのキャラクター基本情報 ScriptableObject を作るための入口にする。

主な項目:

- `schemaVersion`
- `heroineId`
- `displayName`
- `age`
- `height`
- `personality`
- `speakingStyle`
- `firstPerson`
- `secondPerson`
- `likes`
- `dislikes`
- `appearancePrompt`
- `stillCommonPositivePrompt`
- `actionReactionPolicy`
- `endingPolicy`

Unity Editor 拡張は `heroineId` をキーにして既存 `.asset` を検索する。
既存 `.asset` があれば更新し、なければ新規作成する。

### assets_export.json

`assets_export.json` は、採用済み画像だけを Unity 側へ渡すための入口にする。

主な項目:

- `schemaVersion`
- `heroineId`
- `unityImageRoot`
- `assets`

`assets` の各要素:

- `assetId`
- `usage`
- `status`
- `fileName`
- `memo`
- `exportImagePath`
- `exportPromptPath`
- `unityImagePath`

`status` は原則として `Accepted` のみになる。
Unity Editor 拡張は `exportImagePath` から画像をコピーし、`unityImagePath` に対応する Unity asset path として参照する。

### Unity Editor Import の処理順

1. ユーザーが Unity Editor メニューから Import を実行する
2. Import 対象として `Export/<HeroineId>/` フォルダを選ぶ
3. `Data/heroine_profile_export.json` を読み込む
4. `Data/assets_export.json` を読み込む
5. `Images/` 配下の画像を `Assets/Images/Heroines/<HeroineId>/` へコピーする
6. `AssetDatabase.ImportAsset` または `AssetDatabase.Refresh` で画像を Unity に認識させる
7. `heroineId` に対応する `HeroineProfileData.asset` を検索する
8. 既存 `.asset` があれば更新し、なければ作成する
9. `assets_export.json` の各 `asset` から画像参照を設定する
10. `HeroineAssetCatalog.asset` に画像一覧と Sprite 参照を保存する
11. `Prompts/` 配下の prompt JSON は参照資料としてコピーまたはパスだけ記録する
12. `AssetDatabase.SaveAssets` で保存する

現状の Unity 側実装では、`Assets/Editor/HeroineAssetImporter.cs` を追加し、`FantasyLoveSim > Import Heroine Export` メニューから `Export/<HeroineId>/` フォルダを選べる。
`Data/heroine_profile_export.json` を読み、`Assets/Resources/Heroines/<HeroineId>Profile.asset` の `HeroineProfileData` を新規作成または更新する。
`Data/assets_export.json` が存在する場合は、`Accepted` または空ステータスの画像を `exportImagePath` から `unityImagePath` へコピーする。
既存画像は自動上書きせず、警告ログを出してスキップする。
同時に `Assets/Resources/Heroines/<HeroineId>/HeroineAssetCatalog.asset` を生成、更新し、`assetId`、用途、ファイル名、`unityImagePath`、`Sprite` 参照を保存する。
`assetId` が空、または同一 export 内で重複している画像は warning を出してスキップする。
`usage` が `Sprites` で、`assetId` または `fileName` が `Heroine_Normal` / `Heroine_Normal.png` の画像は代表立ち絵候補として扱い、`HeroineProfileData.defaultHeroineSprite` に自動割り当てする。
取り込んだ画像は UI 表示に使えるよう、Unity の import 設定を `Sprite` に補正する。
`GameManager` は `HeroineProfileData.defaultHeroineSprite` を `OutfitManager` に渡し、通常衣装 `Normal` の表示では profile の代表立ち絵を優先する。
通常衣装以外は、衣装に `heroineSprite` が設定されている場合は従来どおり衣装画像を優先し、衣装画像がない場合だけ代表立ち絵へフォールバックする。
`TestHeroine` の追加画像は容量節約のため Unity 側リポジトリへコミットせず、AssetTool 側の export サンプルまたはローカル import 結果として扱う。
`Data/conversations_export.json` が存在する場合は、`Assets/Resources/Heroines/<HeroineId>/Conversations.asset` を生成、更新する。
`Conversations.asset` は `ConversationData.items` に複数会話を持つ container として扱い、実行時には `GameManager` が従来の会話候補へ展開する。
会話 import は `lines[]` を `ConversationDataItem.lines` に保持する。
互換用に最初の本文は `ConversationDataItem.heroineLine`、最初の表情は `expressionId` にも反映する。
実行時は複数行会話として表示し、`lines[].expression` を `HeroineLayeredSpriteView` の表情レイヤー切り替えに使う。
既存の個別 `ConversationData` asset も互換のため読み込めるが、新規 import は `Conversations.asset` にまとめる。
`Data/sprite_layers_export.json` が存在する場合は、`Assets/Resources/Heroines/<HeroineId>/HeroineLayeredSpriteData.asset` を生成、更新する。
レイヤーは `BaseBody` / `Costume` / `Expression` / `Accessory` に分類し、`assetId` から `HeroineAssetCatalog` の Sprite を参照する。
`BaseBody` なし、`Default` 衣装なし、`Neutral` 表情なし、未知 `layerKind`、Sprite 解決失敗は Import warning に残す。
`HeroineLayeredSpriteView` は実装済みで、現在衣装の `costumeId` と会話行の `expressionId` から表示レイヤーを選ぶ。
指定衣装がなければ `Default`、指定表情がなければ `Neutral` へ fallback する。
`Data/game_events_export.json` が存在する場合は、`Assets/Resources/Heroines/<HeroineId>/GameEvents/<EventId>.asset` を生成、更新する。
`category` は `GameStart` / `DayStart` / `Manual` の `GameEventTriggerType` として扱い、未知の場合は warning を出して `Manual` とする。
`lines[]` は `GameEventData.pages` に変換し、`lines[].expression` はイベントページの `expressionId` として保持する。
`imageAssetIds[0]` は `HeroineAssetCatalog` から Sprite 解決し、最初のページのイベントスチルとして設定する。
prompt JSON、行動反応、エンディング本文の JSON import は次段階で対応する。
Importer は `HeroineImportReport` で copied images、catalog assets、layers、conversations、game events、warning 件数を集計し、完了時に Console summary と `EditorUtility.DisplayDialog` で結果を表示する。
続行可能な問題は warning として report に残し、JSON が読めない、`heroineId` が取れないなどのキー情報不足だけ中断する。

次は Unity 側 importer を広げる前に、`FantasyLoveSimAssetTool` 側で会話、イベント、行動反応、エンディング本文を入力、保存、export できるようにする。
Unity 側は受け口を段階的に増やす方針だが、先に Tool 側の JSON 出力を安定させることで、Unity import の実装と検証を進めやすくする。

対応関係:

| WPF export | Unity 側用途 |
| --- | --- |
| `Images/<Usage>/<FileName>` | Unity に取り込む画像ファイル |
| `Data/heroine_profile_export.json` | `HeroineProfileData` 生成、更新 |
| `Data/assets_export.json` | `HeroineAssetCatalog` 生成、更新 |
| `Prompts/<AssetId>.prompt.json` | 生成条件の確認、再生成用メモ |
| `Data/*_draft.md` | 会話、イベント、行動反応、エンディングの下書き |
| `Data/conversations_export.json` | 通常会話データの生成、更新 |
| `Data/game_events_export.json` | ゲームイベント本文データの生成、更新 |
| `Data/action_reactions_export.json` | 行動反応本文データの生成、更新 |
| `Data/endings_export.json` | エンディング本文データの生成、更新 |

### 会話データの将来拡張

会話データ、イベント、行動反応、エンディング本文は、Markdown 下書きと中間 JSON の両方で export できるようにする。
Markdown は人間の確認用、JSON は Unity Editor 拡張による ScriptableObject 生成、更新用に使う。

```text
Data/
  conversations_export.json
  game_events_export.json
  action_reactions_export.json
  endings_export.json
```

このときもツール側から `.asset` を直接生成しない。
Unity Editor 側で `ConversationData`、`GameEventData`、`ActionReactionData`、`EndingData` の `.asset` を生成、更新する。
会話データは item ごとに個別 `.asset` を分けず、まずは `Conversations.asset` 1つにまとめる。

Tool 側で先に作る最小機能:

1. `profile.json` に会話本文データの保存領域を追加する
2. 会話、イベント、行動反応、エンディングを同じ内部モデルで保持する
3. 種別ごとに `conversations_export.json`、`game_events_export.json`、`action_reactions_export.json`、`endings_export.json` を出力する
4. 既存の Accepted 画像 `assetId` を `imageAssetIds` に指定できるようにする
5. export 前に ID 重複、空本文、参照できない画像 `assetId` を検証する

最初に Tool 側で実装する対象は `conversations_export.json` とする。
Unity 側には既に最小 import があるため、Tool 側から export した JSON をそのまま Unity に取り込めるか確認しやすい。
`game_events_export.json` の Unity import も対応済み。
次は Tool 側でゲームイベント export を実データとして増やし、Unity 側で `GameStart` / `DayStart` / `Manual` の発火確認を進める。

各 JSON は次の共通項目を持つ。

- `schemaVersion`
- `heroineId`
- `items`

各 `items` の要素は、基本的に次を持つ。

- `id`: Unity 側の asset ID または entry ID と対応させる一意 ID
- `title`: 画面表示用の短いタイトル
- `category`: 用途分類
- `conditions`: 表示条件、発生条件
- `lines`: 台詞本文
- `imageAssetIds`: 関連する画像 `assetId`
- `priority`: 同条件で複数候補がある場合の優先度
- `memo`: 制作メモ

`imageAssetIds` は `assets_export.json` の `assetId` を参照する。
Unity 側では `assets_export.json` を使って `assetId` から `Assets/Images/Heroines/<HeroineId>/...` の画像参照へ解決する。

#### conversations_export.json

通常会話を扱う。
雑談、好感度条件会話、季節、天候、時間帯、場所などの会話をここに入れる。
Unity 側の対応先は `ConversationData` を想定する。

主な条件キー:

- `locationId`
- `minAffection`
- `maxAffection`
- `weather`
- `season`
- `timeOfDay`

#### game_events_export.json

ゲーム開始、日開始、場所イベント、予定イベントなど、イベント単位で発生する本文を扱う。
イベントスチルを使う場合は `imageAssetIds` に `GameStartIntro_01` などを入れる。
Unity 側の対応先は `GameEventData` を想定する。

主な条件キー:

- `once`
- `locationId`
- `minDay`
- `maxDay`
- `minAffection`
- `weather`
- `requiredOutfitIds`
- `blockedOutfitIds`

#### action_reactions_export.json

プレイヤー行動への反応を扱う。
休憩、散歩、お茶、贈り物など、行動結果として出る本文と画像をここに入れる。
Unity 側の対応先は `ActionReactionData` を想定する。

主な条件キー:

- `actionId`
- `minAffection`
- `maxAffection`
- `requiredItemId`
- `weather`
- `season`
- `timeOfDay`

#### endings_export.json

エンディング本文を扱う。
Good、Normal、Bad などの結果条件と、対応するエンディングスチルをここに入れる。
Unity 側の対応先は `EndingData` を想定する。

主な条件キー:

- `minAffection`
- `requiredFlagIds`
- `requiredShownEventIds`

#### 会話 JSON の検証観点

- `id` が空でない
- 同一 JSON 内で `id` が重複しない
- `heroineId` が profile export と一致する
- `imageAssetIds` が `assets_export.json` に存在する
- `lines` が空でない
- `speaker` と `text` が空でない
- `priority` が数値として扱える
- Unity 側で未知の `category` や条件キーがあっても import が破綻しない

### Unity 取り込みの未決事項

- Unity 側の ScriptableObject 型名とフィールド名の完全な対応
- `HeroineProfileData.asset` の保存先
- prompt JSON を Unity プロジェクト内へコピーするか、WPF export フォルダ参照のままにするか
- Import 時に既存画像を上書きするか、確認ダイアログを出すか
- `speaker` を文字列にするか enum にするか
- 表情を `expression` 文字列で持つか、画像差分 `assetId` で持つか
- 選択肢、分岐、イベントフラグ更新を最初から入れるか
- 会話データをキャラクター単位でまとめるか、item ごとに `.asset` を分けるか

## 将来の拡張

- Unity Editor 拡張で ScriptableObject を自動生成、更新する
- Unity Editor 拡張で export 結果を取り込む
- JSON から `ConversationData` や `GameEventData` を生成する
- 画像の解像度、縦横比、透過、余白を自動チェックする
- 立ち絵の背景透過や表情差分の整合性をチェックする
- 複数ヒロイン間でプロンプトテンプレートを共有する

## 最初に作る最小機能

最初は大きな自動化を狙わず、次だけ作ればよい。

1. キャラクター基本情報を JSON または YAML で保存する
2. 画像用途別フォルダを作成する
3. 採用画像と prompt 記録を同じ ID で保存する
4. Unity 向け export フォルダを作る
5. `heroine_profile_note.md` を出力する

この段階で、Stable Diffusion 画像生成と Unity 取り込みの作業を分離しつつ、後から自動化しやすい形にできる。
