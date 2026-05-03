# 전투 타이밍 SO 리팩토링 방향

## 목표

공격 판정, 회피 윈도우, 콤보 입력, 이펙트, 카메라 흔들림, 히트스톱, 무적/캔슬 구간을 하나의 시각적 타임라인 데이터로 관리한다.

런타임에서는 매번 `ScriptableObject`나 보조 객체를 `Instantiate`하지 않고, 읽기 전용 데이터와 재사용 가능한 실행기만 사용한다.

플레이어 공격과 적 공격이 같은 타이밍 시스템을 쓰게 해서, 새 캐릭터나 새 공격을 추가할 때 코드 수정 없이 데이터와 바인딩만 추가하는 구조를 목표로 한다.

## 현재 구조의 한계

현재 플레이어 쪽은 `ComboAttackData`, `AttackTimingProfile`, `AttackExecutionRuntime` 중심이고, 적 쪽은 `EnemyMeleeAttackData`, `AttackDodgeTimingWindow`, `AttackColliderTimingWindow` 중심이다.

같은 개념인 "특정 시간에 무언가를 켜거나 실행한다"가 플레이어와 적에서 서로 다른 데이터 형식과 런타임 코드로 구현되어 있다. 그래서 재사용성이 낮고, 기능 하나를 추가하면 양쪽에 비슷한 코드를 또 만들어야 한다.

또한 적 공격의 회피 윈도우와 공격 콜라이더 윈도우가 별도 SO로 쪼개져 있어서 에셋 수가 늘어난다. 한 공격의 전체 타이밍을 한 화면에서 보기 어렵고, 공격 판정과 회피 가능 구간의 관계를 조정하기도 번거롭다.

문자열 ID 기반 연결도 위험하다. `"KatanaBasicAttackHitbox"` 같은 문자열이 타이핑 오류나 이름 변경에 취약하고, 에디터에서 안전하게 추적하기 어렵다.

## 권장 구조

핵심은 공격 하나당 하나의 `CombatTimelineAsset`을 두고, 그 안에 여러 종류의 트랙을 넣는 방식이다.

예상 데이터 구조는 다음 방향이 좋다.

```csharp
CombatActionData
{
    string id;
    string animationStateName;
    AnimationClip previewClip;
    GameObject previewModelPrefab;
    CombatTimelineAsset timeline;
    AdditionalRootmotion additionalRootmotion;
    float damageMultiplier;
}

CombatTimelineAsset
{
    CombatTimelineTrack[] tracks;
}

CombatTimelineTrack
{
    CombatTrackType type;
    CombatTimelineClip[] clips;
}

CombatTimelineClip
{
    CombatChannelId channel;
    float startNormalizedTime;
    float endNormalizedTime;
    CombatCuePayload payload;
}
```

여기서 `CombatActionData`는 현재의 `AttackData`, `ComboAttackData`, `EnemyMeleeAttackData` 역할을 합친 상위 공격 데이터다. 플레이어 기본 공격, 대시 공격, 카운터 공격, 적 근접 공격이 모두 같은 형태를 쓴다.

`CombatTimelineAsset`은 "언제 무엇을 할지"만 가진다. 실제 콜라이더, 이펙트 프리팹, 사운드, 카메라 흔들림 실행 방법은 들고 있지 않는다.

## 트랙 종류

처음부터 모든 기능을 넣기보다, 아래 트랙들을 공통 규격으로 설계해두는 것이 좋다.

- `HitboxWindow`: 공격 판정 콜라이더를 켜는 구간
- `DodgeWindow`: 플레이어가 이 구간/영역 안에서 회피하면 퍼펙트 회피가 되는 구간
- `ComboInputWindow`: 다음 콤보 입력을 받을 수 있는 구간
- `CancelWindow`: 회피, 이동, 다른 스킬로 캔슬 가능한 구간
- `InvincibleWindow`: 피격 무시 구간
- `EffectCue`: 특정 시점에 이펙트 재생
- `SoundCue`: 특정 시점에 사운드 재생
- `CameraShakeCue`: 특정 시점에 카메라 흔들림
- `HitStopCue`: 타격 성공 시 히트스톱
- `MotionWarpWindow`: 특정 구간 동안 타겟 방향/위치로 보정 이동
- `RootMotionScaleWindow`: 특정 구간의 루트모션 배율 조정

윈도우형 트랙은 `start`와 `end`가 있고, 큐형 트랙은 `time`만 있으면 된다. 내부적으로는 큐도 `start == end`인 클립으로 처리하면 실행 코드가 단순해진다.

## 문자열 ID 대신 채널 에셋 사용

문자열 ID는 장기적으로 줄이는 편이 좋다.

대신 `CombatChannelId : ScriptableObject` 같은 작은 ID 에셋을 둔다.

