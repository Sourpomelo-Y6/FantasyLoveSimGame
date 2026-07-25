# GameEventData ガイド

`GameEventData` は、ヒロイン別の導入、日開始、手動イベント、ゲーム進行に連動する反応を
共通形式で保持する。配置先は原則として
`Assets/Resources/Heroines/<HeroineId>/GameEvents/` とする。

## トリガー

| 種類 | 用途 | `triggerContextId` |
| --- | --- | --- |
| `GameStart` | 新規ゲーム開始時 | 不要 |
| `DayStart` | 日開始時 | 不要 |
| `Manual` | コードまたはデバッグから明示起動 | 不要 |
| `ScheduledEventCompleted` | 予定完了後 | 探索先IDまたは`ScheduleType`名 |
| `ActionCompleted` | 行動完了後 | 行動ID |
| `LocationEntered` | 場所へ入った後 | 場所ID |
| `QuestCompleted` | クエスト完了後 | クエストID |

コンテキストIDは大文字小文字を区別せず比較する。前後の空白は自動除去しないため、
データ入力時に含めない。
コンテキスト型はID必須。現在実行経路へ接続済みなのは `ScheduledEventCompleted` で、
他の型は対応するゲーム進行機能を接続するまで自動発火しない。

## 主な設定

- `eventId`: セーブの既読管理にも使う固定ID。本番投入後は変更しない
- `showOnce`: 同じセーブデータで一度だけ完了させる場合に有効化する
- `pages`: 話者、本文、表情、スチルをページ単位で設定する
- `affectionChangeOnComplete`: 最終ページ完了時だけ適用する好感度変化
- 日数、好感度、既読イベント、衣装、取得スキルの条件: すべて満たした場合だけ候補になる

`showOnce` の記録と完了報酬は、イベントを開始した時点ではなく最終ページを完了した時点で
確定する。途中で閉じた場合に既読や報酬だけが残らないようにするためである。

## TestHeroineでの確認

- `GameStartIntro`: タイトルから新規ゲームを開始
- `TestManualEvent`: `GameManager.debugManualGameEventId` に設定し、Play中にF7
- `Manual_Consideration_01`: 汎用スキル「気配り」を取得してスキルツリーを閉じる
- `Event_Location_Forest_01`: 好感度200以上で森の探索予定を完了する

スキル取得イベントなど一度だけのイベントを再確認する場合は、新規セーブを使うか、
対象セーブの既読状態を意図的に初期化する。通常プレイでは既読状態を手動変更しない。

## Toolとの連携

AssetToolの `game_events_export.json` は `category`、`triggerContextId`、条件、ページ、
完了時好感度変化をUnityへ渡す。Unity側import後は
`FantasyLoveSim > Validation > Run All Validations` でID、トリガー、参照、条件を確認する。
