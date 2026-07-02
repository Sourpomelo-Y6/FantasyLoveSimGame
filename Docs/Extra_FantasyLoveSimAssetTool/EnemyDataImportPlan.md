# Enemy Data Import Plan

このドキュメントは、`FantasyLoveSimAssetTool` 側で export した敵キャラ素材を Unity 側へ取り込むための実装メモである。
正確な Tool 側 export 契約は `EnemyExportUnityImportSpec.md` を正とする。

敵キャラ素材はヒロイン素材とは独立して扱う。
ヒロイン用の `Export/<HeroineId>/...` ではなく、Unity 側 importer は `Export/Enemies/<EnemyId>/...` を読む。

## 目的

- Tool 側で作成、採用した敵キャラ画像を Unity の Sprite として取り込む。
- `EnemyData` を `Assets/Resources/Enemies/<EnemyId>.asset` に作成、更新し、既存の `Resources.Load<EnemyData>("Enemies/<EnemyId>")` と接続する。
- 敵画像一覧を `EnemyAssetCatalog` として保存し、戦闘 UI が `assetId` から Sprite を探せるようにする。
- Unity の `.asset` YAML を Tool 側で直接生成せず、Unity Editor importer が ScriptableObject を作成、更新する。

## Export フォルダ

Unity Editor では次のフォルダを選ぶ。

```text
Export/
  Enemies/
    <EnemyId>/
      Images/
        Battle/
          <AssetId>.png
      Data/
        enemy_profile_export.json
        enemy_assets_export.json
      Prompts/
        <AssetId>.prompt.json
```

`enemy.json` は Tool 側の作業データなので、Unity import の正規入力にはしない。

## Unity 側操作

Unity Editor で次を実行する。

```text
FantasyLoveSim/Import Enemy Export
```

フォルダ選択では `Export/Enemies/<EnemyId>` を指定する。
Importer は `Data/enemy_profile_export.json` と `Data/enemy_assets_export.json` を読む。
`Export/Enemies` を指定した場合は、直下の各 `<EnemyId>` フォルダをまとめて取り込む。

## 作成、更新されるもの

敵データ:

```text
Assets/Resources/Enemies/<EnemyId>.asset
```

敵画像 catalog:

```text
Assets/Resources/Enemies/<EnemyId>/EnemyAssetCatalog.asset
```

敵画像:

```text
Assets/Images/Enemies/<EnemyId>/Battle/<FileName>
```

画像はコピー後に Texture Type を `Sprite (2D and UI)`、Sprite Mode を `Single` に設定する。

## enemy_profile_export.json の扱い

現在の Tool 側 profile は次の基本情報だけを持つ。

```json
{
  "schemaVersion": 1,
  "enemyId": "ForestSlime",
  "displayName": "森スライム",
  "enemyType": "Slime",
  "memo": ""
}
```

Unity importer は次を反映する。

| JSON field | Unity field | 備考 |
| --- | --- | --- |
| `enemyId` | `EnemyData.enemyId` | 必須 |
| `displayName` | `EnemyData.displayName` | 空の場合は `enemyId` を使う |

Tool 側 export には現時点で戦闘パラメータ、報酬、勝敗メッセージが含まれない。
そのため既存 `EnemyData` を更新する場合、`battleStatus`、`rewardMoney`、`affectionChangeOnWin`、`victoryMessage`、`defeatMessage` は Unity 側の値を保持する。
新規作成時は `EnemyData` の既定値を使う。

## enemy_assets_export.json の扱い

Importer は `assets[]` のうち、`status` が空または `Accepted` のものだけを取り込む。
`exportImagePath` は `Export/Enemies/<EnemyId>/` からの相対パスとして解決する。
`unityImagePath` がある場合はそのパスをコピー先に使い、ない場合は次へ fallback する。

```text
Assets/Images/Enemies/<EnemyId>/Battle/<FileName>
```

`EnemyAssetCatalog.assets` には次を保存する。

| Unity 側項目 | Tool export の対応 |
| --- | --- |
| `enemyId` | `enemy_assets_export.json.enemyId` |
| `assetId` | `assets[].assetId` |
| `usage` | `assets[].usage` |
| `status` | `assets[].status` |
| `fileName` | `assets[].fileName` |
| `memo` | `assets[].memo` |
| `sprite` | コピー後の Sprite |
| `unityImagePath` | `assets[].unityImagePath` |
| `exportPromptPath` | `assets[].exportPromptPath` |

## 上書き方針

画像ファイルは最初の実装では既存ファイルを上書きしない。
同じ `unityImagePath` に画像がある場合は、その既存画像を Sprite として catalog に再登録する。

`EnemyAssetCatalog.assets` は import した JSON の内容で置き換える。
同じ JSON 内で `assetId` が重複した場合は後続を warning にしてスキップする。

## 最初に必要な敵 ID

現在の探索予定は次の敵 ID を前提にしている。

| 探索先 | enemyId | Resources path |
| --- | --- | --- |
| 森 | `ForestSlime` | `Enemies/ForestSlime` |
| 洞窟 | `CaveBat` | `Enemies/CaveBat` |
| 湖 | `LakeSpirit` | `Enemies/LakeSpirit` |

この 3 つは既存コードと接続済みなので、Tool 側から更新する場合も ID を変えない。
ID を変える場合は、`GameManager.GetExplorationEnemyResourcePath()` 側の対応も同時に変更する。

## 後回しにするもの

- 敵の戦闘パラメータ、報酬、勝敗メッセージの Tool export
- 弱点属性、耐性、スキルリスト
- ドロップアイテム
- 行動パターン AI
- 複数敵編成

これらは本格的な戦闘モードのデータ構造が固まってから追加する。
