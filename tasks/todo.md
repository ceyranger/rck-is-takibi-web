# Todo

## Current Task
- [x] Tadilat Takibi ilçe sütunundaki birleşik grup görünümünün neden bozulduğunu doğrula.
- [x] Satır/hücre komutlarını bozmadan boş ilçe hücrelerini kaldırıp satır bazlı sanallaştırmayı koru.
- [x] Tadilat hedefli/tam testleri çalıştır, güvenli Release publish al ve Git commit oluştur.

## Review Update
- Tadilat Takibi ekranı tekrar `DisplayRows` üstünden satır bazlı sanallaştırılmış listeye döndürüldü; aynı ilçenin alt satırlarında boş ilçe hücresi yerine ilçe etiketi görünür kalıyor.
- Satır/hücre düzenleme, sağ tık menüleri, renk/not, taşıma ve boş ilçe için `Görev Ekle` davranışı mevcut `TadilatCellTemplate` ve komutlarıyla korunuyor.
- `dotnet test RizaCanKilicIsTakibi.Tests\RizaCanKilicIsTakibi.Tests.csproj --filter "Tadilat"` geçti: 14/14.
- `dotnet test RizaCanKilicIsTakibi.sln` geçti: 142/142.
- Release publish geçici klasöre alındı; uygulama açık olduğu için exe swap bekletildi, program kapandıktan sonra canonical `RizaCanKilicIsTakibi\bin\Release\publish\RizaCanKilicIsTakibi.exe` güncellendi.
- Canlı `Data\tasks.db` ve `Data\last-save.json` hash/size/timestamp değerleri exe güncellemesi öncesi ve sonrası aynı kaldı.
- Gereksiz test/debug/RID build çıktıları ve geçici publish klasörü temizlendi; repo içinde tek exe canonical publish exe olarak kaldı.

## Current Task
- [x] YİBF Ana Bilgi / YİBF İş Takibi için ortak `WorkGroupId` ve `WorkIdentityId` modelini ekle.
- [x] SQLite migration, backup/restore DTO ve eski veri kimlik doldurma akışını canlı veriyi koruyacak şekilde uygula.
- [x] YİBF ViewModel yükleme/düzenleme/kaydetme akışında iş kimliği normalize işlemini çalıştır.
- [x] `TÜM EKSİKLER` eşleştirmesini mümkün olduğunda yeni iş kimliği alanlarını kullanacak şekilde güncelle.
- [x] Hedefli/tam testleri çalıştır, güvenli Release publish al ve Git commit oluştur.

## Review Update
- YİBF Ana Bilgi ve YİBF İş Takibi kayıtlarına `WorkGroupId` / `WorkIdentityId` eklendi; exact ana iş satırları aynı kimliği, istinat/blok gibi suffix'li satırlar aynı grup içinde ayrı kimliği alıyor.
- SQLite migration eski şemalara kolon ekliyor, boş kimlikleri mevcut satır `Id` değerleriyle dolduruyor ve eski şema tespitinde pre-migration yedeği oluşturuyor.
- Backup/restore, undo/redo snapshot ve kaydetme akışları yeni kimlik alanlarını koruyor.
- `TÜM EKSİKLER` YİBF İş Takibi kayıtlarını önce `WorkGroupId` ile bağlıyor, fallback olarak eski güvenli metin eşleştirmesini kullanıyor; satır bağlamında `İş Kimliği` etiketi gösteriliyor.
- `dotnet test RizaCanKilicIsTakibi.Tests\RizaCanKilicIsTakibi.Tests.csproj --filter "Yibf|TumEksikler"` geçti: 39/39.
- `dotnet test RizaCanKilicIsTakibi.sln` geçti: 142/142.
- Release publish geçici klasöre alındı, uygulama çalışmadığı doğrulanınca canonical `RizaCanKilicIsTakibi\bin\Release\publish\RizaCanKilicIsTakibi.exe` güncellendi.
- Canlı `Data\tasks.db` ve `Data\last-save.json` hash/size/timestamp değerleri publish öncesi ve sonrası aynı kaldı.
- Gereksiz test/debug/RID build çıktıları ve geçici publish klasörü temizlendi; repo içinde tek exe canonical publish exe olarak kaldı.

