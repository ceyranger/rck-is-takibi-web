(function () {
  var TABS = [
    { id: "genel-is-takibi", label: "GENEL İŞ TAKİBİ", roles: ["admin"] },
    { id: "acil-is-ozet", label: "ACİL İŞ ÖZET" },
    { id: "tum-eksikler", label: "TÜM EKSİKLER" },
    { id: "aksiyon", label: "AKSİYON" },
    { id: "eksik-proje", label: "EKSİK PROJE" },
    { id: "karot", label: "KAROT TAKİBİ" },
    { id: "tadilat", label: "TADİLAT TAKİBİ" },
    { id: "proje-takibi", label: "PROJE TAKİBİ" },
    { id: "yibf", label: "YİBF İŞ TAKİBİ" },
    { id: "personel", label: "PERSONEL GÖREV" },
    { id: "arama", label: "ARAMA" }
  ];

  var SESSION_KEY = "rck-web-auth";
  var SESSION_ROLE_KEY = "rck-web-auth-role";
  var SESSION_EXP_KEY = "rck-web-auth-exp";
  var SESSION_TTL_MS = 12 * 60 * 60 * 1000;
  var DEFAULT_USER_TAB = "acil-is-ozet";
  var DEFAULT_ADMIN_TAB = "genel-is-takibi";

  var pinScreen = document.getElementById("pin-screen");
  var mainScreen = document.getElementById("main-screen");
  var pinForm = document.getElementById("pin-form");
  var pinInput = document.getElementById("pin-input");
  var pinError = document.getElementById("pin-error");
  var pinStatus = document.getElementById("pin-status");
  var pinSubmit = document.getElementById("pin-submit");
  var lastUpdated = document.getElementById("last-updated");
  var adminBadge = document.getElementById("admin-badge");
  var refreshBtn = document.getElementById("refresh-btn");
  var globalSearch = document.getElementById("global-search");
  var tabBar = document.getElementById("tab-bar");
  var content = document.getElementById("content");
  var sidebar = document.getElementById("sidebar");
  var sidebarToggle = document.getElementById("sidebar-toggle");
  var sidebarBackdrop = document.getElementById("sidebar-backdrop");

  var TAB_RENDERERS = {
    "genel-is-takibi": WebModules.genelIsTakibi,
    "acil-is-ozet": WebModules.acilIsOzet,
    "tum-eksikler": WebModules.tumEksikler,
    aksiyon: WebModules.aksiyon,
    "eksik-proje": WebModules.eksikProje,
    karot: WebModules.karot,
    tadilat: WebModules.tadilat,
    "proje-takibi": WebModules.projeTakibi,
    yibf: WebModules.yibfIsTakibi,
    personel: WebModules.personel,
    arama: WebModules.arama
  };

  var state = {
    role: null,
    activeTab: DEFAULT_USER_TAB,
    query: "",
    envelope: null,
    loading: false,
    subTabs: {
      karot: "bekleyen",
      aksiyon: "aksiyon"
    },
    filters: {
      projeOnay: "all",
      personel: "all"
    },
    selections: {
      projeTakibiEntryId: null
    }
  };

  function getConfig() {
    return window.WEB_VIEWER_CONFIG || {};
  }

  function normalizePin(value) {
    return String(value || "").replace(/\s+/g, "").trim();
  }

  function getUserPin() {
    return normalizePin(getConfig().webPin || "271179");
  }

  function getAdminPin() {
    return normalizePin(getConfig().adminPin || "0258");
  }

  function resolveRoleFromPin(pin) {
    var normalized = normalizePin(pin);
    if (normalized === getAdminPin()) {
      return "admin";
    }
    if (normalized === getUserPin()) {
      return "user";
    }
    return null;
  }

  function isAdminRole(role) {
    return role === "admin";
  }

  function tabAllowedForRole(tab, role) {
    if (!tab.roles || !tab.roles.length) {
      return true;
    }
    return tab.roles.indexOf(role) >= 0;
  }

  function getVisibleTabs() {
    var role = state.role || "user";
    return TABS.filter(function (tab) {
      return tabAllowedForRole(tab, role);
    });
  }

  function ensureActiveTab() {
    var visible = getVisibleTabs();
    if (!visible.length) {
      return;
    }
    var isVisible = visible.some(function (tab) {
      return tab.id === state.activeTab;
    });
    if (!isVisible) {
      state.activeTab = isAdminRole(state.role) ? DEFAULT_ADMIN_TAB : DEFAULT_USER_TAB;
      if (!visible.some(function (tab) { return tab.id === state.activeTab; })) {
        state.activeTab = visible[0].id;
      }
    }
  }

  function updateAdminBadge() {
    if (!adminBadge) {
      return;
    }
    adminBadge.hidden = !isAdminRole(state.role);
  }

  function resolveDataUrl() {
    var configured = String(getConfig().dataUrl || "").trim();
    if (configured) {
      if (configured.indexOf("http://") === 0 || configured.indexOf("https://") === 0) {
        return configured;
      }
      if (configured.charAt(0) === "/") {
        return configured;
      }
      var basePath = window.location.pathname.replace(/\/[^/]*$/, "/");
      return basePath + configured.replace(/^\.\//, "");
    }
    var basePath = window.location.pathname.replace(/\/[^/]*$/, "/");
    return basePath + "export/web-view-latest.json";
  }

  function saveSession(role) {
    try {
      sessionStorage.setItem(SESSION_KEY, "1");
      sessionStorage.setItem(SESSION_ROLE_KEY, role);
      sessionStorage.setItem(SESSION_EXP_KEY, String(Date.now() + SESSION_TTL_MS));
    } catch (err) {
      /* storage blocked */
    }
  }

  function clearSession() {
    try {
      sessionStorage.removeItem(SESSION_KEY);
      sessionStorage.removeItem(SESSION_ROLE_KEY);
      sessionStorage.removeItem(SESSION_EXP_KEY);
    } catch (err) {
      /* storage blocked */
    }
    state.role = null;
    updateAdminBadge();
  }

  function restoreSessionRole() {
    try {
      var role = sessionStorage.getItem(SESSION_ROLE_KEY);
      if (role === "admin" || role === "user") {
        state.role = role;
        return;
      }
    } catch (err) {
      /* storage blocked */
    }
    state.role = "user";
  }

  function isSessionValid() {
    try {
      if (sessionStorage.getItem(SESSION_KEY) !== "1") {
        return false;
      }
      var expiresAt = parseInt(sessionStorage.getItem(SESSION_EXP_KEY) || "0", 10);
      if (Date.now() >= expiresAt) {
        return false;
      }
      restoreSessionRole();
      return true;
    } catch (err) {
      return false;
    }
  }

  function showPinError(message) {
    pinError.hidden = false;
    pinError.textContent = message;
    pinStatus.hidden = true;
  }

  function showPinStatus(message) {
    pinStatus.hidden = false;
    pinStatus.textContent = message;
    pinError.hidden = true;
  }

  function hidePinMessages() {
    pinError.hidden = true;
    pinStatus.hidden = true;
  }

  function showMainScreen() {
    pinScreen.hidden = true;
    mainScreen.hidden = false;
    mainScreen.style.removeProperty("display");
    updateAdminBadge();
  }

  function showPinScreen() {
    pinScreen.hidden = false;
    mainScreen.hidden = true;
    mainScreen.style.removeProperty("display");
  }

  function setPinLoading(isLoading) {
    state.loading = isLoading;
    pinSubmit.disabled = isLoading;
    pinSubmit.textContent = isLoading ? "Yükleniyor…" : "Giriş";
    refreshBtn.disabled = isLoading;
  }

  function setSidebarOpen(isOpen) {
    document.body.classList.toggle("sidebar-open", isOpen);
    if (sidebarBackdrop) {
      sidebarBackdrop.hidden = !isOpen;
    }
  }

  function closeSidebar() {
    setSidebarOpen(false);
  }

  function buildTabs() {
    ensureActiveTab();
    var visibleTabs = getVisibleTabs();
    tabBar.innerHTML = visibleTabs.map(function (tab) {
      var active = tab.id === state.activeTab ? " active" : "";
      return '<button type="button" class="sidebar-link' + active + '" data-tab="' + tab.id + '">' +
        WebModules.escapeHtml(tab.label) +
        "</button>";
    }).join("");

    tabBar.querySelectorAll(".sidebar-link").forEach(function (btn) {
      btn.addEventListener("click", function () {
        state.activeTab = btn.getAttribute("data-tab");
        buildTabs();
        renderContent();
        closeSidebar();
      });
    });
  }

  function renderContent() {
    if (!state.envelope) {
      content.innerHTML = '<div class="empty">Veri yüklenmedi.</div>';
      return;
    }

    ensureActiveTab();
    var visibleTabs = getVisibleTabs();
    var tab = visibleTabs.find(function (t) { return t.id === state.activeTab; }) || visibleTabs[0];
    var render = tab ? TAB_RENDERERS[tab.id] : null;
    var viewState = {
      envelope: state.envelope,
      query: state.query,
      subTabs: state.subTabs,
      filters: state.filters,
      selections: state.selections,
      role: state.role,
      isAdmin: isAdminRole(state.role)
    };
    if (!render) {
      content.innerHTML = '<div class="empty">Sayfa bulunamadı.</div>';
      return;
    }
    try {
      content.innerHTML = render(viewState);
    } catch (err) {
      console.error("renderContent failed:", err);
      content.innerHTML = '<div class="empty">Sayfa yüklenemedi. Yenile butonunu deneyin.</div>';
    }
  }

  function handleContentClick(event) {
    var entryBtn = event.target.closest("[data-proje-entry]");
    if (entryBtn) {
      state.selections.projeTakibiEntryId = entryBtn.getAttribute("data-proje-entry");
      renderContent();
      return;
    }

    var subBtn = event.target.closest("[data-subtab]");
    if (subBtn) {
      var moduleKey = subBtn.getAttribute("data-module");
      var subTab = subBtn.getAttribute("data-subtab");
      if (moduleKey && subTab) {
        state.subTabs[moduleKey] = subTab;
        renderContent();
      }
      return;
    }

    var filterBtn = event.target.closest("[data-filter]");
    if (filterBtn) {
      var filterModule = filterBtn.getAttribute("data-filter-module");
      var filterValue = filterBtn.getAttribute("data-filter");
      if (filterModule === "projeOnay" && filterValue) {
        state.filters.projeOnay = filterValue;
        renderContent();
        return;
      }
      if (filterModule === "personel" && filterValue) {
        state.filters.personel = filterValue;
        renderContent();
      }
    }
  }

  function fetchEnvelopeXHR(dataUrl) {
    return new Promise(function (resolve, reject) {
      var xhr = new XMLHttpRequest();
      var url = dataUrl + (dataUrl.indexOf("?") >= 0 ? "&" : "?") + "t=" + Date.now();
      xhr.open("GET", url, true);
      xhr.timeout = 120000;
      xhr.onreadystatechange = function () {
        if (xhr.readyState !== 4) return;
        if (xhr.status !== 200) {
          reject(new Error("Veri henüz yok. Bilgisayarda Şimdi Dışa Aktar yapın, 1-2 dk bekleyin."));
          return;
        }
        try {
          var raw = JSON.parse(xhr.responseText);
          if (raw && raw.error) {
            reject(new Error(String(raw.error)));
            return;
          }
          resolve(WebViewParser.normalizeEnvelope(raw));
        } catch (err) {
          reject(new Error("Veri dosyası okunamadı. 1-2 dk sonra tekrar deneyin."));
        }
      };
      xhr.ontimeout = function () {
        reject(new Error("Veri indirme zaman aşımı. Tekrar deneyin."));
      };
      xhr.onerror = function () {
        reject(new Error("Bağlantı hatası. İnterneti kontrol edin."));
      };
      xhr.send();
    });
  }

  function openWithEnvelope(envelope) {
    state.envelope = envelope;
    lastUpdated.textContent = "Son güncelleme: " + WebViewParser.formatDateTime(envelope.exportedAt);
    ensureActiveTab();
    showMainScreen();
    buildTabs();
    renderContent();
  }

  function loadData(options) {
    options = options || {};
    var dataUrl = resolveDataUrl();
    if (!dataUrl) {
      if (!options.silent) {
        showPinError("Site ayarı eksik.");
      }
      return Promise.reject(new Error("Site ayarı eksik."));
    }

    if (!options.silent) {
      setPinLoading(true);
      showPinStatus("Veri indiriliyor…");
    } else {
      content.innerHTML = '<div class="empty">Yenileniyor…</div>';
    }

    return fetchEnvelopeXHR(dataUrl).then(function (envelope) {
      openWithEnvelope(envelope);
      if (!options.silent) {
        hidePinMessages();
      }
      return envelope;
    }).catch(function (err) {
      if (options.silent && isSessionValid()) {
        showPinScreen();
      }
      clearSession();
      if (!options.silent) {
        showPinError(err.message || "Veri yüklenemedi.");
      } else if (state.envelope) {
        content.innerHTML = '<div class="empty">' + WebModules.escapeHtml(err.message || "Yenileme başarısız.") + '</div>';
      }
      throw err;
    }).finally(function () {
      if (!options.silent) {
        setPinLoading(false);
      }
    });
  }

  function handlePinSubmit(event) {
    if (event) event.preventDefault();
    hidePinMessages();

    var pin = normalizePin(pinInput.value);
    if (!pin) {
      showPinError("PIN girin.");
      return;
    }

    var role = resolveRoleFromPin(pin);
    if (!role) {
      showPinError("Geçersiz PIN.");
      pinInput.value = "";
      pinInput.focus();
      return;
    }

    state.role = role;
    state.activeTab = isAdminRole(role) ? DEFAULT_ADMIN_TAB : DEFAULT_USER_TAB;
    saveSession(role);
    loadData({ silent: false }).catch(function () {
      /* error shown */
    });
  }

  function refreshData() {
    if (!isSessionValid()) {
      clearSession();
      showPinScreen();
      showPinError("Oturum süresi doldu. PIN ile tekrar giriş yapın.");
      return;
    }

    loadData({ silent: true }).catch(function () {
      /* error shown in content */
    });
  }

  function tryRestoreSession() {
    if (!isSessionValid()) {
      clearSession();
      return;
    }

    ensureActiveTab();
    showMainScreen();
    content.innerHTML = '<div class="empty">Veri yükleniyor…</div>';
    loadData({ silent: true }).catch(function () {
      /* fallback to pin screen */
    });
  }

  pinForm.addEventListener("submit", handlePinSubmit);
  refreshBtn.addEventListener("click", refreshData);
  content.addEventListener("click", handleContentClick);
  if (sidebarToggle) {
    sidebarToggle.addEventListener("click", function () {
      setSidebarOpen(!document.body.classList.contains("sidebar-open"));
    });
  }
  if (sidebarBackdrop) {
    sidebarBackdrop.addEventListener("click", closeSidebar);
  }

  globalSearch.addEventListener("input", function () {
    state.query = globalSearch.value.trim();
    renderContent();
  });

  buildTabs();
  tryRestoreSession();

  window.RckApp = {
    openWithEnvelope: openWithEnvelope,
    refreshData: refreshData,
    resolveDataUrl: resolveDataUrl
  };
})();
