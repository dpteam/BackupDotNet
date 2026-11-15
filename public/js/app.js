/* ==========================================================================
   Мониторинг бэкапов — веб-интерфейс
   Без фреймворков. Хеш-роутинг, fetch, ES5.
   Бренд берётся из /api/branding (config/branding.json).
   ========================================================================== */
(function () {
  'use strict';

  var app = document.getElementById('app');
  var loginScreen = document.getElementById('login-screen');
  var view = document.getElementById('view');
  var toastEl = document.getElementById('toast');

  var state = {
    summary: null,
    month: null,
    monthValue: null,
    route: 'dashboard',
    companyId: null,
    journal: null,
    settings: null,
    branding: null,
    query: {}
  };

  // ------------------------------------------------------------------ утилиты

  function $(sel, root) { return (root || document).querySelector(sel); }
  function $$(sel, root) { return Array.prototype.slice.call((root || document).querySelectorAll(sel)); }

  function esc(s) {
    if (s === null || s === undefined) return '';
    return String(s)
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
  }

  function pad(n) { return n < 10 ? '0' + n : String(n); }

  function fmtDateTime(iso) {
    if (!iso) return '—';
    var d = new Date(iso);
    if (isNaN(d.getTime())) return '—';
    return pad(d.getDate()) + '.' + pad(d.getMonth() + 1) + '.' + d.getFullYear() +
      ' ' + pad(d.getHours()) + ':' + pad(d.getMinutes());
  }

  function fmtDate(iso) {
    if (!iso) return '—';
    var d = new Date(iso);
    if (isNaN(d.getTime())) return '—';
    return pad(d.getDate()) + '.' + pad(d.getMonth() + 1) + '.' + d.getFullYear();
  }

  function fmtSize(bytes) {
    if (!bytes || bytes <= 0) return '—';
    var units = ['Б', 'КБ', 'МБ', 'ГБ', 'ТБ'];
    var i = 0, b = bytes;
    while (b >= 1024 && i < units.length - 1) { b /= 1024; i++; }
    return (i === 0 ? b.toFixed(0) : b.toFixed(b < 10 ? 2 : 1)) + ' ' + units[i];
  }

  function fmtDuration(sec) {
    if (!sec || sec <= 0) return '—';
    var h = Math.floor(sec / 3600), m = Math.floor((sec % 3600) / 60);
    if (h > 0) return h + ' ч ' + m + ' мин';
    return m + ' мин';
  }

  function ago(iso) {
    if (!iso) return '—';
    var d = new Date(iso);
    if (isNaN(d.getTime())) return '—';
    var diff = (Date.now() - d.getTime()) / 1000;
    if (diff < 0) diff = 0;
    if (diff < 60) return 'только что';
    if (diff < 3600) return Math.floor(diff / 60) + ' мин назад';
    if (diff < 86400) return Math.floor(diff / 3600) + ' ч назад';
    return Math.floor(diff / 86400) + ' дн назад';
  }

  function statusIcon(s) {
    switch (s) {
      case 'OK': return 'fa-check-circle';
      case 'WARN': return 'fa-exclamation-triangle';
      case 'FAIL': return 'fa-times-circle';
      case 'NODATA': return 'fa-question-circle';
      case 'SKIP': return 'fa-minus-circle';
      default: return 'fa-question-circle';
    }
  }

  function badge(status) {
    var s = status || 'NODATA';
    return '<span class="badge s-' + esc(s) + '"><i class="fas ' + statusIcon(s) + '"></i> ' + esc(s) + '</span>';
  }

  function toast(msg, kind) {
    toastEl.textContent = msg;
    toastEl.className = 'toast ' + (kind || '');
    clearTimeout(toast._t);
    toast._t = setTimeout(function () { toastEl.className = 'toast hidden'; }, 3200);
  }

  // ------------------------------------------------------------------ API

  function api(path, opts) {
    opts = opts || {};
    opts.credentials = 'same-origin';
    opts.headers = opts.headers || {};
    if (opts.body && typeof opts.body !== 'string') {
      opts.headers['Content-Type'] = 'application/json';
      opts.body = JSON.stringify(opts.body);
    }
    return fetch(path, opts).then(function (r) {
      if (r.status === 401) {
        showLogin();
        throw new Error('unauthorized');
      }
      return r.text().then(function (t) {
        var data = null;
        try { data = t ? JSON.parse(t) : null; } catch (e) { data = null; }
        if (!r.ok) {
          var msg = (data && data.error) ? data.error : ('HTTP ' + r.status);
          throw new Error(msg);
        }
        return data;
      });
    });
  }

  // ------------------------------------------------------------------ вход

  function showLogin() {
    app.classList.add('hidden');
    loginScreen.classList.remove('hidden');
  }

  function hideLogin() {
    loginScreen.classList.add('hidden');
    app.classList.remove('hidden');
  }

  var loginForm = document.getElementById('login-form');
  loginForm.addEventListener('submit', function (e) {
    e.preventDefault();
    var f = e.target;
    var errEl = document.getElementById('login-error');
    errEl.classList.add('hidden');

    api('/api/login', {
      method: 'POST',
      body: { username: f.username.value, password: f.password.value }
    }).then(function (res) {
      hideLogin();
      f.password.value = '';
      if (res && res.mustChangePassword) {
        toast('Пароль нужно сменить: Настройки → Смена пароля', 'error');
      }
      return api('/api/me').then(function (me) {
        applyPermissions(me);
        route();
      });
    }).catch(function (err) {
      errEl.textContent = err.message;
      errEl.classList.remove('hidden');
    });
  });

  document.getElementById('btn-logout').addEventListener('click', function () {
    api('/api/logout', { method: 'POST' }).then(function () {
      showLogin();
    }).catch(function () { showLogin(); });
  });

  // ------------------------------------------------------------------ тема

  var themeTimer = null;

  function applyTheme(theme, animate) {
    var html = document.documentElement;

    if (animate) {
      // включаем переходы только на время смены темы
      html.classList.add('theme-anim');
      clearTimeout(themeTimer);
      themeTimer = setTimeout(function () {
        html.classList.remove('theme-anim');
      }, 260);
    }

    html.setAttribute('data-theme', theme);
    try { localStorage.setItem('bc-theme', theme); } catch (e) {}
  }

  function toggleTheme() {
    var cur = document.documentElement.getAttribute('data-theme');
    applyTheme(cur === 'dark' ? 'light' : 'dark', true);
  }

  document.getElementById('btn-theme').addEventListener('click', toggleTheme);
  var themeLogin = document.getElementById('theme-toggle-login');
  if (themeLogin) themeLogin.addEventListener('click', function (e) { e.preventDefault(); toggleTheme(); });

  (function initTheme() {
    var saved = null;
    try { saved = localStorage.getItem('bc-theme'); } catch (e) {}
    if (saved) applyTheme(saved, false);
  })();

  // ------------------------------------------------------------------ роутинг

  function parseHash() {
    var h = (location.hash || '#/dashboard').replace(/^#\/?/, '');

    // query-параметры: #/journal?company=demo&date=2026-10-27
    state.query = {};
    var qi = h.indexOf('?');
    if (qi >= 0) {
      var qs = h.substring(qi + 1);
      h = h.substring(0, qi);
      qs.split('&').forEach(function (pair) {
        if (!pair) return;
        var eq = pair.indexOf('=');
        var k = eq >= 0 ? pair.substring(0, eq) : pair;
        var v = eq >= 0 ? pair.substring(eq + 1) : '';
        try { state.query[decodeURIComponent(k)] = decodeURIComponent(v); }
        catch (e) { state.query[k] = v; }
      });
    }

    var parts = h.split('/');
    state.route = parts[0] || 'dashboard';
    state.companyId = parts[1] ? decodeURIComponent(parts[1]) : null;
    state.sub = parts[2] ? decodeURIComponent(parts[2]) : null;
  }

  function setActiveNav() {
    $$('.nav a').forEach(function (a) {
      var nav = a.getAttribute('data-nav');
      a.classList.toggle('active', nav === state.route);
    });
  }

  function route() {
    parseHash();
    setActiveNav();

    if (state.route === 'month') return renderMonth();
    if (state.route === 'uptime') return renderUptime();
    if (state.route === 'journal') return renderJournal();
    if (state.route === 'users') return renderUsers();
    if (state.route === 'deploy') return renderDeploy();
    if (state.route === 'manage') return renderManage();
    if (state.route === 'settings') return renderSettings();
    if (state.route === 'company') {
      if (state.sub === 'edit' || state.companyId === 'new') return renderCompanyForm(state.companyId);
      return renderCompany(state.companyId);
    }
    return renderDashboard();
  }

  window.addEventListener('hashchange', route);

  // ------------------------------------------------------------------ дашборд

  function renderDashboard() {
    view.innerHTML = '<div class="muted">Загрузка…</div>';

    api('/api/summary').then(function (data) {
      state.summary = data;
      var t = data.totals;

      var html = '';
      html += '<h2 class="page-title"><i class="fas fa-tachometer-alt"></i> Обзор</h2>';

      if (data.configErrors && data.configErrors.length) {
        html += '<div class="card" style="border-color:var(--warn)">' +
          '<div class="card-head"><i class="fas fa-exclamation-triangle" style="color:var(--warn)"></i> Проблемы конфигурации</div>' +
          '<div class="card-body"><ul style="margin:0;padding-left:18px">' +
          data.configErrors.map(function (e) { return '<li>' + esc(e) + '</li>'; }).join('') +
          '</ul></div></div>';
      }

      html += '<div class="stats">' +
        stat('ok', 'Успешно', t.ok, 'заданий') +
        stat('warn', 'Внимание', t.warn, 'заданий') +
        stat('fail', 'Ошибки', t.fail, 'заданий') +
        stat('nodata', 'Нет данных', t.nodata, 'заданий') +
        stat('', 'Компаний', t.companies, 'заданий: ' + t.jobs) +
        '</div>';

      html += '<div class="card"><div class="card-head">' +
        '<i class="fas fa-building"></i> Компании' +
        '<span class="spacer"></span>' +
        (canWrite() ? '<button class="btn btn-sm" id="btn-export-companies"><i class="fas fa-download"></i> Экспорт</button> ' : '') +
        (canWrite() ? '<button class="btn btn-sm" id="btn-add-company"><i class="fas fa-plus"></i> Добавить</button> ' : '') +
        (canWrite() ? '<button class="btn btn-sm btn-primary" id="btn-check-all"><i class="fas fa-play"></i> Проверить всё</button>' : '') +
        '</div>';

      if (canWrite() && data.companies.length > 0) {
        html += '<div class="bulk-bar" id="bulk-bar">' +
          '<label class="chk" style="margin:0"><input type="checkbox" id="bulk-all" class="row-check"> Выбрать все</label>' +
          '<span class="spacer"></span>' +
          '<span class="count" id="bulk-count">0</span><span>выбрано</span>' +
          '<button class="btn btn-sm" id="bulk-enable" disabled><i class="fas fa-toggle-on"></i> Включить</button>' +
          '<button class="btn btn-sm" id="bulk-disable" disabled><i class="fas fa-toggle-off"></i> Отключить</button>' +
          (canDelete() ? '<button class="btn btn-sm btn-danger" id="bulk-delete" disabled><i class="fas fa-trash"></i> Удалить</button>' : '') +
          '</div>';
      }

      html += '<div class="card-body">';

      if (!data.companies.length) {
        html += '<p class="muted">Пока нет ни одной компании. Добавьте файл в <code>config/companies/</code>.</p>';
      } else {
        html += '<div class="company-grid">';
        data.companies.forEach(function (c) {
          var s = c.status || 'NODATA';
          html += '<div class="company-card s-' + esc(s) + '" data-company="' + esc(c.id) + '">' +
            '<div class="cc-head">' +
              (canWrite() ? '<input type="checkbox" class="row-check bulk-check" data-id="' + esc(c.id) + '">' : '') +
              '<div style="flex:1">' +
                '<div class="cc-title">' + esc(c.shortName || c.name) + '</div>' +
                '<div class="cc-tags">' + (c.tags || []).map(function (t) { return '<span class="tag">' + esc(t) + '</span>'; }).join('') +
                  (c.enabled === false ? ' <span class="tag" style="color:var(--muted)">выкл</span>' : '') +
                '</div>' +
              '</div>' +
              badge(s) +
            '</div>' +
            '<div class="cc-meta">' +
              '<span><i class="fas fa-tasks"></i> <b>' + c.jobs + '</b> заданий</span>' +
              '<span style="color:var(--ok)"><i class="fas fa-check"></i> ' + c.ok + '</span>' +
              '<span style="color:var(--warn)"><i class="fas fa-exclamation"></i> ' + c.warn + '</span>' +
              '<span style="color:var(--fail)"><i class="fas fa-times"></i> ' + c.fail + '</span>' +
              '<span><i class="fas fa-hdd"></i> ' + fmtSize(c.size) + '</span>' +
              '<span class="muted">' + ago(c.lastUpdate) + '</span>' +
            '</div>' +
          '</div>';
        });
        html += '</div>';
      }

      html += '</div></div>';
      html += '<p class="muted" style="font-size:11.5px">Агент ' + esc(data.agent.version) +
        ' · последний прогон: ' + (data.agent.lastRunAt ? fmtDateTime(data.agent.lastRunAt) : 'ещё не было') +
        ' · обновлено ' + fmtDateTime(data.generatedAt) + '</p>';

      view.innerHTML = html;

      $$('.company-card').forEach(function (el) {
        el.addEventListener('click', function (e) {
          if (e.target.classList.contains('bulk-check')) return;
          location.hash = '#/company/' + encodeURIComponent(el.getAttribute('data-company'));
        });
      });

      var btn = document.getElementById('btn-check-all');
      if (btn) btn.addEventListener('click', runCheckAll);

      var addBtn = document.getElementById('btn-add-company');
      if (addBtn) addBtn.addEventListener('click', function () { location.hash = '#/company/new'; });

      var expBtn = document.getElementById('btn-export-companies');
      if (expBtn) expBtn.addEventListener('click', function () {
        window.location.href = '/api/companies/export';
      });

      setupBulk();

    }).catch(function (err) {
      if (err.message !== 'unauthorized') {
        view.innerHTML = '<div class="card"><div class="card-body">Ошибка: ' + esc(err.message) + '</div></div>';
      }
    });
  }

  // ------------------------------------------------------------------ массовые операции

  function setupBulk() {
    var checks = $$('.bulk-check');
    if (!checks.length) return;

    function selected() {
      return checks.filter(function (c) { return c.checked; })
        .map(function (c) { return c.getAttribute('data-id'); });
    }

    function refresh() {
      var ids = selected();
      var cnt = document.getElementById('bulk-count');
      if (cnt) cnt.textContent = ids.length;

      ['bulk-enable', 'bulk-disable', 'bulk-delete'].forEach(function (id) {
        var b = document.getElementById(id);
        if (b) b.disabled = ids.length === 0;
      });

      var all = document.getElementById('bulk-all');
      if (all) all.checked = ids.length > 0 && ids.length === checks.length;
    }

    checks.forEach(function (c) { c.addEventListener('change', refresh); });

    var all = document.getElementById('bulk-all');
    if (all) all.addEventListener('change', function () {
      checks.forEach(function (c) { c.checked = all.checked; });
      refresh();
    });

    function runBulk(action, confirmText) {
      var ids = selected();
      if (!ids.length) return;
      if (confirmText && !confirm(confirmText.replace('{n}', ids.length))) return;

      api('/api/companies/bulk', { method: 'POST', body: { action: action, ids: ids } })
        .then(function (res) {
          var msg = 'Готово: ' + res.changed;
          if (res.errors && res.errors.length) msg += ' · ошибок: ' + res.errors.length;
          toast(msg, res.errors && res.errors.length ? 'error' : 'ok');
          setTimeout(function () { renderDashboard(); }, 600);
        })
        .catch(function (e) { toast('Ошибка: ' + e.message, 'error'); });
    }

    var e1 = document.getElementById('bulk-enable');
    if (e1) e1.addEventListener('click', function () { runBulk('enable'); });

    var e2 = document.getElementById('bulk-disable');
    if (e2) e2.addEventListener('click', function () { runBulk('disable'); });

    var e3 = document.getElementById('bulk-delete');
    if (e3) e3.addEventListener('click', function () {
      runBulk('delete', 'Удалить {n} компаний?\n\nФайлы уедут в _archive, история останется.');
    });

    refresh();
  }

  function stat(kind, label, value, hint) {
    return '<div class="stat ' + kind + '">' +
      '<div class="label">' + esc(label) + '</div>' +
      '<div class="value">' + (value || 0) + '</div>' +
      '<div class="hint">' + esc(hint || '') + '</div>' +
      '</div>';
  }

  function runCheckAll() {
    api('/api/check', { method: 'POST', body: {} }).then(function () {
      toast('Проверка запущена — результаты через несколько секунд', 'ok');
      setTimeout(function () { route(); }, 4000);
    }).catch(function (e) { toast('Ошибка: ' + e.message, 'error'); });
  }

  // ------------------------------------------------------------------ таблица месяца

  function currentMonth() {
    var d = new Date();
    return d.getFullYear() + '-' + pad(d.getMonth() + 1);
  }

  function renderMonth() {
    if (!state.monthValue) state.monthValue = currentMonth();

    var html = '<h2 class="page-title"><i class="fas fa-table"></i> Календарь бэкапов</h2>';
    html += '<div class="month-toolbar">' +
      '<input type="month" id="month-input" value="' + esc(state.monthValue) + '">' +
      '<button class="btn btn-sm" id="btn-month-prev"><i class="fas fa-chevron-left"></i></button>' +
      '<button class="btn btn-sm" id="btn-month-next"><i class="fas fa-chevron-right"></i></button>' +
      '<button class="btn btn-sm" id="btn-export"><i class="fas fa-file-csv"></i> CSV</button>' +
      '<button class="btn btn-sm" id="btn-print"><i class="fas fa-print"></i> Печать</button>' +
      '<span class="spacer" style="margin-left:auto"></span>' +
      '</div>';
    html += '<div class="card"><div class="card-body" style="padding:0"><div class="table-wrap" id="month-table"><div style="padding:20px" class="muted">Загрузка…</div></div></div></div>';
    html += '<div class="legend">' +
      legend('var(--ok-bg)', 'OK') +
      legend('var(--warn-bg)', 'WARN') +
      legend('var(--fail-bg)', 'FAIL') +
      legend('var(--nodata-bg)', 'NODATA') +
      '<span class="muted">· пустая ячейка = проверок не было</span>' +
      '</div>';

    view.innerHTML = html;

    document.getElementById('month-input').addEventListener('change', function (e) {
      state.monthValue = e.target.value || currentMonth();
      renderMonth();
    });

    document.getElementById('btn-month-prev').addEventListener('click', function () { shiftMonth(-1); });
    document.getElementById('btn-month-next').addEventListener('click', function () { shiftMonth(1); });
    document.getElementById('btn-print').addEventListener('click', function () { window.print(); });
    document.getElementById('btn-export').addEventListener('click', function () {
      window.location.href = '/api/export?month=' + encodeURIComponent(state.monthValue);
    });

    loadMonth();
  }

  function legend(color, label) {
    return '<span><i style="background:' + color + '"></i>' + esc(label) + '</span>';
  }

  // ------------------------------------------------------------------ детали дня

  var dayState = { companyId: null, date: null, prev: null, next: null };

  /**
   * Клик по ячейке календаря → что было в этот день и почему.
   * Проблемные задания сверху, у каждого — сообщение (причина).
   * Стрелки ← → переключают день, клик по строке — история задания.
   */
  function showDayDetails(companyId, dateStr) {
    var existing = document.getElementById('day-modal');
    if (existing) existing.parentNode.removeChild(existing);

    dayState = { companyId: companyId, date: dateStr, prev: null, next: null };

    var html = '<div class="modal-backdrop" id="day-modal">' +
      '<div class="modal modal-wide">' +
      '<div class="day-head">' +
        '<button class="btn btn-sm day-nav" id="day-prev" title="Предыдущий день">' +
          '<i class="fas fa-chevron-left"></i></button>' +
        '<h3><i class="fas fa-search"></i> <span id="day-title">Загрузка…</span></h3>' +
        '<button class="btn btn-sm day-nav" id="day-next" title="Следующий день">' +
          '<i class="fas fa-chevron-right"></i></button>' +
      '</div>' +
      '<div id="day-body"><div class="muted" style="padding:10px 0">Загрузка…</div></div>' +
      '<div class="modal-actions" style="margin-top:14px">' +
      '<button class="btn" id="day-close">Закрыть</button>' +
      '<button class="btn btn-primary" id="day-journal"><i class="fas fa-list-ul"></i> Открыть в журнале</button>' +
      '</div></div></div>';

    var div = document.createElement('div');
    div.innerHTML = html;
    document.body.appendChild(div.firstChild);

    document.getElementById('day-close').addEventListener('click', closeDayModal);
    document.getElementById('day-modal').addEventListener('click', function (e) {
      if (e.target.id === 'day-modal') closeDayModal();
    });

    document.getElementById('day-prev').addEventListener('click', function () { navigateDay(-1); });
    document.getElementById('day-next').addEventListener('click', function () { navigateDay(1); });

    document.getElementById('day-journal').addEventListener('click', function () {
      closeDayModal();
      location.hash = '#/journal?company=' + encodeURIComponent(dayState.companyId) +
        '&date=' + encodeURIComponent(dayState.date);
    });

    loadDay();

    // клавиши внутри окна: ← → листают дни, Esc закрывает
    document.addEventListener('keydown', dayKeys);
  }

  function dayKeys(e) {
    if (!document.getElementById('day-modal')) {
      document.removeEventListener('keydown', dayKeys);
      return;
    }
    if (e.key === 'ArrowLeft') { e.preventDefault(); navigateDay(-1); }
    else if (e.key === 'ArrowRight') { e.preventDefault(); navigateDay(1); }
    else if (e.key === 'Escape') { closeDayModal(); }
  }

  /** Переход на соседний день. Стрелка активна только если там есть данные. */
  function navigateDay(delta) {
    var target = delta < 0 ? dayState.prev : dayState.next;
    if (!target || !target.date) return;

    dayState.date = target.date;
    loadDay();
  }

  function loadDay() {
    var body = document.getElementById('day-body');
    if (body) body.innerHTML = '<div class="muted" style="padding:10px 0">Загрузка…</div>';

    // стрелки пока неактивны, пока не знаем соседей
    setDayArrow('day-prev', null);
    setDayArrow('day-next', null);

    api('/api/day?company=' + encodeURIComponent(dayState.companyId) +
        '&date=' + encodeURIComponent(dayState.date)).then(function (data) {

      dayState.prev = data.prev;
      dayState.next = data.next;

      setDayArrow('day-prev', data.prev);
      setDayArrow('day-next', data.next);

      var dt = new Date(data.date + 'T12:00:00');
      var human = dt.toLocaleDateString('ru-RU', {
        day: 'numeric', month: 'long', year: 'numeric', weekday: 'long'
      });

      document.getElementById('day-title').textContent = data.companyName + ' · ' + human;

      var c = data.counts;
      var parts = [];
      if (c.fail) parts.push('<span style="color:var(--fail)"><b>' + c.fail + '</b> FAIL</span>');
      if (c.warn) parts.push('<span style="color:var(--warn)"><b>' + c.warn + '</b> WARN</span>');
      if (c.nodata) parts.push('<span style="color:var(--muted)"><b>' + c.nodata + '</b> NODATA</span>');
      if (c.ok) parts.push('<span style="color:var(--ok)"><b>' + c.ok + '</b> OK</span>');

      var html = '';

      if (!data.jobs.length) {
        html = '<div class="muted" style="padding:12px 0">В этот день проверок не было.</div>';
      } else {
        html += '<div class="day-summary">' + badge(data.status) +
          ' <span class="muted">·</span> ' + parts.join(' <span class="muted">·</span> ') + '</div>';

        html += '<div class="table-wrap"><table class="journal-table day-table"><thead><tr>' +
          '<th>Задание</th><th>Статус</th><th>Время</th><th>Причина</th>' +
          '</tr></thead><tbody>';

        data.jobs.forEach(function (j) {
          var archived = j.current === false;

          html += '<tr class="day-row' + (archived ? ' day-archived' : '') + '"' +
            (archived ? '' :
              ' data-day-job="' + esc(j.job) + '" title="Показать историю задания"') + '>' +
            '<td><b>' + esc(j.job) + '</b>' +
              (archived
                ? '<div class="vm-disk">архивное · задания больше нет</div>'
                : (j.runs > 1 ? '<div class="vm-disk">прогонов за день: ' + j.runs + '</div>' : '')) +
            '</td>' +
            '<td>' + badge(j.status) + '</td>' +
            '<td style="white-space:nowrap">' + fmtTime(j.ts) + '</td>' +
            '<td class="msg">' + esc(j.message || '—') + '</td>' +
            '</tr>';
        });

        html += '</tbody></table></div>';
        html += '<p class="muted" style="font-size:11.5px;margin:10px 0 0">' +
          'Нажмите на задание, чтобы увидеть его историю за месяц.</p>';
      }

      var box = document.getElementById('day-body');
      if (box) box.innerHTML = html;

      $$('#day-body .day-row[data-day-job]').forEach(function (row) {
        row.addEventListener('click', function () {
          showJobHistory(dayState.companyId, row.getAttribute('data-day-job'), dayState.date);
        });
      });

    }).catch(function (err) {
      var box = document.getElementById('day-body');
      if (box) box.innerHTML = '<div class="muted">Ошибка: ' + esc(err.message) + '</div>';
    });
  }

  /** Включает/выключает стрелку и подсказывает статус соседнего дня. */
  function setDayArrow(id, info) {
    var btn = document.getElementById(id);
    if (!btn) return;

    if (info && info.date) {
      btn.disabled = false;
      btn.title = info.date + ' · ' + info.status + ' (нажмите или ←/→)';
      btn.classList.remove('day-nav-off');
    } else {
      btn.disabled = true;
      btn.title = 'Нет данных';
      btn.classList.add('day-nav-off');
    }
  }

  function closeDayModal() {
    document.removeEventListener('keydown', dayKeys);
    var m = document.getElementById('day-modal');
    if (m) m.parentNode.removeChild(m);
    var h = document.getElementById('jobhist-modal');
    if (h) h.parentNode.removeChild(h);
  }

  // ------------------------------------------------------------------ история задания

  /**
   * История одного задания за месяц вокруг выбранного дня.
   * Помогает понять: проблема разовая или тянется неделями.
   */
  function showJobHistory(companyId, job, dateStr, periodDays) {
    var days = periodDays || 30;

    var html = '<div class="modal-backdrop" id="jobhist-modal" style="z-index:400">' +
      '<div class="modal modal-wide">' +
      '<h3><i class="fas fa-history"></i> ' + esc(job) + '</h3>' +
      '<p class="muted" style="font-size:11.5px;margin-top:0">' +
        'История задания за ' + days + ' дней · <span id="jh-range"></span></p>' +
      '<div id="jh-body"><div class="muted" style="padding:10px 0">Загрузка…</div></div>' +
      '<div class="modal-actions" style="margin-top:14px">' +
      '<button class="btn" id="jh-close">Назад</button>' +
      '</div></div></div>';

    var div = document.createElement('div');
    div.innerHTML = html;
    document.body.appendChild(div.firstChild);

    document.getElementById('jh-close').addEventListener('click', function () {
      var m = document.getElementById('jobhist-modal');
      if (m) m.parentNode.removeChild(m);
    });

    api('/api/job-history?company=' + encodeURIComponent(companyId) +
        '&job=' + encodeURIComponent(job) +
        '&date=' + encodeURIComponent(dateStr) +
        '&days=' + days).then(function (data) {

      var range = document.getElementById('jh-range');
      if (range) range.textContent = data.from + ' — ' + data.to;

      var c = data.counts;
      var parts = [];
      if (c.fail) parts.push('<span style="color:var(--fail)"><b>' + c.fail + '</b> FAIL</span>');
      if (c.warn) parts.push('<span style="color:var(--warn)"><b>' + c.warn + '</b> WARN</span>');
      if (c.nodata) parts.push('<span style="color:var(--muted)"><b>' + c.nodata + '</b> NODATA</span>');
      if (c.ok) parts.push('<span style="color:var(--ok)"><b>' + c.ok + '</b> OK</span>');
      if (c.missing) parts.push('<span class="muted">' + c.missing + ' без данных</span>');

      var body = '<div class="day-summary">' + parts.join(' <span class="muted">·</span> ') + '</div>';

      body += '<div class="table-wrap" style="max-height:46vh;overflow:auto">' +
        '<table class="journal-table day-table"><thead><tr>' +
        '<th>День</th><th>Статус</th><th>Прогонов</th><th>Причина</th>' +
        '</tr></thead><tbody>';

      data.items.forEach(function (d) {
        var cls = d.status ? 'c-' + d.status : '';
        body += '<tr' + (d.status ? '' : ' class="day-archived"') + '>' +
          '<td style="white-space:nowrap">' +
            (d.date === dateStr ? '<b>' + esc(d.date) + '</b>' : esc(d.date)) + '</td>' +
          '<td>' + (d.status ? badge(d.status) : '<span class="muted">—</span>') + '</td>' +
          '<td>' + (d.runs || '—') + '</td>' +
          '<td class="msg">' + esc(d.message || '—') + '</td>' +
          '</tr>';
      });

      body += '</tbody></table></div>';

      var box = document.getElementById('jh-body');
      if (box) box.innerHTML = body;

    }).catch(function (err) {
      var box = document.getElementById('jh-body');
      if (box) box.innerHTML = '<div class="muted">Ошибка: ' + esc(err.message) + '</div>';
    });
  }

  /** Короткое время ЧЧ:ММ — для таблицы деталей дня. */
  function fmtTime(iso) {
    if (!iso) return '—';
    var d = new Date(iso);
    if (isNaN(d.getTime())) return '—';
    return pad(d.getHours()) + ':' + pad(d.getMinutes());
  }

  function shiftMonth(delta) {
    var p = state.monthValue.split('-');
    var d = new Date(parseInt(p[0], 10), parseInt(p[1], 10) - 1 + delta, 1);
    state.monthValue = d.getFullYear() + '-' + pad(d.getMonth() + 1);
    renderMonth();
  }

  function loadMonth() {
    var holder = document.getElementById('month-table');
    api('/api/month?month=' + encodeURIComponent(state.monthValue)).then(function (data) {
      state.month = data;
      var days = data.days;
      var month = data.month;

      var html = '<table class="month-table"><thead><tr>' +
        '<th class="col-company">Компания</th>';

      for (var d = 1; d <= days; d++) {
        var dt = new Date(month + '-' + pad(d) + 'T12:00:00');
        var wd = dt.getDay();
        var weekend = (wd === 0 || wd === 6) ? ' weekend' : '';
        html += '<th class="' + weekend + '" title="' + esc(dt.toLocaleDateString('ru-RU')) + '">' + d + '</th>';
      }
      html += '<th class="col-total">Итог</th></tr></thead><tbody>';

      if (!data.companies.length) {
        html += '<tr><td colspan="' + (days + 2) + '" style="padding:18px" class="muted">Нет компаний</td></tr>';
      }

      data.companies.forEach(function (c) {
        html += '<tr><td class="col-company"><a href="#/company/' + encodeURIComponent(c.id) + '">' +
          esc(c.shortName || c.name) + '</a></td>';
        for (var d = 1; d <= days; d++) {
          var st = c.days[d];
          var dateStr = month + '-' + pad(d);

          if (st) {
            // клик по ячейке → детали дня: что именно упало и почему
            html += '<td class="cell c-' + esc(st) + ' cell-click" ' +
              'data-day-company="' + esc(c.id) + '" data-day-date="' + esc(dateStr) + '" ' +
              'title="' + esc(c.name) + ' · ' + d + '.' + month.split('-')[1] + ': ' + esc(st) +
              ' — нажмите, чтобы узнать причину">' +
              '<span>' + esc(st.charAt(0)) + '</span></td>';
          } else {
            html += '<td class="cell c-none"><span>·</span></td>';
          }
        }
        html += '<td class="col-total">' + badge(c.worst) + '</td></tr>';
      });

      html += '</tbody></table>';
      holder.innerHTML = html;

      $$('.cell-click').forEach(function (cell) {
        cell.addEventListener('click', function () {
          showDayDetails(cell.getAttribute('data-day-company'), cell.getAttribute('data-day-date'));
        });
      });

    }).catch(function (err) {
      holder.innerHTML = '<div style="padding:20px" class="muted">Ошибка: ' + esc(err.message) + '</div>';
    });
  }

  // ------------------------------------------------------------------ доступность

  function renderUptime() {
    var hours = state.uptimeHours || 24;

    var html = '<h2 class="page-title"><i class="fas fa-heartbeat"></i> Доступность сервисов</h2>';
    html += '<div class="month-toolbar">' +
      '<span class="muted">Период:</span>' +
      [24, 24 * 7, 24 * 30].map(function (h) {
        return '<button class="btn btn-sm' + (hours === h ? ' btn-primary' : '') +
          '" data-hours="' + h + '">' + (h === 24 ? '24 ч' : h === 168 ? '7 дней' : '30 дней') + '</button>';
      }).join('') +
      '<span class="spacer" style="margin-left:auto"></span>' +
      '<button class="btn btn-sm" id="btn-uptime-refresh"><i class="fas fa-sync-alt"></i> Обновить</button>' +
      '</div>';

    html += '<div id="uptime-box"><div class="muted">Загрузка…</div></div>';

    view.innerHTML = html;

    $$('[data-hours]').forEach(function (b) {
      b.addEventListener('click', function () {
        state.uptimeHours = parseInt(b.getAttribute('data-hours'), 10);
        renderUptime();
      });
    });

    document.getElementById('btn-uptime-refresh').addEventListener('click', renderUptime);

    loadUptime(hours);
  }

  function loadUptime(hours) {
    var box = document.getElementById('uptime-box');

    api('/api/uptime?hours=' + hours).then(function (data) {
      var items = data.items || [];

      if (!items.length) {
        box.innerHTML = '<div class="card"><div class="card-body">' +
          '<p class="muted" style="margin:0">Проверок доступности пока нет.</p>' +
          '<p class="muted" style="font-size:11.5px;margin:8px 0 0">' +
          'Добавьте источник типа <code>http</code>, <code>tcp</code>, <code>ping</code>, ' +
          '<code>dns</code> или <code>push</code> в карточке компании.</p>' +
          '</div></div>';
        return;
      }

      var html = '';
      items.forEach(function (it) {
        html += renderUptimeCard(it, hours);
      });
      box.innerHTML = html;

    }).catch(function (err) {
      box.innerHTML = '<div class="card"><div class="card-body">Ошибка: ' + esc(err.message) + '</div></div>';
    });
  }

  function renderUptimeCard(it, hours) {
    var s = it.summary || {};
    var uptime = s.uptime || 0;
    var uptimeClass = uptime >= 99.5 ? 'good' : (uptime >= 95 ? 'mid' : 'bad');

    var h = '<div class="card uptime-card">';
    h += '<div class="card-head">' +
      '<i class="fas ' + statusIcon(it.status) + '" style="color:var(--' +
        (it.status === 'OK' ? 'ok' : it.status === 'WARN' ? 'warn' : 'fail') + ')"></i> ' +
      esc(it.job) +
      '<span class="tag" style="margin-left:6px">' + esc(it.kind) + '</span>' +
      '<span class="spacer"></span>' +
      '<span class="uptime-pct ' + uptimeClass + '">' + uptime.toFixed(2) + '%</span>' +
      badge(it.status) +
      '</div>';

    h += '<div class="card-body">';

    // полоска истории
    h += '<div class="hbars" title="' + esc(hours + ' ч') + '">';
    (it.series || []).forEach(function (cell) {
      if (!cell) {
        h += '<i class="hb-none" title="нет данных"></i>';
        return;
      }
      var cls = cell.status === 'OK' ? 'hb-ok' : cell.status === 'WARN' ? 'hb-warn' : 'hb-fail';
      var tip = cell.hour.replace('T', ' ') + ':00 · ' + cell.status +
        ' · ' + (cell.avgMs || 0) + ' мс' +
        ' (ok ' + cell.ok + ', warn ' + cell.warn + ', fail ' + cell.fail + ')';
      h += '<i class="' + cls + '" title="' + esc(tip) + '"></i>';
    });
    h += '</div>';

    h += '<div class="uptime-meta">' +
      '<span><i class="fas fa-clock"></i> отклик: <b>' + (s.avgMs || 0) + ' мс</b></span>' +
      '<span><i class="fas fa-check"></i> проверок: <b>' + (s.checks || 0) + '</b></span>' +
      '<span style="color:var(--fail)"><i class="fas fa-times"></i> сбоев: <b>' + (s.fail || 0) + '</b></span>' +
      '<span class="muted"><i class="fas fa-link"></i> ' + esc(it.target || '') + '</span>' +
      '</div>';

    if (it.message) {
      h += '<div class="uptime-msg ' + (it.status === 'OK' ? 'ok' : 'bad') + '">' + esc(it.message) + '</div>';
    }

    h += '</div></div>';
    return h;
  }

  // ------------------------------------------------------------------ журнал

  function renderJournal() {
    var q = state.query || {};
    var presetCompany = q.company || '';
    var presetDate = q.date || '';
    var presetStatus = (q.status || '').toUpperCase();

    view.innerHTML = '<h2 class="page-title"><i class="fas fa-list-ul"></i> Журнал проверок</h2>' +
      '<div class="card"><div class="card-head"><i class="fas fa-filter"></i> Фильтр' +
      '<span class="spacer"></span>' +
      '<button class="btn btn-sm" id="btn-journal-refresh"><i class="fas fa-sync-alt"></i> Обновить</button>' +
      '</div><div class="card-body">' +
      '<div class="form-grid">' +
        '<label class="fld"><span>Компания</span>' +
          '<select id="jf-company"><option value="">все</option></select></label>' +
        '<label class="fld"><span>Дата</span>' +
          '<input type="date" id="jf-date" value="' + esc(presetDate) + '"></label>' +
        '<label class="fld"><span>Статус</span>' +
          '<select id="jf-status">' +
            ['', 'FAIL', 'WARN', 'NODATA', 'OK'].map(function (s) {
              return '<option value="' + s + '"' + (presetStatus === s ? ' selected' : '') + '>' +
                (s || 'все') + '</option>';
            }).join('') +
          '</select></label>' +
      '</div>' +
      '<input type="text" id="journal-filter" placeholder="Поиск по заданию или сообщению…" ' +
      'style="width:100%;margin-top:10px;padding:7px 10px;border:1px solid var(--border);' +
      'border-radius:6px;background:var(--surface);color:var(--ink)">' +
      '</div></div>' +
      '<div class="card"><div class="card-body" style="padding:0"><div class="table-wrap" id="journal-box">' +
      '<div style="padding:20px" class="muted">Загрузка…</div></div></div></div>';

    // список компаний для выпадающего списка
    api('/api/companies').then(function (res) {
      var sel = document.getElementById('jf-company');
      if (!sel) return;
      (res.companies || []).forEach(function (c) {
        var o = document.createElement('option');
        o.value = c.id;
        o.textContent = c.name;
        sel.appendChild(o);
      });

      // значение ставим ПОСЛЕ заполнения — иначе браузер сбросит его на первую опцию
      sel.value = presetCompany;

      // и только теперь грузим данные: иначе фильтр уйдёт пустым
      loadJournal();
    }).catch(function () {
      loadJournal();
    });

    document.getElementById('btn-journal-refresh').addEventListener('click', loadJournal);
    document.getElementById('journal-filter').addEventListener('input', function () { renderJournalRows(); });

    ['jf-company', 'jf-date', 'jf-status'].forEach(function (id) {
      document.getElementById(id).addEventListener('change', loadJournal);
    });
  }

  function loadJournal() {
    var company = (document.getElementById('jf-company') || {}).value || '';
    var date = (document.getElementById('jf-date') || {}).value || '';
    var status = (document.getElementById('jf-status') || {}).value || '';

    var url = '/api/journal?limit=300';
    if (company) url += '&company=' + encodeURIComponent(company);
    if (date) url += '&date=' + encodeURIComponent(date);
    if (status) url += '&status=' + encodeURIComponent(status);

    api(url).then(function (data) {
      state.journal = data.items || [];
      renderJournalRows();
    }).catch(function (err) {
      var box = document.getElementById('journal-box');
      if (box) box.innerHTML = '<div style="padding:20px" class="muted">Ошибка: ' + esc(err.message) + '</div>';
    });
  }

  function renderJournalRows() {
    var box = document.getElementById('journal-box');
    if (!box || !state.journal) return;

    var filterEl = document.getElementById('journal-filter');
    var q = filterEl ? filterEl.value.toLowerCase() : '';

    var items = state.journal.filter(function (r) {
      if (!q) return true;
      return (r.job || '').toLowerCase().indexOf(q) >= 0 ||
        (r.message || '').toLowerCase().indexOf(q) >= 0 ||
        (r.companyName || '').toLowerCase().indexOf(q) >= 0;
    });

    if (!items.length) {
      box.innerHTML = '<div style="padding:20px" class="muted">Записей нет</div>';
      return;
    }

    var html = '<table class="journal-table"><thead><tr>' +
      '<th>Время</th><th>Компания</th><th>Задание</th><th>Статус</th>' +
      '<th>Последний успех</th><th>Размер</th><th>Длит.</th><th>Сообщение</th>' +
      '</tr></thead><tbody>';

    items.forEach(function (r) {
      html += '<tr>' +
        '<td style="white-space:nowrap">' + fmtDateTime(r.ts) + '</td>' +
        '<td><a href="#/company/' + encodeURIComponent(r.company) + '">' + esc(r.companyName || r.company) + '</a></td>' +
        '<td>' + esc(r.job) + '</td>' +
        '<td>' + badge(r.status) + '</td>' +
        '<td style="white-space:nowrap">' + fmtDateTime(r.lastSuccess) + '</td>' +
        '<td style="white-space:nowrap">' + fmtSize(r.size) + '</td>' +
        '<td style="white-space:nowrap">' + fmtDuration(r.duration) + '</td>' +
        '<td class="msg">' + esc(r.message || '') + '</td>' +
        '</tr>';
    });

    html += '</tbody></table>';
    box.innerHTML = html;
  }

  // ------------------------------------------------------------------ компания

  function renderCompany(id) {
    if (!id) { location.hash = '#/dashboard'; return; }
    view.innerHTML = '<div class="muted">Загрузка…</div>';

    api('/api/company/' + encodeURIComponent(id)).then(function (c) {
      var html = '';
      html += '<h2 class="page-title"><i class="fas fa-building"></i> ' + esc(c.name) + '</h2>';

      html += '<div class="card"><div class="card-body">' +
        '<div class="detail-head">' +
          '<div style="flex:1">' +
            '<div class="dh-title">' + esc(c.name) + '</div>' +
            '<div class="dh-sub">' + (c.tags || []).map(function (t) { return '<span class="tag">' + esc(t) + '</span>'; }).join('') +
              ' · ID: <code>' + esc(c.id) + '</code> · Zabbix: <code>' + esc(c.zabbixHost) + '</code>' +
              (c.remoteAgent ? ' · агент: <code>' + esc(c.remoteAgent) + '</code>' : '') + '</div>' +
          '</div>' +
          (canWrite() ? '<button class="btn btn-sm" id="btn-rename-company"><i class="fas fa-i-cursor"></i> ID</button> ' : '') +
          (canWrite() ? '<button class="btn btn-sm" id="btn-edit-company"><i class="fas fa-pen"></i> Изменить</button> ' : '') +
          (canWrite() ? '<button class="btn btn-primary btn-sm" id="btn-check-company"><i class="fas fa-play"></i> Проверить</button>' : '') +
        '</div>' +
      '</div></div>';

      // вкладки
      var tab = state.companyTab || 'overview';
      html += '<div class="tabs">' +
        ['overview', 'vms', 'hardware', 'inventory', 'diagnostics', 'json', 'history'].map(function (t) {
          var label = {
            overview: 'Обзор', vms: 'ВМ', hardware: 'Железо',
            inventory: 'Инвентарь', diagnostics: 'Диагностика',
            json: 'JSON', history: 'История'
          }[t];
          var icon = {
            overview: 'fa-info-circle', vms: 'fa-server', hardware: 'fa-microchip',
            inventory: 'fa-clipboard-list', diagnostics: 'fa-stethoscope',
            json: 'fa-code', history: 'fa-history'
          }[t];
          return '<button class="tab' + (tab === t ? ' active' : '') + '" data-tab="' + t + '">' +
            '<i class="fas ' + icon + '"></i> ' + label + '</button>';
        }).join('') +
        '</div>';

      html += '<div id="company-tab-content"></div>';

      view.innerHTML = html;

      $$('[data-tab]').forEach(function (b) {
        b.addEventListener('click', function () {
          state.companyTab = b.getAttribute('data-tab');
          renderCompany(c.id);
        });
      });

      var tabContent = document.getElementById('company-tab-content');
      if (tab === 'json') renderCompanyJson(c.id, tabContent);
      else if (tab === 'history') renderCompanyHistory(c.id, tabContent);
      else if (tab === 'hardware') renderCompanyHardware(c, tabContent);
      else if (tab === 'inventory') renderCompanyInventory(c, tabContent);
      else if (tab === 'diagnostics') renderCompanyDiagnostics(c, tabContent);
      else if (tab === 'vms') renderCompanyVms(c, tabContent);
      else renderCompanyOverview(c, tabContent);

      var btn = document.getElementById('btn-check-company');
      if (btn) btn.addEventListener('click', function () {
        api('/api/check', { method: 'POST', body: { company: c.id } }).then(function () {
          toast('Проверка запущена', 'ok');
          setTimeout(function () { renderCompany(c.id); }, 4000);
        }).catch(function (e) { toast('Ошибка: ' + e.message, 'error'); });
      });

      var editBtn = document.getElementById('btn-edit-company');
      if (editBtn) editBtn.addEventListener('click', function () {
        location.hash = '#/company/' + encodeURIComponent(c.id) + '/edit';
      });

      var renameBtn = document.getElementById('btn-rename-company');
      if (renameBtn) renameBtn.addEventListener('click', function () {
        showRenameDialog(c);
      });

    }).catch(function (err) {
      view.innerHTML = '<div class="card"><div class="card-body">Ошибка: ' + esc(err.message) + '</div></div>';
    });
  }

  // ------------------------------------------------------------------ обзор компании

  function renderCompanyOverview(c, box) {
      var html = '';

      // задания
      html += '<div class="card"><div class="card-head"><i class="fas fa-tasks"></i> Задания</div><div class="card-body" style="padding:0">';
      if (!c.jobs.length) {
        html += '<div style="padding:16px" class="muted">Проверок ещё не было.</div>';
      } else {
        html += '<div class="table-wrap"><table class="journal-table"><thead><tr>' +
          '<th>Задание</th><th>Тип</th><th>Статус</th><th>Последний успех</th>' +
          '<th>Возраст</th><th>Размер</th><th>Длит.</th><th>Провалов</th><th>Сообщение</th>' +
          '</tr></thead><tbody>';
        c.jobs.forEach(function (j) {
          html += '<tr>' +
            '<td><b>' + esc(j.job) + '</b></td>' +
            '<td class="muted">' + esc(j.kind) + '</td>' +
            '<td>' + badge(j.status) + '</td>' +
            '<td style="white-space:nowrap">' + fmtDateTime(j.lastSuccess) + '</td>' +
            '<td style="white-space:nowrap">' + (j.ageHours !== undefined && j.ageHours !== null ? j.ageHours + ' ч' : '—') + '</td>' +
            '<td style="white-space:nowrap">' + fmtSize(j.size) + '</td>' +
            '<td style="white-space:nowrap">' + fmtDuration(j.duration) + '</td>' +
            '<td>' + (j.failures || 0) + '</td>' +
            '<td class="msg">' + esc(j.message || '') + '</td>' +
            '</tr>';
        });
        html += '</tbody></table></div>';
      }
      html += '</div></div>';

      // контакты и политика
      html += '<div class="card"><div class="card-head"><i class="fas fa-info-circle"></i> Реквизиты</div><div class="card-body">';
      html += '<div class="kv">';
      html += '<div class="k">Политика</div><div>свежесть до ' + (c.policy.freshnessHours || '—') +
        ' ч, провалов до FAIL: ' + (c.policy.consecutiveFailuresToFail || '—') +
        (c.policy.minSizeMb ? ', минимум ' + c.policy.minSizeMb + ' МБ' : '') + '</div>';

      if (c.contacts && c.contacts.length) {
        html += '<div class="k">Контакты</div><div>' + c.contacts.map(function (ct) {
          return esc(ct.name || ct.role) +
            (ct.phone ? ' · <a href="tel:' + esc(ct.phone) + '">' + esc(ct.phone) + '</a>' : '') +
            (ct.email ? ' · <a href="mailto:' + esc(ct.email) + '">' + esc(ct.email) + '</a>' : '');
        }).join('<br>') + '</div>';
      }

      if (c.sources && c.sources.length) {
        html += '<div class="k">Источники</div><div>' + c.sources.map(function (s) {
          return '<code>' + esc(s.type) + '</code>' + (s.enabled ? '' : ' <span class="muted">(выкл)</span>');
        }).join('<br>') + '</div>';
      }
      html += '</div></div></div>';

      // история
      html += '<div class="card"><div class="card-head"><i class="fas fa-history"></i> История</div><div class="card-body" style="padding:0">';
      if (!c.history || !c.history.length) {
        html += '<div style="padding:16px" class="muted">Записей нет.</div>';
      } else {
        html += '<div class="table-wrap"><table class="journal-table"><thead><tr>' +
          '<th>Время</th><th>Задание</th><th>Статус</th><th>Последний успех</th><th>Размер</th><th>Сообщение</th>' +
          '</tr></thead><tbody>';
        c.history.forEach(function (r) {
          html += '<tr>' +
            '<td style="white-space:nowrap">' + fmtDateTime(r.ts) + '</td>' +
            '<td>' + esc(r.job) + '</td>' +
            '<td>' + badge(r.status) + '</td>' +
            '<td style="white-space:nowrap">' + fmtDateTime(r.lastSuccess) + '</td>' +
            '<td style="white-space:nowrap">' + fmtSize(r.size) + '</td>' +
            '<td class="msg">' + esc(r.message || '') + '</td>' +
            '</tr>';
        });
        html += '</tbody></table></div>';
      }
      html += '</div></div>';

      box.innerHTML = html;
  }

  // ------------------------------------------------------------------ железо

  var HW_KINDS = {
    smart: 'Диски (SMART)',
    raid: 'RAID-контроллер',
    hardware: 'Температуры и вентиляторы',
    mikrotik: 'MikroTik (RouterOS)',
    esxi: 'VMware ESXi'
  };

  function hardwareIcon(kind) {
    if (kind === 'smart') return '<i class="fas fa-hdd"></i>';
    if (kind === 'raid') return '<i class="fas fa-server"></i>';
    if (kind === 'mikrotik') return '<i class="fas fa-network-wired"></i>';
    if (kind === 'esxi') return '<i class="fas fa-cube"></i>';
    return '<i class="fas fa-thermometer-half"></i>';
  }

  function renderCompanyHardware(c, box) {
    // ВМ ESXi показываем во вкладке «ВМ», а не здесь
    var hw = (c.jobs || []).filter(function (j) {
      if (!HW_KINDS[j.kind]) return false;
      if (j.kind === 'esxi' && j.details && j.details.role === 'vm') return false;
      return true;
    });

    var html = '<div class="card">' +
      '<div class="card-head"><i class="fas fa-microchip"></i> Здоровье железа' +
      '<span class="spacer"></span><span class="muted" style="font-size:11px">' + hw.length + '</span>' +
      '</div><div class="card-body">';

    if (!hw.length) {
      html += '<p class="muted" style="margin:0">Проверок железа нет. Добавьте источник типов ' +
        '<code>smart</code>, <code>raid</code>, <code>hardware</code> или <code>mikrotik</code> в форме компании.</p>';
    } else {
      ['smart', 'raid', 'hardware', 'mikrotik', 'esxi'].forEach(function (kind) {
        var items = hw.filter(function (j) { return j.kind === kind; });
        if (!items.length) return;

        html += '<div class="hw-group"><div class="hw-group-title">' + HW_KINDS[kind] + '</div>';
        items.forEach(function (j) {
          html += '<div class="hw-row">' +
            '<div class="hw-icon s-' + esc(j.status) + '">' + hardwareIcon(kind) + '</div>' +
            '<div style="flex:1;min-width:0">' +
              '<div class="hw-name">' + esc(j.job) + '</div>' +
              '<div class="hw-msg">' + esc(j.message || '') + '</div>' +
            '</div>' +
            badge(j.status) +
            '<div class="hw-when">' + ago(j.updated) + '</div>' +
            '</div>';
        });
        html += '</div>';
      });
    }

    html += '</div></div>';

    html += '<p class="muted" style="font-size:11.5px">' +
      'SMART и RAID читаются локально на машине агента; MikroTik и ESXi — по сети, ' +
      'только чтение. Подробнее: <code>docs/hardware.md</code>, ' +
      '<code>docs/mikrotik.md</code>, <code>docs/esxi.md</code>.</p>';

    box.innerHTML = html;
  }

  // ------------------------------------------------------------------ инвентарь

  function renderCompanyInventory(c, box) {
    var inv = (c.jobs || []).filter(function (j) { return j.kind === 'inventory'; });

    if (!inv.length) {
      box.innerHTML = '<div class="card"><div class="card-body">' +
        '<p class="muted" style="margin:0">Инвентаризация не настроена. ' +
        'Добавьте источник типа <code>inventory</code> в форме компании.</p>' +
        '</div></div>';
      return;
    }

    // ---- система: берём задание, у которого в деталях есть host
    var sys = null;
    inv.forEach(function (j) {
      if (!sys && j.details && j.details.host) sys = { job: j, d: j.details };
    });

    var html = '';

    if (sys) {
      var d = sys.d;

      html += '<div class="card">' +
        '<div class="card-head"><i class="fas fa-desktop"></i> ' + esc(d.host || 'Система') +
        '<span class="spacer"></span>' + badge(sys.job.status) +
        '</div><div class="card-body">';

      html += '<div class="inv-grid">';
      html += invRow('ОС', d.os, d.osVersion ? 'build ' + d.osBuild + ' · ' + d.architecture : '');
      html += invRow('Модель', d.manufacturer, d.model);
      html += invRow('Процессор', d.cpu, d.cpuCores ? d.cpuCores + ' логических ядер' : '');
      html += invRow('Память', fmtMemMb(d.memoryMb), '');
      html += invRow('Домен / группа', d.domain, '');
      html += invRow('Uptime', fmtUptimeHours(d.uptimeHours), '');
      if (d.osInstalled) html += invRow('ОС установлена', fmtDate(d.osInstalled), '');
      if (d.rebootPending) {
        html += '<div class="inv-k">Перезагрузка</div>' +
          '<div class="inv-v"><span style="color:var(--warn)">' +
          '<i class="fas fa-exclamation-triangle"></i> требуется перезагрузка</span></div>';
      }
      html += '</div>';

      if (sys.job.message) {
        html += '<div class="inv-msg">' + esc(sys.job.message) + '</div>';
      }

      html += '</div></div>';
    }

    // ---- службы
    if (sys && sys.d.services && sys.d.services.length) {
      html += '<div class="card"><div class="card-head"><i class="fas fa-cogs"></i> Службы' +
        '<span class="spacer"></span><span class="muted" style="font-size:11px">' +
        sys.d.services.length + '</span></div><div class="card-body" style="padding:0">';

      html += '<div class="table-wrap"><table class="journal-table"><thead><tr>' +
        '<th>Служба</th><th>Состояние</th><th>Запуск</th></tr></thead><tbody>';

      sys.d.services.forEach(function (s) {
        var ok = String(s.state).toLowerCase() === 'running';
        html += '<tr>' +
          '<td><b>' + esc(s.name) + '</b>' +
            (s.displayName ? '<div class="vm-disk">' + esc(s.displayName) + '</div>' : '') + '</td>' +
          '<td><span class="vm-state ' + (ok ? 'vm-running' : 'vm-paused') + '">' +
            '<i class="vm-dot"></i>' + esc(s.state || '—') + '</span></td>' +
          '<td style="font-size:11.5px">' + esc(s.startMode || '—') + '</td>' +
          '</tr>';
      });

      html += '</tbody></table></div></div></div>';
    }

    // ---- тома
    var vols = inv.filter(function (j) { return j.details && j.details.volume; });
    if (vols.length) {
      html += '<div class="card"><div class="card-head"><i class="fas fa-hdd"></i> Диски' +
        '<span class="spacer"></span><span class="muted" style="font-size:11px">' +
        vols.length + '</span></div><div class="card-body" style="padding:0">';

      html += '<div class="table-wrap"><table class="journal-table"><thead><tr>' +
        '<th>Том</th><th>Всего</th><th>Свободно</th><th style="width:180px">Заполнено</th>' +
        '<th>Статус</th></tr></thead><tbody>';

      vols.forEach(function (j) {
        var v = j.details;
        var free = v.freePercent === undefined ? 0 : v.freePercent;
        var used = Math.max(0, Math.min(100, 100 - free));

        var barColor = free < 5 ? 'var(--fail)' : (free < 10 ? 'var(--warn)' : 'var(--ok)');

        html += '<tr>' +
          '<td><b>' + esc(v.volume) + '</b>' +
            (v.label ? ' <span class="muted">' + esc(v.label) + '</span>' : '') + '</td>' +
          '<td style="white-space:nowrap">' + fmtSize(v.totalBytes) + '</td>' +
          '<td style="white-space:nowrap">' + fmtSize(v.freeBytes) +
            ' <span class="muted">(' + Math.round(free) + '%)</span></td>' +
          '<td><div class="inv-bar"><i style="width:' + used + '%;background:' + barColor + '"></i></div></td>' +
          '<td>' + badge(j.status) + '</td>' +
          '</tr>';
      });

      html += '</tbody></table></div></div></div>';
    }

    // ---- программы
    var soft = null;
    inv.forEach(function (j) {
      if (!soft && j.details && j.details.items && j.details.count !== undefined &&
          !j.details.volume && !j.details.host) soft = { job: j, d: j.details };
    });

    if (soft) {
      html += '<div class="card"><div class="card-head"><i class="fas fa-box-open"></i> Программы' +
        '<span class="spacer"></span><span class="muted" style="font-size:11px">' +
        soft.d.count + '</span>' +
        '<button class="btn btn-sm" id="btn-inv-software" style="margin-left:10px">' +
        '<i class="fas fa-list"></i> Показать</button>' +
        '</div><div class="card-body">' +
        '<div class="muted" style="font-size:11.5px">' + esc(soft.job.message || '') + '</div>' +
        '</div></div>';
    }

    // ---- обновления
    var hf = null;
    inv.forEach(function (j) {
      if (!hf && j.details && j.details.count !== undefined && j.details.newest !== undefined) {
        hf = { job: j, d: j.details };
      }
    });

    if (hf) {
      html += '<div class="card"><div class="card-head"><i class="fas fa-shield-alt"></i> Обновления' +
        '<span class="spacer"></span>' + badge(hf.job.status) +
        '</div><div class="card-body">' +
        '<div class="muted" style="font-size:12px">' + esc(hf.job.message || '') + '</div>' +
        '</div></div>';
    }

    box.innerHTML = html;

    // кнопка «Показать программы»
    var swBtn = document.getElementById('btn-inv-software');
    if (swBtn && soft) {
      swBtn.addEventListener('click', function () { showSoftware(soft.d); });
    }
  }

  function invRow(k, v, sub) {
    if (!v) return '';
    return '<div class="inv-k">' + esc(k) + '</div>' +
      '<div class="inv-v">' + esc(v) +
      (sub ? '<div class="vm-disk">' + esc(sub) + '</div>' : '') + '</div>';
  }

  function showSoftware(d) {
    var html = '<div class="modal-backdrop" id="soft-modal" style="z-index:400">' +
      '<div class="modal modal-wide">' +
      '<h3><i class="fas fa-box-open"></i> Программы</h3>' +
      '<p class="muted" style="font-size:11.5px;margin-top:0">' +
        'Всего установлено: <b>' + d.count + '</b>' +
        (d.truncated ? ' · показаны первые ' + (d.items || []).length : '') + '</p>' +
      '<input type="text" id="soft-filter" placeholder="Фильтр по названию…" ' +
        'style="width:100%;padding:7px 10px;border:1px solid var(--border);' +
        'border-radius:6px;background:var(--surface);color:var(--ink);margin-bottom:10px">' +
      '<div id="soft-list" style="max-height:50vh;overflow:auto"></div>' +
      '<div class="modal-actions" style="margin-top:12px">' +
      '<button class="btn" id="soft-close">Закрыть</button>' +
      '</div></div></div>';

    var div = document.createElement('div');
    div.innerHTML = html;
    document.body.appendChild(div.firstChild);

    function render(q) {
      var items = (d.items || []).filter(function (s) {
        if (!q) return true;
        return (s.name || '').toLowerCase().indexOf(q) >= 0 ||
          (s.publisher || '').toLowerCase().indexOf(q) >= 0;
      });

      if (!items.length) {
        document.getElementById('soft-list').innerHTML =
          '<div class="muted" style="padding:10px">Ничего не найдено</div>';
        return;
      }

      var h = '<table class="journal-table"><thead><tr>' +
        '<th>Название</th><th>Версия</th><th>Издатель</th><th>Установлено</th>' +
        '</tr></thead><tbody>';

      items.forEach(function (s) {
        h += '<tr>' +
          '<td>' + esc(s.name) + '</td>' +
          '<td style="white-space:nowrap">' + esc(s.version || '—') + '</td>' +
          '<td style="font-size:11.5px">' + esc(s.publisher || '—') + '</td>' +
          '<td style="white-space:nowrap;font-size:11.5px">' + esc(s.installDate || '—') + '</td>' +
          '</tr>';
      });

      h += '</tbody></table>';
      document.getElementById('soft-list').innerHTML = h;
    }

    render('');

    document.getElementById('soft-filter').addEventListener('input', function (e) {
      render(e.target.value.toLowerCase());
    });

    document.getElementById('soft-close').addEventListener('click', function () {
      var m = document.getElementById('soft-modal');
      if (m) m.parentNode.removeChild(m);
    });
  }

  function fmtDate(iso) {
    if (!iso) return '';
    var d = new Date(iso);
    if (isNaN(d.getTime())) return '';
    return pad(d.getDate()) + '.' + pad(d.getMonth() + 1) + '.' + d.getFullYear();
  }

  // ------------------------------------------------------------------ диагностика

  // Состояния писателей VSS (коды одинаковы для всех языков Windows):
  // 1 = Stable, 2 = Waiting for freeze, 3 = In progress,
  // 4 = Waiting for thaw, 5 = Failed, 6 = Failed with error,
  // 7 = Timeout, 8 = Reverted.
  function vssStateClass(state) {
    if (state === 1) return 'vm-running';
    if (state === 5 || state === 6 || state === 7) return 'vm-missing';
    return 'vm-paused';
  }

  // \\?\Volume{8f3a...}\  →  {8f3a…}
  function shortVolume(v) {
    if (!v) return '—';
    var m = String(v).match(/\{[0-9a-fA-F-]{36}\}/);
    if (m) return m[0].substring(0, 10) + '…}';
    return String(v);
  }

  function renderCompanyDiagnostics(c, box) {
    var jobs = (c.jobs || []).filter(function (j) {
      return j.kind === 'vss' || j.kind === 'eventlog' ||
        j.kind === 'netconfig' || j.kind === 'tasks';
    });

    if (!jobs.length) {
      box.innerHTML = '<div class="card"><div class="card-head">' +
        '<i class="fas fa-stethoscope"></i> Диагностика</div><div class="card-body">' +
        '<p class="muted" style="margin:0 0 10px">Диагностика не настроена. ' +
        'Добавьте источник типа <code>vss</code>, <code>eventlog</code>, ' +
        '<code>netconfig</code> или <code>tasks</code> в форме компании.</p>' +
        '<p class="muted" style="font-size:11.5px;margin:0">' +
        '<b>VSS</b> — самая частая причина, по которой бэкап на Windows не создаётся. ' +
        '<b>Журнал событий</b> показывает, что именно падало на сервере. ' +
        '<b>Сеть</b> — сменился IP или отвалился маршрут. ' +
        '<b>Задачи</b> — есть ли задача бэкапа и как она отработала. ' +
        'Все проверки только читают данные и ничего не меняют. ' +
        'Подробнее: <code>docs/vss.md</code>, <code>docs/eventlog.md</code>, ' +
        '<code>docs/netconfig.md</code>, <code>docs/tasks.md</code>.</p>' +
        '</div></div>';
      return;
    }

    var writers = null, shadows = null, storage = null, evlog = null;
    var net = null, routes = null, tasksJob = null, autostart = null;

    jobs.forEach(function (j) {
      var d = j.details || {};
      if (j.kind === 'eventlog') { if (!evlog) evlog = j; return; }
      if (j.kind === 'tasks') {
        if (j.target === 'autostart') { if (!autostart) autostart = j; return; }
        if (!tasksJob && d.items) tasksJob = j;
        return;
      }
      if (j.kind === 'netconfig') {
        if (j.target === 'routes') { if (!routes) routes = j; return; }
        if (!net && d.adapters) net = j;
        return;
      }
      if (!writers && d.writers) writers = j;
      if (!shadows && j.target === 'shadows') shadows = j;
      if (!storage && j.target === 'storage') storage = j;
    });

    var html = '';

    // ---------------------------------------------------------- писатели
    if (writers) {
      var wd = writers.details || {};
      var list = wd.writers || [];
      var failed = 0, unstable = 0;

      list.forEach(function (w) {
        if (w.state === 5 || w.state === 6 || w.state === 7) failed++;
        else if (w.state !== 1) unstable++;
      });

      html += '<div class="card">' +
        '<div class="card-head"><i class="fas fa-camera-retro"></i> VSS: писатели' +
        '<span class="spacer"></span>' +
        '<span class="muted" style="font-size:11px;margin-right:10px">' +
          ago(writers.updated) + '</span>' + badge(writers.status) +
        '</div>';

      if (!list.length) {
        html += '<div class="card-body"><p class="muted" style="margin:0">' +
          esc(writers.message || 'писатели не найдены') + '</p>' +
          '<p class="muted" style="font-size:11.5px;margin:8px 0 0">' +
          'Обычно значит, что служба <code>VSS</code> остановлена. ' +
          'Без неё Veeam не сможет снять согласованный снимок.</p></div>';
      } else {
        var summary = 'писателей ' + list.length +
          (failed ? ' · <b style="color:var(--fail)">проблемных ' + failed + '</b>' : '') +
          (unstable ? ' · <b style="color:var(--warn)">нестабильных ' + unstable + '</b>' : '') +
          (!failed && !unstable ? ' · все стабильны' : '');

        html += '<div class="card-body" style="padding:0">' +
          '<div style="padding:10px 14px;border-bottom:1px solid var(--border);' +
            'font-size:11.5px" class="muted">' + summary + '</div>' +
          '<div class="table-wrap"><table class="journal-table"><thead><tr>' +
          '<th>Писатель</th><th style="width:190px">Состояние</th>' +
          '<th>Последняя ошибка</th></tr></thead><tbody>';

        list.forEach(function (w) {
          var cls = vssStateClass(w.state);
          var rowStyle = (w.state === 5 || w.state === 6 || w.state === 7)
            ? ' style="background:rgba(200,40,40,.06)"' : '';

          html += '<tr' + rowStyle + '>' +
            '<td><b>' + esc(w.name) + '</b></td>' +
            '<td><span class="vm-state ' + cls + '"><i class="vm-dot"></i>' +
              esc(w.stateText || ('state ' + w.state)) + '</span></td>' +
            '<td style="font-size:11.5px' +
              (w.lastError ? ';color:var(--fail)' : '') + '">' +
              esc(w.lastError || '—') + '</td>' +
            '</tr>';
        });

        html += '</tbody></table></div>';

        if (failed) {
          html += '<div style="padding:10px 14px;border-top:1px solid var(--border);' +
            'font-size:11.5px;color:var(--fail)">' +
            '<i class="fas fa-exclamation-triangle"></i> ' +
            'Писатель в состоянии Failed — Veeam не сможет сделать снимок этого ' +
            'компонента. Чаще всего помогает перезапуск службы писателя ' +
            '(например <code>VSS</code>, SQL, Exchange) или перезагрузка сервера.</div>';
        } else if (unstable) {
          html += '<div style="padding:10px 14px;border-top:1px solid var(--border);' +
            'font-size:11.5px;color:var(--warn)">' +
            '<i class="fas fa-hourglass-half"></i> ' +
            'Часть писателей не в состоянии Stable — возможно, прямо сейчас ' +
            'идёт создание снимка.</div>';
        }

        html += '</div>';
      }

      html += '</div>';
    }

    // ---------------------------------------------------------- теневые копии
    if (shadows) {
      var sd = shadows.details || {};
      var copies = sd.items || [];

      html += '<div class="card">' +
        '<div class="card-head"><i class="fas fa-clone"></i> Теневые копии' +
        '<span class="spacer"></span>' +
        '<span class="muted" style="font-size:11px;margin-right:10px">' +
          ago(shadows.updated) + '</span>' + badge(shadows.status) +
        '</div><div class="card-body">';

      html += '<div class="muted" style="font-size:12px">' +
        esc(shadows.message || '') + '</div>';

      if (copies.length) {
        html += '<div class="table-wrap" style="margin-top:10px">' +
          '<table class="journal-table"><thead><tr>' +
          '<th>Том</th><th>Создана</th><th>Возраст</th></tr></thead><tbody>';

        copies.slice(0, 50).forEach(function (sc) {
          html += '<tr>' +
            '<td><code title="' + esc(sc.volume || '') + '">' +
              esc(shortVolume(sc.volume)) + '</code></td>' +
            '<td style="white-space:nowrap;font-size:11.5px">' +
              esc(fmtDateTime(sc.created)) + '</td>' +
            '<td style="white-space:nowrap;font-size:11.5px">' +
              esc(ago(sc.created)) + '</td>' +
            '</tr>';
        });

        html += '</tbody></table></div>';

        if (copies.length > 50) {
          html += '<div class="muted" style="font-size:11px;margin-top:6px">' +
            'показаны первые 50 из ' + copies.length + '</div>';
        }
      }

      if (!copies.length) {
        html += '<p class="muted" style="font-size:11.5px;margin:8px 0 0">' +
          'Копий нет. Для сервера без VSS-бэкапов это нормально; ' +
          'если Veeam настроен — значит снимки не создаются.</p>';
      }

      html += '</div></div>';
    }

    // ---------------------------------------------------------- хранилище
    if (storage) {
      var st = storage.details || {};
      var items = st.items || [];

      html += '<div class="card">' +
        '<div class="card-head"><i class="fas fa-database"></i> Хранилище теней' +
        '<span class="spacer"></span>' +
        '<span class="muted" style="font-size:11px;margin-right:10px">' +
          ago(storage.updated) + '</span>' + badge(storage.status) +
        '</div>';

      if (!items.length) {
        html += '<div class="card-body"><p class="muted" style="margin:0">' +
          esc(storage.message || 'хранилище не настроено') + '</p></div>';
      } else {
        html += '<div class="card-body" style="padding:0">' +
          '<div class="table-wrap"><table class="journal-table"><thead><tr>' +
          '<th>Том</th><th>Занято</th><th>Лимит</th>' +
          '<th style="width:200px">Заполнено</th></tr></thead><tbody>';

        items.forEach(function (s) {
          var pct = s.usedPercent;
          var color = 'var(--ok)';
          if (pct >= 95) color = 'var(--fail)';
          else if (pct >= 80) color = 'var(--warn)';

          html += '<tr>' +
            '<td><code>' + esc(s.volume) + '</code></td>' +
            '<td style="white-space:nowrap">' + fmtSize(s.usedBytes) + '</td>' +
            '<td style="white-space:nowrap">' +
              (s.maxBytes > 0 ? fmtSize(s.maxBytes) : '<span class="muted">без лимита</span>') +
            '</td>' +
            '<td>' +
              (pct === undefined
                ? '<span class="muted">—</span>'
                : '<div class="inv-bar"><i style="width:' +
                    Math.max(0, Math.min(100, pct)) + '%;background:' + color + '"></i></div>' +
                  '<div class="muted" style="font-size:10.5px;margin-top:2px">' +
                    Math.round(pct * 10) / 10 + '%</div>') +
            '</td>' +
            '</tr>';
        });

        html += '</tbody></table></div></div>';
      }

      html += '</div>';
    }

    // ---------------------------------------------------------- журнал событий
    if (evlog) {
      var ed = evlog.details || {};
      var groups = ed.bySource || [];
      var events = ed.items || [];

      html += '<div class="card">' +
        '<div class="card-head"><i class="fas fa-clipboard-list"></i> Журнал событий' +
        '<span class="spacer"></span>' +
        '<span class="muted" style="font-size:11px;margin-right:10px">' +
          ago(evlog.updated) + '</span>' + badge(evlog.status) +
        '</div>';

      html += '<div class="card-body">';
      html += '<div class="muted" style="font-size:12px">' + esc(evlog.message || '') + '</div>';

      if (ed.logs && ed.logs.length) {
        html += '<div class="muted" style="font-size:11px;margin-top:4px">' +
          'журналы: ' + esc(ed.logs.join(', ')) +
          (ed.ignored ? ' · проигнорировано ' + ed.ignored : '') +
          '</div>';
      }

      html += '</div>';

      if (groups.length) {
        html += '<div class="table-wrap"><table class="journal-table"><thead><tr>' +
          '<th>Источник</th><th>Событие</th><th>Уровень</th>' +
          '<th>Кол-во</th><th>Последнее</th></tr></thead><tbody>';

        groups.slice(0, 20).forEach(function (g) {
          var lvlColor = g.level === 'Critical' || g.level === 'Error'
            ? 'var(--fail)' : (g.level === 'Warning' ? 'var(--warn)' : '');

          html += '<tr>' +
            '<td><b>' + esc(g.source) + '</b></td>' +
            '<td><code>' + esc(g.id) + '</code></td>' +
            '<td style="font-size:11.5px' + (lvlColor ? ';color:' + lvlColor : '') + '">' +
              esc(g.level) + '</td>' +
            '<td><b>' + g.count + '</b></td>' +
            '<td style="white-space:nowrap;font-size:11.5px">' +
              esc(ago(g.last)) + '</td>' +
            '</tr>';
        });

        html += '</tbody></table></div>';
      } else if (ed.count === 0) {
        html += '<div class="card-body" style="border-top:1px solid var(--border)">' +
          '<p class="muted" style="margin:0;font-size:11.5px">' +
          '<i class="fas fa-check" style="color:var(--ok)"></i> ' +
          'Ошибок за период нет.</p></div>';
      }

      if (events.length) {
        html += '<div class="card-body" style="border-top:1px solid var(--border);padding:0">' +
          '<div style="padding:8px 14px;font-size:11px" class="muted">последние события</div>' +
          '<div class="table-wrap"><table class="journal-table"><thead><tr>' +
          '<th style="width:120px">Время</th><th style="width:150px">Источник</th>' +
          '<th>Сообщение</th></tr></thead><tbody>';

        events.slice(0, 20).forEach(function (e) {
          var isErr = e.level === 'Critical' || e.level === 'Error';

          html += '<tr>' +
            '<td style="white-space:nowrap;font-size:11px">' +
              esc(fmtDateTime(e.time)) + '</td>' +
            '<td style="font-size:11px">' +
              '<b' + (isErr ? ' style="color:var(--fail)"' : '') + '>' +
              esc(e.source) + '</b> <code>' + esc(e.id) + '</code></td>' +
            '<td style="font-size:11.5px">' + esc(e.message || '—') + '</td>' +
            '</tr>';
        });

        html += '</tbody></table></div></div>';
      }

      html += '</div>';
    }

    // ---------------------------------------------------------- сеть
    if (net) {
      var nd = net.details || {};
      var adapters = nd.adapters || [];

      html += '<div class="card">' +
        '<div class="card-head"><i class="fas fa-network-wired"></i> Сеть' +
        '<span class="spacer"></span>' +
        '<span class="muted" style="font-size:11px;margin-right:10px">' +
          ago(net.updated) + '</span>' + badge(net.status) +
        '</div>';

      html += '<div class="card-body" style="padding:0">';

      if (!adapters.length) {
        html += '<div style="padding:12px 14px" class="muted">' +
          esc(net.message || 'адаптеры не найдены') + '</div>';
      } else {
        html += '<div class="table-wrap"><table class="journal-table"><thead><tr>' +
          '<th>Адаптер</th><th>Адрес</th><th>Шлюз</th><th>DNS</th>' +
          '<th style="width:110px">Канал</th></tr></thead><tbody>';

        adapters.forEach(function (a) {
          var ip = (a.ip || []).filter(function (v) { return v.indexOf(':') < 0; });
          var ipText = ip.length ? ip.join(', ') : (a.ip || []).join(', ');
          if (a.dhcp) ipText += ' <span class="muted">(DHCP)</span>';

          var gw = (a.gateway || []).join(', ') || '—';
          var dns = (a.dns || []).join(', ') || '—';

          var linkCls = a.status === 'Connected' ? 'vm-running' :
            (a.status === 'Disconnected' || a.status === 'Media disconnected'
              ? 'vm-off' : 'vm-paused');
          var linkText = a.status || '—';
          if (a.speedMbps > 0) {
            linkText += ' · ' + (a.speedMbps >= 1000
              ? (a.speedMbps / 1000) + ' Гбит/с'
              : a.speedMbps + ' Мбит/с');
          }

          html += '<tr>' +
            '<td><b>' + esc(a.name) + '</b>' +
              (a.mac ? '<div class="vm-disk">' + esc(a.mac) + '</div>' : '') + '</td>' +
            '<td style="font-size:11.5px">' + ipText + '</td>' +
            '<td style="font-size:11.5px">' + esc(gw) + '</td>' +
            '<td style="font-size:11.5px">' + esc(dns) + '</td>' +
            '<td><span class="vm-state ' + linkCls + '"><i class="vm-dot"></i>' +
              esc(linkText) + '</span></td>' +
            '</tr>';
        });

        html += '</tbody></table></div>';
      }

      if (routes) {
        var ri = (routes.details || {}).items || [];
        html += '<div style="padding:10px 14px;border-top:1px solid var(--border);' +
          'font-size:11.5px" class="muted">' +
          '<i class="fas fa-route"></i> по умолчанию: ' +
          (ri.length
            ? esc(ri.map(function (r) { return r.gateway + ' (метрика ' + r.metric + ')'; }).join(', '))
            : '<b style="color:var(--warn)">нет маршрута</b>') +
          '</div>';
      }

      html += '</div></div>';
    }

    // ---------------------------------------------------------- задачи планировщика
    if (tasksJob) {
      var td = tasksJob.details || {};
      var tl = td.items || [];

      html += '<div class="card">' +
        '<div class="card-head"><i class="fas fa-clock"></i> Задачи планировщика' +
        '<span class="spacer"></span>' +
        '<span class="muted" style="font-size:11px;margin-right:10px">' +
          ago(tasksJob.updated) + '</span>' + badge(tasksJob.status) +
        '</div>';

      if (!tl.length) {
        html += '<div class="card-body"><p class="muted" style="margin:0">' +
          esc(tasksJob.message || 'задач нет') + '</p></div>';
      } else {
        html += '<div class="card-body" style="padding:0">' +
          '<div style="padding:10px 14px;border-bottom:1px solid var(--border);' +
            'font-size:11.5px" class="muted">' + esc(tasksJob.message || '') + '</div>' +
          '<div class="table-wrap"><table class="journal-table"><thead><tr>' +
          '<th>Задача</th><th style="width:110px">Состояние</th>' +
          '<th style="width:150px">Последний запуск</th><th style="width:130px">Код</th>' +
          '</tr></thead><tbody>';

        tl.forEach(function (t) {
          var bad = t.state === 'Disabled';
          var err = t.lastResult && t.lastResult !== 0 && t.lastResult !== 267009 && t.lastResult !== 267011;

          html += '<tr' + (bad ? ' style="background:rgba(200,40,40,.06)"' : '') + '>' +
            '<td><b>' + esc(t.path) + '</b>' +
              (t.action ? '<div class="vm-disk">' + esc(t.action) + '</div>' : '') + '</td>' +
            '<td>' +
              (t.state === 'Disabled'
                ? '<span class="vm-state vm-off"><i class="vm-dot"></i>отключена</span>'
                : (t.state === 'Running'
                    ? '<span class="vm-state vm-running"><i class="vm-dot"></i>выполняется</span>'
                    : '<span class="vm-state vm-running"><i class="vm-dot"></i>' + esc(t.state) + '</span>')) +
            '</td>' +
            '<td style="font-size:11.5px;white-space:nowrap">' +
              (t.lastRun ? esc(fmtDateTime(t.lastRun)) + ' <span class="muted">(' +
                esc(ago(t.lastRun)) + ')</span>' : '<span class="muted">—</span>') + '</td>' +
            '<td style="font-size:11.5px' + (err ? ';color:var(--fail)' : '') + '">' +
              (err ? esc(t.lastResultText || t.lastResult) : '<span class="muted">0</span>') +
            '</td>' +
            '</tr>';
        });

        html += '</tbody></table></div></div>';
      }

      html += '</div>';
    }

    // ---------------------------------------------------------- автозагрузка
    if (autostart) {
      var ai = (autostart.details || {}).items || [];

      html += '<div class="card">' +
        '<div class="card-head"><i class="fas fa-power-off"></i> Автозагрузка' +
        '<span class="spacer"></span>' +
        '<span class="muted" style="font-size:11px;margin-right:10px">' +
          ai.length + '</span>' + badge(autostart.status) +
        '</div>';

      if (!ai.length) {
        html += '<div class="card-body"><p class="muted" style="margin:0;font-size:11.5px">' +
          'Записей автозагрузки нет.</p></div>';
      } else {
        html += '<div class="card-body" style="padding:0">' +
          '<div class="table-wrap"><table class="journal-table"><thead><tr>' +
          '<th>Название</th><th>Команда</th><th style="width:170px">Где</th>' +
          '</tr></thead><tbody>';

        ai.forEach(function (a) {
          html += '<tr>' +
            '<td><b>' + esc(a.name) + '</b></td>' +
            '<td style="font-size:11px"><code>' + esc(a.command) + '</code></td>' +
            '<td style="font-size:11px" class="muted">' + esc(a.where) + '</td>' +
            '</tr>';
        });

        html += '</tbody></table></div></div>';
      }

      html += '</div>';
    }

    html += '<p class="muted" style="font-size:11.5px">' +
      'Только чтение, ничего не меняется. VSS: <code>vssadmin list writers</code> и WMI ' +
      '<code>Win32_ShadowCopy</code> / <code>Win32_ShadowStorage</code> — ' +
      'если писатель в <b>Failed</b>, это почти всегда и есть причина, ' +
      'по которой бэкап не создаётся. Журнал: <code>EventLogReader</code>, ' +
      'ошибки и критичные события. Сеть: WMI, адреса, шлюзы, DNS. ' +
      'Задачи: <code>Schedule.Service</code> и ветки <code>Run</code>. ' +
      'Подробнее: <code>docs/vss.md</code>, ' +
      '<code>docs/eventlog.md</code>, <code>docs/netconfig.md</code>, ' +
      '<code>docs/tasks.md</code>, <code>docs/management.md</code>.</p>';

    box.innerHTML = html;
  }

  function fmtDateTime(iso) {
    if (!iso) return '—';
    var d = new Date(iso);
    if (isNaN(d.getTime())) return '—';
    return pad(d.getDate()) + '.' + pad(d.getMonth() + 1) + '.' + d.getFullYear() +
      ' ' + pad(d.getHours()) + ':' + pad(d.getMinutes());
  }

  // ------------------------------------------------------------------ виртуальные машины

  function vmStateClass(state) {
    if (!state) return '';
    var s = String(state).toLowerCase();
    if (s === 'running' || s === 'poweredon') return 'vm-running';
    if (s === 'off' || s === 'saved' || s === 'fastsaved' || s === 'poweredoff') return 'vm-off';
    if (s === 'paused' || s === 'paused-critical' || s === 'suspended') return 'vm-paused';
    if (s === 'missing') return 'vm-missing';
    return 'vm-other';
  }

  function vmIsRunning(state) {
    var s = String(state || '').toLowerCase();
    return s === 'running' || s === 'poweredon';
  }

  function fmtUptimeHours(h) {
    if (h === undefined || h === null || h < 0) return '—';
    if (h < 1) return Math.round(h * 60) + ' мин';
    if (h < 48) return Math.round(h) + ' ч';
    return Math.floor(h / 24) + ' дн';
  }

  function fmtMemMb(mb) {
    if (!mb || mb <= 0) return '—';
    if (mb >= 1024) return (mb / 1024).toFixed(mb % 1024 === 0 ? 0 : 1) + ' ГБ';
    return mb + ' МБ';
  }

  function renderCompanyVms(c, box) {
    var vms = (c.jobs || []).filter(function (j) {
      if (j.kind === 'hyperv') return true;
      if (j.kind === 'esxi' && j.details && j.details.role === 'vm') return true;
      return false;
    });

    var html = '<div class="card">' +
      '<div class="card-head"><i class="fas fa-server"></i> Виртуальные машины' +
      '<span class="spacer"></span><span class="muted" style="font-size:11px">' + vms.length + '</span>' +
      '</div>';

    if (!vms.length) {
      html += '<div class="card-body"><p class="muted" style="margin:0">' +
        'Проверок ВМ нет. Добавьте источник типа <code>hyperv</code> или <code>esxi</code> в форме компании.</p></div>';
    } else {
      // порядок: сначала проблемы, потом работающие, потом остальные
      var order = { FAIL: 0, WARN: 1, NODATA: 2, OK: 3, SKIP: 4 };
      vms = vms.slice().sort(function (a, b) {
        var d = (order[a.status] === undefined ? 9 : order[a.status]) -
                (order[b.status] === undefined ? 9 : order[b.status]);
        if (d !== 0) return d;
        return String(a.job).localeCompare(String(b.job));
      });

      var running = vms.filter(function (j) {
        return j.details && vmIsRunning(j.details.state);
      }).length;

      html += '<div class="card-body" style="padding:0">';
      html += '<div class="vm-summary">' +
        '<span><i class="fas fa-play" style="color:var(--ok)"></i> работают: <b>' + running + '</b></span>' +
        '<span><i class="fas fa-server"></i> всего: <b>' + vms.length + '</b></span>' +
        '</div>';

      html += '<div class="table-wrap"><table class="journal-table vm-table"><thead><tr>' +
        '<th>ВМ</th><th>Состояние</th><th>vCPU</th><th>Память</th>' +
        '<th>Uptime</th><th>Снимки</th><th>Статус</th><th>Сообщение</th>' +
        '</tr></thead><tbody>';

      vms.forEach(function (j) {
        var d = j.details || {};
        var name = String(j.job || '').replace(/^[^:]*:\s*/, '');

        html += '<tr>' +
          '<td><b>' + esc(name) + '</b>' +
            (d.disks && d.disks.length ? '<div class="vm-disk">' + esc(d.disks[0]) + '</div>' : '') +
          '</td>' +
          '<td><span class="vm-state ' + vmStateClass(d.state) + '">' +
            '<i class="vm-dot"></i>' + esc(d.state || '—') + '</span>' +
            (d.dynamicMemory ? ' <span class="tag">dyn</span>' : '') +
          '</td>' +
          '<td>' + (d.vcpu || '—') + '</td>' +
          '<td style="white-space:nowrap">' + fmtMemMb(d.memoryMb) + '</td>' +
          '<td style="white-space:nowrap">' + fmtUptimeHours(d.uptimeHours) + '</td>' +
          '<td>' + (d.snapshots || d.checkpoints || '—') + '</td>' +
          '<td>' + badge(j.status) + '</td>' +
          '<td class="msg">' + esc(j.message || '') + '</td>' +
          '</tr>';
      });

      html += '</tbody></table></div></div>';
    }

    html += '</div>';

    html += '<p class="muted" style="font-size:11.5px">' +
      'Данные читаются через WMI <code>root\\virtualization\\v2</code> (Hyper-V) ' +
      'или SOAP vim25 (ESXi). Каждая ВМ — отдельное задание, поэтому в Zabbix видно, ' +
      'какая именно упала. Подробнее: <code>docs/hyperv.md</code>, <code>docs/esxi.md</code>.</p>';

    box.innerHTML = html;
  }

  // ------------------------------------------------------------------ JSON компании

  function renderCompanyJson(id, box) {
    box.innerHTML = '<div class="card"><div class="card-body"><span class="muted">Загрузка…</span></div></div>';

    api('/api/company/' + encodeURIComponent(id) + '/raw').then(function (raw) {
      var text = JSON.stringify(raw, null, 2);
      var canEdit = canWrite();

      var html = '<div class="card">' +
        '<div class="card-head"><i class="fas fa-code"></i> JSON компании' +
        '<span class="spacer"></span>' +
        (canEdit ? '<button class="btn btn-sm" id="json-format"><i class="fas fa-align-left"></i> Формат</button> ' : '') +
        (canEdit ? '<button class="btn btn-sm btn-primary" id="json-save"><i class="fas fa-save"></i> Сохранить</button>' : '') +
        '</div><div class="card-body">' +
        '<p class="muted" style="font-size:11.5px;margin-top:0">' +
        'Полный доступ ко всем полям, включая те, которых нет в форме. ' +
        'Пароли хранятся в <code>secrets.json</code> — здесь только ключи ' +
        '(<code>passwordSecret</code>). Чтобы задать новый пароль, используйте форму.</p>' +
        '<textarea id="json-editor" class="json-editor" spellcheck="false"' +
        (canEdit ? '' : ' readonly') + '>' + esc(text) + '</textarea>' +
        '<div id="json-status" class="json-status"></div>' +
        '</div></div>';

      box.innerHTML = html;

      var ta = document.getElementById('json-editor');

      if (canEdit) {
        document.getElementById('json-format').addEventListener('click', function () {
          try {
            ta.value = JSON.stringify(JSON.parse(ta.value), null, 2);
            setJsonStatus('отформатировано', 'good');
          } catch (e) {
            setJsonStatus('некорректный JSON: ' + e.message, 'bad');
          }
        });

        document.getElementById('json-save').addEventListener('click', function () {
          var parsed;
          try {
            parsed = JSON.parse(ta.value);
          } catch (e) {
            setJsonStatus('некорректный JSON: ' + e.message, 'bad');
            return;
          }

          api('/api/company/' + encodeURIComponent(id) + '/raw', {
            method: 'PUT', body: parsed
          }).then(function (res) {
            var msg = 'Сохранено';
            if (res.warnings && res.warnings.length) msg += ' · ' + res.warnings.join('; ');
            setJsonStatus(msg, 'good');
            toast('JSON сохранён', 'ok');
            setTimeout(function () { renderCompany(id); }, 900);
          }).catch(function (e) {
            setJsonStatus('ошибка: ' + e.message, 'bad');
          });
        });
      }

    }).catch(function (err) {
      box.innerHTML = '<div class="card"><div class="card-body">Ошибка: ' + esc(err.message) + '</div></div>';
    });
  }

  function setJsonStatus(text, kind) {
    var el = document.getElementById('json-status');
    if (!el) return;
    el.textContent = text;
    el.className = 'json-status ' + (kind || '');
  }

  // ------------------------------------------------------------------ история компании

  function renderCompanyHistory(id, box) {
    box.innerHTML = '<div class="card"><div class="card-body"><span class="muted">Загрузка…</span></div></div>';

    api('/api/company/' + encodeURIComponent(id) + '/history').then(function (res) {
      var items = res.items || [];

      var html = '<div class="card"><div class="card-head"><i class="fas fa-history"></i> История изменений' +
        '<span class="spacer"></span><span class="muted" style="font-size:11px">' + items.length + '</span>' +
        '</div><div class="card-body" style="padding:0">';

      if (!items.length) {
        html += '<div style="padding:16px" class="muted">Изменений пока не было.</div>';
      } else {
        html += '<div class="table-wrap"><table class="journal-table"><thead><tr>' +
          '<th>Когда</th><th>Кто</th><th>Действие</th><th>Детали</th>' +
          '</tr></thead><tbody>';
        items.forEach(function (e) {
          html += '<tr>' +
            '<td style="white-space:nowrap">' + fmtDateTime(e.ts) + '</td>' +
            '<td>' + esc(e.user || '—') + '</td>' +
            '<td><code>' + esc(e.action || '') + '</code></td>' +
            '<td class="msg">' + esc(e.detail || '') + '</td>' +
            '</tr>';
        });
        html += '</tbody></table></div>';
      }

      html += '</div></div>';
      box.innerHTML = html;

    }).catch(function (err) {
      box.innerHTML = '<div class="card"><div class="card-body">Ошибка: ' + esc(err.message) + '</div></div>';
    });
  }

  // ------------------------------------------------------------------ переименование

  function showRenameDialog(c) {
    var html = '<div class="modal-backdrop" id="rename-modal">' +
      '<div class="modal">' +
      '<h3><i class="fas fa-i-cursor"></i> Сменить ID компании</h3>' +
      '<p class="muted" style="font-size:11.5px">Текущий: <code>' + esc(c.id) + '</code></p>' +
      '<label class="fld" style="margin-top:10px"><span>Новый ID</span>' +
      '<input type="text" id="rename-new" value="' + esc(c.id) + '"></label>' +
      '<label class="chk"><input type="checkbox" id="rename-history" checked> Перенести историю и свёртки</label>' +
      '<label class="chk"><input type="checkbox" id="rename-zabbix"> Переименовать хост в Zabbix</label>' +
      '<div class="modal-actions" style="margin-top:14px">' +
      '<button class="btn" id="rename-cancel">Отмена</button>' +
      '<button class="btn btn-primary" id="rename-ok">Переименовать</button>' +
      '</div></div></div>';

    var div = document.createElement('div');
    div.innerHTML = html;
    document.body.appendChild(div.firstChild);

    document.getElementById('rename-cancel').addEventListener('click', function () {
      var m = document.getElementById('rename-modal');
      if (m) m.parentNode.removeChild(m);
    });

    document.getElementById('rename-ok').addEventListener('click', function () {
      var newId = document.getElementById('rename-new').value.trim();
      if (!newId || newId === c.id) {
        toast('ID не изменился', 'error');
        return;
      }

      api('/api/company/' + encodeURIComponent(c.id) + '/rename', {
        method: 'POST',
        body: {
          newId: newId,
          moveHistory: document.getElementById('rename-history').checked,
          renameZabbix: document.getElementById('rename-zabbix').checked
        }
      }).then(function (res) {
        var m = document.getElementById('rename-modal');
        if (m) m.parentNode.removeChild(m);
        toast('ID изменён: ' + res.oldId + ' → ' + res.newId +
          (res.movedRuns ? ' (история: ' + res.movedRuns + ')' : ''), 'ok');
        location.hash = '#/company/' + encodeURIComponent(res.newId);
        renderCompany(res.newId);
      }).catch(function (e) {
        toast('Ошибка: ' + e.message, 'error');
      });
    });
  }

  // ------------------------------------------------------------------ настройки

  // ------------------------------------------------------------------ оформление (P17)

  function loadBrandingBox() {
    var box = document.getElementById('brand-box');
    if (!box) return;

    api('/api/branding').then(function (b) {
      var html = '<div class="kv">' +
        '<div class="k">Название</div><div><span class="bt-brand" style="color:' +
          esc(b.part1Color) + '">' + esc(b.part1) + '</span> <span class="bt-rest" style="color:' +
          esc(b.part2Color) + '">' + esc(b.part2) + '</span></div>' +
        '<div class="k">Подзаголовок</div><div>' + esc(b.tagline) + '</div>' +
        '<div class="k">Заголовок вкладки</div><div>' + esc(b.title) + '</div>' +
        '<div class="k">Логотип</div><div><code>' + esc(b.logoLight) + '</code> / <code>' + esc(b.logoDark) + '</code></div>' +
        '<div class="k">Подпись</div><div>' + esc(b.footerText) + ' · <a href="' +
          esc(b.footerUrl) + '" target="_blank" rel="noopener">' + esc(b.footerUrlText) + '</a></div>' +
        '<div class="k">Название продукта</div><div>' + esc(b.productName) + '</div>' +
        '</div>' +
        '<p class="muted" style="font-size:11.5px;margin:10px 0 0">' +
        'Правится в <code>config/branding.json</code>. Изменения применяются ' +
        'сразу после перезагрузки страницы — перезапуск службы не нужен.</p>';

      box.innerHTML = html;

      var open = document.getElementById('brand-open');
      if (open) open.addEventListener('click', openBrandEditor);
    }).catch(function (err) {
      box.innerHTML = '<div class="muted">Ошибка: ' + esc(err.message) + '</div>';
    });
  }

  function persistFile(filename, text) {
    if (!filename) return false;
    var a = document.createElement('a');
    var blob = new Blob([text], { type: 'application/json;charset=utf-8' });
    a.href = URL.createObjectURL(blob);
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    setTimeout(function () { URL.revokeObjectURL(a.href); a.parentNode.removeChild(a); }, 500);
    return true;
  }

  // Пример полного бренда — чтобы было видно все поля, включая пути и службы.
  function sampleBrand() {
    var b = state.branding || {};
    return {
      documentTitle: (b.productName || 'Backup Monitor'),
      brand: {
        part1: b.part1 || 'Backup',
        part2: b.part2 || 'Monitor',
        part1Color: b.part1Color || '#FF9800',
        part2Color: b.part2Color || '#FFFFFF',
        tagline: b.tagline || 'Мониторинг бэкапов',
        logoLight: b.logoLight || 'img/logo.png',
        logoDark: b.logoDark || 'img/logo-dark.png',
        favicon: b.favicon || 'img/logo.png',
        footer: b.footerText || 'Компания',
        footerUrl: b.footerUrl || 'https://example.local',
        footerUrlText: b.footerUrlText || 'example.local',
        productName: b.productName || 'Backup Monitor',
        company: b.company || 'Компания',
        description: b.description || 'Мониторинг бэкапов',
        installDir: b.installDir || 'C:\\Program Files\\BackupMonitor',
        dataDir: b.dataDir || 'C:\\ProgramData\\BackupMonitor',
        serviceName: b.serviceName || 'BackupMonitor',
        serviceNameRemote: b.serviceNameRemote || 'BackupMonitorRemote',
        userAgent: b.userAgent || 'BackupMonitor',
        zabbixTemplateName: b.zabbixTemplateName || 'Backup Monitor'
      }
    };
  }

  function openBrandEditor() {
    api('/api/branding/raw').then(function (res) {
      var html = '<div class="modal-backdrop" id="brand-modal">' +
        '<div class="modal modal-wide">' +
        '<h3><i class="fas fa-palette"></i> Оформление и бренд</h3>' +
        '<p class="muted" style="font-size:11.5px;margin-top:0">' +
        'Файл: <code>' + esc(res.path) + '</code></p>' +
        '<textarea id="brand-editor" class="json-editor" spellcheck="false" ' +
        'style="min-height:360px">' + esc(res.json) + '</textarea>' +
        '<div id="brand-status" class="json-status"></div>' +
        '<details style="margin-top:10px">' +
        '<summary class="muted" style="cursor:pointer;font-size:11.5px">' +
        'Что можно менять: поля и их значения</summary>' +
        '<div class="table-wrap" style="margin-top:8px"><table class="journal-table">' +
        '<thead><tr><th>Поле</th><th>Что это</th><th>Пример</th></tr></thead><tbody>' +
        '<tr><td><code>part1</code> / <code>part2</code></td><td>название в шапке и на входе</td><td>Acme / Backup</td></tr>' +
        '<tr><td><code>productName</code></td><td>полное имя продукта</td><td>Acme Backup</td></tr>' +
        '<tr><td><code>company</code></td><td>издатель в свойствах exe и аудите</td><td>Acme LLC</td></tr>' +
        '<tr><td><code>footer</code>, <code>footerUrl</code></td><td>подпись внизу</td><td>acme.example</td></tr>' +
        '<tr><td><code>installDir</code></td><td>каталог установки; из него берутся пути новых агентов</td><td>C:\\Program Files\\Acme</td></tr>' +
        '<tr><td><code>dataDir</code></td><td>служебный каталог (diag.log, раннер)</td><td>C:\\ProgramData\\Acme</td></tr>' +
        '<tr><td><code>serviceName</code> / <code>serviceNameRemote</code></td><td>имена служб</td><td>AcmeBackup / AcmeAgent</td></tr>' +
        '<tr><td><code>userAgent</code></td><td>как продукт представляется снаружи</td><td>AcmeBackup</td></tr>' +
        '<tr><td><code>zabbixTemplateName</code></td><td>имя шаблона Zabbix</td><td>Acme Backup</td></tr>' +
        '</tbody></table></div>' +
        '<p class="muted" style="font-size:11px;margin:8px 0 0">' +
        'Смена <code>installDir</code> и <code>serviceName</code> на <b>уже установленном</b> экземпляре ' +
        'не перенесёт службу — это для новых установок. Цвета, логотипы и подписи применяются сразу.</p>' +
        '</details>' +
        '<div class="modal-actions" style="margin-top:12px">' +
        '<button class="btn" id="brand-cancel">Отмена</button>' +
        '<button class="btn" id="brand-example"><i class="fas fa-magic"></i> Пример</button>' +
        '<button class="btn" id="brand-download"><i class="fas fa-download"></i> Скачать</button>' +
        '<button class="btn btn-primary" id="brand-save"><i class="fas fa-save"></i> Сохранить</button>' +
        '</div></div></div>';

      var div = document.createElement('div');
      div.innerHTML = html;
      document.body.appendChild(div.firstChild);

      document.getElementById('brand-cancel').addEventListener('click', closeBrandModal);

      // подставить пример целиком: удобно, когда меняешь всё, а не только цвет
      document.getElementById('brand-example').addEventListener('click', function () {
        var ta = document.getElementById('brand-editor');
        if (!confirm('Заменить текущее содержимое примером со всеми полями?')) return;
        ta.value = JSON.stringify(sampleBrand(), null, 2);
      });

      // сохранить файл локально — потом можно отдать другой компании
      document.getElementById('brand-download').addEventListener('click', function () {
        persistFile('branding.json', document.getElementById('brand-editor').value);
      });

      document.getElementById('brand-save').addEventListener('click', function () {
        var ta = document.getElementById('brand-editor');
        var st = document.getElementById('brand-status');

        try { JSON.parse(ta.value); }
        catch (e) {
          st.textContent = 'некорректный JSON: ' + e.message;
          st.className = 'json-status bad';
          return;
        }

        api('/api/branding/raw', { method: 'PUT', body: { json: ta.value } })
          .then(function (r) {
            closeBrandModal();
            toast('Бренд сохранён', 'ok');
            if (r.warnings && r.warnings.length) toast(r.warnings.join('; '), 'error');
            // применяем сразу
            applyBranding(r.effective);
            state.branding = r.effective;
            loadBrandingBox();
          })
          .catch(function (e) {
            st.textContent = 'ошибка: ' + e.message;
            st.className = 'json-status bad';
          });
      });

    }).catch(function (err) {
      toast('Ошибка: ' + err.message, 'error');
    });
  }

  function closeBrandModal() {
    var m = document.getElementById('brand-modal');
    if (m) m.parentNode.removeChild(m);
  }

  // ------------------------------------------------------------------ удалённые агенты

  function loadAgents() {
    var box = document.getElementById('agents-box');
    if (!box) return;

    api('/api/agents').then(function (res) {
      var agents = res.agents || [];
      var cnt = document.getElementById('agents-count');
      if (cnt) cnt.textContent = agents.length;

      if (!agents.length) {
        box.innerHTML = '<div style="padding:16px" class="muted">' +
          'Агентов нет. Создайте <code>config/agents.json</code> — см. <code>docs/remote-agents.md</code>.</div>';
        return;
      }

      var html = '<div class="table-wrap"><table class="journal-table"><thead><tr>' +
        '<th>Агент</th><th>Состояние</th><th>Компаний</th><th>Адрес</th>' +
        '<th>Версия</th><th>Последний выход</th>' +
        '</tr></thead><tbody>';

      agents.forEach(function (a) {
        var online = a.online;
        html += '<tr>' +
          '<td><b>' + esc(a.id) + '</b><div class="muted" style="font-size:11px">' + esc(a.name) + '</div></td>' +
          '<td><span class="vm-state ' + (online ? 'vm-running' : 'vm-off') + '">' +
            '<i class="vm-dot"></i>' + (online ? 'на связи' : 'молчит') + '</span>' +
            (a.lastError ? '<div class="muted" style="font-size:11px;color:var(--fail)">' + esc(a.lastError) + '</div>' : '') +
          '</td>' +
          '<td>' + (a.companyCount || 0) + '</td>' +
          '<td style="font-size:11.5px">' + (a.lastIp ? esc(a.lastIp) : '—') + '</td>' +
          '<td style="font-size:11.5px">' + (a.lastVersion ? esc(a.lastVersion) : '—') + '</td>' +
          '<td style="white-space:nowrap">' + (a.lastSeen ? ago(a.lastSeen) : 'ни разу') + '</td>' +
          '</tr>';
      });

      html += '</tbody></table></div>';
      box.innerHTML = html;

    }).catch(function (err) {
      box.innerHTML = '<div style="padding:16px" class="muted">Ошибка: ' + esc(err.message) + '</div>';
    });
  }

  function renderSettings() {
    view.innerHTML = '<div class="muted">Загрузка…</div>';

    api('/api/settings').then(function (s) {
      state.settings = s;
      var html = '<h2 class="page-title"><i class="fas fa-cog"></i> Настройки</h2>';

      html += '<div class="card"><div class="card-head"><i class="fas fa-info-circle"></i> Агент</div><div class="card-body">' +
        '<div class="kv">' +
        '<div class="k">Название</div><div>' + esc(s.agentName) + '</div>' +
        '<div class="k">Порт</div><div>' + s.httpPort + '</div>' +
        '<div class="k">Часовой пояс</div><div>' + esc(s.timezone) + '</div>' +
        '<div class="k">Каталог данных</div><div><code>' + esc(s.dataDir) + '</code></div>' +
        '<div class="k">Тик планировщика</div><div>' + s.tickSeconds + ' с</div>' +
        '<div class="k">Интервал по умолчанию</div><div>' + s.defaultIntervalMinutes + ' мин</div>' +
        '<div class="k">Параллельных проверок</div><div>' + s.maxParallelChecks + '</div>' +
        '</div></div></div>';

      html += '<div class="card"><div class="card-head"><i class="fas fa-broadcast-tower"></i> Zabbix</div><div class="card-body">' +
        '<div class="kv">' +
        '<div class="k">Отправка</div><div>' + (s.zabbixEnabled ? 'включена' : 'выключена') + '</div>' +
        '<div class="k">Сервер</div><div>' + (s.zabbixServer ? esc(s.zabbixServer) + ':' + s.zabbixPort : '—') + '</div>' +
        '<div class="k">API</div><div>' + (s.zabbixApiEnabled ? 'включено' : 'выключено') +
          (s.zabbixAutoCreateHosts ? ' · автосоздание хостов' : '') + '</div>' +
        '</div></div></div>';

      html += '<div class="card"><div class="card-head"><i class="fas fa-network-wired"></i> Удалённые агенты' +
        '<span class="spacer"></span><span class="muted" style="font-size:11px" id="agents-count"></span>' +
        '</div><div class="card-body" style="padding:0" id="agents-box">' +
        '<div style="padding:16px" class="muted">Загрузка…</div></div></div>';

      html += '<div class="card"><div class="card-head"><i class="fas fa-palette"></i> Оформление (бренд)' +
        '<span class="spacer"></span>' +
        (state.me && state.me.canEditBranding ? '<button class="btn btn-sm" id="brand-open"><i class="fas fa-pen"></i> Изменить</button>' : '') +
        '</div><div class="card-body" id="brand-box">' +
        '<div class="muted">Загрузка…</div></div></div>';

      html += '<div class="card"><div class="card-head"><i class="fas fa-key"></i> Смена пароля</div><div class="card-body">' +
        '<form id="pwd-form" style="max-width:420px">' +
        '<label style="display:block;margin-bottom:4px;font-weight:600;color:var(--ink-soft)">Новый пароль (минимум 8 символов)</label>' +
        '<input type="password" id="pwd-new" required minlength="8" ' +
        'style="width:100%;padding:7px 10px;border:1px solid var(--border);border-radius:6px;background:var(--surface);color:var(--ink);margin-bottom:10px">' +
        '<button type="submit" class="btn btn-primary"><i class="fas fa-save"></i> Сохранить</button>' +
        '</form></div></div>';

      if (s.configErrors && s.configErrors.length) {
        html += '<div class="card"><div class="card-head"><i class="fas fa-exclamation-triangle" style="color:var(--warn)"></i> Ошибки конфигурации</div>' +
          '<div class="card-body"><ul style="margin:0;padding-left:18px">' +
          s.configErrors.map(function (e) { return '<li>' + esc(e) + '</li>'; }).join('') +
          '</ul></div></div>';
      }

      html += '<div class="card"><div class="card-head"><i class="fas fa-plug"></i> Поддерживаемые источники</div><div class="card-body">' +
        (s.collectorTypes || []).map(function (t) { return '<span class="tag">' + esc(t) + '</span>'; }).join(' ') +
        '</div></div>';

      view.innerHTML = html;

      loadAgents();
      loadBrandingBox();

      document.getElementById('pwd-form').addEventListener('submit', function (e) {
        e.preventDefault();
        var pwd = document.getElementById('pwd-new').value;
        api('/api/password', { method: 'POST', body: { username: 'admin', password: pwd } })
          .then(function () {
            toast('Пароль изменён', 'ok');
            document.getElementById('pwd-new').value = '';
          })
          .catch(function (err) { toast('Ошибка: ' + err.message, 'error'); });
      });

    }).catch(function (err) {
      view.innerHTML = '<div class="card"><div class="card-body">Ошибка: ' + esc(err.message) + '</div></div>';
    });
  }

  // ------------------------------------------------------------------ форма компании

  // Шум, который есть почти на каждом сервере (совпадает со списком в
  // EventLogCollector.DefaultIgnore). Формат: "Источник/Id" или просто "Источник".
  var EVENTLOG_DEFAULT_IGNORE = [
    'SideBySide/59',
    'Microsoft-Windows-DistributedCOM/10010',
    'Microsoft-Windows-DistributedCOM/10016',
    'Perflib/1008',
    'Perflib/1020'
  ];

  var SOURCE_TYPES = [
    { id: 'veeam-rest',   label: 'Veeam B&R (REST API)',  hint: 'Veeam 12+ с включённой службой REST API' },
    { id: 'zip',          label: 'ZIP-архив',             hint: 'Проверка целостности архива (CRC)' },
    { id: 'file-age',     label: 'Файл / папка',          hint: 'Свежесть файлов по маске' },
    { id: 'veeam-files',  label: 'Veeam (файлы)',         hint: 'Репозиторий .vbk/.vib без API' },
    { id: 'iperius-ftp',  label: 'Iperius (FTP)',         hint: 'Архивы на FTP-сервере' },
    { id: 'http',         label: 'HTTP(S) — доступность', hint: 'Код ответа, ключевое слово, время отклика' },
    { id: 'tcp',          label: 'TCP-порт — доступность',hint: 'Открыт ли порт' },
    { id: 'ping',         label: 'Ping (ICMP)',           hint: 'Отвечает ли хост' },
    { id: 'dns',          label: 'DNS-запись',            hint: 'Разрешается ли имя' },
    { id: 'push',         label: 'Push-heartbeat',        hint: 'Сервис сам пингует нас' },
    { id: 'smart',        label: 'SMART дисков',          hint: 'Износ SSD, температура, ошибки, часы работы' },
    { id: 'raid',         label: 'RAID-контроллер',       hint: 'Состояние томов, дисков и батареи' },
    { id: 'hardware',     label: 'Температуры / вентиляторы', hint: 'IPMI, WMI ACPI — нагрев и обороты' },
    { id: 'hyperv',       label: 'ВМ Hyper-V',            hint: 'Запущены ли машины, снимки, heartbeat' },
    { id: 'mikrotik',     label: 'MikroTik (RouterOS)',   hint: 'Версия прошивки, uptime, CPU, температура' },
    { id: 'esxi',         label: 'VMware ESXi',           hint: 'Хост, ВМ, хранилища (SOAP)' },
    { id: 'inventory',    label: 'Инвентаризация',        hint: 'ОС, тома, службы, софт, обновления' },
    { id: 'vss',          label: 'VSS (писатели, тени)',   hint: 'Причина падений бэкапов на Windows' },
    { id: 'eventlog',     label: 'Журнал событий',         hint: 'Ошибки и критичные события Windows за N часов' },
    { id: 'netconfig',    label: 'Сеть: адреса, DNS, маршруты', hint: 'Сменился IP, отвалился шлюз, нет DNS' },
    { id: 'tasks',        label: 'Задачи планировщика',    hint: 'Бэкап по расписанию: есть ли задача, отработала ли' }
  ];

  function isUptimeType(type) {
    return type === 'http' || type === 'tcp' || type === 'ping' || type === 'dns' || type === 'push';
  }

  function emptyCompany() {
    return {
      id: '', name: '', shortName: '', tags: [], enabled: true, timezone: 'Europe/Moscow',
      contacts: [{ role: 'admin', name: '', phone: '', email: '' }],
      sources: [emptySource('veeam-rest')],
      policy: { freshnessHours: 26, minSizeMb: 500, consecutiveFailuresToFail: 2, maxSizeDropPercent: 40 },
      zabbix: { enabled: true, host: '', displayName: '' }
    };
  }

  function emptySource(type) {
    var s = { type: type, id: '', enabled: true, comment: '', intervalMinutes: 0 };

    if (type === 'veeam-rest') {
      s.url = 'https://'; s.username = ''; s.password = ''; s.apiVersion = '1.1-rev1';
      s.ignoreCertErrors = true; s.jobs = []; s.sessionTypes = ['BackupJob', 'BackupCopyJob', 'ReplicaJob'];
    } else if (type === 'iperius-ftp') {
      s.jobName = 'Iperius FTP';
      s.ftp = { host: '', port: 21, username: '', password: '', remotePath: '/', useTls: false, pattern: '*.zip', maxFileAgeHours: 26, keepLocalCopy: false };
    } else if (type === 'zip') {
      s.jobName = 'Архивы ZIP'; s.paths = []; s.checkCrc = 'weekly';
    } else if (type === 'http') {
      s.jobName = ''; s.url = 'https://'; s.method = 'GET'; s.expectedStatus = '200';
      s.keyword = ''; s.ignoreCertErrors = true; s.timeoutSeconds = 10;
      s.slowWarnMs = 2000; s.slowFailMs = 10000; s.intervalMinutes = 1;
    } else if (type === 'tcp') {
      s.jobName = ''; s.host = ''; s.port = 443; s.timeoutSeconds = 5;
      s.slowWarnMs = 1000; s.slowFailMs = 5000; s.intervalMinutes = 1;
    } else if (type === 'ping') {
      s.jobName = ''; s.host = ''; s.attempts = 3; s.timeoutMs = 3000;
      s.slowWarnMs = 100; s.slowFailMs = 1000; s.intervalMinutes = 1;
    } else if (type === 'dns') {
      s.jobName = ''; s.name = ''; s.recordType = 'A'; s.expect = ''; s.timeoutSeconds = 5;
      s.intervalMinutes = 5;
    } else if (type === 'push') {
      s.jobName = 'Push'; s.pushToken = randomToken(); s.staleMinutes = 15;
      s.intervalMinutes = 1;
    } else if (type === 'smart') {
      s.jobName = 'SMART дисков'; s.checkSmartctl = true;
      s.minLifePercent = 20; s.warnTempC = 45; s.maxTempC = 55;
      s.intervalMinutes = 60;
    } else if (type === 'raid') {
      s.jobName = 'RAID';
      s.intervalMinutes = 30;
    } else if (type === 'hardware') {
      s.jobName = 'Железо'; s.useIpmi = true;
      s.bmcHost = ''; s.bmcUser = ''; s.password = ''; s.ipmiInterface = 'lanplus';
      s.bmcTransport = 'ipmi';
      s.cpuTempWarn = 75; s.cpuTempFail = 90; s.fanMinRpm = 600; s.psuFailIsCritical = true;
      s.intervalMinutes = 15;
    } else if (type === 'hyperv') {
      s.jobName = 'ВМ'; s.expectedRunning = []; s.allowStopped = [];
      s.checkpoints = { maxAgeDays: 7, maxCount: 3 };
      s.uptimeWarnMinutes = 10; s.checkHeartbeat = true;
      s.checkDiskFree = true; s.diskFreeWarnPercent = 10;
      s.intervalMinutes = 5;
    } else if (type === 'mikrotik') {
      s.jobName = 'MikroTik'; s.host = ''; s.port = 0; s.username = ''; s.password = '';
      s.useTls = true; s.transport = 'auto'; s.snmpCommunity = '';
      s.minVersion = ''; s.cpuWarnPercent = 80; s.tempWarnC = 70;
      s.uptimeWarnMinutes = 60; s.timeoutSeconds = 10;
      s.intervalMinutes = 360;
    } else if (type === 'esxi') {
      s.jobName = 'ESXi'; s.host = ''; s.port = 443; s.username = ''; s.password = '';
      s.ignoreCertErrors = true; s.timeoutSeconds = 40;
      s.expectedRunning = []; s.allowStopped = [];
      s.checkDatastores = true; s.datastoreWarnPercent = 15; s.datastoreFailPercent = 5;
      s.checkSnapshots = true; s.snapshotMaxAgeDays = 7;
      s.intervalMinutes = 5;
    } else if (type === 'inventory') {
      s.jobName = 'Инвентарь'; s.services = [];
      s.checkVolumes = true; s.volumeMinFreePercent = 10; s.checkDiskFree = 5;
      s.collectSoftware = true; s.softwareMaxItems = 60;
      s.checkHotfixes = false; s.hotfixMaxAgeDays = 60;
      s.intervalMinutes = 720;
    } else if (type === 'vss') {
      s.jobName = 'VSS';
      s.maxShadowAgeHours = 48; s.storageWarnPercent = 80; s.storageFailPercent = 95;
      s.intervalMinutes = 60;
    } else if (type === 'eventlog') {
      s.jobName = 'Журнал';
      s.logs = ['System', 'Application'];
      s.ignore = EVENTLOG_DEFAULT_IGNORE.slice();
      s.hours = 24; s.includeWarnings = false;
      s.warnCount = 5; s.failCount = 30; s.maxEvents = 500;
      s.intervalMinutes = 60;
    } else if (type === 'netconfig') {
      s.jobName = 'Сеть';
      s.checkDns = true; s.checkGateway = true; s.minLinkSpeedMbps = 0;
      s.intervalMinutes = 360;
    } else if (type === 'tasks') {
      s.jobName = 'Задачи';
      s.expect = []; s.ignore = ['\\Microsoft\\*'];
      s.autostartIgnore = [];
      s.depth = 3; s.maxLastRunAgeHours = 0; s.includeAutostart = true;
      s.intervalMinutes = 60;
    } else {
      s.jobName = type === 'veeam-files' ? 'Veeam (файлы)' : 'Файлы';
      s.paths = []; s.maxAgeHours = 26;
    }    return s;
  }

  function randomToken() {
    var chars = 'abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789';
    var out = '';
    for (var i = 0; i < 24; i++) out += chars.charAt(Math.floor(Math.random() * chars.length));
    return out;
  }

  function slugifyRu(s) {
    var map = { 'а':'a','б':'b','в':'v','г':'g','д':'d','е':'e','ё':'e','ж':'zh','з':'z','и':'i','й':'y',
      'к':'k','л':'l','м':'m','н':'n','о':'o','п':'p','р':'r','с':'s','т':'t','у':'u','ф':'f','х':'h',
      'ц':'c','ч':'ch','ш':'sh','щ':'sch','ъ':'','ы':'y','ь':'','э':'e','ю':'yu','я':'ya' };
    var out = '';
    s = (s || '').toLowerCase();
    for (var i = 0; i < s.length; i++) {
      var c = s.charAt(i);
      if (map[c] !== undefined) out += map[c];
      else if (/[a-z0-9]/.test(c)) out += c;
      else if (/[\s._\/]/.test(c)) out += '-';
    }
    return out.replace(/-+/g, '-').replace(/^-|-$/g, '').substring(0, 40);
  }

  function renderCompanyForm(companyId) {
    var isNew = !companyId || companyId === 'new';
    view.innerHTML = '<div class="muted">Загрузка…</div>';

    var load = isNew
      ? Promise.resolve(emptyCompany())
      : api('/api/company/' + encodeURIComponent(companyId) + '/raw');

    load.then(function (data) {
      var f = normalizeForForm(data);
      f._isNew = isNew;
      f._origId = isNew ? '' : companyId;

      // список удалённых агентов — для выбора, кто проверяет компанию
      api('/api/agents').then(function (res) {
        f._agents = (res && res.agents) ? res.agents : [];
        renderForm(f);
      }).catch(function () {
        f._agents = [];
        renderForm(f);
      });
    }).catch(function (err) {
      view.innerHTML = '<div class="card"><div class="card-body">Ошибка: ' + esc(err.message) + '</div></div>';
    });
  }

  function normalizeForForm(raw) {
    var f = emptyCompany();
    f.id = raw.id || '';
    f.name = raw.name || '';
    f.shortName = raw.shortName || '';
    f.tags = raw.tags || [];
    f.enabled = raw.enabled !== false;
    f.timezone = raw.timezone || 'Europe/Moscow';
    f.remoteAgent = raw.remoteAgent || '';
    f.contacts = (raw.contacts && raw.contacts.length) ? raw.contacts : [{ role: 'admin', name: '', phone: '', email: '' }];
    f.sources = (raw.sources && raw.sources.length) ? raw.sources : [emptySource('veeam-rest')];
    if (raw.policy) {
      f.policy = {
        freshnessHours: raw.policy.freshnessHours || 26,
        minSizeMb: raw.policy.minSizeMb || 0,
        consecutiveFailuresToFail: raw.policy.consecutiveFailuresToFail || 2,
        maxSizeDropPercent: raw.policy.maxSizeDropPercent || 0
      };
    }
    if (raw.zabbix) f.zabbix = raw.zabbix;
    return f;
  }

  function input(label, name, value, type, hint) {
    return '<label class="fld"><span>' + esc(label) + '</span>' +
      '<input type="' + (type || 'text') + '" data-f="' + esc(name) + '" value="' + esc(value === undefined || value === null ? '' : value) + '"' +
      (type === 'password' ? ' autocomplete="new-password"' : '') + '>' +
      (hint ? '<small>' + esc(hint) + '</small>' : '') + '</label>';
  }

  function renderForm(f) {
    var html = '';
    html += '<h2 class="page-title"><i class="fas fa-' + (f._isNew ? 'plus-circle' : 'pen') + '"></i> ' +
      (f._isNew ? 'Новая компания' : 'Редактирование: ' + esc(f.name)) + '</h2>';

    html += '<form id="company-form" autocomplete="off">';

    // ---- основное
    html += '<div class="card"><div class="card-head"><i class="fas fa-building"></i> Основное</div><div class="card-body">';
    html += '<div class="form-grid">';
    html += input('Название *', 'name', f.name);
    html += input('Короткое имя', 'shortName', f.shortName, 'text', 'для таблицы и карточек');
    html += input('ID (slug)', 'id', f.id, 'text', 'a-z, 0-9, дефис. Заполнится из названия');
    html += input('Теги', 'tags', (f.tags || []).join(', '), 'text', 'через запятую: 1С, РДП, Veeam');
    html += '</div>';
    html += '<label class="chk"><input type="checkbox" data-f="enabled"' + (f.enabled ? ' checked' : '') + '> Компания активна</label>';

    // удалённый агент: кто выполняет проверки
    var agents = f._agents || [];
    if (agents.length) {
      html += '<label class="fld" style="margin-top:10px"><span>Проверяет</span>' +
        '<select data-f="remoteAgent">' +
        '<option value=""' + (!f.remoteAgent ? ' selected' : '') + '>этот сервер (локально)</option>' +
        agents.map(function (a) {
          return '<option value="' + esc(a.id) + '"' + (f.remoteAgent === a.id ? ' selected' : '') + '>' +
            esc(a.name || a.id) + (a.online ? ' — на связи' : ' — молчит') + '</option>';
        }).join('') +
        '</select>' +
        '<small>если выбран агент, центральный сервер не проверяет компанию сам</small></label>';
    }

    html += '</div></div>';

    // ---- контакты
    html += '<div class="card"><div class="card-head"><i class="fas fa-address-book"></i> Контакт</div><div class="card-body">';
    var ct = f.contacts[0] || { name: '', phone: '', email: '' };
    html += '<div class="form-grid">';
    html += input('Имя', 'contact.name', ct.name);
    html += input('Телефон', 'contact.phone', ct.phone);
    html += input('E-mail', 'contact.email', ct.email);
    html += '</div></div></div>';

    // ---- источники
    html += '<div class="card"><div class="card-head"><i class="fas fa-plug"></i> Что проверять' +
      '<span class="spacer"></span>' +
      '<select id="add-source-type" class="sel-inline">' +
      SOURCE_TYPES.map(function (t) { return '<option value="' + t.id + '">' + esc(t.label) + '</option>'; }).join('') +
      '</select>' +
      '<button type="button" class="btn btn-sm" id="btn-add-source"><i class="fas fa-plus"></i> Источник</button>' +
      '</div><div class="card-body" id="sources-box">';

    f.sources.forEach(function (src, idx) {
      html += renderSourceBlock(src, idx, f);
    });

    html += '</div></div>';

    // ---- политика
    html += '<div class="card"><div class="card-head"><i class="fas fa-shield-alt"></i> Правила оценки</div><div class="card-body">';
    html += '<div class="form-grid">';
    html += input('Свежесть, часов', 'policy.freshnessHours', f.policy.freshnessHours, 'number', 'бэкап старше → FAIL');
    html += input('Мин. размер, МБ', 'policy.minSizeMb', f.policy.minSizeMb, 'number', '0 = не проверять');
    html += input('Провалов до FAIL', 'policy.consecutiveFailuresToFail', f.policy.consecutiveFailuresToFail, 'number');
    html += input('Падение размера, %', 'policy.maxSizeDropPercent', f.policy.maxSizeDropPercent, 'number', '0 = не проверять');
    html += '</div></div></div>';

    // ---- zabbix
    html += '<div class="card"><div class="card-head"><i class="fas fa-broadcast-tower"></i> Zabbix</div><div class="card-body">';
    html += '<div class="form-grid">';
    html += input('Хост', 'zabbix.host', f.zabbix.host, 'text', 'пусто = BC-<id>');
    html += input('Отображаемое имя', 'zabbix.displayName', f.zabbix.displayName);
    html += '</div></div></div>';

    // ---- кнопки
    html += '<div class="form-actions">' +
      '<button type="submit" class="btn btn-primary"><i class="fas fa-save"></i> Сохранить</button>' +
      '<button type="button" class="btn" id="btn-cancel">Отмена</button>' +
      (f._isNew ? '' : '<span class="spacer"></span><button type="button" class="btn btn-danger" id="btn-delete"><i class="fas fa-trash"></i> Удалить</button>') +
      '</div>';

    html += '</form>';

    view.innerHTML = html;

    // ---- обработчики
    document.getElementById('company-form').addEventListener('submit', function (e) {
      e.preventDefault();
      var collected = collectForm();
      collected._origId = f._origId;
      collected._isNew = f._isNew;
      saveCompany(collected);
    });

    document.getElementById('btn-cancel').addEventListener('click', function () {
      location.hash = f._isNew ? '#/dashboard' : '#/company/' + encodeURIComponent(f._origId);
    });

    var del = document.getElementById('btn-delete');
    if (del) del.addEventListener('click', function () {
      if (!confirm('Удалить компанию «' + f.name + '»?\n\nФайл будет перемещён в _archive, данные останутся в журнале.')) return;
      api('/api/company/' + encodeURIComponent(f._origId), { method: 'DELETE' })
        .then(function () { toast('Компания удалена', 'ok'); location.hash = '#/dashboard'; })
        .catch(function (err) { toast('Ошибка: ' + err.message, 'error'); });
    });

    document.getElementById('btn-add-source').addEventListener('click', function () {
      var cur = collectForm();
      var type = document.getElementById('add-source-type').value;
      cur.sources.push(emptySource(type));
      cur._isNew = f._isNew; cur._origId = f._origId;
      renderForm(cur);
    });

    // смена типа источника — перерисовываем блок, сохраняя общие поля
    $$('[data-src-type]').forEach(function (sel) {
      sel.addEventListener('change', function () {
        var i = parseInt(sel.getAttribute('data-src-type'), 10);
        var cur = collectForm();
        cur._isNew = f._isNew; cur._origId = f._origId;

        var old = cur.sources[i] || {};
        var fresh = emptySource(sel.value);

        // переносим общие поля
        if (old.id) fresh.id = old.id;
        if (old.jobName) fresh.jobName = old.jobName;
        fresh.enabled = old.enabled !== false;
        if (old.intervalMinutes) fresh.intervalMinutes = old.intervalMinutes;
        if (old.comment) fresh.comment = old.comment;

        cur.sources[i] = fresh;
        renderForm(cur);
      });
    });

    $$('[data-remove-source]').forEach(function (btn) {
      btn.addEventListener('click', function () {
        var i = parseInt(btn.getAttribute('data-remove-source'), 10);
        var cur = collectForm();
        cur.sources.splice(i, 1);
        if (!cur.sources.length) cur.sources.push(emptySource('veeam-rest'));
        cur._isNew = f._isNew; cur._origId = f._origId;
        renderForm(cur);
      });
    });

    $$('[data-test-veeam]').forEach(function (btn) {
      btn.addEventListener('click', function () {
        var i = parseInt(btn.getAttribute('data-test-veeam'), 10);
        testVeeam(i, f);
      });
    });

    // чекбоксы заданий синхронизируются с текстовым полем
    $$('[data-job]').forEach(function (cb) {
      cb.addEventListener('change', function () {
        var i = cb.getAttribute('data-job');
        var box = $('[data-src="' + i + '"]');
        if (!box) return;
        var sel = $$('[data-job="' + i + '"]:checked', box).map(function (x) { return x.value; });
        var field = $('[data-f="src.' + i + '.jobs"]', box);
        if (field) field.value = sel.join(', ');
      });
    });

    // авто-slug из названия
    var nameInput = document.querySelector('[data-f="name"]');
    var idInput = document.querySelector('[data-f="id"]');
    if (nameInput && idInput && f._isNew) {
      nameInput.addEventListener('input', function () {
        if (!idInput.dataset.touched) idInput.value = slugifyRu(nameInput.value);
      });
      idInput.addEventListener('input', function () { idInput.dataset.touched = '1'; });
    }
  }

  function renderSourceBlock(src, idx, f) {
    var h = '<div class="src-block" data-src="' + idx + '">';

    // тип источника — выпадающий список (можно менять после создания)
    h += '<div class="src-head">' +
      '<select class="src-type" data-src-type="' + idx + '">' +
      SOURCE_TYPES.map(function (t) {
        return '<option value="' + t.id + '"' + (src.type === t.id ? ' selected' : '') + '>' +
          esc(t.label) + '</option>';
      }).join('') + '</select>' +
      '<label class="chk" style="margin-left:10px"><input type="checkbox" data-src-enabled="' + idx + '"' +
        (src.enabled !== false ? ' checked' : '') + '> вкл</label>' +
      '<span class="spacer"></span>' +
      '<button type="button" class="btn btn-sm" data-remove-source="' + idx + '"><i class="fas fa-times"></i></button>' +
      '</div>';

    h += '<div class="form-grid">';

    if (src.type === 'veeam-rest') {
      h += input('Адрес сервера', 'src.' + idx + '.url', src.url, 'text', 'https://10.0.0.5:9419');
      h += input('Логин', 'src.' + idx + '.username', src.username);
      h += input('Пароль', 'src.' + idx + '.password', '', 'password',
        src.hasPassword ? 'задан — оставьте пустым, чтобы не менять' : 'будет зашифрован в secrets.json');
      h += input('Версия API', 'src.' + idx + '.apiVersion', src.apiVersion || '1.1-rev1');
      h += '</div>';
      h += '<label class="chk"><input type="checkbox" data-src-ignorecert="' + idx + '"' +
        (src.ignoreCertErrors !== false ? ' checked' : '') + '> Игнорировать ошибки сертификата (самоподписанный)</label>';

      h += '<div style="margin-top:10px">' +
        '<button type="button" class="btn btn-sm" data-test-veeam="' + idx + '"><i class="fas fa-plug"></i> Проверить подключение</button>' +
        '<span id="veeam-test-' + idx + '" class="test-result' + (src._testResult ? ' good' : '') + '">' +
        (src._testResult ? ' ' + esc(src._testResult) : '') + '</span>' +
        '</div>';

      h += '<div style="margin-top:10px"><label class="fld"><span>Задания</span>' +
        '<input type="text" data-f="src.' + idx + '.jobs" value="' + esc((src.jobs || []).join(', ')) + '">' +
        '<small>через запятую; пусто = все задания сервера</small></label></div>';

      if (src._jobList && src._jobList.length) {
        h += '<div class="job-picker">';
        src._jobList.forEach(function (j) {
          var checked = (src.jobs || []).indexOf(j) >= 0;
          h += '<label class="chk"><input type="checkbox" data-job="' + idx + '" value="' + esc(j) + '"' +
            (checked ? ' checked' : '') + '> ' + esc(j) + '</label>';
        });
        h += '</div>';
      }

    } else if (src.type === 'http') {
      h += input('Имя задания', 'src.' + idx + '.jobName', src.jobName, 'text', 'например: 1С веб');
      h += input('URL', 'src.' + idx + '.url', src.url, 'text', 'https://1c.alpha.ru/health');
      h += '<label class="fld"><span>Метод</span><select data-f="src.' + idx + '.method">' +
        ['GET', 'HEAD', 'POST'].map(function (v) {
          return '<option value="' + v + '"' + (src.method === v ? ' selected' : '') + '>' + v + '</option>';
        }).join('') + '</select></label>';
      h += input('Ожидаемый код', 'src.' + idx + '.expectedStatus', src.expectedStatus, 'text', '200 или 200,301');
      h += input('Ключевое слово', 'src.' + idx + '.keyword', src.keyword, 'text', 'искать в теле (пусто = не проверять)');
      h += input('Таймаут, с', 'src.' + idx + '.timeoutSeconds', src.timeoutSeconds, 'number');
      h += input('WARN от, мс', 'src.' + idx + '.slowWarnMs', src.slowWarnMs, 'number', '0 = не проверять');
      h += input('FAIL от, мс', 'src.' + idx + '.slowFailMs', src.slowFailMs, 'number', '0 = не проверять');
      h += '</div>';
      h += '<label class="chk"><input type="checkbox" data-src-ignorecert="' + idx + '"' +
        (src.ignoreCertErrors !== false ? ' checked' : '') + '> Игнорировать ошибки сертификата</label>';

    } else if (src.type === 'tcp') {
      h += input('Имя задания', 'src.' + idx + '.jobName', src.jobName, 'text', 'например: RDP');
      h += input('Хост', 'src.' + idx + '.host', src.host, 'text', '10.20.0.5');
      h += input('Порт', 'src.' + idx + '.port', src.port, 'number');
      h += input('Таймаут, с', 'src.' + idx + '.timeoutSeconds', src.timeoutSeconds, 'number');
      h += input('WARN от, мс', 'src.' + idx + '.slowWarnMs', src.slowWarnMs, 'number');
      h += input('FAIL от, мс', 'src.' + idx + '.slowFailMs', src.slowFailMs, 'number');
      h += '</div>';

    } else if (src.type === 'ping') {
      h += input('Имя задания', 'src.' + idx + '.jobName', src.jobName, 'text', 'например: Шлюз');
      h += input('Хост', 'src.' + idx + '.host', src.host, 'text', '10.20.0.1');
      h += input('Попыток', 'src.' + idx + '.attempts', src.attempts, 'number');
      h += input('Таймаут, мс', 'src.' + idx + '.timeoutMs', src.timeoutMs, 'number');
      h += input('WARN от, мс', 'src.' + idx + '.slowWarnMs', src.slowWarnMs, 'number');
      h += input('FAIL от, мс', 'src.' + idx + '.slowFailMs', src.slowFailMs, 'number');
      h += '</div>';

    } else if (src.type === 'dns') {
      h += input('Имя задания', 'src.' + idx + '.jobName', src.jobName);
      h += input('Имя', 'src.' + idx + '.name', src.name, 'text', 'alpha.ru');
      h += '<label class="fld"><span>Тип записи</span><select data-f="src.' + idx + '.recordType">' +
        ['A', 'AAAA', 'MX', 'TXT', 'CNAME'].map(function (v) {
          return '<option value="' + v + '"' + (src.recordType === v ? ' selected' : '') + '>' + v + '</option>';
        }).join('') + '</select></label>';
      h += input('Ожидаемый ответ', 'src.' + idx + '.expect', src.expect, 'text', 'необязательно');
      h += input('Таймаут, с', 'src.' + idx + '.timeoutSeconds', src.timeoutSeconds, 'number');
      h += '</div>';

    } else if (src.type === 'push') {
      h += input('Имя задания', 'src.' + idx + '.jobName', src.jobName);
      h += input('Токен', 'src.' + idx + '.pushToken', src.pushToken, 'text', 'часть URL для пинга');
      h += input('Нет пинга, мин', 'src.' + idx + '.staleMinutes', src.staleMinutes, 'number', 'через сколько FAIL');
      h += '</div>';
      h += '<div class="push-url">' +
        '<span class="muted">URL для пинга:</span> ' +
        '<code>POST ' + esc(location.origin) + '/api/push/' + esc(src.pushToken || '…') + '</code>' +
        '</div>';

    } else if (src.type === 'smart') {
      h += input('Имя задания', 'src.' + idx + '.jobName', src.jobName);
      h += input('Износ: WARN, % остатка', 'src.' + idx + '.minLifePercent', src.minLifePercent, 'number', '20 = предупредить, когда осталось 20%');
      h += input('Температура: WARN, °C', 'src.' + idx + '.warnTempC', src.warnTempC, 'number');
      h += input('Температура: FAIL, °C', 'src.' + idx + '.maxTempC', src.maxTempC, 'number');
      h += input('Тип устройства (-d)', 'src.' + idx + '.deviceType', src.deviceType, 'text', 'пусто = авто; например megaraid,0');
      h += '</div>';
      h += '<label class="chk"><input type="checkbox" data-src-smartctl="' + idx + '"' +
        (src.checkSmartctl !== false ? ' checked' : '') + '> Использовать smartctl, если найден</label>';
      h += '<p class="muted" style="font-size:11.5px;margin:8px 0 0">' +
        'Без smartctl читается WMI: износ, температура, часы работы, ошибки. ' +
        'На аппаратных RAID-контроллерах SMART обычно скрыт — нужен smartctl с <code>-d</code> ' +
        'или вендорная утилита (см. <code>docs/hardware.md</code>).</p>';

    } else if (src.type === 'raid') {
      h += input('Имя задания', 'src.' + idx + '.jobName', src.jobName);
      h += '</div>';
      h += '<p class="muted" style="font-size:11.5px;margin:8px 0 0">' +
        'Ищет <code>storcli</code>, <code>perccli</code>, <code>ssacli</code>, ' +
        '<code>arcconf</code>, <code>MegaCli</code> в PATH и стандартных папках. ' +
        'Если утилиты нет — статус «нет данных», а не ошибка.</p>';

    } else if (src.type === 'hardware') {
      h += input('Имя задания', 'src.' + idx + '.jobName', src.jobName);
      h += input('BMC: адрес', 'src.' + idx + '.bmcHost', src.bmcHost, 'text', 'например 192.168.27.10 (пусто = локально)');
      h += input('BMC: логин', 'src.' + idx + '.bmcUser', src.bmcUser, 'text', 'обычно ADMIN');
      h += input('BMC: пароль', 'src.' + idx + '.password', '', 'password',
        src.hasPassword ? 'задан — оставьте пустым' : 'будет зашифрован в secrets.json');
      h += '<label class="fld"><span>Транспорт BMC</span><select data-f="src.' + idx + '.bmcTransport">' +
        [['ipmi', 'IPMI (ipmitool, UDP 623)'], ['ssh', 'SSH / SMASH-CLP (порт 22)']].map(function (p) {
          return '<option value="' + p[0] + '"' + ((src.bmcTransport || 'ipmi') === p[0] ? ' selected' : '') + '>' + p[1] + '</option>';
        }).join('') + '</select></label>';
      h += '<label class="fld"><span>Интерфейс IPMI</span><select data-f="src.' + idx + '.ipmiInterface">' +
        ['lanplus', 'lan'].map(function (v) {
          return '<option value="' + v + '"' + (src.ipmiInterface === v ? ' selected' : '') + '>' + v + '</option>';
        }).join('') + '</select></label>';
      h += input('Температура: WARN, °C', 'src.' + idx + '.cpuTempWarn', src.cpuTempWarn, 'number');
      h += input('Температура: FAIL, °C', 'src.' + idx + '.cpuTempFail', src.cpuTempFail, 'number');
      h += input('Минимум вентилятора, об/мин', 'src.' + idx + '.fanMinRpm', src.fanMinRpm, 'number');
      h += '</div>';
      h += '<label class="chk"><input type="checkbox" data-src-ipmi="' + idx + '"' +
        (src.useIpmi !== false ? ' checked' : '') + '> Проверять датчики через BMC</label>';
      h += '<label class="chk"><input type="checkbox" data-src-psu="' + idx + '"' +
        (src.psuFailIsCritical !== false ? ' checked' : '') + '> Сбой блока питания = FAIL (иначе WARN)</label>';
      h += '<p class="muted" style="font-size:11.5px;margin:8px 0 0">' +
        'Источники по порядку: BMC (IPMI или SSH) → WMI ACPI → LibreHardwareMonitor. ' +
        'Для <b>IPMI</b> нужен <code>ipmitool.exe</code> и открытый <b>UDP 623</b> на BMC. ' +
        'Если IPMI over LAN выключен, но на BMC открыт <b>SSH (22)</b> — выберите транспорт ' +
        '<b>SSH / SMASH-CLP</b>: тот же набор датчиков без проброса UDP. ' +
        'Агент должен работать в сети, где виден BMC. ' +
        'На виртуальной машине датчиков нет — проверка честно скажет об этом.</p>';

    } else if (src.type === 'hyperv') {
      h += input('Имя задания', 'src.' + idx + '.jobName', src.jobName);
      h += '<label class="fld"><span>Обязаны работать (по одной в строке)</span>' +
        '<textarea rows="3" data-f="src.' + idx + '.expectedRunning">' +
        esc((src.expectedRunning || []).join('\n')) + '</textarea>' +
        '<small>если такая ВМ не Running → FAIL; пусто = следим только за состоянием</small></label>';
      h += '<label class="fld"><span>Можно держать остановленными</span>' +
        '<textarea rows="2" data-f="src.' + idx + '.allowStopped">' +
        esc((src.allowStopped || []).join('\n')) + '</textarea>' +
        '<small>тестовые и шаблонные ВМ — не считать проблемой</small></label>';
      var cp = src.checkpoints || {};
      h += input('Снимок: макс. возраст, дней', 'src.' + idx + '.checkpoints.maxAgeDays', cp.maxAgeDays === undefined ? 7 : cp.maxAgeDays, 'number', '0 = не проверять');
      h += input('Снимков: максимум', 'src.' + idx + '.checkpoints.maxCount', cp.maxCount === undefined ? 3 : cp.maxCount, 'number', '0 = не проверять');
      h += input('Uptime WARN, мин', 'src.' + idx + '.uptimeWarnMinutes', src.uptimeWarnMinutes, 'number', 'меньше → возможно циклические перезагрузки');
      h += input('Свободно на томе, %', 'src.' + idx + '.diskFreeWarnPercent', src.diskFreeWarnPercent, 'number', 'ниже → WARN');
      h += '</div>';
      h += '<label class="chk"><input type="checkbox" data-src-heartbeat="' + idx + '"' +
        (src.checkHeartbeat !== false ? ' checked' : '') + '> Проверять heartbeat (связь с гостем)</label>';
      h += '<label class="chk"><input type="checkbox" data-src-diskfree="' + idx + '"' +
        (src.checkDiskFree !== false ? ' checked' : '') + '> Проверять свободное место на томах с .vhdx</label>';
      h += '<p class="muted" style="font-size:11.5px;margin:8px 0 0">' +
        'Читается локально через WMI <code>root\\virtualization\\v2</code>. ' +
        'Служба агента работает как LocalSystem, поэтому права на Hyper-V есть. ' +
        'Каждая ВМ — отдельное задание.</p>';

    } else if (src.type === 'mikrotik') {
      h += input('Имя задания', 'src.' + idx + '.jobName', src.jobName);
      h += input('Адрес роутера', 'src.' + idx + '.host', src.host, 'text', '10.20.0.1 или 192.168.27.1');
      h += input('Порт', 'src.' + idx + '.port', src.port, 'number', '0 = авто (443 → 8728 → 161)');
      h += input('Логин', 'src.' + idx + '.username', src.username, 'text', 'учётка только на чтение');
      h += input('Пароль', 'src.' + idx + '.password', '', 'password',
        src.hasPassword ? 'задан — оставьте пустым' : 'будет зашифрован в secrets.json');
      h += '<label class="fld"><span>Транспорт</span><select data-f="src.' + idx + '.transport">' +
        [['auto', 'авто (REST → API → SNMP → MNDP)'], ['rest', 'REST (443)'], ['api', 'API (8728)'],
         ['snmp', 'SNMP (161)'], ['mndp', 'MNDP (UDP 5678, без авторизации)']]
          .map(function (v) {
            return '<option value="' + v[0] + '"' + (src.transport === v[0] ? ' selected' : '') + '>' + v[1] + '</option>';
          }).join('') + '</select></label>';
      h += input('SNMP community', 'src.' + idx + '.snmpCommunity', src.snmpCommunity, 'text', 'только если используете SNMP');
      h += input('Минимальная версия', 'src.' + idx + '.minVersion', src.minVersion, 'text', 'например 7.10');
      h += input('CPU WARN, %', 'src.' + idx + '.cpuWarnPercent', src.cpuWarnPercent, 'number');
      h += input('Температура WARN, °C', 'src.' + idx + '.tempWarnC', src.tempWarnC, 'number');
      h += input('Uptime WARN, мин', 'src.' + idx + '.uptimeWarnMinutes', src.uptimeWarnMinutes, 'number', 'меньше → возможно перезагрузки');
      h += input('Таймаут, с', 'src.' + idx + '.timeoutSeconds', src.timeoutSeconds, 'number');
      h += '</div>';
      h += '<label class="chk"><input type="checkbox" data-src-tls="' + idx + '"' +
        (src.useTls !== false ? ' checked' : '') + '> Использовать HTTPS/TLS</label>';
      h += '<p class="muted" style="font-size:11.5px;margin:8px 0 0">' +
        'Читает только: <code>system resource</code>, <code>identity</code>, ' +
        '<code>routerboard</code>, <code>health</code>. Ничего не меняет. ' +
        'На роутере нужен <code>read</code>-доступ и включённый REST/API/SNMP. ' +
        'Подробнее: <code>docs/mikrotik.md</code>.</p>';

    } else if (src.type === 'esxi') {
      h += input('Имя задания', 'src.' + idx + '.jobName', src.jobName);
      h += input('Адрес', 'src.' + idx + '.host', src.host, 'text', '192.168.27.43 или vCenter');
      h += input('Порт', 'src.' + idx + '.port', src.port || 443, 'number');
      h += input('Логин', 'src.' + idx + '.username', src.username, 'text', 'учётка Read-Only');
      h += input('Пароль', 'src.' + idx + '.password', '', 'password',
        src.hasPassword ? 'задан — оставьте пустым' : 'будет зашифрован в secrets.json');
      h += input('Таймаут, с', 'src.' + idx + '.timeoutSeconds', src.timeoutSeconds || 40, 'number');
      h += '<label class="fld"><span>Обязаны работать (по одной в строке)</span>' +
        '<textarea rows="3" data-f="src.' + idx + '.expectedRunning">' +
        esc((src.expectedRunning || []).join('\n')) + '</textarea>' +
        '<small>если такая ВМ не poweredOn → FAIL; пусто = следим только за состоянием</small></label>';
      h += '<label class="fld"><span>Можно держать выключенными</span>' +
        '<textarea rows="2" data-f="src.' + idx + '.allowStopped">' +
        esc((src.allowStopped || []).join('\n')) + '</textarea></label>';
      h += input('Хранилище: WARN, % свободно', 'src.' + idx + '.datastoreWarnPercent', src.datastoreWarnPercent, 'number');
      h += input('Хранилище: FAIL, % свободно', 'src.' + idx + '.datastoreFailPercent', src.datastoreFailPercent, 'number');
      h += input('Снимок: макс. возраст, дней', 'src.' + idx + '.snapshotMaxAgeDays', src.snapshotMaxAgeDays, 'number', '0 = не проверять');
      h += '</div>';
      h += '<label class="chk"><input type="checkbox" data-src-ignorecert="' + idx + '"' +
        (src.ignoreCertErrors !== false ? ' checked' : '') + '> Игнорировать ошибки сертификата</label>';
      h += '<label class="chk"><input type="checkbox" data-src-datastores="' + idx + '"' +
        (src.checkDatastores !== false ? ' checked' : '') + '> Проверять свободное место на хранилищах</label>';
      h += '<label class="chk"><input type="checkbox" data-src-snapshots="' + idx + '"' +
        (src.checkSnapshots !== false ? ' checked' : '') + '> Проверять снимки ВМ</label>';
      h += '<label class="chk"><input type="checkbox" data-src-smart="' + idx + '"' +
        (src.checkSmart ? ' checked' : '') + '> Проверять SMART дисков (по SSH)</label>';
      h += '<div class="form-grid" id="smart-fields-' + idx + '"' +
        (src.checkSmart ? '' : ' style="display:none"') + '>';
      h += input('SSH-логин', 'src.' + idx + '.sshUser', src.sshUser, 'text', 'пусто = как у SOAP');
      h += input('SSH-пароль', 'src.' + idx + '.sshPassword', '', 'password',
        src.hasSshPassword ? 'задан — оставьте пустым' : 'будет зашифрован');
      h += input('SSH-порт', 'src.' + idx + '.sshPort', src.sshPort || 22, 'number');
      h += input('Температура: WARN, °C', 'src.' + idx + '.smartWarnTempC', src.smartWarnTempC || 50, 'number');
      h += input('Температура: FAIL, °C', 'src.' + idx + '.smartMaxTempC', src.smartMaxTempC || 60, 'number');
      h += '</div>';
      h += '<p class="muted" style="font-size:11.5px;margin:8px 0 0">' +
        'Работает по SOAP (<code>/sdk</code>); SMART — дополнительно по SSH через ' +
        '<code>esxcli</code>, потому что в SOAP этих данных нет. ' +
        'Нужен <code>plink.exe</code> в <code>tools\\</code> и включённый SSH на хосте. ' +
        'Подробнее: <code>docs/esxi.md</code>.</p>';

    } else if (src.type === 'inventory') {
      h += input('Имя задания', 'src.' + idx + '.jobName', src.jobName);
      h += '<label class="fld"><span>Службы (должны быть запущены)</span>' +
        '<textarea rows="3" data-f="src.' + idx + '.services">' +
        esc((src.services || []).join('\n')) + '</textarea>' +
        '<small>короткие имена: VeeamBackupSvc, SQLSERVERAGENT, Spooler</small></label>';
      h += input('Свободно на томе: WARN, %', 'src.' + idx + '.volumeMinFreePercent', src.volumeMinFreePercent, 'number');
      h += input('Свободно на томе: FAIL, %', 'src.' + idx + '.checkDiskFree', src.checkDiskFree, 'number');
      h += input('Программ в деталях', 'src.' + idx + '.softwareMaxItems', src.softwareMaxItems, 'number', 'сколько названий хранить');
      h += input('Обновления: порог, дней', 'src.' + idx + '.hotfixMaxAgeDays', src.hotfixMaxAgeDays, 'number', 'WARN, если не обновлялась дольше');
      h += '</div>';
      h += '<label class="chk"><input type="checkbox" data-src-volumes="' + idx + '"' +
        (src.checkVolumes !== false ? ' checked' : '') + '> Проверять свободное место на дисках</label>';
      h += '<label class="chk"><input type="checkbox" data-src-software="' + idx + '"' +
        (src.collectSoftware !== false ? ' checked' : '') + '> Собирать список программ</label>';
      h += '<label class="chk"><input type="checkbox" data-src-hotfixes="' + idx + '"' +
        (src.checkHotfixes ? ' checked' : '') + '> Проверять обновления Windows</label>';
      h += '<p class="muted" style="font-size:11.5px;margin:8px 0 0">' +
        'Собирается локально: ОС, модель, ядра, память, uptime, тома, службы, ' +
        'программы, обновления. Работает и на удалённом агенте. ' +
        'Подробнее: <code>docs/inventory.md</code>.</p>';

    } else if (src.type === 'vss') {
      h += input('Имя задания', 'src.' + idx + '.jobName', src.jobName);
      h += input('Снимок: макс. возраст, ч', 'src.' + idx + '.maxShadowAgeHours', src.maxShadowAgeHours, 'number', 'WARN, если старее — возможно завис');
      h += input('Хранилище: WARN, %', 'src.' + idx + '.storageWarnPercent', src.storageWarnPercent, 'number');
      h += input('Хранилище: FAIL, %', 'src.' + idx + '.storageFailPercent', src.storageFailPercent, 'number');
      h += '</div>';
      h += '<p class="muted" style="font-size:11.5px;margin:8px 0 0">' +
        'Читает <code>vssadmin list writers</code> и теневые копии через WMI. ' +
        'Писатель в состоянии <b>Failed</b> — самая частая причина, по которой ' +
        'бэкап не создаётся. Только чтение, ничего не меняет. ' +
        'Подробнее: <code>docs/management.md</code>.</p>';

    } else if (src.type === 'eventlog') {
      h += input('Имя задания', 'src.' + idx + '.jobName', src.jobName);
      h += input('За сколько часов', 'src.' + idx + '.hours', src.hours, 'number', 'максимум 168 (неделя)');
      h += input('Ошибок: WARN больше', 'src.' + idx + '.warnCount', src.warnCount, 'number');
      h += input('Ошибок: FAIL больше', 'src.' + idx + '.failCount', src.failCount, 'number');
      h += input('Событий не больше', 'src.' + idx + '.maxEvents', src.maxEvents, 'number', 'предохранитель на журнал');
      h += '</div>';
      h += '<label class="fld" style="display:block;margin-top:8px"><span>Журналы</span>' +
        '<textarea rows="2" data-f="src.' + idx + '.logs">' +
        esc((src.logs || []).join('\n')) + '</textarea>' +
        '<small>по одному в строке: System, Application, Setup</small></label>';
      h += '<label class="fld" style="display:block;margin-top:8px"><span>Не считать ошибкой</span>' +
        '<textarea rows="5" data-f="src.' + idx + '.ignore">' +
        esc((src.ignore || []).join('\n')) + '</textarea>' +
        '<small>формат «Источник/Id» или просто «Источник»; пусто = не игнорировать ничего</small></label>';
      h += '<label class="chk"><input type="checkbox" data-src-warnings="' + idx + '"' +
        (src.includeWarnings ? ' checked' : '') + '> Считать и предупреждения (Level&nbsp;3)</label>';
      h += '<p class="muted" style="font-size:11.5px;margin:8px 0 0">' +
        'Читает журналы событий через <code>EventLogReader</code>: ошибки и критичные ' +
        'события за N часов. Показывает, что именно падало (VSS, ESENT, Disk, службы). ' +
        'Только чтение. Подробнее: <code>docs/eventlog.md</code>.</p>';

    } else if (src.type === 'netconfig') {
      h += input('Имя задания', 'src.' + idx + '.jobName', src.jobName);
      h += input('Скорость не ниже, Мбит/с', 'src.' + idx + '.minLinkSpeedMbps', src.minLinkSpeedMbps, 'number', '0 = не проверять');
      h += '</div>';
      h += '<label class="chk"><input type="checkbox" data-src-netdns="' + idx + '"' +
        (src.checkDns !== false ? ' checked' : '') + '> Проверять, что DNS-серверы заданы</label>';
      h += '<label class="chk"><input type="checkbox" data-src-netgw="' + idx + '"' +
        (src.checkGateway !== false ? ' checked' : '') + '> Проверять маршрут по умолчанию</label>';
      h += '<p class="muted" style="font-size:11.5px;margin:8px 0 0">' +
        'Читает через WMI: адаптеры, IP, маски, шлюзы, DNS, скорость канала и ' +
        'маршруты по умолчанию. Ловит APIPA (<code>169.254.x</code>), отсутствие ' +
        'шлюза или DNS. Только чтение. Подробнее: <code>docs/netconfig.md</code>.</p>';

    } else if (src.type === 'tasks') {
      h += input('Имя задания', 'src.' + idx + '.jobName', src.jobName);
      h += input('Глубина папок', 'src.' + idx + '.depth', src.depth, 'number', 'сколько уровней обходить');
      h += input('Не запускалась дольше, ч', 'src.' + idx + '.maxLastRunAgeHours', src.maxLastRunAgeHours, 'number', '0 = не проверять');
      h += '</div>';
      h += '<label class="fld" style="display:block;margin-top:8px"><span>Ожидаемые задачи</span>' +
        '<textarea rows="3" data-f="src.' + idx + '.expect">' +
        esc((src.expect || []).join('\n')) + '</textarea>' +
        '<small>полный путь через слеши: <code>\\Backup\\Nightly</code> — если задачи нет или она отключена, будет FAIL</small></label>';
      h += '<label class="fld" style="display:block;margin-top:8px"><span>Не показывать</span>' +
        '<textarea rows="2" data-f="src.' + idx + '.ignore">' +
        esc((src.ignore || []).join('\n')) + '</textarea>' +
        '<small>по префиксу пути: <code>\\Microsoft\\*</code></small></label>';
      h += '<label class="chk"><input type="checkbox" data-src-autorun="' + idx + '"' +
        (src.includeAutostart !== false ? ' checked' : '') + '> Собирать автозагрузку</label>';
      h += '<p class="muted" style="font-size:11.5px;margin:8px 0 0">' +
        'Читает планировщик заданий через <code>Schedule.Service</code> и ветки ' +
        '<code>Run</code>/<code>RunOnce</code> + папки «Автозагрузка». ' +
        'Показывает состояние, последний код возврата и что именно запускается. ' +
        'Только чтение. Подробнее: <code>docs/tasks.md</code>.</p>';

    } else if (src.type === 'iperius-ftp') {
      var ftp = src.ftp || {};
      h += input('Имя задания', 'src.' + idx + '.jobName', src.jobName);
      h += input('FTP-хост', 'src.' + idx + '.ftp.host', ftp.host);
      h += input('Порт', 'src.' + idx + '.ftp.port', ftp.port || 21, 'number');
      h += input('Логин', 'src.' + idx + '.ftp.username', ftp.username);
      h += input('Пароль', 'src.' + idx + '.ftp.password', '', 'password',
        ftp.hasPassword ? 'задан — оставьте пустым' : 'будет зашифрован');
      h += input('Путь', 'src.' + idx + '.ftp.remotePath', ftp.remotePath || '/');
      h += input('Маска', 'src.' + idx + '.ftp.pattern', ftp.pattern || '*.zip');
      h += input('Макс. возраст, ч', 'src.' + idx + '.ftp.maxFileAgeHours', ftp.maxFileAgeHours || 26, 'number');
      h += '</div>';
      h += '<label class="chk"><input type="checkbox" data-src-ftptls="' + idx + '"' +
        (ftp.useTls ? ' checked' : '') + '> Использовать TLS (FTPS)</label>';

    } else {
      h += input('Имя задания', 'src.' + idx + '.jobName', src.jobName);
      h += '</div>';

      h += '<label class="fld" style="margin-top:8px"><span>Пути (по одному в строке)</span>' +
        '<textarea rows="3" data-f="src.' + idx + '.paths">' + esc((src.paths || []).join('\n')) + '</textarea>' +
        '<small>например: D:\\Backups\\1C\\*.zip или \\\\srv\\share\\*.bak</small></label>';

      h += '<div class="form-grid" style="margin-top:8px">';
      if (src.type === 'zip') {
        h += '<label class="fld"><span>Проверка CRC</span><select data-f="src.' + idx + '.checkCrc">' +
          ['always', 'weekly', 'never'].map(function (v) {
            return '<option value="' + v + '"' + (src.checkCrc === v ? ' selected' : '') + '>' +
              ({ always: 'каждый раз', weekly: 'раз в неделю', never: 'только размер и дата' })[v] + '</option>';
          }).join('') + '</select></label>';
      } else {
        h += input('Макс. возраст, ч', 'src.' + idx + '.maxAgeHours', src.maxAgeHours || 26, 'number');
      }
      h += '</div>';
    }

    h += '</div>';
    return h;
  }

  function collectForm() {
    var f = { contacts: [{ role: 'admin', name: '', phone: '', email: '' }], sources: [] };

    $$('[data-f]').forEach(function (el) {
      var path = el.getAttribute('data-f');
      var val = el.value;

      if (path === 'tags') { f.tags = val.split(',').map(function (s) { return s.trim(); }).filter(Boolean); return; }
      if (path === 'enabled') { f.enabled = el.checked; return; }
      if (path.indexOf('contact.') === 0) { f.contacts[0][path.substring(8)] = val; return; }
      if (path.indexOf('policy.') === 0) {
        f.policy = f.policy || {};
        f.policy[path.substring(7)] = parseInt(val, 10) || 0;
        return;
      }
      if (path.indexOf('zabbix.') === 0) {
        f.zabbix = f.zabbix || { enabled: true };
        f.zabbix[path.substring(7)] = val;
        return;
      }
      if (path === 'name') f.name = val;
      else if (path === 'shortName') f.shortName = val;
      else if (path === 'id') f.id = val;
      else if (path === 'remoteAgent') f.remoteAgent = val;
    });

    // источники
    $$('[data-src]').forEach(function (block) {
      var idx = parseInt(block.getAttribute('data-src'), 10);
      var src = {};

      $$('[data-f^="src.' + idx + '."]', block).forEach(function (el) {
        var path = el.getAttribute('data-f').substring(('src.' + idx + '.').length);
        var val = el.value;

        if (path === 'paths') { src.paths = val.split('\n').map(function (s) { return s.trim(); }).filter(Boolean); }
        else if (path === 'jobs') { src.jobs = val.split(',').map(function (s) { return s.trim(); }).filter(Boolean); }
        else if (path === 'expectedRunning' || path === 'allowStopped' || path === 'services' ||
                 path === 'logs' || path === 'ignore' || path === 'expect' ||
                 path === 'autostartIgnore') {
          src[path] = val.split('\n').map(function (s) { return s.trim(); }).filter(Boolean);
        }
        else if (path.indexOf('checkpoints.') === 0) {
          src.checkpoints = src.checkpoints || {};
          src.checkpoints[path.substring('checkpoints.'.length)] = parseInt(val, 10) || 0;
        }
        else if (path === 'ftp.port' || path === 'ftp.maxFileAgeHours') {
          src.ftp = src.ftp || {}; src.ftp[path.substring(4)] = parseInt(val, 10) || 0;
        }
        else if (path.indexOf('ftp.') === 0) { src.ftp = src.ftp || {}; src.ftp[path.substring(4)] = val; }
        else { src[path] = val; }
      });

      var en = $('[data-src-enabled="' + idx + '"]', block);
      src.enabled = en ? en.checked : true;

      var cert = $('[data-src-ignorecert="' + idx + '"]', block);
      if (cert) src.ignoreCertErrors = cert.checked;

      var tls = $('[data-src-ftptls="' + idx + '"]', block);
      if (tls) { src.ftp = src.ftp || {}; src.ftp.useTls = tls.checked; }

      var smartctl = $('[data-src-smartctl="' + idx + '"]', block);
      if (smartctl) src.checkSmartctl = smartctl.checked;

      var ipmi = $('[data-src-ipmi="' + idx + '"]', block);
      if (ipmi) src.useIpmi = ipmi.checked;

      var psu = $('[data-src-psu="' + idx + '"]', block);
      if (psu) src.psuFailIsCritical = psu.checked;

      var hb = $('[data-src-heartbeat="' + idx + '"]', block);
      if (hb) src.checkHeartbeat = hb.checked;

      var df = $('[data-src-diskfree="' + idx + '"]', block);
      if (df) src.checkDiskFree = df.checked;

      var tls2 = $('[data-src-tls="' + idx + '"]', block);
      if (tls2) src.useTls = tls2.checked;

      var ds = $('[data-src-datastores="' + idx + '"]', block);
      if (ds) src.checkDatastores = ds.checked;

      var sn = $('[data-src-snapshots="' + idx + '"]', block);
      if (sn) src.checkSnapshots = sn.checked;

      var vol = $('[data-src-volumes="' + idx + '"]', block);
      if (vol) src.checkVolumes = vol.checked;

      var sw = $('[data-src-software="' + idx + '"]', block);
      if (sw) src.collectSoftware = sw.checked;

      var hf = $('[data-src-hotfixes="' + idx + '"]', block);
      if (hf) src.checkHotfixes = hf.checked;

      var warn = $('[data-src-warnings="' + idx + '"]', block);
      if (warn) src.includeWarnings = warn.checked;

      var netDns = $('[data-src-netdns="' + idx + '"]', block);
      if (netDns) src.checkDns = netDns.checked;

      var netGw = $('[data-src-netgw="' + idx + '"]', block);
      if (netGw) src.checkGateway = netGw.checked;

      var autorun = $('[data-src-autorun="' + idx + '"]', block);
      if (autorun) src.includeAutostart = autorun.checked;

      var smart = $('[data-src-smart="' + idx + '"]', block);
      if (smart) src.checkSmart = smart.checked;

      // тип берём из выпадающего списка (явно, без эвристики)
      var typeSel = $('[data-src-type="' + idx + '"]', block);
      src.type = typeSel ? typeSel.value : (src.type || '');

      if (src.type) f.sources.push(src);
    });

    f.timezone = 'Europe/Moscow';
    f.policy = f.policy || { freshnessHours: 26, minSizeMb: 0, consecutiveFailuresToFail: 2, maxSizeDropPercent: 0 };
    f.zabbix = f.zabbix || { enabled: true };
    f.zabbix.enabled = true;

    return f;
  }

  function saveCompany(f) {
    var isNew = !f._origId;
    var url = isNew ? '/api/company' : '/api/company/' + encodeURIComponent(f._origId);
    var method = isNew ? 'POST' : 'PUT';

    api(url, { method: method, body: f }).then(function (res) {
      var msg = isNew ? 'Компания создана' : 'Сохранено';
      if (res.warnings && res.warnings.length) msg += ' · ' + res.warnings.join('; ');
      toast(msg, 'ok');
      location.hash = '#/company/' + encodeURIComponent(res.id);
    }).catch(function (err) {
      toast('Ошибка: ' + err.message, 'error');
    });
  }
  function testVeeam(idx, f) {
    var block = $('[data-src="' + idx + '"]');
    if (!block) return;

    var url = $('[data-f="src.' + idx + '.url"]', block).value;
    var user = $('[data-f="src.' + idx + '.username"]', block).value;
    var pwd = $('[data-f="src.' + idx + '.password"]', block).value;
    var apiVer = $('[data-f="src.' + idx + '.apiVersion"]', block).value;
    var cert = $('[data-src-ignorecert="' + idx + '"]', block);

    var out = document.getElementById('veeam-test-' + idx);
    out.textContent = ' проверяю…';
    out.className = 'test-result';

    var body = {
      url: url, username: user, password: pwd, apiVersion: apiVer,
      ignoreCertErrors: cert ? cert.checked : true
    };

    // при редактировании пароль может быть не введён — берём сохранённый
    var src = f.sources[idx];
    if (!pwd && src && src.passwordSecretKey) body.passwordSecret = src.passwordSecretKey;

    api('/api/veeam/test', { method: 'POST', body: body }).then(function (res) {
      if (!res.ok) {
        out.textContent = ' ' + (res.error || 'ошибка');
        out.className = 'test-result bad';
        return;
      }
      out.textContent = ' ' + (res.server || 'OK') + ' · заданий: ' + (res.jobs || []).length;
      out.className = 'test-result good';

      // показать чекбоксы заданий, сохранив введённые значения
      var cur = collectForm();
      cur.sources[idx]._jobList = res.jobs || [];
      cur.sources[idx].password = pwd;              // не теряем введённый пароль
      cur.sources[idx].hasPassword = !!pwd;
      cur.sources[idx]._testResult = (res.server || 'OK') + ' · заданий: ' + (res.jobs || []).length;
      cur._isNew = f._isNew; cur._origId = f._origId;
      renderForm(cur);
    }).catch(function (err) {
      out.textContent = ' ' + err.message;
      out.className = 'test-result bad';
    });
  }

  // ------------------------------------------------------------------ пользователи

  function renderUsers() {
    view.innerHTML = '<div class="muted">Загрузка…</div>';

    api('/api/users').then(function (data) {
      var users = data.users || [];
      var roles = data.roles || [];
      var me = data.currentUser || '';

      var html = '<h2 class="page-title"><i class="fas fa-users"></i> Пользователи</h2>';

      html += '<div class="card"><div class="card-head">' +
        '<i class="fas fa-user-plus"></i> Учётные записи' +
        '<span class="spacer"></span>' +
        '<button class="btn btn-sm btn-primary" id="btn-add-user"><i class="fas fa-plus"></i> Добавить</button>' +
        '</div><div class="card-body" style="padding:0">';

      html += '<div class="table-wrap"><table class="journal-table"><thead><tr>' +
        '<th>Логин</th><th>Имя</th><th>Роль</th><th>Компании</th>' +
        '<th>Активен</th><th>Последний вход</th><th></th>' +
        '</tr></thead><tbody>';

      users.forEach(function (u) {
        var roleLabel = u.role;
        var roleDesc = u.roleDescription || '';
        var companies = (u.companyIds && u.companyIds.length)
          ? u.companyIds.join(', ')
          : (u.role === 'client' ? '<span class="muted">не заданы</span>' : '<span class="muted">все</span>');

        var isMe = u.username === me;

        html += '<tr>' +
          '<td><b>' + esc(u.username) + '</b>' + (isMe ? ' <span class="tag">вы</span>' : '') + '</td>' +
          '<td>' + esc(u.displayName || '—') + '</td>' +
          '<td title="' + esc(roleDesc) + '"><span class="role-badge role-' + esc(roleLabel) + '">' +
            esc(roleLabel) + '</span></td>' +
          '<td class="msg">' + companies + '</td>' +
          '<td>' + (u.enabled
            ? '<span style="color:var(--ok)"><i class="fas fa-check-circle"></i></span>'
            : '<span style="color:var(--muted)"><i class="fas fa-ban"></i></span>') +
            (u.locked ? ' <span style="color:var(--fail)" title="заблокирован после неудачных попыток">' +
              '<i class="fas fa-lock"></i></span>' : '') + '</td>' +
          '<td style="white-space:nowrap">' + (u.lastLogin ? fmtDateTime(u.lastLogin) : '—') + '</td>' +
          '<td style="white-space:nowrap">' +
            '<button class="btn btn-sm" data-edit-user="' + esc(u.username) + '"><i class="fas fa-pen"></i></button> ' +
            '<button class="btn btn-sm" data-reset-user="' + esc(u.username) + '" title="Сбросить пароль">' +
              '<i class="fas fa-key"></i></button>' +
            (isMe || u.username === 'admin' ? '' :
              ' <button class="btn btn-sm btn-danger" data-del-user="' + esc(u.username) + '" title="Удалить">' +
              '<i class="fas fa-trash"></i></button>') +
          '</td>' +
          '</tr>';
      });

      html += '</tbody></table></div></div></div>';

      // роли — справка
      html += '<div class="card"><div class="card-head"><i class="fas fa-shield-alt"></i> Роли</div><div class="card-body">';
      html += '<div class="roles-help">';
      roles.forEach(function (r) {
        html += '<div class="role-row"><span class="role-badge role-' + esc(r.id) + '">' + esc(r.id) + '</span>' +
          '<span>' + esc(r.description) + '</span></div>';
      });
      html += '</div></div></div>';

      view.innerHTML = html;

      document.getElementById('btn-add-user').addEventListener('click', function () {
        renderUserForm(null, roles);
      });

      $$('[data-edit-user]').forEach(function (b) {
        b.addEventListener('click', function () {
          var name = b.getAttribute('data-edit-user');
          var u = users.filter(function (x) { return x.username === name; })[0];
          renderUserForm(u, roles);
        });
      });

      $$('[data-reset-user]').forEach(function (b) {
        b.addEventListener('click', function () {
          var name = b.getAttribute('data-reset-user');
          if (!confirm('Сбросить пароль пользователя «' + name + '»?\n\nБудет сгенерирован новый, и все его сессии закроются.')) return;
          api('/api/users/' + encodeURIComponent(name) + '/reset', { method: 'POST', body: {} })
            .then(function (res) {
              if (res.password) {
                showPasswordModal(name, res.password);
              } else {
                toast('Пароль сброшен', 'ok');
              }
              renderUsers();
            })
            .catch(function (e) { toast('Ошибка: ' + e.message, 'error'); });
        });
      });

      $$('[data-del-user]').forEach(function (b) {
        b.addEventListener('click', function () {
          var name = b.getAttribute('data-del-user');
          if (!confirm('Удалить пользователя «' + name + '»?')) return;
          api('/api/users/' + encodeURIComponent(name), { method: 'DELETE' })
            .then(function () { toast('Удалён', 'ok'); renderUsers(); })
            .catch(function (e) { toast('Ошибка: ' + e.message, 'error'); });
        });
      });

    }).catch(function (err) {
      view.innerHTML = '<div class="card"><div class="card-body">Ошибка: ' + esc(err.message) + '</div></div>';
    });
  }

  function showPasswordModal(username, password) {
    var html = '<div class="modal-backdrop" id="pwd-modal">' +
      '<div class="modal">' +
      '<h3><i class="fas fa-key"></i> Пароль для «' + esc(username) + '»</h3>' +
      '<p class="muted" style="font-size:11.5px">Скопируйте сейчас — больше он не покажется.</p>' +
      '<div class="pwd-box"><code id="pwd-value">' + esc(password) + '</code></div>' +
      '<div class="modal-actions">' +
      '<button class="btn" id="pwd-copy"><i class="fas fa-copy"></i> Копировать</button>' +
      '<button class="btn btn-primary" id="pwd-close">Готово</button>' +
      '</div></div></div>';

    var div = document.createElement('div');
    div.innerHTML = html;
    document.body.appendChild(div.firstChild);

    document.getElementById('pwd-copy').addEventListener('click', function () {
      var t = document.getElementById('pwd-value').textContent;
      if (navigator.clipboard) {
        navigator.clipboard.writeText(t).then(function () { toast('Скопировано', 'ok'); });
      } else {
        var ta = document.createElement('textarea');
        ta.value = t; document.body.appendChild(ta); ta.select();
        try { document.execCommand('copy'); toast('Скопировано', 'ok'); } catch (e) {}
        document.body.removeChild(ta);
      }
    });

    document.getElementById('pwd-close').addEventListener('click', function () {
      var m = document.getElementById('pwd-modal');
      if (m) m.parentNode.removeChild(m);
    });
  }

  function renderUserForm(u, roles) {
    var isNew = !u;
    var f = u || { username: '', displayName: '', email: '', role: 'viewer', enabled: true, companyIds: [] };

    var html = '<h2 class="page-title"><i class="fas fa-' + (isNew ? 'user-plus' : 'user-edit') + '"></i> ' +
      (isNew ? 'Новый пользователь' : 'Пользователь: ' + esc(f.username)) + '</h2>';

    html += '<form id="user-form">';

    html += '<div class="card"><div class="card-head"><i class="fas fa-id-card"></i> Основное</div><div class="card-body">';
    html += '<div class="form-grid">';
    html += input('Логин *', 'u.username', f.username, 'text', isNew ? 'a-z, 0-9, точка, дефис' : 'изменить нельзя');
    html += input('Имя', 'u.displayName', f.displayName);
    html += input('E-mail', 'u.email', f.email, 'email');
    html += '<label class="fld"><span>Роль</span><select data-f="u.role">' +
      roles.map(function (r) {
        return '<option value="' + r.id + '"' + (f.role === r.id ? ' selected' : '') + '>' +
          esc(r.id) + ' — ' + esc(r.description) + '</option>';
      }).join('') + '</select></label>';
    html += '</div>';
    html += '<label class="chk"><input type="checkbox" data-f="u.enabled"' + (f.enabled ? ' checked' : '') + '> Учётная запись активна</label>';
    html += '</div></div>';

    if (isNew) {
      html += '<div class="card"><div class="card-head"><i class="fas fa-key"></i> Пароль</div><div class="card-body">';
      html += '<div class="form-grid">';
      html += input('Пароль', 'u.password', '', 'password', 'пусто = сгенерировать автоматически');
      html += '</div>';
      html += '<label class="chk"><input type="checkbox" data-f="u.mustChange" checked> Потребовать смену при первом входе</label>';
      html += '</div></div>';
    }

    // компании (только для роли client)
    html += '<div class="card" id="user-companies-card"><div class="card-head"><i class="fas fa-building"></i> Доступ к компаниям</div>' +
      '<div class="card-body"><p class="muted" style="font-size:11.5px;margin-top:0">' +
      'Для роли <code>client</code> — отметьте компании, которые пользователь увидит. ' +
      'Для остальных ролей доступны все компании.</p>' +
      '<div id="user-companies"></div></div></div>';

    html += '<div class="form-actions">' +
      '<button type="submit" class="btn btn-primary"><i class="fas fa-save"></i> Сохранить</button>' +
      '<button type="button" class="btn" id="user-cancel">Отмена</button>' +
      '</div>';

    html += '</form>';
    view.innerHTML = html;

    // список компаний
    api('/api/companies').then(function (res) {
      var box = document.getElementById('user-companies');
      var list = res.companies || [];
      if (!list.length) { box.innerHTML = '<span class="muted">нет компаний</span>'; return; }
      box.innerHTML = list.map(function (c) {
        var checked = (f.companyIds || []).indexOf(c.id) >= 0;
        return '<label class="chk" style="display:flex;margin-top:4px">' +
          '<input type="checkbox" data-company="' + esc(c.id) + '"' + (checked ? ' checked' : '') + '> ' +
          esc(c.name) + ' <span class="muted">(' + esc(c.id) + ')</span></label>';
      }).join('');
    }).catch(function () {
      document.getElementById('user-companies').innerHTML = '<span class="muted">не удалось загрузить</span>';
    });

    document.getElementById('user-cancel').addEventListener('click', function () {
      location.hash = '#/users';
      renderUsers();
    });

    document.getElementById('user-form').addEventListener('submit', function (e) {
      e.preventDefault();

      var body = {
        username: $('[data-f="u.username"]').value.trim(),
        displayName: $('[data-f="u.displayName"]').value.trim(),
        email: $('[data-f="u.email"]').value.trim(),
        role: $('[data-f="u.role"]').value,
        enabled: $('[data-f="u.enabled"]').checked,
        companyIds: $$('[data-company]').filter(function (c) { return c.checked; })
          .map(function (c) { return c.getAttribute('data-company'); })
      };

      if (isNew) {
        var pwd = $('[data-f="u.password"]');
        if (pwd) body.password = pwd.value;
        var mc = $('[data-f="u.mustChange"]');
        body.mustChangePassword = mc ? mc.checked : true;
      }

      var url = isNew ? '/api/users' : '/api/users/' + encodeURIComponent(f.username);
      var method = isNew ? 'POST' : 'PUT';

      api(url, { method: method, body: body }).then(function (res) {
        if (res.password) {
          showPasswordModal(res.username, res.password);
        } else {
          toast(isNew ? 'Пользователь создан' : 'Сохранено', 'ok');
        }
        location.hash = '#/users';
        renderUsers();
      }).catch(function (err) {
        toast('Ошибка: ' + err.message, 'error');
      });
    });
  }

  // ------------------------------------------------------------------ обновление

  document.getElementById('btn-refresh').addEventListener('click', function () {
    toast('Обновляю…');
    route();
  });

  // периодическое обновление дашборда
  setInterval(function () {
    if (state.route === 'dashboard' && !document.hidden) renderDashboard();
  }, 60000);

  // ------------------------------------------------------------------ бренд (P17)

  /**
   * Подставляет название, цвета, логотипы и подпись из config/branding.json.
   * Вызывается до показа экрана входа — иначе мелькнёт чужое название.
   */
  function applyBranding(b) {
    state.branding = b;

    $$('.brand-part1').forEach(function (el) { el.textContent = b.part1; });
    $$('.brand-part2').forEach(function (el) { el.textContent = b.part2; });
    $$('.brand-tagline').forEach(function (el) { el.textContent = b.tagline; });

    var root = document.documentElement;
    if (b.part1Color) root.style.setProperty('--brand-part1', b.part1Color);
    if (b.part2Color) root.style.setProperty('--brand-part2', b.part2Color);

    if (b.title) document.title = b.title;

    var fav = document.getElementById('brand-favicon');
    if (fav && b.favicon) fav.setAttribute('href', b.favicon);

    if (b.logoLight) {
      $$('.brand-logo-light').forEach(function (img) { img.src = b.logoLight; });
    }
    if (b.logoDark) {
      $$('.brand-logo-dark').forEach(function (img) { img.src = b.logoDark; });
    }
    $$('.brand-logo-light, .brand-logo-dark').forEach(function (img) {
      img.alt = b.fullName || '';
    });

    $$('.brand-footer-text').forEach(function (el) { el.textContent = b.footerText; });
    $$('.brand-footer-url').forEach(function (el) {
      el.textContent = b.footerUrlText;
      el.href = b.footerUrl;
    });
  }

  function loadBranding() {
    return api('/api/branding').then(function (b) {
      applyBranding(b);
    }).catch(function () {
      // брендинг не критичен — остаются значения из HTML
    });
  }

  // ------------------------------------------------------------------ развёртывание

  var deployState = {
    tab: 'subnet',
    spec: '',
    adDomain: '',
    adBaseDn: '',
    targets: [],
    selected: {},
    jobs: [],
    timer: null,
    busy: ''
  };

  function renderDeploy() {
    stopDeployPolling();
    view.innerHTML = '<div class="muted">Загрузка…</div>';

    api('/api/deploy/status').then(function (st) {
      deployState.jobs = st.jobs || [];

      var html = '<h2 class="page-title"><i class="fas fa-cloud-upload-alt"></i> Развёртывание агентов</h2>';

      html += '<p class="muted" style="margin:-6px 0 14px;font-size:11.5px">' +
        'Установка, обновление и удаление агента на машинах по WinRM. ' +
        'Найдя машины в подсети или в Active Directory, отметьте нужные и нажмите кнопку. ' +
        'Ничего не происходит без вашего действия. Подробнее: <code>docs/deploy.md</code>.</p>';

      // ------------------------------------------------ доступ
      var cred = st.credential || {};
      var hasCred = !!cred.hasPassword;

      html += '<div class="card"><div class="card-head">' +
        '<i class="fas fa-key"></i> Доступ к машинам' +
        '<span class="spacer"></span>' +
        (hasCred
          ? '<span class="badge s-OK"><i class="fas fa-check-circle"></i> задан</span>'
          : '<span class="badge s-WARN"><i class="fas fa-exclamation-triangle"></i> не задан</span>') +
        '</div><div class="card-body">';

      html += '<div class="deploy-grid">' +
        '<label class="fld"><span>Учётная запись</span>' +
        '<input type="text" id="dp-user" value="' + esc(cred.user || '') + '" placeholder="TempAdmin или DOMAIN\\admin"></label>' +
        '<label class="fld"><span>Пароль</span>' +
        '<input type="password" id="dp-pass" placeholder="' +
          (hasCred ? 'задан — оставьте пустым, чтобы не менять' : 'пароль') + '"></label>' +
        '</div>';

      html += '<p class="muted" style="font-size:11.5px;margin:8px 0 0">' +
        'Нужны права локального администратора на целевых машинах. Пароль хранится ' +
        'в <code>secrets.json</code> под DPAPI — в открытом виде на диск не попадает. ' +
        'Для домена подойдёт <code>DOMAIN\\admin</code>, для локальной учётки — просто имя.</p>';

      html += '<div style="margin-top:10px"><button class="btn btn-primary" id="dp-save-cred">' +
        '<i class="fas fa-save"></i> Сохранить доступ</button></div>';

      html += '</div></div>';

      // ------------------------------------------------ поиск
      html += '<div class="card"><div class="card-head">' +
        '<i class="fas fa-search"></i> Где искать машины</div><div class="card-body">';

      html += '<div class="deploy-tabs">' +
        '<button class="btn btn-sm' + (deployState.tab === 'subnet' ? ' btn-primary' : '') +
          '" data-dp-tab="subnet"><i class="fas fa-network-wired"></i> Подсеть</button>' +
        '<button class="btn btn-sm' + (deployState.tab === 'ad' ? ' btn-primary' : '') +
          '" data-dp-tab="ad"><i class="fas fa-sitemap"></i> Active Directory</button>' +
        '<button class="btn btn-sm' + (deployState.tab === 'manual' ? ' btn-primary' : '') +
          '" data-dp-tab="manual"><i class="fas fa-keyboard"></i> Вручную</button>' +
        '</div>';

      if (deployState.tab === 'subnet') {
        var subs = st.subnets || [];
        html += '<div class="deploy-grid" style="margin-top:12px">' +
          '<label class="fld"><span>Подсеть или диапазон</span>' +
          '<input type="text" id="dp-spec" value="' + esc(deployState.spec) + '" ' +
            'placeholder="192.168.27.0/24"></label>' +
          '<label class="fld"><span>&nbsp;</span>' +
          '<button class="btn btn-primary" id="dp-scan"><i class="fas fa-search"></i> Найти машины</button></label>' +
          '</div>';

        if (subs.length) {
          html += '<div class="muted" style="font-size:11px">локальные подсети: ';
          subs.forEach(function (su, i) {
            html += (i ? ' · ' : '') + '<a href="#" data-dp-spec="' + esc(su) + '">' + esc(su) + '</a>';
          });
          html += '</div>';
        }

        html += '<p class="muted" style="font-size:11.5px;margin:8px 0 0">' +
          'Форматы: <code>192.168.27.0/24</code>, <code>192.168.27.10-40</code>, ' +
          '<code>192.168.27.5</code>. Максимум 1024 адреса — /16 целиком не пройдёт.</p>';

      } else if (deployState.tab === 'ad') {
        html += '<div class="deploy-grid" style="margin-top:12px">' +
          '<label class="fld"><span>Домен (пусто = текущий)</span>' +
          '<input type="text" id="dp-domain" value="' + esc(deployState.adDomain) + '" placeholder="corp.local"></label>' +
          '<label class="fld"><span>OU (необязательно)</span>' +
          '<input type="text" id="dp-basedn" value="' + esc(deployState.adBaseDn) + '" placeholder="OU=Servers,DC=corp,DC=local"></label>' +
          '</div>';
        html += '<div style="margin-top:10px"><button class="btn btn-primary" id="dp-ad">' +
          '<i class="fas fa-sitemap"></i> Запросить компьютеры</button></div>';
        html += '<p class="muted" style="font-size:11.5px;margin:8px 0 0">' +
          'Один LDAP-поиск по <code>objectCategory=computer</code>, отключённые учётные записи ' +
          'пропускаются. Используется та же учётная запись, что и для WinRM.</p>';

      } else {
        html += '<div class="deploy-grid" style="margin-top:12px">' +
          '<label class="fld"><span>Хосты (по одному в строке)</span>' +
          '<textarea id="dp-manual" rows="4" placeholder="192.168.27.10&#10;back.corp.local"></textarea></label>' +
          '<label class="fld"><span>&nbsp;</span>' +
          '<button class="btn btn-primary" id="dp-add-manual"><i class="fas fa-plus"></i> Добавить</button></label>' +
          '</div>';
      }

      html += '</div></div>';

      // ------------------------------------------------ агенты и версии
      var agents = st.agents || [];
      var outdated = agents.filter(function (a) { return a.outdated; });

      html += '<div class="card"><div class="card-head">' +
        '<i class="fas fa-robot"></i> Агенты' +
        '<span class="spacer"></span>' +
        '<span class="muted" style="font-size:11px;margin-right:10px">версия ' +
          esc(st.currentVersion || '?') + '</span>' +
        (outdated.length
          ? '<button class="btn btn-sm btn-primary" id="dp-update-all">' +
            '<i class="fas fa-sync-alt"></i> Обновить устаревшие (' + outdated.length + ')</button>'
          : '') +
        '</div>';

      if (!agents.length) {
        html += '<div class="card-body"><p class="muted" style="margin:0;font-size:11.5px">' +
          'Агентов пока нет. Установите первого на вкладке «Подсеть» или «Active Directory».</p></div>';
      } else {
        html += '<div class="card-body" style="padding:0">' +
          '<div class="table-wrap"><table class="journal-table"><thead><tr>' +
          '<th>Агент</th><th>Адрес</th><th style="width:110px">Версия</th>' +
          '<th style="width:130px">Последний отчёт</th><th style="width:90px"></th>' +
          '</tr></thead><tbody>';

        agents.forEach(function (a) {
          html += '<tr' + (a.outdated ? ' style="background:rgba(220,150,30,.07)"' : '') + '>' +
            '<td><b>' + esc(a.name) + '</b>' +
              '<div class="vm-disk">' + esc(a.id) + '</div></td>' +
            '<td style="font-size:11.5px">' + esc(a.lastIp || '—') + '</td>' +
            '<td style="font-size:11.5px">' + esc(a.lastVersion || '—') +
              (a.outdated ? ' <span style="color:var(--warn)">устарел</span>' : '') + '</td>' +
            '<td style="font-size:11.5px">' + esc(a.lastSeen ? ago(a.lastSeen) : '—') + '</td>' +
            '<td>' + (a.lastIp
              ? '<button class="btn btn-sm" data-dp-update="' + esc(a.lastIp) + '">' +
                '<i class="fas fa-sync-alt"></i></button>'
              : '') + '</td>' +
            '</tr>';
        });

        html += '</tbody></table></div></div>';
      }

      html += '</div></div>';

      html += '<div class="card"><div class="card-head">' +
        '<i class="fas fa-server"></i> Машины' +
        '<span class="spacer"></span><span class="muted" style="font-size:11px" id="dp-count">' +
        (deployState.targets.length || 0) + '</span>' +
        '</div><div id="deploy-targets"></div></div>';

      // ------------------------------------------------ действия
      html += '<div class="card"><div class="card-head">' +
        '<i class="fas fa-play"></i> Действия</div><div class="card-body">';

      html += '<div class="deploy-grid">' +
        '<label class="fld"><span>Режим</span><select id="dp-mode">' +
        '<option value="agent">Удалённый агент (отчитывается сюда)</option>' +
        '<option value="central">Полный экземпляр (свой веб-интерфейс)</option>' +
        '</select></label>' +
        '<label class="fld"><span>Центральная машина</span>' +
        '<input type="text" id="dp-central" value="' + esc(deployState.centralUrl || st.centralUrl || '') + '" ' +
        'placeholder="http://192.168.27.192:8080"></label>' +
        '</div>';

      html += '<details style="margin-top:10px"><summary class="muted" style="cursor:pointer;font-size:11.5px">' +
        'Дополнительно: каталог и имя службы</summary>' +
        '<div class="deploy-grid" style="margin-top:8px">' +
        '<label class="fld"><span>Каталог установки</span>' +
        '<input type="text" id="dp-dir" value="' + esc(deployDefaults().installDir) + '"></label>' +
        '<label class="fld"><span>Служба</span>' +
        '<input type="text" id="dp-svc" value="' + esc(deployDefaults().serviceNameRemote) + '"></label>' +
        '</div></details>';

      html += '<div style="margin-top:12px;display:flex;gap:8px;flex-wrap:wrap">' +
        '<button class="btn btn-primary" id="dp-install"><i class="fas fa-cloud-upload-alt"></i> Установить / обновить</button>' +
        '<button class="btn" id="dp-uninstall"><i class="fas fa-trash-alt"></i> Удалить агента</button>' +
        '<button class="btn" id="dp-probe"><i class="fas fa-plug"></i> Проверить WinRM</button>' +
        '</div>';

      html += '<p class="muted" style="font-size:11.5px;margin:10px 0 0">' +
        'Центральная машина должна быть доступна с целевого сервера по этому адресу ' +
        'и порту. Ставится пакет ~1.2 МБ: exe, веб-интерфейс, шаблоны и утилиты.</p>';

      html += '</div></div>';

      // ------------------------------------------------ задания
      html += '<div class="card"><div class="card-head">' +
        '<i class="fas fa-list-check"></i> Задания' +
        '<span class="spacer"></span><span class="muted" style="font-size:11px" id="dp-jobs-count"></span>' +
        '</div><div id="deploy-jobs" class="card-body" style="padding:0"></div></div>';

      view.innerHTML = html;

      deployState.centralUrl = $('[id="dp-central"]').value;
      deployState.savedCentralUrl = st.centralUrl || '';
      renderDeployTargets();
      renderDeployJobs();

      // ------------------------------------------------ события
      $$('[data-dp-tab]').forEach(function (b) {
        b.addEventListener('click', function () {
          saveDeployInputs();
          deployState.tab = b.getAttribute('data-dp-tab');
          renderDeploy();
        });
      });

      $$('[data-dp-spec]').forEach(function (a) {
        a.addEventListener('click', function (e) {
          e.preventDefault();
          deployState.spec = a.getAttribute('data-dp-spec');
          renderDeploy();
        });
      });

      $('#dp-save-cred').addEventListener('click', function () {
        var user = $('#dp-user').value.trim();
        var pass = $('#dp-pass').value;
        if (!user) { toast('Укажите учётную запись', 'error'); return; }
        api('/api/deploy/credential', { method: 'POST', body: { username: user, password: pass } })
          .then(function () { toast('Доступ сохранён', 'ok'); renderDeploy(); })
          .catch(function (e) { toast('Ошибка: ' + e.message, 'error'); });
      });

      var scanBtn = $('#dp-scan');
      if (scanBtn) scanBtn.addEventListener('click', function () {
        var spec = $('#dp-spec').value.trim();
        if (!spec) { toast('Укажите подсеть', 'error'); return; }
        deployState.spec = spec;
        scanBtn.disabled = true;
        scanBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Ищу…';
        api('/api/deploy/scan', { method: 'POST', body: { spec: spec } })
          .then(function (r) {
            deployState.targets = r.targets || [];
            deployState.selected = {};
            deployState.targets.forEach(function (t) { deployState.selected[t.host] = true; });
            toast('Найдено машин: ' + (r.count || 0) + ' из ' + (r.checked || 0), 'ok');
            renderDeploy();
          })
          .catch(function (e) {
            toast('Ошибка: ' + e.message, 'error');
            scanBtn.disabled = false;
            scanBtn.innerHTML = '<i class="fas fa-search"></i> Найти машины';
          });
      });

      var adBtn = $('#dp-ad');
      if (adBtn) adBtn.addEventListener('click', function () {
        deployState.adDomain = $('#dp-domain').value.trim();
        deployState.adBaseDn = $('#dp-basedn').value.trim();
        adBtn.disabled = true;
        adBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Запрашиваю…';
        api('/api/deploy/ad', { method: 'POST', body: { domain: deployState.adDomain, baseDn: deployState.adBaseDn } })
          .then(function (r) {
            deployState.targets = r.targets || [];
            deployState.selected = {};
            deployState.targets.forEach(function (t) { deployState.selected[t.host] = true; });
            toast('Компьютеров: ' + (r.count || 0), 'ok');
            renderDeploy();
          })
          .catch(function (e) {
            toast('Ошибка: ' + e.message, 'error');
            adBtn.disabled = false;
            adBtn.innerHTML = '<i class="fas fa-sitemap"></i> Запросить компьютеры';
          });
      });

      var manBtn = $('#dp-add-manual');
      if (manBtn) manBtn.addEventListener('click', function () {
        var text = $('#dp-manual').value || '';
        var added = 0;
        text.split(/[\s,;]+/).forEach(function (h) {
          h = h.trim();
          if (!h) return;
          if (deployState.targets.some(function (t) { return t.host === h; })) return;
          deployState.targets.push({ host: h, ip: h, source: 'manual', online: true, winrm: null });
          deployState.selected[h] = true;
          added++;
        });
        if (!added) { toast('Нечего добавлять', 'error'); return; }
        $('#dp-manual').value = '';
        renderDeployTargets();
        toast('Добавлено: ' + added, 'ok');
      });

      $('#dp-probe').addEventListener('click', function () { probeDeployTargets(); });

      $('#dp-install').addEventListener('click', function () { deployAction('install'); });
      $('#dp-uninstall').addEventListener('click', function () { deployAction('uninstall'); });

      // обновление одного агента: тот же путь установки, адрес центра из конфига
      $$('[data-dp-update]').forEach(function (b) {
        b.addEventListener('click', function () {
          updateAgents([b.getAttribute('data-dp-update')]);
        });
      });

      var updAll = document.getElementById('dp-update-all');
      if (updAll) {
        updAll.addEventListener('click', function () {
          var hosts = outdated.map(function (a) { return a.lastIp; }).filter(Boolean);
          if (!hosts.length) { toast('Нет агентов с известным адресом', 'error'); return; }
          if (!confirm('Обновить агентов: ' + hosts.length + '?\n\n' + hosts.join('\n') +
              '\n\nСлужба на каждой машине будет перезапущена.')) return;
          updateAgents(hosts);
        });
      }

      startDeployPolling();
    }).catch(function (e) {
      view.innerHTML = '<div class="card"><div class="card-body">Ошибка: ' + esc(e.message) + '</div></div>';
    });
  }

  // Каталог и имя службы по умолчанию берём из бренда: у другой компании
  // не должно быть названия одной компании в путях и именах служб.
  function deployDefaults() {
    var b = state.branding || {};
    return {
      installDir: b.installDir || 'C:\\Program Files\\BackupMonitor',
      serviceName: b.serviceName || 'BackupMonitor',
      serviceNameRemote: b.serviceNameRemote || 'BackupMonitorRemote'
    };
  }

  function saveDeployInputs() {    var c = $('[id="dp-central"]');
    if (c) deployState.centralUrl = c.value.trim();
    var s = $('[id="dp-spec"]');
    if (s) deployState.spec = s.value.trim();
    var d = $('[id="dp-domain"]');
    if (d) deployState.adDomain = d.value.trim();
    var b = $('[id="dp-basedn"]');
    if (b) deployState.adBaseDn = b.value.trim();
  }

  function selectedHosts() {
    return deployState.targets
      .filter(function (t) { return deployState.selected[t.host]; })
      .map(function (t) { return t.host; });
  }

  function renderDeployTargets() {
    var box = document.getElementById('deploy-targets');
    if (!box) return;

    var countEl = document.getElementById('dp-count');
    if (countEl) countEl.textContent = deployState.targets.length;

    if (!deployState.targets.length) {
      box.innerHTML = '<div style="padding:14px" class="muted">' +
        'Машины ещё не найдены. Выберите способ поиска выше.</div>';
      return;
    }

    var h = '<div class="table-wrap"><table class="journal-table"><thead><tr>' +
      '<th style="width:34px"><input type="checkbox" id="dp-all"></th>' +
      '<th>Хост</th><th>IP</th><th>ОС</th><th style="width:90px">WinRM</th>' +
      '<th style="width:130px">Агент</th><th style="width:90px"></th>' +
      '</tr></thead><tbody>';

    deployState.targets.forEach(function (t) {
      var checked = deployState.selected[t.host] ? ' checked' : '';

      var winrm = t.winrm === true ? '<span style="color:var(--ok)">есть</span>'
        : (t.winrm === false ? '<span class="muted">нет</span>' : '<span class="muted">?</span>');

      var agent = t.hasAgent
        ? '<span style="color:var(--ok)">' + esc(t.agentVersion || 'есть') + '</span>'
        : '<span class="muted">нет</span>';

      h += '<tr>' +
        '<td><input type="checkbox" data-dp-host="' + esc(t.host) + '"' + checked + '></td>' +
        '<td><b>' + esc(t.host) + '</b>' +
          (t.note ? '<div class="vm-disk">' + esc(t.note) + '</div>' : '') + '</td>' +
        '<td style="font-size:11.5px">' + esc(t.ip || '—') + '</td>' +
        '<td style="font-size:11.5px">' + esc(t.os || '—') + '</td>' +
        '<td>' + winrm + '</td>' +
        '<td>' + agent + '</td>' +
        '<td><button class="btn btn-sm" data-dp-test="' + esc(t.host) + '">' +
          '<i class="fas fa-stethoscope"></i></button></td>' +
        '</tr>';
    });

    h += '</tbody></table></div>';
    box.innerHTML = h;

    var all = document.getElementById('dp-all');
    if (all) {
      all.checked = deployState.targets.every(function (t) { return deployState.selected[t.host]; });
      all.addEventListener('change', function () {
        deployState.targets.forEach(function (t) { deployState.selected[t.host] = all.checked; });
        renderDeployTargets();
      });
    }

    $$('[data-dp-host]').forEach(function (cb) {
      cb.addEventListener('change', function () {
        deployState.selected[cb.getAttribute('data-dp-host')] = cb.checked;
      });
    });

    $$('[data-dp-test]').forEach(function (b) {
      b.addEventListener('click', function () { testDeployHost(b.getAttribute('data-dp-test')); });
    });
  }

  function testDeployHost(host) {
    toast('Проверяю ' + host + '…');
    api('/api/deploy/test', { method: 'POST', body: { host: host } })
      .then(function (r) {
        if (r.ok) {
          toast(host + ': ' + (r.name || host) + ', ' + (r.os || '') + ', админ: ' + (r.admin ? 'да' : 'НЕТ'), 'ok');
          var t = deployState.targets.filter(function (x) { return x.host === host; })[0];
          if (t) {
            t.winrm = true;
            t.online = true;
            if (r.os) t.os = r.os;
            if (r.name && r.name !== host) t.note = r.name;
            renderDeployTargets();
          }
        } else {
          toast(host + ': ' + (r.error || 'не удалось'), 'error');
          var t2 = deployState.targets.filter(function (x) { return x.host === host; })[0];
          if (t2) { t2.winrm = false; renderDeployTargets(); }
        }
      })
      .catch(function (e) { toast('Ошибка: ' + e.message, 'error'); });
  }

  function probeDeployTargets() {
    var hosts = selectedHosts();
    if (!hosts.length) hosts = deployState.targets.map(function (t) { return t.host; });
    if (!hosts.length) { toast('Сначала найдите машины', 'error'); return; }

    toast('Проверяю ' + hosts.length + ' машин…');
    api('/api/deploy/probe', { method: 'POST', body: { hosts: hosts } })
      .then(function (r) {
        (r.targets || []).forEach(function (found) {
          var t = deployState.targets.filter(function (x) { return x.host === found.host; })[0];
          if (!t) return;
          t.online = found.online;
          t.winrm = found.winrm;
          t.hasAgent = found.hasAgent;
          t.agentVersion = found.agentVersion;
          if (found.os) t.os = found.os;
        });
        renderDeployTargets();
        toast('Проверка завершена', 'ok');
      })
      .catch(function (e) { toast('Ошибка: ' + e.message, 'error'); });
  }

  // Обновление агентов: адрес центра берём из сохранённого (deploy.json),
  // а не из браузера — иначе агент переедет на localhost.
  function updateAgents(hosts) {
    if (!hosts || !hosts.length) return;

    var body = {
      hosts: hosts,
      mode: 'agent',
      centralUrl: deployState.centralUrl || deployState.savedCentralUrl || '',
      installDir: ($('[id="dp-dir"]') || {}).value || '',
      serviceName: ($('[id="dp-svc"]') || {}).value || ''
    };

    if (!body.centralUrl) {
      toast('Не известен адрес центральной машины — укажите его в «Действия»', 'error');
      return;
    }

    api('/api/deploy/install', { method: 'POST', body: body })
      .then(function (r) {
        toast('Обновление запущено: ' + (r.count || 0), 'ok');
        startDeployPolling(true);
      })
      .catch(function (e) { toast('Ошибка: ' + e.message, 'error'); });
  }

  function deployAction(kind) {
    var hosts = selectedHosts();
    if (!hosts.length) { toast('Отметьте машины в таблице', 'error'); return; }

    if (kind === 'uninstall') {
      if (!confirm('Удалить агента на ' + hosts.length + ' машин(ах)?\n\n' +
        hosts.join('\n') + '\n\nСлужба будет остановлена и удалена, каталог — тоже.')) return;
    }

    saveDeployInputs();

    var body = {
      hosts: hosts,
      installDir: ($('[id="dp-dir"]') || {}).value || '',
      serviceName: ($('[id="dp-svc"]') || {}).value || ''
    };

    if (kind === 'install') {
      body.mode = ($('[id="dp-mode"]') || {}).value || 'agent';
      body.centralUrl = deployState.centralUrl;

      // если служба оставлена как есть, для режима «сервер» подставляем
      // имя основной службы, а не удалённого агента
      var dflt = deployDefaults();
      if (body.mode === 'central' && body.serviceName === dflt.serviceNameRemote)
        body.serviceName = dflt.serviceName;
    } else {
      body.keepFiles = false;
      body.removeAgent = true;
    }

    api('/api/deploy/' + kind, { method: 'POST', body: body })
      .then(function (r) {
        toast('Заданий запущено: ' + (r.count || 0), 'ok');
        startDeployPolling(true);
      })
      .catch(function (e) { toast('Ошибка: ' + e.message, 'error'); });
  }

  // ------------------------------------------------ задания и опрос

  function renderDeployJobs() {
    var box = document.getElementById('deploy-jobs');
    if (!box) return;

    var countEl = document.getElementById('dp-jobs-count');
    var running = deployState.jobs.filter(function (j) { return !j.done; }).length;
    if (countEl) countEl.textContent = running ? (running + ' выполняется') : '';

    if (!deployState.jobs.length) {
      box.innerHTML = '<div style="padding:14px" class="muted">Заданий пока не было.</div>';
      return;
    }

    var h = '<div class="table-wrap"><table class="journal-table"><thead><tr>' +
      '<th style="width:70px">Начало</th><th>Машина</th><th style="width:110px">Операция</th>' +
      '<th style="width:230px">Этап</th><th style="width:60px">Итог</th><th></th>' +
      '</tr></thead><tbody>';

    deployState.jobs.forEach(function (j) {
      var cls = j.done ? (j.ok ? 's-OK' : 's-FAIL') : 's-WARN';

      var barColor = j.done ? (j.ok ? 'var(--ok)' : 'var(--fail)') : 'var(--brand)';
      var pct = Math.max(0, Math.min(100, j.percent || 0));

      h += '<tr>' +
        '<td style="font-size:11px;white-space:nowrap">' + esc(fmtTimeSec(j.started)) + '</td>' +
        '<td><b>' + esc(j.host) + '</b></td>' +
        '<td style="font-size:11.5px">' + esc(j.kind) + '</td>' +
        '<td>' +
          '<div style="font-size:11.5px">' + esc(j.phase || '') + '</div>' +
          '<div class="inv-bar" style="margin-top:3px"><i style="width:' + pct +
            '%;background:' + barColor + '"></i></div>' +
          (j.message ? '<div class="vm-disk">' + esc(j.message) + '</div>' : '') +
        '</td>' +
        '<td><span class="badge ' + cls + '">' + (j.done ? (j.ok ? 'OK' : 'FAIL') : pct + '%') + '</span></td>' +
        '<td><button class="btn btn-sm" data-dp-log="' + esc(j.id) + '">' +
          '<i class="fas fa-list"></i></button></td>' +
        '</tr>';
    });

    h += '</tbody></table></div>';
    box.innerHTML = h;

    $$('[data-dp-log]').forEach(function (b) {
      b.addEventListener('click', function () { showDeployLog(b.getAttribute('data-dp-log')); });
    });
  }

  function fmtTimeSec(iso) {
    if (!iso) return '—';
    var d = new Date(iso);
    if (isNaN(d.getTime())) return '—';
    return pad(d.getHours()) + ':' + pad(d.getMinutes()) + ':' + pad(d.getSeconds());
  }

  function showDeployLog(id) {
    var j = deployState.jobs.filter(function (x) { return x.id === id; })[0];
    if (!j) return;

    var h = '<div class="modal-backdrop" id="dp-log-modal" style="z-index:400">' +
      '<div class="modal modal-wide">' +
      '<h3><i class="fas fa-list"></i> ' + esc(j.kind) + ': ' + esc(j.host) + '</h3>' +
      '<p class="muted" style="font-size:11.5px;margin-top:0">' +
        esc(j.phase || '') + (j.message ? ' · ' + esc(j.message) : '') +
        (j.done ? ' · ' + (j.ok ? 'успешно' : 'ошибка') : ' · выполняется') + '</p>' +
      '<pre style="max-height:55vh;overflow:auto;background:var(--surface2);' +
        'border:1px solid var(--border);border-radius:6px;padding:10px;' +
        'font-size:11.5px;line-height:1.5;white-space:pre-wrap">' +
        esc((j.log || []).join('\n')) + '</pre>' +
      '<div class="modal-actions" style="margin-top:12px">' +
      '<button class="btn" id="dp-log-close">Закрыть</button></div>' +
      '</div></div>';

    var div = document.createElement('div');
    div.innerHTML = h;
    document.body.appendChild(div.firstChild);

    document.getElementById('dp-log-close').addEventListener('click', function () {
      var m = document.getElementById('dp-log-modal');
      if (m) m.parentNode.removeChild(m);
    });
  }

  function startDeployPolling(force) {
    stopDeployPolling();

    function tick() {
      if (state.route !== 'deploy') { stopDeployPolling(); return; }

      api('/api/deploy/jobs?limit=20').then(function (r) {
        var prevRunning = deployState.jobs.filter(function (j) { return !j.done; }).length;
        deployState.jobs = r.jobs || [];
        renderDeployJobs();

        var running = deployState.jobs.filter(function (j) { return !j.done; }).length;

        // только что закончилось — обновим таблицу машин и агентов
        if (prevRunning > 0 && running === 0) {
          api('/api/deploy/status').then(function (st) {
            deployState.jobs = st.jobs || [];
            renderDeployJobs();
          });
        }
      }).catch(function () { });
    }

    if (force) tick();

    deployState.timer = setInterval(tick, 3000);
  }

  function stopDeployPolling() {
    if (deployState.timer) {
      clearInterval(deployState.timer);
      deployState.timer = null;
    }
  }

  // ------------------------------------------------------------------ управление

  var manageState = {
    catalog: null,
    targets: [],
    target: '',
    op: null,
    jobs: [],
    timer: null,
    pending: null
  };

  function renderManage() {
    stopManagePolling();
    view.innerHTML = '<div class="muted">Загрузка…</div>';

    Promise.all([api('/api/manage/catalog'), api('/api/manage/targets'), api('/api/manage/jobs?limit=20')])
      .then(function (res) {
        manageState.catalog = res[0];
        manageState.targets = res[1].targets || [];
        manageState.hasCredential = !!res[1].hasCredential;
        manageState.jobs = res[2].jobs || [];

        if (!manageState.target && manageState.targets.length) {
          manageState.target = manageState.targets[0].id;
        }

        var html = '<h2 class="page-title"><i class="fas fa-sliders-h"></i> Управление</h2>';

        html += '<p class="muted" style="margin:-6px 0 14px;font-size:11.5px">' +
          'Фиксированный набор операций. Свободной командной строки нет: параметры ' +
          'проверяются, опасные операции требуют подтверждения. ' +
          'Подробнее: <code>docs/management.md</code>.</p>';

        // ---- цель
        html += '<div class="card"><div class="card-head">' +
          '<i class="fas fa-crosshairs"></i> Куда применяем</div><div class="card-body">';

        html += '<div class="deploy-grid">' +
          '<label class="fld"><span>Цель</span><select id="mg-target">' +
          manageState.targets.map(function (t) {
            var label = t.name + (t.host && t.kind === 'agent' ? ' — ' + t.host : '');
            return '<option value="' + esc(t.id) + '"' +
              (t.id === manageState.target ? ' selected' : '') +
              (t.kind === 'agent' && !t.host ? ' disabled' : '') + '>' + esc(label) + '</option>';
          }).join('') + '</select></label>' +
          '<label class="fld"><span>&nbsp;</span>' +
          '<div class="muted" style="font-size:11.5px;padding-top:6px">' +
          (manageState.hasCredential
            ? 'WinRM доступ настроен'
            : '<span style="color:var(--warn)">учётные данные WinRM не заданы — настройте на странице «Развёртывание»</span>') +
          '</div></label></div>';

        html += '</div></div>';

        // ---- операции
        var groups = manageState.catalog.groups || [];
        var ops = manageState.catalog.operations || [];

        html += '<div class="card"><div class="card-head">' +
          '<i class="fas fa-list"></i> Операции</div><div class="card-body" style="padding:0">';

        groups.forEach(function (g) {
          var items = ops.filter(function (o) { return o.group === g; });
          if (!items.length) return;

          html += '<div style="padding:8px 14px;border-bottom:1px solid var(--border);' +
            'background:var(--surface2);font-size:11.5px;font-weight:600">' + esc(g) + '</div>';

          items.forEach(function (o) {
            var riskColor = o.risk === 'high' ? 'var(--fail)' : (o.risk === 'medium' ? 'var(--warn)' : 'var(--ok)');
            var riskText = o.risk === 'high' ? 'высокий' : (o.risk === 'medium' ? 'средний' : 'низкий');

            html += '<div style="padding:10px 14px;border-bottom:1px solid var(--border)">' +
              '<div style="display:flex;gap:10px;align-items:flex-start">' +
                '<div style="flex:1;min-width:0">' +
                  '<div style="font-weight:600;font-size:12.5px">' + esc(o.title) +
                    ' <span class="muted" style="font-weight:400;font-size:11px">' + esc(o.id) + '</span></div>' +
                  '<div class="muted" style="font-size:11.5px;margin-top:2px">' + esc(o.description) + '</div>' +
                '</div>' +
                '<div style="white-space:nowrap;font-size:11px;color:' + riskColor + '">' + riskText + '</div>' +
                '<button class="btn btn-sm" data-mg-op="' + esc(o.id) + '">' +
                  '<i class="fas fa-play"></i></button>' +
              '</div>' +
              '<div id="mg-form-' + esc(o.id) + '" class="hidden" style="margin-top:10px;padding:10px;' +
                'background:var(--surface2);border:1px solid var(--border);border-radius:6px"></div>' +
              '</div>';
          });
        });

        html += '</div></div>';

        // ---- задания
        html += '<div class="card"><div class="card-head">' +
          '<i class="fas fa-tasks"></i> Задания' +
          '<span class="spacer"></span><span class="muted" style="font-size:11px" id="mg-jobs-count"></span>' +
          '</div><div id="manage-jobs" class="card-body" style="padding:0"></div></div>';

        view.innerHTML = html;

        renderManageJobs();

        $('#mg-target').addEventListener('change', function (e) {
          manageState.target = e.target.value;
        });

        $$('[data-mg-op]').forEach(function (b) {
          b.addEventListener('click', function () { openOpForm(b.getAttribute('data-mg-op')); });
        });

        startManagePolling();
      })
      .catch(function (e) {
        view.innerHTML = '<div class="card"><div class="card-body">Ошибка: ' + esc(e.message) + '</div></div>';
      });
  }

  function openOpForm(opId) {
    var ops = (manageState.catalog || {}).operations || [];
    var op = ops.filter(function (o) { return o.id === opId; })[0];
    if (!op) return;

    var box = document.getElementById('mg-form-' + opId);
    if (!box) return;

    if (!box.classList.contains('hidden')) { box.classList.add('hidden'); box.innerHTML = ''; return; }

    var h = '';

    (op.params || []).forEach(function (p) {
      h += '<label class="fld" style="display:block;margin-bottom:8px"><span>' + esc(p.label) +
        (p.required ? '' : ' (необязательно)') + '</span>';
      h += '<input type="text" data-mg-p="' + esc(p.name) + '" value="' + esc(p.default || '') + '"' +
        (p.hint ? ' placeholder="' + esc(p.hint) + '"' : '') + '>';
      h += '</label>';
    });

    if (op.needsConfirmText) {
      h += '<label class="fld" style="display:block;margin-bottom:8px">' +
        '<span style="color:var(--fail)">Подтверждение: введите имя цели</span>' +
        '<input type="text" data-mg-confirm="1" placeholder="' + esc(manageState.target) + '">' +
        '</label>';
    }

    if (op.risk !== 'low') {
      h += '<p class="muted" style="font-size:11px;margin:0 0 8px">' +
        'Операция средней/высокой опасности: сначала создаётся заявка, затем её нужно подтвердить.</p>';
    }

    if (op.rollback) {
      h += '<p class="muted" style="font-size:11px;margin:0 0 8px">Откат: ' + esc(op.rollback) + '</p>';
    }

    h += '<button class="btn btn-primary btn-sm" data-mg-run="' + esc(opId) + '">' +
      '<i class="fas fa-play"></i> ' + (op.risk === 'low' ? 'Выполнить' : 'Создать заявку') + '</button>';

    box.innerHTML = h;
    box.classList.remove('hidden');

    var runBtn = box.querySelector('[data-mg-run]');
    if (runBtn) runBtn.addEventListener('click', function () { submitOp(op, box); });
  }

  function submitOp(op, box) {
    var params = {};
    $$('[data-mg-p]', box).forEach(function (el) {
      params[el.getAttribute('data-mg-p')] = el.value.trim();
    });

    var confirmEl = $('[data-mg-confirm]', box);
    var confirmText = confirmEl ? confirmEl.value.trim() : '';

    var body = { op: op.id, target: manageState.target, params: params, confirmText: confirmText };

    api('/api/manage/submit', { method: 'POST', body: body })
      .then(function (r) {
        if (r.needsApproval) {
          manageState.pending = r.job;
          toast('Заявка создана — подтвердите её ниже', 'ok');
        } else {
          toast('Операция запущена', 'ok');
        }
        box.classList.add('hidden');
        box.innerHTML = '';
        startManagePolling(true);
      })
      .catch(function (e) { toast('Ошибка: ' + e.message, 'error'); });
  }

  function renderManageJobs() {
    var box = document.getElementById('manage-jobs');
    if (!box) return;

    var pending = manageState.jobs.filter(function (j) { return j.status === 'pending'; });
    var running = manageState.jobs.filter(function (j) { return j.status === 'running' || j.status === 'queued'; }).length;

    var countEl = document.getElementById('mg-jobs-count');
    if (countEl) {
      countEl.textContent = pending.length ? (pending.length + ' ждут подтверждения') :
        (running ? (running + ' выполняется') : '');
    }

    if (!manageState.jobs.length) {
      box.innerHTML = '<div style="padding:14px" class="muted">Заданий пока не было.</div>';
      return;
    }

    var h = '<div class="table-wrap"><table class="journal-table"><thead><tr>' +
      '<th style="width:70px">Время</th><th style="width:150px">Операция</th>' +
      '<th>Цель</th><th style="width:110px">Статус</th><th style="width:70px"></th>' +
      '</tr></thead><tbody>';

    manageState.jobs.forEach(function (j) {
      var cls = j.status === 'ok' ? 's-OK' : (j.status === 'fail' ? 's-FAIL' :
        (j.status === 'rejected' ? 's-NODATA' : 's-WARN'));
      var label = { ok: 'OK', fail: 'FAIL', pending: 'заявка', rejected: 'отклонено',
        running: 'идёт', queued: 'в очереди' }[j.status] || j.status;

      h += '<tr>' +
        '<td style="font-size:11px;white-space:nowrap">' + esc(fmtTimeSec(j.requestedAt)) + '</td>' +
        '<td style="font-size:11.5px">' + esc(j.opTitle || j.op) + '</td>' +
        '<td style="font-size:11.5px">' + esc(j.target) + '</td>' +
        '<td><span class="badge ' + cls + '">' + esc(label) + '</span></td>' +
        '<td style="white-space:nowrap">' +
          (j.status === 'pending'
            ? '<button class="btn btn-sm btn-primary" data-mg-approve="' + esc(j.id) + '">' +
                '<i class="fas fa-check"></i></button> ' +
              '<button class="btn btn-sm" data-mg-reject="' + esc(j.id) + '">' +
                '<i class="fas fa-times"></i></button>'
            : '<button class="btn btn-sm" data-mg-log="' + esc(j.id) + '">' +
                '<i class="fas fa-list"></i></button>') +
        '</td>' +
        '</tr>';
    });

    h += '</tbody></table></div>';
    box.innerHTML = h;

    $$('[data-mg-approve]').forEach(function (b) {
      b.addEventListener('click', function () {
        var id = b.getAttribute('data-mg-approve');
        if (!confirm('Выполнить операцию? Это изменение на сервере.')) return;
        api('/api/manage/approve', { method: 'POST', body: { job: id } })
          .then(function () { toast('Операция запущена', 'ok'); startManagePolling(true); })
          .catch(function (e) { toast('Ошибка: ' + e.message, 'error'); });
      });
    });

    $$('[data-mg-reject]').forEach(function (b) {
      b.addEventListener('click', function () {
        var id = b.getAttribute('data-mg-reject');
        api('/api/manage/reject', { method: 'POST', body: { job: id } })
          .then(function () { toast('Заявка отклонена'); startManagePolling(true); })
          .catch(function (e) { toast('Ошибка: ' + e.message, 'error'); });
      });
    });

    $$('[data-mg-log]').forEach(function (b) {
      b.addEventListener('click', function () { showOpLog(b.getAttribute('data-mg-log')); });
    });
  }

  function showOpLog(id) {
    var j = manageState.jobs.filter(function (x) { return x.id === id; })[0];
    if (!j) return;

    var text = '';
    if (j.output) text += j.output;
    if (j.error) text += (text ? '\n\n' : '') + 'ОШИБКА: ' + j.error;
    if (!text) text = '(нет вывода)';

    var h = '<div class="modal-backdrop" id="mg-log-modal" style="z-index:400">' +
      '<div class="modal modal-wide">' +
      '<h3><i class="fas fa-list"></i> ' + esc(j.opTitle || j.op) + '</h3>' +
      '<p class="muted" style="font-size:11.5px;margin-top:0">' +
        esc(j.target) + ' · ' + esc(j.status) +
        (j.durationMs ? ' · ' + Math.round(j.durationMs / 1000) + ' с' : '') +
        (j.requestedBy ? ' · запросил ' + esc(j.requestedBy) : '') +
        (j.approvedBy && j.approvedBy !== j.requestedBy ? ' · подтвердил ' + esc(j.approvedBy) : '') +
        '</p>' +
      '<pre style="max-height:55vh;overflow:auto;background:var(--surface2);' +
        'border:1px solid var(--border);border-radius:6px;padding:10px;' +
        'font-size:11.5px;line-height:1.5;white-space:pre-wrap">' + esc(text) + '</pre>' +
      '<div class="modal-actions" style="margin-top:12px">' +
      '<button class="btn" id="mg-log-close">Закрыть</button></div>' +
      '</div></div>';

    var div = document.createElement('div');
    div.innerHTML = h;
    document.body.appendChild(div.firstChild);

    document.getElementById('mg-log-close').addEventListener('click', function () {
      var m = document.getElementById('mg-log-modal');
      if (m) m.parentNode.removeChild(m);
    });
  }

  function startManagePolling(force) {
    stopManagePolling();

    function tick() {
      if (state.route !== 'manage') { stopManagePolling(); return; }

      api('/api/manage/jobs?limit=20').then(function (r) {
        manageState.jobs = r.jobs || [];
        renderManageJobs();
      }).catch(function () { });
    }

    if (force) tick();
    manageState.timer = setInterval(tick, 3000);
  }

  function stopManagePolling() {
    if (manageState.timer) {
      clearInterval(manageState.timer);
      manageState.timer = null;
    }
  }

  // ------------------------------------------------------------------ старт

  function applyPermissions(me) {
    state.me = me;

    var navUsers = document.getElementById('nav-users');
    if (navUsers) {
      if (me && me.canManageUsers) navUsers.classList.remove('hidden');
      else navUsers.classList.add('hidden');
    }

    var navDeploy = document.getElementById('nav-deploy');
    if (navDeploy) {
      if (me && me.canManageUsers) navDeploy.classList.remove('hidden');
      else navDeploy.classList.add('hidden');
    }

    var navManage = document.getElementById('nav-manage');
    if (navManage) {
      if (me && me.canManageUsers) navManage.classList.remove('hidden');
      else navManage.classList.add('hidden');
    }
  }

  function canWrite() {
    return !!(state.me && state.me.canWrite);
  }

  function canDelete() {
    return !!(state.me && state.me.canDeleteCompanies);
  }

  loadBranding().then(function () {
    return api('/api/health');
  }).then(function () {
    return api('/api/me');
  }).then(function (me) {
    applyPermissions(me);
    return api('/api/summary');
  }).then(function () {
    hideLogin();
    route();
  }).catch(function () {
    // нужна авторизация
    showLogin();
  });

})();
