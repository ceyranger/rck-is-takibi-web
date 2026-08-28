window.WebModules = window.WebModules || {};
var escapeHtml = WebModules.escapeHtml;

WebModules.personel = function renderPersonel(state) {
  var rows = state.envelope.derived.personnelGorevItems || [];
  var q = state.query;

  var filtered = rows.filter(function (r) {
    return WebViewParser.includesQuery(WebViewParser.joinSearchable(
      r.personnelName, r.moduleLabel, r.summary, r.fieldLabel, r.projectIdentity, r.statusLabel
    ), q);
  });

  if (!filtered.length) {
    return '<div class="empty">Personel görevi bulunamadı.</div>';
  }

  return filtered.map(function (r) {
    return '<article class="card">' +
      '<h2>' + escapeHtml(r.personnelName) + '</h2>' +
      '<div class="meta">' + escapeHtml(r.moduleLabel) + ' · ' + escapeHtml(r.assignedAtText) + '</div>' +
      '<p>' + escapeHtml(r.summary || r.fieldLabel) + '</p>' +
      (r.projectIdentity ? '<p class="meta">' + escapeHtml(r.projectIdentity) + '</p>' : '') +
      '<span class="badge ' + (r.isOpen ? 'open' : '') + '">' + escapeHtml(r.statusLabel) + '</span> ' +
      '<span class="badge">' + escapeHtml(r.priorityLabel) + '</span>' +
      '</article>';
  }).join("");
};
