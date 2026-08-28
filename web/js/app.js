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

  function fetchEnvelopeXHR(dataUrl) {
    return new Promise(function (resolve, reject) {
      var xhr = new XMLHttpRequest();
      xhr.open("GET", dataUrl + (dataUrl.indexOf("?") >= 0 ? "&" : "?") + "t=" + Date.now(), true);
      xhr.onreadystatechange = function () {
        if (xhr.readyState !== 4) return;
        if (xhr.status !== 200) {
          reject(new Error("Veri alınamadı."));
          return;
        }
        try {
          resolve(WebViewParser.normalizeEnvelope(JSON.parse(xhr.responseText)));
        } catch (err) {
          reject(err);
        }
      };
      xhr.onerror = function () {
        reject(new Error("Bağlantı hatası."));
      };
      xhr.send();
    });
  }

  function openWithEnvelope(envelope, pin) {
    state.pin = pin || state.pin;
    state.envelope = envelope;
    lastUpdated.textContent = "Son güncelleme: " + WebViewParser.formatDateTime(envelope.exportedAt);
    buildTabs();
    renderContent();
  }

  async function refreshData() {
    if (!state.pin) return;
    var dataUrl = String(getConfig().dataUrl || "").trim();
    if (!dataUrl) return;
    content.innerHTML = '<div class="empty">Yenileniyor…</div>';
    try {
      var envelope = await fetchEnvelopeXHR(dataUrl);
      openWithEnvelope(envelope, state.pin);
    } catch (err) {
      content.innerHTML = '<div class="empty">' + WebModules.escapeHtml(err.message || "Yenileme başarısız.") + '</div>';
    }
  }

  refreshBtn.addEventListener("click", refreshData);

  globalSearch.addEventListener("input", function () {
    state.query = globalSearch.value.trim();
    renderContent();
  });

  buildTabs();
  window.RckApp = {
    openWithEnvelope: openWithEnvelope,
    refreshData: refreshData
  };
})();