## Current Task
- [x] Tadilat Takibi scroll kasmasının kök nedenini incele.
- [x] İç içe ilçe/satır listelerini tek sanallaştırılmış satır listesine çevir.
- [x] İlçe etiketi, boş ilçe için görev ekleme ve mevcut hücre düzenleme davranışını koru.
- [x] Tadilat hedefli/tam testleri çalıştır, güvenli Release publish al ve Git commit oluştur.

## Review Update
- Tadilat ekranındaki iç içe `ListBox` yapısı kaldırıldı; satırlar `DisplayRows` üzerinden tek sanallaştırılmış listede çiziliyor.
- İlçe adı ilk satırda gösteriliyor, aynı ilçenin devam satırlarında ilçe sütunu boş kalıyor; boş ilçeler için `Görev Ekle` satırı korunuyor.
- Satır seçim, hücre düzenleme, sağ tık renk/not işlemleri mevcut satır ve hücre ViewModel'leriyle devam ediyor.
- `dotnet test RizaCanKilicIsTakibi.Tests\RizaCanKilicIsTakibi.Tests.csproj --filter "Tadilat"` geçti: 14/14.
- `dotnet test RizaCanKilicIsTakibi.sln` geçti: 134/134.

## Current Task
- [x] `TÜM EKSİKLER` eksik maddelerine kaynak satır bağlamı ekle.
- [x] Tadilat, YİBF İş Takibi, Karot ve Eksik Proje için dolu sütunlardan `Satır: ...` metni üret.
- [x] UI'da eksik nedeni altında ayrı bağlam satırı göster.
- [x] Hedefli/tam testleri çalıştır, güvenli Release publish al ve Git commit oluştur.

## Review Update
- `TÜM EKSİKLER` maddelerinde eksik nedeni altında ayrı `Satır: ...` bağlam satırı gösteriliyor.
- Tadilat, YİBF İş Takibi, Karot ve Eksik Proje için yalnız dolu kaynak sütunları bağlama ekleniyor; boş alanlar `(boş)` olarak yazılmıyor.
- Arama filtresi artık satır bağlamı içinde de arama yapıyor.
- `dotnet test RizaCanKilicIsTakibi.Tests\RizaCanKilicIsTakibi.Tests.csproj --filter "TumEksikler"` geçti: 8/8.
- `dotnet test RizaCanKilicIsTakibi.sln` geçti: 133/133.
- Release publish geçici klasöre alındı, uygulama çalışmadığı doğrulanınca canonical `RizaCanKilicIsTakibi\bin\Release\publish\RizaCanKilicIsTakibi.exe` güncellendi.
- Canlı `Data\tasks.db` ve `Data\last-save.json` hash/size/timestamp değerleri publish öncesi ve sonrası aynı kaldı.

## Current Task
- [x] `TÜM EKSİKLER` listesindeki Karot maddelerinde `Kat Bilgisi` metnini görünür yap.
- [x] Hedefli/tam testleri çalıştır, güvenli Release publish al ve Git commit oluştur.

## Review Update
- Karot kaynaklı eksik maddelerinde durum metnine `Kat Bilgisi: ...` eklendi; liste satırında doğrudan okunuyor.
- `dotnet test RizaCanKilicIsTakibi.Tests\RizaCanKilicIsTakibi.Tests.csproj --filter "TumEksikler"` geçti: 6/6.
- `dotnet test RizaCanKilicIsTakibi.sln` geçti: 131/131.
- Release publish geçici klasöre alındı, uygulama çalışmadığı doğrulanınca canonical `RizaCanKilicIsTakibi\bin\Release\publish\RizaCanKilicIsTakibi.exe` güncellendi.
- Canlı `Data\tasks.db` ve `Data\last-save.json` hash/size/timestamp değerleri publish öncesi ve sonrası aynı kaldı.
- Gereksiz test/debug/RID build çıktıları ve geçici publish klasörü temizlendi; canlı `Data`, `Backup`, `Logs` korunuyor.

