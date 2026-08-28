window.WebModules = window.WebModules || {};
var escapeHtml = WebModules.escapeHtml;

WebModules.karot = function renderKarot(state) {
  var entries = state.envelope.data.karotEntries || [];
  var q = state.query;

  var filtered = entries.filter(function (e) {
    return WebViewParser.includesQuery(WebViewParser.joinSearchable(
      e.adaParsel, e.yapiSahibi, e.aciklama, e.muteahhit, e.betonFirmasi, e.laboratuvar
    ), q);
  });

  if (!filtered.length) {
    return '<div class="empty">Karot kaydı bulunamadı.</div>';
  }

  return '<div class="table-wrap"><table><thead><tr>' +
    '<th>Ada/Parsel</th><th>Yapı Sahibi</th><th>Durum</th><th>Açıklama</th>' +
    '</tr></thead><tbody>' +
    filtered.map(function (e) {
      return '<tr>' +
        '<td>' + escapeHtml(e.adaParsel) + '</td>' +
        '<td>' + escapeHtml(e.yapiSahibi) + '</td>' +
        '<td>' + escapeHtml(String(e.status ?? "")) + '</td>' +
        '<td>' + escapeHtml(e.aciklama) + '</td>' +
        '</tr>';
    }).join("") +
    '</tbody></table></div>';
};
