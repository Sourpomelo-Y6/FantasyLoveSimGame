# Conversation Data Plan

このドキュメントは、`FantasyLoveSimAssetTool` で会話、イベント、行動反応、エンディング本文を作成し、Unity 側へ渡すための設計案をまとめる。

現時点では WPF ツールは画像素材と prompt 記録の管理を主目的にしている。
会話データは次の段階で追加する。

`GameEvents` のカテゴリ、条件、イベントスチル参照の具体運用は `Docs/GameEventDataGuide.md` にまとめる。

## 基本方針

- WPF ツールは会話、イベント、行動反応、エンディング本文をキャラクター単位で編集する。
- WPF ツールは Unity の ScriptableObject `.asset` を直接生成しない。
- WPF ツールは中間 JSON を `Export/<HeroineId>/Data/` に出力する。
- Unity Editor 拡張が中間 JSON を読み、Unity Editor 内で ScriptableObject `.asset` を生成、更新する。
- 最初は分岐や演出を過度に複雑にせず、本文、条件、選択肢、参照画像、メモを確実に渡せる形にする。

## Export JSON

将来追加する export JSON は次を基本にする。

```text
Export/
  <HeroineId>/
    Data/
      conversations_export.json
      game_events_export.json
      scheduled_events_export.json
      action_reactions_export.json
      endings_export.json
```

現状の `*_draft.md` は、人間が下書きを確認するために残す。
JSON export が追加された後も、Markdown はメモまたはレビュー用として併存できる。

## 共通項目

各 JSON は、次の共通項目を持つ。

- `schemaVersion`
- `heroineId`
- `items`

各 `items` の要素は、基本的に次を持つ。

- `id`: Unity 側の asset ID または entry ID と対応させる一意 ID
- `title`: 画面表示用の短いタイトル
- `category`: 用途分類
- `conditions`: 表示条件、発生条件
- `lines`: 台詞本文
- `imageAssetIds`: 関連する画像 AssetId
- `priority`: 同条件で複数候補がある場合の優先度
- `memo`: 制作メモ

`imageAssetIds` は、WPF 側で登録している `HeroineAsset.AssetId` を参照する。
Unity 側では `assets_export.json` を使って `AssetId` から画像パスへ解決する。

## Unity 側フィールド対応

Unity 側では、会話は JSON ファイルごとに1つの ScriptableObject `.asset` を生成、更新する。
ゲームイベントは既存の `GameManager` が `Resources.LoadAll<GameEventData>` で読むため、item ごとに個別 `.asset` を生成、更新する。
行動反応、エンディングは実装時に container 方式か個別 asset 方式を決める。

| JSON | Unity 側 ScriptableObject | 保存先 |
| --- | --- | --- |
| `conversations_export.json` | `ConversationData` | `Assets/Resources/Heroines/<HeroineId>/Conversations.asset` |
| `game_events_export.json` | `GameEventData` | `Assets/Resources/Heroines/<HeroineId>/GameEvents/<EventId>.asset` |
| `scheduled_events_export.json` | `ScheduledEventData` | `Assets/Resources/ScheduledEvents/<ScheduledEvent>.asset` |
| `action_reactions_export.json` | `ActionData.reactions` | `Assets/Resources/Heroines/<HeroineId>/Actions/<Action>.asset` |
| `endings_export.json` | `EndingData` | `Assets/Resources/Heroines/<HeroineId>/Endings/<EndingId>.asset` |

JSON item と Unity 側 item のフィールド対応は次を基本にする。
Unity 側の実装で別名を使う場合も、WPF 側の JSON 名はこの表の左列を維持する。

