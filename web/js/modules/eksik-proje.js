window.WebModules = window.WebModules || {};

WebModules.eksikProje = function renderEksikProje(state) {
  var UI = WebUI;
  var q = state.query;
  var entries = (state.envelope.data.missingProjectEntries || []).filter(function (e) {
    return WebViewParser.includesQuery(WebViewParser.joinSearchable(
      e.adaParsel, e.yapiSahibi, e.recordMediumText, e.missingProjectText, e.description
    ), q);
  });

  if (!entries.length) {
    return UI.wrapModule("EKSİK PROJE", 0, UI.emptyState("Eksik proje kaydı bulunamadı."));
  }

  return UI.wrapModule("EKSİK PROJE", entries.length, UI.renderTable([
    { key: "adaParsel", label: "Ada Parsel", sticky: true },
    { key: "yapiSahibi", label: "Yapı Sahibi", className: "text-wrap" },
    { key: "recordMediumText", label: "Fiziksel / Dijital" },
    { key: "missingProjectText", label: "Eksik Proje", className: "text-wrap" },
    { key: "description", label: "Açıklama", className: "text-wrap" }
  ], entries));
};
