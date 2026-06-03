# FantasyLoveSim

Unity で作成された、会話選択と日常行動によって好感度を上げていく恋愛シミュレーションの試作プロジェクトです。

## プレイ画面

![プレイ画面](Docs/Images/play-screen.png)
※画像は開発途中のものです。プレイ画面のスクリーンショットは `Docs/Images/play-screen.png` に配置しています。

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## 制作メモ

- ソースコードの作成には Codex `gpt-5.4` / `gpt-5.5` を使用しました
- 画像の作成には ChatGPT Images 2.0 と Stable Diffusion を使用しました
- Codex / ChatGPT を使った Unity 開発の進め方は [`Docs/CodexUnityWorkflow.md`](Docs/CodexUnityWorkflow.md) にまとめています

## 概要

- 画面下の行動ボタンから `会話` / `休む` / `散歩` / `お茶` / `贈り物` を選べる
- 予定パネルから翌日の予定を設定できる
- 予定によって行動制限、会話候補、衣装選びが少し変化する
- タイトルから新規開始するとゲーム開始イベントを再生できる
- `会話` からは `Daily` / `Food` / `Adventure` / `Love` のジャンル会話に進める
- `贈り物` 以外に、着替えた衣装への反応を選ぶ `褒める` / `嫌う` / `退屈` / `着替える` の導線がある
- 会話は `Next` ボタンまたはメッセージウィンドウクリックで進行し、必要に応じて選択肢を選ぶ
- 行動と会話の結果は ScriptableObject のデータで管理している
- 汎用イベント、予定イベント、エンディングも ScriptableObject で管理している
- ヒロインごとの画像、会話、イベント、行動反応、エンディングを `HeroineProfileData` で束ねる方針
- 衣装ごとの好みや反応履歴を保存し、衣装評価に反映している
- 予定の状態もセーブ/ロードで保存している
- セーブ/ロードは4つのスロットから選択できる
- メッセージログとスチル回想をメイン画面から開ける
- 時間帯と天候に応じて背景画像を切り替えられる
- 時間経過と日数進行がある
- 好感度が一定値に達するとエンディングシーンへ進める

## 動作環境

- Unity `2021.3.45f1`
- URP 2D
- TextMeshPro 使用

## 起動方法

1. Unity Hub でこのフォルダをプロジェクトとして開く
2. `Assets/Scenes/MainScene.unity` を開く
3. Play ボタンで実行する
4. Unity で日本語フォントを使える状態にして、`Assets/Fonts/NotoSansJP-VariableFont_wght.ttf` と `Assets/Fonts/NotoSansJP-VariableFont_wght SDF.asset` を再設定する

## 注意

- 日本語アセットの一部はファイルサイズが大きいため、GitHub へそのままコミットできない場合があります
- セットアップ時に不足があれば、`Docs/Images` や `Assets/Images` 配下の画像を個別に追加してください

## 操作

- 画面下の行動ボタンを押す
- `会話` を押した場合はジャンルボタンから会話を開始する
- 予定を選ぶと予定パネルが開き、翌日の予定を設定する
- 予定パネルは戻るボタンで閉じる
- 会話文が表示されたら `Next` ボタンで進める
- メッセージウィンドウをクリックしても、`Next` ボタン相当の進行ができる
- 選択肢が出たら、表示された選択肢ボタンを押してから `Next` ボタンで確定する
- `休む` / `散歩` / `お茶` / `贈り物` はそのまま実行され、結果表示後に `Next` で戻る
- タイトル画面の `Continue` からロード用スロット選択を開く
- メイン画面の `Save` / `Load` からセーブロード用スロット選択を開く
- セーブ時は青いパネル、ロード時はオレンジのパネルで表示される
- 好感度が `100` に達すると `Ending` ボタンが表示され、`EndingScene` へ遷移する

## ゲームの流れ

- 行動を選ぶ
- 必要なら会話ジャンルを選ぶ
- ヒロインの返答や結果を見る
- 好感度が変化する
- 時間が進む
- 日数が進み、条件を満たすとエンディングへ進める

## 主なファイル

