# Todo

## Current Task: Repo içi web export + git push
- [x] AppSettings: WebViewRepoRoot + WebViewGitSyncEnabled
- [x] WebViewGitSyncService + WebViewRepoPaths
- [x] MainViewModel.WebView + Settings UI (repo yolu, klasör seç kaldırıldı)
- [x] WebViewGitSyncServiceTests
- [x] Build/test 236/236 + Release publish (Data korundu)

## Review Update
- Kaydet sonrası JSON `web/export/web-view-latest.json` dosyasına yazılır; git add/commit/push ile site güncellenir.
- Yedek Drive klasörü artık kullanılmıyor; repo kökü Ayarlarda (varsayılan masaüstü repo).
- Git push başarısız olursa GitHub Contents API yedek devreye girer.