## Current Task
- [x] `TÜM EKSİKLER` ana sekmesini navigasyona ekle.
- [x] YİBF Ana Bilgi, YİBF İş Takibi, Tadilat, Eksik Proje ve Karot kaynaklarından eksik özetini üret.
- [x] Zorunlu takip alanlarındaki boş hücreleri kontrollü şekilde eksik olarak göster.
- [x] Net eşleşmeyen kayıtları ayrı bölümde göster ve yanlış otomatik eşleştirme yapma.
- [x] Filtre/arama ve ilgili kayda gitme davranışını ekle.
- [x] Hedefli/tam testleri, güvenli publish doğrulamasını ve Git commit'i tamamla.

## Review Update
- `TÜM EKSİKLER` ana sekmesi eklendi; YİBF Ana Bilgi ana grup kaynağı olarak kullanılıyor, YİBF İş Takibi/Tadilat/Eksik Proje/Karot eksikleri tek ekranda gruplanıyor.
- Zorunlu takip alanlarındaki boş hücreler `Boş takip alanı` olarak, kırmızı/sarı hücreler ve YİBF olayları ise kritik/uyarı olarak gösteriliyor.
- Net eşleşmeyen kayıtlar otomatik tahminle bağlanmadan `Eşleşmeyen Eksikler` altında tutuluyor.
- Filtreler ve çift tıkla ilgili sekmeye gitme davranışı eklendi; ekran salt okunur kaldı.
- `dotnet test RizaCanKilicIsTakibi.Tests\RizaCanKilicIsTakibi.Tests.csproj --filter "TumEksikler|Yibf|Tadilat"` geçti: 40/40.
- `dotnet test RizaCanKilicIsTakibi.sln` geçti: 131/131.
- Release publish geçici klasöre alındı, uygulama çalışmadığı doğrulanınca canonical `RizaCanKilicIsTakibi\bin\Release\publish\RizaCanKilicIsTakibi.exe` güncellendi.
- Canlı `Data\tasks.db` ve `Data\last-save.json` hash/size/timestamp değerleri publish öncesi ve sonrası aynı kaldı.
- Gereksiz test/debug/RID build çıktıları ve geçici publish klasörü temizlendi; canlı `Data`, `Backup`, `Logs` korunuyor.

## Current Task
- [x] Tadilat Takibi satırlarına aynı ilçe/sekme içinde yukarı-aşağı taşıma ekle.
- [x] YİBF Ana Bilgi iş listesine yukarı-aşağı taşıma ekle.
- [x] YİBF İş Takibi satırlarına yukarı-aşağı taşıma ekle.
- [x] Sıralama davranışını undo/redo, persist/reload ve sınır senaryolarıyla test et.
- [x] Testleri çalıştır, güvenli Release publish al ve Git commit oluştur.

## Review Update
- Tadilat Takibi ekranında seçili satır artık aynı ilçe ve aynı Aktif/Biten sekmesi içinde `Yukarı` / `Aşağı` komutlarıyla taşınabiliyor; ilçe sınırı geçilmiyor.
- YİBF Ana Bilgi `Tüm İşler` listesine ve YİBF İş Takibi satırlarına aynı yukarı/aşağı sıralama komutları eklendi; mevcut `DisplayOrder`, undo/redo ve persist akışı korunuyor.
- Tadilat, YİBF Ana Bilgi ve YİBF İş Takibi için sıra değişimi, undo/redo ve persist/reload regresyon testleri `ModuleGuardTests` içine eklendi.
- `dotnet test RizaCanKilicIsTakibi.Tests\RizaCanKilicIsTakibi.Tests.csproj --filter "Tadilat|Yibf"` geçti: 34/34.
- `dotnet test RizaCanKilicIsTakibi.sln` geçti: 125/125.
- Release publish geçici klasöre alındı, uygulama çalışmadığı doğrulanınca canonical `RizaCanKilicIsTakibi\bin\Release\publish\RizaCanKilicIsTakibi.exe` güncellendi.
- Canlı `Data\tasks.db` ve `Data\last-save.json` hash/size/timestamp değerleri publish öncesi ve sonrası aynı kaldı.
- Gereksiz test/debug/RID build çıktıları temizlendi; repo içinde tek exe canonical publish exe olarak kaldı.

