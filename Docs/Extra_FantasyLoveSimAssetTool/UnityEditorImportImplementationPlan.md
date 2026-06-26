# Unity Editor Import Implementation Plan

このドキュメントは、`FantasyLoveSimAssetTool` の `Export/<HeroineId>/` を Unity プロジェクト側で取り込む Editor 拡張の実装メモである。
データ契約そのものは `Docs/UnityImportPlan.md` を正とし、このドキュメントでは Unity 側に置くファイル、クラス、処理順、更新ルールを具体化する。

Unity 側で手修正したデータを WPF Tool 側へ戻す逆方向同期は、通常 Import とは別機能として扱う。
方針は `Docs/UnityToWpfSyncPlan.md` を参照する。

## 目的

- WPF export の JSON と画像を Unity Editor 内で読み込む。
- Unity の ScriptableObject `.asset` を Unity Editor 側で生成、更新する。
- `.meta`、GUID、fileID、ScriptableObject 型情報は Unity に管理させる。
- WPF ツール側では Unity `.asset` を直接生成しない。

## 配置案

Unity プロジェクト側に次の構成を追加する。

```text
Assets/
  FantasyLoveSim/
    Editor/
      HeroineImport/
        HeroineExportImporter.cs
        HeroineExportJsonModels.cs
        HeroineImportReport.cs
    Runtime/
      Heroines/
        HeroineProfileData.cs
        HeroineAssetCatalog.cs
        HeroineLayeredSpriteData.cs
        ConversationData.cs
        GameEventData.cs
        ActionReactionData.cs
        EndingData.cs
```

`Editor/` 配下は Unity Editor 専用にする。
ScriptableObject 型と実行時に参照する表示コンポーネントは `Runtime/` 配下に置く。

## Editor メニュー

最初は次のメニューだけでよい。

```text
Tools/FantasyLoveSim/Import Heroine Export...
```

処理:

1. `EditorUtility.OpenFolderPanel` で `Export/<HeroineId>/` を選ばせる。
2. 選択フォルダに `Data/heroine_profile_export.json` があるか確認する。
3. Import を実行する。
4. 結果を `EditorUtility.DisplayDialog` と Console warning に出す。

将来、直近の export パスを `EditorPrefs` に保存し、再実行メニューを追加してもよい。

## ScriptableObject 型

### HeroineProfileData

`Data/heroine_profile_export.json` から生成、更新する。

主なフィールド:

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

### HeroineAssetCatalog

`Data/assets_export.json` から生成、更新する。

主なフィールド:

- `heroineId`
- `List<HeroineAssetEntry> assets`

`HeroineAssetEntry`:

- `assetId`
- `usage`
- `status`
- `fileName`
- `memo`
- `Sprite sprite`
- `string unityImagePath`
- `string exportPromptPath`

画像参照は `unityImagePath` から `AssetDatabase.LoadAssetAtPath<Sprite>` で解決する。

### HeroineLayeredSpriteData

`Data/sprite_layers_export.json` から生成、更新する。

主なフィールド:

- `heroineId`
- `defaultCostumeId = "Default"`
- `defaultExpressionId = "Neutral"`
- `List<LayerEntry> baseBodyLayers`
- `List<LayerEntry> costumeLayers`
- `List<LayerEntry> expressionLayers`
- `List<LayerEntry> accessoryLayers`

`LayerEntry`:

- `assetId`
- `layerKind`
- `costumeId`
- `expressionId`
- `displayName`
- `drawOrder`
- `Sprite sprite`

`assetId` を更新キーにする。
同じ `assetId` が既存 `.asset` にある場合は上書き更新し、ない場合は追加する。

### ConversationData 系

会話は item ごとに個別 `.asset` を作る。
ゲームイベントは既存の `GameManager` が `Resources.LoadAll<GameEventData>` で読むため、item ごとに個別 `.asset` を作る。
予定イベントは `HeroineProfileData.scheduledEventResourcePath` から読むため、item ごとにヒロイン別 `ScheduledEvents` フォルダへ個別 `.asset` を作る。
行動反応は既存 `ActionData.reactions` を更新し、エンディングは item ごとに個別 `.asset` を作る。

- `conversations_export.json` -> `Conversations/<ConversationId>.asset`
- `game_events_export.json` -> `GameEvents/<EventId>.asset`
- `scheduled_events_export.json` -> `ScheduledEvents/<ScheduledEvent>.asset`
- `action_reactions_export.json` -> `Actions/<Action>.asset` の `ActionData.reactions`
- `endings_export.json` -> `Endings/<EndingId>.asset`

