window.WebModules = window.WebModules || {};

WebModules.eksikProje = function renderEksikProje(state) {
  var UI = WebUI;
  var Parser = WebViewParser;
  var cellStateMap = Parser.buildCellStateMap(state.envelope.data.missingProjectCellStates);
  var q = state.query;
  var cell = UI.createTrackedCellRenderer.bind(UI, cellStateMap);

  var entries = (state.envelope.data.missingProjectEntries || []).filter(function (e) {
    return Parser.includesQuery(Parser.joinSearchable(
      e.adaParsel, e.yapiSahibi, e.recordMediumText, e.missingProjectText, e.description,
      Parser.collectEntryCellNotes(cellStateMap, e.id)
    ), q);
  });

  if (!entries.length) {
    return UI.wrapModule("EKSİK PROJE", 0, UI.emptyState("Eksik proje kaydı bulunamadı."));
  }

  return UI.wrapModule("EKSİK PROJE", entries.length, UI.renderTable([
    { label: "Ada Parsel", sticky: true, render: cell("AdaParsel", function (row) { return row.adaParsel; }, { textMode: true }) },
    { label: "Yapı Sahibi", className: "text-wrap", render: cell("YapiSahibi", function (row) { return row.yapiSahibi; }, { textMode: true }) },
    { label: "Fiziksel / Dijital", render: cell("RecordMediumText", function (row) { return row.recordMediumText; }, { textMode: true }) },
    { label: "Eksik Proje", className: "text-wrap", render: cell("MissingProjectText", function (row) { return row.missingProjectText; }, { textMode: true }) },
    { label: "Açıklama", className: "text-wrap", render: cell("Description", function (row) { return row.description; }, { textMode: true }) }
  ], entries));
};
