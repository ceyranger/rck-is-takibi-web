window.WebModules = window.WebModules || {};

WebModules.arama = function renderArama(state) {
  var UI = WebUI;
  var q = state.query;
  if (!q) {
    return UI.emptyState("Arama kutusuna metin yazın.");
  }

  var viewState = {
    envelope: state.envelope,
    query: state.query,
    subTabs: state.subTabs,
    filters: state.filters
  };

  var sections = [
    WebModules.genelIsTakibi(viewState),
    WebModules.acilIsOzet(viewState),
    WebModules.tumEksikler(viewState),
    WebModules.aksiyon(viewState),
    WebModules.eksikProje(viewState),
    WebModules.karot(viewState),
    WebModules.tadilat(viewState),
    WebModules.projeTakibi(viewState),
    WebModules.yibfIsTakibi(viewState),
    WebModules.personel(viewState)
  ];

  var parts = sections.filter(function (html) {
    return html.indexOf("empty-state") < 0;
  });

  return parts.length ? parts.join("") : UI.emptyState("Sonuç bulunamadı.");
};
