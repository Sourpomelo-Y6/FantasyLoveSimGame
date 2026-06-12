# Unity Import Plan

このドキュメントは、`FantasyLoveSimAssetTool` の export 結果を Unity プロジェクト `FantasyLoveSim` 側へ渡す方法をまとめる。

WPF ツールは Unity の ScriptableObject `.asset` を直接生成しない。
WPF ツールは画像と中間 JSON を出力し、Unity Editor 拡張が Unity Editor 内で JSON を読み込んで `.asset` を生成、更新する。

## 基本方針

- WPF ツールと Unity プロジェクトは、原則として別リポジトリのまま運用する。
- WPF ツールは `Export/<HeroineId>/` を出力する。
- Unity 側は `Export/<HeroineId>/Images/` の画像を `Assets/Images/Heroines/<HeroineId>/` へ取り込む。
- Unity 側は `Export/<HeroineId>/Data/heroine_profile_export.json`、`assets_export.json`、必要に応じて `sprite_layers_export.json` と会話データ JSON を読む。
- Unity 側の `.asset` 生成、更新は Unity Editor 拡張が担当する。
- `.meta`、GUID、fileID、ScriptableObject 型情報は Unity Editor に管理させる。
- `Prompts/` 配下の prompt JSON は、生成条件の参照資料として保持する。

## リポジトリ運用

WPF ツールと Unity プロジェクトは、別リポジトリで管理することを推奨する。

```text
FantasyLoveSim/                 Unity 本体リポジトリ
FantasyLoveSimAssetTool/        WPF ツールリポジトリ
FantasyLoveSimExport/           必要なら一時 export 置き場
```

Unity プロジェクトは `Assets/`、`ProjectSettings/`、`.meta`、アセット import 設定など、Unity 固有の差分管理が多い。
WPF ツールは通常の C# アプリとして管理できるため、同じリポジトリにまとめると履歴、差分確認、ビルド環境の責務が混ざりやすい。

両者の共有点はコードではなく、Export / Import のデータ契約に限定する。
共有する契約は次のファイルとフォルダ構成にする。

- `Data/heroine_profile_export.json`
- `Data/assets_export.json`
- `Data/conversations_export.json`
- `Data/game_events_export.json`
- `Data/action_reactions_export.json`
- `Data/endings_export.json`
- `Images/<Usage>/<FileName>`
- `Prompts/<AssetId>.prompt.json`

Unity 側は上記の契約を読み込む Editor 拡張を持つ。
WPF 側はこの契約に沿った export を出す。
どちらかの実装を変更する場合も、まず `Docs/UnityImportPlan.md` の契約を更新し、その後で WPF 側と Unity 側を合わせる。

必要になった場合は、WPF ツール側に Unity プロジェクトの import 用フォルダへ直接 export する設定を追加する。
この場合もリポジトリを統合する必要はなく、出力先パスを設定として持つだけでよい。

## WPF Export 構成

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
      sprite_layers_export.json
      conversations_export.json
      game_events_export.json
      action_reactions_export.json
      endings_export.json
      conversations_draft.md
      game_events_draft.md
      action_reactions_draft.md
      endings_draft.md
    Prompts/
      <AssetId>.prompt.json
```

## Unity 側の取り込み先

画像の取り込み先は次を基本にする。

```text
Assets/Images/Heroines/<HeroineId>/
  Sprites/
  Event/
  Actions/
  Ending/
```

ScriptableObject の保存先は次を基本にする。

```text
Assets/Resources/Heroines/<HeroineId>/
  HeroineProfileData.asset
  HeroineAssetCatalog.asset
  Conversations.asset
  GameEvents.asset
  ActionReactions.asset
  Endings.asset