個別 item ごとに `.asset` を分ける運用は、実行時ローダーとの相性を優先する。

## Import 処理順

`HeroineExportImporter` は次の順で処理する。

1. export root を受け取る。
2. `Data/heroine_profile_export.json` を読む。
3. `heroineId` を取得する。
4. Unity 側の出力先フォルダを作る。
5. `Images/` 配下を `Assets/Images/Heroines/<HeroineId>/` へコピーする。
6. `AssetDatabase.Refresh` を実行する。
7. `Data/assets_export.json` を読む。
8. `HeroineProfileData.asset` を作成、更新する。
9. `HeroineAssetCatalog.asset` を作成、更新する。
10. `Data/sprite_layers_export.json` があれば `HeroineLayeredSpriteData.asset` を作成、更新する。
11. 会話系 JSON があれば、それぞれの ScriptableObject を作成、更新する。
12. `AssetDatabase.SaveAssets` を実行する。
13. Import report を表示する。

画像コピーの前に既存画像がある場合は、最初の実装では上書きしてよい。
確認ダイアログは、運用で事故が出てから追加する。

## 保存先

画像:

```text
Assets/Images/Heroines/<HeroineId>/
  Sprites/
  Event/
  Actions/
  Ending/
```

ScriptableObject:

```text
Assets/Resources/Heroines/<HeroineId>/
  HeroineProfileData.asset
  HeroineAssetCatalog.asset
  HeroineLayeredSpriteData.asset
  Conversations/
    <ConversationId>.asset
  GameEvents/
    <EventId>.asset
  ActionReactions.asset
  Endings.asset
```

`.asset` の検索、更新は保存先パスを優先する。
既存 `.asset` を別フォルダから検索して移動する処理は、最初の実装では不要。

## JSON DTO

Unity Editor 側では、WPF 側の JSON を読むための DTO を Editor 専用で持つ。
Runtime の ScriptableObject 型を JSON DTO と兼用しない。

理由:

- JSON 契約と Unity 実行時モデルを分けられる。
- `Sprite` など Unity 参照型を JSON と混ぜずに済む。
- WPF export のフィールド追加に強くなる。

DTO は `HeroineExportJsonModels.cs` にまとめる。
JSON パーサーは Unity 標準の `JsonUtility` でもよいが、トップレベル配列や柔軟性が必要なら `Newtonsoft.Json` を使う。
Unity プロジェクト側の依存方針に合わせる。

## Sprite 解決

`unityImagePath` を基準にする。

```csharp
Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(unityImagePath);
```

Sprite が取れない場合の確認事項:

- 画像が `Assets/Images/Heroines/<HeroineId>/...` にコピーされているか
- `AssetDatabase.Refresh` 後に解決しているか
- Texture Import Settings の Sprite Mode が `Sprite` になっているか

必要なら Importer 側で `TextureImporter.textureType = TextureImporterType.Sprite` を設定する。

## HeroineLayeredSpriteView

レイヤー表示コンポーネントは Importer とは別タスクにする。
実装時の責務は次の通り。

1. `HeroineLayeredSpriteData` を参照する。
2. 現在の `costumeId` と `expressionId` を受け取る。
3. `BaseBody` を必ず表示する。
4. 指定衣装があれば使い、なければ `Default` を使う。
5. 指定表情があれば使い、なければ `Neutral` を使う。
6. 条件に合う `Accessory` を追加する。
7. `drawOrder` 昇順で SpriteRenderer または UI Image に割り当てる。

現在の Unity 側実装は UI Image 版を採用している。
`HeroineLayeredSpriteView` に `BaseBodyImage`、`CostumeImage`、`ExpressionImage`、`AccessoryImage` を割り当て、`drawOrder` に応じて sibling order を調整する。

## Warning

Import report は warning の一覧と import 件数を持つ。
Unity 側の現状実装では、`HeroineImportReport` が copied images、catalog assets、layers、conversations、warning 件数を集計し、完了時に Console summary と `EditorUtility.DisplayDialog` で表示する。
最初に必要な warning は次の通り。

- export root が不正
- 必須 JSON が存在しない
- `schemaVersion` が未対応
- `heroineId` が空
- `assets_export.json` の `assetId` 重複
- 画像コピー元が見つからない
- `unityImagePath` から Sprite を解決できない
- `sprite_layers_export.json` の `layerKind` が未知
- `BaseBody` がない
- `Default` 衣装がない
- `Neutral` 表情がない
- `Costume` なのに `costumeId` が空
- `Expression` なのに `expressionId` が空
- 会話データの `imageAssetIds` が `HeroineAssetCatalog` に存在しない

