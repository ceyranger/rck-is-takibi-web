# Todo

## Current Task
- [x] İstinat türü seçilince üst proje seçim alanının görünür ve çalışır olduğundan emin ol
- [x] `Guid?` ComboBox SelectedValue bağlama sorununu ProjectPicker ile çöz
- [x] Üst proje listesi boşsa kullanıcıya net mesaj göster
- [x] Hedefli test + build/test doğrula
- [ ] critical-notes güncelle; Git commit checkpoint (kullanıcı onayı bekleniyor)

## Review Update
- Proje Kataloğu ekleme dialogunda Tür=İstinat seçilince **Üst Proje (zorunlu)** alanı DataTrigger ile görünür hale geldi.
- Üst proje seçimi boş/bozuk `ComboBox` + `Guid?` yerine `ProjectPickerControl` ile yapılıyor; arama kutusu tıklanınca Normal projeler listeleniyor.
- Katalogda Normal üst proje yoksa kırmızı uyarı: Ana Bilgi'den doldur / önce Normal ekle.
- `ProjectCatalogEntryDialogViewModelTests`: 4/4 geçti.
- `dotnet test RizaCanKilicIsTakibi.sln -c Release`: 173/173 geçti.
- Canonical publish henüz güncellenmedi (kullanıcıya sorulacak).
