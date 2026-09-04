window.WebModules = window.WebModules || {};

WebModules.tumEksikler = function renderTumEksikler(state) {
  var UI = WebUI;
  var groups = state.envelope.derived.tumEksikler || [];
  var q = state.query;

  var sections = groups.filter(function (g) {
    var hay = WebViewParser.joinSearchable(g.headerText, g.detailText, g.adaParsel, g.yapiSahibi);
    if (WebViewParser.includesQuery(hay, q)) return true;
    return (g.items || []).some(function (item) {
      return WebViewParser.includesQuery(WebViewParser.joinSearchable(
        item.reason, item.fieldLabel, item.sourceModule, item.sourceContext, item.currentValue
      ), q);
    });
  }).map(function (g) {
    var items = (g.items || []).filter(function (item) {
      return !q || WebViewParser.includesQuery(WebViewParser.joinSearchable(
        g.headerText, item.reason, item.fieldLabel, item.sourceModule, item.sourceContext, item.currentValue
      ), q);
    });
    if (!items.length) return "";

    var table = UI.renderTable([
      { key: "sourceModule", label: "Kaynak" },
      { key: "fieldLabel", label: "Alan" },
      { key: "reason", label: "Eksik Nedeni", className: "text-wrap" },
      { key: "currentValue", label: "Mevcut Değer", className: "text-wrap" },
      { key: "sourceContext", label: "Satır", className: "text-wrap" },
      { key: "assignedPersonnelBadge", label: "Personel" },
      {
        label: "Önem",
        render: function (row) {
          var sev = String(row.severity || "").toLowerCase();
          var badgeClass = sev.indexOf("critical") >= 0 ? "critical" : sev.indexOf("warning") >= 0 ? "warning" : "";
          return '<span class="badge ' + badgeClass + '">' + UI.escapeHtml(row.severityLabel || row.severity) + "</span>";
        }
      }
    ], items);

    return '<section class="module-section" style="margin-bottom:0.75rem">' +
      UI.moduleHeader(g.headerText, items.length, g.detailText) +
      table +
      "</section>";
  }).filter(Boolean);

  if (!sections.length) {
    return UI.wrapModule("TÜM EKSİKLER", 0, UI.emptyState("Eksik kaydı bulunamadı."));
  }

  return '<div class="content">' + sections.join("") + "</div>";
};
