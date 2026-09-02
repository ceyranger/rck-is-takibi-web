# Todo

## Current Task: Web olay sırası ve anlık senkronizasyon
- [x] `parser.js`: Proje Takibi olay listesi WPF ile aynı (tarih azalan, en yeni üstte)
- [x] `MainViewModel.WebView.cs`: Kayıt sonrası 2 sn debounced export
- [x] `AppSettings`: `WebViewExportEnabled` varsayılan `true`
- [x] `app.js`: 30 sn otomatik yenileme, arka plan hatalarında oturum korunur
- [x] Build + test (236/236)

## Review
- Web olay sırası `groupYibfEventsByEntry` + `getLatestYibfEvent` ile düzeltildi.
- Site senkronu: Kaydet sonrası debounced export; web oturumunda 30 sn polling (sekme gizliyken durur).

## Previous: Web viewer iyileştirmeleri (GitHub Pages)
- [x] YİBF/Tadilat/Karot/Eksik Proje hücre renkleri + notları (`yibfCellStates`, `tadilatCellStates`, vb.)
- [x] Renkli hücrelerde simsiyah yazı (WPF uyumu)
- [x] Hücre notları ikona tıklayınca açılsın
- [x] Proje Takibi listesi son işten ilk işe sıralansın
- [x] Aydınlık/karanlık tema seçici + karanlık mod okunabilirliği
- [x] Commit + push (`f247ad1`)

## Review
- Web kodu `rck-is-takibi-web` reposuna push edildi; site cache bust `?v=20260828r`.
- Karot hücre notları export'ta boşsa sitede görünmez; canlı veri için yeniden dışa aktarım gerekir.
