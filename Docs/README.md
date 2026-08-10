# ドキュメント索引

このフォルダには、現在の操作方法、実装済み仕様、将来計画、外部AssetToolとの連携資料が
混在している。内容が食い違う場合は、コードとUnityのScene／Prefabを最終的な正本とし、
次に「現在仕様」の文書、最後に計画文書の順で参照する。

## 最初に読む文書

- [`../Readme.md`](../Readme.md): プロジェクト概要、起動方法、日本語フォント設定
- [`Handoff.md`](Handoff.md): 現在の実装状況、Unity上の設定、確認事項
- [`LoveSimDevelopmentPlan.md`](LoveSimDevelopmentPlan.md): 全体設計と今後の優先作業
- [`FAQ.md`](FAQ.md): 開発中によく起きる問題

## 現在仕様・運用

- [`JapaneseFontSetup.md`](JapaneseFontSetup.md): TextMeshPro日本語フォントのローカル設定
- [`ScheduleUiExpansionPlan.md`](ScheduleUiExpansionPlan.md): 週間・月間予定とテンプレート
- [`StatusAndAchievementUiReorganizationPlan.md`](StatusAndAchievementUiReorganizationPlan.md): 状態・実績画面
- [`TrainingSelectionExpansionPlan.md`](TrainingSelectionExpansionPlan.md): 訓練一覧UI、調子限定、一回限定、前提訓練による解放
- [`AssetToolDocumentation.md`](AssetToolDocumentation.md): AssetTool側を正本とするデータ契約文書の参照先
- [`SaveDataAndLocalFiles.md`](SaveDataAndLocalFiles.md): セーブデータと端末共通ファイル
- [`DisplayAndResolutionPlan.md`](DisplayAndResolutionPlan.md): 解像度、画面比率、Canvas設定の方針
- [`ReleaseChecklist.md`](ReleaseChecklist.md): 配布前の確認

## 制作・将来計画

- [`TitleAndAudioPresentationPlan.md`](TitleAndAudioPresentationPlan.md): タイトル、免責、BGM、SE、ボイス、ユーザー説明書
- [`UserManualScreenshotChecklist.md`](UserManualScreenshotChecklist.md): HTML説明書用画像の撮影・更新確認
- [`AssetCredits.md`](AssetCredits.md): 画像・音声・フォント等の権利記録
- [`CodexUnityWorkflow.md`](CodexUnityWorkflow.md): Codexを使ったUnity開発手順

## 外部AssetTool関連

AssetToolの操作、素材制作、Export／Import JSON契約は、別リポジトリ
`FantasyLoveSimAssetTool` の文書を正本とする。参照先と文書の分担は
[`AssetToolDocumentation.md`](AssetToolDocumentation.md) を確認する。

## 文書更新ルール

- 実装完了時は「将来」「予定」を「実装済み」へ直し、操作手順も更新する
- テスト件数は増減するため、恒久資料では固定せず「全件成功・失敗0件」と記録する
- セーブ形式、JSON schema、固定IDを変えた場合は移行方法も同じ変更で記録する
- UI名を変えた場合はREADME、Handoff、HTMLユーザー説明書の該当箇所を検索する
- ローカル画像・フォント・音声の実ファイルは、権利とGit方針を確認してから追加する

## 手動追加メモ

- 画面表示と解像度の対応方針は [`DisplayAndResolutionPlan.md`](DisplayAndResolutionPlan.md) に整理済み
