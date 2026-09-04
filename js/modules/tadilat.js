window.WebModules = window.WebModules || {};

WebModules.tadilat = function renderTadilat(state) {
  var UI = WebUI;
  var Parser = WebViewParser;
  var cellStateMap = Parser.buildCellStateMap(state.envelope.data.tadilatCellStates);
  var entries = (state.envelope.data.tadilatEntries || []).filter(function (e) {
    return e.subTab === 0 || e.subTab === "Aktif" || e.subTab === undefined;
  });
  var q = state.query;
  var cell = UI.createTrackedCellRenderer.bind(UI, cellStateMap);

  var filtered = entries.filter(function (e) {
    return Parser.includesQuery(Parser.joinSearchable(
      e.district, e.jobName, e.projectType, e.digitalReceived, e.inspectorApproved,
      e.outputAndReportArrived, e.officialLetterSubmitted, e.archivedFromMunicipality,
      e.description1, e.description2,
      Parser.collectEntryCellNotes(cellStateMap, e.id)
    ), q);
  });

  if (!filtered.length) {
    return UI.wrapModule("TADİLAT TAKİBİ", 0, UI.emptyState("Tadilat kaydı bulunamadı."));
  }

  return UI.wrapModule("TADİLAT TAKİBİ", filtered.length, UI.renderTable([
    { key: "district", label: "İlçe" },
    { label: "İşin İsmi", sticky: true, className: "text-wrap", render: cell("JobName", function (row) { return row.jobName; }, { textMode: true }) },
    { label: "Proje Türü", render: cell("ProjectType", function (row) { return row.projectType; }, { textMode: true }) },
    { label: "Dijital", render: cell("DigitalReceived", function (row) { return row.digitalReceived; }) },
    { label: "Denetçi Onayı", render: cell("InspectorApproved", function (row) { return row.inspectorApproved; }) },
    { label: "Çıktı/Rapor", render: cell("OutputAndReportArrived", function (row) { return row.outputAndReportArrived; }) },
    { label: "Üst Yazı", render: cell("OfficialLetterSubmitted", function (row) { return row.officialLetterSubmitted; }) },
    { label: "Arşiv", render: cell("ArchivedFromMunicipality", function (row) { return row.archivedFromMunicipality; }) },
    { label: "Açıklama 1", className: "text-wrap", render: cell("Description1", function (row) { return row.description1; }, { textMode: true }) },
    { label: "Açıklama 2", className: "text-wrap", render: cell("Description2", function (row) { return row.description2; }, { textMode: true }) }
  ], filtered, { compact: true }));
};
