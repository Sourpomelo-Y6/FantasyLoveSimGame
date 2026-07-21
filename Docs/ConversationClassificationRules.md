# 通常会話の分類・選択ルール

AssetTool側の正本は `Docs/ConversationClassificationRules.md`。この文書はUnity実装と運用上重要な規則を同期して残す。

## category

通常会話の `category` は `ConversationGenre` へ直接変換するため、次の4値だけを使う。

| category | 用途 |
|---|---|
| `Daily` | 挨拶、生活、雑談 |
| `Food` | 料理、食事、飲み物 |
| `Adventure` | 探索、戦闘、旅、装備 |
| `Love` | 好意、関係、将来、親密な話題 |

場所や天候などの細分類を独自categoryで表さない。未知の値はImporterで `Daily` になる。ゲーム開始・日開始・物語進行は `GameEvents`、行動後は `ActionReactions`、予定は `ScheduledEvents`、結末は `Endings` を使う。

## ID

```text
Conv_<Genre>_<Context>_<NN>
```

例は `Conv_Daily_Greeting_01`、`Conv_Food_Tea_02`、`Conv_Love_RainyNight_01`。IDは表示済み管理とセーブデータで使うため、本番公開後は変更しない。

既存の `Daily_001` や `Love_Affection30_001` などはセーブデータとの互換性を優先して維持する。
新規会話から上記の `Conv_<Genre>_<Context>_<NN>` 形式を使う。Unity上のファイル名は
`conversationId` と一致させ、IDには英字で始まる英数字とアンダースコアだけを使用する。

## 現在利用できる条件

- 好感度 `minAffection / maxAffection`。0から9999で両端を含む。
- `costumeId`。空は衣装不問。
- `timeOfDay`、`season`、`weather`。空は不問。
- `once`。trueならconversationIdを表示済みとして保存する。

`locationId`、`actionId`、`requiredItemId`、`requiredFlagIds`、`requiredSkillIds` は現状の通常会話Importerと実行時判定へ接続されていない。通常会話の必須条件には使わず、GameEventsまたはActionReactionsを使う。

## 好感度帯

| 段階 | 推奨範囲 |
|---|---:|
| 初期 | 0–199 |
| 親しみ | 200–399 |
| 信頼 | 400–599 |
| 親密 | 600–799 |
| 深い関係 | 800–9999 |

各ジャンルに少なくとも1件、好感度 `0–9999`で他条件なし、`once=false`の会話を残す。

## priority

| priority | 用途 |
|---:|---|
| `0` | 無条件フォールバック |
| `100` | 通常の条件付き会話 |
| `200` | 季節、天候、時間帯、衣装に強く対応する会話 |
| `300` | 高好感度または一度だけの重要会話 |
| `400`以上 | 明示的に最優先する特殊会話 |

当日の予定によるジャンル補正が最大10程度加わるため、意味の異なる段階は20以上離す。同じ条件・priorityは、ランダム差分を意図する場合だけ重複させる。

## 実行時の選択順

1. プレイヤーが選択したジャンルと一致する候補を集める。
2. 好感度、衣装、時間帯、季節、天候を満たさない候補を除外する。
3. 一度だけ表示する会話のうち表示済みを除外する。
4. priorityへ当日の予定によるジャンル補正を加える。
5. 最高スコアが同じ候補からランダムに1件選ぶ。

候補が0件でも別ジャンルを自動検索せず、「現在の条件に合う会話データがありません」と表示する。各ジャンルのフォールバックは `priority=0`、`once=false`、好感度 `0–9999`、衣装・時間帯・季節・天候を空にする。

## once

- 自己紹介、関係進展、秘密の告白など一度だけで意味がある会話に限定する。
- conversationIdを必須にする。
- 日常会話とフォールバックには使わない。
- 表示後も候補が残るよう、同じジャンル・好感度帯に反復可能な会話を置く。

## AssetTool検証方針

- categoryは4ジャンルを候補とする。
- 空・未知category、空・重複ID、空本文、負priority、好感度範囲逆転を警告する。
- 各ジャンルの無条件フォールバックを確認する。
- 同ジャンル・条件・priorityの重複を確認する。
- 通常会話で未接続条件を設定した場合は、Unityの選択条件として使われないことを案内する。

Unity側では `FantasyLoveSim > Validation > Data > Conversation Data` を実行し、全ヒロインの
空・不正・重複ID、ファイル名との不一致、配置先ヒロインと `heroineId` の不一致、
会話種別と選択肢の不整合、同一条件・priority、ジャンル別フォールバック不足を確認する。
既存IDは検証結果を直す目的だけで自動変更しない。
