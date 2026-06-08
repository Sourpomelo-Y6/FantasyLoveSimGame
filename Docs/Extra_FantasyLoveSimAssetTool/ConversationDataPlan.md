# Conversation Data Plan

このドキュメントは、`FantasyLoveSimAssetTool` で会話、イベント、行動反応、エンディング本文を作成し、Unity 側へ渡すための設計案をまとめる。

現時点では WPF ツールは画像素材と prompt 記録の管理を主目的にしている。
次の作業では、Unity 側 importer をさらに広げる前に、WPF ツール側で会話データを入力、保存、export できるようにする。
Unity 側は `conversations_export.json` の最小 import があるため、まず Tool 側から通常会話 JSON を出力して検証する。

## 基本方針

- WPF ツールは会話、イベント、行動反応、エンディング本文をキャラクター単位で編集する。
- WPF ツールは Unity の ScriptableObject `.asset` を直接生成しない。
- WPF ツールは中間 JSON を `Export/<HeroineId>/Data/` に出力する。
- Unity Editor 拡張が中間 JSON を読み、Unity Editor 内で ScriptableObject `.asset` を生成、更新する。
- 最初は分岐や演出を過度に複雑にせず、本文、条件、参照画像、メモを確実に渡せる形にする。

## Export JSON

将来追加する export JSON は次を基本にする。

```text
Export/
  <HeroineId>/
    Data/
      conversations_export.json
      game_events_export.json
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

```json
{
  "schemaVersion": 1,
  "heroineId": "TestHeroine",
  "items": [
    {
      "id": "Event_GameStartIntro",
      "title": "ゲーム開始導入",
      "category": "GameStart",
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

Unity 側の対応先は `ActionReactionData` を想定する。

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

Unity 側の対応先は `EndingData` を想定する。

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
- 関連画像 AssetId
- `priority`
- `memo`
- カテゴリ、条件、表情、画像 AssetId の候補反映
- 種別とカテゴリに基づく ID 自動生成

複雑な分岐、選択肢、演出命令、音声参照は後回しにする。

## Tool 側の実装順

Unity 側の `game_events_export.json` import を進める前に、まず `FantasyLoveSimAssetTool` 側の export を安定させる。

1. `profile.json` に `ConversationEntries` を追加する。
2. `ConversationEntry` の内部モデルを作る。
3. 会話データタブを追加し、`Conversations` の一覧、詳細編集、保存を実装する。
4. `conversations_export.json` を出力する。
5. export 前の検証を追加する。
6. Accepted 画像の `AssetId` を候補として `imageAssetIds` に追加できるようにする。
7. `GameEvents`、`ActionReactions`、`Endings` は同じモデルを使って後から種別を広げる。

最初の実装対象は `conversations_export.json` のみでよい。
Unity 側には通常会話の最小 import があるため、Tool 側で作った JSON を Unity に取り込んで、実際に会話ジャンルから表示できるか確認する。

### ConversationEntry 最小項目

`profile.json` に保存する `ConversationEntry` は、最初は次の項目で足りる。

- `Kind`: `Conversation`、`GameEvent`、`ActionReaction`、`Ending`
- `Id`
- `Title`
- `Category`
- `MinAffection`
- `MaxAffection`
- `Weather`
- `Season`
- `TimeOfDay`
- `Once`
- `Lines`
- `ImageAssetIds`
- `Priority`
- `Memo`

`Lines` は複数行を保持できる形にする。
最初は 1 行だけでもよいが、JSON は `lines` 配列として出力する。

### conversations_export.json の初期マッピング

Tool 側の `ConversationEntry` から `conversations_export.json` へは次のように変換する。

| ConversationEntry | conversations_export.json |
| --- | --- |
| `Id` | `items[].id` |
| `Title` | `items[].title` |
| `Category` | `items[].category` |
| `MinAffection` | `items[].conditions.minAffection` |
| `MaxAffection` | `items[].conditions.maxAffection` |
| `Weather` | `items[].conditions.weather` |
| `Season` | `items[].conditions.season` |
| `TimeOfDay` | `items[].conditions.timeOfDay` |
| `Once` | `items[].conditions.once` |
| `Lines` | `items[].lines` |
| `ImageAssetIds` | `items[].imageAssetIds` |
| `Priority` | `items[].priority` |
| `Memo` | `items[].memo` |

`Category` は Unity 側の `ConversationGenre` に合わせ、最初は `Daily`、`Food`、`Adventure`、`Love` を候補にする。
未知のカテゴリを export できるようにしてもよいが、Unity 側では `Daily` にフォールバックするため、Tool 側では候補選択を基本にする。

### Tool 側の検証

export 前に最低限次を検証する。

- `Id` が空でない
- 同一 `Kind` 内で `Id` が重複しない
- `Lines` に空でない本文がある
- `MinAffection` が `MaxAffection` を超えていない
- `Category` が空でない
- `ImageAssetIds` が指定されている場合、Accepted 画像の `AssetId` に存在する
- `Priority` が数値として扱える

## Unity Import 対応

Unity Editor 拡張は、既存の画像 import と同じ `Export/<HeroineId>/Data/` から JSON を読む。

処理順は次を想定する。

1. `heroine_profile_export.json` を読む。
2. `assets_export.json` を読む。
3. 画像を import し、AssetId から Unity asset path へ解決できる辞書を作る。
4. `conversations_export.json` を読む。
5. `game_events_export.json` を読む。
6. `action_reactions_export.json` を読む。
7. `endings_export.json` を読む。
8. 各 JSON から ScriptableObject `.asset` を生成、更新する。
9. `imageAssetIds` は `assets_export.json` の情報を使って画像参照へ変換する。
10. `AssetDatabase.SaveAssets` で保存する。

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

- Unity 側の `ConversationData` などの正確なフィールド名
- 条件キーの確定
- `speaker` を文字列にするか enum にするか
- 表情を `expression` 文字列で持つか、画像差分 AssetId で持つか
- 選択肢、分岐、イベントフラグ更新を最初から入れるか
- 会話データをキャラクター単位でまとめるか、item ごとに `.asset` を分けるか
