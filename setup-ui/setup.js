/* ==========================================================================
   Логика мастера установки.

   Общается с движком по HTTP (локальный сервер внутри установщика), поэтому
   страница остаётся обычной: её можно открыть и в браузере для отладки.
   ========================================================================== */

(function () {
  'use strict';

  var state = {
    step: 1,
    env: null,
    session: null,
    mode: 'local',
    installing: false,
    pollTimer: null
  };

  // ---------------------------------------------------------------- утилиты

  function $(id) { return document.getElementById(id); }

  function esc(s) {
    if (s === null || s === undefined) return '';
    return String(s)
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
  }

  function api(path, opts) {
    opts = opts || {};
    var init = { method: opts.method || 'GET', headers: {} };
    if (opts.body) {
      init.headers['Content-Type'] = 'application/json';
      init.body = JSON.stringify(opts.body);
    }
    return fetch(path, init).then(function (r) {
      return r.text().then(function (t) {
        var data = null;
        try { data = t ? JSON.parse(t) : null; } catch (e) { data = null; }
        if (!r.ok) {
          throw new Error((data && data.error) || ('HTTP ' + r.status));
        }
        return data;
      });
    });
  }

  // ---------------------------------------------------------------- шаги

  var STEPS = 5;

  function go(step) {
    if (step < 1 || step > STEPS) return;
    state.step = step;

    for (var i = 1; i <= STEPS; i++) {
      var pane = $('pane-' + i);
      if (pane) pane.classList.toggle('active', i === step);
    }

    var items = document.querySelectorAll('.step');
    for (var j = 0; j < items.length; j++) {
      var n = parseInt(items[j].getAttribute('data-step'), 10);
      items[j].classList.toggle('active', n === step);
      items[j].classList.toggle('done', n < step);
    }

    var back = $('btn-back');
    var next = $('btn-next');
    back.disabled = step === 1 || state.installing;
    back.classList.toggle('hidden', step === 5);

    if (step === 4) {
      next.textContent = 'Установить';
      runPreflight();
    } else if (step === 5) {
      next.classList.add('hidden');
      back.classList.add('hidden');
    } else {
      next.innerHTML = 'Далее';
      next.classList.remove('hidden');
      back.classList.remove('hidden');
    }

    if (step === 1) $('foot-hint').textContent = '';
    if (step === 2) $('foot-hint').textContent = 'Способ можно поменять до установки';
    if (step === 3) {
      $('foot-hint').textContent = state.mode === 'local'
        ? 'Пути можно оставить по умолчанию'
        : 'Сначала проверьте связь с машиной';
      updateNextState();
    }
    if (step === 4) $('foot-hint').textContent = 'Проверка ничего не меняет';
    if (step === 5) $('foot-hint').textContent = '';
  }

  // ---------------------------------------------------------------- шаг 1

  function loadEnv() {
    return api('/api/env').then(function (env) {
      state.env = env;
      return api('/api/session').then(function (s) {
        state.session = s;
        env.setupVersion = (s && s.setupVersion) || '';
        renderEnv(env);
        renderPayload(s);
      });
    }).catch(function (err) {
      $('env-facts').innerHTML =
        '<div class="fact"><div class="fact-k">Ошибка</div>' +
        '<div class="fact-v">' + esc(err.message) + '</div></div>';
    });
  }

  function renderEnv(env) {
    $('top-version').textContent = 'v' + (env.setupVersion || '');

    var facts = [
      ['Компьютер', env.machineName],
      ['Пользователь', env.userName],
      ['Права', env.isAdmin ? 'администратор' : 'обычные'],
      ['ОС', env.osVersion],
      ['Домен', env.isDomainJoined ? 'в домене' : 'рабочая группа'],
      ['WebView2', env.webview2 && env.webview2.found
        ? env.webview2.version
        : 'не найден']
    ];

    $('env-facts').innerHTML = facts.map(function (f) {
      return '<div class="fact"><div class="fact-k">' + esc(f[0]) + '</div>' +
        '<div class="fact-v">' + esc(f[1] || '—') + '</div></div>';
    }).join('');

    var notes = '';
    if (!env.isAdmin) {
      notes += '<div class="note fail"><i class="fas fa-exclamation-circle"></i>' +
        '<div><b>Нет прав администратора.</b> Установщик запущен без повышения — ' +
        'создать службу не получится. Закройте мастер и запустите ' +
        '<code>BackupDotNet.Setup.exe</code> от имени администратора.</div></div>';
    }
    if (env.existing && env.existing.found) {
      notes += '<div class="note warn"><i class="fas fa-exclamation-triangle"></i>' +
        '<div><b>Агент уже установлен на этой машине.</b> ' +
        'Найденное: ' + esc((env.existing.folders || []).join(', ') || '—') +
        (env.existing.services && env.existing.services.length
          ? ', службы: ' + esc(env.existing.services.join(', ')) : '') +
        '. Установка перезапишет файлы, конфигурация останется прежней.</div></div>';
    }
    if (!env.isDomainJoined) {
      notes += '<div class="note"><i class="fas fa-info-circle"></i>' +
        '<div>Машина в рабочей группе. Установка по сети потребует ' +
        '<code>TrustedHosts</code> на цели — мастер предложит это позже.</div></div>';
    }
    $('env-notes').innerHTML = notes;

    // Значения по умолчанию для формы.
    var d = env.defaults || {};
    if ($('install-dir') && !$('install-dir').value) $('install-dir').value = d.installDir || '';
    if ($('data-dir') && !$('data-dir').value) $('data-dir').value = d.dataDir || '';
    if ($('service-name') && !$('service-name').value) $('service-name').value = d.serviceName || '';
    if ($('port') && !$('port').value) $('port').value = d.port || 8080;
  }

  function renderPayload(session) {
    var box = $('payload-box');
    if (!box) return;

    if (session && session.payloadReady) {
      box.innerHTML =
        '<div class="note ok" style="margin-bottom:0"><i class="fas fa-check-circle"></i>' +
        '<div><b>Пакет на месте.</b> Версия агента: ' +
        esc(session.payloadVersion || 'неизвестна') + '.<br>' +
        'Источник: <code>' + esc(session.payloadDir) + '</code></div></div>';
    } else {
      box.innerHTML =
        '<div class="note fail" style="margin-bottom:0"><i class="fas fa-exclamation-circle"></i>' +
        '<div><b>Файлы агента не найдены.</b> Рядом с установщиком нужен каталог ' +
        '<code>payload</code> с <code>BackupDotNet.exe</code>. ' +
        'Распакуйте архив целиком.</div></div>';
    }
  }

  // ---------------------------------------------------------------- шаг 2

  function selectMode(mode) {
    state.mode = mode;
    var modes = document.querySelectorAll('.mode');
    for (var i = 0; i < modes.length; i++) {
      modes[i].classList.toggle('sel', modes[i].getAttribute('data-mode') === mode);
    }

    var isLocal = mode === 'local';
    $('form-local').classList.toggle('hidden', !isLocal);
    $('form-remote').classList.toggle('hidden', isLocal);

    // Порт и HTTPS — только для WinRM.
    var isWinRm = mode === 'winrm';
    $('rm-port-wrap').classList.toggle('hidden', !isWinRm);
    $('rm-https-wrap').classList.toggle('hidden', !isWinRm);

    var hints = {
      local: 'Куда поставить агент и как назвать службу.',
      smb: 'Нужны права администратора на целевой машине и доступ к админ-шарам.',
      winrm: 'Нужна служба WinRM на цели. В рабочей группе — TrustedHosts.',
      ad: 'Поиск компьютеров в каталоге появится следующим шагом.'
    };
    $('pane3-lead').textContent = hints[mode] || '';

    // Подсказка под формой.
    var hintText = {
      smb: 'Копирование идёт через <code>\\\\имя\\ADMIN$</code>, служба создаётся ' +
        'через <code>sc.exe</code> по RPC. WinRM при этом не нужен — поэтому способ ' +
        'работает и там, где PowerShell Remoting выключен политикой.',
      winrm: 'Файлы передаются по самому WinRM, доступ к админ-шарам не требуется. ' +
        'Медленнее SMB, но проходит там, где шары закрыты.'
    }[mode] || '';
    $('rm-hint-text').innerHTML = hintText;
    $('rm-hint').classList.toggle('hidden', !hintText);

    var next = $('btn-next');
    if (state.step === 2) {
      next.disabled = mode === 'ad';
      next.title = mode === 'ad' ? 'Этот способ ещё в работе' : '';
    }
  }

  /** Проверка связи с удалённой машиной. */
  function testRemote() {
    var btn = $('rm-test');
    var out = $('rm-test-result');
    var host = $('rm-host').value.trim();
    if (!host) {
      out.textContent = 'укажите компьютер';
      out.style.color = 'var(--warn)';
      return;
    }

    btn.disabled = true;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Проверяю…';
    out.textContent = '';
    out.style.color = '';

    var payload = {
      mode: state.mode,
      host: host,
      user: $('rm-user').value.trim(),
      password: $('rm-pass').value,
      port: parseInt($('rm-port').value, 10) || 5985,
      https: $('rm-https').checked
    };

    api('/api/remote/diagnose', { method: 'POST', body: payload }).then(function (d) {
      state.diag = d;
      state.remoteOk = !!d.ok;
      renderDiagnostics(d);

      var broken = (d.broken || []).length;
      out.style.color = d.ok ? 'var(--ok)' : 'var(--fail)';
      out.textContent = d.ok
        ? 'всё в порядке'
        : 'препятствий: ' + broken;

      $('rm-retest').classList.toggle('hidden', d.ok);
      updateNextState();

      if (!d.ok) {
        loadFixScript();
      } else {
        $('rm-fix-card').classList.add('hidden');
      }
    }).catch(function (err) {
      out.style.color = 'var(--fail)';
      out.textContent = 'Ошибка: ' + err.message;
      state.remoteOk = false;
      updateNextState();
    }).then(function () {
      btn.disabled = false;
      btn.innerHTML = '<i class="fas fa-stethoscope"></i> Проверить связь';
    });
  }

  /** Показывает список проверок с состояниями. */
  function renderDiagnostics(d) {
    var card = $('rm-diag-card');
    var list = $('rm-diag-list');
    var checks = d.checks || [];

    if (!checks.length) {
      card.classList.add('hidden');
      return;
    }

    var icons = {
      ok: '<i class="fas fa-check-circle" style="color:var(--ok)"></i>',
      warn: '<i class="fas fa-exclamation-triangle" style="color:var(--warn)"></i>',
      fail: '<i class="fas fa-exclamation-circle" style="color:var(--fail)"></i>',
      skip: '<i class="fas fa-minus" style="color:var(--muted)"></i>'
    };

    list.innerHTML = checks.map(function (c) {
      var icon = icons[c.state] || icons.skip;
      var fix = c.fixHint
        ? '<div class="check-detail" style="color:var(--warn)">→ ' + esc(c.fixHint) + '</div>'
        : '';
      return '<li class="check-item ' + esc(c.state) + '">' + icon +
        '<div class="check-text">' + esc(c.title) +
        '<div class="check-detail">' + esc(c.detail) + '</div>' + fix + '</div></li>';
    }).join('');

    var okCount = checks.filter(function (c) { return c.state === 'ok'; }).length;
    $('rm-diag-summary').textContent = 'пройдено ' + okCount + ' из ' + checks.length;
    card.classList.remove('hidden');
  }

  /** Загружает скрипт исправления для найденных проблем. */
  function loadFixScript() {
    if (!state.diag) return;

    var format = $('rm-fix-format').value;
    api('/api/remote/fix', { method: 'POST', body: {
      broken: state.diag.broken || [],
      mode: state.mode,
      format: format
    } }).then(function (r) {
      var card = $('rm-fix-card');
      if (r.empty) {
        // Скрипт не всегда возможен: имя не разрешается, машина выключена.
        card.classList.add('hidden');
        if (r.note) {
          var out = $('rm-test-result');
          out.textContent = r.note;
          out.style.color = 'var(--warn)';
        }
        return;
      }
      $('rm-fix-text').textContent = r.script;
      card.classList.remove('hidden');
    }).catch(function (err) {
      $('rm-fix-text').textContent = 'Не удалось собрать скрипт: ' + err.message;
      $('rm-fix-card').classList.remove('hidden');
    });
  }

  /** Копирует скрипт в буфер обмена. */
  function copyFixScript() {
    var text = $('rm-fix-text').textContent || '';
    if (!text) return;

    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).then(function () {
        var btn = $('rm-fix-copy');
        var old = btn.innerHTML;
        btn.innerHTML = '<i class="fas fa-check"></i> Скопировано';
        setTimeout(function () { btn.innerHTML = old; }, 1600);
      }).catch(function () {
        fallbackCopy(text);
      });
    } else {
      fallbackCopy(text);
    }
  }

  /** Запасной способ копирования: старый API, работает и в WebView2. */
  function fallbackCopy(text) {
    var ta = document.createElement('textarea');
    ta.value = text;
    ta.style.position = 'fixed';
    ta.style.left = '-9999px';
    document.body.appendChild(ta);
    ta.select();
    try {
      document.execCommand('copy');
      var btn = $('rm-fix-copy');
      var old = btn.innerHTML;
      btn.innerHTML = '<i class="fas fa-check"></i> Скопировано';
      setTimeout(function () { btn.innerHTML = old; }, 1600);
    } catch (e) {
      alert('Не удалось скопировать. Выделите текст и нажмите Ctrl+C.');
    }
    document.body.removeChild(ta);
  }

  /** Сохраняет скрипт в файл рядом с установщиком. */
  function saveFixScript() {
    var text = $('rm-fix-text').textContent || '';
    if (!text) return;
    var format = $('rm-fix-format').value;
    var name = 'prepare-host.' + format;

    api('/api/save-script', { method: 'POST', body: { name: name, text: text } })
      .then(function (r) {
        toastLine('Сохранено: ' + (r.path || name));
      })
      .catch(function (err) {
        toastLine('Не удалось сохранить: ' + err.message);
      });
  }

  function toastLine(text) {
    var out = $('rm-test-result');
    if (!out) return;
    out.textContent = text;
    out.style.color = 'var(--ink-soft)';
  }

  /**
   * Открывает RDP к цели и кладёт скрипт в буфер: человеку останется
   * ввести пароль и вставить текст.
   */
  function openRdp() {
    var host = $('rm-host').value.trim();
    if (!host) {
      toastLine('укажите компьютер');
      return;
    }

    var btn = $('rm-rdp');
    var old = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Открываю…';

    // Скрипт копируем ДО запуска RDP: если подключение развернётся на весь
    // экран, взять текст из установщика будет уже неудобно.
    var scriptText = ($('rm-fix-text') || {}).textContent || '';
    if (scriptText) {
      copyFixScript();
    }

    api('/api/rdp/open', { method: 'POST', body: {
      host: host,
      user: $('rm-user').value.trim(),
      adminSession: true,
      launch: true
    } }).then(function (r) {
      var text = r.message || '';
      if (scriptText) {
        text += ' Скрипт в буфере обмена: вставьте в PowerShell от имени администратора.';
      }
      toastLine(text);
    }).catch(function (err) {
      toastLine('Ошибка RDP: ' + err.message);
    }).then(function () {
      btn.disabled = false;
      btn.innerHTML = old;
    });
  }

  /** Кнопка «Далее» активна, если для сетевого режима связь проверена. */
  function updateNextState() {
    if (state.step !== 3) return;
    var next = $('btn-next');
    if (state.mode === 'local') {
      next.disabled = false;
      next.title = '';
      return;
    }
    next.disabled = !state.remoteOk;
    next.title = state.remoteOk ? '' : 'Сначала проверьте связь с машиной';
  }

  // ---------------------------------------------------------------- шаг 4

  function collectRequest() {
    if (state.mode === 'local') {
      return {
        mode: 'local',
        installDir: $('install-dir').value.trim(),
        dataDir: $('data-dir').value.trim(),
        serviceName: $('service-name').value.trim(),
        port: parseInt($('port').value, 10) || 8080,
        overwriteConfig: $('overwrite-config').checked
      };
    }

    return {
      mode: state.mode,
      host: $('rm-host').value.trim(),
      user: $('rm-user').value.trim(),
      password: $('rm-pass').value,
      port: parseInt($('rm-port').value, 10) || 5985,
      https: $('rm-https').checked,
      installDir: $('rm-install-dir').value.trim(),
      dataDir: $('rm-data-dir').value.trim(),
      serviceName: $('rm-service').value.trim(),
      webPort: parseInt($('rm-webport').value, 10) || 8080
    };
  }

  function runPreflight() {
    var list = $('plan-list');
    list.innerHTML = '<li class="check-item"><i class="fas fa-spinner fa-spin"></i>' +
      '<div class="check-text">Проверяю условия…</div></li>';

    var req = collectRequest();

    // Для сетевых режимов проверяем связь и права на цели.
    if (req.mode !== 'local') {
      api('/api/remote/test', { method: 'POST', body: req })
        .then(function (r) {
          renderRemotePreflight(r, req);
        })
        .catch(function (err) {
          list.innerHTML = '<li class="check-item fail"><i class="fas fa-exclamation-circle"></i>' +
            '<div class="check-text">Ошибка проверки: ' + esc(err.message) + '</div></li>';
        });
      return;
    }

    api('/api/preflight', { method: 'POST', body: req })
      .then(function (p) {
        renderPreflight(p);
      })
      .catch(function (err) {
        list.innerHTML = '<li class="check-item fail"><i class="fas fa-exclamation-circle"></i>' +
          '<div class="check-text">Ошибка проверки: ' + esc(err.message) + '</div></li>';
      });
  }

  function renderRemotePreflight(r, req) {
    var rows = [];
    var transport = req.mode === 'winrm'
      ? 'через WinRM (порт ' + req.port + ')'
      : 'через админ-шару ADMIN$';

    [
      ['Подключиться к ' + esc(req.host), transport],
      ['Скопировать файлы', 'во временный каталог на цели'],
      ['Создать службу', esc(req.serviceName) + ' с автозапуском'],
      ['Открыть порт', 'firewall и urlacl для ' + req.webPort],
      ['Запустить и проверить', 'состояние службы на цели']
    ].forEach(function (row) {
      rows.push('<li class="check-item"><i class="fas fa-angle-right" style="color:var(--muted)"></i>' +
        '<div class="check-text">' + row[0] +
        '<div class="check-detail">' + row[1] + '</div></div></li>');
    });

    // Результат проверки — сверху.
    if (r.ok) {
      rows.unshift('<li class="check-item ok"><i class="fas fa-check-circle"></i>' +
        '<div class="check-text">Связь есть<div class="check-detail">' +
        esc((r.steps || []).join(', ')) + '</div></div></li>');
    } else {
      rows.unshift('<li class="check-item fail"><i class="fas fa-exclamation-circle"></i>' +
        '<div class="check-text">' + esc(r.message || 'нет связи') + '</div></li>');
    }

    $('plan-list').innerHTML = rows.join('');

    var notes = '';
    (r.warnings || []).forEach(function (t) {
      notes += '<div class="note warn"><i class="fas fa-exclamation-triangle"></i>' +
        '<div>' + esc(t) + '</div></div>';
    });
    $('preflight-notes').innerHTML = notes;

    $('btn-next').disabled = !r.ok;
    $('btn-next').title = r.ok ? '' : 'Сначала устраните проблему со связью';
  }

  function renderPreflight(p) {
    var req = collectRequest();
    var rows = [];

    // Что будет сделано — списком, чтобы человек понимал масштаб.
    var planned = [
      ['Создать каталоги', esc(req.installDir) + ' и ' + esc(req.dataDir)],
      ['Скопировать файлы', 'из пакета в каталог программы'],
      ['Создать конфигурацию', 'из шаблонов *.example.json'],
      ['Зарегистрировать службу', esc(req.serviceName) + ' с автозапуском'],
      ['Открыть порт', 'firewall и urlacl для ' + req.port],
      ['Запустить и проверить', 'ответ /api/health на порту ' + req.port]
    ];

    planned.forEach(function (row) {
      rows.push('<li class="check-item"><i class="fas fa-angle-right" style="color:var(--muted)"></i>' +
        '<div class="check-text">' + esc(row[0]) +
        '<div class="check-detail">' + row[1] + '</div></div></li>');
    });

    $('plan-list').innerHTML = rows.join('');

    var notes = '';
    (p.problems || []).forEach(function (t) {
      notes += '<div class="note fail"><i class="fas fa-exclamation-circle"></i>' +
        '<div>' + esc(t) + '</div></div>';
    });
    (p.warnings || []).forEach(function (t) {
      notes += '<div class="note warn"><i class="fas fa-exclamation-triangle"></i>' +
        '<div>' + esc(t) + '</div></div>';
    });
    if (p.ok && !(p.warnings || []).length) {
      notes = '<div class="note ok"><i class="fas fa-check-circle"></i>' +
        '<div>Всё готово. Можно устанавливать.</div></div>';
    }
    $('preflight-notes').innerHTML = notes;

    $('btn-next').disabled = !p.ok;
    $('btn-next').title = p.ok ? '' : 'Есть препятствия — см. выше';
  }

  // ---------------------------------------------------------------- шаг 5

  function startInstall() {
    state.installing = true;
    go(5);

    var req = collectRequest();
    var remote = req.mode !== 'local';

    $('install-title').textContent = 'Установка';
    $('install-lead').textContent = remote
      ? 'Не закрывайте окно. Установка на ' + req.host + ' занимает до нескольких минут.'
      : 'Не закрывайте окно. Обычно занимает до минуты.';
    $('done-box').classList.add('hidden');
    $('bar').style.width = '0%';
    $('progress-step').textContent = 'Готовлюсь…';
    $('install-log').textContent = '';

    var endpoint = remote ? '/api/install/remote' : '/api/install/local';

    api(endpoint, { method: 'POST', body: req })
      .then(function (r) {
        if (!r.ok) {
          appendLog('Ошибка: ' + (r.error || r.message));
          finish(false, r);
          return;
        }
        poll();
      })
      .catch(function (err) {
        appendLog('Ошибка: ' + err.message);
        finish(false, { message: err.message });
      });
  }

  function poll() {
    clearTimeout(state.pollTimer);
    api('/api/progress').then(function (p) {
      if (p.step) $('progress-step').textContent = p.step;
      if (typeof p.percent === 'number') $('bar').style.width = Math.max(0, Math.min(100, p.percent)) + '%';
      if (p.log) $('install-log').textContent = p.log;

      if (p.done) {
        finish(p.ok, p.result || {});
        return;
      }
      state.pollTimer = setTimeout(poll, 500);
    }).catch(function () {
      state.pollTimer = setTimeout(poll, 1200);
    });
  }

  function appendLog(text) {
    var box = $('install-log');
    if (box) box.textContent = (box.textContent ? box.textContent + '\n' : '') + text;
  }

  function finish(ok, result) {
    state.installing = false;
    $('bar').style.width = '100%';
    $('progress-step').textContent = ok ? 'Готово' : 'Установка не завершена';

    if (!ok) {
      $('install-title').textContent = 'Установка не завершена';
      $('install-lead').textContent = 'Ниже — что именно не получилось.';
      $('done-box').classList.add('hidden');
      var back = $('btn-back');
      back.classList.remove('hidden');
      back.textContent = 'Вернуться к параметрам';
      back.disabled = false;
      back.onclick = function () {
        back.onclick = null;
        back.textContent = 'Назад';
        go(3);
      };
      return;
    }

    $('install-title').textContent = 'Установка завершена';
    $('install-lead').textContent = '';
    $('done-box').classList.remove('hidden');
    $('done-sub').textContent = result.message || '';

    var kv = [];
    var req = collectRequest();
    var remote = req.mode !== 'local';
    var webPort = remote ? req.webPort : req.port;

    if (remote) {
      kv.push(['Компьютер', req.host]);
      kv.push(['Способ', req.mode === 'winrm' ? 'WinRM' : 'SMB (админ-шара)']);
    }
    kv.push(['Каталог', req.installDir]);
    kv.push(['Служба', req.serviceName]);
    kv.push(['Порт', String(webPort)]);
    if (result.warnings && result.warnings.length) {
      kv.push(['Замечания', result.warnings.join('; ')]);
    }
    $('done-kv').innerHTML = kv.map(function (r) {
      return '<div class="k">' + esc(r[0]) + '</div><div class="v">' + esc(r[1]) + '</div>';
    }).join('');

    var back = $('btn-back');
    back.classList.remove('hidden');
    back.textContent = 'Закрыть мастер';
    back.disabled = false;
    back.onclick = function () {
      back.onclick = null;
      api('/api/close', { method: 'POST', body: {} }).catch(function () {});
      back.textContent = 'Мастер можно закрыть';
      back.disabled = true;
    };

    // Ссылка на веб-интерфейс: для удалённой установки — адрес цели.
    var hostForUrl = remote ? req.host : 'localhost';
    var link = document.createElement('div');
    link.className = 'note ok';
    link.style.marginTop = '14px';
    link.innerHTML = '<i class="fas fa-check-circle"></i><div>' +
      'Открыть веб-интерфейс: <a href="http://' + esc(hostForUrl) + ':' + webPort +
      '/" target="_blank">http://' + esc(hostForUrl) + ':' + webPort + '/</a></div>';
    $('done-box').appendChild(link);
  }

  // ---------------------------------------------------------------- события

  function bind() {
    $('btn-next').addEventListener('click', function () {
      if (state.step === 4) {
        startInstall();
        return;
      }
      go(state.step + 1);
    });

    $('btn-back').addEventListener('click', function () {
      if (state.step === 5) return;
      go(state.step - 1);
    });

    var modes = document.querySelectorAll('.mode');
    for (var i = 0; i < modes.length; i++) {
      modes[i].addEventListener('click', function (e) {
        e.preventDefault();
        selectMode(this.getAttribute('data-mode'));
      });
    }

    $('rm-test').addEventListener('click', testRemote);
    $('rm-retest').addEventListener('click', testRemote);
    $('rm-fix-copy').addEventListener('click', copyFixScript);
    $('rm-fix-save').addEventListener('click', saveFixScript);
    $('rm-rdp').addEventListener('click', openRdp);
    $('rm-fix-format').addEventListener('change', loadFixScript);

    // Правка полей удалённой установки сбрасывает проверку: связь могла
    // измениться, и «Далее» должна снова требовать теста.
    ['rm-host', 'rm-user', 'rm-pass', 'rm-port'].forEach(function (id) {
      var el = $(id);
      if (el) {
        el.addEventListener('input', function () {
          state.remoteOk = false;
          state.diag = null;
          $('rm-test-result').textContent = '';
          $('rm-retest').classList.add('hidden');
          $('rm-diag-card').classList.add('hidden');
          $('rm-fix-card').classList.add('hidden');
          updateNextState();
        });
      }
    });
  }

  // ---------------------------------------------------------------- старт

  document.addEventListener('DOMContentLoaded', function () {
    bind();
    selectMode('local');
    loadEnv().then(function () {
      go(1);
    });
  });
})();
