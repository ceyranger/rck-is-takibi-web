window.WebModules = window.WebModules || {};

WebModules.acil = function renderAcil(state) {
  const q = state.query;
  const tasks = (state.envelope.data.tasks || []).filter(function (t) {
    return t.boardType === 0 || t.boardType === "Acil";
  });

  const filtered = tasks.filter(function (t) {
    return WebViewParser.includesQuery(WebViewParser.joinSearchable(t.title, t.description), q);
  });

  if (!filtered.length) {
    return '<div class="empty">Acil iş bulunamadı.</div>';
  }

  return filtered.map(function (t) {
    const notes = (t.notes || []).map(function (n) { return n.text; }).join(" · ");
    return '<article class="card">' +
      '<h2>' + escapeHtml(t.title || "(Başlıksız)") + '</h2>' +
      (t.description ? '<p>' + escapeHtml(t.description) + '</p>' : '') +
      (notes ? '<p class="meta">Not: ' + escapeHtml(notes) + '</p>' : '') +
      '</article>';
  }).join("");
};

function escapeHtml(value) {
  return String(value || "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

WebModules.escapeHtml = escapeHtml;
