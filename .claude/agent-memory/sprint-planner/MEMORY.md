# Sprint Planner 메모리

이 파일은 sprint-planner 에이전트의 영구 메모리입니다.
프로젝트 진행 상황, 기술 스택, 패턴 등을 기록합니다.

## 메모리 인덱스

- [project_stack.md](project_stack.md) — 프로젝트 기술 스택 및 구조
- [sprint_status.md](sprint_status.md) — 스프린트 진행 현황 (Sprint 1~6)
- [feedback_skill.md](feedback_skill.md) — 스킬 관련 피드백 및 대체 방법론
- [feedback_mockup_first.md](feedback_mockup_first.md) — 목업 우선 원칙 (2026-03-15 확립)

## 핵심 패턴: 목업 우선 원칙

새 기능 개발 시 반드시 `목업 → 구현` 순서를 따른다.

- 목업 스프린트: 프론트엔드만, mock 데이터, DB/백엔드 변경 없음
- 구현 스프린트: 목업 검토 + 요구사항 확정 후에만 진행
- 버그 수정: 범위가 작고 명확하면 목업 생략 가능 (파일 3개 이하, 코드 50줄 이하)
