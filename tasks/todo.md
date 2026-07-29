# Todo

## Current Task: Entry dialog identity-loss fix
- [x] BuildEntry/toggle: görünür kimlik değerlerini kaybetme
- [x] Regresyon testi + build/test

## Review Update
- `BuildEntry` artık VM kimliğini silmiyor; `ApplyProjectSelection` yalnızca boş alanları dolduruyor.
- Eksik katalogda “Projeden kullan” manuel girdiyi silmez.
- Tests: 191/191. Canlı Data dokunulmadı. Commit/publish bekleniyor.
