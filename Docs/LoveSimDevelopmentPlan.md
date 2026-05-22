# 恋愛シミュレーションゲーム作成計画

このドキュメントは、今の実装を踏まえた今後の開発方針をまとめたものです。

## 現在の到達点

実装済みの中心ループは以下です。

```text
行動を選ぶ
↓
会話ジャンルを選ぶ、または日常行動を実行する
↓
反応を表示する
↓
好感度が変化する
↓
時間が進む
↓
条件を満たすとエンディングボタンが出る
```

### いまある機能

| 機能 | 内容 |
| ---- | ---- |
| タイトル画面 | `TitleScene` |
| メイン画面 | `MainScene` |
| 会話 | `Daily` / `Food` / `Adventure` / `Love` |
| 日常行動 | `会話` / `休む` / `散歩` / `お茶` / `贈り物` |
| 予定システム | 予定パネルで翌日の予定を選択し、保存・復元できる |
| 選択肢会話 | 2〜3択で好感度変化 |
| 行動反応 | 天候・時間帯・季節・好感度で差分を切り替え |
| 衣装システム | 着用中の衣装に対する反応と評価を保存 |
| 時間経過 | 朝→昼→夜→翌日 |
| 好感度 | 0〜100 |
| エンディング | 好感度100でボタン表示 |

通常の起点は `TitleScene` で、そこから `MainScene` に進む構成を想定しています。

## 今後の方針

最初から全部作ろうとせず、まずは「会話と日常行動で好感度を上げ、条件達成でエンディングを見る」体験を磨くのが優先です。

そのうえで、以下を段階的に追加します。

1. スチル表示
2. スチル回想
3. 立ち絵変更
4. セーブスロット UI の調整
5. ミニゲーム
6. エンディング分岐

着せ替えと衣装評価は、いまの実装で導線と保存が入っているため、今後は評価の種類追加や UI の整理を中心に詰めるとよいです。

## 画面構成の考え方

`MainScene` に主要導線を集約し、必要に応じてサブシステムを足していくのがよいです。
UI デザイン、Unity シーン編集、Inspector の参照設定は手作業で行い、コード側は必要な接続ポイントを用意する方針です。

```text
Canvas
├── BackgroundImage
├── HeroineImage
├── StatusPanel
│   ├── DayText
│   ├── TimeText
│   ├── AffectionText
│   ├── WeatherText
│   └── SeasonText
├── SchedulePanel
├── DialoguePanel
│   ├── SpeakerNameText
│   ├── DialogueText
│   ├── ChoiceButtonArea
│   └── NextButton
└── CommandPanel
    ├── ActionButtonArea
    ├── GenreButtonArea
    └── EndingButton
```

## データ設計の方針

### ConversationData

会話は引き続き `ConversationData` で管理します。
会話の追加は `Assets/Resources/Conversations/` にアセットを置くだけで済むようにするのが目標です。

### ActionData

日常行動は `ActionData` で管理します。

```csharp
public class ActionData : ScriptableObject
{
    public string actionId;
    public string displayName;
    public ActionExecutionType executionType;
    public string resultMessage;
    public string unavailableMessage;
    public bool useHeroineNameAsSpeaker;
    public int affectionChange;
    public bool advanceTime;
    public List<ActionReactionData> reactions;
    public int sortOrder;
    public bool isEnabled;
}
```

### ActionReactionData

条件分岐は `ActionReactionData` に分けると、次の拡張がしやすくなります。

- 時間帯ごとの反応
- 天候ごとの反応
- 季節ごとの反応
- 好感度帯ごとの反応

## 実装メモ

- `GameManager` は状態管理と UI 反映をまとめている
- `BackgroundZoom` は会話開始時の演出用
- 行動の一部は `OpenConversationGenres` のように会話導線へ分岐する
- 予定パネルは `OpenSchedulePanel` で開く
- 予定パネルは戻るボタンで閉じる運用にしている
- 衣装反応は `OpenOutfitReactionPanel` で専用パネルに切り替える
- `Next` ボタンは会話結果、行動結果、選択肢表示の進行を兼ねる
- セーブデータは複数スロット対応済みで、`slot 0` は従来の `save.json` を使う
- セーブロード UI は `Assets/Prefabs/SaveLoadPanel.prefab` として作成済み
- `TitleScene` と `MainScene` の `SaveManager.saveSlotCount` は `4`
- `TitleScene` の `ContinueButton` は `SaveLoadPanel.OpenLoad()` でロード用スロット選択を開く
- `MainScene` の `SaveButton` / `LoadButton` は `SaveLoadPanel.OpenSave()` / `OpenLoad()` でスロット選択を開く
- `SaveLoadPanel` はセーブ時に青背景・`セーブ`、ロード時にオレンジ背景・`ロード` に切り替える
- `MainScene` でロードした後は `SaveLoadPanel` を閉じる

### 案2: `ScheduleType -> ActionId` 変換表

これは「予定を翌日に自動実行する」ための提案で、まだ実装していない。
`ActionId` は既存の `ActionData` と分けて、予約実行専用の内部 ID として扱う想定。

| `ScheduleType` | Proposed `ActionId` | 現在の既存行動への近いフォールバック | 用途 |
| ---- | ---- | ---- | ---- |
| `None` | `None` | なし | 自動実行しない |
| `SoloForest` | `AutoWalkForest` | `Walk` | 森への散歩や探索 |
| `SoloCave` | `AutoWalkCave` | `Walk` | 洞窟への探索 |
| `SoloLake` | `AutoWalkLake` | `Walk` | 湖への散歩 |
| `SoloShopping` | `AutoWalkShopping` | `Walk` | 街への買い物や外出 |
| `DuoForest` | `AutoDuoForest` | `Talk` | 二人で森林デート |
| `DuoCave` | `AutoDuoCave` | `Talk` | 二人で洞窟デート |
| `DuoLake` | `AutoDuoLake` | `Talk` | 二人で湖デート |
| `DuoShopping` | `AutoDuoShopping` | `Talk` | 二人で買い物デート |
| `StayHome` | `AutoStayHome` | `Rest` | 家で過ごす日 |

この方式にすると、翌日の開始時に `ScheduleType` を見て自動で行動イベントへ変換できる。
いまの実装に足すなら、次の順で進めるのがよい。

1. `ScheduleType -> ActionId` の変換関数を追加する
2. 翌日開始時に自動実行のイベントを発火する
3. 自動実行後に通常の行動 UI へ戻す
4. 必要なら `ScheduleType` ごとの専用メッセージを追加する

## 優先度の高い改善候補

1. 行動の反応パターン追加
2. 会話データの整理と命名規則の統一
3. スチル表示と回想の導線追加
4. 立ち絵切り替えと表情差分の整理
5. セーブスロット UI の調整
6. セーブ/ロードの補強
7. UI の視認性改善
8. エンディングの分岐追加

## 補足

- 旧文書に残っていた `SampleScene` や `Assets/Data/Conversations/` は現在の構成とずれているので、今後は `MainScene` と `Assets/Resources/...` を基準にする
- 実装済みの機能を増やす時は、まずデータ追加で済むかを優先して考える
