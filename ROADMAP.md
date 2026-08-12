# Reactive 로드맵

우선순위: `P0` 결함·안전성 → `P1` 핵심 구조 → `P2` API·성능 → `P3` 장기 확장

## 작업 순서

1. **P0-01 — 이벤트 정확성 테스트 (완료, 사용자 Unity Test Runner 검증 통과)**
   - Add/Remove/Replace/Move/Clear와 재진입, listener 제거, 예외 발생 시 동작을 검증합니다.
   - `Tests/Editor/ReactiveListTests.cs`, `Tests/Editor/ReactiveFieldTests.cs` 추가 완료,
     `dotnet build` 오류 0개 확인, 사용자가 Unity Test Runner에서 전체 PASS를 확인했습니다.
   - 검증 중 발견한 결함: `ReactiveList<T>.Move`에 인덱스 범위 검사가 빠져 있었음(수정 완료).
   - **정정**: 처음에는 "리스너 하나가 던진 예외가 다른 리스너·상태 변경에 영향을 주지 않는 것은
     `UnityEvent`가 리스너별로 예외를 격리하기 때문"이라고 잘못 판단했습니다. 실제로는
     `UnityEventBase.Invoke`(디컴파일로 확인)에 개별 호출 try/catch가 전혀 없어 전혀 격리되지
     않았습니다. `ReactiveFieldBase`/`ReactiveList`가 런타임 리스너를 격리 wrapper로 감싸 등록하는
     방식으로 실제 격리를 구현했습니다(상세는 `CHANGELOG.md` `[0.2.0]` 참고). 이 과정에서 발견한
     추가 결함(동일 델리게이트 중복 구독 시 단일 Dictionary 매핑이 첫 wrapper를 덮어써 생기는 리스너
     누수)도 `Stack` 기반으로 교체해 해결했습니다.
   - `ReactiveList<T>`가 이제 `ObservableList<T>`(ObservableCollections)에 위임하므로,
     검증 범위는 "우리 어댑터가 CollectionChanged를 UnityEvent로 정확히 중계하는지"·
     "Clear/Sort/Reverse의 일시 구독 해제-재구독-수동 발행이 안전한지"에 집중합니다.
2. **P1-01 — 자체 계약과 R3 어댑터 전략 (결정 완료, 2026-08-11)**
   - **결정**: `Jeomseon.Unity.Core`에 반응형 백엔드 교체용 인터페이스 계층을 새로 두지 않습니다.
     P3-01(선택적 스트림 어댑터)이 이미 "백엔드 교체는 패키지 단위"로 정하고 있어, 구현체가
     하나뿐인 인터페이스는 의미 없는 Wrapper가 됩니다. 두 번째 실제 백엔드가 필요해지는 시점에
     그때 근거를 갖고 인터페이스를 뽑아냅니다(`Jeomseon.Unity.EditorToolkit` 하위 패키지화 보류와
     동일한 논리).
   - **결정**: `ReactiveList<T>`/`ReactiveField<T>`는 `Cysharp/ObservableCollections`를
     필수 의존성으로 사용합니다(`org.nuget.observablecollections`, UnityNuGet 레지스트리 경유).
     Inspector 직렬화 필드(`List<T>`)와 실제 알림 로직(`ObservableList<T>`)을 분리해, Inspector
     동작은 그대로 유지하면서 Add/Remove/Replace/Move/Clear 정확성은 검증된 라이브러리에
     위임합니다.
   - **결정**: R3는 필수 의존성으로 두지 않습니다. "패키지 자체는 서드파티 반응형 라이브러리와
     독립적으로 동작해야 하고, 프로젝트에 R3가 추가로 있을 때만 잘 호환되면 된다"는 기준에 따라
     선택적 확장으로만 제공합니다(아래 P3-01 참고).
   - **폐기**: Unity 비의존 순수 C# `ReactiveList<T>`는 제거했습니다. `ObservableList<T>`가
     WPF/Blazor/Unity를 모두 지원해 ".NET 환경에서도 쓰기 위한" 원래 목적을 이미 충족합니다.
3. **P1-02 — 순수 C#과 Unity 직렬화 계층 분리 (해소, 2026-08-11)**
   - 순수 C# `ReactiveList<T>` 제거로 "분리"가 아니라 "통합"으로 해소됐습니다.
     `IReadOnlyReactiveList<T>`/`AddOrRemoveHandler<T>`/`ElementChangedHandler<T>`는
     `Jeomseon.UnityReactive` namespace 하나로 합쳤습니다.
4. **P1-03 — ValueProcessor 타입 선택 UI 복구 (구현 완료, Unity 검증 필요)**
   - 외부 `SerializeReferenceDropdown` 조건부 컴파일은 되살리지 않고, 범용 기능을 소유하는
     `Jeomseon.Unity.Attributes`의 `[SerializeReferenceSelector]`를 사용합니다.
   - `ValueProcessors` 리스트에서 `ClampIntProcessor`, `MinIntProcessor`, `MaxIntProcessor`, `(None)`을
     선택할 수 있으며, 각 processor는 Selector 생성을 위한 매개변수 없는 생성자를 제공합니다.
   - `(None)` 원소는 값 처리 중 건너뛰며, 생성자와 null 원소 계약을 EditMode 테스트로 검증합니다.
   - `ReactiveSample` Scene에 직렬화된 `ClampIntProcessor(0, 10)`을 포함합니다.
5. **P2-01 — 알림 할당 최적화**
   - 변경마다 생성되는 index/item 배열을 읽기 전용 이벤트 구조로 대체할지 측정합니다.
   - `ObservableList<T>` 자체의 `Span<T>` 기반 API를 얼마나 활용할 수 있는지도 함께 검토합니다.
6. **P3-01 — 선택적 R3 호환 확장**
   - `#if` 조건부 컴파일(`ReactiveField`가 쓰던 `SERIALIZEREFERENCEDROPDOWN_INSTALLED` 패턴과 동일)로
     R3가 있을 때만 컴파일되는 `IReadOnlyReactiveList<T>`/`IReadOnlyReactiveField<T>` →
     `Observable<T>` 변환 확장 메서드를 추가합니다. `ObservableCollections.R3`도 UnityNuGet에
     `org.nuget.observablecollections.r3`로 등록돼 있어(확인 완료) 필요 시 같은 경로로 설치
     가능합니다. R3 자체는 여전히 필수 의존성이 아닙니다.
7. **P3-02 — 직렬화 가능한 ReactiveDictionary (보류)**
   - `ObservableCollections`에 `ObservableDictionary<TKey,TValue>`가 있지만 Unity는
     `Dictionary<K,V>`를 Inspector에서 직렬화하지 못해 커스텀 key/value 배열 직렬화 스킴이
     필요합니다. Unity 6000.6에서 Dictionary 직렬화가 공식 지원될 예정이라(2026-08-11 확인,
     6000.6은 아직 베타), 6000.6 LTS가 나오면 이 패키지가 아직 1.x 이전일 경우 최소 지원 버전을
     6000.6으로 올리고 공식 기능을 활용하는 방향을 우선 검토합니다.