warning があっても Import は続行する。
JSON が読めない、`heroineId` が取れないなど、キー情報が欠ける場合だけ中断する。

## 更新ルール

- `HeroineProfileData` は `heroineId` が同じなら全フィールド更新する。
- `HeroineAssetCatalog.assets` は `assetId` をキーに更新する。
- `HeroineLayeredSpriteData` の各 layer は `assetId` をキーに更新する。
- 会話系データは `id` をキーに更新する。
- JSON から消えた item を Unity `.asset` から削除するかは、最初は「JSON と同じ一覧に置き換える」でよい。

Unity 側で手編集する項目が増えた場合は、上書きしてよい項目と保持する項目を分ける。
現段階では WPF export を正として扱う。

## 最初の実装範囲

最初に作る範囲は次に絞る。

1. `HeroineProfileData`
2. `HeroineAssetCatalog`
3. 画像コピー
4. Sprite 解決
5. Import report
6. `HeroineLayeredSpriteData`

会話系 ScriptableObject は、画像とレイヤー取り込みが安定してから実装してよい。

`HeroineLayeredSpriteData` は実装済み。
`sprite_layers_export.json` がある場合、`BaseBody`、`Costume`、`Expression`、`Accessory` の各リストに振り分け、`assetId` から `HeroineAssetCatalog` の Sprite を参照する。
表示用の `HeroineLayeredSpriteView` は実装済み。
`OutfitManager` が現在衣装を `costumeId` として保持し、会話表示時に `ConversationData.lines[].expressionId` を渡して表情レイヤーを切り替える。
実行時 fallback は、指定衣装がなければ `Default`、指定表情がなければ `Neutral` を使う。
`BaseBody` がない場合は warning を出し、表示不能として扱う。

次の確認は、WPF export の `sprite_layers_export.json` に `Default` 衣装、`Neutral` / `Smile` / `Sad` 表情を入れ、Unity 側の1つの会話で `Neutral -> Smile -> Sad` の順に表示が切り替わるかを見る。

`game_events_export.json` import は実装済み。
`category` を `GameEventTriggerType` に変換し、`lines[]` を `GameEventData.pages` に変換する。
`imageAssetIds[0]` は `HeroineAssetCatalog` から Sprite 解決し、最初のページのスチルとして設定する。
イベントページの `expressionId` は会話と同じ表情切り替えに使う。

`scheduled_events_export.json` import は実装済み。
`conditions.scheduleType` を `ScheduleType` に変換し、`Assets/Resources/Heroines/<HeroineId>/ScheduledEvents/<ScheduledEvent>.asset` の `ScheduledEventData` を作成、更新する。
`preparationMessage` は朝の予定表示、`eventMessage` は予定実行時の本文に反映する。
`preparationMessage` / `eventMessage` が空の場合は、`lines[0]` を準備文、`lines[1...]` を実行本文として補完する。
`imageAssetIds[0]` は `stillId` / `stillSprite` に解決する。
ヒロイン別予定イベントが存在しない `ScheduleType` は、実行時に共通 `Assets/Resources/ScheduledEvents/` のデータで補完する。
通常行動の `Walk` と翌日予定の外出、デートは別データとして扱い、予定側は `ScheduledEventData` を正とする。

`action_reactions_export.json` import は実装済み。
`conditions.actionId` で既存 `ActionData` を探し、その action の `reactions` を JSON 由来で置き換える。
既存 action がない場合は最小の `ActionData` を作成する。
`lines[0]` を `ActionReactionData.resultMessage`、`imageAssetIds[0]` を `stillId` / `stillSprite`、`conditions.weather` / `season` / `timeOfDay` を条件として反映する。

`endings_export.json` import は実装済み。
item ごとに `Assets/Resources/Heroines/<HeroineId>/Endings/<EndingId>.asset` を作成、更新する。
`lines[]` は改行結合して `EndingData.message` に入れ、`imageAssetIds[0]` を `EndingData.stillSprite` に解決する。

## WPF 側との境界

WPF 側の責務:

- キャラクター情報、画像、prompt、差分定義を管理する。
- `Export/<HeroineId>/` に画像と JSON を出す。
- Export warning で明らかな契約違反を出す。

Unity 側の責務:

- JSON を読み込む。
- 画像を Unity asset として import する。
- ScriptableObject `.asset` を作成、更新する。
- Sprite 参照や Unity 固有の import 設定を管理する。

この境界を維持し、WPF 側から Unity `.asset` を直接編集しない。
