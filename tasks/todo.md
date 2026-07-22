# Todo

## Current Task
- [x] YibfModuleViewModel: overlay aramayı kaldır, Tadilat tarzı IsTakibiSearchText + RefreshIsTakibiRows filtrelemesi ekle
- [x] YibfIsTakibiSectionView: Ara/overlay kaldır, toolbar search chip + boş durum + sayaç
- [x] Hedefli testler, güvenli publish ve Data hash doğrulaması
- [x] critical-notes güncelle; Git commit checkpoint

## Review Update
- YİBF İş Takibi modal `Ara` overlay kaldırıldı.
- Tadilat gibi sürekli görünen arama kutusu satırları yerinde filtreliyor; `×` temizler; sayaç `Görünen: X / Y`.
- Hücre notları da aramaya dahil; Ctrl+F ile satıra gitmede aktif arama temizleniyor.
- `CommitPendingEdits` filtrelenmiş satırları da kapsıyor (`_isTakibiRowLookup`).
- Tests: 175/175. Publish: Data/Backup/Logs hash korundu.
