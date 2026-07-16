# TextMeshPro 日本語フォント設定

このプロジェクトでは、ライセンスと容量の都合により、利用者が用意する日本語フォント、生成したTMP Font Asset、フォントアトラスをGit管理しません。リポジトリをCloneまたはPullした環境ごとに、ローカルで設定してください。

## 前提

- Unity `2021.3.45f2`
- TextMeshPro `3.0.6`
- 日本語グリフを収録し、ゲームへの組み込みがライセンス上許可された`.ttf`または`.otf`

`Window > TextMeshPro`メニューが見つからない場合は、`Window > TextMeshPro > Import TMP Essential Resources`を先に実行してください。Essential Resourcesはプロジェクトに導入済みですが、新しい環境で不足している場合の復旧に利用できます。

## 最短の導入手順

1. 使用許諾を確認した `.ttf` または `.otf` を用意する
2. Unity Projectウィンドウで `Assets/Fonts/Local` を作成し、フォントを配置する
3. `Tools > TextMeshPro > Japanese Font Setup` を開く
4. `Source Font (.ttf / .otf)` に元フォントを指定する
5. 次のいずれかでTMP Font Assetを作成する
   - `Window > TextMeshPro > Font Asset Creator`
   - Projectウィンドウでフォントを右クリックし、`Create > TextMeshPro > Font Asset`
6. 作成先も `Assets/Fonts/Local` 配下にする
7. EditorWindowの `Default Font Asset` に作成したTMP Font Assetを指定する
8. `選択フォントを設定へ保存` を実行する
9. Playして日本語表示を確認する

設定アセットが存在しない環境では、EditorWindowの`設定アセットを作成`から`Assets/Resources/JapaneseFontSettings.asset`を作成できます。このリポジトリには未設定状態のアセットが含まれているため、通常は新規作成不要です。

## Font Asset Creatorの設定

日本語は収録文字数が多いため、最初の確認では次の設定を推奨します。

- `Source Font File`: `Assets/Fonts/Local`へ追加したフォント
- `Sampling Point Size`: `Auto Sizing`またはフォントに合う値
- `Padding`: 初期確認では標準値
- `Atlas Resolution`: 文字の欠け方と使用メモリを見ながら`2048 x 2048`以上を検討
- `Render Mode`: SDF系の標準設定
- `Atlas Population Mode`: 幅広い日本語を扱う試作段階では`Dynamic`を推奨
- `Enable Multi Atlas Support`: 使用文字が多い場合は有効化を検討

`Dynamic`では未登録文字を実行時に追加できるため、会話や名前が増える試作と相性がよい一方、元フォントファイルがBuildに必要です。完成版で使用文字を固定できる場合は`Static`へ切り替え、必要な日本語文字セットを含めてアトラスを生成する方法もあります。

生成後はFont Asset、マテリアル、アトラスを含む関連ファイルを`Assets/Fonts/Local`へまとめてください。これらは`.gitignore`で除外されます。

`JapaneseFontApplier` はゲーム起動時に自動生成され、`Assets/Resources/JapaneseFontSettings.asset` にフォントが設定されている場合だけ、非アクティブを含むロード済みScene内の `TMP_Text` へ適用します。設定アセットやFont Assetがない場合は既存フォントを変更せず、例外も発生させません。CanvasやUIルートへコンポーネントを手動追加する必要はありません。

Sceneロード後に動的生成するUIには、TMP SettingsのDefault Font Assetを利用するか、生成したUIルートに対して `JapaneseFontApplier.ApplyToHierarchy(...)` を呼び出してください。

設定アセットがない、または`defaultFontAsset`が未設定の場合、Runtime処理は何も変更しません。Editorでは未設定警告を1セッションに1回だけConsoleへ表示します。

## 既存Scene・Prefabへの一括適用

EditorWindowには次の補助機能があります。

- 現在開いているSceneだけへ適用する
- プロジェクト内の全Sceneへ適用して保存する
- プロジェクト内の全Prefabへ適用して保存する

非アクティブなGameObjectも対象です。全Scene処理では現在開いているScene構成を処理後に復元します。Prefabは `PrefabUtility.LoadPrefabContents` で個別に処理し、壊れたPrefabなどでエラーが発生しても次のPrefabへ進みます。

現在開いているSceneだけへの適用はSceneをDirty状態にしますが、自動保存しません。Prefabインスタンス上のTMP Textを変更するとPrefab Overrideが作成されるため、保存前に変更内容を確認してください。

通常の動作確認では、Scene・Prefabへ直接適用せず、`JapaneseFontSettings`と起動時の`JapaneseFontApplier`を利用する方法を推奨します。一括適用はEditor上の見た目をローカルで確認したい場合に限って使用してください。

## TMP Settings

TextMeshPro 3.0.6のDefault Font Assetは公開APIから安全に書き換えられないため、ツールはリフレクションを使用しません。新規作成するTextMeshProにも日本語フォントを使う場合は、次から手動設定します。

`Edit > Project Settings > TextMesh Pro > Default Font Asset`

EditorWindowの `TMP Settings.assetを選択` ボタンから設定アセットを選択することもできます。Default Font Assetは新しく作成するTMPコンポーネント向けで、既存コンポーネントには一括適用またはRuntime適用が必要です。

## 動作確認

設定後は次を確認します。

1. Consoleの`Japanese TextMeshPro font is not configured.`警告が次回起動時に出ない
2. `MainScene`をPlayし、会話、ボタン、ステータス、メッセージログの日本語が表示される
3. 非アクティブから開くパネルにも設定フォントが適用される
4. 使用予定の漢字、ひらがな、カタカナ、記号に豆腐文字や欠落がない
5. `git status`で`Assets/Fonts/Local`内のファイルが表示されない

日本語だけが欠ける場合は、元フォントに対象グリフがあるか、Dynamic Font Assetになっているか、Staticアトラスへ必要文字を含めたかを確認してください。

## Git管理

コミットするもの：

- `Assets/Editor/JapaneseFontSetupWindow.cs` と `.meta`
- `Assets/Scripts/Core/JapaneseFontSettings.cs` と `.meta`
- `Assets/Scripts/Core/JapaneseFontApplier.cs` と `.meta`
- 未設定状態の `Assets/Resources/JapaneseFontSettings.asset` と `.meta`
- `.gitignore`、README、本文書

コミットしないもの：

- 利用者が用意した `.ttf` / `.otf` とその `.meta`
- 利用者が生成したTMP Font Assetとその `.meta`
- 生成されたフォントアトラスとその `.meta`
- ローカルFont Assetを割り当てた後の `JapaneseFontSettings.asset` の差分
- ローカルFont Assetを直接適用したScene・Prefabの差分

Git管理されないFont AssetをSceneやPrefabへ直接設定すると、そのGUIDがScene・Prefabへ保存されます。その変更をコミットすると、Font Assetを持たない別環境では参照切れになります。一括適用機能はローカル表示確認用として扱い、通常は未設定状態でGit管理する `JapaneseFontSettings.asset` と起動時の `JapaneseFontApplier` を利用してください。

ローカルフォントを設定すると`JapaneseFontSettings.asset`自体にFont AssetのGUIDが保存されるため、このファイルのローカル差分もコミット対象から外します。コミット前には次で確認してください。

```text
git status --short
```

誤ってScene・Prefabへ直接適用した場合は、必要なローカル変更を確認してから、フォント参照を戻すか、そのScene・Prefabをコミット対象から外してください。
