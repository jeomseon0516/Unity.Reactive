# 변경 기록

## [Unreleased]

- TODO(ROADMAP P1-04): `IValueProcessor.Process<T>(T value)`가 메서드 제네릭이라 `ReactiveField<T>`의
  T와 무관한 processor를 Inspector에서 붙일 수 있고, 타입이 안 맞으면 조용히 no-op됩니다(기존에
  의도적으로 봉합·테스트됨). `IValueProcessor<T>`로 인터페이스 자체를 제네릭화해 근본 차단하는 방향을
  검토합니다 — 착수 전 `SerializeReferenceSelector`의 닫힌 제네릭 필터링 지원 여부 확인 필요.
- TODO(ROADMAP P1-05): `ReactiveField<T>.value`가 `[SerializeField]`(Unity 클래식 직렬화)라 T가
  인터페이스/추상 클래스면 조용히 직렬화되지 않습니다. `Wrapper<T> where T : class` +
  `[SerializeReference, SerializeReferenceSelector]`를 내부적으로만 쓰는 별도
  `ReactiveReferenceField<T>` 도입을 검토했으나, 실사용 빈도가 낮을 것으로 판단해 보류합니다.

## [0.3.0] - 2026-08-13

- `Jeomseon.Unity.Core`/`Jeomseon.Unity.Attributes`의 워크스페이스 네임스페이스 규칙 적용에 맞춰
  `using` 참조를 갱신했습니다(`Jeomseon.Events`→`Jeomseon.Unity.Core.Events`,
  `Jeomseon.Attribute`→`Jeomseon.Unity.Attributes`). 이 패키지 자체 공개 API 변경은 없습니다.

- **(Breaking)** 네임스페이스를 `Jeomseon.UnityReactive` 하나에서 폴더 구조를 따르는
  `Jeomseon.Unity.Reactive.ReactiveField`/`Jeomseon.Unity.Reactive.ReactiveList`/
  `Jeomseon.Unity.Reactive.ValueProcessor`로 분리했습니다. 기존 네임스페이스가 asmdef의
  `rootNamespace`(`Jeomseon`)와 폴더 경로(`Runtime/UnityReactive/<하위 폴더>`)를 따르지 않아 Rider
  기본 네임스페이스 규칙과 어긋나 있었습니다. `Runtime/UnityReactive/` 중간 폴더도 패키지 이름과
  중복돼 제거하고 `Runtime/ReactiveField`·`Runtime/ReactiveList`·`Runtime/ValueProcessor`로
  옮겼습니다(GUID는 보존). Runtime asmdef의 `rootNamespace`도 `Jeomseon` → `Jeomseon.Unity.Reactive`로
  변경했습니다. 소비 코드는 `using Jeomseon.Unity.Reactive.ReactiveField;` 등 새 네임스페이스로
  갱신해야 합니다.
- `ReactiveField<T>.ValueProcessors`에 Attributes 패키지의 `[SerializeReferenceSelector]`를 적용해
  Inspector에서 `IValueProcessor` 구체 타입을 선택할 수 있게 했습니다. 외부
  `SerializeReferenceDropdown` 조건부 의존성 없이 단일한 패키지 계약으로 제공됩니다.
- `ReactiveSample` Scene에 직렬화된 `ClampIntProcessor`를 추가하고 타입 선택·Scene 재오픈 유지
  검증 절차를 Sample README에 기록했습니다.

- **Sample 정책 위반 수정**: `Basic Usage` Sample에 `.unity` Scene 자산이 없어 README가 "GameObject를
  만들어 컴포넌트를 붙이라"고만 안내하던 것을 발견해 수정했습니다(AGENTS.md "샘플" 절 위반).
  컴포넌트가 이미 부착된 `ReactiveSample.unity`를 추가하고, Scene이 참조하는 두 Sample 스크립트의
  GUID를 고정했습니다.
- `ReactiveFieldSample`을 신규 추가했습니다. `ReactiveList<T>`만 다루던 Sample에 `ReactiveField<T>`
  예제가 없었습니다. `ValueProcessors`(`ClampIntProcessor`) 파이프라인, 같은 값 대입 시
  `ChangedEvent` 미발행, `SetValueAndForceInvoke`의 강제 발행을 한 화면에서 확인합니다.

## [0.2.0] - 2026-08-12

