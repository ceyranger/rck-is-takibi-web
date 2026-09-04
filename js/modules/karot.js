window.WebModules = window.WebModules || {};

WebModules.karot = function renderKarot(state) {
  var UI = WebUI;
  var Parser = WebViewParser;
  var cellStateMap = Parser.buildCellStateMap(state.envelope.data.karotCellStates);
  var q = state.query;
  var subTab = state.subTabs.karot || "bekleyen";
  var cell = UI.createTrackedCellRenderer.bind(UI, cellStateMap);

  var entries = (state.envelope.data.karotEntries || []).filter(function (e) {
    var isYapilan = e.status === 2 || e.status === "KarotAlindiOlumlu";
    var matchTab = subTab === "yapilan" ? isYapilan : !isYapilan;
    if (!matchTab) return false;
    return Parser.includesQuery(Parser.joinSearchable(
      e.yibfNo, e.adaParsel, e.yapiSahibi, e.muteahhit, e.katBilgisi,
      e.betonSinifi, e.twentyEightDayResult, e.betonFirmasi, e.laboratuvar, e.aciklama,
      Parser.collectEntryCellNotes(cellStateMap, e.id)
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
    { label: "Numune Tarihi", render: cell("SampleReceivedDate", function (row) { return UI.formatDate(row.sampleReceivedDate); }, { textMode: true }) },
    { label: "YİBF No", render: cell("YibfNo", function (row) { return row.yibfNo; }, { textMode: true }) },
    { label: "Ada/Parsel", sticky: true, render: cell("AdaParsel", function (row) { return row.adaParsel; }, { textMode: true }) },
    { label: "Yapı Sahibi", className: "text-wrap", render: cell("YapiSahibi", function (row) { return row.yapiSahibi; }, { textMode: true }) },
    { label: "Müteahhit", className: "text-wrap", render: cell("Muteahhit", function (row) { return row.muteahhit; }, { textMode: true }) },
    { label: "Kat Bilgisi", className: "text-wrap", render: cell("KatBilgisi", function (row) { return row.katBilgisi; }, { textMode: true }) },
    { label: "Beton Sınıfı", render: cell("BetonSinifi", function (row) { return row.betonSinifi; }, { textMode: true }) },
    { label: "28 Gün Sonuç", render: cell("TwentyEightDayResult", function (row) { return row.twentyEightDayResult; }, { textMode: true }) },
    { label: "Beton Firması", className: "text-wrap", render: cell("BetonFirmasi", function (row) { return row.betonFirmasi; }, { textMode: true }) },
    { label: "Laboratuvar", className: "text-wrap", render: cell("Laboratuvar", function (row) { return row.laboratuvar; }, { textMode: true }) },
    { label: "Açıklama", className: "text-wrap", render: cell("Aciklama", function (row) { return row.aciklama; }, { textMode: true }) }
  ], entries, { rowClass: UI.karotRowClass, compact: true }));
};
