# Unity To WPF Sync Plan

このドキュメントは、Unity 側で手修正したヒロイン関連データを `FantasyLoveSimAssetTool` 側へ戻すための補助機能案をまとめる。

通常の正方向は、WPF Tool で作成、Export、Unity Editor Import とする。
Unity 側から WPF 側へ戻す機能は、正本を Unity に移すためではなく、Unity 側で発生した手修正を失わないための救済、同期補助として扱う。

## 基本方針

- WPF Tool 側をヒロイン制作データの正本にする。
- Unity `.asset` YAML を WPF Tool が直接読む、編集する運用は避ける。
- Unity Editor 拡張が ScriptableObject を読み、WPF Tool 互換または FromUnity 専用 JSON を出力する。
- WPF Tool は FromUnity JSON を読み、既存データとの差分を確認してから反映する。
- 最初は自動上書きではなく、隔離フォルダへの取り込みと手確認を優先する。

Unity `.asset` には GUID、fileID、Sprite 参照、Unity 固有シリアライズが混ざる。
そのため、WPF Tool 側で `.asset` を直接解析するより、Unity Editor 内で型付き ScriptableObject として読み、必要な情報だけ JSON に戻す方が安全。

## 推奨フロー

```text
Unity ScriptableObject
  -> Unity Editor: Export Heroine Unity Data
  -> UnityImport/FromUnity/<HeroineId>/*.json
  -> WPF Tool: FromUnity JSON Import
  -> 差分確認
  -> profile.json / Definitions / 補助データへ反映
```

出力先は、最初は Unity プロジェクト内またはユーザー指定フォルダにする。
WPF Tool 側ではそのフォルダを選んで読み込む。

推奨出力:

```text
UnityImport/
  FromUnity/
    <HeroineId>/
      actions_from_unity.json
      conversations_from_unity.json
      game_events_from_unity.json
      endings_from_unity.json
      sprite_links_from_unity.json
      export_report.json
```

## 対象優先順位

### 1. ActionData

最初の対象にする。

理由:

- Unity 側でメニュー、表示名、結果文、条件付き反応を手修正しやすい。
- 画像や Sprite 参照より壊れにくい。
- `DefaultHeroine` から `TestHeroine` へ行動メニューをコピーしたような作業を WPF 側へ戻す価値が高い。

想定 JSON:

```text
actions_from_unity.json
```

WPF 側では、最初から完全統合せず、内容確認用の import preview として扱う。
将来的に `ActionReactions` または専用 action 定義へ merge する。

### 2. ConversationData

Unity 側で通常会話を追加、修正した場合の救済用。

出力:

```text
conversations_from_unity.json
```

WPF 側では `ConversationEntries` の `Kind=Conversations` へ merge する候補にする。
同じ `id` がある場合は自動上書きせず、差分確認を行う。

### 3. GameEventData

Unity 側で GameStart、DayStart、Manual、Location などのイベント本文やスチル ID を修正した場合の救済用。

出力:

```text
game_events_from_unity.json
```

WPF 側では `ConversationEntries` の `Kind=GameEvents` へ merge する候補にする。
`category` と `conditions` は `Docs/GameEventDataGuide.md` の運用へ寄せる。

### 4. HeroineAssetCatalog / HeroineLayeredSpriteData

画像実体を戻すのではなく、ID 対応を戻す用途に限定する。

戻してよい情報:

- `assetId`
- `usage`
- `unityImagePath`
- `costumeId`
- `expressionId`
- `layerKind`
- `drawOrder`

戻さない情報:

- Unity の Sprite 参照そのもの
- GUID / fileID
- 画像ファイル本体
- Unity `.meta`

画像本体は WPF Tool の生成物、Export、または手元の素材フォルダを正とする。

### 5. EndingData

Unity 側でエンディング本文、条件、スチル参照を手修正した場合の救済用。

出力:

```text
endings_from_unity.json
```

`HeroineProfileData.endingResourcePath` から `EndingData` を読み、`endingId`、`displayName`、`message`、`stillSprite`、`requiredAffection`、`requiredShownEventIds` を戻す。
Tool 側の `endings_export.json` と合わせるため、`category`、`conditions.minAffection`、`conditions.requiredFlagIds`、`priority` も出力する。
`category` は `endingId` / `displayName` に `Good`、`Normal`、`Bad` が含まれる場合はそれを使い、それ以外は `Ending` とする。
`EndingData` には `stillId` がないため、`imageAssetIds` には `stillSprite.name` を入れる。
画像ファイル本体や Unity の GUID / fileID は戻さない。

## Unity Editor 拡張案

メニュー:

```text
Tools/FantasyLoveSim/Export Heroine Unity Data...
```

処理:

1. `HeroineProfileData` または HeroineId を選ぶ。
2. `Assets/Resources/Heroines/<HeroineId>/` を基準に関連 ScriptableObject を探す。
3. `ActionData`、`ConversationData`、`GameEventData`、`EndingData`、`HeroineAssetCatalog`、`HeroineLayeredSpriteData` を読む。
4. FromUnity JSON DTO へ変換する。
5. `UnityImport/FromUnity/<HeroineId>/` へ JSON を出す。
6. 件数と warning を `export_report.json` と Console に出す。

Unity Editor 拡張側で型付き ScriptableObject を読むため、WPF Tool 側は Unity 固有の `.asset` 形式を知らなくてよい。

## WPF Tool 側 Import 案