## Current Task
- [x] YİBF ana bilgi olay silme akışındaki stale timeline model referansını düzelt.
- [x] İki olay ekleme/kaydetme ve ardışık silme/persist regresyonlarını testlerle kilitle.
- [x] AGENTS.md ve PROJECT_RULES.md dosyalarını bu repo exe/publish/veri koruma kurallarına göre düzelt.
- [x] Hedefli/tam testleri çalıştır, güvenli Release publish al ve canlı `bin/Release/publish` verisini koru.
- [x] Gereksiz build çıktısı kalıntılarını canlı `Data`, `Backup`, `Logs` klasörlerine dokunmadan temizle.

## Review Update
- YİBF ana bilgi timeline item'ları artık refresh sırasında güncel `YibfAnaBilgiEvent` model referansına bağlanıyor; undo/snapshot sonrası bayat model yüzünden ikinci silmenin no-op olması kapatıldı.
- Silme komutu seçili olay referansını id üzerinden mevcut `AnaBilgiEvents` koleksiyonuna çözüyor; bildirim gösterip gerçek koleksiyondan silmeme regresyonu testle kilitlendi.
- `ModuleGuardTests` içine çoklu olay ekleme/persist ve ardışık olay silme/persist testleri eklendi.
- `AGENTS.md`, `PROJECT_RULES.md` ve `memories/repo/critical-notes.md` bu repo için canonical Release publish exe ve canlı `Data`/`Backup`/`Logs` korumasını anlatacak şekilde güncellendi.
- `dotnet test RizaCanKilicIsTakibi.Tests\RizaCanKilicIsTakibi.Tests.csproj --filter "Yibf"` geçti: 21/21.
- `dotnet test RizaCanKilicIsTakibi.sln` geçti: 122/122.
- Release publish önce geçici klasöre alındı, sonra `RizaCanKilicIsTakibi\bin\Release\publish\RizaCanKilicIsTakibi.exe` güncellendi. Canlı `tasks.db` ve `last-save.json` hash/size/timestamp değerleri değişmedi.
- Gereksiz test/app `bin`/`obj`, RID ara publish çıktısı ve nested `LatoFont\LatoFont` kalıntısı temizlendi; repo içinde tek exe canonical publish exe olarak kaldı.
- `.git` dizini olmadığı için zorunlu commit checkpoint'i uygulanamadı.

## Current Task
- [x] Genel İş Takibi ve diğer veri giriş sekmelerinde kaydetmeden önce aktif editör commit akışını analiz et.
- [x] Kaydetme başlamadan odaktaki düzenlemeyi zorla commit edecek minimal düzeltmeyi uygula.
- [x] Genel İş Takibi, Aksiyon, Eksik Proje, Tadilat ve YİBF için aynı paternin regresyonunu testlerle doğrula.
- [x] Review notlarını ve doğrulama sonucunu güncelle.

## Review Update
- Kaydet akışı artık önce odaktaki editörü `PendingEditCommitHelper` ile flush ediyor, ardından Aksiyon, Eksik Proje, Tadilat ve YİBF modüllerinde edit modunda kalan draft hücreleri model seviyesinde topluca commit ediyor.
- Böylece kullanıcı mevcut kaydı düzenleyip doğrudan `Kaydet` dediğinde persist snapshot'ı eski değeri almıyor; draft önce gerçek modele yazılıyor, sonra repository save çalışıyor.
- Genel İş Takibi, Aksiyon, Eksik Proje, Tadilat ve YİBF sekmeleri için aynı patern hedefli testlerle doğrulandı; ayrıca daha önce var olan yüklü-kayıt düzenleme testleri yeniden geçirildi.
- Genel İş Takibi için ek olarak başlık editörlerinde `TextChanged` anında `CommitGeneralEditCommand` tetikleniyor; böylece kullanıcı focus değiştirmeden yazarken bile `HasUnsavedChanges` hemen oluşuyor ve `Kaydedilecek değişiklik yok` yanlış-negatifi kapanıyor.
- `dotnet test RizaCanKilicIsTakibi.Tests\\RizaCanKilicIsTakibi.Tests.csproj --filter "SaveActiveTabCommand_Commits_Pending_Action_Edit_Before_Persist|SaveActiveTabCommand_Commits_Pending_MissingProject_Edit_Before_Persist|SaveActiveTabCommand_Commits_Pending_Tadilat_Edit_Before_Persist|SaveActiveTabCommand_Commits_Pending_Yibf_Edit_Before_Persist|SaveActiveTabCommand_Persists_Loaded_General_Task_Title_Edit"` geçti: 5/5.
- `dotnet test RizaCanKilicIsTakibi.Tests\\RizaCanKilicIsTakibi.Tests.csproj --filter "SaveActiveTabCommand_Persists_Loaded_Action_Edit|SaveActiveTabCommand_Persists_Loaded_MissingProject_Edit|SaveActiveTabCommand_Persists_Loaded_Tadilat_Edit|SaveActiveTabCommand_Persists_Loaded_Yibf_IsTakibi_Edit|SaveActiveTabCommand_Persists_Loaded_General_Task_Title_Edit"` geçti: 5/5.
- `dotnet build RizaCanKilicIsTakibi.sln` başarılı oldu.

