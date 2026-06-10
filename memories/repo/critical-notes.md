# Critical Notes

- Canonical daily-use executable: `RizaCanKilicIsTakibi\bin\Release\publish\RizaCanKilicIsTakibi.exe`.
- Live data is stored beside that executable under `Data\tasks.db`, `Data\last-save.json`, `Backup\`, and `Logs\`; publish and cleanup work must preserve these paths.
- This workspace has a `.git` directory. After meaningful code changes, run tests and create a Git commit checkpoint.
- For release delivery, publish to a temporary folder first, then copy only application output into `bin\Release\publish` after confirming `RizaCanKilicIsTakibi.exe` is not running.
- 2026-06-10: YİBF ana bilgi olay silme bug fix completed. Live publish exe was updated and `Data\tasks.db` / `Data\last-save.json` hashes were verified unchanged after publish.
- 2026-06-10: Tadilat Takibi, YİBF Ana Bilgi, and YİBF İş Takibi row reorder commands completed. Live publish exe was updated after full tests; `Data\tasks.db` and `Data\last-save.json` hashes were verified unchanged.
- 2026-06-10: `TÜM EKSİKLER` tab completed. It is read-only, uses YİBF Ana Bilgi as the primary grouping source, and shows unmatched YİBF İş Takibi/Tadilat/Eksik Proje/Karot records separately instead of guessing identity matches. Live publish `Data`, `Backup`, and `Logs` remain protected during build cleanup and publish updates.
- 2026-06-10: `TÜM EKSİKLER` Karot items now include visible `Kat Bilgisi: ...` text in the deficiency reason. Full tests passed and live publish data hashes stayed unchanged.
- 2026-06-10: `TÜM EKSİKLER` deficiency rows now show a separate `Satır: ...` source context line built only from non-empty source columns. Search includes this context. Full tests passed and live publish data hashes stayed unchanged.
- 2026-06-10: Tadilat Takibi scroll performance work completed. The view now uses one virtualized `DisplayRows` list instead of nested district row lists, preserving district labels and empty-district add rows. Full tests passed and live publish data hashes stayed unchanged.
- 2026-06-10: YİBF Ana Bilgi / YİBF İş Takibi work identity fields were added (`WorkGroupId`, `WorkIdentityId`, `WorkVariantLabel`). SQLite migration creates a pre-migration backup under `Backup\schema-migration-yibf-work-id-*` before altering old YİBF schemas. Full tests passed, live publish exe was updated, and `Data\tasks.db` / `Data\last-save.json` hashes stayed unchanged.
