window.WebModules = window.WebModules || {};

WebModules.personel = function renderPersonel(state) {
  var UI = WebUI;
  var rows = state.envelope.derived.personnelGorevItems || [];
  var q = state.query;

  var filtered = rows.filter(function (r) {
    return WebViewParser.includesQuery(WebViewParser.joinSearchable(
      r.personnelName, r.moduleLabel, r.summary, r.fieldLabel, r.projectIdentity,
      r.statusLabel, r.priorityLabel
    ), q);
  });

  if (!filtered.length) {
    return UI.emptyState("Personel görevi bulunamadı.");
  }

  return UI.wrapModule("Personel Görevleri", filtered.length, UI.renderTable([
    { key: "personnelName", label: "Personel", sticky: true },
    { key: "moduleLabel", label: "Modül" },
    { key: "projectIdentity", label: "Proje", className: "text-wrap" },
    {
      label: "Görev",
      className: "text-wrap",
      render: function (row) { return UI.cell(row.summary || row.fieldLabel); }
    },
    { key: "priorityLabel", label: "Öncelik" },
    {
      label: "Durum",
      render: function (row) {
        return '<span class="badge ' + (row.isOpen ? "open" : "") + '">' + UI.escapeHtml(row.statusLabel) + "</span>";
      }
    },
    { key: "assignedAtText", label: "Atama" }
  ], filtered));
};
