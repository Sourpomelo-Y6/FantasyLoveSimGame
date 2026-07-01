# Battle Character Image Plan

このドキュメントは、`FantasyLoveSimAssetTool` 側で戦闘画面用のキャラクター表示画像を扱うための設計メモである。

現時点の Unity 側は、通常メニューの `DebugBattleAction` から `BattlePanel` を開くデバッグ入口を想定している。
今後、戦闘画面にプレイヤー、ヒロイン、敵の画像を表示するため、AssetTool 側でも戦闘用画像の用途、命名、export JSON、Unity 取り込み先を整理しておく。

## 目的

- 戦闘 UI で表示する立ち絵、敵画像、ダメージ差分、勝敗差分を Tool 側で管理できるようにする。
- 画像生成 prompt、採用状態、Unity 側 assetId、表示用途を `assets_export.json` で渡せるようにする。
- Unity 側では画像ファイルを `Assets/Images/Heroines/<HeroineId>/Battle/` へ取り込み、必要に応じて `HeroineAssetCatalog` または将来の `BattleCharacterAssetData` から参照できるようにする。
- 最初は見た目確認用の静止画だけを対象にし、アニメーション、エフェクト、装備差分の自動合成は後回しにする。

## Export フォルダ構成

AssetTool 側の export には `Images/Battle/` を追加する。

```text
Export/
  <HeroineId>/
    Images/
      Sprites/
      Event/
      Actions/
      Ending/
      Battle/
    Data/
      heroine_profile_export.json
      assets_export.json
      battle_images_export.json
    Prompts/
      <AssetId>.prompt.json
```

`Battle/` には、戦闘中に表示するヒロイン画像を置く。
敵画像はヒロインに紐づかない共通素材になる可能性が高いため、将来的には共通 export を分けてもよい。
最初は Tool 側の都合を優先し、ヒロイン export 内に敵画像を含めてもよい。

## Unity 側取り込み先

ヒロイン別の戦闘画像:

```text
Assets/Images/Heroines/<HeroineId>/Battle/
```

敵の共通画像を分ける場合:

```text
Assets/Images/Enemies/
```

ScriptableObject は最初から専用型を増やしすぎない。
まずは `assets_export.json` の `usage = Battle` と `assetId` で `HeroineAssetCatalog` に登録し、Unity 側の戦闘 UI が必要になった時点で専用データへ切り出す。

将来の専用データ候補:

```text
Assets/Resources/Heroines/<HeroineId>/BattleAssets/
  BattleCharacterAssets.asset

Assets/Resources/Enemies/
  <EnemyId>.asset
```

## 画像用途

`assets_export.json.assets[].usage` に `Battle` を追加する。

`assetId` は用途が分かる名前にする。

| 用途 | assetId 例 | 備考 |
| --- | --- | --- |
| ヒロイン通常戦闘立ち絵 | `Battle_Heroine_Idle` | 最初に必要 |
| ヒロイン攻撃 | `Battle_Heroine_Attack` | コマンド演出用 |
| ヒロイン被ダメージ | `Battle_Heroine_Damage` | ダメージ演出用 |
| ヒロイン勝利 | `Battle_Heroine_Victory` | 勝利結果用 |
| ヒロイン敗北/撤退 | `Battle_Heroine_Defeat` | 敗北結果用 |
| プレイヤー表示用 | `Battle_Player_Idle` | 主人公を画面に出す場合 |
| 敵通常 | `Battle_Enemy_<EnemyId>_Idle` | `Battle_Enemy_ForestSlime_Idle` など |
| 敵攻撃 | `Battle_Enemy_<EnemyId>_Attack` | 必要になってから |
| 敵被ダメージ | `Battle_Enemy_<EnemyId>_Damage` | 必要になってから |

最初の最小セットは次でよい。

```text
Battle_Heroine_Idle
Battle_Heroine_Attack
Battle_Heroine_Damage
Battle_Enemy_ForestSlime_Idle
```

## ファイル命名

Unity 側で追いやすいように、ファイル名は `assetId` と一致させる。

```text
Images/Battle/Battle_Heroine_Idle.png
Images/Battle/Battle_Heroine_Attack.png
Images/Battle/Battle_Heroine_Damage.png
Images/Battle/Battle_Heroine_Victory.png
Images/Battle/Battle_Heroine_Defeat.png
Images/Battle/Battle_Enemy_ForestSlime_Idle.png
```

差分が増える場合は、末尾に連番ではなく状態名を付ける。

```text
Battle_Heroine_Attack_Sword.png
Battle_Heroine_Attack_Magic.png
Battle_Heroine_Damage_Light.png
Battle_Heroine_Damage_Heavy.png
```

## assets_export.json 追加項目

既存の `assets_export.json` に `usage = Battle` を許可する。
最小項目は既存形式に合わせる。