| JSON field | Unity field | 備考 |
| --- | --- | --- |
| `id` | `entryId` | 一意 ID。Unity 側の検索、更新キー |
| `title` | `title` | Editor 表示用 |
| `category` | `category` | 会話分類、イベント分類。空は警告対象 |
| `conditions.locationId` | `condition.locationId` | 空は条件なし |
| `conditions.minAffection` | `condition.minAffection` | 最小好感度 |
| `conditions.maxAffection` | `condition.maxAffection` | 最大好感度 |
| `conditions.weather` | `condition.weather` | 空は条件なし |
| `conditions.season` | `condition.season` | 空は条件なし |
| `conditions.timeOfDay` | `condition.timeOfDay` | 空は条件なし |
| `conditions.actionId` | `condition.actionId` | 行動反応用。空は条件なし |
| `conditions.requiredItemId` | `condition.requiredItemId` | 空は条件なし |
| `conditions.once` | `condition.once` | 1回限りイベント用 |
| `conditions.requiredFlagIds` | `condition.requiredFlagIds` | 空配列は条件なし |
| `lines[].speaker` | `lines[].speaker` | 例: `Heroine`, `Player` |
| `lines[].text` | `lines[].text` | 本文。空は警告対象 |
| `lines[].expression` | `lines[].expression` | 表情 ID。空は通常表情扱い |
| `choices[].choiceText` | `choices[].choiceText` | 選択肢本文。空は警告対象 |
| `choices[].responseText` | `choices[].responseText` | 選択後の返答本文。空は警告対象 |
| `choices[].affectionChange` | `choices[].affectionChange` | 選択時の好感度変化 |
| `imageAssetIds[]` | `imageAssetIds[]` | `assets_export.json` から画像参照へ解決 |
| `priority` | `priority` | 同条件で複数候補がある場合の優先度 |
| `memo` | `memo` | Editor 用メモ。ゲーム実行時には使わなくてもよい |

## Unity 側で受け取る値

現時点では、WPF 側の入力候補と Export 検証は次の値を基準にする。
空文字は「条件なし」として扱う。
Unity 側で実際の ID を変更した場合は、この一覧、WPF 側の候補、Export 検証を同時に更新する。

| 項目 | 値 |
| --- | --- |
| `locationId` | `Forest`, `Lake`, `Cave`, `Room`, `Town` |
| `actionId` | `Tea`, `Rest`, `Walk`, `Gift`, `Talk` |
| `weather` | `Sunny`, `Rainy`, `Cloudy`, `Snow` |
| `season` | `Spring`, `Summer`, `Autumn`, `Winter` |
| `timeOfDay` | `Morning`, `Day`, `Evening`, `Night` |
| `expression` | `Neutral`, `Smile`, `Sad`, `Angry`, `Shy`, `Surprised` |

## conversations_export.json

通常会話を扱う。
雑談、好感度条件会話、季節、天候、時間帯、場所などの会話をここに入れる。

```json
{
  "schemaVersion": 1,
  "heroineId": "TestHeroine",
  "items": [
    {
      "id": "Talk_Forest_Friendship01",
      "title": "森での雑談 1",
      "category": "LocationTalk",
      "conditions": {
        "locationId": "Forest",
        "minAffection": 10,
        "maxAffection": 100,
        "weather": "",
        "season": "",
        "timeOfDay": ""
      },
      "lines": [
        {
          "speaker": "Heroine",
          "text": "ここ、静かで落ち着くね。",
          "expression": "Smile"
        }
      ],
      "imageAssetIds": [],
      "priority": 100,
      "memo": ""
    }
  ]
}
```

Unity 側の対応先は `ConversationData` を想定する。

## game_events_export.json

ゲーム開始、日開始、場所イベント、予定イベントなど、イベント単位で発生する本文を扱う。
イベントスチルを使う場合は `imageAssetIds` に `GameStartIntro_01` などを入れる。
カテゴリ、発火条件、`once` とフラグの扱いは `Docs/GameEventDataGuide.md` を参照する。