## Current Task
- [x] Son kayıt göstergesini WAL veya startup-dokunan dosya zamanlarından bağımsız kalıcı metadata ile besle.
- [x] Açılışta metadata varsa onu, yoksa kontrollü fallback'i kullanacak şekilde `MainViewModel` başlangıç akışını düzelt.
- [x] İlgili metadata servis testlerini ve save-status viewmodel testlerini güncelle.
- [x] Testleri çalıştır, publish al ve aktif `bin/Release/publish` klasörünü veri kaybetmeden güncelle.

## Review Update
- Sol alttaki "Son kayıt" bilgisi artık `last-save.json` içindeki uygulama tarafından yazılan gerçek başarılı kayıt zamanını kullanıyor; SQLite `-wal` dokunuşları açılışta tarihi bugüne çekemiyor.
- Başlangıçta metadata varsa doğrudan o okunuyor; metadata henüz yoksa ilk geçiş için yalnız `tasks.db` ve `settings.json` üzerinden kontrollü fallback çalışıyor, `tasks.db-wal` dikkate alınmıyor.
- Başarılı genel/modül/ayar kayıtları tek yerden hem `LastSuccessfulSaveAt` alanını hem de kalıcı metadata dosyasını güncelliyor.
- `dotnet test RizaCanKilicIsTakibi.sln` çalıştı: 115/115 test geçti.

## Current Task
- [x] Açılıştaki kayıt durumu göstergesini placeholder yerine diskteki son kayıt zamanını gösterecek şekilde güncelle.
- [x] İlgili viewmodel testlerini yeni başlangıç davranışına göre güncelle.
- [ ] Testleri çalıştır, publish al ve aktif `bin/Release/publish` klasörünü veri kaybetmeden güncelle.

## Review Update
- Sol alttaki kayıt göstergesi artık açılışta `tasks.db` ve varsa `settings.json` dosyalarının en güncel son yazılma zamanını kullanıyor; kullanıcı ilk anda önceki gerçek disk kaydını görüyor.
- Oturum içinde yapılan başarılı kayıtlar aynı göstergede ilerlemeye devam ediyor; mevcut `Kaydedildi` / `Kaydedilmedi` mantığı değişmedi.
- Başlangıç testi, placeholder metin yerine kalıcı dosyalardan türetilen son kayıt zamanını doğrulayacak şekilde güncellendi.

## Current Task
- [x] Kısmi kaydetme hatasında kalıcı veriyi koruyacak güvenli rollback akışını ekle.
- [x] Manuel JSON yedeğinde tüm modüllerin yüklenmesini zorunlu kıl.
- [x] Boş veya yapısal olarak geçersiz JSON backup import'unu reddet.
- [x] Yedek dosya adı çakışma riskini azalt ve hedefli testleri çalıştır.

