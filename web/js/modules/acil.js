window.WebModules = window.WebModules || {};

WebModules.acil = function renderAcil(state) {
  var UI = WebUI;
  var q = state.query;
  var tasks = (state.envelope.data.tasks || []).filter(function (t) {
    return t.boardType === 1 || t.boardType === "Acil";
  });

  var filtered = tasks.filter(function (t) {
    return WebViewParser.includesQuery(WebViewParser.joinSearchable(
      t.title, t.description, (t.notes || []).map(function (n) { return n.text; }).join(" ")
    ), q);
  });

  if (!filtered.length) {
    return UI.emptyState("Acil iş bulunamadı.");
  }

  return UI.wrapModule("Acil İşler", filtered.length, UI.renderTable([
    { key: "title", label: "Başlık", sticky: true, className: "text-wrap" },
    { key: "description", label: "Açıklama", className: "text-wrap" },
    {
      label: "Notlar",
      className: "text-wrap",
      render: function (row) {
        var notes = (row.notes || []).map(function (n) { return n.text; }).filter(Boolean).join(" · ");
        return UI.cell(notes);
      }
    },
    {
      label: "Son Tarih",
      render: function (row) { return UI.formatDate(row.dueDate); }
    }
  ], filtered));
};
