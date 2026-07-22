# Todo

## Current Task
- [x] İstinat türü seçilince üst proje seçim alanının görünür ve çalışır olduğundan emin ol
- [x] `Guid?` ComboBox SelectedValue bağlama sorununu ProjectPicker ile çöz
- [x] Üst proje listesi boşsa kullanıcıya net mesaj göster
- [x] Hedefli test + build/test doğrula
- [x] Güvenli Release publish (Data/Backup/Logs korunarak)
- [x] Git commit checkpoint

## Review Update
- Proje Kataloğu ekleme dialogunda Tür=İstinat seçilince **Üst Proje (zorunlu)** alanı DataTrigger ile görünür.
- Üst proje seçimi `ProjectPickerControl` ile yapılıyor.
- `dotnet test RizaCanKilicIsTakibi.sln -c Release`: 173/173.
- Canonical publish exe güncellendi; `tasks.db` / `last-save.json` hash+size+timestamp aynı kaldı; `Data`/`Backup`/`Logs` dokunulmadı.
- Commit: `fc31913` Add project catalog with İstinat parent picker and safe linking.
