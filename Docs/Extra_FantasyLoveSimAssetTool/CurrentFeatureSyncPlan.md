# Current Unity Feature Sync Plan

このドキュメントは、Unity側で先行して追加された機能を `FantasyLoveSimAssetTool` の編集、保存、export、FromUnity importへ反映するための同期計画である。

訓練画像の個別仕様は `TrainingImagePlan.md` を参照する。

## 基本方針

- Toolはヒロイン制作データ、文章、画像生成条件の正本とする。
- Unity固有のGUID、fileID、Sprite参照はToolへ持ち込まない。
- 既存JSONに新フィールドがない場合、Unity Importerは既存値を消去しない。
- 新フィールドは省略可能にし、旧exportを引き続き取り込めるようにする。
- IDは表示名と分離し、一度本番利用したIDを安易に変更しない。
- TestHeroineを先行確認対象とし、DefaultHeroineへ反映する前に往復結果を比較する。

## Toolへ同期する項目

### 優先度1: 訓練画像

- `usage = Training`
- `Images/Training/`
- 固定された5つの `trainingVisualState`
- `training_images_export.json`
- prompt、採用状態、Unity path
- Unity側 `HeroineTrainingImageData` との対応

### 優先度2: ゲームイベント条件

Unityの `GameEventData.requiredSkillIds` に対応するため、`game_events_export.json.items[].conditions.requiredSkillIds` を追加する。

Toolの編集画面では文字列の自由入力だけでなく、既知のスキルID候補を選択できるようにする。未知IDも読み込み時に失わず、export前のwarning対象にする。

Unity Importerは `requiredSkillIds` がJSONに存在する場合だけ更新し、省略された旧JSONでは既存の条件を維持する。FromUnity export/importでも同じ配列を往復させる。

### 優先度3: HeroineProfileData（実装済み）

Toolのプロフィール編集、保存、`heroine_profile_export.json`、Unityからの
`heroine_profile_from_unity.json` 読み込みに次の項目を追加済み。

- `initialDialogueMessage`
- `nextActionPrompt`
- `morningGreeting`
- `goodNightGreeting`
- `gameStartFallbackMessage`
- `gameStartFollowUpMessage`
- `outfitMessageOverrides`
- `outfitReactionMessageOverrides`
- `battleSkills`
- 各種Resources path

`battleSkills` はヒロイン固有定義としてToolで編集可能にする。MP、効果、対象、使用確率、優先度、最大使用回数などUnity側の全フィールドを確認してDTOを固定する。旧JSONのImportで既存戦闘スキルを空配列に置き換えない。

共通メッセージ、衣装メッセージ、Resources path、`battleSkills` は
Toolのプロフィール画面から編集できる。Unityからの読み込みではJSONで省略された項目を
既存値の維持として扱い、`battleSkills: []` が明示された場合だけ空配列を反映する。
保存・再読込・export・旧profile JSONの既定値補完はMSTestで回帰確認する。

### 優先度4: ヒロイン別スキルツリーと訓練スキル

ヒロイン固有の戦闘・訓練スキルとツリーノードをToolで制作できるようにする。ただし、主人公共通スキル、敵バランス、共通 `TrainingData` は当面Unityを正本とする。

Tool側の候補JSON:

```text
Data/heroine_skills_export.json
```

保持候補:

- `heroineId`
- ヒロイン戦闘スキルIDと表示用情報
- ヒロイン訓練スキルの `SkillData` 相当値
- `targetHeroineId`
- `grantedHeroineSkillId`
- 必要SP、前提ノード、解放条件、`treePosition`
- 訓練スキルの適用範囲と対象ID

共通データを誤って上書きしないよう、Unity Importerは `targetHeroineId` が対象ヒロインと一致するノードだけを更新する。

### 優先度5: 戦闘・訓練・結果文章

既存の会話系データに加えて、次をToolで編集・同期する候補とする。

- `BattleResultEvents`
- `BattlePanelResultMessages`
- 訓練開始、切替、LP消費、終了時のヒロイン別文章

訓練文章の専用データ型がUnity側で確定するまでは、Toolへ先行して不安定なJSONを追加せず、`training_images_export.json` と画像生成を優先する。

## Toolで扱わない範囲

当面、次はUnity側を正本とする。

- 主人公スキルと主人公スキルツリーノード
- 共通 `TrainingData` のHP消費、報酬、最大ステップ
- `SaveData`、セーブバージョン、実績カウンター
- 敵の戦闘AIとバランス値
- Scene、Prefab、UI参照
- TMP日本語フォント設定

Tool側へ必要なのは、これらのIDを参照候補として読み込む機能であり、最初から全値を編集する機能ではない。

## JSON互換と更新規則

- 追加フィールドはnullableまたは省略可能にする。
- 配列が「未指定」か「空に変更」かを区別する。
- Import前に差分previewを表示し、追加、更新、維持、warningを分ける。
- Unityに存在しないID参照はwarningにし、他の正常項目のImportを継続する。
- 同じIDが重複した場合は自動で片方を選ばず、その項目をskipする。
- FromUnity importは既存Toolデータを無条件上書きせず、新規追加または選択反映にする。
- `schemaVersion` を更新し、読み込み可能な最小・最大versionを明示する。

## 推奨実装順

1. `requiredSkillIds` とHeroineProfile共通セリフを既存export/importへ追加し、欠落による上書きを防ぐ。
2. AssetToolの画像用途に `Training` を追加する。
3. 9枚のTestHeroine訓練画像を生成、採用できるUIを追加する。
4. `training_images_export.json` とUnity Importerを接続する。
5. `battleSkills` の往復対応を追加する。
6. ヒロイン別スキル・ノードのexportを追加する。
7. TestHeroineで Tool -> Unity -> FromUnity -> Tool の往復テストを行う。

## 完了条件

- 旧ToolデータをImportしてもUnity側の新規フィールドが消えない。
- `requiredSkillIds` がToolとUnityの往復で保持される。
- Training画像の生成条件、採用画像、状態対応が再現できる。
- 存在しないスキル、訓練、画像IDがwarningとして確認できる。
- TestHeroineの共通セリフ、戦闘スキル、イベント条件、訓練画像が往復後も一致する。
