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

  return {
    normalizeEnvelope,
    formatDateTime,
    includesQuery,
    joinSearchable,
    buildPersonnelGorevRows
  };
})();
