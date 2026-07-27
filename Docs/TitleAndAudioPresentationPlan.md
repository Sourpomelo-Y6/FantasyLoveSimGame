# タイトル画面・音響演出計画

タイトル画面の見た目、免責表示、BGM・SE、ボイス再生基盤の実装状況と運用方針をまとめる。
画像・音声の実データは容量、制作状況、ライセンスを考慮し、機能実装とは分けて扱う。

音響コード基盤、オプション、主要ゲームデータへのVoice ID接続、AssetToolとの双方向同期、
ローカル音源検証、実音声不要の回帰テストは実装済みである。本番素材の選定・登録は
ライセンス確認後の別工程として扱う。

## タイトル画面用画像

タイトル画面には専用の背景画像またはキービジュアルを追加する。
画像が未登録でもボタン操作やキャラクター選択ができるようにし、画像参照を必須にはしない。

候補となる構成:

- 作品の舞台を見せる背景とロゴを中心にした構成
- 選択中のヒロイン立ち絵と共通背景を組み合わせる構成
- ヒロインごとにキービジュアルを切り替える構成
- 背景へ半透明の暗いオーバーレイを重ね、文字とボタンの可読性を優先する構成

画像の表示には `Image` の `Preserve Aspect` を使用し、画面比率が変わっても主要部分が
切れすぎないようにする。タイトルロゴ、メニュー、キャラクター選択、セーブスロットが
重ならないよう、背景画像には文字を置くための余白を確保する。

タイトル画面の初期デザイン案:

```text
┌────────────────────────────────────────┐
│               作品ロゴ                  │
│                                        │
│   キービジュアル／選択中ヒロイン        │
│                         New Game       │
│                         Continue       │
│                         Gallery        │
│                         Options        │
│                                        │
│  この作品はフィクションです。           │
│  実在の人物･団体･事件とは一切関係がありません。│
└────────────────────────────────────────┘
```

ロゴやキービジュアルが未完成の間は、TMPの作品名と単色または既存背景で代用する。

## フィクション表記

ゲーム起動後、最初のタイトル画面で次の文言を表示する。

```text
この作品はフィクションです。実在の人物･団体･事件とは一切関係がありません。
```

実装方針:

- `TitleScene` のCanvas内へ `TextMeshProUGUI` を配置する
- 全面の黒背景上へ表示し、閉じるまでは背後のメニューボタンを操作させない
- 小さすぎない文字サイズと十分なコントラストを確保する
- 日本語TMPフォント設定がない環境でも例外を発生させない
- 文言を画像へ焼き込まず、テキストとして保持して修正可能にする

### 現在の実装

初期実装では常時下部表示ではなく、ゲームを起動して最初に `TitleScene` へ入ったときだけ
`DisclaimerArea` を全面表示する。画面をクリックすると閉じ、同じ起動セッション中に
MainSceneからタイトルへ戻った場合は再表示しない。アプリを終了して再起動した場合は再表示する。

表示済み状態はゲーム進行ではないためセーブデータへ保存せず、
`TitleDisclaimerPanel` のセッション中だけの状態として保持する。
背景Imageはクリックを受けるため `Raycast Target` を有効にし、
`FictionDisclaimerText` は親へクリックを通すため無効にする。

## BGM

タイトル、日常、訓練、戦闘、エンディングなどの場面ごとにBGMを切り替えられる基盤を追加する。

実装候補:

- `AudioManager` をシーン間で維持する
- BGM用とSE用の `AudioSource` を分離する
- 同じBGMの重複再生を防ぐ
- 曲切り替え時は短いフェードアウト・フェードインを行う
- 曲が未設定の場合は無音のまま継続し、例外を発生させない
- BGM音量とミュートを端末共通オプションへ保存する

最初の対象:

- タイトルBGM
- 日常画面BGM
- 訓練BGM
- 戦闘BGM
- エンディングBGM

### 現在のBGM・SE基盤

`AudioManager` は実装済み。RuntimeInitializeで自動生成し、`DontDestroyOnLoad` によりScene間で
1個だけ維持する。BGM用とSE用の `AudioSource` を分離し、同じBGMの重複再生防止、
短いフェード切り替え、未設定時の安全な無音動作に対応する。

