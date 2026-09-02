# RCK İş Takibi — Cloudflare API (Aşama 1)

GitHub push **korunur**. Bu API paralel çalışır; doğrulandıktan sonra git akışı kaldırılabilir.

## Mimari

```
WPF Kaydet → JSON (yerel + isteğe bağlı git push)
           → PUT Cloudflare Worker → R2

Web sitesi → GET Worker /api/data (PIN) → anında veri
           → başarısızsa export/web-view-latest.json (GitHub yedek)
```

## Kurulum

1. Cloudflare hesabında R2 bucket oluşturun: `rck-is-takibi-data`
2. Bu klasörde giriş yapın: `npx wrangler login`
3. Upload anahtarı: `npx wrangler secret put UPLOAD_API_KEY` (güçlü rastgele değer)
4. Deploy: `npx wrangler deploy`
5. Worker URL örneği: `https://rck-is-takibi-api.<hesap>.workers.dev/api/data`

## Uygulama ayarları

**Ayarlar → Web Görüntüleme**

- Cloudflare API URL: Worker `/api/data` adresi
- Cloudflare API Key: `UPLOAD_API_KEY` ile aynı değer
- Cloudflare yükleme: açık
- Git push: isteğe bağlı (yedek, aşama 1)

## Web sitesi

`web/config.js`:

```js
window.WEB_VIEWER_CONFIG = {
  webPin: "271179",
  adminPin: "0258",
  cloudflareDataUrl: "https://rck-is-takibi-api.<hesap>.workers.dev/api/data",
  dataUrl: "export/web-view-latest.json"
};
```

`cloudflareDataUrl` doluysa önce Cloudflare denenir; hata olursa `dataUrl` (GitHub) kullanılır.

## Güvenlik

| İstek | Header |
|--------|--------|
| Web okuma (GET) | `X-Web-Pin: 271179` |
| Masaüstü yazma (PUT) | `X-API-Key: <UPLOAD_API_KEY>` |

R2 bucket public değildir; erişim yalnızca Worker üzerinden.
