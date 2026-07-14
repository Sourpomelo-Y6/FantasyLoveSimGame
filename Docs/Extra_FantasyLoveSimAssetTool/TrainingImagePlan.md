# Training Image Plan

このドキュメントは、訓練画面で訓練選択時と LP 消費時に表示するヒロイン画像を切り替え、`FantasyLoveSimAssetTool` で画像生成、採用、export、Unity import まで扱うための設計メモである。

現時点では設計だけを記録し、Unity Runtime、Unity Editor Importer、AssetTool の実装は後続作業とする。

## 目的

- 訓練ボタンを押した時点で、現在の訓練とセッション進行状況に合う画像を表示する。
- 主人公、ヒロイン、双方同時の LP 消費を別画像で表現する。
- ヒロインごとの外見を保った訓練画像を AssetTool で生成、管理できるようにする。
- 共通の `TrainingData` にヒロイン固有 Sprite を直接持たせない。
- 画像や対応データが未設定でも、現在の `TrainingPanel.heroineImage` 表示を維持して進行を止めない。

## 状態の定義

「一度訓練したか」は、セーブデータの累計訓練回数や熟練度ではなく、現在の `TrainingPanel` を開いてから有効なステップを実行したかで判断する。

- 開始前: `currentState == null` または `currentState.elapsedSteps == 0`
- 進行後: `currentState.elapsedSteps > 0`

訓練を途中で別メニューへ切り替えても `elapsedSteps` はリセットしない。したがって、一度でもステップを実行した後に別の訓練ボタンを押した場合は、その訓練の「進行後」画像を表示する。

## 画像切替ルール

| 操作・結果 | 表示状態 | 判定 |
| --- | --- | --- |
| 訓練画面を開き、最初の訓練を選ぶ | `SelectedBeforeFirstStep` | `elapsedSteps == 0` |
| ステップ前に別の訓練ボタンを押す | `SelectedBeforeFirstStep` | `elapsedSteps == 0` |
| 1ステップ以上実行後に訓練ボタンを押す | `SelectedAfterFirstStep` | `elapsedSteps > 0` |
| 通常ステップを実行し、LP消費なし | `SelectedAfterFirstStep` | ステップ成功後 |
| 主人公だけLPを消費 | `PlayerLpConsumed` | 主人公LP差分 > 0、ヒロインLP差分 = 0 |
| ヒロインだけLPを消費 | `HeroineLpConsumed` | 主人公LP差分 = 0、ヒロインLP差分 > 0 |
| 双方が同じステップでLPを消費 | `SimultaneousLpConsumed` | 双方のLP差分 > 0 |

同時消費は個別消費より優先する。LP画像を表示した次の通常ステップでは `SelectedAfterFirstStep` へ戻し、訓練ボタンを押した場合は選択した訓練の進行状況画像へ切り替える。訓練終了後の結果専用画像は今回の範囲に含めない。

LP消費判定はセッション全体の累計カウンターから推測せず、各 `AdvanceStep()` の直前と直後の差分を使う。実装時は `TrainingStepResult` に `playerLpConsumed` / `heroineLpConsumed` を追加し、画像、ログ、実績集計が同じステップ結果を参照できるようにする。

## Unity Runtime データ案

ヒロイン別の ScriptableObject として `HeroineTrainingImageData` を追加する。

```text
Assets/Resources/Heroines/<HeroineId>/TrainingImages/
  HeroineTrainingImageData.asset
```

想定フィールド:

```csharp
string heroineId;
Sprite defaultBeforeFirstStepSprite;
Sprite defaultAfterFirstStepSprite;
Sprite defaultPlayerLpConsumedSprite;
Sprite defaultHeroineLpConsumedSprite;
Sprite defaultSimultaneousLpConsumedSprite;
List<HeroineTrainingImageEntry> entries;
```

各 `HeroineTrainingImageEntry` は次を持つ。

```csharp
string trainingId;
Sprite selectedBeforeFirstStepSprite;
Sprite selectedAfterFirstStepSprite;
Sprite playerLpConsumedSprite;
Sprite heroineLpConsumedSprite;
Sprite simultaneousLpConsumedSprite;
```

訓練別画像があれば優先し、未設定なら同じ状態の共通画像へフォールバックする。共通画像もなければ現在表示中の画像を変更しない。`TrainingData` が削除、改名された場合に備え、存在しない `trainingId` は警告して無視する。

`TrainingPanel` は既存の `heroineImage` をそのまま使用し、新しい Image を必須にしない。表示状態は訓練画面内の一時状態であり、セーブ対象にしない。

## AssetTool の画像用途

`assets_export.json.assets[].usage` に `Training` を追加する。

Tool export:

```text
Export/<HeroineId>/
  Images/Training/
  Data/training_images_export.json
  Prompts/<AssetId>.prompt.json
```

Unity 取り込み先:

```text
Assets/Images/Heroines/<HeroineId>/Training/
```

### assetId とファイル名

訓練別の選択画像:

```text
Training_<TrainingId>_SelectedBeforeFirstStep
Training_<TrainingId>_SelectedAfterFirstStep
```

LP消費画像:

```text
Training_Common_PlayerLpConsumed
Training_Common_HeroineLpConsumed
Training_Common_SimultaneousLpConsumed
```

