\## 인코딩 및 한글 보존 규칙



이 저장소에는 한글 주석, 문자열, 식별자 등 비ASCII 문자가 포함될 수 있다.

파일을 수정할 때 기존 텍스트는 명시적으로 수정이 필요한 부분을 제외하고 정확히 그대로 보존해야 한다.



규칙:

\- 기존 한글 및 비ASCII 문자를 절대 임의로 변경하지 말 것.

\- 파일 인코딩, BOM, 줄바꿈 방식(LF/CRLF)을 명시적 요청 없이 변경하지 말 것.

\- 작은 수정에 파일 전체 재작성(full rewrite)을 하지 말 것.

\- 가능한 한 최소 범위의 patch 기반 수정만 수행할 것.

\- 수정 요청과 무관한 주석, 문자열, 포맷은 건드리지 말 것.

\- 저장 전, 기존 한글 텍스트가 의도한 수정 라인 외에는 바뀌지 않았는지 확인할 것.

\- 인코딩이 불명확하거나 UTF-8이 아닌 것으로 보이면, 임의 저장하지 말고 먼저 보고할 것.



\## 중요 금지사항



\- 기존 한국어 주석/문자열이 깨지면 해당 수정은 실패로 간주한다.

\- 한글이 깨진 상태로 파일을 저장하지 말 것.



\## Editing rules (critical)



\- Preserve all existing Korean text exactly as-is.

\- Do not change encoding, BOM, or line endings.

\- Use minimal patch-based edits only.

\- If Korean text becomes corrupted, revert and retry with a safer edit method.

