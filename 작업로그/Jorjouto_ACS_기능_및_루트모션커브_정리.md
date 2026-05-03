# Jorjouto ACS 기능 및 루트모션 커브 정리

작성일: 2026-05-03

## 조사 대상

- `Assets/Jorjouto/ACS/Source/ScriptableObject_AnimComposer.cs`
- `Assets/Jorjouto/ACS/Source/Components/AnimCoordinatorComponent.cs`
- `Assets/Jorjouto/ACS/Source/ActionBlock_Base.cs`
- `Assets/Jorjouto/ACS/Editor/Source/AnimComposerEditorUI.cs`
- `Assets/Jorjouto/ACS/Editor/AnimComposerEditor.uxml`
- `Assets/Jorjouto/ACS/Sample/Source/ActionBlocks/*.cs`
- `Assets/Jorjouto/ACS/Sample/Source/AnimationTester.cs`
- `Assets/Jorjouto/ACS/Sample/Source/AnimDispatcher.cs`

## 핵심 결론

Jorjouto Animation Composer System(ACS)은 `ScriptableObject_AnimComposer` 데이터를 런타임에서 재생하고, 그 재생 구간에 맞춰 커스텀 액션 블록을 실행하는 도구다. Unity `AnimatorController`의 상태 전이만으로 처리하기 어려운 공격, 피격, 사운드, VFX, 프리팹 생성 같은 타이밍 기반 작업을 하나의 애니메이션 데이터 안에 묶어 관리하는 구조에 가깝다.

다만 `ScriptableObject_AnimComposer` 안의 `RootMotionCurves`와 `NormalizedRootMotionCurves`는 현재 런타임 루트모션을 수정하는 입력값으로 사용되지 않는다. 이 커브를 수정해도 실제 움직임이 바뀌지 않는 이유는 런타임 루트모션 적용 코드가 이 커브를 전혀 읽지 않고, `Animator.deltaPosition`과 `Animator.deltaRotation`만 사용하기 때문이다.

## 런타임에서 할 수 있는 일

### 1. AnimComposer 데이터 재생

`AnimCoordinatorComponent.PlayAnimComposer(...)`를 호출하면 `ScriptableObject_AnimComposer`의 `AnimationClip`이 Unity Playables API를 통해 재생된다.

지원되는 주요 재생 옵션은 다음과 같다.

- `AnimationLayer`: 어느 ACS 레이어에서 재생할지 결정
- `BlendInTime`, `BlendInCurve`: 재생 시작 시 블렌드 인 시간과 가중치 곡선
- `BlendOutTime`, `BlendOutCurve`, `BlendOutOffset`: 종료 시 블렌드 아웃 처리
- `PlayRate`: 애니메이션 재생 속도
- `Loop`: 반복 재생 여부
- `shouldLoop` 파라미터: 호출 시점에 반복 여부를 덮어쓰기
- `customBlendInTime`, `customBlendInCurve`: 호출 시점에 블렌드 인 설정을 덮어쓰기

즉, 런타임에서는 단순히 애니메이션 클립을 트는 것뿐 아니라 재생 속도, 반복, 블렌딩, 레이어를 데이터 단위로 제어할 수 있다.

### 2. 같은 레이어의 기존 AnimComposer 인터럽트

새 AnimComposer를 재생할 때 같은 레이어에서 이미 재생 중인 AnimComposer는 `InterruptActiveAnimComposersInLayer(...)`로 정리된다. 이때 즉시 끊는 것이 아니라 지정된 블렌드 아웃 시간으로 종료된다.

전체 종료용으로는 `InterruptAllAnimComposers(float blendOutTime)`가 제공된다. 샘플의 `AnimationTester`도 입력을 받아 `PlayAnimComposer(...)` 또는 `InterruptAllAnimComposers(0.2f)`를 호출한다.

### 3. 레이어, AvatarMask, Additive 레이어

`AnimCoordinatorComponent`에는 `AnimationLayers` 설정이 있고, 각 레이어는 다음 값을 가진다.

- `AvatarMask`: 특정 신체 부위만 레이어 애니메이션 영향을 받도록 제한
- `IsAdditive`: Additive 레이어 여부
- `LayerWeight`: 레이어 전체 가중치

