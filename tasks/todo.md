# Todo

## Current Task: Cloudflare geçişi (Aşama 1 — tamamlandı)
- [x] Worker deploy: `https://rck-is-takibi-api.rck-istakibi.workers.dev/api/data`
- [x] KV depolama (R2 hesapta etkin değil)
- [x] Publish exe güncellendi; `Data/settings.json` Cloudflare yapılandırıldı
- [x] `web/config.js` cloudflareDataUrl ayarlandı
- [x] GitHub push yedek olarak korunuyor

## Sizin kontrol listesi
- [ ] Uygulamayı aç → Ayarlar → Kaydet → Cloudflare durumunu kontrol et
- [ ] Web sitesinde PIN ile giriş → veri geliyor mu?
- [ ] Birkaç gün sorunsuz → aşama 2: git/GitHub kaldır

## API key
Dosya: `cloudflare/.upload-api-key.local` (repoda yok)

## Aşama 2 (ileride)
- [ ] Git push / GitHub Pages kaldır
- [ ] İsteğe bağlı: R2 etkinleştir ve KV'den taşı