## Safety Review Update
- `SaveAllTabsSafelyAsync` ve genel görevler için güvenli persist sarmalı eklenerek başarısız kaydetmede disk üstündeki önceki kalıcı veri snapshot'tan geri yükleniyor.
- Import/reset rollback hattı ikinci kez `SaveAllTabsAsync` çalıştırmak yerine dosya snapshot restore + UI state restore modeline geçirildi; böylece rollback sırasında kaydedilmemiş eski çalışma diske zorla yazılmıyor.
- Manuel JSON yedeği artık backup almadan önce tüm modülleri initialize ediyor.
- `BackupService` boş/geçersiz JSON yedeğini reddediyor ve otomatik yedek adlarını milisaniye + GUID ile benzersiz üretiyor.
- `dotnet test RizaCanKilicIsTakibi.sln` çalıştı: 95/95 test geçti.

## Analysis
- [x] Workspace içindeki proje kural dosyalarını doğrula.
- [x] Uygulama başlangıç akışını ve bağımlılık kurulumunu incele.
- [x] Veri saklama, yedekleme ve içe/dışa aktarma yapısını incele.
- [x] Test kapsamı ve riskli alanları özetle.
- [x] Kullanıcıya veri güvenliğini bozmadan ilerleme stratejisi sun.

## Review
- Uygulama WPF + MVVM yapısında ve merkezi orkestrasyon `MainViewModel` üzerinden ilerliyor.
- Canlı veri `Data/tasks.db`, ayarlar `Data/settings.json`, otomatik JSON yedekler `Backup/`, loglar `Logs/` altında tutuluyor.
- Modüller arası toplu kayıt işlemleri gerçek tek veritabanı transaction'ı yerine snapshot + telafi rollback mantığıyla korunuyor; bu alan değişikliklerde en hassas bölge.
- `+` ile eklenen genel görevler üste alınacak şekilde güncellendi; yapıştırma davranışı korunuyor.
- `AppSettingsService` atomik yazıma çevrildi ve `PathService` single-file uyumlu olarak sadeleştirildi.
- Modül save-state akışı Karot ve üst seviye kaydet komutları üzerinden regresyon testleriyle doğrulandı; ek kod değişikliği gerektiren yeni bir yanlış-negatif paterni yeniden üretilemedi.
- `dotnet test RizaCanKilicIsTakibi.sln` çalıştı: 76/76 test geçti.

## Silent Save Audit
- [ ] Karot ve Eksik Proje için stale row ve reset sonrası dirty-state akışını doğrula, gerekirse lokal patch uygula.
- [ ] Genel İş Takibi `LostFocus` kaynaklı aktif edit kaybı riskini düzelt.
- [ ] Tadilat, YİBF ve Aksiyon için snapshot sonrası sessiz kayıt regresyon testleri ekle; ancak yeniden üretim varsa kodu değiştir.
- [ ] Tüm çözüm testlerini çalıştır ve sonuçları review bölümüne ekle.

## Current Work
- [x] Karot ve Eksik Proje için satır/viewmodel stale referanslarını ve save-state akışını doğrula, gerekiyorsa düzelt.
- [x] Genel İş Takibi içindeki `LostFocus` tabanlı aktif edit kayıp riskini düzelt.
- [x] Tadilat, YİBF ve Aksiyon için snapshot sonrası sessiz kayıt regresyon testlerini ekle.
- [x] Karot, Eksik Proje ve Genel İş Takibi için hedefli regresyon testlerini ekle.
- [x] Tüm çözümü test et ve review notunu güncelle.

## Current Task
- [x] Bozuk `settings.json` için sessiz default fallback'i kurtarma + uyarı modeline çevir.
- [x] Hücre bazlı sağ tık kopyala/yapıştır özelliğini hedef sekmelere ekle.
- [x] Hedefli testleri güncelle, tüm çözümü doğrula ve review notlarını yaz.