기본 AnimatorController는 0번 입력으로 유지되고, ACS 레이어들은 그 위에 추가 믹서로 연결된다. 따라서 이동/대기 같은 기본 상태는 AnimatorController로 돌리고, 공격/리액션/상체 액션 같은 데이터는 ACS로 덮어씌우는 구성이 가능하다.

### 4. ActionBlock 실행

`ScriptableObject_AnimComposer`는 여러 `AnimationTrack`을 가지고, 각 트랙은 여러 `ActionBlockData`를 가진다. 각 액션 블록은 시작 프레임/종료 프레임과 시작 시간/종료 시간을 가진다.

런타임 재생 중 `ScriptableObject_AnimComposer.Tick(...)`이 매 프레임 호출되며, 현재 시간이 액션 블록 구간에 들어오면 다음 순서로 실행된다.

- 구간 진입: `ActionBlock_Base.OnStart(...)`
- 구간 유지: `ActionBlock_Base.OnUpdate(...)`
- 구간 이탈: `ActionBlock_Base.OnExit()`

이 구조 때문에 ACS는 애니메이션 타이밍에 맞춘 게임플레이 이벤트를 넣는 용도로 쓸 수 있다. 기본 제공 샘플 액션 블록은 다음과 같다.

- `ActionBlock_PlaySound`: 지정한 사운드 중 하나를 재생, 루프 사운드도 지원
- `ActionBlock_PlayVfx`: VFX 프리팹을 생성하고 파티클을 재생, 소켓 부착/루프/시뮬레이션 속도 지원
- `ActionBlock_InstantiatePrefab`: 프리팹을 생성하고 소켓에 붙이거나 월드 위치에 배치

직접 `ActionBlock_Base`를 상속하면 히트박스 On/Off, 무적 프레임, 카메라 쉐이크, 입력 버퍼 오픈, 공격 판정 알림 같은 프로젝트 전용 동작도 같은 타임라인 안에 추가할 수 있다.

### 5. 루트모션 적용

런타임 루트모션은 `AnimCoordinatorComponent.HandleRootMotion()`에서 처리된다. AnimComposer 데이터의 다음 토글이 실제 적용 축을 결정한다.

- `ApplyHorizontalRootMotion`: `Animator.deltaPosition`의 X/Z 적용
- `ApplyVerticalRootMotion`: `Animator.deltaPosition`의 Y 적용
- `ApplyRotationRootMotion`: `Animator.deltaRotation` 적용

적용 방식은 캐릭터에 붙은 컴포넌트에 따라 달라진다.

- `CharacterController`가 있으면 `CharacterController.Move(...)`
- non-kinematic `Rigidbody`가 있으면 `Rigidbody.MovePosition(...)` 또는 `MoveRotation(...)`
- 둘 다 없으면 `Transform.position`/`Transform.rotation` 직접 변경

또한 `SetRootMotionBlocked(bool)`로 루트모션 적용을 임시로 막을 수 있고, 마지막으로 적용된 이동 속도는 `LastRootMotionMovement`에 저장된다.

주의할 점은 이 루트모션이 ACS 커브 리스트를 평가해서 만드는 움직임이 아니라는 것이다. 실제 이동량은 Unity Animator가 해당 프레임에 계산한 `deltaPosition`/`deltaRotation`에서 나온다.

### 6. 발 IK 관련 신호

`ScriptableObject_AnimComposer.IsFootIK` 값에 따라 베이스 레이어에서 애니메이션을 재생할 때 `IKFootBlocked` 또는 `IKFootUnblocked` 이벤트 채널이 발생한다. 이름은 다소 헷갈리지만, 의도는 현재 애니메이션이 발 IK를 허용할지/막을지를 외부 시스템에 알리는 것이다.

에디터 미리보기에는 별도로 `PreviewFootIK`가 있으며, 이는 프리뷰 Playable의 `SetApplyFootIK(...)`에만 연결된다.

### 7. 재생 이벤트와 속도 제어

`AnimCoordinatorComponent`는 다음 이벤트/제어 기능도 제공한다.

