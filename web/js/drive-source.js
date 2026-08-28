(function (global) {
  var STORAGE_KEY = "rckDriveFileId";
  var STORAGE_LABEL_KEY = "rckDriveFileLabel";

  function extractDriveFileId(input) {
    var raw = String(input || "").trim();
    if (!raw) {
      return "";
    }

    if (/^[A-Za-z0-9_-]{10,}$/.test(raw) && raw.indexOf("/") < 0 && raw.indexOf(" ") < 0) {
      return raw;
    }

    var match = raw.match(/\/file\/d\/([A-Za-z0-9_-]+)/);
    if (match && match[1]) {
      return match[1];
    }

    match = raw.match(/[?&]id=([A-Za-z0-9_-]+)/);
    if (match && match[1]) {
      return match[1];
    }

    return "";
  }

  function getStoredFileId(config) {
    try {
      var stored = String(localStorage.getItem(STORAGE_KEY) || "").trim();
      if (stored) {
        return stored;
      }
    } catch (e) {
      /* ignore */
    }

    return String((config && config.defaultDriveFileId) || "").trim();
  }

  function getStoredLabel() {
    try {
      return String(localStorage.getItem(STORAGE_LABEL_KEY) || "").trim();
    } catch (e) {
      return "";
    }
  }

  function saveFileId(fileId, label) {
    var normalized = String(fileId || "").trim();
    if (!normalized) {
      throw new Error("Geçerli bir Drive dosya kimliği gerekli.");
    }

    try {
      localStorage.setItem(STORAGE_KEY, normalized);
      localStorage.setItem(STORAGE_LABEL_KEY, String(label || normalized).trim());
    } catch (e) {
      throw new Error("Tarayıcı ayarı kaydedilemedi.");
    }
  }

  function clearFileId() {
    try {
      localStorage.removeItem(STORAGE_KEY);
      localStorage.removeItem(STORAGE_LABEL_KEY);
    } catch (e) {
      /* ignore */
    }
  }

  function formatSourceLabel(fileId, label) {
    if (label && label !== fileId) {
      return label;
    }
    if (!fileId) {
      return "Kaynak seçilmedi";
    }
    if (fileId.length <= 14) {
      return fileId;
    }
    return fileId.slice(0, 6) + "…" + fileId.slice(-4);
  }

  global.RckDriveSource = {
    extractDriveFileId: extractDriveFileId,
    getStoredFileId: getStoredFileId,
    getStoredLabel: getStoredLabel,
    saveFileId: saveFileId,
    clearFileId: clearFileId,
    formatSourceLabel: formatSourceLabel
  };
})(window);