- **(Breaking)** `UnityReactiveList<T>`를 `ReactiveList<T>`로 이름을 줄였습니다. 순수 C#
  `ReactiveList<T>`를 이번에 제거해 이름이 비어 있었고, `Unity` 접두사를 붙이면 이 패키지의
  다른 타입(`ReactiveField<T>`)과도 이름 스타일이 안 맞아 반대로 짧은 쪽으로 통일했습니다.
  Sample·테스트(`Tests/Editor/ReactiveListTests.cs`)도 함께 갱신했습니다.
- **(Breaking)** 아무 곳에서도 참조되지 않던 `SafeAction`, `SafeUnityEvent`(+
  `IUnityEventListenerModifier<...>`, `ISafeUnityEventBase`)를 제거했습니다. `ReactiveList<T>`/
  `ReactiveField<T>` 어디에서도 쓰지 않았고 Sample·테스트도 없어 사용성이 없다고 판단했습니다.
- **(Breaking)** `ReactiveField<T>._value`가 프로퍼티(`Value`와 이름이 겹쳐 PascalCase를 쓸 수
  없었음)였던 것을 일반 `[SerializeField] protected T value;` 필드로 내렸습니다. 세터 안에서
  `value`가 세터의 암시적 매개변수를 가리키는 것과 필드 자체를 구분해야 해서 필드 접근은
  `this.value`로 명시했습니다. 기존에 직렬화된 Scene/Prefab의 `ReactiveField` 값은
  초기화됩니다(의도적 — 사용자 확인).
- 명명 규칙 정리: `ReactiveList<T>`의 `[NonSerialized] private` 필드 `runtime`을
  `_runtime`으로, `private`/`private static` 메서드(`removeListenerSafe`, `addListenerSafe`,
  `initializeRuntime`, `onRuntimeCollectionChanged`, `invokeAddOrRemove`)를 PascalCase로
  정리했습니다. `ReactiveField<T>`의 `private static readonly` 필드
  `_defaultEqualityComparer`를 `DefaultEqualityComparer`로 정리했습니다(`static readonly`는
  접근 제한자 무관 PascalCase 규칙). `ValueProcessor.cs`의 `MaxIntProcessor` 생성자 매개변수
  이름이 실제로는 `Max`를 설정하면서 `min`으로 돼 있던 것도 `max`로 고쳤습니다(네이밍 규칙은
  아니지만 오해의 소지가 있는 이름). 공개 API 변경은 없습니다(전부 `private`/생성자 매개변수).
- `Tests/Editor/ReactiveListTests.cs`를 추가했습니다(ROADMAP P0-01). Add/AddRange/Insert/
  Remove/RemoveAt/RemoveRange/RemoveAll/Clear/indexer-set/Move/Sort/Reverse가 올바른 인덱스·
  아이템으로 이벤트를 발행하는지, `RemoveAll`/`Clear`가 개별 항목마다가 아니라 한 번에 묶여서
  이벤트를 발행하는지, 잘못된 인덱스가 조용히 무시되는지, 리스너 안에서 같은 리스트를 재진입
  변경해도 안전한지, 리스너가 dispatch 도중 자신을 구독 해제할 수 있는지를 검증합니다. 이
  과정에서 `Move(int, int)`에 인덱스 범위 검사가 빠져 있던 것을 발견해 다른 변경 메서드와
  동일하게 조용히 무시하도록 고쳤습니다.
- `Tests/Editor/ReactiveFieldTests.cs`를 추가했습니다(ROADMAP P0-01). 값이 같으면 `ChangedEvent`가
  발행되지 않는지, `SetValueAndForceInvoke`는 같은 값이어도 강제 발행하는지, 구독 시 현재 값을
  replay하는지, `ValueProcessors` 파이프라인(여러 프로세서 체이닝, 타입 불일치 시 통과)이 값을
  저장·발행 전에 올바르게 가공하는지, 재진입·리스너 제거를 검증합니다.
