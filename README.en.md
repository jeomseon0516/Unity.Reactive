# Jeomseon Unity Reactive

Reactive values, lists, events, and Unity-facing reactive fields.

## Install via OpenUPM

Register the OpenUPM scoped registry once in your project's `Packages/manifest.json`.

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.jeomseon"
      ]
    }
  ],
  "dependencies": {
    "com.jeomseon.unity.reactive": "0.4.2"
  }
}
```

## Install via Git URL

Enter the following URL in Unity Package Manager's `Install package from git URL`.

```text
https://github.com/jeomseon0516/Unity.Reactive.git#v0.4.2
```

## ReactiveList collection contract

`ReactiveList<T>` implements both `IList<T>` and non-generic `IList`. It can be passed directly, without
copying, to APIs such as UI Toolkit's `BaseVerticalCollectionView.itemsSource`. Mutations through that path
raise the same ReactiveList events.