Scene BGMはアセットのGUIDをSceneへ保存せず、次のResourcesパスから任意ロードする。

| Scene | Resourcesパス | ローカル配置例 |
| --- | --- | --- |
| `TitleScene` | `Audio/Bgm/Title` | `Assets/Resources/Audio/Bgm/Title.ogg` |
| `MainScene` | `Audio/Bgm/Main` | `Assets/Resources/Audio/Bgm/Main.ogg` |
| `EndingScene` | `Audio/Bgm/Ending` | `Assets/Resources/Audio/Bgm/Ending.ogg` |

対応ファイルが存在しない場合はBGMを停止し、例外を発生させない。
戦闘や訓練のようにMainScene内で切り替える場合は、パネル開始時に
`AudioManager.Instance.PlayBgmById(...)` を呼び、終了時にMain用BGMへ戻す。
この切り替えは実装済みで、戦闘開始時は `Audio/Bgm/Battle`、訓練画面開始時は
`Audio/Bgm/Training` を要求する。戦闘結果確定、訓練結果通知、または各パネルを手動で
閉じたときに `Audio/Bgm/Main` へ戻る。同一BGMは重複再生せず、音源未配置時は無音で継続する。

SEは `AudioManager.Instance.PlaySeById(string)` へ論理IDを渡し、
`Assets/Resources/Audio/SE/<ID>.*` から任意ロードする。実音源がない場合は無音のまま継続する。
Sceneロード後、一般的なButtonには名前から決定・キャンセル・次送りSEを自動接続する。
購入、スキル取得、予定、訓練、戦闘のように成否を伴う操作は、自動接続の対象外とし、
処理結果が確定した箇所から専用SEを要求して二重再生を避ける。

現在の規約ID:

| 用途 | 論理ID | ローカル配置例 |
| --- | --- | --- |
| 決定 | `UI/Confirm` | `Assets/Resources/Audio/SE/UI/Confirm.ogg` |
| キャンセル | `UI/Cancel` | `Assets/Resources/Audio/SE/UI/Cancel.ogg` |
| 次送り | `UI/Next` | `Assets/Resources/Audio/SE/UI/Next.ogg` |
| エラー | `UI/Error` | `Assets/Resources/Audio/SE/UI/Error.ogg` |
| 購入成功／失敗 | `Shop/PurchaseSuccess`, `Shop/PurchaseFailed` | `Assets/Resources/Audio/SE/Shop/...` |
| スキル取得成功／失敗 | `Skill/AcquireSuccess`, `Skill/AcquireFailed` | `Assets/Resources/Audio/SE/Skill/...` |
| 予定設定／取消 | `Schedule/Set`, `Schedule/Cancel` | `Assets/Resources/Audio/SE/Schedule/...` |
| 訓練進行／完了／中断 | `Training/Step`, `Training/Complete`, `Training/Cancel` | `Assets/Resources/Audio/SE/Training/...` |
| 戦闘行動 | `Battle/Attack`, `Battle/Defend`, `Battle/Skill`, `Battle/Item` | `Assets/Resources/Audio/SE/Battle/...` |
| 戦闘結果 | `Battle/Victory`, `Battle/Defeat`, `Battle/Escape` | `Assets/Resources/Audio/SE/Battle/...` |
| 回復 | `Battle/Heal` | `Assets/Resources/Audio/SE/Battle/Heal.ogg` |
| イベント開始 | `Event/Start` | `Assets/Resources/Audio/SE/Event/Start.ogg` |

拡張子はUnityが読み込める音声形式でよく、コードには含めない。実音源と `.meta` は
ローカル確認用としてGitへコミットしない。

同一IDのSEには短い再生間隔制限を適用する。一般操作音は0.1秒、購入・取得結果と
イベント開始は0.35秒、戦闘結果と訓練完了は0.75秒を基本とする。別IDのSEは続けて
再生できる。戦闘では回復成功に `Battle/Heal`、回復不能、MP不足、スキル未装備、
アイテム未所持・使用不能などに `UI/Error` を使用する。

### ローカル音源の検証

Unity Editorの次のメニューから、現在コードが要求するBGM・SEの導入状況を確認できる。