예시:

```csharp
CombatChannelId KatanaBasicHitbox;
CombatChannelId HellCatBiteHitbox;
CombatChannelId HellCatDodgeArea;
CombatChannelId KatanaSlashEffect;
```

타임라인 클립은 문자열이 아니라 `CombatChannelId`를 참조한다.

각 캐릭터 프리팹에는 `CombatBindingSet` 컴포넌트를 둔다.

```csharp
CombatBindingSet
{
    CombatHitboxBinding[] hitboxes;
    CombatObjectBinding[] dodgeAreas;
    CombatEffectBinding[] effects;
    CombatSoundBinding[] sounds;
}
```

런타임은 타임라인에서 `CombatChannelId`를 받고, 현재 캐릭터의 `CombatBindingSet`에서 실제 오브젝트를 찾는다. 이렇게 하면 같은 타임라인 구조를 플레이어와 적이 공유하면서도, 실제 연결은 각 프리팹에서 다르게 할 수 있다.

## 런타임 실행 방식

런타임에는 `CombatTimelineRunner` 하나를 둔다.

상태 코드는 공격 진입 시 한 번만 초기화한다.

```csharp
timelineRunner.Play(actionData, context);
```

매 프레임에는 애니메이션의 현재 `normalizedTime`만 넘긴다.

```csharp
timelineRunner.Evaluate(normalizedTime);
```

상태 종료 시에는 활성화된 윈도우를 모두 닫는다.

```csharp
timelineRunner.Stop();
```

상태 코드가 직접 `AttackColliderTimingWindow` 배열을 돌거나, 회피 윈도우 배열을 돌거나, 해시셋을 만들어 비교하지 않게 하는 것이 핵심이다.

상태 코드는 다음 정도만 알면 된다.

```csharp
public void Enter()
{
    _runner.Play(_actionData, _context);
    _animator.CrossFade(_actionData.AnimationHash, blendTime, 0, 0f);
}

public void Tick()
{
    AnimatorChecker.TryGetActiveAnimatorStateInfo(..., out _stateInfo);
    _runner.Evaluate(_stateInfo.normalizedTime);

    if (_stateInfo.normalizedTime >= _actionData.ExitNormalizedTime)
    {
        Transition(...);
    }
}

public void Exit()
{
    _runner.Stop();
}
```

이 구조면 공격 종류가 늘어나도 상태 코드가 거의 늘어나지 않는다.

## Instantiate를 피하는 방식

SO는 절대 런타임마다 복제하지 않는다. `CombatActionData`, `CombatTimelineAsset`, `CombatChannelId`는 읽기 전용 데이터로만 사용한다.

`CombatTimelineRunner`는 캐릭터가 생성될 때 한 번 만들거나 컴포넌트로 붙여 재사용한다. 실행 중 필요한 임시 상태는 내부 배열, 리스트, 해시셋을 재사용한다.

이펙트도 매번 `Instantiate`하지 않는 방향이 좋다. `CombatBindingSet`이나 별도 `CombatEffectPool`에서 미리 풀을 만들고, `EffectCue`가 발생하면 풀에서 꺼내 재생한다. 지금처럼 타격 이펙트를 매번 생성/삭제하면 짧은 공격이 많은 액션 게임에서 GC와 프레임 스파이크가 생기기 쉽다.

## 시각 편집기 방향

커스텀 인스펙터보다는 `EditorWindow` 기반 타임라인 에디터가 좋다.

필수 기능은 다음 정도다.

- 애니메이션 클립을 위에 표시하고 길이를 초 단위/정규화 시간으로 표시
- 트랙별 lane 표시
- 윈도우 클립을 드래그해서 시작/끝 조절
- 큐 마커를 드래그해서 발생 시점 조절
- 현재 프레임 슬라이더
- 미리보기 모델 재생/정지/스크럽
- 선택한 클립의 채널, payload, 색상, 설명 편집
- 겹침, 범위 역전, 누락된 바인딩 경고 표시

현재 이미 `ComboAttackDataEditor`, `AttackDodgeTimingWindowEditor`, `AttackColliderTimingWindowEditor`, `AdditionalRootmotionEditor`가 있으므로, 완전히 새로 만들더라도 여기서 쓸 수 있는 미리보기/타임라인 그리기 코드는 참고할 수 있다.

시각적으로는 다음 lane 구성이 적당하다.

```text
Animation      |------------------------------------------------|
Hitbox         |        [ Slash 1 ]      [ Slash 2 ]             |
Dodge Window   |     [ Perfect Dodge Area ]                      |
Combo Input    |                  [ Next Combo ]                 |
Cancel         |                         [ Dodge Cancel ]        |
Effect         |      * slash_fx                 * impact_fx      |
Camera/Hitstop |                         * shake                 |
Motion         |          [ Forward Lunge ]                      |
```

