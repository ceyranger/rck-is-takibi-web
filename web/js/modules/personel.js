window.WebModules = window.WebModules || {};

WebModules.personel = function renderPersonel(state) {
  var UI = WebUI;
  var rows = WebViewParser.buildPersonnelGorevRows(state.envelope);
  var q = state.query;
  var activeFilter = (state.filters && state.filters.personel) || "all";

  var filtered = rows.filter(function (r) {
    if (activeFilter === "unassigned") {
      if (r.personnelName !== "Atanmamış") return false;
    } else if (activeFilter !== "all") {
      if (r.personnelName !== activeFilter) return false;
    }
    return WebViewParser.includesQuery(WebViewParser.joinSearchable(
      r.personnelName, r.moduleLabel, r.summary, r.fieldLabel, r.projectIdentity,
      r.statusLabel, r.priorityLabel
    ), q);
  });

  if (!rows.length) {
    return UI.wrapModule(
      "PERSONEL GÖREV",
      0,
      UI.emptyState("Personel görevi bulunamadı. Bilgisayarda Şimdi Dışa Aktar yapıp 1-2 dk bekleyin."),
      "Açık görevler"
    );
  }

  var chips = buildPersonnelChips(rows, activeFilter);
  var chipsHtml = UI.renderFilterChips("personel", chips, activeFilter);

  if (!filtered.length) {
    return UI.wrapModule(
      "PERSONEL GÖREV",
      rows.length,
      chipsHtml + UI.emptyState("Seçili filtreye uygun görev bulunamadı."),
      "Açık görevler"
    );
  }

  return UI.wrapModule(
    "PERSONEL GÖREV",
    filtered.length,
    chipsHtml + UI.renderPersonnelGorevBoard(filtered),
    "Açık görevler · " + rows.length + " toplam"
  );
};

function buildPersonnelChips(rows, activeFilter) {
  var counts = {};
  rows.forEach(function (row) {
    var key = row.personnelName || "Atanmamış";
    counts[key] = (counts[key] || 0) + 1;
  });

  var names = Object.keys(counts).sort(function (a, b) {
    return a.localeCompare(b, "tr");
  });

  var chips = [{ id: "all", label: "Tümü", count: rows.length }];
  if (counts["Atanmamış"]) {
    chips.push({ id: "unassigned", label: "Atanmamış", count: counts["Atanmamış"] });
  }
  names.forEach(function (name) {
    if (name === "Atanmamış") return;
    chips.push({ id: name, label: name, count: counts[name] });
  });

  if (activeFilter !== "all" && activeFilter !== "unassigned" && !counts[activeFilter]) {
    chips.push({ id: activeFilter, label: activeFilter, count: 0 });
  }

  return chips;
}