```text
FantasyLoveSim
→ Validation
→ Assets
→ Audio Assets
```

Title、Main、Ending、Battle、TrainingのBGM、現在接続済みのSE、およびヒロインデータが
実際に参照するVOICEについて、調査数、検出数、不足数を種類別に表示する。不足した論理ID
ごとのローカル配置例はConsoleへ出力する。VOICE警告には参照元アセットとフィールド位置も
表示し、警告をクリックすると対象アセットを選択できる。音源は任意のため、不足しても
ゲームのコンパイルや実行を停止せず、無音で継続する。

VOICE参照は次のデータから動的に収集する。

- ヒロインプロフィールの共通セリフ
- 通常会話、複数行会話、選択肢への返答
- 行動の条件反応
- ゲームイベントと予定イベント
- 訓練セリフ
- 戦闘結果イベントと戦闘パネル結果
- エンディングページ

空のvoice IDは「ボイスなし」として許可する。同じ音声を複数のセリフから参照することも
許可し、各参照位置を個別に調査する。Resourcesパスは実行時と同じ
`AudioManager.BuildVoiceResourcePath` で生成する。

この検証はローカル専用音源の準備確認なので、Git管理データの整合性を調べる
`Run All Validations` には含めない。

## SE

画面操作とゲーム結果が分かりやすくなるよう、SE再生機能を追加する。

最初の対象:

- 決定
- キャンセル／戻る
- パネルを開く
- 購入成功／購入失敗
- スキル使用
- 攻撃／回復
- スケジュール設定／キャンセル
- イベント開始

連続クリックやログ送りで音が過剰に重ならないよう、必要なSEには短い再生間隔制限を設ける。
SE音量とミュートも端末共通オプションへ保存する。

### 端末共通オプション

`game_options.json` はボイス設定追加によりversion 3へ更新済みで、次を保存する。

- `bgmVolume`: 0～1
- `bgmMuted`
- `seVolume`: 0～1
- `seMuted`

version 1からロードした場合はBGM・SE音量を1、ミュートをOFFとして補完する。
version 1・2のボイス設定は音量1、ミュートOFF、自動再生ONとして補完する。
範囲外の音量は0～1へ丸める。

`GameOptionsPanel` にはUIを後から割り当てられる次のInspector参照を追加済み。

- `Bgm Volume Slider`
- `Bgm Mute Toggle`
- `Se Volume Slider`
- `Se Mute Toggle`

Sliderの `Min Value` は0、`Max Value` は1、`Whole Numbers` はOFFにする。
ToggleとSliderはコード側でイベントを登録するため、Inspectorの `On Value Changed` へ
メソッドを手動登録しない。

## ボイス再生機能

実際のボイスデータは現段階では追加しない。
会話やイベントへ音声を割り当てられる再生基盤を実装済み。

実装済みの要件:

- 会話ページごとに任意の `voiceId` を持てる
- ページ表示時に対応ボイスを一度再生する
- 次のページへ進んだ場合は現在のボイスを停止する
- 同じページのUI更新だけでは重複再生しない
- ボイス未設定・参照切れの場合は無音のまま本文を表示する
- ボイス再生中でもNext操作を妨げない
- ボイス音量、ミュート、自動再生ON/OFFを端末共通オプションへ保存する
- 実音声を必要としない再生要求・データ同期テストを用意する

### 現在のボイス再生基盤

`AudioManager` はボイス専用 `AudioSource` を持ち、BGM・SEとは独立して再生する。
通常会話の `ConversationLineData.voiceId`、旧1行形式の `ConversationData.voiceId`、
`GameEventPageData.voiceId`、`EndingPageData.voiceId` をページ表示時に読み込む。

通常の `voiceId` は次のResourcesパスへ解決する。

```text
Assets/Resources/Audio/Voice/<HeroineId>/<voiceId>.ogg
```

`Audio/Voice/` から始まるIDは共通音声などの完全なResourcesパスとして扱う。
拡張子はデータへ含めない。ファイルが存在しない場合、またはIDが空の場合は、
現在の文章表示を維持したまま無音で進行する。Next操作とページ切り替えでは現在の
ボイスを停止する。