- [`Assets/Scripts/Core/GameManager.cs`](Assets/Scripts/Core/GameManager.cs): 会話、行動、好感度、時間進行、UI 更新の制御
- [`Assets/Scripts/Core/EndingManager.cs`](Assets/Scripts/Core/EndingManager.cs): エンディングシーンの表示とタイトル復帰
- [`Assets/Scripts/Core/SaveLoadPanel.cs`](Assets/Scripts/Core/SaveLoadPanel.cs): セーブロードスロット UI の制御
- [`Assets/Prefabs/SaveLoadPanel.prefab`](Assets/Prefabs/SaveLoadPanel.prefab): タイトル画面とメイン画面で共用するセーブロード UI
- [`Assets/Resources/Heroines/DefaultHeroine/Endings/`](Assets/Resources/Heroines/DefaultHeroine/Endings): 現在ヒロインのエンディングデータ
- [`Assets/Resources/Heroines/DefaultHeroine/GameEvents/`](Assets/Resources/Heroines/DefaultHeroine/GameEvents): 現在ヒロインのゲーム開始、日開始、手動確認用イベント
- [`Assets/Resources/Heroines/`](Assets/Resources/Heroines): ヒロインプロフィールデータ
- [`Assets/Resources/StatusAbilities/`](Assets/Resources/StatusAbilities): 詳細ステータス画面に表示する能力データ
- [`Assets/Scripts/Action/`](Assets/Scripts/Action): 行動データの型定義
- [`Assets/Scripts/Outfit/`](Assets/Scripts/Outfit): 衣装データ、衣装反応、衣装評価の管理
- [`Assets/Scripts/Schedule/`](Assets/Scripts/Schedule): 予定データと予定パネルの制御
- [`Assets/Scripts/Conversation/`](Assets/Scripts/Conversation): 会話データの型定義
- [`Assets/Resources/Heroines/DefaultHeroine/Actions/`](Assets/Resources/Heroines/DefaultHeroine/Actions): 現在ヒロインの行動データ本体
- [`Assets/Resources/Heroines/DefaultHeroine/Actions/ScheduleAction.asset`](Assets/Resources/Heroines/DefaultHeroine/Actions/ScheduleAction.asset): 予定パネルを開く行動データ
- [`Assets/Resources/Outfits/`](Assets/Resources/Outfits): 衣装データ本体
- [`Assets/Resources/Heroines/DefaultHeroine/Conversations/`](Assets/Resources/Heroines/DefaultHeroine/Conversations): 現在ヒロインの会話データ本体
- [`Assets/Sprites/Heroines/DefaultHeroine/`](Assets/Sprites/Heroines/DefaultHeroine): 現在ヒロインの立ち絵
- [`Assets/Images/Heroines/DefaultHeroine/`](Assets/Images/Heroines/DefaultHeroine): 現在ヒロインの行動・イベント・エンディングスチル
- [`Assets/Scenes/MainScene.unity`](Assets/Scenes/MainScene.unity): メインシーン
- [`Assets/Scenes/EndingScene.unity`](Assets/Scenes/EndingScene.unity): エンディングシーン
- [`Packages/manifest.json`](Packages/manifest.json): 利用パッケージ

## メモ

- 画面上の日本語テキストは TextMeshPro のフォント資産を利用しています
- Unity の UI はシーン上で手動配置し、Inspector で参照を割り当てる前提です
- Unity Editor で UI を手作業変更した後は、Codex にシーン編集を依頼する前に必ず `Ctrl+S` でシーンを保存してください。未保存の UI 変更は `MainScene.unity` に存在しないため、後続の scene patch と食い違うことがあります
- 会話データと行動データは ScriptableObject として分離されています
- ヒロイン差し替えは `HeroineProfileData` で読み込みパスを切り替えます。`Images/Background` は共通背景として扱います
- 新ヒロイン追加時の必要データと素材は [`Docs/LoveSimDevelopmentPlan.md`](Docs/LoveSimDevelopmentPlan.md) と [`Docs/Handoff.md`](Docs/Handoff.md) のチェックリストにまとめています
- 行動には条件付き反応を持たせられるので、時間帯、天候、季節、好感度で結果を変えやすいです。反応ごとに `stillSprite` も持てます
- 衣装は着用時に保存され、衣装反応パネルから評価を付けられるようになっています
- 予定は `ScheduleManager` で管理され、保存データにも反映されています
- 予定の保存と復元は動作確認済みです
- セーブスロットは `SaveManager.saveSlotCount` で管理し、現在は4スロットです
- 保存済みスロットには日数と好感度の概要が表示されます
- `slot 0` は従来の `save.json` を使うため、既存セーブとの互換を保っています
- 背景ズーム用の `BackgroundZoom` を使って、会話や窓を見る演出を切り替えています
- 話者ラベルは `SYSTEM` / `予定` / `衣装` / ヒロイン名に分けていますが、メッセージ表示自体は共通なので、今後は表示スタイルの分離も検討しています
- このプロジェクトは試作段階のため、今後 UI や会話データを拡張しやすい構成になっています