```json
{
  "schemaVersion": 1,
  "heroineId": "TestHeroine",
  "items": [
    {
      "id": "Event_GameStartIntro",
      "title": "ゲーム開始導入",
      "category": "Intro",
      "conditions": {
        "once": true,
        "locationId": "",
        "minAffection": 0
      },
      "lines": [
        {
          "speaker": "Heroine",
          "text": "はじめまして。あなたが今日から一緒に過ごす人？",
          "expression": "Neutral"
        }
      ],
      "imageAssetIds": [
        "GameStartIntro_01"
      ],
      "priority": 100,
      "memo": ""
    }
  ]
}
```

Unity 側の対応先は `GameEventData` を想定する。

## scheduled_events_export.json

翌日予定として発生する外出、デート、家で過ごす予定を扱う。
通常行動の `Walk` や `ActionReactionData` とは別系統で、Unity 側の対応先は `ScheduledEventData` とする。
保存先はヒロイン別ではなく、現行ランタイムが読む `Assets/Resources/ScheduledEvents/`。

```json
{
  "schemaVersion": 1,
  "heroineId": "TestHeroine",
  "items": [
    {
      "id": "AutoWalkForest",
      "title": "森への外出",
      "category": "SoloForest",
      "conditions": {
        "scheduleType": "SoloForest",
        "actionId": "AutoWalkForest",
        "triggerTimeSlot": "Noon",
        "outfitPromptMode": "Conditional",
        "eventSpeakerType": "Heroine",
        "affectionChange": 1
      },
      "preparationMessage": "今日は昼に森へ出かける予定です。",
      "eventMessage": "森を歩きながら、静かな時間を過ごしました。",
      "imageAssetIds": [
        "Walk_Forest_01"
      ],
      "priority": 0,
      "memo": ""
    }
  ]
}
```

`conditions.scheduleType` は `SoloForest` / `SoloCave` / `SoloLake` / `SoloShopping` / `DuoForest` / `DuoCave` / `DuoLake` / `DuoShopping` / `StayHome` のいずれかを指定する。
`preparationMessage` は朝の予定表示、`eventMessage` は予定実行時の本文に入る。
`preparationMessage` / `eventMessage` が空の場合、Unity importer は `lines[0]` を準備文、`lines[1...]` を実行本文として補完する。
`imageAssetIds[0]` は `stillId` / `stillSprite` に変換する。

## action_reactions_export.json

プレイヤー行動への反応を扱う。
休憩、散歩、お茶、贈り物など、行動結果として出る本文と画像をここに入れる。

```json
{
  "schemaVersion": 1,
  "heroineId": "TestHeroine",
  "items": [
    {
      "id": "Reaction_Tea_Friendship01",
      "title": "お茶への反応 1",
      "category": "Tea",
      "conditions": {
        "actionId": "Tea",
        "minAffection": 10,
        "requiredItemId": ""
      },
      "lines": [
        {
          "speaker": "Heroine",
          "text": "この香り、好きかも。",
          "expression": "Smile"
        }
      ],
      "imageAssetIds": [
        "TeaReaction_01"
      ],
      "priority": 100,
      "memo": ""
    }
  ]
}
```

Unity 側の対応先は `ActionData.reactions` 内の `ActionReactionData` とする。
`conditions.actionId` に一致する `ActionData` を探し、その action の reactions を JSON 由来で置き換える。
既存 action がない場合は最小の `ActionData` を作成する。
`lines[0]` を `resultMessage`、`imageAssetIds[0]` を `stillId` / `stillSprite` に変換する。

## endings_export.json

エンディング本文を扱う。
Good、Normal、Bad などの結果条件と、対応するエンディングスチルをここに入れる。

```json
{
  "schemaVersion": 1,
  "heroineId": "TestHeroine",
  "items": [
    {
      "id": "Ending_Good",
      "title": "Good Ending",
      "category": "Good",
      "conditions": {
        "minAffection": 80,
        "requiredFlagIds": []
      },
      "lines": [
        {
          "speaker": "Heroine",
          "text": "これからも、隣にいてくれる？",
          "expression": "Smile"
        }
      ],
      "imageAssetIds": [
        "GoodEnding_01"
      ],
      "priority": 100,
      "memo": ""
    }
  ]
}
```

