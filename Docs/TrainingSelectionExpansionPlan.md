# 訓練選択UI・条件付き訓練の拡張計画

## 目的

訓練数が増えても選択しやすい一覧UIを維持し、次のような訓練をデータで表現できるようにする。

- 調子が「絶好調」の日だけ出現する訓練
- 一度だけ実行でき、完了すると別の訓練を解放する訓練
- 不調・絶不調の日は一覧に表示するが実行できない訓練
- スキルツリー、前提訓練、実績など複数の条件を持つ訓練

この文書は段階実装計画であり、現時点の正本は `TrainingData`、`TrainingPanel`、
`GameManager.IsTrainingUnlocked(...)` とする。一覧の選択・詳細・開始分離、
実行可能フィルター、安定ソート、選択枠のコード接続は実装済み。
Scene上のScrollRectと詳細UI配置は見送り。調子による表示・実行条件、
一回限定、成功完了記録、前提訓練チェーンのコード基盤は実装済みである。

## 現状

`TrainingPanel` は `trainingListParent` の下へ `trainingButtonPrefab` を動的生成するため、
訓練データを増やすこと自体には対応している。一方、次の制約がある。

- 一覧のスクロール、カテゴリー絞り込み、並び順をコード上の契約として定義していない
- 使用可能と未解放の2状態が中心で、「表示されるが今日は実行不可」などを表現できない
- 調子は全訓練の数値補正にだけ使われ、出現条件・実行条件には使われていない
- 永続解放は `unlockedByDefault` とスキルツリーノードから導出する
- 訓練回数実績は中断したセッションも数えるため、「成功完了済み」の判定には使えない

## UI方針

### 基本構成

訓練選択部は固定ボタンを横へ増やさず、一覧と詳細を分離する。

```text
┌ 絞り込み・並び替え ───────────────────┐
│ すべて / 実行可能 / 未完了  カテゴリー▼ │
├ スクロール可能な訓練一覧 ┬ 選択中の詳細 ┤
│ 訓練名                    │ 説明          │
│ 熟練度・状態バッジ        │ 消費・報酬    │
│ 実行不可理由              │ 出現条件      │
│ ...                       │ 開始ボタン    │
└───────────────────────────┴───────────────┘
```

Unity UIでは次を基本とする。

- 一覧外側に `ScrollRect` と `Viewport`、`RectMask2D` を置く
- `Content` に `VerticalLayoutGroup` と `ContentSizeFitter` を設定する
- `trainingListParent` は `Content` を参照する
- `trainingButtonPrefab` は高さを固定または `LayoutElement` で最低高を指定する
- ボタン本文を長文化せず、訓練名、熟練度、短い状態バッジまでに留める
- 詳細説明、消費、報酬、すべての条件と実行不可理由は右側の詳細欄へ表示する
- 選択と実行を分け、一覧項目を押しただけでは訓練を開始しない

訓練数が数十件程度なら既存のPrefab生成方式で問題ない。50～100件を超えて
プロファイル上の負荷が確認された段階で、ボタンの再利用または仮想化を検討する。

### 一覧項目の状態

状態は色だけに依存せず、文字またはアイコンを併記する。

| 状態 | 一覧での扱い | 例 |
| --- | --- | --- |
| 実行可能 | 通常表示、選択可能 | `実行可能` |
| 今日だけ出現 | 強調表示、選択可能 | `絶好調限定` |
| 条件未達 | 表示方針に従い非表示または無効 | `前提未達` |
| 今日実行不可 | 表示するが開始不可 | `不調時不可` |
| 一回限定・未完了 | 強調表示 | `一回限定` |
| 一回限定・完了済み | 原則「完了済み」として無効表示 | `完了` |

完了済み項目を常に消すと、何を達成したか分からなくなる。標準では無効表示し、
「完了済みを隠す」フィルターを用意する。物語上、存在自体を伏せたい訓練だけ
完了後非表示を指定できるようにする。

並び順は次を推奨する。

1. 今日実行可能な期間限定・新規訓練
2. 今日実行可能な通常訓練
3. 今日実行できないが条件を確認できる訓練
4. 完了済みの一回限定訓練

同じ状態の中では `sortOrder`、表示名、`trainingId` の順で安定ソートする。

## 条件モデル

### 表示条件と実行条件を分離する