`game_options.json` はversion 3へ更新し、以下を端末共通設定として保存する。

- `voiceVolume`: 0～1
- `voiceMuted`
- `voiceAutoPlay`

version 1・2からの移行時は、音量1、ミュートOFF、自動再生ONを補完する。
`GameOptionsPanel` には後からUIを接続できる `Voice Volume Slider`、
`Voice Mute Toggle`、`Voice Auto Play Toggle` を追加済み。参照が未設定でも例外は発生しない。

### ボイス手動再生

`AudioManager` は現在のページで読み込めたボイスを保持し、`ReplayCurrentVoice()` で先頭から
再生できる。手動再生は `voiceAutoPlay` がOFFでも実行できるが、`voiceMuted` がONの場合は
実行しない。

`GameManager`、`EndingManager`、`TrainingPanel` には任意の `Voice Replay Button` 参照を追加済み。
現在のページに有効な音声がなければボタンを非表示にし、ミュート中は操作不可にする。
ボタンの `On Click` はコード側で登録するため、Scene側でメソッドを手動登録しない。

対象データ候補:

- `ConversationLineData`
- `GameEventPageData`
- `ScheduledEventData`の本文ページ
- `ActionReactionData`
- `EndingPageData`
- 訓練セリフ
- 戦闘結果メッセージ

通常会話、`GameEventPageData`、エンディング、予定イベント、行動反応、選択肢返答、
ヒロイン共通メッセージへの接続は実装済み。
予定イベントは準備用 `preparationVoiceId` と結果用 `eventVoiceId` を分ける。
行動反応は `ActionReactionData.voiceId`、選択肢は
`ConversationChoice.responseVoiceId` を使用する。ヒロイン共通メッセージは
`HeroineProfileData` の初期表示、次行動、朝、就寝前それぞれにボイスIDを持つ。
訓練セリフへの接続も実装済み。`HeroineTrainingDialogueEntry.voicedMessages` に本文と
`voiceId` の組を登録すると、訓練選択・切替・ステップ進行時に自動再生する。
従来の `messages` は互換用として引き続き利用でき、音声付き候補が1件以上ある枠では
音声付き候補を優先する。訓練画面を閉じる、途中終了する、訓練結果が確定する場合は停止する。
戦闘結果への個別接続も実装済み。`BattleResultEventData.voiceId` は戦闘後イベント本文を
会話キューへ表示するときに再生する。`BattlePanelResultMessageData.voiceId` は複数ページに
分割される戦闘ログの先頭ページだけで再生し、ページ送りで同じ音声を繰り返さない。
どちらも既存の `VoiceReplayButton` から手動再生でき、次ページ表示時は前の音声を停止する。

### AssetToolとのVoice ID同期

音声ファイルそのものは同期せず、JSONの文字列 `voiceId` だけを往復する。
通常会話、ゲームイベント、予定イベント、行動反応、エンディングでは各 `lines[]` の
`voiceId` を共通形式として使用する。予定イベントは2行ある場合、1行目を準備音声、
2行目以降の最初の本文を結果音声としてUnityへ取り込む。

訓練セリフは `voicedMessages[]`、戦闘結果は
`battle_result_events_*` / `battle_panel_result_messages_*` の各 `voiceId` を使用する。
UnityのImporterと逆Exporterは、次のデータを対象に双方向同期する。

- 通常会話
- ゲームイベント
- 予定イベントの準備・結果
- 行動の条件反応
- 訓練セリフ
- 戦闘後イベントと戦闘パネル結果文
- エンディングの各ページ

旧JSONに `voiceId` がない場合は空IDとして安全に読み込み、音声なしで本文を維持する。
通常会話・予定イベント・行動反応・エンディングのVoice IDあり／なしと逆Exportは
`VoiceIdHeroineDataSyncIntegrationTests`、ゲームイベントは
`RequiredSkillIdGameEventIntegrationTests`で検証する。

### ローカル動作確認

