window.WebModules = window.WebModules || {};

WebModules.genelIsTakibi = function renderGenelIsTakibi(state) {
  var UI = WebUI;
  var q = state.query;
  var tasks = state.envelope.data.tasks || [];

  var acil = tasks.filter(function (t) {
    return (t.boardType === 1 || t.boardType === "Acil") &&
      WebViewParser.includesQuery(WebViewParser.joinSearchable(t.title, t.description), q);
  });

  var genel = tasks.filter(function (t) {
    return (t.boardType === 0 || t.boardType === "Genel") &&
      WebViewParser.includesQuery(WebViewParser.joinSearchable(t.title, t.description), q);
  });

  return UI.wrapModule(
    "GENEL İŞ TAKİBİ",
    acil.length + genel.length,
    UI.splitColumns(
      "Acil İşler (" + acil.length + ")",
      UI.renderTaskTable(acil),
      "Genel İşler (" + genel.length + ")",
      UI.renderTaskTable(genel)
    ),
    "Masaüstü uygulamadaki iki sütun düzeni"
  );
};
