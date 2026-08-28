window.WebModules = window.WebModules || {};

WebModules.projeOnay = function renderProjeOnay(state) {
  var UI = WebUI;
  var groups = state.envelope.derived.projeOnayItems || [];
  var q = state.query;

  var cards = groups.filter(function (g) {
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
      return '<div class="event-row" style="border-left-color:' + UI.escapeHtml(ev.categoryColor || "#94a3b8") + '">' +
        '<div><strong>' + UI.escapeHtml(ev.statusLabel) + '</strong>' +
        (ev.isOverdue ? ' <span class="badge critical">7+ gün</span>' : '') + '</div>' +
        '<div class="text-wrap">' + UI.escapeHtml(ev.summary) + '</div>' +
        '<div class="meta">' + UI.escapeHtml(ev.eventDateText) + ' · ' + UI.escapeHtml(ev.daysElapsedText) + '</div>' +
        '</div>';
    }).join("");

    return '<article class="card">' +
      '<h2>' + UI.escapeHtml(g.titleText) + '</h2>' +
      '<div class="meta">' + UI.escapeHtml(g.adaParsel) + ' · ' + UI.escapeHtml(g.yapiSahibi) + '</div>' +
      (g.isOverdue ? '<span class="badge warning">Gecikmiş</span>' : '') +
      eventsHtml +
      '</article>';
  }).filter(Boolean);

  if (!cards.length) {
    return UI.emptyState("Proje onay kaydı bulunamadı.");
  }

  return UI.wrapModule("Proje Onay Takibi", cards.length, '<div class="card-grid">' + cards.join("") + '</div>');
};
