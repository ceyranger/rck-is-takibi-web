window.WebModules = window.WebModules || {};

WebModules.karot = function renderKarot(state) {
  var UI = WebUI;
  var q = state.query;
  var subTab = state.subTabs.karot || "bekleyen";
  var entries = (state.envelope.data.karotEntries || []).filter(function (e) {
    var isYapilan = e.status === 2 || e.status === "KarotAlindiOlumlu";
    var matchTab = subTab === "yapilan" ? isYapilan : !isYapilan;
    if (!matchTab) return false;
    return WebViewParser.includesQuery(WebViewParser.joinSearchable(
      e.yibfNo, e.adaParsel, e.yapiSahibi, e.muteahhit, e.katBilgisi,
      e.betonSinifi, e.twentyEightDayResult, e.betonFirmasi, e.laboratuvar, e.aciklama
    ), q);
  });

  var subTabs = UI.renderSubTabs("karot", [
    { id: "bekleyen", label: "BEKLEYEN" },
    { id: "yapilan", label: "YAPILAN" }
  ], subTab);

  if (!entries.length) {
    return UI.wrapModule("KAROT TAKİBİ", 0, subTabs + UI.emptyState("Karot kaydı bulunamadı."));
  }

  return UI.wrapModule("KAROT TAKİBİ", entries.length, subTabs + UI.renderTable([
    { label: "Kayıt Durumu", render: function (row) { return UI.karotStatusLabel(row.status); } },
    { label: "Numune Tarihi", render: function (row) { return UI.formatDate(row.sampleReceivedDate); } },
    { key: "yibfNo", label: "YİBF No" },
    { key: "adaParsel", label: "Ada/Parsel", sticky: true },
    { key: "yapiSahibi", label: "Yapı Sahibi", className: "text-wrap" },
    { key: "muteahhit", label: "Müteahhit", className: "text-wrap" },
    { key: "katBilgisi", label: "Kat Bilgisi", className: "text-wrap" },
    { key: "betonSinifi", label: "Beton Sınıfı" },
    { key: "twentyEightDayResult", label: "28 Gün Sonuç" },
    { key: "betonFirmasi", label: "Beton Firması", className: "text-wrap" },
    { key: "laboratuvar", label: "Laboratuvar", className: "text-wrap" },
    { key: "aciklama", label: "Açıklama", className: "text-wrap" }
  ], entries, { rowClass: UI.karotRowClass, compact: true }));
};
