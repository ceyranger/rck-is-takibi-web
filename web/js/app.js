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

  var pinScreen = document.getElementById("pin-screen");
  var mainScreen = document.getElementById("main-screen");
  var pinInput = document.getElementById("pin-input");
  var pinSubmit = document.getElementById("pin-submit");
  var pinError = document.getElementById("pin-error");
  var lastUpdated = document.getElementById("last-updated");
  var refreshBtn = document.getElementById("refresh-btn");
  var globalSearch = document.getElementById("global-search");
  var tabBar = document.getElementById("tab-bar");
  var content = document.getElementById("content");

  var state = {
    pin: "",
    activeTab: TABS[0].id,
    query: "",
    envelope: null
  };

  function getConfig() {
    return window.WEB_VIEWER_CONFIG || {};
  }

  function showScreen(name) {
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

  function showInlineError(message) {
    pinError.hidden = false;
    pinError.textContent = message;
  }

  function clearInlineError() {
    pinError.hidden = true;
    pinError.textContent = "";
  }

  function validatePin(pin) {
    var expected = String(getConfig().webPin || "").trim();
    if (expected && pin !== expected) {
      throw new Error("Geçersiz PIN.");
    }
  }

  async function fetchEnvelope(pin) {
    validatePin(pin);
    var dataUrl = String(getConfig().dataUrl || "").trim();
    if (!dataUrl) {
      throw new Error("Site yapılandırması eksik.");
    }

    var response;
    try {
      response = await fetch(dataUrl, { method: "GET", cache: "no-store" });
    } catch (networkErr) {
      throw new Error("İnternet bağlantısı yok veya siteye ulaşılamıyor.");
    }

    if (!response.ok) {
      throw new Error("Veri henüz yüklenmemiş. Uygulamada Şimdi Dışa Aktar yapın, 2 dk bekleyip tekrar deneyin.");
    }

    var raw;
    try {
      raw = await response.json();
    } catch (parseErr) {
      throw new Error("Veri dosyası okunamadı. Birkaç dakika sonra Yenile deneyin.");
    }

    if (raw && raw.error) {
      throw new Error(raw.error);
    }
    return WebViewParser.normalizeEnvelope(raw);
  }

  async function loadData(pin, keepScreen) {
    clearInlineError();
    pinSubmit.disabled = true;
    pinSubmit.textContent = "Yükleniyor…";
    try {
      var envelope = await fetchEnvelope(pin);
      state.pin = pin;
      state.envelope = envelope;
      lastUpdated.textContent = "Son güncelleme: " + WebViewParser.formatDateTime(envelope.exportedAt);
      showScreen("main");
      buildTabs();
      renderContent();
    } catch (err) {
      if (!keepScreen) {
        showInlineError(err.message || "Veri alınamadı.");
      } else {
        content.innerHTML = '<div class="empty">' + WebModules.escapeHtml(err.message || "Yenileme başarısız.") + '</div>';
      }
    } finally {
      pinSubmit.disabled = false;
      pinSubmit.textContent = "Giriş";
    }
  }

  pinSubmit.addEventListener("click", function () {
    var pin = (pinInput.value || "").trim();
    if (!pin) {
      showInlineError("PIN girin.");
      return;
    }
    loadData(pin, false);
  });

  pinInput.addEventListener("keydown", function (e) {
    if (e.key === "Enter") {
      pinSubmit.click();
    }
  });

  refreshBtn.addEventListener("click", function () {
    if (!state.pin) return;
    loadData(state.pin, true);
  });

  globalSearch.addEventListener("input", function () {
    state.query = globalSearch.value.trim();
    renderContent();
  });

  buildTabs();
  showScreen("pin");
})();
