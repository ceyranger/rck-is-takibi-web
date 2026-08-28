window.WebModules = window.WebModules || {};
var escapeHtml = WebModules.escapeHtml;

WebModules.yibfIsTakibi = function renderYibfIsTakibi(state) {
  var entries = state.envelope.data.yibfIsTakibiEntries || [];
  var q = state.query;

  var filtered = entries.filter(function (e) {
    return WebViewParser.includesQuery(WebViewParser.joinSearchable(
      e.adaParsel, e.yapiSahibi, e.yibfNo, e.belediye, e.muteahhit, e.muellif
    ), q);
  });

  if (!filtered.length) {
    return '<div class="empty">YİBF İş Takibi kaydı bulunamadı.</div>';
  }

  return '<div class="table-wrap"><table><thead><tr>' +
    '<th>Ada/Parsel</th><th>Yapı Sahibi</th><th>YİBF No</th><th>Belediye</th><th>Müteahhit</th>' +
    '</tr></thead><tbody>' +
    filtered.map(function (e) {
      return '<tr>' +
        '<td>' + escapeHtml(e.adaParsel) + '</td>' +
        '<td>' + escapeHtml(e.yapiSahibi) + '</td>' +
        '<td>' + escapeHtml(e.yibfNo) + '</td>' +
        '<td>' + escapeHtml(e.belediye) + '</td>' +
        '<td>' + escapeHtml(e.muteahhit) + '</td>' +
        '</tr>';
    }).join("") +
    '</tbody></table></div>';
};
