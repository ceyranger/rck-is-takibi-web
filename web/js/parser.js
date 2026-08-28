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

  return {
    normalizeEnvelope,
    formatDateTime,
    includesQuery,
    joinSearchable
  };
})();