条件は一つの `bool` にまとめず、評価結果を次の3段階に分ける。

```text
Hidden       一覧に出さない
Disabled     一覧に出すが実行できない。理由を表示する
Available    実行できる
```

`TrainingAvailabilityEvaluator` のような副作用のない評価処理を用意し、
`TrainingPanel`、予定画面、検証ツール、テストで同じ判定を共有する。
評価結果には状態だけでなく、ユーザー表示用の理由一覧を含める。

例:

- 絶好調の日だけ出現: それ以外は `Hidden`
- 不調だと実行できない: 一覧には残して `Disabled`
- 前提訓練完了前は伏せたい: `Hidden`
- スキルツリー未取得を案内したい: `Disabled`

### TrainingDataへ追加する候補

最初から汎用式エンジンを作らず、用途が明確な項目を追加する。

```csharp
int sortOrder;
TrainingConditionRank[] visibleConditionRanks;
TrainingConditionRank[] executableConditionRanks;
TrainingOccurrenceType occurrenceType;
string[] requiredCompletedTrainingIds;
bool requireAllCompletedTrainings;
bool hideUntilPrerequisitesMet;
bool hideAfterCompletion;
```

`TrainingOccurrenceType` の初期候補:

```text
Repeatable        何度でも実行可能
OncePerSave       同じセーブデータで成功完了は一度だけ
```

`OncePerDay` や期間限定回数は、実際に必要になってから追加する。
日ごとの実行回数を早期に追加すると、セーブ形式と日付変更処理が複雑になるためである。

調子配列が空の場合は制限なしとする。これにより既存アセットは変更せず従来動作を維持できる。

### 永続解放との関係

条件の役割を混ぜない。

- スキルツリー取得: 恒久的な訓練解放
- 前提訓練の成功完了: 訓練チェーンの解放
- 当日の調子: 一時的な出現・実行可否
- 一回限定の完了: 再実行防止

最終的に実行可能となるのは、恒久解放、前提完了、回数制限、当日条件をすべて満たした場合である。
前提条件は最初はANDを基本とし、`requireAllCompletedTrainings = false` の場合だけORとして扱う。

### 限定条件を使う際の注意

絶好調限定を完全に隠すと、プレイヤーが存在や出現理由に気付けない可能性がある。
物語上の秘密でなければ、状態画面や訓練一覧へ「絶好調の日に特別な訓練がある」などの
未解放ヒントを表示する。30日周期は日付から再現できるため、予定画面へ将来の調子予報を
出す場合にも同じ `TrainingConditionResolver` を使い、UI独自の周期計算を作らない。

期間限定訓練を通常訓練より常に高効率にすると、その日の日程が事実上固定される。
限定訓練は高報酬だけでなく、物語、初回報酬、後続解放など役割を分け、
実行しなかったことを恒久的な取り返しのつかない失敗にはしない。
一回限定は「一度だけ出現」より「成功するまで再挑戦でき、成功後は完了」とする方が安全である。

## 完了記録とセーブ

既存の `SkillProgressStats.trainingCount` は、1ステップ以上実行した中断セッションも
実績として数える。この値を一回限定の消費や後続訓練の解放に使ってはいけない。

成功完了専用に、次のいずれかを保存する。

```csharp
List<TrainingCompletionRecord> trainingCompletionRecords;

class TrainingCompletionRecord
{
    string trainingId;
    int completionCount;
    int firstCompletedDay;
    int lastCompletedDay;
}
```

最初の実装で一回限定だけを扱う場合は `completedTrainingIds` でもよいが、
将来の「3回完了で解放」「初回完了日によるイベント」を考慮するとレコード形式を推奨する。

成功完了は、次をすべて満たした結果確定時に記録する。

- 1ステップ以上実行している
- `TrainingResult` が終了済み
- 途中中断ではない

画面を閉じた、中断ボタンを押した、開始前に選択を変えただけの場合は消費しない。
記録と後続解放判定は結果確定処理で一度だけ行い、二重通知で加算されないようにする。

### 訓練切り替えとの整合

現在は同じセッション中に訓練を切り替えられる。一回限定やチェーン訓練では
「どの訓練を完了したか」が曖昧になりやすい。

初期実装では次のルールを推奨する。

- ステップ実行前は自由に選び直せる
- 1ステップ実行後は別訓練へ切り替えない
- セッション開始時の `trainingId` を完了対象とする