最初は専用タブではなく、メニューまたはボタンからフォルダを選ぶ最小実装でよい。

処理:

1. `UnityImport/FromUnity/<HeroineId>/` を選ぶ。
2. `*_from_unity.json` を読む。
3. 対象 `HeroineId` の `profile.json` と比較する。
4. 新規、変更、削除候補を一覧表示する。
5. ユーザーが選んだものだけ反映する。
6. 反映後に `profile.json` を保存する。

最初の実装で差分 UI が重い場合は、次の安全策を取る。

- WPF Tool の `ImportPreview/FromUnity/<HeroineId>/` にコピーする。
- 差分 summary をテキスト表示する。
- 反映は手作業または限定的な `新規のみ追加` にする。

現在の WPF Tool 側の初期実装では、`会話データ` タブの `Unity Action読込` から `actions_from_unity.json` を選び、`ActionReactions` の `ConversationEntry` として新規追加する。
同じ `Id` または同じ `ActionId` が既に存在する場合は上書きせずスキップする。
`Unity 会話読込` から `conversations_from_unity.json` を選んだ場合は、`Conversations` の `ConversationEntry` として新規追加する。
同じ `Id` の通常会話が既に存在する場合は上書きせずスキップする。
`Unity Event読込` から `game_events_from_unity.json` を選んだ場合は、`GameEvents` の `ConversationEntry` として新規追加する。
同じ `Id` のイベントが既に存在する場合は上書きせずスキップする。
Unity 側 exporter が `sourceMetadata.choices` に退避した選択肢は、WPF 側の `Choices` に取り込む。
保持する項目は `choiceText`、`responseText`、`affectionChange` とする。
`Unity Ending読込` から `endings_from_unity.json` を選んだ場合は、`Endings` の `ConversationEntry` として新規追加する想定にする。
同じ `Id` のエンディングが既に存在する場合は上書きせずスキップする。
差分表示、既存データの選択更新、削除同期はまだ行わない。

## Merge 方針

同じ ID がある場合は、無条件上書きしない。

推奨ルール:

| 状態 | 初期対応 |
| --- | --- |
| WPF にない ID | 新規追加候補 |
| WPF と Unity の内容が同じ | 変更なし |
| WPF と Unity の内容が違う | 差分確認候補 |
| Unity 側にないが WPF 側にある | 削除候補にはしない |

削除同期は事故が大きいため、最初は扱わない。
Unity 側から戻す処理は、追加と更新候補の提示に限定する。

## ActionData の初期 JSON 案

`ActionData` は Unity 側の実装名に合わせて調整する。
最初は WPF Tool 側に直接モデルがない可能性があるため、FromUnity 専用 JSON として隔離してよい。

```json
{
  "schemaVersion": 1,
  "heroineId": "TestHeroine",
  "source": "Unity",
  "items": [
    {
      "id": "Tea",
      "displayName": "お茶",
      "category": "DailyAction",
      "requiredItemId": "",
      "requiredFlagIds": [],
      "resultLines": [
        {
          "speaker": "Heroine",
          "text": "この香り、悪くないわね。",
          "expression": "Smile"
        }
      ],
      "imageAssetIds": [
        "Tea_01"
      ],
      "priority": 100,
      "memo": "Unity側から逆export"
    }
  ]
}
```

この JSON をそのまま `action_reactions_export.json` に変換するか、別の action 定義として WPF Tool に持つかは次の設計課題にする。

## Conversation / GameEvent の FromUnity JSON

通常会話とイベントは、既存の WPF export JSON に近い形で戻す。

```text
conversations_from_unity.json
game_events_from_unity.json
endings_from_unity.json
```

フィールドは `Docs/ConversationDataPlan.md` と `Docs/GameEventDataGuide.md` を基準にする。
Unity 側にしかないフィールドは `unity` または `sourceMetadata` のような補助領域に逃がし、WPF の正本フィールドとは混ぜない。

## Warning

Unity Editor 側の逆exportで出す warning:

- HeroineId が取れない。
- 対象 ScriptableObject が見つからない。
- 同じ ID が複数存在する。
- Sprite 参照はあるが `assetId` がない。
- `GameEventData` の category が WPF 側候補にない。
- `imageAssetIds` が HeroineAssetCatalog に存在しない。
- WPF 互換 JSON へ変換できないフィールドがある。

WPF Tool 側の import preview で出す warning:

- 対象 HeroineId の profile がない。
- 同じ ID の既存データと差分がある。
- FromUnity JSON の schemaVersion が未対応。
- category、condition、expression が WPF 側候補にない。
- `imageAssetIds` が WPF 側の `Assets` にない、または `Accepted` ではない。

## やらないこと

初期段階では次をやらない。

- WPF Tool が Unity `.asset` YAML を直接読む。
- WPF Tool が Unity `.asset` を直接書き換える。
- Unity の GUID、fileID、Sprite 参照を WPF Tool の正本データに保存する。
- 画像ファイル本体を Unity から WPF Tool へ戻す。
- Unity 側にないデータを WPF 側から自動削除する。

## 実装順

1. この方針を Unity 側にも共有する。
2. Unity Editor 拡張に `Export Heroine Unity Data...` の空メニューを追加する。
3. `ActionData` だけを `actions_from_unity.json` として出力する。
4. WPF Tool 側に `actions_from_unity.json` の読み込み preview を追加する。
5. 新規 action のみ追加できるようにする。
6. 差分表示と選択 merge を追加する。
7. ConversationData、GameEventData に対象を広げる。