- **(중요, 정정)** 위 두 테스트를 처음 작성할 때 "리스너 하나가 예외를 던져도 나머지 리스너와
  상태 변경에 영향이 없는 것은 `UnityEvent`가 리스너별로 예외를 격리하기 때문"이라고 잘못
  판단했습니다. `UnityEngine.CoreModule.dll`의 `UnityEventBase.Invoke`를 디컴파일해 확인한 결과
  실제로는 개별 리스너 호출에 try/catch가 전혀 없어, 하나가 던지면 나머지 리스너 호출이 그대로
  중단되고 예외가 호출자까지 전파됩니다(표준 C# 멀티캐스트 delegate와 동일한 동작). 즉 이 패키지가
  보장한다고 문서화했던 "리스너 격리"는 실제로는 전혀 격리되지 않고 있었습니다.
  `ReactiveFieldBase`/`ReactiveList`가 `AddListener`로 등록하는 런타임 리스너를 격리 wrapper
  (`try`/`catch` + `Debug.LogException`)로 감싸 등록하도록 고쳐 실제로 격리를 구현했습니다.
  `changedEvent`/`addedEvent` 등 `[SerializeField] UnityEvent<...>` 필드 자체와 Inspector
  persistent listener 직렬화 경로는 그대로 둡니다(런타임 리스너 등록 경로에만 적용).
  `Delegate.CreateDelegate(Target, Method)`로 `UnityAction`을 재구성하던 기존 방식은 이미
  combine된 멀티캐스트 델리게이트가 들어오면 마지막 호출 대상만 남기고 나머지를 조용히 누락시키는
  결함도 있었는데, 원본을 그대로 호출하는 wrapper로 대체해 이 문제도 함께 해결했습니다.
- **(버그 수정)** 위 wrapper 등록을 `Dictionary<TDelegate, UnityAction<...>>` 단일 매핑으로
  구현했을 때, 동일 델리게이트(같은 Target+Method)를 두 번 구독하면 두 번째 구독이 첫 번째
  wrapper 매핑을 덮어써 이후 한 번만 구독 해지해도 첫 wrapper가 영원히 제거되지 않는 리스너
  누수가 있었습니다. `Dictionary<TDelegate, Stack<UnityAction<...>>>`로 교체해 add/remove를
  LIFO로 정확히 짝지었습니다. `ReactiveFieldTests`/`ReactiveListTests`에 회귀 테스트를
  추가했습니다.
- 한 파일에 여러 타입이 섞여 있던 `ReactiveField.cs`(`IReadOnlyReactiveField<T>`/
  `IReactiveField<T>`/`ReactiveFieldBase<T>`/`ReactiveField<T>`), `IReadOnlyReactiveList.cs`
  (`AddOrRemoveHandler<T>`/`ElementChangedHandler<T>`/`IReadOnlyReactiveList<T>`),
  `ValueProcessor.cs`(`IValueProcessor`/`MinIntProcessor`/`MaxIntProcessor`/`ClampIntProcessor`)를
  타입 하나당 파일 하나로 분리하고 `Runtime/UnityReactive/{ReactiveField,ReactiveList,
  ValueProcessor}/` 폴더로 재배치했습니다(AGENTS.md 코드 구조 규칙). namespace는 그대로
  `Jeomseon.UnityReactive`라 공개 API 변경은 없습니다.
- **(Breaking)** Unity 비의존 순수 C# `Jeomseon.Reactive.ReactiveList<T>`/`IReadOnlyReactiveList<T>`를
  제거했습니다. `Cysharp/ObservableCollections`의 `ObservableList<T>`가 WPF/Blazor/Unity를 모두
  지원하는 성숙한 대체재라 자체 구현을 유지할 근거가 약했습니다(AGENTS.md 판단 순서 1-3번).
  `IReadOnlyReactiveList<T>`·`AddOrRemoveHandler<T>`·`ElementChangedHandler<T>`는
  `Jeomseon.UnityReactive` namespace로 옮겼습니다(더 이상 두 namespace로 나눌 이유가 없음).
- `ReactiveList<T>` 내부 구현을 `ObservableList<T>`(`org.nuget.observablecollections`) 기반으로
  교체했습니다. Add/Remove/Replace/Move/Clear/Sort/Reverse 로직을 직접 구현하지 않고 위임하며, 그
  결과 이전에 없던 **`Move(int oldIndex, int newIndex)`**를 지원합니다. Inspector에 보이는 필드는
  변경 전과 동일한 `[SerializeField] private List<T> list`이며, `ISerializationCallbackReceiver`로
  Inspector 편집/저장 시점에만 내부 `ObservableList<T>`와 동기화합니다 — Inspector UI·직렬화 데이터
  포맷은 기존과 완전히 동일합니다.
  - **의존성 조사**: `Cysharp/ObservableCollections`는 UPM으로 직접 배포되지 않고(저장소에
    `package.json` 없음, NuGet 전용), 커뮤니티 OpenUPM 미러(`com.cysharp.observablecollections`)는
    2022-09-30(`1.1.3`)에서 멈춰 있어 사용하지 않았습니다. 대신 UnityNuGet 공식 레지스트리
    (`https://unitynuget-registry.openupm.com`, scope `org.nuget`)가 NuGet 릴리스와 동기화되는
    `org.nuget.observablecollections`(현재 3.3.4, 2025-07-04 갱신)를 제공해 이걸 채택했습니다.
    `package.json`에 `org.nuget.observablecollections: 3.3.4`를 의존성으로 추가했고, Runtime
    asmdef에 `ObservableCollections` 참조를 추가했습니다. 실제 패키지 tarball을 내려받아 DLL 기준
    `dotnet build` 오류 0개까지 확인했습니다.
  - **API 축소**: `ObservableList<T>`에 대응 API가 없는 `Capacity`, `BinarySearch`, `ConvertAll`,
    `TrimExcess`는 제거했습니다. 나머지 List<T> 호환 메서드(`Find`, `FindAll`, `FindIndex`,
    `GetRange`, `LastIndexOf`, `TrueForAll` 등)는 유지됩니다.
- `ReactiveField<T>`의 `ValueProcessors`(`List<IValueProcessor>`)가
  `#if SERIALIZEREFERENCEDROPDOWN_INSTALLED`로 완전히 게이트돼 있어 이 서드파티 패키지가 없으면
  `ReactiveField<T>` 자체가 컴파일 대상에서 빠지던 것을 제거했습니다. Unity 내장
  `[SerializeReference]`만 사용하도록 정리해 외부 의존성 없이 항상 컴파일됩니다(Inspector에서
  다형성 타입 선택 UI는 Unity 기본 UI로 대체 — 커스텀 드롭다운 검색은 빠집니다).
- R3(Cysharp)는 필수 의존성으로 두지 않았습니다 — 이 패키지는 R3 유무와 무관하게 독립적으로
  동작해야 한다는 기준에 따라, R3가 프로젝트에 별도로 추가된 경우에만 호환되는 선택적 확장을
  이후 추가할 계획입니다(ROADMAP P1-01 참고).
- 향후 계획: 직렬화 가능한 `ReactiveDictionary`를 검토 중입니다. Unity 6000.6 정식 릴리스가
  `[SerializeField] Dictionary<TKey,TValue>`를 직접 지원하므로 커스텀 `SerializedDictionary`나
  key/value 배열 직렬화 구현은 추가하지 않습니다. 기능 착수 시 이 패키지 버전만 최소 Unity 버전을
  6000.6으로 올리고, 공식 Dictionary 저장소와 `ObservableDictionary<TKey,TValue>` 알림을 결합합니다.
- 이월: 정적 이벤트·전역 인스턴스의 Domain Reload 비활성화 호환성 검토는 아직 진행하지 않았습니다.

## [0.1.4] - 2026-08-11

- 워크스페이스 명명 규칙에 맞춰 `ReactiveField`, `ReactiveList`, `SafeUnityEvent`의
  `[SerializeField] private`/`protected` 필드를 `_camelCase`에서 `camelCase`로 정리하고 기존
  이름을 `[FormerlySerializedAs]`로 보존했습니다. 공개 API 변경은 없으며 기존 Scene·Prefab의
  직렬화된 값은 그대로 유지됩니다.

## [0.1.2] - 2026-07-29

- asmdef의 `rootNamespace`와 Reactive 자료구조 파일 위치를 namespace에 맞게 정리했습니다.

## [0.1.1] - 2026-07-29

- `ReactiveList<T>` 이벤트를 확인하는 `Basic Usage` 샘플을 추가했습니다.

## [0.1.0] - 2026-07-29

- JeomseonScriptPack의 관련 모듈을 독립 UPM 패키지로 분리했습니다.


## [0.1.3] - 2026-08-05

- Unity 6000.5.7f1을 최소 지원 버전으로 상향했습니다.
