# RCK İş Takibi — Salt Okunur Web Görüntüleme

Personel **paylaşılabilir bir site linki** + **PIN** ile verileri telefondan salt okunur görür. Localhost veya bilgisayarınızın açık olması gerekmez.

## Nasıl çalışır?

```
PC uygulaması → web-view-latest.json → Google Drive (sync)
                                              ↓
Personel → Site linki + PIN → Drive'daki JSON okunur
```

---

## 1. Uygulama (sizin bilgisayar)

1. **Ayarlar → Web Görüntüleme**
2. **Kaydet sonrası web dosyası üret** işaretleyin
3. **Klasör Seç** — örn. `C:\Users\...\İŞ TAKİBİ WEB YEDEK`
4. Ayarları **Kaydet**, **Şimdi Dışa Aktar** ile test edin

Dosya adı: `web-view-latest.json`

---

## 2. Google Drive sync

1. [Google Drive for Desktop](https://www.google.com/drive/download/) kurun
2. Export klasörünü Drive ile sync edin (veya klasörü Drive içinde tutun)
3. `web-view-latest.json` dosyasına sağ tık → **Paylaş** → **Bağlantıya sahip olan herkes** (görüntüleyici)
4. Dosya linkini kopyalayın (ör. `https://drive.google.com/file/d/XXXX/view`)

**Önemli:** `tasks.db` veya exe klasörünü sync etmeyin — yalnızca JSON klasörü.

---

## 3. Site (GitHub Pages — bir kez)

1. Bu repoyu GitHub'a yükleyin
2. **Settings → Pages → Branch: main → Folder: `/web`**
3. Birkaç dakika sonra site adresi: `https://KULLANICI.github.io/REPO/`

`web/config.js` içinde `appsScriptUrl` zaten tanımlı olmalı (Apps Script aşağıda).

---

## 4. Apps Script (bir kez — PIN doğrulama)

Script, site ile Drive arasında köprü görevi görür (CORS nedeniyle gerekli).

1. [script.google.com](https://script.google.com) → Yeni proje
2. [`apps-script/Code.gs`](apps-script/Code.gs) içeriğini yapıştırın
3. `kurulumYap` fonksiyonunu bir kez **Çalıştır** (izin ver)
4. **Dağıt → Yeni dağıtım → Web uygulaması**
   - Yürüt: **Ben**
   - Erişim: **Herkes**
5. `/exec` ile biten URL'yi `web/config.js` → `appsScriptUrl` alanına yazın

Site artık Drive dosya kimliğini (`fileId`) istekle gönderir; Script properties'teki `DRIVE_FILE_ID` yalnızca yedektir.

---

## 5. İlk site kurulumu (siz veya personel)

1. Site linkini açın
2. **Veri kaynağı** ekranında Drive'daki `web-view-latest.json` **paylaşım linkini** yapıştırın → **Kaydet**
3. PIN girin: **271179** (veya sizin belirlediğiniz)

Dosya taşınırsa veya yeniden oluşturulursa: sitede **Veri kaynağını değiştir** veya ana ekranda **Kaynak** butonu ile yeni linki yapıştırın.

---

## Personel için özet

| Bilgi | Değer |
|--------|--------|
| Site | GitHub Pages linki (ör. `https://....github.io/.../`) |
| PIN | 271179 |

Drive linkini personelle paylaşmanız gerekmez; yalnızca site linki + PIN yeterli (kaynak bir kez sizin cihazınızda veya sitede ayarlanır).

---

## Modüller (salt okunur)

Acil İşler, Proje Onay, Personel, Karot, Tadilat, YİBF İş, Tüm Eksikler, Arama

---

## Sorun giderme

| Sorun | Çözüm |
|--------|--------|
| Veri eski | Drive sync (1–2 dk); uygulamada Son dışa aktarma zamanına bakın |
| Geçersiz PIN | PIN: 271179 |
| Dosya okunamadı | Drive paylaşımı “bağlantıya sahip herkes”; sitede doğru dosya linki seçili mi? |
| Bağlantı hatası | `appsScriptUrl` doğru mu? Script “Herkes” erişimli mi? |
