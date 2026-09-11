# Jeomseon Unity Reactive

Reactive values, lists, events, and Unity-facing reactive fields.

## OpenUPM으로 설치

프로젝트의 `Packages/manifest.json`에 OpenUPM scoped registry를 한 번 등록합니다.

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.jeomseon"
      ]
    }
  ],
  "dependencies": {
    "com.jeomseon.unity.reactive": "0.4.2"
  }
}
```

## Git URL로 설치

Unity Package Manager의 `Install package from git URL`에 다음 주소를 사용합니다.

```text
https://github.com/jeomseon0516/Unity.Reactive.git#v0.4.2
```

## ReactiveList 컬렉션 계약

`ReactiveList<T>`는 `IList<T>`와 비제네릭 `IList`를 모두 구현합니다. UI Toolkit의
`BaseVerticalCollectionView.itemsSource`처럼 비제네릭 목록을 요구하는 API에도 복사 없이 직접
전달할 수 있으며, 해당 경로의 변경도 기존 ReactiveList 이벤트를 동일하게 발생시킵니다.

## 리팩토링 방침

Unity가 제공하는 동등 기능과 비교해 대체 가능한 코드는 소스의 한글 TODO 주석과 CHANGELOG의 Unreleased 항목에서 추적합니다.
