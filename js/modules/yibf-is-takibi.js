window.WebModules = window.WebModules || {};

WebModules.yibfIsTakibi = function renderYibfIsTakibi(state) {
  var UI = WebUI;
  var Parser = WebViewParser;
  var entries = state.envelope.data.yibfIsTakibiEntries || [];
  var cellStateMap = Parser.buildCellStateMap(state.envelope.data.yibfCellStates);
  var q = state.query;
  var cell = UI.createTrackedCellRenderer.bind(UI, cellStateMap);

  var filtered = entries.filter(function (e) {
    return Parser.includesQuery(Parser.joinSearchable(
      e.jobName, e.workVariantLabel, e.muellifBilgileriGeldiMi, e.denetciAtamalariYapildiMi,
      e.tumProjelerinDijitaliVarMi, e.evraklarTamMi, e.yibfSozlesmeHazirlandiMi, e.dekontAlindiMi,
      e.ruhsatBasvurusuYapildiMi, e.ruhsatNushasiAlindiMi, e.isyeriTeslimTutangiHazirlandiMi,
      e.isgYazisiHazirlandiMi, e.saglikGuvenlikPlaniGeldiMi, e.temelTopraklamaTutanagiHazirlandiMi,
      Parser.collectEntryCellNotes(cellStateMap, e.id)
    ), q);
  });

  if (!filtered.length) {
    return UI.wrapModule("YİBF İŞ TAKİBİ", 0, UI.emptyState("YİBF İş Takibi kaydı bulunamadı."));
  }

  return UI.wrapModule(
    "YİBF İŞ TAKİBİ",
    filtered.length,
    UI.renderTable([
      { label: "İşin İsmi", sticky: true, className: "text-wrap", render: cell("JobName", function (row) { return row.jobName; }, { textMode: true }) },
      { label: "Müellif Bilgileri", render: cell("MuellifBilgileriGeldiMi", function (row) { return row.muellifBilgileriGeldiMi; }) },
      { label: "Denetçi Atamaları", render: cell("DenetciAtamalariYapildiMi", function (row) { return row.denetciAtamalariYapildiMi; }) },
      { label: "Dijital", render: cell("TumProjelerinDijitaliVarMi", function (row) { return row.tumProjelerinDijitaliVarMi; }) },
      { label: "Evraklar", render: cell("EvraklarTamMi", function (row) { return row.evraklarTamMi; }) },
      { label: "YİBF Sözleşme", render: cell("YibfSozlesmeHazirlandiMi", function (row) { return row.yibfSozlesmeHazirlandiMi; }) },
      { label: "Dekont", render: cell("DekontAlindiMi", function (row) { return row.dekontAlindiMi; }) },
      { label: "Ruhsat Başvurusu", render: cell("RuhsatBasvurusuYapildiMi", function (row) { return row.ruhsatBasvurusuYapildiMi; }) },
      { label: "Ruhsat Nüshası", render: cell("RuhsatNushasiAlindiMi", function (row) { return row.ruhsatNushasiAlindiMi; }) },
      { label: "İşyeri Teslim", render: cell("IsyeriTeslimTutangiHazirlandiMi", function (row) { return row.isyeriTeslimTutangiHazirlandiMi; }) },
      { label: "İSG Yazısı", render: cell("IsgYazisiHazirlandiMi", function (row) { return row.isgYazisiHazirlandiMi; }) },
      { label: "SG Planı", render: cell("SaglikGuvenlikPlaniGeldiMi", function (row) { return row.saglikGuvenlikPlaniGeldiMi; }) },
      { label: "Topraklama", render: cell("TemelTopraklamaTutanagiHazirlandiMi", function (row) { return row.temelTopraklamaTutanagiHazirlandiMi; }) }
    ], filtered, { compact: true })
  );
};
