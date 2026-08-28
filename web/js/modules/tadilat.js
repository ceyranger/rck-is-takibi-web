window.WebModules = window.WebModules || {};
var escapeHtml = WebModules.escapeHtml;

WebModules.tadilat = function renderTadilat(state) {
  var entries = state.envelope.data.tadilatEntries || [];
  var q = state.query;

  var filtered = entries.filter(function (e) {
    return WebViewParser.includesQuery(WebViewParser.joinSearchable(
      e.adaParsel, e.yapiSahibi, e.district, e.muteahhit, e.aciklama
    ), q);
  });

  if (!filtered.length) {
    return '<div class="empty">Tadilat kaydı bulunamadı.</div>';
  }

  return filtered.map(function (e) {
    return '<article class="card">' +
      '<h2>' + escapeHtml(e.adaParsel) + ' · ' + escapeHtml(e.yapiSahibi) + '</h2>' +
      '<div class="meta">' + escapeHtml(e.district) + '</div>' +
      (e.aciklama ? '<p>' + escapeHtml(e.aciklama) + '</p>' : '') +
      '</article>';
  }).join("");
};