## Review Update
- `AppSettingsService.Load()` artık eksik/geçerli/bozuk dosya ayrımı yapan sonuç nesnesi döndürüyor; bozuk `settings.json` zaman damgalı `.corrupt.json` adına korunup uygulama varsayılan ayarlarla açılmaya devam ediyor.
- Startup sonrası bozuk ayar dosyası durumu mevcut toast altyapısı üzerinden kullanıcıya bildiriliyor; sessiz default fallback kaldırıldı.
- Karot, Eksik Proje, Tadilat ve YİBF hücre menülerine sistem panosu tabanlı `Kopyala` / `Yapıştır` eklendi; yapıştırma mevcut dirty-state ve save akışlarıyla uyumlu çalışıyor.
- Hücre panosu için OS bağımlı erişim `IClipboardService` üzerinden soyutlandı; bu sayede hedefli regresyon testleri gerçek Windows panosuna bağlı olmadan çalışıyor.
- `dotnet test RizaCanKilicIsTakibi.sln` çalıştı: 101/101 test geçti.

## Current Task
- [x] Bozuk `settings.json` için sessiz default fallback yerine kurtarma sonucu modeli ekle.
- [x] Startup sırasında bozuk ayar dosyasını `.corrupt` olarak koruyup kullanıcıya toast göster.
- [x] Karot, Eksik Proje, Tadilat ve YİBF hücrelerinde sağ tık kopyala/yapıştır komutlarını ekle.
- [x] İlgili testleri ve çözüm testlerini çalıştır, review notunu güncelle.

## Review Update
- `AppSettingsService.Load()` artık durum nesnesi döndürüyor; bozuk `settings.json` dosyası zaman damgalı `.corrupt.json` adına taşınıp varsayılan ayarlarla açılış sürdürülüyor.
- Startup akışında bozuk ayar dosyası tespit edilirse kullanıcıya toast ile kurtarma bilgisi gösteriliyor; eksik dosya davranışı değişmedi.
- Karot, Eksik Proje, Tadilat ve YİBF hücrelerine sağ tık `Kopyala` / `Yapıştır` eklendi; pano akışı yalnız düz metin taşıyor ve yapıştırma sonrası dirty-state doğru set ediliyor.
- Tadilat `Biten` gibi read-only hücrelerde yapıştırma komutu devre dışı kaldı; mevcut not/renk davranışları korunuyor.
- `dotnet test RizaCanKilicIsTakibi.sln` çalıştı: 101/101 test geçti.

## Current Task
- [x] Sol alt sidebar'a son başarılı global kayıt durum alanını ekle.
- [x] `MainViewModel` içine global kayıt zamanı ve durum metinlerini ekle.
- [x] Başarılı kaydetme noktalarında global kayıt zamanını güncelle; hata ve no-op akışlarında ilerletme.
- [x] ViewModel testlerini çalıştır ve review notunu güncelle.

## Review Update
- Sol sidebar'da `AYARLAR` butonunun üstüne küçük bir kayıt durumu alanı eklendi; `Kaydedildi` / `Kaydedilmedi` metni ve son başarılı kayıt zamanı gösteriliyor.
- `MainViewModel` artık `LastSuccessfulSaveAt`, `SaveStatusText` ve `SaveStatusTimestampText` üretiyor; bu alanlar birleşik dirty-state ile hizalı çalışıyor.
- Global kayıt zamanı yalnız gerçekten başarılı kaydetme sonrası güncelleniyor; `Kaydedilecek değişiklik yok` ve başarısız kayıt akışları zamanı ilerletmiyor.
- Modül bazlı dirty-state değişimleri kayıt durumu alanını anında yeniliyor; bu yüzden kullanıcı sol alttan canlı olarak kaydedilmemiş durumunu görebiliyor.
- `dotnet test RizaCanKilicIsTakibi.sln` çalıştı: 105/105 test geçti.

## Current Task
- [x] Açılışta sol alt kayıt göstergesini bu oturum varsayılanı yerine son kalıcı kayıt zamanı ile başlat.
- [x] Başlangıçta veritabanı ve ayar dosyası zaman damgalarından en güncel kalıcı kayıt zamanını türet.
- [x] İlgili viewmodel testlerini güncelle, tüm çözümü test et ve aktif publish klasörünü veri kaybetmeden güncelle.

