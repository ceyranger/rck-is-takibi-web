window.WebUI = (function () {
  function escapeHtml(value) {
    return String(value || "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function cell(value) {
    var text = value === null || value === undefined ? "" : String(value).trim();
    if (!text) {
      return '<span class="cell-empty">—</span>';
    }
    return escapeHtml(text);
  }

  function formatDate(value) {
    if (!value) {
      return '<span class="cell-empty">—</span>';
    }
    try {
      var date = new Date(value);
      if (isNaN(date.getTime())) {
        return cell(value);
      }
      return escapeHtml(new Intl.DateTimeFormat("tr-TR", { dateStyle: "short" }).format(date));
    } catch (err) {
      return cell(value);
    }
  }

  function karotStatusLabel(status) {
    var labels = {
      0: "Karot Alınacak",
      1: "Karot Alındı Sonuç Bekleniyor",
      2: "Karot Alındı Olumlu",
      3: "Karot Alındı Olumsuz"
    };
    var text = labels[status] || String(status || "");
    return '<span class="status-chip karot-' + escapeHtml(String(status)) + '">' + escapeHtml(text) + "</span>";
  }

  function karotRowClass(row) {
    return "karot-row karot-row-" + String(row.status ?? "");
  }

  function statusPill(value) {
    var text = String(value || "").trim();
    if (!text) {
      return '<span class="cell-empty">—</span>';
    }
    var upper = text.toLocaleUpperCase("tr-TR");
    var cls = "pill";
    if (upper === "EVET") cls += " pill-yes";
    else if (upper === "HAYIR") cls += " pill-no";
    else if (upper === "MUAF") cls += " pill-muaf";
    return '<span class="' + cls + '">' + escapeHtml(text) + "</span>";
  }

  function moduleHeader(title, count, subtitle) {
    return '<div class="module-header">' +
      '<div class="module-header-text">' +
      '<h2 class="module-title">' + escapeHtml(title) + "</h2>" +
      (subtitle ? '<p class="module-subtitle">' + escapeHtml(subtitle) + "</p>" : "") +
      "</div>" +
      '<span class="module-count">' + count + " kayıt</span>" +
      "</div>";
  }

  function emptyState(message) {
    return '<div class="empty-state"><p>' + escapeHtml(message) + "</p></div>";
  }

  function renderTable(columns, rows, options) {
    options = options || {};
    var rowClassFn = options.rowClass;
    var tableClass = options.compact ? "data-table data-table-compact" : "data-table";

    var thead = columns.map(function (column) {
      var classes = [column.className || "", column.sticky ? "col-sticky" : ""].filter(Boolean).join(" ");
      return '<th class="' + classes + '">' + escapeHtml(column.label) + "</th>";
    }).join("");

    var tbody = rows.map(function (row) {
      var rowClass = rowClassFn ? rowClassFn(row) : "";
      var cells = columns.map(function (column) {
        var content = column.render ? column.render(row) : cell(row[column.key]);
        var classes = [column.className || "", column.sticky ? "col-sticky" : ""].filter(Boolean).join(" ");
        return '<td class="' + classes + '">' + content + "</td>";
      }).join("");
      return '<tr class="' + rowClass + '">' + cells + "</tr>";
    }).join("");

    return '<div class="data-panel"><div class="table-scroll"><table class="' + tableClass + '">' +
      "<thead><tr>" + thead + "</tr></thead><tbody>" + tbody + "</tbody></table></div></div>";
  }

  function wrapModule(title, count, innerHtml, subtitle) {
    return '<section class="module-section">' + moduleHeader(title, count, subtitle) + innerHtml + "</section>";
  }

  return {
    escapeHtml: escapeHtml,
    cell: cell,
    formatDate: formatDate,
    karotStatusLabel: karotStatusLabel,
    karotRowClass: karotRowClass,
    statusPill: statusPill,
    moduleHeader: moduleHeader,
    emptyState: emptyState,
    renderTable: renderTable,
    wrapModule: wrapModule
  };
})();

window.WebModules = window.WebModules || {};
WebModules.escapeHtml = WebUI.escapeHtml;