訓練ごとの差分が必要になった場合は次を許可する。

```text
Training_<TrainingId>_PlayerLpConsumed
Training_<TrainingId>_HeroineLpConsumed
Training_<TrainingId>_SimultaneousLpConsumed
```

ファイル名は原則として `assetId + ".png"` と一致させる。

## 最初に作る画像

初期3訓練について、選択時の前後画像を各2枚用意する。

```text
Training_LightPractice_SelectedBeforeFirstStep.png
Training_LightPractice_SelectedAfterFirstStep.png
Training_SparringPractice_SelectedBeforeFirstStep.png
Training_SparringPractice_SelectedAfterFirstStep.png
Training_EnduranceTraining_SelectedBeforeFirstStep.png
Training_EnduranceTraining_SelectedAfterFirstStep.png
```

LP消費画像は最初は訓練共通の3枚にする。

```text
Training_Common_PlayerLpConsumed.png
Training_Common_HeroineLpConsumed.png
Training_Common_SimultaneousLpConsumed.png
```

最小セットは合計9枚とする。これで表示ロジックを確認してから、必要な訓練だけLP画像を追加する。

## training_images_export.json

画像ファイルの一覧は従来どおり `assets_export.json` を正本とし、`training_images_export.json` は表示状態との対応だけを持つ。

```json
{
  "schemaVersion": 1,
  "heroineId": "TestHeroine",
  "defaults": {
    "beforeFirstStepImageAssetId": "",
    "afterFirstStepImageAssetId": "",
    "playerLpConsumedImageAssetId": "Training_Common_PlayerLpConsumed",
    "heroineLpConsumedImageAssetId": "Training_Common_HeroineLpConsumed",
    "simultaneousLpConsumedImageAssetId": "Training_Common_SimultaneousLpConsumed"
  },
  "items": [
    {
      "trainingId": "LightPractice",
      "beforeFirstStepImageAssetId": "Training_LightPractice_SelectedBeforeFirstStep",
      "afterFirstStepImageAssetId": "Training_LightPractice_SelectedAfterFirstStep",
      "playerLpConsumedImageAssetId": "",
      "heroineLpConsumedImageAssetId": "",
      "simultaneousLpConsumedImageAssetId": "",
      "memo": ""
    }
  ]
}
```

各 `*ImageAssetId` は `assets_export.json` 内の Accepted 画像を参照する。空文字は共通画像または現在画像へのフォールバックを意味する。

## prompt JSON

訓練画像の prompt 記録には次を追加する。

- `usage = Training`
- `heroineId`
- `trainingId`、共通画像なら `Common`
- `trainingVisualState`
- `playerVisible` と `heroineVisible`
- 衣装、表情、ポーズ、構図
- 同じ訓練内で外見と背景を揃えるための参照画像、seed、ControlNet情報

`trainingVisualState` は次の固定値にする。

```text
SelectedBeforeFirstStep
SelectedAfterFirstStep
PlayerLpConsumed
HeroineLpConsumed
SimultaneousLpConsumed
```

画像だけを見て誰のLPが減ったか判断できる構図にする。主人公LP消費とヒロインLP消費で、表情や倒れ方の主体を逆にしない。双方同時画像は片方だけが目立ちすぎない構図にする。

## Unity Import と検証

Unity Editor Importer は次の順で処理する。

1. `Images/Training/` と `assets_export.json` を通常画像と同じ方法で取り込む。
2. `HeroineAssetCatalog` に `usage = Training` の画像を登録する。
3. `training_images_export.json` を読む。
4. `assetId` をカタログから Sprite に解決する。
5. `HeroineTrainingImageData.asset` を作成または更新する。
6. `trainingId`、画像参照、重複状態を検証して結果件数を表示する。

検証対象:

- `heroineId` が profile と一致する。
- `trainingId` が `Resources/Training` に存在する。
- 同一 `trainingId` が重複していない。
- `*ImageAssetId` が Accepted 画像として存在し、`usage = Training` である。
- `trainingVisualState` が既知の値である。
- 同じ `assetId` を意図せず複数の相反する状態へ割り当てていない。

## Git 管理

試作用画像をGit管理しない場合、画像を直接参照する `HeroineTrainingImageData.asset` だけをコミットすると別環境で Missing になる。次のどちらかに統一する。

- 本番素材: PNG、`.meta`、`HeroineAssetCatalog.asset`、`HeroineTrainingImageData.asset` をまとめてコミットする。
- ローカル試作素材: PNGと参照を持つ生成アセットをコミットせず、AssetTool export と再Import手順を正本にする。

Runtime は参照切れや未設定を許容し、画像がないことを理由に訓練処理を停止させない。

## 実装順

1. AssetTool に `Training` 用途、固定状態、9枚の生成・採用枠を追加する。
2. `training_images_export.json` を出力する。
3. Unity Runtime に `HeroineTrainingImageData` と状態判定を追加する。
4. Unity Editor Importer に Training 画像と対応JSONのImportを追加する。
5. TestHeroineで選択前後、主人公LP、ヒロインLP、同時LPを確認する。
6. 表示確認後にDefaultHeroineや追加訓練へ展開する。