Unity 側の対応先は `EndingData` とする。
item ごとに `Assets/Resources/Heroines/<HeroineId>/Endings/<EndingId>.asset` を作成、更新する。
`lines[]` は改行結合して `message` に入れ、`imageAssetIds[0]` をエンディングスチルとして解決する。

## WPF 画面案

最初は専用の「会話データ」タブを追加する。
画像管理やスチル作業とは分け、文章データだけを集中して編集できるようにする。

現時点では、会話データタブの最小実装として `profile.json` 内に `ConversationEntries` を保存する。
Export 時は `ConversationEntries` を種別ごとに分け、`conversations_export.json`、`game_events_export.json`、`action_reactions_export.json`、`endings_export.json` として出力する。
`ImageAssetIdsText` と `RequiredFlagIdsText` は、改行、カンマ、セミコロン区切りを配列に変換する。
入力補助として、種別ごとのカテゴリ候補、場所、行動、天候、季節、時間帯、表情の候補、Accepted 画像 AssetId の追加、カテゴリに基づく ID 自動生成を用意する。

画面構成の候補は次の通り。

- 左側: データ種別切り替え
- 中央: 選択中データ種別の一覧
- 右側: 選択 item の詳細編集
- 下部: JSON export preview または検証結果
- 一覧上部: `id`、`title`、`category`、条件値、本文、表情を対象にした検索と、カテゴリ、警告あり、画像あり/なし絞り込み

データ種別は次に分ける。

- `Conversations`
- `GameEvents`
- `ActionReactions`
- `Endings`

詳細編集では、最初に次だけ編集できればよい。

- `id`
- `title`
- `category`
- 条件の主要項目
- 台詞行
- 選択肢
- 関連画像 AssetId
- `priority`
- `memo`
- カテゴリ、条件、表情、画像 AssetId の候補反映
- 種別とカテゴリに基づく ID 自動生成

複雑なノード分岐、条件付き選択肢、演出命令、音声参照は後回しにする。
現時点の選択肢は Unity 側の `ConversationChoice` に合わせ、`choiceText`、`responseText`、`affectionChange` の単純な分岐だけを扱う。
Unity importer は `choices[]` を `ConversationDataItem.choices` に復元し、選択肢が 1 件以上ある場合は `ConversationType.Choice` にする。
Unity 側の現行 UI は選択肢 3 件までのため、4 件以上ある場合は warning を出す。

## Unity Import 対応

Unity Editor 拡張は、既存の画像 import と同じ `Export/<HeroineId>/Data/` から JSON を読む。

処理順は次を想定する。

1. `heroine_profile_export.json` を読む。
2. `assets_export.json` を読む。
3. 画像を import し、AssetId から Unity asset path へ解決できる辞書を作る。
4. `conversations_export.json` を読む。
5. `game_events_export.json` を読む。
6. `scheduled_events_export.json` を読む。
7. `action_reactions_export.json` を読む。
8. `endings_export.json` を読む。
9. 各 JSON から ScriptableObject `.asset` を生成、更新する。
10. `imageAssetIds` は `assets_export.json` の情報を使って画像参照へ変換する。
11. `AssetDatabase.SaveAssets` で保存する。

## 検証観点

- `id` が空でない
- 同一 JSON 内で `id` が重複しない
- `heroineId` が profile export と一致する
- `imageAssetIds` が `assets_export.json` に存在する
- `lines` が空でない
- `speaker` と `text` が空でない
- `priority` が数値として扱える
- Unity 側で未知の `category` や条件キーがあっても import が破綻しない

## 未決事項

- `speaker` を文字列にするか enum にするか
- 表情を `expression` 文字列で持つか、画像差分 AssetId で持つか
- 選択肢、分岐、イベントフラグ更新を最初から入れるか
