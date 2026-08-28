window.WebModules = window.WebModules || {};
var escapeHtml = WebModules.escapeHtml;

WebModules.tumEksikler = function renderTumEksikler(state) {
  var groups = state.envelope.derived.tumEksikler || [];
  var q = state.query;

  var html = groups.filter(function (g) {
    var hay = WebViewParser.joinSearchable(g.headerText, g.detailText, g.adaParsel, g.yapiSahibi);
    if (WebViewParser.includesQuery(hay, q)) return true;
    return (g.items || []).some(function (item) {
      return WebViewParser.includesQuery(WebViewParser.joinSearchable(item.reason, item.fieldLabel, item.sourceModule), q);
    });
  }).map(function (g) {
    var items = (g.items || []).filter(function (item) {
      return !q || WebViewParser.includesQuery(WebViewParser.joinSearchable(
        g.headerText, item.reason, item.fieldLabel, item.sourceModule, item.sourceContext
      ), q);
    });
    if (!items.length) return "";

    var itemsHtml = items.map(function (item) {
      var sev = (item.severity || "").toLowerCase();
      var badgeClass = sev.indexOf("critical") >= 0 ? "critical" : sev.indexOf("warning") >= 0 ? "warning" : "";
      return '<div class="event-row">' +
        '<div><span class="badge ' + badgeClass + '">' + escapeHtml(item.severityLabel || item.severity) + '</span> ' +
        '<strong>' + escapeHtml(item.sourceModule) + '</strong></div>' +
        '<div>' + escapeHtml(item.fieldLabel) + ': ' + escapeHtml(item.reason) + '</div>' +
        (item.sourceContext ? '<div class="meta">' + escapeHtml(item.sourceContext) + '</div>' : '') +
        (item.assignedPersonnelBadge ? '<div class="meta">Personel: ' + escapeHtml(item.assignedPersonnelBadge) + '</div>' : '') +
        '</div>';
    }).join("");

    return '<article class="card">' +
      '<h2>' + escapeHtml(g.headerText) + '</h2>' +
      '<div class="meta">' + escapeHtml(g.detailText) + ' · ' + escapeHtml(g.countText || (g.eksikCount + " eksik")) + '</div>' +
      itemsHtml +
      '</article>';
  }).filter(Boolean).join("");

  return html || '<div class="empty">Eksik kaydı bulunamadı.</div>';
};
