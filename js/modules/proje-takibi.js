window.WebModules = window.WebModules || {};

WebModules.projeTakibi = function renderProjeTakibi(state) {
  var UI = WebUI;
  var q = state.query;
  var entries = (state.envelope.data.yibfAnaBilgiEntries || []).slice();
  var events = state.envelope.data.yibfAnaBilgiEvents || [];
  var eventsByEntry = WebViewParser.groupYibfEventsByEntry(events);
  var pendingCount = (state.envelope.derived.projeOnayItems || []).length;

  entries.sort(function (a, b) {
    return (b.displayOrder || 0) - (a.displayOrder || 0);
  });

  var filtered = entries.filter(function (entry) {
    var entryEvents = eventsByEntry[entry.id] || [];
    var haystack = WebViewParser.joinSearchable(
      entry.adaParsel,
      entry.yibfNo,
      entry.idare,
      entry.yapiSahibi,
      entry.muteahhit,
      entryEvents.map(function (ev) {
        return WebViewParser.joinSearchable(ev.description, ev.noteText, ev.approvalStatus);
      }).join(" ")
    );
    return WebViewParser.includesQuery(haystack, q);
  });

  if (!filtered.length) {
    return UI.wrapModule(
      "PROJE TAKİBİ",
      0,
      UI.emptyState("Proje kaydı bulunamadı."),
      "Ana bilgi ve olay akışı"
    );
  }

  var selectedId = state.selections && state.selections.projeTakibiEntryId;
  var selected = filtered.find(function (entry) { return entry.id === selectedId; }) || filtered[0];
  var selectedEvents = (eventsByEntry[selected.id] || []).slice();

  return UI.wrapModule(
    "PROJE TAKİBİ",
    filtered.length,
    UI.renderProjeTakibiLayout(filtered, selected, selectedEvents, eventsByEntry, pendingCount),
    filtered.length + " iş · " + pendingCount + " bekleyen onay grubu"
  );
};
