# AssetTool 関連文書

素材制作、AssetTool の操作、Export／Import JSON 契約の正本は、別リポジトリ
`FantasyLoveSimAssetTool` の `ReadMe.md` と `Docs/` に集約する。

Unity 側には同じ文書のコピーを置かない。両方のリポジトリを同じ親フォルダへ配置する必要はなく、
次のリポジトリ内パスを基準に参照する。

- `FantasyLoveSimAssetTool/Docs/CharacterAssetGenerationToolSpec.md`
- `FantasyLoveSimAssetTool/Docs/ConversationClassificationRules.md`
- `FantasyLoveSimAssetTool/Docs/GameEventDataGuide.md`
- `FantasyLoveSimAssetTool/Docs/ToolUsabilityReorganizationPlan.md`
- `FantasyLoveSimAssetTool/Docs/Extra/`: UnityとのExport／Import、同期、画像種別ごとの契約

ゲーム内での利用方法、Unity Runtime の挙動、Scene／Prefab の設定は、引き続きこのリポジトリの
`Docs/Handoff.md` と各機能文書を正本とする。

文書を更新するときは、データ契約をAssetTool側、Unity固有の実装説明をUnity側へ記録し、
同じ全文を両方へコピーしない。
