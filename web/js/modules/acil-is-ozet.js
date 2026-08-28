window.WebModules = window.WebModules || {};

WebModules.acilIsOzet = function renderAcilIsOzet(state) {
  var UI = WebUI;
  var q = state.query;
  var items = (state.envelope.derived.acilIsOzetItems || []).slice();
  if (!items.length) {
    items = (state.envelope.data.tasks || [])
      .filter(function (t) { return t.boardType === 1 || t.boardType === "Acil"; })
      .map(function (t) {
        return {
          category: "Genel",
          priorityLabel: "ACİL",
          priorityRank: 0,
          summary: t.title + (t.description ? " - " + t.description : "")
        };
      });
  }
  items = items.filter(function (item) {
    return WebViewParser.includesQuery(WebViewParser.joinSearchable(item.category, item.priorityLabel, item.summary), q);
  });

  var groups = state.envelope.derived.projeOnayItems || [];
  var filterKey = state.filters.projeOnay || "all";
  var filteredGroups = groups.filter(function (g) {
    var hay = WebViewParser.joinSearchable(g.titleText, g.adaParsel, g.yapiSahibi);
    if (WebViewParser.includesQuery(hay, q)) return true;
    return (g.events || []).some(function (ev) {
      return WebViewParser.includesQuery(WebViewParser.joinSearchable(ev.summary, ev.statusLabel), q);
    });
  });

  var chips = [
    { id: "all", label: "Tümü", count: countEvents(filteredGroups, "all") },
    { id: "Incelenecek", label: "İncelenecek", count: countEvents(filteredGroups, "Incelenecek") },
    { id: "DenetcidenDonus", label: "Denetçi", count: countEvents(filteredGroups, "DenetcidenDonus") },
    { id: "MuelliftenRevize", label: "Revize", count: countEvents(filteredGroups, "MuelliftenRevize") },
    { id: "Beklenen", label: "Beklenen", count: countEvents(filteredGroups, "Beklenen") },
    { id: "FilterKategorisiz", label: "Kategorisiz", count: countEvents(filteredGroups, "FilterKategorisiz") }
  ];

  var projePanel = '<div class="panel-stack">' +
    UI.renderFilterChips("projeOnay", chips, filterKey) +
    '<div class="card-grid">' + UI.renderProjeOnayGroups(filteredGroups, filterKey === "all" ? "" : filterKey) + "</div></div>";

  return UI.wrapModule(
    "ACİL İŞ ÖZET",
    items.length + filteredGroups.length,
    UI.splitColumns("Acil İş Özeti", UI.renderAcilOzetList(items), "Proje Onay Takibi", projePanel)
  );
};

function countEvents(groups, filterKey) {
  var total = 0;
  groups.forEach(function (g) {
    (g.events || []).forEach(function (ev) {
      if (filterKey === "all" || ev.filterKey === filterKey) total++;
    });
  });
  return total;
}
