# Reactive 로드맵

우선순위: `P0` 결함·안전성 → `P1` 핵심 구조 → `P2` API·성능 → `P3` 장기 확장

## 작업 순서

1. **P0-01 — 이벤트 정확성 테스트**
   - Add/Remove/Replace/Move/Clear와 재진입, listener 제거, 예외 발생 시 동작을 검증합니다.
2. **P1-01 — 자체 계약과 R3 어댑터 전략 결정**
   - 할당, 직렬화, UnityEvent 연동, 학습 비용을 비교하고 Core 계약을 최소화합니다.
3. **P1-02 — 순수 C#과 Unity 직렬화 계층 분리**
   - `ReactiveList`와 `UnityReactiveList`의 책임·namespace·asmdef 경계를 명확히 합니다.
4. **P2-01 — 알림 할당 최적화**
   - 변경마다 생성되는 index/item 배열을 읽기 전용 이벤트 구조로 대체할지 측정합니다.
5. **P3-01 — 선택적 스트림 어댑터**
   - 외부 Reactive 패키지는 선택적 별도 패키지로 제공해 기본 의존성을 늘리지 않습니다.
