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
5. **P1-04 — IValueProcessor 타입 안전성 재설계 (미착수, 2026-08-13 발견)**
   - **문제**: `IValueProcessor.Process<T>(T value)`가 인터페이스가 아니라 **메서드**에 제네릭을 뒀습니다
     (`Runtime/ValueProcessor/IValueProcessor.cs`). `ReactiveField<T>.ValueProcessors`
     (`List<IValueProcessor>`, non-generic)에는 T와 무관하게 아무 processor나 담을 수 있고,
     `[SerializeReferenceSelector]`가 뽑는 Inspector 후보 목록도 필드의 T를 전혀 모릅니다.
   - **실제 동작**: `ClampIntProcessor`/`MinIntProcessor`/`MaxIntProcessor`
     (`Runtime/ValueProcessor/{Clamp,Min,Max}IntProcessor.cs`)는 `value is int` 런타임 패턴 매칭만
     하고 안 맞으면 원본을 그대로 반환합니다. `ReactiveField<Vector3>`나 `ReactiveField<string>` 같은
     필드에 "Clamp Int"를 Inspector에서 붙여도 크래시나 경고 없이 **영원히 조용한 no-op**으로
     남습니다. 이 동작 자체는 `Tests/Editor/ReactiveFieldTests.cs`의
     `ValueProcessors_DoNotAffectMismatchedType`가 "정상"으로 이미 못박아 놨습니다 — 크래시 방지
     차원의 의도적 봉합이었지, 애초에 타입 안 맞는 processor를 못 붙이게 막는 설계는 아니었습니다.
   - **제안 방향**: `IValueProcessor`를 `IValueProcessor<T>`(인터페이스 자체를 제네릭으로)로
     바꾸고, `ReactiveField<T>.ValueProcessors`를 `List<IValueProcessor<T>>`로 변경합니다.
     `SerializeReferenceSelector`가 후보를 찾을 때 자연스럽게 `IValueProcessor<int>` 구현체만
     걸러져, 애초에 안 맞는 processor는 Inspector 드롭다운에 뜨지도 않게 됩니다(런타임 봉합이
     아니라 근본 차단).
   - **미확인 리스크**: `Jeomseon.Unity.Attributes`의 `SerializeReferenceSelector`가 TypeCache
     기반으로 후보를 찾는데, **닫힌 제네릭 인터페이스(`IValueProcessor<int>`) 호환 필터링을 실제로
     지원하는지 확인 안 됨** — 착수 전 Attributes 소스부터 조사해야 실현 가능성이 확정됩니다.
   - **범위 참고**: `Jeomseon.Unity.Reactive`는 `PACKAGE_ORDER.md`상 이미 "안정화 완료"로 닫힌
     패키지입니다. 이 항목은 그 뒤에 새로 발견한 구조적 결함이라 재작업 근거가 있다고 보고
     기록하지만, 착수 여부는 별도 확정이 필요합니다. `IValueProcessor`/`ClampIntProcessor`/
     `MinIntProcessor`/`MaxIntProcessor`/`ReactiveField<T>`/관련 테스트 전부 Breaking 변경입니다.
6. **P1-05 — 인터페이스/추상 클래스 T에 대한 Value 직렬화 (미착수, 2026-08-13 발견)**
   - **문제**: `ReactiveField<T>.value`(`[SerializeField] protected T value;`,
     `Runtime/ReactiveField/ReactiveField.cs`)는 Unity 클래식 직렬화를 씁니다. T가 인터페이스나
     추상 클래스면 Unity가 그 필드를 조용히 직렬화하지 않습니다(에러 없이 값이 유지되지 않음).
     `ReactiveField<IDamageable>`처럼 다형적 참조 타입을 담으려는 용도로는 지금 못 씁니다.
   - **검토한 해결안**: `Wrapper<T> where T : class { [field: SerializeReference,
     SerializeReferenceSelector] public T Reference { get; set; } }`를 만들고, `ReactiveField<T>`와
     별개로 `ReactiveReferenceField<T> : ReactiveFieldBase<T> where T : class`를 두어 내부적으로만
     `Wrapper<T>`를 보관하고 `Value`는 그대로 `T`를 노출하는 방식을 검토했습니다.
   - **`ReactiveField<Wrapper<T>>`(T 자리에 Wrapper를 직접 노출)를 채택하지 않은 이유**: `Wrapper`가
     `Equals`/`GetHashCode`를 오버라이드하지 않으면 `EqualityComparer<T>.Default`가 Wrapper 인스턴스
     참조 동일성으로 비교돼, 안의 `Reference`가 안 바뀌어도 매번 새 Wrapper를 대입할 때마다
     `changedEvent`가 스푸리어스하게 발동합니다. 오버라이드하면 이 문제는 막을 수 있지만, 그래도
     `Value`가 `T`가 아니라 `Wrapper<T>`가 되어 소비 코드가 전부 `.Value.Reference`로 한 단계 더
     파야 하는 API 비용은 남습니다 — 그래서 `Wrapper<T>`는 내부 구현 디테일로만 쓰는 별도 타입
     방향을 우선 검토 대상으로 남겼습니다.
   - **보류 사유**: "실제로 많이 쓸 것 같지 않다"는 판단으로 이번 범위에서는 착수하지 않고 기록만
     남깁니다. 필요해지면 `Wrapper<T>`는 `Jeomseon.Unity.Core`에 범용으로, `ReactiveReferenceField<T>`는
     `Jeomseon.Unity.Reactive`에 추가하는 구조를 우선 검토합니다.
   - P1-04(ValueProcessor 타입 안전성)와는 독립적인 문제입니다 — 둘 다 `ReactiveField<T>`의 제네릭
     T 처리와 관련은 있지만 서로 다른 필드(`value` vs `ValueProcessors`)를 다룹니다.
7. **P2-01 — 알림 할당 최적화**
   - 변경마다 생성되는 index/item 배열을 읽기 전용 이벤트 구조로 대체할지 측정합니다.
   - `ObservableList<T>` 자체의 `Span<T>` 기반 API를 얼마나 활용할 수 있는지도 함께 검토합니다.
8. **P3-01 — 선택적 R3 호환 확장**
   - `#if` 조건부 컴파일(`ReactiveField`가 쓰던 `SERIALIZEREFERENCEDROPDOWN_INSTALLED` 패턴과 동일)로
     R3가 있을 때만 컴파일되는 `IReadOnlyReactiveList<T>`/`IReadOnlyReactiveField<T>` →
     `Observable<T>` 변환 확장 메서드를 추가합니다. `ObservableCollections.R3`도 UnityNuGet에
     `org.nuget.observablecollections.r3`로 등록돼 있어(확인 완료) 필요 시 같은 경로로 설치
     가능합니다. R3 자체는 여전히 필수 의존성이 아닙니다.
9. **P3-02 — 직렬화 가능한 ReactiveDictionary (보류)**
   - `ObservableCollections`에 `ObservableDictionary<TKey,TValue>`가 있지만 Unity는
     `Dictionary<K,V>`를 Inspector에서 직렬화하지 못해 커스텀 key/value 배열 직렬화 스킴이
     필요합니다. Unity 6000.6에서 Dictionary 직렬화가 공식 지원될 예정이라(2026-08-11 확인,
     6000.6은 아직 베타), 6000.6 LTS가 나오면 이 패키지가 아직 1.x 이전일 경우 최소 지원 버전을
     6000.6으로 올리고 공식 기능을 활용하는 방향을 우선 검토합니다.