```json
{
  "assetId": "Battle_Heroine_Idle",
  "usage": "Battle",
  "status": "Accepted",
  "fileName": "Battle_Heroine_Idle.png",
  "memo": "戦闘画面の通常立ち絵",
  "exportImagePath": "Images/Battle/Battle_Heroine_Idle.png",
  "exportPromptPath": "Prompts/Battle_Heroine_Idle.prompt.json",
  "unityImagePath": "Assets/Images/Heroines/TestHeroine/Battle/Battle_Heroine_Idle.png"
}
```

将来的に戦闘画像専用の条件を持たせたい場合は、`battleRole` や `battlePose` を追加する。
ただし最初は `assetId` と `usage` だけで十分に運用できる。

## battle_images_export.json 案

`assets_export.json` だけで足りなくなったら、戦闘表示用の紐づけを `battle_images_export.json` として分離する。

```json
{
  "schemaVersion": 1,
  "heroineId": "TestHeroine",
  "items": [
    {
      "id": "TestHeroineBattleSet",
      "displayName": "テストヒロイン戦闘画像",
      "defaultImageAssetId": "Battle_Heroine_Idle",
      "attackImageAssetId": "Battle_Heroine_Attack",
      "damageImageAssetId": "Battle_Heroine_Damage",
      "victoryImageAssetId": "Battle_Heroine_Victory",
      "defeatImageAssetId": "Battle_Heroine_Defeat",
      "memo": ""
    }
  ]
}
```

Unity 側の将来対応先は `BattleCharacterAssetData` のような ScriptableObject を想定する。

```text
Assets/Resources/Heroines/<HeroineId>/BattleAssets/BattleCharacterAssets.asset
```

最初の段階ではこの JSON は必須にしない。
Unity 側が `HeroineAssetCatalog` から `assetId` で直接探すだけでもよい。

## prompt JSON に残す情報

`Prompts/<AssetId>.prompt.json` には、通常の prompt に加えて戦闘用の見た目制約を残す。

```json
{
  "schemaVersion": 1,
  "assetId": "Battle_Heroine_Attack",
  "usage": "Battle",
  "heroineId": "TestHeroine",
  "pose": "attack",
  "expression": "determined",
  "costumeId": "Adventure",
  "transparentBackground": true,
  "framing": "full body, game battle UI sprite",
  "camera": "front three-quarter view",
  "lighting": "clear game asset lighting",
  "positivePrompt": "",
  "negativePrompt": "",
  "memo": ""
}
```

重要な制約:

- 背景透過を基本にする。
- 画面上で左右反転しても破綻しにくい構図にする。
- 立ち絵として使うため、手足や武器が大きく切れないようにする。
- 通常立ち絵、攻撃、被ダメージで顔や衣装が別人に見えないようにする。
- 既存の `HeroineProfileData.appearancePrompt` と同じキャラクター特徴を使う。

## 画像サイズと import 設定

初期推奨:

- ヒロイン戦闘画像: `1024x1024` または `1024x1536`
- 敵画像: `1024x1024`
- 背景透過 PNG
- Unity Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Single`
- Pixels Per Unit は UI 表示中心なら厳密でなくてよい

BattlePanel では UI Image として表示する想定なので、まずは `Sprite` として取り込めればよい。
アニメーションやパーツ差分が必要になったら、SpriteAtlas やレイヤー分割を検討する。

## Unity Import 処理の追加案

Unity Editor Importer は、`Images/Battle/` を見つけたら次へコピーする。

```text
Export/<HeroineId>/Images/Battle/*.png
  -> Assets/Images/Heroines/<HeroineId>/Battle/*.png
```

`assets_export.json` の `usage = Battle` は `HeroineAssetCatalog` に登録する。
既存の `HeroineAssetEntry.assetId` で検索し、同じ `assetId` があれば Sprite 参照と path を更新する。

将来 `battle_images_export.json` を導入した場合は、Import 順に追加する。

1. `assets_export.json` を読み、Battle 画像をコピー、Sprite 参照を解決する。
2. `HeroineAssetCatalog` を更新する。
3. `battle_images_export.json` があれば読む。
4. `BattleCharacterAssetData` を作成、更新する。
5. `battle_images_export.json` 内の `*ImageAssetId` を `HeroineAssetCatalog` の `assetId` から Sprite へ解決する。

## 最初に作るべき画像

本格的に増やす前に、デバッグ BattlePanel 表示確認用として次だけ用意する。

```text
Battle_Heroine_Idle.png
Battle_Enemy_ForestSlime_Idle.png
```

これで BattlePanel にヒロイン側 Image と敵側 Image を追加したとき、表示・サイズ・アンカー・左右配置を確認できる。
攻撃差分やダメージ差分は、BattlePanel の画像表示が安定してから追加する。

## 後回しにするもの

- 戦闘アニメーション
- 武器や衣装による自動差分合成
- 敵ごとの大量差分
- エフェクト画像
- HP 低下に応じた表情変化
- 予定探索から戦闘モードへ入ったときの画像自動切り替え
- 勝敗結果から戦闘後イベント画像へ接続する処理
