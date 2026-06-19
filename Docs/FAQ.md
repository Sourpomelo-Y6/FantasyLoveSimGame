# FAQ

このドキュメントは、開発中によく出る確認事項と対処をまとめる。

## TestHeroine の行動メニューが DefaultHeroine より少ない

### 原因

メイン画面の行動メニューは、現在の `HeroineProfileData.actionResourcePath` から `ActionData` を読み込んで作られる。

`DefaultHeroineProfile` は次を参照している。

```text
Heroines/DefaultHeroine/Actions
```

`TestHeroineProfile` は次を参照している。

```text
Heroines/TestHeroine/Actions
```

そのため、`Assets/Resources/Heroines/TestHeroine/Actions/` に `ActionData` が少ない場合、TestHeroine の行動メニューも少なくなる。
現状、TestHeroine は差し替え確認用の最小データから始めているため、DefaultHeroine と同じメニュー数にならないことがある。

### 確認する場所

```text
Assets/Resources/Heroines/DefaultHeroine/Actions/
Assets/Resources/Heroines/TestHeroine/Actions/
Assets/Resources/Heroines/TestHeroineProfile.asset
```

`TestHeroineProfile.asset` の `actionResourcePath` が `Heroines/TestHeroine/Actions` になっている場合、TestHeroine 用メニューは `TestHeroine/Actions` 配下だけから読み込まれる。

### DefaultHeroine と同等のメニューにする方法

`Assets/Resources/Heroines/DefaultHeroine/Actions/` にある `ActionData` を、`Assets/Resources/Heroines/TestHeroine/Actions/` に用意する。

DefaultHeroine の基本メニューは次の通り。

- `TalkAction`
- `StatusDetailAction`
- `StillGalleryAction`
- `MessageLogAction`
- `RestAction`
- `WalkAction`
- `TeaAction`
- `GiftAction`
- `DressUp`
- `OutfitReactionAction`
- `ScheduleAction`

TestHeroine 側に既に `TalkAction.asset` があり、TestHeroine 用の文言にしている場合は、それを上書きしない。
不足している `ActionData` だけをコピーする。

### 注意点

DefaultHeroine からコピーした `ActionData` には、DefaultHeroine 用の `resultMessage`、`reactions`、`stillSprite` が残ることがある。
メニューを揃えるだけならそのままでも動くが、TestHeroine 用に運用する場合は次を差し替える。

- `displayName`
- `resultMessage`
- `reactions`
- `stillId`
- `stillSprite`
- `useHeroineNameAsSpeaker`

特に `stillSprite` は DefaultHeroine の画像を参照している可能性がある。
TestHeroine の画像を使う場合は、`Assets/Images/Heroines/TestHeroine/Actions/` などに画像を置き、対象の `ActionData` または `ActionReactionData` へ割り当てる。

### Unity Editor での手順

1. Project ウィンドウで `Assets/Resources/Heroines/DefaultHeroine/Actions/` を開く。
2. `TalkAction` 以外の必要な action asset を選ぶ。
3. `Assets/Resources/Heroines/TestHeroine/Actions/` へコピーする。
4. TestHeroine 用に文言や画像参照を調整する。
5. Play し直し、Console の `Loaded Actions` 件数とメイン画面のボタン数を確認する。

### 判断基準

本番ヒロインでは、DefaultHeroine と同じ行動メニューを最初から用意する方が扱いやすい。
ただし、ヒロイン固有の行動だけに絞る場合は、`Actions` 配下に置く `ActionData` を意図的に減らしてよい。

