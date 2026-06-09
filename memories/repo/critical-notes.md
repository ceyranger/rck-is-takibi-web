# Critical Notes

- Canonical daily-use executable: `RizaCanKilicIsTakibi\bin\Release\publish\RizaCanKilicIsTakibi.exe`.
- Live data is stored beside that executable under `Data\tasks.db`, `Data\last-save.json`, `Backup\`, and `Logs\`; publish and cleanup work must preserve these paths.
- This workspace currently has no `.git` directory. If Git is still unavailable when code work finishes, report that commit checkpointing could not be performed instead of treating it as a repo failure.
- For release delivery, publish to a temporary folder first, then copy only application output into `bin\Release\publish` after confirming `RizaCanKilicIsTakibi.exe` is not running.
- 2026-06-10: YİBF ana bilgi olay silme bug fix completed. Live publish exe was updated and `Data\tasks.db` / `Data\last-save.json` hashes were verified unchanged after publish.
