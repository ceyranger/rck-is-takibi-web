window.WebModules = window.WebModules || {};
var escapeHtml = WebModules.escapeHtml;

WebModules.arama = function renderArama(state) {
  var q = state.query;
  if (!q) {
    return '<div class="empty">Arama kutusuna metin yazın.</div>';
  }

  var sections = [
    { title: "Acil İşler", html: WebModules.acil(state) },
    { title: "Proje Onay", html: WebModules.projeOnay(state) },
    { title: "Personel Görevleri", html: WebModules.personel(state) },
    { title: "Karot", html: WebModules.karot(state) },
    { title: "Tadilat", html: WebModules.tadilat(state) },
    { title: "YİBF İş Takibi", html: WebModules.yibfIsTakibi(state) },
    { title: "Tüm Eksikler", html: WebModules.tumEksikler(state) }
  ];

  var parts = sections.map(function (s) {
    if (s.html.indexOf("bulunamadı") >= 0 && s.html.indexOf("empty") >= 0) {
      return "";
    }
    return '<section class="card"><h2>' + escapeHtml(s.title) + '</h2>' + s.html + '</section>';
  }).filter(Boolean);

  return parts.length ? parts.join("") : '<div class="empty">Sonuç bulunamadı.</div>';
};
