# Migration: 0.1.4 → 0.2.0

## `UnityReactiveList<T>` → `ReactiveList<T>`

순수 C# `ReactiveList<T>`를 제거하면서 비게 된 짧은 이름으로 통일했습니다.

```csharp
// 0.1.4
private UnityReactiveList<int> _items;

// 0.2.0
private ReactiveList<int> _items;
```

기존에 직렬화된 Scene/Prefab 필드는 타입 이름이 아니라 어셈블리의 클래스 GUID 기반으로 참조되므로
자동으로 유지됩니다. 스크립트 코드의 타입 이름만 갱신하면 됩니다.

## Unity 비의존 순수 C# `ReactiveList<T>`/`IReadOnlyReactiveList<T>` 제거

`Jeomseon.Reactive` namespace의 순수 C# 구현을 제거했습니다. `Cysharp/ObservableCollections`의
`ObservableList<T>`가 WPF/Blazor/Unity를 모두 지원하는 성숙한 대체재입니다. Unity 프로젝트에서는
계속 `Jeomseon.UnityReactive.ReactiveList<T>`를 사용하세요(Unity 비의존 순수 C# 프로젝트에서
쓰고 있었다면 `ObservableList<T>`로 직접 교체하세요).

## `SafeAction`/`SafeUnityEvent` 제거

워크스페이스 어디에서도 참조되지 않던 미사용 타입이라 대체 없이 제거했습니다
(`IUnityEventListenerModifier<...>`, `ISafeUnityEventBase` 포함).

## `ReactiveField<T>._value` 프로퍼티 → 필드

내부 구현 세부사항이라 공개 API 변경은 아니지만, `[SerializeField] protected T value;`로 직렬화
방식이 바뀌면서 **기존에 직렬화된 Scene/Prefab의 `ReactiveField` 값이 초기화됩니다**(의도적).
업그레이드 후 Inspector에서 값을 다시 설정하세요.
