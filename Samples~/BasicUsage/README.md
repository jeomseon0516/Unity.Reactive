# Reactive 기본 예제

`ReactiveSample.unity` Scene에 `ReactiveListSample`, `ReactiveFieldSample` 컴포넌트가 이미 부착돼
있습니다.

1. `ReactiveSample.unity`를 엽니다. 각 GameObject의 Inspector에서 `items`/`health` 필드를
   직접 편집해볼 수 있습니다(`[SerializeField]`로 노출돼 있음).
   - `Reactive Field Sample > Health > Value Processors`를 펼쳐 각 원소의 드롭다운에서
     `ClampIntProcessor`, `MinIntProcessor`, `MaxIntProcessor`, `(None)`을 선택할 수 있습니다.
   - 타입과 값을 변경하고 Scene을 저장·재오픈해 managed-reference 타입과 값이 유지되는지 확인합니다.
2. Play Mode로 진입합니다.
3. Console에서 다음을 확인합니다.
   - `Reactive List Sample` GameObject: `항목 추가: Reactive Item` — `ReactiveList<T>.AddedEvent`가
     발행됩니다.
   - `Reactive Field Sample` GameObject: `체력 변경: 10`이 두 번 찍힙니다 — 첫 번째는
     `ClampIntProcessor(0, 10)`가 `15`를 `10`으로 클램프해 발행한 것이고, 두 번째는 같은 값(`10`)을
     `SetValueAndForceInvoke`로 강제 발행한 것입니다(그 사이 `Value = 10` 대입은 이전 값과
     같아 `ChangedEvent`를 발행하지 않습니다).
