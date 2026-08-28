window.WebModules = window.WebModules || {};

WebModules.arama = function renderArama(state) {
  var UI = WebUI;
  var q = state.query;
  if (!q) {
    return UI.emptyState("Arama kutusuna metin yazın.");
  }

  var sections = [
    WebModules.acil(state),
    WebModules.projeOnay(state),
    WebModules.personel(state),
    WebModules.karot(state),
    WebModules.tadilat(state),
    WebModules.yibfIsTakibi(state),
    WebModules.tumEksikler(state)
  ];

  var parts = sections.filter(function (html) {
    return html.indexOf("empty-state") < 0;
  });

  return parts.length ? parts.join("") : UI.emptyState("Sonuç bulunamadı.");
};