- `OnAnimStart`: AnimComposer 재생 시작 시 발생
- `OnAnimEnd`: AnimComposer 종료 또는 인터럽트 시 발생
- `SetAnimComposerPlayerRate(float)`: 현재 ACS 레이어 믹서들의 속도 변경
- `SetIsHitStopApplied(bool)`: 히트스톱 상태 플래그 저장
- `PlayAnimatorState(...)`: 내부 `AnimatorControllerPlayable`에 직접 상태 재생 명령 전달

## 에디터에서 할 수 있는 일

ACS 에디터 창은 단순한 인스펙터가 아니라 애니메이션 편집/프리뷰 도구다.

지원되는 주요 작업은 다음과 같다.

- AnimComposer 에셋을 더블클릭하면 전용 에디터 창 열기
- `AnimationClip`과 `PreviewModel` 지정
- 미리보기 창에서 애니메이션 재생, 정지, 타임라인 스크럽
- 현재 시간/프레임 확인
- 트랙 추가, 삭제, 이동, 복사, 붙여넣기
- 액션 블록 추가, 삭제, 이동, 복사, 붙여넣기
- 액션 블록별 상세 프로퍼티 편집
- 액션 블록 디버그 실행
- 미리보기용 장착 아이템 추가
- 장착 아이템 소켓 지정, 위치/회전/스케일/표시 여부 조정
- 프리뷰 카메라 회전, 팬, 줌, 높이 조정
- 프리뷰 배경색 변경
- 프리뷰 모델/아이템을 unlit 머티리얼로 보기
- 프리뷰에서 루트모션 보기
- 프리뷰에서 Foot IK 보기
- 애니메이션 클립에서 루트모션 커브 추출

`PreviewRootMotion`은 이름 그대로 에디터 미리보기용이다. `AnimComposerEditor.uxml`의 툴팁에도 “in-game animation is unaffected”라고 되어 있으며, 실제로 에디터 코드에서도 `previewAnimator.applyRootMotion`에만 연결된다.

## RootMotionCurves의 의미

`ScriptableObject_AnimComposer`에는 다음 두 리스트가 있다.

- `RootMotionCurves`
- `NormalizedRootMotionCurves`

에디터의 `Extract Root Motion Curves` 버튼을 누르면 `AnimComposerEditorUI.CreateRootMotionCurvesButtonBindings()`가 실행된다. 이 코드는 `AnimationClip`의 에디터 커브 바인딩 중 다음 프로퍼티만 찾아서 추출한다.

- `RootT.x`
- `RootT.y`
- `RootT.z`

추출 과정은 다음과 같다.

1. 애니메이션 클립에서 `RootT.x/y/z` 커브를 찾는다.
2. 각 커브의 첫 키 값을 기준 오프셋으로 보고 전체 값을 그만큼 빼서 0 시작 커브로 만든다.
3. 이 결과를 `RootMotionCurves`에 저장한다.
4. 같은 키를 복사해 시간축을 0~1 범위로 정규화한다.
5. 값도 해당 축의 최대 절대값 기준으로 정규화한다.
6. 이 결과를 `NormalizedRootMotionCurves`에 저장한다.

즉, 이 커브들은 “애니메이션 클립에 들어 있는 루트 이동 궤적을 데이터로 뽑아 둔 것”이다. 원본 이동량에 가까운 데이터는 `RootMotionCurves`, 시간과 값이 0~1 범위로 맞춰진 참조용 데이터는 `NormalizedRootMotionCurves`라고 보면 된다.

## 커브를 수정해도 실제 루트모션이 바뀌지 않는 이유

현재 코드 기준으로 `RootMotionCurves`와 `NormalizedRootMotionCurves`를 읽는 런타임 코드는 없다.

검색 결과, 이 두 필드는 다음 위치에서만 사용된다.

- `ScriptableObject_AnimComposer.cs`: 필드 선언
- `AnimComposerEditor.uxml`: CurveField 바인딩
- `AnimComposerEditorUI.cs`: Extract 버튼으로 커브를 생성/저장
- 샘플 `.asset`: 추출된 커브 데이터 저장

반면 실제 런타임 이동은 `AnimCoordinatorComponent.HandleRootMotion()`에서 다음 값을 사용한다.

- `animator.deltaPosition`
- `animator.deltaRotation`
- `ApplyHorizontalRootMotion`
- `ApplyVerticalRootMotion`
- `ApplyRotationRootMotion`
- `SetRootMotionBlocked(...)`