## Review Update
- Sol alttaki kayıt göstergesi artık yeni oturum açıldığında boş varsayılan yerine diskteki son kalıcı kayıt zamanını gösteriyor; zaman damgası görev veritabanı ve ayar dosyası arasında en güncel yazımdan türetiliyor.
- Yeni oluşturulmuş ama henüz gerçek veri kaydı içermeyen boş veritabanı dosyası artık “son kayıt” gibi gösterilmiyor; bu durumda mevcut `Bu oturumda kayıt yapılmadı` fallback'i korunuyor.
- Ayar dosyası görev verisinden daha yeni ise startup göstergesi ayar kaydının zamanını baz alıyor; böylece sol alttaki bilgi gerçekten son global kalıcı yazımı temsil ediyor.
- `dotnet test RizaCanKilicIsTakibi.sln` çalıştı: 106/106 test geçti.

## Review Update
- Genel İş Takibi başlık alanlarında `UpdateSourceTrigger=PropertyChanged` kullanılarak aktif editin kaydetme anında kaybolma riski kapatıldı.
- Karot ve Eksik Proje için daha önce uygulanan `Reset` sonrası yeniden bağlanma düzeltmesi korunup regresyon testleriyle desteklendi.
- Tadilat, YİBF ve Aksiyon modüllerinde yeni sessiz kayıt kod değişikliği gerektiren bir patern yeniden üretilmedi; bunun yerine `yükle -> düzenle -> kaydet` ve undo/redo sonrası kayıt akışları testlerle kilitlendi.
- `dotnet test RizaCanKilicIsTakibi.sln` çalıştı: 91/91 test geçti.

## Current Task
- [x] Eksik Proje, Tadilat ve YİBF için hücre panosunu `metin + renk + not` taşıyacak şekilde genişlet.
- [x] Structured clipboard payload için ortak model ve servis desteği ekle; düz metin fallback'ini koru.
- [x] Regresyon testlerini güncelle, tüm çözümü doğrula ve aktif publish klasörünü veri kaybetmeden güncelle.

## Review Update
- Hücre panosu artık `CellClipboardPayload` ile `metin + dolgu rengi + not` taşıyor; `IClipboardService` structured payload okuyup yazabiliyor ve düz metin fallback'i korunuyor.
- `Eksik Proje`, `Tadilat` ve `YİBF` modüllerinde sağ tık `Kopyala/Yapıştır` tam ez davranışıyla çalışıyor; kaynakta boş renk/not varsa hedef state temizleniyor.
- `Karot` bu sürümde bilinçli olarak text-only kaldı; hücre bazlı dolgu rengi altyapısı olmadığı için kapsam dışında tutuldu.
- Clipboard regresyon testleri serializer güvenliği, text fallback, renk/not kopyası, state temizleme ve persist davranışını kapsayacak şekilde genişletildi.
- `dotnet test RizaCanKilicIsTakibi.sln` çalıştı: 109/109 test geçti.

## Current Task
- [x] Genel İş detay panelinden `İş Bitti` butonunu ve bağlı komut akışını kaldır.
- [x] Aksiyon satırlarına sağ tık `Üste Satır Ekle` / `Alta Satır Ekle` komutlarını dialog tabanlı insert mantığıyla ekle.
- [x] Aksiyon insert ve veri giriş sekmeleri kayıt akışı için hedefli testleri güncelle; tüm çözümü test et ve aktif publish klasörünü veri kaybetmeden güncelle.

## Review Update
- Genel İş detay panelindeki `İş Bitti` düğmesi kaldırıldı; buna bağlı `MarkAsCompleted` kısayolu da viewmodel'den temizlendi.
- Aksiyon satırlarına sağ tık menüsü eklendi; `Üste Satır Ekle` ve `Alta Satır Ekle` mevcut ekleme dialogu ile çalışıp yeni kaydı aynı ilçe ve aynı alt sekmede doğru konuma yerleştiriyor.
- Aksiyon insert akışı mevcut snapshot/undo-redo modeli ile uyumlu; seçili ilçe sırası normalize ediliyor ve persist sonrası kategori/sıra korunuyor.
- Veri girişi olan sekmeler için kayıt doğrulaması otomatik testlerle genişletildi; Genel İş, Aksiyon, Eksik Proje, Karot, Tadilat, YİBF Ana Bilgi ve YİBF İş Takibi kayıt akışları kapsanıyor.
- `dotnet test RizaCanKilicIsTakibi.sln` çalıştı: 112/112 test geçti.
