# Unity Import Plan

このドキュメントは、`FantasyLoveSimAssetTool` の export 結果を Unity プロジェクト `FantasyLoveSim` 側へ渡す方法をまとめる。

WPF ツールは Unity の ScriptableObject `.asset` を直接生成しない。
WPF ツールは画像と中間 JSON を出力し、Unity Editor 拡張が Unity Editor 内で JSON を読み込んで `.asset` を生成、更新する。

## 基本方針

- WPF ツールは `Export/<HeroineId>/` を出力する。
- Unity 側は `Export/<HeroineId>/Images/` の画像を `Assets/Images/Heroines/<HeroineId>/` へ取り込む。
- Unity 側は `Export/<HeroineId>/Data/heroine_profile_export.json` と `Export/<HeroineId>/Data/assets_export.json` を読む。
- Unity 側の `.asset` 生成、更新は Unity Editor 拡張が担当する。
- `.meta`、GUID、fileID、ScriptableObject 型情報は Unity Editor に管理させる。
- `Prompts/` 配下の prompt JSON は、生成条件の参照資料として保持する。

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

ScriptableObject の保存先は、Unity プロジェクト側の既存設計に合わせる。
未確定の場合は次を候補にする。

```text
Assets/Resources/Heroines/<HeroineId>/
  HeroineProfileData.asset
  HeroineAssetCatalog.asset
```

画像は `assets_export.json` の `unityImagePath` を基準に参照する。
WPF 側の export では `unityImagePath` を `Assets/Images/Heroines/<HeroineId>/<Usage>/<FileName>` として出力する。

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
11. `Prompts/` 配下の prompt JSON は参照資料としてコピーまたはパスだけ記録する。
12. `AssetDatabase.SaveAssets` で保存する。

## 対応関係

| WPF export | Unity 側用途 |
| --- | --- |
| `Images/<Usage>/<FileName>` | Unity に取り込む画像ファイル |
| `Data/heroine_profile_export.json` | `HeroineProfileData` 生成、更新 |
| `Data/assets_export.json` | 画像一覧、用途、Unity asset path の生成 |
| `Prompts/<AssetId>.prompt.json` | 生成条件の確認、再生成用メモ |
| `Data/*_draft.md` | 会話、イベント、行動反応、エンディングの下書き |

## 会話データの将来拡張

会話データ、イベント、行動反応、エンディング本文は、現時点では Markdown 下書きとして export する。
将来は次の JSON を追加し、Unity Editor 拡張で ScriptableObject 化する。

```text
Data/
  conversations_export.json
  game_events_export.json
  action_reactions_export.json
  endings_export.json
```

このときも WPF 側から `.asset` を直接生成しない。
Unity Editor 側で `ConversationData`、`GameEventData`、`ActionReactionData`、`EndingData` の `.asset` を生成、更新する。

## 未決事項

- Unity 側の ScriptableObject 型名とフィールド名
- `HeroineProfileData.asset` の保存先
- 画像一覧を `HeroineProfileData` に直接持たせるか、別の `HeroineAssetCatalog` に分けるか
- prompt JSON を Unity プロジェクト内へコピーするか、WPF export フォルダ参照のままにするか
- Import 時に既存画像を上書きするか、確認ダイアログを出すか
- 会話データ JSON のスキーマ