## 시간 단위

저장은 `normalizedTime` 기준을 추천한다. 공격 애니메이션 속도가 바뀌거나 상태 속도가 바뀌어도 타이밍이 애니메이션 비율에 붙어 있기 때문이다.

다만 에디터에서는 초 단위도 함께 보여줘야 한다.

```text
0.47 normalized = 0.612s / 1.302s
```

디자이너가 조절할 때는 초 단위가 직관적이고, 코드에서는 정규화 시간이 단순하다.

## 데이터 검증

타임라인 에셋은 `OnValidate`와 에디터 검증을 강하게 넣는 것이 좋다.

검증 항목:

- 모든 클립의 시간이 0~1 범위인지
- `end < start`인 윈도우가 없는지
- 필수 채널이 비어 있지 않은지
- 동일 트랙에서 의도치 않게 겹친 윈도우가 있는지
- 현재 선택한 캐릭터 프리팹의 `CombatBindingSet`에 필요한 채널 바인딩이 모두 있는지
- 공격 판정 트랙은 있는데 데미지 payload가 없는지
- 이펙트 큐는 있는데 풀/프리팹 바인딩이 없는지

검증 결과는 콘솔 로그보다 에디터 타임라인 안에서 빨간색/노란색 경고로 보여주는 편이 좋다.

## 코드 복잡도를 낮추는 핵심 규칙

타임라인 런타임은 "시간이 지나며 클립의 진입/유지/이탈을 계산"하는 일만 한다.

실제 효과는 handler가 맡는다.

```csharp
ICombatTimelineHandler
{
    void OnClipEnter(CombatTimelineClip clip);
    void OnClipUpdate(CombatTimelineClip clip, float localNormalizedTime);
    void OnClipExit(CombatTimelineClip clip);
    void OnCue(CombatTimelineClip clip);
}
```

플레이어와 적은 같은 runner를 쓰되, context만 다르게 준다.

```csharp
CombatTimelineContext
{
    Transform owner;
    Animator animator;
    CombatBindingSet bindings;
    IDamageDealer damageDealer;
    IDodgeTimingSource dodgeTimingSource;
    ICombatEffectPlayer effectPlayer;
    ICameraImpulse cameraImpulse;
}
```

이렇게 하면 `HellCatBasicAttackState` 같은 상태 클래스는 콜라이더 ID 수집, 활성 오브젝트 동기화, 회피 윈도우 동기화를 몰라도 된다.

## 마이그레이션 순서

1. 공통 `CombatChannelId`와 `CombatBindingSet`부터 만든다.
2. 현재 문자열 ID를 바로 제거하지 말고, 기존 문자열 ID와 새 채널 ID를 같이 지원하는 어댑터를 둔다.
3. `CombatTimelineAsset`과 `CombatTimelineRunner`를 만든다.
4. 플레이어 공격 하나 또는 HellCat 기본 공격 하나만 새 구조로 옮긴다.
5. 기존 `ComboAttackDataEditor`나 적 윈도우 에디터의 타임라인 UI를 통합 에디터로 흡수한다.
6. 이펙트 `Instantiate/Destroy`를 풀 기반으로 바꾼다.
7. 모든 플레이어/적 공격을 `CombatActionData + CombatTimelineAsset`으로 옮긴다.
8. 기존 `AttackDodgeTimingWindow`, `AttackColliderTimingWindow`, `ComboAttackData` 의존 코드를 제거한다.

한 번에 전부 갈아엎기보다, 새 runner를 먼저 만들고 공격 하나만 새 구조로 돌려보는 편이 안전하다. 구조가 맞으면 나머지는 데이터 이관 작업이 된다.

## 최종 형태

최종적으로는 공격 상태 코드가 다음 책임만 가진다.

- 어떤 공격 데이터를 재생할지 선택
- 애니메이션 재생
- 현재 normalizedTime 전달
- 상태 종료 조건 판단

타이밍 시스템은 다음 책임을 가진다.

- 윈도우 열림/닫힘 계산
- 큐 1회 실행
- 바인딩된 콜라이더/이펙트/사운드/카메라 이벤트 실행
- 종료 시 열린 윈도우 정리

에디터는 다음 책임을 가진다.

- 한 공격의 모든 타이밍을 한 화면에서 보여주기
- 드래그로 조절하기
- 미리보기 모델과 애니메이션 기준으로 검증하기
- 누락된 바인딩과 위험한 구간을 즉시 표시하기

이 방향으로 가면 현재보다 SO 수가 줄고, 플레이어/적 코드가 통합되며, 새 공격을 만들 때 코드보다 데이터 편집이 중심이 된다.
