window.WebViewParser = (function () {
  function normalizeEnvelope(raw) {
    if (!raw || typeof raw !== "object") {
      throw new Error("Geçersiz JSON.");
    }
    if (raw.error) {
      throw new Error(raw.error);
    }
    if (raw.kind !== "web-view") {
      throw new Error("Beklenmeyen dosya türü.");
    }
    if (!raw.data || typeof raw.data !== "object") {
      throw new Error("Veri bölümü eksik.");
    }
    return {
      exportedAt: raw.exportedAt ? new Date(raw.exportedAt) : null,
      checksum: raw.checksum || "",
      data: raw.data,
      derived: raw.derived || {}
    };
  }

  function formatDateTime(value) {
    if (!value) return "—";
    try {
      return new Intl.DateTimeFormat("tr-TR", {
        dateStyle: "medium",
        timeStyle: "short"
      }).format(value);
    } catch {
      return String(value);
    }
  }

  function includesQuery(text, query) {
    if (!query) return true;
    return String(text || "").toLocaleLowerCase("tr-TR").includes(query.toLocaleLowerCase("tr-TR"));
  }

  function joinSearchable() {
    return Array.from(arguments).filter(Boolean).join(" ");
  }

  var MODULE_LABELS = {
    0: "Genel İş Takibi",
    1: "Acil İş Takibi",
    2: "Aksiyon",
    3: "Eksik Proje",
    4: "Karot Takibi",
    5: "Tadilat Takibi",
    6: "Proje Takibi",
    7: "YİBF İş Takibi",
    8: "Manuel"
  };

  var PRIORITY_LABELS = {
    1: "Uyarı",
    2: "Kritik",
    3: "Acil"
  };

  function personnelModuleLabel(sourceModule, snapshot) {
    if (snapshot && String(snapshot).trim()) {
      return String(snapshot).trim();
    }
    return MODULE_LABELS[sourceModule] || "—";
  }

  function personnelPriorityLabel(priority) {
    return PRIORITY_LABELS[priority] || "";
  }

  function formatPersonnelAssignedAt(value) {
    if (!value) return "—";
    try {
      var date = new Date(value);
      if (isNaN(date.getTime())) {
        return String(value);
      }
      return new Intl.DateTimeFormat("tr-TR", {
        dateStyle: "short",
        timeStyle: "short"
      }).format(date);
    } catch (err) {
      return String(value);
    }
  }

  function mapPersonnelAssignment(assignment, nameMap) {
    var personnelName = assignment.personnelId
      ? (nameMap[assignment.personnelId] || "Atanmamış")
      : "Atanmamış";
    var isOpen = assignment.status === 0;
    return {
      personnelName: personnelName,
      moduleLabel: personnelModuleLabel(assignment.sourceModule, assignment.moduleLabelSnapshot),
      summary: assignment.summarySnapshot || "",
      fieldLabel: assignment.fieldLabelSnapshot || "",
      projectIdentity: assignment.projectIdentitySnapshot || "",
      priorityLabel: personnelPriorityLabel(assignment.prioritySnapshot),
      statusLabel: isOpen ? "Açık" : "Tamamlandı",
      assignedAtText: formatPersonnelAssignedAt(assignment.assignedAt),
      isOpen: isOpen
    };
  }

  function buildPersonnelGorevRows(envelope) {
    var derived = envelope.derived && envelope.derived.personnelGorevItems;
    if (Array.isArray(derived) && derived.length) {
      return derived;
    }

    var assignments = (envelope.data && envelope.data.personnelAssignments) || [];
    if (!assignments.length) {
      return [];
    }

    var personnel = (envelope.data && envelope.data.personnel) || [];
    var nameMap = {};
    personnel.forEach(function (person) {
      if (person && person.id) {
        nameMap[person.id] = person.name || "";
      }
    });

    return assignments
      .filter(function (assignment) { return assignment.status === 0; })
      .map(function (assignment) { return mapPersonnelAssignment(assignment, nameMap); });
  }

  var APPROVAL_STATUS_LABELS = {
    Incelenecek: "İncelenecek",
    DenetcidenDonus: "Denetçiden dönüş bekleniyor",
    MuelliftenRevize: "Müelliften revize bekleniyor",
    Beklenen: "Beklenen",
    Onaylanan: "Onaylanan",
    Pasif: "Pasif"
  };

  var APPROVAL_STATUS_COLORS = {
    Incelenecek: "#FF0000",
    DenetcidenDonus: "#FFA500",
    MuelliftenRevize: "#FFFF00",
    Beklenen: "#E8E0A8",
    Onaylanan: "#92D050",
    Pasif: "#9E9E9E",
    Kategorisiz: "#D9D9D9"
  };

  function normalizeApprovalStatus(status) {
    var value = String(status || "").trim();
    if (!value) return "";
    var keys = Object.keys(APPROVAL_STATUS_LABELS);
    for (var i = 0; i < keys.length; i++) {
      if (keys[i].toLocaleLowerCase("tr-TR") === value.toLocaleLowerCase("tr-TR")) {
        return keys[i];
      }
    }
    return "";
  }

  function approvalStatusLabel(status) {
    var normalized = normalizeApprovalStatus(status);
    return APPROVAL_STATUS_LABELS[normalized] || "Kategorisiz";
  }

  function approvalStatusColor(status) {
    var normalized = normalizeApprovalStatus(status);
    return APPROVAL_STATUS_COLORS[normalized] || APPROVAL_STATUS_COLORS.Kategorisiz;
  }

  function wpfColorToCss(color) {
    var value = String(color || "").trim();
    if (!value) return "";
    if (value.length === 9 && value.charAt(0) === "#") {
      return "#" + value.slice(3);
    }
    return value;
  }

  function formatShortDate(value) {
    if (!value) return "—";
    try {
      var date = new Date(value);
      if (isNaN(date.getTime())) return String(value);
      return new Intl.DateTimeFormat("tr-TR", { dateStyle: "short" }).format(date);
    } catch (err) {
      return String(value);
    }
  }

  function groupYibfEventsByEntry(events) {
    var map = {};
    (events || []).forEach(function (event) {
      if (!event || !event.entryId) return;
      if (!map[event.entryId]) map[event.entryId] = [];
      map[event.entryId].push(event);
    });
    Object.keys(map).forEach(function (entryId) {
      map[entryId].sort(function (a, b) {
        var orderDiff = (a.displayOrder || 0) - (b.displayOrder || 0);
        if (orderDiff !== 0) return orderDiff;
        return new Date(a.eventDate || 0) - new Date(b.eventDate || 0);
      });
    });
    return map;
  }

  function getLatestYibfEvent(events) {
    if (!events || !events.length) return null;
    return events[events.length - 1];
  }

  function buildCellStateMap(cellStates) {
    var map = {};
    (cellStates || []).forEach(function (state) {
      if (!state || !state.entryId || !state.columnKey) return;
      map[state.entryId + "\0" + state.columnKey] = state;
    });
    return map;
  }

  function getCellState(map, entryId, columnKey) {
    if (!map || !entryId || !columnKey) return null;
    return map[entryId + "\0" + columnKey] || null;
  }

  function collectEntryCellNotes(map, entryId) {
    if (!map || !entryId) return "";
    var notes = [];
    Object.keys(map).forEach(function (key) {
      if (key.indexOf(entryId + "\0") !== 0) return;
      var note = map[key].noteText;
      if (note && String(note).trim()) notes.push(String(note).trim());
    });
    return notes.join(" ");
  }

  return {
    normalizeEnvelope,
    formatDateTime,
    includesQuery,
    joinSearchable,
    buildPersonnelGorevRows,
    approvalStatusLabel,
    approvalStatusColor,
    wpfColorToCss,
    formatShortDate,
    groupYibfEventsByEntry,
    getLatestYibfEvent,
    buildCellStateMap,
    getCellState,
    collectEntryCellNotes
  };
})();
