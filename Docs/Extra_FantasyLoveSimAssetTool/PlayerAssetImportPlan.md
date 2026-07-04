# Player Asset Import Plan

このドキュメントは、プレイヤー戦闘画像を Unity 側へ取り込むための実装メモである。
プレイヤー画像はヒロイン素材や敵素材とは独立した共通素材として扱う。

## 目的

- 戦闘 UI で表示するプレイヤー画像を Unity の Sprite として取り込む。
- `PlayerAssetCatalog` を `Assets/Resources/Player/PlayerAssetCatalog.asset` に作成、更新する。
- BattlePanel が `Battle_Player_Idle` などの `assetId` から Sprite を探せるようにする。
- Unity の `.asset` YAML を Tool 側で直接生成せず、Unity Editor importer が ScriptableObject を作成、更新する。

## Export フォルダ

Unity Editor では次のどちらかのフォルダを選ぶ。

```text
Export/
  Player/
    Images/
      Battle/
        Battle_Player_Idle.png
    Data/
      player_assets_export.json
    Prompts/
      Battle_Player_Idle.prompt.json
```

複数プレイヤーを想定する場合は、次の親フォルダを選んでもよい。

```text
Export/
  Players/
    <PlayerId>/
      Images/
        Battle/
          Battle_Player_Idle.png
      Data/
        player_assets_export.json
      Prompts/
        Battle_Player_Idle.prompt.json
```

現時点の Unity 側は単一プレイヤー運用を前提にし、catalog の保存先は共通で `Assets/Resources/Player/PlayerAssetCatalog.asset` とする。

## Unity 側操作

Unity Editor で次を実行する。

```text
FantasyLoveSim/Import Player Export
```

フォルダ選択では `Export/Player`、または `Export/Players/<PlayerId>` を指定する。
`Export/Players` を指定した場合は、直下の各 `<PlayerId>` フォルダをまとめて取り込む。

Importer は `Data/player_assets_export.json` を読む。
互換用に `Data/assets_export.json` も読むが、プレイヤー素材では `player_assets_export.json` を正とする。

## 作成、更新されるもの

プレイヤー画像 catalog:

```text
Assets/Resources/Player/PlayerAssetCatalog.asset
```

プレイヤー画像:

```text
Assets/Images/Player/Battle/<FileName>
```

画像はコピー後に Texture Type を `Sprite (2D and UI)`、Sprite Mode を `Single` に設定する。

## player_assets_export.json

最小形式:

```json
{
  "schemaVersion": 1,
  "playerId": "Player",
  "unityImageRoot": "Assets/Images/Player",
  "assets": [
    {
      "assetId": "Battle_Player_Idle",
      "usage": "Battle",
      "status": "Accepted",
      "fileName": "Battle_Player_Idle.png",
      "memo": "通常表示",
      "exportImagePath": "Images/Battle/Battle_Player_Idle.png",
      "exportPromptPath": "Prompts/Battle_Player_Idle.prompt.json",
      "unityImagePath": "Assets/Images/Player/Battle/Battle_Player_Idle.png"
    }
  ]
}
```

`assets[]` のうち、`status` が空または `Accepted` のものだけを取り込む。
`unityImagePath` がある場合はそのパスをコピー先に使い、ない場合は次へ fallback する。

```text
Assets/Images/Player/Battle/<FileName>
```

## 標準 AssetId

最初に必要なのは通常表示だけでよい。

| assetId | 用途 |
| --- | --- |
| `Battle_Player_Idle` | 通常表示 |
| `Battle_Player_Attack` | 攻撃 |
| `Battle_Player_Damage` | 被ダメージ |
| `Battle_Player_Victory` | 勝利 |
| `Battle_Player_Defeat` | 敗北 |

## 上書き方針

画像ファイルは最初の実装では既存ファイルを上書きしない。
同じ `unityImagePath` に画像がある場合は、その既存画像を Sprite として catalog に再登録する。

`PlayerAssetCatalog.assets` は import した JSON の内容で置き換える。
同じ JSON 内で `assetId` が重複した場合は後続を warning にしてスキップする。

## 後回しにするもの

- プレイヤー基本情報 profile の import
- 複数プレイヤーごとの別 catalog 保存
- 装備や衣装による画像差し替え
- BattlePanel での Attack / Damage / Victory / Defeat 画像切り替え