1. 音声を `Assets/Resources/Audio/Voice/<HeroineId>/<voiceId>.*` へ置く
2. データまたはAssetToolで、拡張子を除いたVoice IDを設定する
3. AssetToolから出力した場合はUnityのヒロインImportを実行する
4. `FantasyLoveSim > Validation > Assets > Audio Assets`で参照と実ファイルを照合する
5. Play Modeで自動再生、Mute、Auto Play、Replay、次ページでの停止を確認する

音源を追加せず同期だけ確認する場合は、上記Integration Testを実行する。

## Git管理

コード、ScriptableObjectの定義、設定用アセット、Scene・Prefab、`.meta` はGit管理対象とする。
制作途中またはライセンスを確認できない画像・音声データはコミットしない。

当面コミットしない対象:

```text
タイトル用テスト画像
*.wav
*.mp3
*.ogg
実際のボイスデータ
ライセンスが確定していないBGM・SE
```

Git管理されない音声アセットをSceneやPrefabから直接参照すると、別環境でGUID参照切れになる。
本番採用する素材はライセンスと配布可否を確認してからGitまたは外部配布手段を決定する。
ローカル素材を使う間は、参照がなくても安全に動作する設計を維持する。

## ユーザー用説明書

プレイヤーがゲームの始め方と主要画面の操作を確認できる、スクリーンショット付きの
HTML説明書を作成する。

想定する保存先:

```text
Docs/UserManual/index.html
Docs/UserManual/css/
Docs/UserManual/images/
```

説明する内容:

- 動作環境とゲームの起動方法
- タイトル画面のNew Game、Continue、Gallery、Options
- キャラクター選択とセーブデータの関係
- メイン画面と時間、天候、好感度、HP・MPの見方
- 会話、行動、訓練、スキルツリー
- 週間・月間スケジュールとテンプレート
- 探索、戦闘、アイテム、ショップ
- 状態詳細、実績、メッセージログ
- セーブ、ロード、タイトルへ戻る操作
- BGM、SE、ボイスなどのオプション
- よくある問題と確認方法

HTMLは外部Webサービスやビルドツールを必要とせず、`index.html` をブラウザーで
直接開いて読める構成を基本とする。PC幅とスマートフォン幅の両方で読めるようにし、
見出し、目次、前後移動、画像キャプション、注意表示を用意する。

スクリーンショットは実装中のテスト画像と区別する。画面構成が確定してから撮影し、
個人情報、ローカルパス、デバッグConsole、未許諾素材が写らないことを確認する。
必要に応じて矢印、枠、番号を加え、本文から対応する操作箇所を参照できるようにする。

Git管理するもの:

- HTML、CSS、必要なJavaScript
- 採用済み画面の説明用スクリーンショット
- スクリーンショットの `.meta` はUnityの `Assets` 内へ置く場合のみ管理する

制作途中の確認画像や、現在未追跡になっているテスト用ゲーム画像は説明書へ流用せず、
完成版として選定したスクリーンショットだけを明示的に追加する。

完成条件:

- 新規ユーザーが説明書だけで新規ゲーム開始、予定設定、訓練または探索、セーブまで行える
- 主要ボタン名が現在のUI表記と一致している
- すべての画像リンクとページ内リンクが切れていない
- 画像がなくても代替テキストで内容を理解できる
- UI変更時に更新すべき説明書箇所を開発文書から確認できる

## 推奨実装順

1. タイトル画面の免責テキスト配置とセッション初回のクリック終了（実装済み）
2. タイトル画像の任意参照と画像なしフォールバックを作る
3. BGM・SEを分離した `AudioManager` を実装する（実装済み）
4. BGM・SE音量とミュートをゲームオプションへ追加する（実装・UI配置済み）
5. ボイスデータなしで動作する共通ボイス再生基盤を実装する（実装済み）
6. 通常会話、イベント、訓練、戦闘結果へ `voiceId` を接続する（実装済み）
7. AssetToolとのVoice ID双方向同期と回帰テストを追加する（実装済み）
8. 素材のライセンス確認後に本番用画像・BGM・SE・ボイスを登録する
9. 主要UIが固まった段階でスクリーンショット付きHTMLユーザー説明書を作成する

タイトル画面の画像配置とHTML説明書のスクリーンショットは、
`Docs/DisplayAndResolutionPlan.md` の基準解像度・確認比率にも従う。