따라서 `ScriptableObject_AnimComposer`의 루트모션 커브 필드를 수정해도 `Animator.deltaPosition` 자체가 바뀌지 않고, ACS도 그 커브를 평가해서 이동량으로 쓰지 않는다. 그래서 인게임 움직임이 변하지 않는다.

## 이 커브의 현재 의의

현재 이 에셋 코드만 기준으로 보면 루트모션 커브의 의미는 다음 정도로 정리할 수 있다.

1. 애니메이션 클립의 루트 이동 궤적을 시각적으로 확인하기 위한 데이터
2. 원본 루트모션의 가속/감속 형태를 분석하기 위한 데이터
3. 추후 커스텀 motion warping, 거리 보정, 추가 이동 액션 블록 등을 만들 때 사용할 수 있는 참조 데이터
4. 에셋 제작자가 의도한 확장 지점이지만, 기본 런타임 재생 코드에는 아직 연결되지 않은 데이터

특히 `AnimComposerEditor.uxml`의 Curves Foldout 툴팁에는 “custom motion-warping actions” 용도로 추출/정규화한다고 적혀 있다. 하지만 현재 프로젝트 안에는 이 정규화 커브를 사용하는 액션 블록이나 런타임 보정 코드가 없다.

## 실제 루트모션을 수정하려면

현재 ACS 구조에서 실제 루트모션 움직임을 바꾸는 방법은 크게 세 가지다.

### 1. 원본 AnimationClip의 루트모션 자체를 수정

ACS가 실제로 사용하는 이동량은 Unity Animator가 계산한 `deltaPosition`/`deltaRotation`이다. 따라서 원본 애니메이션 클립의 루트 트랜스폼, import setting, bake into pose 설정, humanoid root transform 설정 등을 바꾸면 실제 루트모션도 바뀔 수 있다.

### 2. ACS의 루트모션 적용 축만 조절

AnimComposer 데이터에서 다음 토글을 조절하면 적용 축을 켜고 끌 수 있다.

- `ApplyHorizontalRootMotion`
- `ApplyVerticalRootMotion`
- `ApplyRotationRootMotion`

예를 들어 전진 이동은 쓰되 회전은 막거나, 수직 이동은 무시하는 식의 제어는 가능하다. 하지만 곡선 모양을 바꿔 이동 거리/속도 프로파일을 직접 수정하는 기능은 아니다.

### 3. 커브를 직접 사용하는 커스텀 코드를 추가

만약 `RootMotionCurves` 또는 `NormalizedRootMotionCurves`를 사용해 실제 이동을 제어하고 싶다면 별도의 런타임 코드가 필요하다.

예를 들어 다음과 같은 방식이 가능하다.

- `Animator.deltaPosition`을 그대로 쓰지 않고, 정규화 커브를 평가해 원하는 거리로 재스케일
- 특정 공격의 목표 거리까지 `NormalizedRootMotionCurves.z`를 기반으로 motion warping
- `ActionBlock_Base`를 상속한 커스텀 액션 블록에서 커브를 평가해 `CharacterController.Move(...)` 호출
- `AnimCoordinatorComponent.HandleRootMotion()`를 확장해 커브 기반 보정량을 곱하거나 대체

다만 이 경우 기존 ACS 코드와 충돌하지 않도록 `Animator.deltaPosition` 기반 루트모션과 커브 기반 이동 중 어느 쪽이 최종 권한을 가지는지 명확히 정해야 한다.

## 실무적인 해석

현재 상태에서 AnimComposer의 루트모션 커브는 “실제 루트모션 편집기”가 아니다. 원본 애니메이션에서 루트 이동 곡선을 추출해 보여주고 저장하는 보조 데이터에 가깝다.

따라서 “커브를 수정해서 루트모션 움직임을 바꿀 수 있을 것”이라고 기대했다면, 현재 코드 기준으로는 그 기대와 다르게 동작하는 것이 정상이다. 실제 움직임을 바꾸려면 원본 애니메이션 클립을 수정하거나, ACS의 루트모션 처리부 또는 커스텀 액션 블록에 해당 커브를 평가하는 코드를 직접 추가해야 한다.
