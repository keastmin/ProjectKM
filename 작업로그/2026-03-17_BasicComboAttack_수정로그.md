# 2026-03-17 BasicComboAttack 수정 로그

## 요청 내용
- `Player`의 `BasicComboAttack` 상태가 1콤보 후 `Idle`로 넘어간 직후 다시 1콤보를 시작하면,
  이전 공격 애니메이션이 `Idle`로 완전히 바뀌기 전에 일부가 남아 있어 현재 애니메이션 이름 검사와 충돌함.
- 그 결과 `BasicComboAttack`에 즉시 재진입했을 때 현재 상태가 이미 1타 공격 상태이고 `normalizedTime`도 많이 지난 것으로 판정되어,
  새 1타를 재생하지 못하고 곧바로 다시 `Idle`로 돌아가는 문제를 해결해 달라는 요청.
- 위 요구사항과 해결 내용을 폴더를 만들어 한국어 로그로 남겨 달라는 요청.

## 원인
- `BasicComboAttackState.Enter()`에서 같은 공격 애니메이션 이름으로 다시 `CrossFade`할 때,
  이전 공격 1타의 상태 정보가 아직 `Current`에 남아 있는 구간이 존재했음.
- 이 상태에서 `Tick()`이 `GetCurrentAnimatorStateInfo(0)`만 보고 동일한 애니메이션 이름과 높은 `normalizedTime`을 그대로 사용해서,
  새로 시작한 공격이 아니라 이전 공격의 끝부분으로 잘못 판정했음.

## 적용한 해결 방법
- `BasicComboAttack` 진입 시 `Animator.CrossFade(..., 0, 0f)` 형태로 호출해,
  같은 애니메이션 이름으로 재진입해도 항상 0 지점부터 다시 시작되도록 수정함.
- `Tick()`에서는 애니메이션 전환 중일 경우 `Current`보다 `Next` 상태를 우선 사용하도록 변경함.
  그래서 이전 공격 잔상이 남아 있어도 실제로 진입 중인 새 공격 상태의 정보를 기준으로 판정하게 함.
- 공격 상태에 들어온 직후에는 애니메이션이 실제로 0 근처에서 다시 시작됐는지 확인할 때까지
  종료 판정을 잠시 보류하는 `_isWaitingForAnimationRestart` 보호 로직을 추가함.

## 수정 파일
- `Assets/Scripts/Player/SFM/States/BasicComboAttackState.cs`
- `작업로그/2026-03-17_BasicComboAttack_수정로그.md`
