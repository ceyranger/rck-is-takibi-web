window.WebModules = window.WebModules || {};
var escapeHtml = WebModules.escapeHtml;

WebModules.projeOnay = function renderProjeOnay(state) {
  var groups = state.envelope.derived.projeOnayItems || [];
  var q = state.query;

  var html = groups.filter(function (g) {
    var hay = WebViewParser.joinSearchable(g.titleText, g.adaParsel, g.yapiSahibi);
    if (WebViewParser.includesQuery(hay, q)) return true;
    return (g.events || []).some(function (ev) {
      return WebViewParser.includesQuery(WebViewParser.joinSearchable(ev.summary, ev.statusLabel), q);
    });
  }).map(function (g) {
    var events = (g.events || []).filter(function (ev) {
      return !q || WebViewParser.includesQuery(WebViewParser.joinSearchable(g.titleText, ev.summary, ev.statusLabel), q);
    });

    if (!events.length) return "";

    var eventsHtml = events.map(function (ev) {
      return '<div class="event-row" style="border-left-color:' + escapeHtml(ev.categoryColor || "#ccc") + '">' +
        '<div><strong>' + escapeHtml(ev.statusLabel) + '</strong>' +
        (ev.isOverdue ? ' <span class="badge critical">7+ gün</span>' : '') + '</div>' +
        '<div>' + escapeHtml(ev.summary) + '</div>' +
        '<div class="meta">' + escapeHtml(ev.eventDateText) + ' · ' + escapeHtml(ev.daysElapsedText) + '</div>' +
        '</div>';
    }).join("");

    return '<article class="card">' +
      '<h2>' + escapeHtml(g.titleText) + '</h2>' +
      (g.isOverdue ? '<span class="badge warning">Gecikmiş</span> ' : '') +
      eventsHtml +
      '</article>';
  }).filter(Boolean).join("");

  return html || '<div class="empty">Proje onay kaydı bulunamadı.</div>';
};
