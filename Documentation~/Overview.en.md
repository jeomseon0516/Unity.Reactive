# Jeomseon Unity Reactive

[한국어](Overview.md) · [Installation](../README.en.md)

## Purpose

Adds change notifications to Inspector-serializable values/lists, using ObservableCollections and optional R3 integration.

## Usage flow

Create or serialize a ReactiveField/ReactiveList, subscribe, mutate through Value/list APIs, and unsubscribe at consumer shutdown. WithoutNotify variants distinguish initial notification behavior.

## Contracts and limitations

ReactiveList implements IList<T> and IList with events on both paths. Access Unity UI on the main thread; do not assume package-wide arbitrary-thread safety.

## Package requirements

- Package ID: `com.jeomseon.unity.reactive`
- Version: `0.4.2`
- Unity: `6000.6` / `0f1`
- `com.jeomseon.unity.attributes`: `0.4.2`
- `com.jeomseon.unity.core`: `0.3.2`
- `org.nuget.observablecollections`: `3.3.4`

## Samples and verification

Import the following from Package Manager > Samples. Open the included Scene or Editor tool; follow any Setup step in its sample README.

- [Basic Usage](../Samples~/BasicUsage/README.md)
  - [ReactiveSample](../Samples~/BasicUsage/ReactiveSample.unity)

[Test sources](../Tests)

Add the package ID to manifest `testables`, then run the relevant Unity Test Runner assemblies. These links describe available tests, not a new successful test run.

## Source map

- [ReactiveFieldR3Extensions](../Runtime/R3/ReactiveFieldR3Extensions.cs)
- [ReactiveListR3Extensions](../Runtime/R3/ReactiveListR3Extensions.cs)
- [IReactiveField](../Runtime/ReactiveField/IReactiveField.cs)
- [IReadOnlyReactiveField](../Runtime/ReactiveField/IReadOnlyReactiveField.cs)
- [ReactiveField](../Runtime/ReactiveField/ReactiveField.cs)
- [ReactiveFieldBase](../Runtime/ReactiveField/ReactiveFieldBase.cs)
- [IReadOnlyReactiveList](../Runtime/ReactiveList/IReadOnlyReactiveList.cs)
- [ReactiveList](../Runtime/ReactiveList/ReactiveList.cs)
- [ClampIntProcessor](../Runtime/ValueProcessor/ClampIntProcessor.cs)
- [IValueProcessor](../Runtime/ValueProcessor/IValueProcessor.cs)
- [MaxIntProcessor](../Runtime/ValueProcessor/MaxIntProcessor.cs)
- [MinIntProcessor](../Runtime/ValueProcessor/MinIntProcessor.cs)
