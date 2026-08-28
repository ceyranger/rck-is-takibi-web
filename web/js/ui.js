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

  function renderSubTabs(moduleKey, tabs, activeId) {
    return '<div class="sub-tab-bar">' + tabs.map(function (tab) {
      var active = tab.id === activeId ? " active" : "";
      return '<button type="button" class="sub-tab-btn' + active + '" data-module="' + escapeHtml(moduleKey) + '" data-subtab="' + escapeHtml(tab.id) + '">' + escapeHtml(tab.label) + "</button>";
    }).join("") + "</div>";
  }

  function renderFilterChips(moduleKey, chips, activeId) {
    return '<div class="filter-chip-bar">' + chips.map(function (chip) {
      var active = chip.id === activeId ? " active" : "";
      return '<button type="button" class="filter-chip' + active + '" data-filter-module="' + escapeHtml(moduleKey) + '" data-filter="' + escapeHtml(chip.id) + '">' + escapeHtml(chip.label) + " (" + chip.count + ")</button>";
    }).join("") + "</div>";
  }

  function splitColumns(leftTitle, leftHtml, rightTitle, rightHtml) {
    return '<div class="split-layout">' +
      '<section class="split-panel"><div class="split-panel-title">' + escapeHtml(leftTitle) + "</div>" + leftHtml + "</section>" +
      '<section class="split-panel"><div class="split-panel-title">' + escapeHtml(rightTitle) + "</div>" + rightHtml + "</section>" +
      "</div>";
  }

  function renderTaskTable(tasks) {
    if (!tasks.length) {
      return emptyState("Kayıt bulunamadı.");
    }
    return renderTable([
      { key: "title", label: "Başlık", sticky: true, className: "text-wrap" },
      { key: "description", label: "Açıklama", className: "text-wrap" },
      {
        label: "Notlar",
        className: "text-wrap",
        render: function (row) {
          return cell((row.notes || []).map(function (n) { return n.text; }).filter(Boolean).join(" · "));
        }
      },
      { label: "Son Tarih", render: function (row) { return formatDate(row.dueDate); } }
    ], tasks);
  }

  function renderActionTable(entries) {
    if (!entries.length) {
      return emptyState("Kayıt bulunamadı.");
    }
    var groups = {};
    entries.forEach(function (entry) {
      var district = String(entry.district || "—").trim() || "—";
      if (!groups[district]) groups[district] = [];
      groups[district].push(entry);
    });
    var districts = Object.keys(groups).sort(function (a, b) { return a.localeCompare(b, "tr"); });
    var rows = [];
    districts.forEach(function (district) {
      groups[district].forEach(function (entry, index) {
        rows.push({
          district: index === 0 ? district : "",
          ownerParcelText: entry.ownerParcelText,
          workText: entry.workText
        });
      });
    });
    return renderTable([
      { key: "district", label: "İlçe", className: "district-cell" },
      { key: "ownerParcelText", label: "Ada Parsel Yapı Sahibi", className: "text-wrap" },
      { key: "workText", label: "Yapılacak İş", className: "text-wrap" }
    ], rows);
  }

  function priorityBadge(label, rank) {
    var cls = rank === 0 ? "priority-acil" : "priority-dikkat";
    return '<span class="priority-badge ' + cls + '">' + escapeHtml(label) + "</span>";
  }

  function renderAcilOzetList(items) {
    if (!items.length) {
      return emptyState("Acil iş özeti bulunamadı.");
    }
    var groups = {};
    items.forEach(function (item) {
      var cat = item.category || "Diğer";
      if (!groups[cat]) groups[cat] = [];
      groups[cat].push(item);
    });
    return Object.keys(groups).sort().map(function (category) {
      var cards = groups[category].map(function (item) {
        return '<article class="ozet-card">' +
          priorityBadge(item.priorityLabel, item.priorityRank) +
          '<p class="text-wrap">' + escapeHtml(item.summary) + "</p>" +
          "</article>";
      }).join("");
      return '<details class="ozet-group" open><summary>' + escapeHtml(category) + " (" + groups[category].length + ")</summary>" + cards + "</details>";
    }).join("");
  }

  function personnelPriorityClass(label) {
    var upper = String(label || "").toLocaleUpperCase("tr-TR");
    if (upper === "KRİTİK" || upper === "KRITIK") return "priority-kritik";
    if (upper === "UYARI") return "priority-uyari";
    if (upper === "ACİL" || upper === "ACIL") return "priority-acil";
    return "";
  }

  function renderPersonnelTaskCard(row) {
    var priorityCls = personnelPriorityClass(row.priorityLabel);
    var priorityHtml = row.priorityLabel
      ? '<span class="personnel-priority ' + priorityCls + '">' + escapeHtml(row.priorityLabel) + "</span>"
      : "";
    var taskText = row.summary || row.fieldLabel || "—";
    return '<article class="personnel-task-card' + (row.isOpen ? "" : " completed") + '">' +
      '<div class="personnel-task-top">' +
      '<span class="personnel-module">' + escapeHtml(row.moduleLabel) + "</span>" +
      priorityHtml +
      '<span class="badge' + (row.isOpen ? " open" : "") + '">' + escapeHtml(row.statusLabel) + "</span>" +
      "</div>" +
      '<h4 class="personnel-task-title text-wrap">' + escapeHtml(taskText) + "</h4>" +
      (row.projectIdentity && row.projectIdentity !== taskText
        ? '<p class="personnel-task-project text-wrap">' + escapeHtml(row.projectIdentity) + "</p>"
        : "") +
      (row.fieldLabel && row.summary && row.fieldLabel !== taskText
        ? '<p class="personnel-task-field text-wrap">' + escapeHtml(row.fieldLabel) + "</p>"
        : "") +
      '<p class="personnel-task-meta">Atama: ' + escapeHtml(row.assignedAtText || "—") + "</p>" +
      "</article>";
  }

  function renderPersonnelGorevBoard(rows) {
    var groups = {};
    rows.forEach(function (row) {
      var key = row.personnelName || "Atanmamış";
      if (!groups[key]) groups[key] = [];
      groups[key].push(row);
    });

    var names = Object.keys(groups).sort(function (a, b) {
      return a.localeCompare(b, "tr");
    });

    return '<div class="personnel-board">' + names.map(function (name) {
      var items = groups[name];
      var initials = name.split(/\s+/).filter(Boolean).slice(0, 2).map(function (part) {
        return part.charAt(0);
      }).join("").toLocaleUpperCase("tr-TR") || "?";

      return '<section class="personnel-group-card">' +
        '<header class="personnel-group-head">' +
        '<span class="personnel-avatar" aria-hidden="true">' + escapeHtml(initials) + "</span>" +
        '<div class="personnel-group-text">' +
        '<h3 class="text-wrap">' + escapeHtml(name) + "</h3>" +
        '<p>' + items.length + " açık görev</p>" +
        "</div>" +
        '<span class="module-count">' + items.length + "</span>" +
        "</header>" +
        '<div class="personnel-task-grid">' +
        items.map(renderPersonnelTaskCard).join("") +
        "</div></section>";
    }).join("") + "</div>";
  }

  function renderProjeOnayGroups(groups, filterKey) {
    var UI = {
      escapeHtml: escapeHtml,
      emptyState: emptyState
    };
    var filteredGroups = groups.map(function (group) {
      var events = (group.events || []).filter(function (ev) {
        if (!filterKey || filterKey === "all") return true;
        return ev.filterKey === filterKey;
      });
      return { group: group, events: events };
    }).filter(function (item) { return item.events.length > 0; });

    if (!filteredGroups.length) {
      return emptyState("Proje onay kaydı bulunamadı.");
    }

    return filteredGroups.map(function (item) {
      var g = item.group;
      var eventsHtml = item.events.map(function (ev) {
        return '<div class="pending-event">' +
          '<div class="pending-event-stripe" style="background:' + escapeHtml(ev.categoryColor || "#94a3b8") + '"></div>' +
          '<div class="pending-event-body">' +
          '<div class="pending-event-top">' +
          '<strong class="text-wrap">' + escapeHtml(ev.statusLabel) + "</strong>" +
          '<span class="pending-date">' + escapeHtml(ev.eventDateText) + "</span>" +
          '<span class="pending-days' + (ev.isOverdue ? " overdue" : "") + '">' + escapeHtml(ev.daysElapsedText) + "</span>" +
          "</div>" +
          '<p class="text-wrap pending-summary">' + escapeHtml(ev.summary) + "</p>" +
          "</div></div>";
      }).join("");

      return '<article class="proje-card' + (g.isOverdue ? " overdue" : "") + '">' +
        '<div class="proje-card-head">' +
        '<h3 class="text-wrap">' + escapeHtml(g.titleText) + "</h3>" +
        '<span class="module-count">' + item.events.length + " olay</span>" +
        "</div>" +
        eventsHtml +
        "</article>";
    }).join("");
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
    wrapModule: wrapModule,
    renderSubTabs: renderSubTabs,
    renderFilterChips: renderFilterChips,
    splitColumns: splitColumns,
    renderTaskTable: renderTaskTable,
    renderActionTable: renderActionTable,
    renderAcilOzetList: renderAcilOzetList,
    renderPersonnelGorevBoard: renderPersonnelGorevBoard,
    renderProjeOnayGroups: renderProjeOnayGroups
  };
})();

window.WebModules = window.WebModules || {};
WebModules.escapeHtml = WebUI.escapeHtml;