既存の途中切り替えを残す場合は、一回限定訓練だけ切り替え不可にする方法もあるが、
UI説明と結果集計が複雑になるため、全訓練で統一する方が分かりやすい。

## ユーザー例

### 絶好調限定訓練

```text
trainingId: InspirationTraining
visibleConditionRanks: [Excellent]
executableConditionRanks: [Excellent]
occurrenceType: Repeatable
```

絶好調以外の日には一覧へ出さない。当日は一覧上部へ「絶好調限定」として表示する。

### 不調時に実行できない訓練

```text
trainingId: HighIntensityTraining
visibleConditionRanks: []
executableConditionRanks: [Excellent, Good, Normal]
occurrenceType: Repeatable
```

不調・絶不調の日も一覧で存在を確認できるが、開始ボタンを無効にして
「今日は調子が悪いため実行できません」と表示する。

### 一回限定から後続訓練を解放

```text
trainingId: TrialLesson
occurrenceType: OncePerSave

trainingId: AdvancedLesson
requiredCompletedTrainingIds: [TrialLesson]
requireAllCompletedTrainings: true
hideUntilPrerequisitesMet: true
```

`TrialLesson` を成功完了すると完了レコードを保存し、次回の一覧更新から
`AdvancedLesson` を表示する。中断した場合は完了扱いにせず、再挑戦できる。

## AssetToolとの関係

訓練のコスト、報酬、条件、解放関係はUnity側 `TrainingData` を正本とする。
AssetToolはゲームバランスを編集せず、`training_catalog_from_unity.json` から次を参照する。

- 表示名、カテゴリー
- 一回限定か
- 調子限定か
- 前提訓練ID
- 制作対象として現在表示され得るか

条件付き訓練も画像・セリフ制作が必要なため、現在実行可能な訓練だけでなく、
現在ヒロインで将来出現可能な訓練をカタログへ含める。Tool側では限定条件を
バッジまたは説明として表示し、従来どおり訓練数×表示状態の不足枠を準備できるようにする。

## 検証

`TrainingDataValidator` へ段階的に次を追加する。

- 存在しない前提訓練ID
- 自分自身を前提にする設定
- 前提訓練の循環
- `visibleConditionRanks` と `executableConditionRanks` の矛盾
- 一回限定完了後も自分自身の完了を要求する到達不能設定
- 同一 `sortOrder` は許容するが、安定ソートされること
- スキルツリーにも訓練チェーンにも解放経路がないデータ

自動テストでは少なくとも次を確認する。

- 旧TrainingDataは全調子で従来どおり表示・実行できる
- 絶好調限定は絶好調だけ `Available`
- 不調時不可は不調時に `Disabled` となり理由を返す
- 一回限定は中断で消費されず、成功完了後だけ再実行できない
- 前提完了後に後続訓練が解放される
- セーブ／ロード後も完了状態と解放状態が維持される
- 前提循環をValidatorが検出する

## 実装順

1. `ScrollRect`、詳細欄、絞り込みを追加し、既存データだけで大量表示へ対応する
   - コード接続、実行可能フィルター、安定ソート、選択枠は実装済み
   - Scene上のUI配置と参照設定は未実施
2. `TrainingAvailabilityEvaluator` と調子による表示・実行条件を追加する
   - 実装済み
3. 完了レコードをセーブし、一回限定と前提訓練チェーンを追加する
   - 実装済み
4. Validator、セーブ回帰テスト、評価ロジックテストを追加する
   - 評価ロジックテストと訓練条件Validatorを追加済み
   - セーブ回帰テストは未実装
5. FromUnity訓練カタログとAssetToolの条件表示を拡張する
6. 実データは絶好調限定、不調時不可、一回限定チェーンを各1件ずつ追加して確認する
   - 一回限定の `FirstJointTraining`（初めての合同訓練）を追加済み
   - その成功完了後に表示される反復可能な `AdvancedJointTraining`
     （連携強化訓練）を追加済み。中断では解放されない
   - 絶好調の日だけ表示・実行できる反復可能な `InspirationTraining`
     （ひらめきの特別訓練）を追加済み

最初にUIだけを拡張し、条件付きデータは評価基盤とセーブ回帰テストが揃ってから追加する。
これにより既存の訓練進行、熟練度、実績、画像・セリフ同期への影響を分離できる。
