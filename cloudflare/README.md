# RCK İş Takibi — Cloudflare API (Aşama 1)

GitHub push **korunur**. Bu API paralel çalışır.

## Depolama

**Cloudflare R2** bucket: `rck-is-takibi-data`  
Object key: `web-view-latest.json`

## Canlı adres

- Worker: `https://rck-is-takibi-api.rck-istakibi.workers.dev`
- Veri API: `https://rck-is-takibi-api.rck-istakibi.workers.dev/api/data`

## Kurulum (tamamlandı)

1. `wrangler login` ✓
2. KV namespace `RCK_DATA` ✓
3. `UPLOAD_API_KEY` secret ✓
4. `wrangler deploy` ✓
5. workers.dev subdomain: `rck-istakibi` ✓

Upload API key: `cloudflare/.upload-api-key.local` (gitignore'da, repoya gitmez)

## Uygulama ayarları (publish Data/settings.json güncellendi)

- Cloudflare R2'ye yükle: **açık**
- API URL: yukarıdaki `/api/data` adresi
- API Key: `.upload-api-key.local` içindeki değer

## Web sitesi

`web/config.js` → `cloudflareDataUrl` ayarlandı. GitHub `dataUrl` yedek olarak duruyor.

## Yeniden deploy

```powershell
cd cloudflare
.\deploy.ps1 -SkipLogin
```

