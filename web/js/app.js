(function () {
  var TABS = [
    { id: "acil", label: "Acil İşler", render: WebModules.acil },
    { id: "proje-onay", label: "Proje Onay", render: WebModules.projeOnay },
    { id: "personel", label: "Personel", render: WebModules.personel },
    { id: "karot", label: "Karot", render: WebModules.karot },
    { id: "tadilat", label: "Tadilat", render: WebModules.tadilat },
    { id: "yibf", label: "YİBF İş", render: WebModules.yibfIsTakibi },
    { id: "eksikler", label: "Tüm Eksikler", render: WebModules.tumEksikler },
    { id: "arama", label: "Arama", render: WebModules.arama }
  ];

  var setupScreen = document.getElementById("setup-screen");
  var pinScreen = document.getElementById("pin-screen");
  var mainScreen = document.getElementById("main-screen");
  var driveLinkInput = document.getElementById("drive-link-input");
  var setupSave = document.getElementById("setup-save");
  var setupError = document.getElementById("setup-error");
  var pinInput = document.getElementById("pin-input");
  var pinSubmit = document.getElementById("pin-submit");
  var pinChangeSource = document.getElementById("pin-change-source");
  var pinError = document.getElementById("pin-error");
  var pinSourceLabel = document.getElementById("pin-source-label");
  var mainSourceLabel = document.getElementById("main-source-label");
  var lastUpdated = document.getElementById("last-updated");
  var refreshBtn = document.getElementById("refresh-btn");
  var settingsBtn = document.getElementById("settings-btn");
  var settingsDialog = document.getElementById("settings-dialog");
  var settingsDriveLink = document.getElementById("settings-drive-link");
  var settingsCancel = document.getElementById("settings-cancel");
  var settingsError = document.getElementById("settings-error");
  var globalSearch = document.getElementById("global-search");
  var tabBar = document.getElementById("tab-bar");
  var content = document.getElementById("content");

  var state = {
    pin: "",
    activeTab: TABS[0].id,
    query: "",
    envelope: null,
    driveFileId: ""
  };

  function getConfig() {
    return window.WEB_VIEWER_CONFIG || {};
  }

  function resolveDriveFileId() {
    return RckDriveSource.getStoredFileId(getConfig());
  }

  function refreshSourceLabels() {
    var fileId = resolveDriveFileId();
    state.driveFileId = fileId;
    var label = "Kaynak: " + RckDriveSource.formatSourceLabel(fileId, RckDriveSource.getStoredLabel());
    pinSourceLabel.textContent = label;
    mainSourceLabel.textContent = label;
  }

  function showScreen(name) {
    setupScreen.hidden = name !== "setup";
    pinScreen.hidden = name !== "pin";
    mainScreen.hidden = name !== "main";
  }

  function buildTabs() {
    tabBar.innerHTML = TABS.map(function (tab) {
      var active = tab.id === state.activeTab ? " active" : "";
      return '<button type="button" class="tab-btn' + active + '" data-tab="' + tab.id + '">' + tab.label + '</button>';
    }).join("");

    tabBar.querySelectorAll(".tab-btn").forEach(function (btn) {
      btn.addEventListener("click", function () {
        state.activeTab = btn.getAttribute("data-tab");
        buildTabs();
        renderContent();
      });
    });
  }

  function renderContent() {
    if (!state.envelope) {
      content.innerHTML = '<div class="empty">Veri yüklenmedi.</div>';
      return;
    }

    var tab = TABS.find(function (t) { return t.id === state.activeTab; }) || TABS[0];
    var viewState = { envelope: state.envelope, query: state.query };
    content.innerHTML = tab.render(viewState);
  }

  function showInlineError(el, message) {
    el.hidden = false;
    el.textContent = message;
  }

  function clearInlineError(el) {
    el.hidden = true;
    el.textContent = "";
  }

  function usesSiteJsonPrimary() {
    return !!String(getConfig().dataUrl || "").trim();
  }

  function isHtmlResponse(text) {
    var sample = String(text || "").trim().slice(0, 64).toLowerCase();
    return sample.indexOf("<!doctype") >= 0 || sample.indexOf("<html") >= 0;
  }

  async function readJsonResponse(response) {
    var text = await response.text();
    if (isHtmlResponse(text)) {
      throw new Error("Sunucu JSON yerine giriş sayfası döndürdü. Apps Script erişimi 'Herkes' olmalı.");
    }
    try {
      return JSON.parse(text);
    } catch (e) {
      throw new Error("Sunucu yanıtı okunamadı.");
    }
  }

  function validatePin(pin) {
    var config = getConfig();
    var expected = String(config.webPin || "").trim();
    if (expected && pin !== expected) {
      throw new Error("Geçersiz PIN.");
    }
  }

  function saveDriveLink(rawLink, errorEl) {
    clearInlineError(errorEl);
    var fileId = RckDriveSource.extractDriveFileId(rawLink);
    if (!fileId) {
      showInlineError(errorEl, "Geçerli bir Drive dosya linki yapıştırın.");
      return false;
    }

    RckDriveSource.saveFileId(fileId, rawLink.trim());
    refreshSourceLabels();
    return true;
  }

  function fetchViaJsonp(url) {
    return new Promise(function (resolve, reject) {
      var callbackName = "rckJsonp_" + Date.now();
      var timeout = setTimeout(function () {
        cleanup();
        reject(new Error("Bağlantı zaman aşımı. Drive sync veya linki kontrol edin."));
      }, 45000);

      function cleanup() {
        clearTimeout(timeout);
        try { delete window[callbackName]; } catch (e) { window[callbackName] = undefined; }
        if (script.parentNode) {
          script.parentNode.removeChild(script);
        }
      }

      window[callbackName] = function (data) {
        cleanup();
        resolve(data);
      };

      var script = document.createElement("script");
      script.src = url + (url.indexOf("?") >= 0 ? "&" : "?") + "callback=" + encodeURIComponent(callbackName);
      script.onerror = function () {
        cleanup();
        reject(new Error("Sunucuya bağlanılamadı. İnternet ve Apps Script URL'sini kontrol edin."));
      };
      document.head.appendChild(script);
    });
  }

  function buildRequestUrl(pin, fileId) {
    var config = getConfig();
    var baseUrl = String(config.appsScriptUrl || "").trim();
    if (!baseUrl) {
      throw new Error("Site yapılandırması eksik (appsScriptUrl).");
    }

    return baseUrl
      + (baseUrl.indexOf("?") >= 0 ? "&" : "?")
      + "pin=" + encodeURIComponent(pin)
      + "&fileId=" + encodeURIComponent(fileId);
  }

  function normalizeApiResponse(raw) {
    if (raw && raw.error) {
      throw new Error(raw.error);
    }
    return WebViewParser.normalizeEnvelope(raw);
  }

  async function fetchEnvelope(pin, fileId) {
    validatePin(pin);
    var config = getConfig();
    var dataUrl = String(config.dataUrl || "").trim();
    var fetchError = null;

    if (dataUrl) {
      try {
        var localResponse = await fetch(dataUrl, { method: "GET", cache: "no-store" });
        if (!localResponse.ok) {
          throw new Error("Site verisi henüz yok. Uygulamada Şimdi Dışa Aktar yapın, 1-2 dk bekleyin.");
        }
        var localRaw = await readJsonResponse(localResponse);
        return WebViewParser.normalizeEnvelope(localRaw);
      } catch (localErr) {
        fetchError = localErr;
        var appsScriptUrl = String(config.appsScriptUrl || "").trim();
        if (!appsScriptUrl || !fileId) {
          throw localErr;
        }
      }
    }

    if (!String(config.appsScriptUrl || "").trim()) {
      throw fetchError || new Error("Veri kaynağı tanımlı değil.");
    }
    if (!fileId) {
      throw fetchError || new Error("Drive dosya linki gerekli (Ayarlar > Kaynak).");
    }

    var requestUrl = buildRequestUrl(pin, fileId);
    try {
      var response = await fetch(requestUrl, { method: "GET", cache: "no-store", redirect: "follow" });
      var raw = await readJsonResponse(response);
      return normalizeApiResponse(raw);
    } catch (fetchErr) {
      try {
        var rawJsonp = await fetchViaJsonp(requestUrl);
        return normalizeApiResponse(rawJsonp);
      } catch (jsonpErr) {
        throw fetchError || fetchErr || jsonpErr;
      }
    }
  }

  async function loadData(pin, keepScreen) {
    clearInlineError(pinError);
    pinSubmit.disabled = true;
    pinSubmit.textContent = "Yükleniyor…";
    try {
      var fileId = resolveDriveFileId();
      if (!fileId) {
        showScreen("setup");
        return;
      }

      var envelope = await fetchEnvelope(pin, fileId);
      state.pin = pin;
      state.envelope = envelope;
      lastUpdated.textContent = "Son güncelleme: " + WebViewParser.formatDateTime(envelope.exportedAt);
      showScreen("main");
      refreshSourceLabels();
      buildTabs();
      renderContent();
    } catch (err) {
      if (!keepScreen) {
        showInlineError(pinError, err.message || "Veri alınamadı.");
      } else {
        content.innerHTML = '<div class="empty">' + WebModules.escapeHtml(err.message || "Yenileme başarısız.") + '</div>';
      }
    } finally {
      pinSubmit.disabled = false;
      pinSubmit.textContent = "Giriş";
    }
  }

  function openSettingsDialog() {
    clearInlineError(settingsError);
    settingsDriveLink.value = RckDriveSource.getStoredLabel() || resolveDriveFileId();
    if (typeof settingsDialog.showModal === "function") {
      settingsDialog.showModal();
    } else {
      settingsDialog.setAttribute("open", "open");
    }
  }

  function closeSettingsDialog() {
    if (typeof settingsDialog.close === "function") {
      settingsDialog.close();
    } else {
      settingsDialog.removeAttribute("open");
    }
  }

  function boot() {
    refreshSourceLabels();
    if (usesSiteJsonPrimary()) {
      showScreen("pin");
      return;
    }
    if (!resolveDriveFileId()) {
      showScreen("setup");
      return;
    }
    showScreen("pin");
  }

  setupSave.addEventListener("click", function () {
    if (!saveDriveLink(driveLinkInput.value, setupError)) {
      return;
    }
    showScreen("pin");
    pinInput.focus();
  });

  driveLinkInput.addEventListener("keydown", function (e) {
    if (e.key === "Enter") {
      setupSave.click();
    }
  });

  pinSubmit.addEventListener("click", function () {
    var pin = (pinInput.value || "").trim();
    if (!pin) {
      showInlineError(pinError, "PIN girin.");
      return;
    }
    loadData(pin, false);
  });

  pinInput.addEventListener("keydown", function (e) {
    if (e.key === "Enter") {
      pinSubmit.click();
    }
  });

  pinChangeSource.addEventListener("click", function () {
    driveLinkInput.value = RckDriveSource.getStoredLabel() || resolveDriveFileId();
    clearInlineError(setupError);
    showScreen("setup");
  });

  refreshBtn.addEventListener("click", function () {
    if (!state.pin) return;
    loadData(state.pin, true);
  });

  settingsBtn.addEventListener("click", openSettingsDialog);

  settingsCancel.addEventListener("click", function () {
    closeSettingsDialog();
  });

  settingsDialog.addEventListener("close", function () {
    clearInlineError(settingsError);
  });

  settingsDialog.querySelector("form").addEventListener("submit", function (e) {
    e.preventDefault();
    if (!saveDriveLink(settingsDriveLink.value, settingsError)) {
      return;
    }
    closeSettingsDialog();
    if (state.pin) {
      loadData(state.pin, true);
    } else {
      showScreen("pin");
    }
  });

  globalSearch.addEventListener("input", function () {
    state.query = globalSearch.value.trim();
    renderContent();
  });

  buildTabs();
  boot();
})();

