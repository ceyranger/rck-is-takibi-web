window.WebModules = window.WebModules || {};

WebModules.karot = function renderKarot(state) {
  var UI = WebUI;
  var entries = state.envelope.data.karotEntries || [];
  var q = state.query;

  var filtered = entries.filter(function (e) {
    return WebViewParser.includesQuery(WebViewParser.joinSearchable(
      e.yibfNo, e.adaParsel, e.yapiSahibi, e.muteahhit, e.katBilgisi,
      e.betonSinifi, e.twentyEightDayResult, e.betonFirmasi, e.laboratuvar, e.aciklama
    ), q);
  });

  if (!filtered.length) {
    return UI.emptyState("Karot kaydı bulunamadı.");
  }

  return UI.wrapModule("Karot Takibi", filtered.length, UI.renderTable([
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
  ], filtered, { rowClass: UI.karotRowClass, compact: true }));
};
