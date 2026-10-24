# Jeomseon Unity Reactive

[한국어](README.md) | English

Adds change notifications to Inspector-serializable values/lists, using ObservableCollections and optional R3 integration.

## Quick start

Create or serialize a ReactiveField/ReactiveList, subscribe, mutate through Value/list APIs, and unsubscribe at consumer shutdown. WithoutNotify variants distinguish initial notification behavior.

Complete sample: [ReactiveListSample.cs](Samples~/BasicUsage/ReactiveListSample.cs).

```csharp
using Jeomseon.Unity.Reactive.ReactiveList;
using UnityEngine;

namespace Jeomseon.Samples.Reactive
{
    public sealed class ReactiveListSample : MonoBehaviour
    {
        [SerializeField] private ReactiveList<string> items = new();

        private void Awake()
        {
            items.AddListenerToAddedEventWithoutNotify(
                (indices, addedItems) => Debug.Log($"항목 추가: {addedItems[0]}"));
            items.Add("Reactive Item");
        }
    }
}
```

## Contracts and limitations

ReactiveList implements IList<T> and IList with events on both paths. Access Unity UI on the main thread; do not assume package-wide arbitrary-thread safety.

## Requirements

- Unity `6000.6.0f1`
- Package: `com.jeomseon.unity.reactive` `0.4.2`
- `com.jeomseon.unity.attributes`: `0.4.2`
- `com.jeomseon.unity.core`: `0.3.2`
- `org.nuget.observablecollections`: `3.3.4`

## Installation

Merge this into your project's `Packages/manifest.json`; preserve existing dependencies and registries.

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.jeomseon"
      ]
    },
    {
      "name": "Unity NuGet",
      "url": "https://unitynuget-registry.openupm.com",
      "scopes": [
        "org.nuget"
      ]
    }
  ],
  "dependencies": {
    "com.jeomseon.unity.reactive": "0.4.2"
  }
}
```

Or use **Install package from git URL** in Package Manager:

```text
https://github.com/jeomseon0516/Unity.Reactive.git#v0.4.2
```

Keep the Unity NuGet registry when installing by Git URL too; Reactive uses ObservableCollections.

## API usage and details

### ReactiveList collection contract

`ReactiveList<T>` implements both `IList<T>` and non-generic `IList`. It can be passed directly, without
copying, to APIs such as UI Toolkit's `BaseVerticalCollectionView.itemsSource`. Mutations through that path
raise the same ReactiveList events.

## Samples

Import the desired entry from **Package Manager > Samples**. Use the included Scene/Editor tool and complete its Setup step first, if required.

- [Basic Usage](Samples~/BasicUsage/README.md)
  - [ReactiveSample](Samples~/BasicUsage/ReactiveSample.unity)

## Tests

[Test sources](Tests)

Add the package ID to manifest `testables`, then execute its relevant EditMode/PlayMode assemblies in Unity Test Runner.

## Further reading

[Complete source map and package metadata](Documentation~/Overview.en.md)
- [Changelog](CHANGELOG.md)

## License

[License](LICENSE.md)