```

画像は `assets_export.json` の `unityImagePath` を基準に参照する。
WPF 側の export では `unityImagePath` を `Assets/Images/Heroines/<HeroineId>/<Usage>/<FileName>` として出力する。

Unity 側の ScriptableObject 型は次を基本にする。

| WPF export | Unity 側 ScriptableObject | 保存先 |
| --- | --- | --- |
| `heroine_profile_export.json` | `HeroineProfileData` | `Assets/Resources/Heroines/<HeroineId>/HeroineProfileData.asset` |
| `assets_export.json` | `HeroineAssetCatalog` | `Assets/Resources/Heroines/<HeroineId>/HeroineAssetCatalog.asset` |
| `conversations_export.json` | `ConversationData` | `Assets/Resources/Heroines/<HeroineId>/Conversations.asset` |
| `game_events_export.json` | `GameEventData` | `Assets/Resources/Heroines/<HeroineId>/GameEvents.asset` |
| `action_reactions_export.json` | `ActionReactionData` | `Assets/Resources/Heroines/<HeroineId>/ActionReactions.asset` |
| `endings_export.json` | `EndingData` | `Assets/Resources/Heroines/<HeroineId>/Endings.asset` |
| `sprite_layers_export.json` | `HeroineLayeredSpriteData` | `Assets/Resources/Heroines/<HeroineId>/HeroineLayeredSpriteData.asset` |

## heroine_profile_export.json

`heroine_profile_export.json` は、Unity 側で `HeroineProfileData` などのキャラクター基本情報 ScriptableObject を作るための入口にする。

主な項目は次の通り。

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

## assets_export.json

`assets_export.json` は、採用済み画像だけを Unity 側へ渡すための入口にする。

主な項目は次の通り。

- `schemaVersion`
- `heroineId`
- `unityImageRoot`
- `assets`

`assets` の各要素は次を持つ。

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

## Unity Editor Import の処理順

1. ユーザーが Unity Editor メニューから Import を実行する。
2. Import 対象として `Export/<HeroineId>/` フォルダを選ぶ。
3. `Data/heroine_profile_export.json` を読み込む。
4. `Data/assets_export.json` を読み込む。
5. `Images/` 配下の画像を `Assets/Images/Heroines/<HeroineId>/` へコピーする。
6. `AssetDatabase.ImportAsset` または `AssetDatabase.Refresh` で画像を Unity に認識させる。
7. `heroineId` に対応する `HeroineProfileData.asset` を検索する。
8. 既存 `.asset` があれば更新し、なければ作成する。
9. `assets_export.json` の各 `asset` から画像参照を設定する。
10. 必要に応じて `HeroineAssetCatalog.asset` のような画像一覧 ScriptableObject を作る。
11. `sprite_layers_export.json` があれば読み込み、透過レイヤー素材の ScriptableObject `.asset` を生成、更新する。
12. `conversations_export.json`、`game_events_export.json`、`action_reactions_export.json`、`endings_export.json` を読み込む。
13. 会話、イベント、行動反応、エンディング本文の ScriptableObject `.asset` を生成、更新する。
14. 会話データ内の `imageAssetIds` は `assets_export.json` から Unity asset path に解決する。
15. `Prompts/` 配下の prompt JSON は参照資料としてコピーまたはパスだけ記録する。
16. `AssetDatabase.SaveAssets` で保存する。

## 対応関係

| WPF export | Unity 側用途 |
| --- | --- |
| `Images/<Usage>/<FileName>` | Unity に取り込む画像ファイル |
| `Data/heroine_profile_export.json` | `HeroineProfileData` 生成、更新 |
| `Data/assets_export.json` | 画像一覧、用途、Unity asset path の生成 |
| `Data/sprite_layers_export.json` | 透過レイヤー素材一覧、描画順、表情、衣装 ID の生成 |
| `Data/conversations_export.json` | 通常会話データの生成、更新 |
| `Data/game_events_export.json` | ゲームイベント本文データの生成、更新 |
| `Data/action_reactions_export.json` | 行動反応本文データの生成、更新 |
| `Data/endings_export.json` | エンディング本文データの生成、更新 |
| `Prompts/<AssetId>.prompt.json` | 生成条件の確認、再生成用メモ |
| `Data/*_draft.md` | 会話、イベント、行動反応、エンディングの確認用下書き |

## 会話データ

会話データ、イベント、行動反応、エンディング本文は、WPF ツールから次の JSON として export する。
Markdown 下書きは、人間が内容を確認するための補助として併存させる。

```text
Data/
  conversations_export.json
  game_events_export.json
  action_reactions_export.json
  endings_export.json
```

このときも WPF 側から `.asset` を直接生成しない。
Unity Editor 側で `ConversationData`、`GameEventData`、`ActionReactionData`、`EndingData` の `.asset` を生成、更新する。
会話データの JSON スキーマと WPF 画面方針は `Docs/ConversationDataPlan.md` にまとめる。

会話データの ScriptableObject は、各 JSON ファイルごとに1つの `.asset` を作る。
各 `.asset` は `heroineId` と `items` 相当のリストを持つ。
会話 item ごとに個別 `.asset` を分ける運用は、件数が増えてから必要性を再判断する。

Unity 側で受け取る条件値は次を基準にする。
WPF 側は同じ値を入力候補として表示し、Export 時に候補外の値を警告する。
空文字は「条件なし」として扱う。

| 項目 | 値 |
| --- | --- |
| `locationId` | `Forest`, `Lake`, `Cave`, `Room`, `Town` |
| `actionId` | `Tea`, `Rest`, `Walk`, `Gift`, `Talk` |
| `weather` | `Sunny`, `Rainy`, `Cloudy`, `Snow` |
| `season` | `Spring`, `Summer`, `Autumn`, `Winter` |
| `timeOfDay` | `Morning`, `Day`, `Evening`, `Night` |
| `expression` | `Neutral`, `Smile`, `Sad`, `Angry`, `Shy`, `Surprised` |

## 未決事項

- 画像一覧を `HeroineProfileData` に直接持たせるか、別の `HeroineAssetCatalog` に分けるか
- prompt JSON を Unity プロジェクト内へコピーするか、WPF export フォルダ参照のままにするか
- Import 時に既存画像を上書きするか、確認ダイアログを出すか

## 表情、衣装の透過レイヤー方式

表情差分、衣装差分を完成済み立ち絵ではなく透過 PNG レイヤーとして扱う場合は、Unity 側にレイヤー合成用の ScriptableObject と表示コンポーネントを追加する。

追加 export:

```text
Data/
  sprite_layers_export.json
```

`sprite_layers_export.json` は次の形で出力する。

```json
{
  "schemaVersion": 1,
  "heroineId": "TestHeroine",
  "unityImageRoot": "Assets/Images/Heroines/TestHeroine",
  "layers": [
    {
      "assetId": "Expression_Smile",
      "layerKind": "Expression",
      "costumeId": "",
      "expressionId": "Smile",
      "displayName": "レイヤー: 表情 笑顔",
      "drawOrder": 20,
      "fileName": "Expression_Smile.png",
      "exportImagePath": "Images/Sprites/Expression_Smile.png",
      "unityImagePath": "Assets/Images/Heroines/TestHeroine/Sprites/Expression_Smile.png"
    }
  ]
}
```

WPF 側では、`Definitions/layer_assets.json` に定義され、かつ Accepted 画像として登録されているレイヤー素材だけを `layers` に出力する。
未採用のレイヤー素材は Export warning に表示する。

### sprite_layers_export.json の契約

トップレベルの項目は次の通り。

| field | 必須 | 内容 |
| --- | --- | --- |
| `schemaVersion` | 必須 | 現時点では `1` |
| `heroineId` | 必須 | 対象キャラクター ID |
| `unityImageRoot` | 必須 | Unity 側の画像取り込み先 root |
| `layers` | 必須 | 透過レイヤー素材の配列 |

`layers[]` の項目は次の通り。

| field | 必須 | 空欄 | 内容 |
| --- | --- | --- | --- |
| `assetId` | 必須 | 不可 | WPF と Unity の主キー。`assets_export.json` の `assetId` と一致する |
| `layerKind` | 必須 | 不可 | `BaseBody`, `Costume`, `Expression`, `Accessory` のいずれか |
| `costumeId` | 条件付き | 可 | `layerKind == Costume` では必須。それ以外では空欄可 |
| `expressionId` | 条件付き | 可 | `layerKind == Expression` では必須。それ以外では空欄可 |
| `displayName` | 必須 | 不可 | Unity Editor 表示用の名前 |
| `drawOrder` | 必須 | 不可 | 小さい順に背面から前面へ重ねる |
| `fileName` | 必須 | 不可 | 画像ファイル名。原則 `.png` |
| `exportImagePath` | 必須 | 不可 | WPF export フォルダから見た相対画像パス |
| `unityImagePath` | 必須 | 不可 | Unity asset path。`Assets/...` で始まる |

`layerKind` ごとの意味は次の通り。

| layerKind | 意味 | ID ルール |
| --- | --- | --- |
| `BaseBody` | 体、髪、基本シルエットなど、常に表示するベース | `costumeId`, `expressionId` は空欄でよい |
| `Costume` | 衣装差分 | `costumeId` が必須 |
| `Expression` | 表情差分、顔パーツ差分 | `expressionId` が必須 |
| `Accessory` | 任意の小物、装飾 | 必要に応じて `costumeId` や `expressionId` で条件付けしてよい |

Unity 側では、`assetId` を主キーとして既存レイヤーを更新する。
`displayName` は表示用であり、参照キーには使わない。
`drawOrder` は Unity 側の `sortingOrder` や UI 階層順へ変換してよいが、WPF export 上の順序は `drawOrder` の昇順を正とする。

### Unity 側 ScriptableObject 案

Unity 側の想定:

- `HeroineLayeredSpriteData.asset`
  - `BaseBody`
  - `Costume` layers
  - `Expression` layers
  - `Accessory` layers
- `HeroineLayeredSpriteView`
  - 現在の `costumeId` と `expressionId` から表示レイヤーを選ぶ
  - `drawOrder` 順に重ねる
  - 表情がない場合は `Neutral`、衣装がない場合は `Default` へ fallback する

`HeroineLayeredSpriteData` のフィールド案:

```csharp
public class HeroineLayeredSpriteData : ScriptableObject
{
    public string heroineId;
    public string defaultCostumeId = "Default";
    public string defaultExpressionId = "Neutral";
    public List<LayerEntry> baseBodyLayers;
    public List<LayerEntry> costumeLayers;
    public List<LayerEntry> expressionLayers;
    public List<LayerEntry> accessoryLayers;
}
```

`LayerEntry` のフィールド案:

```csharp
[Serializable]
public class LayerEntry
{
    public string assetId;
    public string layerKind;
    public string costumeId;
    public string expressionId;
    public string displayName;
    public int drawOrder;
    public Sprite sprite;
}
```

`HeroineLayeredSpriteView` の責務:

1. `HeroineLayeredSpriteData` を参照する。
2. 現在の `costumeId` と `expressionId` を受け取る。
3. `BaseBody` を常に表示する。
4. `Costume` は現在の `costumeId` に一致するものを表示する。
5. `Expression` は現在の `expressionId` に一致するものを表示する。
6. `Accessory` は常時表示、または条件が一致するものだけ表示する。
7. 表示対象を `drawOrder` 昇順で並べる。

### Unity Import 手順

`sprite_layers_export.json` を取り込む場合、Unity Editor 拡張は次の順で処理する。

1. `Data/sprite_layers_export.json` を読み込む。
2. `schemaVersion == 1` であることを確認する。
3. `layers[]` を `layerKind` ごとに分類する。
4. 各 `layer.unityImagePath` から `Sprite` を読み込む。
5. `Sprite` が見つからない場合は Import warning に出し、そのレイヤーは参照なしで残すか、取り込み対象から外す。
6. `HeroineLayeredSpriteData.asset` を `heroineId` で検索する。
7. 既存 `.asset` があれば `assetId` をキーに更新し、なければ新規作成する。
8. `BaseBody`, `Costume`, `Expression`, `Accessory` の各リストへ `LayerEntry` を設定する。
9. `drawOrder` 昇順で各リスト、または表示時の統合リストを並べる。
10. `AssetDatabase.SaveAssets` で保存する。

Import 時の推奨警告:

- `layers` が空
- `BaseBody` が1件もない
- `Default` の `Costume` がない
- `Neutral` の `Expression` がない
- 同じ `assetId` が複数ある
- 同じ `layerKind + costumeId + expressionId` の表示対象が複数ある
- `unityImagePath` から `Sprite` を解決できない
- `layerKind` が未知
- `Costume` なのに `costumeId` が空
- `Expression` なのに `expressionId` が空

### fallback ルール

実行時の fallback は次を基本にする。

1. 指定された `costumeId` の衣装があればそれを使う。
2. なければ `Default` 衣装を使う。
3. 指定された `expressionId` の表情があればそれを使う。
4. なければ `Neutral` 表情を使う。
5. `BaseBody` がない場合は表示不能としてログまたはエラー表示にする。
6. `Accessory` は条件なしなら常時表示、`costumeId` または `expressionId` が入っている場合は一致時だけ表示する。

WPF 側 Export warning の追加候補:

- Accepted 済みの `BaseBody` がない
- Accepted 済みの `Default` 衣装がない
- Accepted 済みの `Neutral` 表情がない
- `layerKind` が `Costume` なのに `costumeId` が空
- `layerKind` が `Expression` なのに `expressionId` が空
- 同じ `assetId` のレイヤー定義が複数ある
- 同じ `layerKind + costumeId + expressionId` のレイヤー定義が複数ある
- レイヤー画像が透過 PNG ではない
- レイヤー画像のキャンバスサイズが BaseBody と一致しない
- レイヤー画像の縦横比が BaseBody と一致しない

この方式では、会話データの `expression` は完成画像の `AssetId` ではなく、表情レイヤーの `expressionId` として扱う。
衣装は会話本文ではなく、ゲーム状態、季節、イベント、プレイヤー操作などから現在衣装として決める。
詳細は `Docs/ExpressionCostumeVariantRoadmap.md` を参照する。
