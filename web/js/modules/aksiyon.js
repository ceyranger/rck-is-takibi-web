window.WebModules = window.WebModules || {};

WebModules.aksiyon = function renderAksiyon(state) {
  var UI = WebUI;
  var q = state.query;
  var subTab = state.subTabs.aksiyon || "aksiyon";
  var entries = (state.envelope.data.actionEntries || []).filter(function (entry) {
    var isAksiyon = entry.category === 0 || entry.category === "Aksiyon";
    var matchTab = subTab === "aksiyon" ? isAksiyon : !isAksiyon;
    if (!matchTab) return false;
    return WebViewParser.includesQuery(WebViewParser.joinSearchable(
      entry.district, entry.ownerParcelText, entry.workText
    ), q);
  });

  var subTabs = UI.renderSubTabs("aksiyon", [
    { id: "aksiyon", label: "Aksiyon" },
    { id: "eklenecekler", label: "Aksiyona Eklenecekler" }
  ], subTab);

  return UI.wrapModule("AKSİYON", entries.length, subTabs + UI.renderActionTable(entries));
};
