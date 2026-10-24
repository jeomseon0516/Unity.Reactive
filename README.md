# Jeomseon Unity Reactive

한국어 | [English](README.en.md)

Inspector에서 직렬화 가능한 값·목록에 변경 알림을 붙입니다. ObservableCollections와 선택적 R3 연동을 사용합니다.

## 빠르게 시작하기

ReactiveField/ReactiveList 생성 또는 직렬화 필드 연결 → listener 구독 → Value/목록 API로 변경 → 소비자 수명 종료 시 구독 해제. WithoutNotify 계열은 초기 알림 정책을 구분합니다.

전체 샘플 코드: [ReactiveListSample.cs](Samples~/BasicUsage/ReactiveListSample.cs).

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

## 계약과 주의사항

ReactiveList는 IList<T>와 IList를 지원하며 비제네릭 경로도 이벤트를 발생시킵니다. Unity UI 접근은 메인 스레드에서 수행하세요. 전체 패키지의 임의 스레드 안전성을 가정하면 안 됩니다.

## 요구 사항

- Unity `6000.6.0f1`
- 패키지: `com.jeomseon.unity.reactive` `0.4.2`
- `com.jeomseon.unity.attributes`: `0.4.2`
- `com.jeomseon.unity.core`: `0.3.2`
- `org.nuget.observablecollections`: `3.3.4`

## 설치

프로젝트 `Packages/manifest.json`의 기존 dependencies와 registry를 유지하면서 다음 항목을 병합합니다.

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

또는 Package Manager의 **Install package from git URL**을 사용합니다:

```text
https://github.com/jeomseon0516/Unity.Reactive.git#v0.4.2
```

Git URL로 설치해도 Reactive의 ObservableCollections 의존성을 위해 Unity NuGet registry를 유지합니다.

## API 사용과 상세 설명

### ReactiveList 컬렉션 계약

`ReactiveList<T>`는 `IList<T>`와 비제네릭 `IList`를 모두 구현합니다. UI Toolkit의
`BaseVerticalCollectionView.itemsSource`처럼 비제네릭 목록을 요구하는 API에도 복사 없이 직접
전달할 수 있으며, 해당 경로의 변경도 기존 ReactiveList 이벤트를 동일하게 발생시킵니다.

## 샘플 실행

**Package Manager > Samples**에서 예제를 Import합니다. 포함된 Scene 또는 Editor 도구를 열고 필요한 Setup 메뉴를 먼저 실행합니다.

- [Basic Usage](Samples~/BasicUsage/README.md)
  - [ReactiveSample](Samples~/BasicUsage/ReactiveSample.unity)

## 테스트

[테스트 코드](Tests)

manifest의 `testables`에 패키지 ID를 추가하고 Unity Test Runner의 관련 EditMode/PlayMode 어셈블리를 실행합니다.

## 추가 문서

[전체 코드 지도와 패키지 구성](Documentation~/Overview.md)
- [Changelog](CHANGELOG.md)

## 라이선스

[License](LICENSE.md)
