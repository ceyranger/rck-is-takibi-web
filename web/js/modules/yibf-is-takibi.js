window.WebModules = window.WebModules || {};

WebModules.yibfIsTakibi = function renderYibfIsTakibi(state) {
  var UI = WebUI;
  var entries = state.envelope.data.yibfIsTakibiEntries || [];
  var q = state.query;

  var filtered = entries.filter(function (e) {
    return WebViewParser.includesQuery(WebViewParser.joinSearchable(
      e.jobName, e.workVariantLabel, e.muellifBilgileriGeldiMi, e.denetciAtamalariYapildiMi,
      e.tumProjelerinDijitaliVarMi, e.evraklarTamMi, e.yibfSozlesmeHazirlandiMi, e.dekontAlindiMi,
      e.ruhsatBasvurusuYapildiMi, e.ruhsatNushasiAlindiMi, e.isyeriTeslimTutangiHazirlandiMi,
      e.isgYazisiHazirlandiMi, e.saglikGuvenlikPlaniGeldiMi, e.temelTopraklamaTutanagiHazirlandiMi
    ), q);
  });

  if (!filtered.length) {
    return UI.wrapModule("YİBF İŞ TAKİBİ", 0, UI.emptyState("YİBF İş Takibi kaydı bulunamadı."));
  }

  return UI.wrapModule(
    "YİBF İŞ TAKİBİ",
    filtered.length,
    UI.renderTable([
      { key: "jobName", label: "İşin İsmi", sticky: true, className: "text-wrap" },
      { label: "Müellif Bilgileri", render: function (row) { return UI.statusPill(row.muellifBilgileriGeldiMi); } },
      { label: "Denetçi Atamaları", render: function (row) { return UI.statusPill(row.denetciAtamalariYapildiMi); } },
      { label: "Dijital", render: function (row) { return UI.statusPill(row.tumProjelerinDijitaliVarMi); } },
      { label: "Evraklar", render: function (row) { return UI.statusPill(row.evraklarTamMi); } },
      { label: "YİBF Sözleşme", render: function (row) { return UI.statusPill(row.yibfSozlesmeHazirlandiMi); } },
      { label: "Dekont", render: function (row) { return UI.statusPill(row.dekontAlindiMi); } },
      { label: "Ruhsat Başvurusu", render: function (row) { return UI.statusPill(row.ruhsatBasvurusuYapildiMi); } },
      { label: "Ruhsat Nüshası", render: function (row) { return UI.statusPill(row.ruhsatNushasiAlindiMi); } },
      { label: "İşyeri Teslim", render: function (row) { return UI.statusPill(row.isyeriTeslimTutangiHazirlandiMi); } },
      { label: "İSG Yazısı", render: function (row) { return UI.statusPill(row.isgYazisiHazirlandiMi); } },
      { label: "SG Planı", render: function (row) { return UI.statusPill(row.saglikGuvenlikPlaniGeldiMi); } },
      { label: "Topraklama", render: function (row) { return UI.statusPill(row.temelTopraklamaTutanagiHazirlandiMi); } }
    ], filtered, { compact: true })
  );
};
