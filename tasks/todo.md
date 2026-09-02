# Todo

## Current Task: Cloudflare geçişi (Aşama 1 — GitHub korunur)
- [x] `cloudflare/worker.js` + `wrangler.toml` (R2 GET/PUT, PIN + API key)
- [x] `WebViewCloudflareSyncService` + AppSettings + Ayarlar UI
- [x] Kayıt sonrası: Cloudflare upload + isteğe bağlı git push (paralel)
- [x] Web: `cloudflareDataUrl` önce, `dataUrl` GitHub yedek
- [x] Build + test (242/242)

## Sonraki adım (aşama 2)
- [ ] Cloudflare Worker deploy + `UPLOAD_API_KEY` secret
- [ ] `web/config.js` → gerçek Worker URL
- [ ] Uygulama ayarlarında Cloudflare URL/key
- [ ] Birkaç gün doğrulama
- [ ] Doğrulanınca git push / GitHub Pages kaldır

## Review
- Aşama 1 hibrit: GitHub yedek olarak duruyor; Cloudflare başarısız olursa site statik JSON'dan okur.

## Previous: Web olay sırası ve anlık senkronizasyon
- [x] parser.js olay sırası WPF ile hizalı
- [x] Debounced export + 30 sn web polling
