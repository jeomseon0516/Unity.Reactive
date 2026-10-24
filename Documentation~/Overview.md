# Jeomseon Unity Reactive

[English](Overview.en.md) · [설치](../README.md)

## 목적

Inspector에서 직렬화 가능한 값·목록에 변경 알림을 붙입니다. ObservableCollections와 선택적 R3 연동을 사용합니다.

## 사용 흐름

ReactiveField/ReactiveList 생성 또는 직렬화 필드 연결 → listener 구독 → Value/목록 API로 변경 → 소비자 수명 종료 시 구독 해제. WithoutNotify 계열은 초기 알림 정책을 구분합니다.

## 계약과 제한

ReactiveList는 IList<T>와 IList를 지원하며 비제네릭 경로도 이벤트를 발생시킵니다. Unity UI 접근은 메인 스레드에서 수행하세요. 전체 패키지의 임의 스레드 안전성을 가정하면 안 됩니다.

## 패키지 요구 사항

- Package ID: `com.jeomseon.unity.reactive`
- Version: `0.4.2`
- Unity: `6000.6` / `0f1`
- `com.jeomseon.unity.attributes`: `0.4.2`
- `com.jeomseon.unity.core`: `0.3.2`
- `org.nuget.observablecollections`: `3.3.4`

## 샘플과 검증

Package Manager > Samples에서 다음 예제를 Import하세요. 포함된 Scene 또는 Editor 도구를 열고 샘플 README의 Setup 절차를 먼저 수행합니다.

- [Basic Usage](../Samples~/BasicUsage/README.md)
  - [ReactiveSample](../Samples~/BasicUsage/ReactiveSample.unity)

[테스트 코드](../Tests)

소비 프로젝트 manifest의 `testables`에 패키지 ID를 추가하고 Unity Test Runner의 관련 어셈블리를 실행합니다. 위 링크는 테스트 위치이며 이번 문서 수정으로 새 테스트 실행을 주장하지 않습니다.

## 코드 지도

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
