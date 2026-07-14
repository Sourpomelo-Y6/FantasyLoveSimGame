# TextMeshPro 日本語フォント設定

このプロジェクトでは、ライセンスと容量の都合により、利用者が用意する日本語フォント、生成したTMP Font Asset、フォントアトラスをGit管理しません。リポジトリをCloneまたはPullした環境ごとに、ローカルで設定してください。

## 導入手順

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

`JapaneseFontApplier` はゲーム起動時に自動生成され、`Assets/Resources/JapaneseFontSettings.asset` にフォントが設定されている場合だけ、非アクティブを含むロード済みScene内の `TMP_Text` へ適用します。設定アセットやFont Assetがない場合は既存フォントを変更せず、例外も発生させません。CanvasやUIルートへコンポーネントを手動追加する必要はありません。

Sceneロード後に動的生成するUIには、TMP SettingsのDefault Font Assetを利用するか、生成したUIルートに対して `JapaneseFontApplier.ApplyToHierarchy(...)` を呼び出してください。

## 既存Scene・Prefabへの一括適用

EditorWindowには次の補助機能があります。

- 現在開いているSceneだけへ適用する
- プロジェクト内の全Sceneへ適用して保存する
- プロジェクト内の全Prefabへ適用して保存する

非アクティブなGameObjectも対象です。全Scene処理では現在開いているScene構成を処理後に復元します。Prefabは `PrefabUtility.LoadPrefabContents` で個別に処理し、壊れたPrefabなどでエラーが発生しても次のPrefabへ進みます。

現在開いているSceneだけへの適用はSceneをDirty状態にしますが、自動保存しません。Prefabインスタンス上のTMP Textを変更するとPrefab Overrideが作成されるため、保存前に変更内容を確認してください。

## TMP Settings

TextMeshPro 3.0.6のDefault Font Assetは公開APIから安全に書き換えられないため、ツールはリフレクションを使用しません。新規作成するTextMeshProにも日本語フォントを使う場合は、次から手動設定します。

`Edit > Project Settings > TextMesh Pro > Default Font Asset`

EditorWindowの `TMP Settings.assetを選択` ボタンから設定アセットを選択することもできます。Default Font Assetは新しく作成するTMPコンポーネント向けで、既存コンポーネントには一括適用またはRuntime適用が必要です。

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
