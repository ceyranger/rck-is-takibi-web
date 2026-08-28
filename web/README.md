# RCK İş Takibi — Salt Okunur Web Görüntüleme

Telefondan verilerinizi **salt okunur** görüntülemek için üç parça gerekir:

1. **WPF uygulaması** — `web-view-latest.json` üretir (canlı `tasks.db` dosyasına dokunmaz)
2. **Google Drive Desktop** — JSON dosyasını buluta sync eder
3. **Statik web sitesi + Apps Script** — PIN ile Drive'daki JSON'u okur

## Maliyet

Google Drive (kişisel 15 GB), Apps Script ve GitHub Pages **ücretsiz**dir.

---

## 1. Google Drive klasörü

1. Google Drive'da `RCK-Web-View` klasörü oluşturun.
2. [Google Drive for Desktop](https://www.google.com/drive/download/) kurun.
3. Klasörün PC'deki sync yolunu not alın (ör. `G:\My Drive\RCK-Web-View`).

**Önemli:** Bu klasöre yalnızca `web-view-latest.json` gitsin. `tasks.db` veya exe klasörünü sync etmeyin.

---

## 2. WPF uygulaması ayarı

1. Uygulamayı açın → **Ayarlar**
2. **Web Görüntüleme (Salt Okunur)** bölümünde:
   - **Kaydet sonrası web dosyası üret** işaretleyin
   - **Klasör Seç** ile Drive sync klasörünü seçin
3. Ayarları **Kaydet** edin.
4. **Şimdi Dışa Aktar** ile test edin veya **Tümünü Kaydet** yapın.

Dosya adı: `web-view-latest.json`

---

## 3. Google Apps Script (PIN + CORS proxy)

1. [script.google.com](https://script.google.com) → Yeni proje
2. [`apps-script/Code.gs`](apps-script/Code.gs) içeriğini yapıştırın
3. **Proje ayarları → Script properties**:
   - `DRIVE_FILE_ID` — Drive'daki `web-view-latest.json` dosya kimliği (URL'deki `id=...`)
   - `PIN_HASH` — PIN'in SHA-256 hex değeri (büyük harf)

PIN hash üretmek için `generatePinHashForSetup()` fonksiyonunu geçici PIN ile bir kez çalıştırın; çıkan değeri Script properties'e yazın. Kaynak kodda PIN bırakmayın.

4. **Dağıt → Yeni dağıtım → Web uygulaması**
   - Yürüt: **Ben**
   - Erişim: **Herkes**
5. Dağıtım URL'sini kopyalayın (`.../exec` ile biter)

---

## 4. Web sitesi yapılandırması

1. `config.example.js` dosyasını `config.js` olarak kopyalayın (repo'da boş şablon var).
2. `appsScriptUrl` alanına Apps Script Web App URL'sini yazın.

```js
window.WEB_VIEWER_CONFIG = {
  appsScriptUrl: "https://script.google.com/macros/s/XXXX/exec",
  allowLocalSample: false
};
```

---

## 5. GitHub Pages yayını

1. GitHub repo → **Settings → Pages**
2. **Deploy from a branch** → Branch: `main` (veya `master`) → Folder: **`/web`**
3. Birkaç dakika sonra site adresi: `https://KULLANICI.github.io/REPO/`

Alternatif: [Cloudflare Pages](https://pages.cloudflare.com/) — private repo için de ücretsiz.

---

## Modüller (salt okunur)

- Acil İşler
- Proje Onay Takibi
- Personel Görevleri
- Karot
- Tadilat
- YİBF İş Takibi
- Tüm Eksikler
- Arama

Düzenleme yoktur; veri tek yönlü akar: PC → JSON → Drive → web.

---

## Güvenlik notları

- PIN yalnızca Apps Script tarafında doğrulanır.
- Yanlış PIN denemeleri dakikada 5 ile sınırlıdır.
- Drive dosyası herkese açık olmak zorunda değildir; Script sizin adınıza okur.
- Güçlü PIN kullanın (en az 6 karakter, tahmin edilmesi zor).

---

## Sorun giderme

| Sorun | Çözüm |
|--------|--------|
| Telefonda veri eski | Drive sync gecikmesi (1–2 dk); WPF'te Son dışa aktarma zamanına bakın |
| PIN hatası | Script properties `PIN_HASH` doğru mu? |
| CORS / fetch hatası | Apps Script Web App URL'si `/exec` ile bitmeli |
| Dosya bulunamadı | `DRIVE_FILE_ID` doğru dosyayı gösteriyor mu? WPF export klasörü = Drive sync klasörü mü? |
