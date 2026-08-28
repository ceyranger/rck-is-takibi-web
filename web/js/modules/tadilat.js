window.WebModules = window.WebModules || {};

WebModules.tadilat = function renderTadilat(state) {
  var UI = WebUI;
  var entries = (state.envelope.data.tadilatEntries || []).filter(function (e) {
    return e.subTab === 0 || e.subTab === "Aktif" || e.subTab === undefined;
  });
  var q = state.query;

  var filtered = entries.filter(function (e) {
    return WebViewParser.includesQuery(WebViewParser.joinSearchable(
      e.district, e.jobName, e.projectType, e.digitalReceived, e.inspectorApproved,
      e.outputAndReportArrived, e.officialLetterSubmitted, e.archivedFromMunicipality,
      e.description1, e.description2
    ), q);
  });

  if (!filtered.length) {
    return UI.wrapModule("TADİLAT TAKİBİ", 0, UI.emptyState("Tadilat kaydı bulunamadı."));
  }

  return UI.wrapModule("TADİLAT TAKİBİ", filtered.length, UI.renderTable([
    { key: "district", label: "İlçe" },
    { key: "jobName", label: "İşin İsmi", sticky: true, className: "text-wrap" },
    { key: "projectType", label: "Proje Türü" },
    { label: "Dijital", render: function (row) { return UI.statusPill(row.digitalReceived); } },
    { label: "Denetçi Onayı", render: function (row) { return UI.statusPill(row.inspectorApproved); } },
    { label: "Çıktı/Rapor", render: function (row) { return UI.statusPill(row.outputAndReportArrived); } },
    { label: "Üst Yazı", render: function (row) { return UI.statusPill(row.officialLetterSubmitted); } },
    { label: "Arşiv", render: function (row) { return UI.statusPill(row.archivedFromMunicipality); } },
    { key: "description1", label: "Açıklama 1", className: "text-wrap" },
    { key: "description2", label: "Açıklama 2", className: "text-wrap" }
  ], filtered, { compact: true }));
};
