# Enemy Export / Unity Import Specification

このドキュメントは、`FantasyLoveSimAssetTool` の「敵キャラ素材」タブから出力した enemy export を Unity 側へ取り込むための仕様である。

敵キャラ素材はヒロイン素材とは独立して扱う。
ヒロイン用の `Export/<HeroineId>/...` ではなく、必ず `Export/Enemies/<EnemyId>/...` を読む。

## Tool 側の前提

敵キャラごとの作業データは次に保存される。

```text
Enemies/
  <EnemyId>/
    enemy.json
    Images/
      Battle/
        <AssetId>.png
    Prompts/
      <AssetId>.prompt.json
```

`enemy.json` は Tool 側の作業データであり、Unity import の正規入力ではない。
Unity 側は export 後の `Export/Enemies/<EnemyId>/Data/*.json` を読む。

## Export 対象条件

enemy export に出る Asset は、次をすべて満たすものだけである。

- `Status` が `Accepted`
- `StoredPath` が空ではない
- `Enemies/<EnemyId>/<StoredPath>` に画像ファイルが存在する

`標準候補追加` で追加しただけの Asset は、画像未登録の `Pending` 候補である。
この状態では `enemy_assets_export.json` の `assets` には出ない。

標準候補を Unity へ渡すには、各候補で次の操作が必要。

1. 敵画像一覧で候補を選ぶ
2. `Comfy 送信`
3. `画像取得`
4. `Comfy 採用`
5. Status が `Accepted` になっていることを確認
6. `Enemy Export`

外部で作った画像を使う場合は、候補を選択してから `元画像` に画像パスを入れ、`画像登録` する。

## Export 出力構成

`Enemy Export` を押すと、次の構成で出力する。

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

例:

```text
Export/
  Enemies/
    ForestSlime/
      Images/
        Battle/
          Enemy_ForestSlime_Idle.png
      Data/
        enemy_profile_export.json
        enemy_assets_export.json
      Prompts/
        Enemy_ForestSlime_Idle.prompt.json
```

## enemy_profile_export.json

敵キャラの基本情報を出す。

```json
{
  "schemaVersion": 1,
  "enemyId": "ForestSlime",
  "displayName": "森スライム",
  "enemyType": "Slime",
  "memo": ""
}
```

Unity 側の想定:

- `enemyId`: Unity 側の敵データ ID。ファイル名や Resources 配置にも使う。
- `displayName`: ゲーム内表示名。
- `enemyType`: 分類。現状は画像管理用の補助情報。
- `memo`: Tool 側メモ。Unity 側で不要なら無視してよい。

## enemy_assets_export.json

Unity で使う画像 Asset の一覧を出す。
`assets` には `Accepted` かつ画像登録済みのものだけが入る。

```json
{
  "schemaVersion": 1,
  "enemyId": "ForestSlime",
  "unityImageRoot": "Assets/Images/Enemies/ForestSlime",
  "assets": [
    {
      "assetId": "Enemy_ForestSlime_Idle",
      "usage": "Battle",
      "status": "Accepted",
      "fileName": "Enemy_ForestSlime_Idle.png",
      "memo": "通常画像",
      "exportImagePath": "Images/Battle/Enemy_ForestSlime_Idle.png",
      "exportPromptPath": "Prompts/Enemy_ForestSlime_Idle.prompt.json",
      "unityImagePath": "Assets/Images/Enemies/ForestSlime/Battle/Enemy_ForestSlime_Idle.png"
    }
  ]
}
```

フィールドの意味:

| フィールド | 意味 |
| --- | --- |
| `assetId` | 敵画像を識別する ID。Unity 側の catalog key にする |
| `usage` | 現状は `Battle` のみ |
| `status` | export 対象は `Accepted` のみ |
| `fileName` | export 内と Unity 配置後の画像ファイル名 |
| `memo` | 通常画像、攻撃画像などの補助説明 |
| `exportImagePath` | `Export/Enemies/<EnemyId>/` から見た画像相対パス |
| `exportPromptPath` | `Export/Enemies/<EnemyId>/` から見た prompt JSON 相対パス |
| `unityImagePath` | Unity プロジェクト内にコピーした後の想定パス |

## Unity 側コピー先

Unity importer は、Tool export の画像を次へコピーする。

```text
Export/Enemies/<EnemyId>/Images/Battle/<fileName>
  -> Assets/Images/Enemies/<EnemyId>/Battle/<fileName>
```

`unityImagePath` はこのコピー後のパスと一致する。
Unity 側で Sprite として読む場合は、コピー後に importer 設定を `Sprite (2D and UI)` にする。

## Unity 側 ScriptableObject 案

最小構成では、次のいずれかでよい。

単一 Asset:

```text
Assets/Resources/Enemies/<EnemyId>.asset
```

分割 Asset:

```text
Assets/Resources/Enemies/<EnemyId>/
  EnemyProfileData.asset
  EnemyAssetCatalog.asset
```

`EnemyAssetCatalog` 相当のデータには、少なくとも次を持たせる。

| Unity 側項目 | Tool export の対応 |
| --- | --- |
| `enemyId` | `enemy_assets_export.json.enemyId` |
| `assetId` | `assets[].assetId` |
| `usage` | `assets[].usage` |
| `sprite` | `Assets/Images/Enemies/<EnemyId>/Battle/<fileName>` を Sprite 化したもの |
| `unityImagePath` | `assets[].unityImagePath` |
| `memo` | `assets[].memo` |

## 標準 AssetId

Tool の `標準候補追加` は次を作る。

| assetId | 想定用途 |
| --- | --- |
| `Enemy_<EnemyId>_Idle` | 通常表示 |
| `Enemy_<EnemyId>_Attack` | 攻撃 |
| `Enemy_<EnemyId>_Damage` | 被ダメージ |
| `Enemy_<EnemyId>_Defeat` | 撃破 |

Unity 側の戦闘表示で最低限必要なのは `Enemy_<EnemyId>_Idle`。
他の差分は、Unity 側の戦闘演出が参照する段階で追加する。

## Import 手順

Unity importer は次の順で処理する。

1. `Export/Enemies/<EnemyId>/Data/enemy_profile_export.json` を読む
2. `Export/Enemies/<EnemyId>/Data/enemy_assets_export.json` を読む
3. `enemyId` が両 JSON で一致することを検証
4. `assets[]` を列挙
5. 各 Asset の `exportImagePath` を `Export/Enemies/<EnemyId>/` から解決
6. 画像ファイルが存在することを検証
7. `Assets/Images/Enemies/<EnemyId>/Battle/<fileName>` へコピー
8. コピー先画像の TextureImporter を Sprite 設定にする
9. `unityImagePath` の Sprite を読み込む
10. `EnemyProfileData` / `EnemyAssetCatalog` 相当へ反映
11. AssetDatabase を保存、refresh する

## よくある取り込み失敗

### enemy_assets_export.json の assets が空

原因:

- 画像候補が `Pending` のまま
- `Comfy 採用` または `画像登録` をしていない
- 画像登録後に Status を `Accepted` にしていない

対応:

- 敵画像一覧で対象を選び、画像がプレビュー表示されることを確認する
- Status を `Accepted` にする
- `Enemy Export` を押し直す

### 画像ファイルが export にない

原因:

- `StoredPath` が空
- `Enemies/<EnemyId>/Images/Battle/` の画像ファイルが存在しない
- 登録解除した Asset を export しようとしている

対応:

- `Comfy 採用` または `画像登録` をやり直す
- export 後に `Export/Enemies/<EnemyId>/Images/Battle/` を確認する

### Unity 側のパスと一致しない

原因:

- Unity 側 importer が `Export/<HeroineId>/...` を読んでいる
- 敵画像を `Assets/Images/Event` や `Assets/Images/Heroines` にコピーしている
- `enemy_assets_export.json.assets[].unityImagePath` を使っていない

対応:

- enemy export は必ず `Export/Enemies/<EnemyId>/...` を読む
- コピー先は `Assets/Images/Enemies/<EnemyId>/Battle/` に統一する

### Prompt はあるが画像がない

原因:

- `標準候補追加` は prompt JSON を作るが、画像は作らない
- Comfy 生成後に `Comfy 採用` していない

対応:

- prompt は作画用の入力データであり、Unity import 対象の本体ではない
- Unity へ渡すには画像登録済みの `Accepted` Asset が必要

## 現時点の責務分担

Tool 側:

- 敵プロファイルを作る
- 敵画像候補を作る
- ComfyUI または外部画像で画像を登録する
- `Accepted` の画像だけを export する
- export JSON と画像を `Export/Enemies/<EnemyId>/` に出す

Unity 側:

- `Export/Enemies/<EnemyId>/Data/*.json` を読む
- 画像を `Assets/Images/Enemies/<EnemyId>/Battle/` へコピーする
- Sprite import 設定を行う
- 敵用 ScriptableObject または catalog に `assetId` と Sprite の対応を作る

## 取り込み確認チェックリスト

- `Export/Enemies/<EnemyId>/Data/enemy_profile_export.json` が存在する
- `Export/Enemies/<EnemyId>/Data/enemy_assets_export.json` が存在する
- `enemy_assets_export.json.assets` が 1 件以上ある
- `Export/Enemies/<EnemyId>/Images/Battle/` に PNG がある
- `assets[].exportImagePath` のファイルが実在する
- `assets[].unityImagePath` が `Assets/Images/Enemies/<EnemyId>/Battle/<fileName>` になっている
- Unity 側で同じパスへ画像をコピーしている
- Unity 側で画像が Sprite として import されている
