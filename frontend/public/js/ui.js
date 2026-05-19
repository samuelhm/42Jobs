const PALETTE = [
  '#e6a845', '#d06b5a', '#5b9fd4', '#55c980', '#c4a43e',
  '#e87860', '#4d8dc0', '#48b868', '#d49c3a', '#c06088',
  '#57b0d8', '#e89068', '#6cac58', '#b870d0', '#d4a848',
  '#609cc8', '#e8a040', '#58c0a0', '#d06078', '#7898d0',
  '#e09858', '#48b8c0', '#b8c058', '#c878a0', '#d8b048',
  '#5c90c8', '#a8d060', '#e08060', '#50b0a8', '#d078b0',
  '#68b0d0', '#e8b058', '#80a8d8', '#c8a040', '#60c898',
  '#d8a068', '#a8b8e0', '#e0c058', '#58b8b8', '#d89078'
];

var categories = [];
var currentCategoryId = null;
var currentJobs = [];
var currentKeywords = [];
var userKeywords = {};
var chart = null;
var notesTimer = null;
var notesCurrentJobId = null;

function escHtml(str) {
  if (!str) return '';
  return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

function formatDate(dateStr) {
  if (!dateStr) return '-';
  return dateStr.split('T')[0];
}

function isRecent(dateStr) {
  if (!dateStr) return false;
  var d = new Date(dateStr);
  var now = new Date();
  return (now - d) < 24 * 60 * 60 * 1000;
}

async function loadUserKeywords() {
  var res = await API.getUserKeywords();
  if (res.success) {
    userKeywords = {};
    res.data.forEach(function (k) { userKeywords[k.name] = { id: k.id, status: k.learning_status }; });
  }
}

async function loadCategories() {
  var res = await API.getCategories();
  if (res.success) {
    categories = res.data;
    renderTabs();
    if (categories.length > 0) { selectTab(categories[0].id); }
    else { renderEmpty(); }
  }
}

function renderTabs() {
  var scroll = document.getElementById('tabs-scroll');
  scroll.innerHTML = categories.map(function (c) {
    return '<button class="tab-btn" data-id="' + c.id + '">' + escHtml(c.name) + '<span class="tab-count">' + c.job_count + '</span></button>';
  }).join('');
  scroll.querySelectorAll('.tab-btn').forEach(function (btn) {
    btn.addEventListener('click', function () { selectTab(Number(btn.dataset.id)); });
  });
}

function selectTab(categoryId) {
  currentCategoryId = categoryId;
  document.querySelectorAll('.tab-btn').forEach(function (b) { b.classList.toggle('active', Number(b.dataset.id) === categoryId); });
  loadCategoryData(categoryId);
}

async function loadCategoryData(categoryId) {
  showLoading(true);
  var results = await Promise.all([API.getCategoryJobs(categoryId), API.getCategoryKeywords(categoryId)]);
  currentJobs = results[0].success ? results[0].data : [];
  currentKeywords = results[1].success ? results[1].data : [];
  renderJobList(); renderChart(); showLoading(false);
}

function getMatchPct(job) {
  if (!job.keywords || job.keywords.length === 0) return 0;
  var matchCount = 0;
  job.keywords.forEach(function (kw) {
    var uk = userKeywords[kw];
    if (uk && uk.status !== 'not_learned') matchCount++;
  });
  return Math.round((matchCount / job.keywords.length) * 100);
}

function getMatchClass(pct) { if (pct >= 50) return 'high'; if (pct >= 20) return 'medium'; return 'low'; }

async function handleDeleteJob(jobId, jobTitle) {
  if (!confirm('Eliminar "' + jobTitle + '"?')) return;
  var res = await API.deleteJob(jobId);
  if (res.success) {
    currentJobs = currentJobs.filter(function (j) { return j.id !== jobId; });
    renderJobList();
    await loadCategories();
  }
}

function renderJobList() {
  var container = document.getElementById('ofertas-list');
  var count = document.getElementById('offer-count');
  var summary = document.getElementById('summary');
  count.textContent = currentJobs.length + ' ofertas';
  var cat = categories.find(function (c) { return c.id === currentCategoryId; });
  summary.textContent = 'Monitor de ' + currentJobs.length + ' posiciones \u00b7 ' + (cat ? cat.name : '');

  if (currentJobs.length === 0) {
    container.innerHTML = '<div class="empty-state"><p>No hay ofertas</p><p class="hint">Usa + para buscar</p></div>';
    document.getElementById('kw-count').textContent = '0 keywords';
    return;
  }

  container.innerHTML = currentJobs.map(function (job, i) {
    var pct = getMatchPct(job);
    var matchClass = getMatchClass(pct);
    var badgeHtml = job.company_type ? '<span class="badge ' + escHtml(job.company_type) + '">' + escHtml(job.company_type) + '</span>' : '';
    var notesIcon = job.notes ? 'has-notes' : '';
    var keywordTags = renderKeywordTags(job.keywords);
    var desc = job.description || '';
    var descHtml = desc ? '<div class="accordion-field desc-field"><div class="label">Descripcion</div><div class="value desc-text">' + escHtml(desc) + '</div></div>' : '';
    var recentBadge = isRecent(job.posted_date) ? '<span class="recent-badge">Nuevo</span>' : '';

    return '<div class="oferta-card' + (isRecent(job.posted_date) ? ' recent' : '') + '" data-index="' + i + '" id="oferta-' + i + '">' +
      '<div class="oferta-info">' +
        '<h3>' + escHtml(job.title) + recentBadge + '</h3>' +
        '<div class="oferta-meta">' +
          '<span class="empresa">' + escHtml(job.company_name) + '</span>' + badgeHtml +
        '</div>' +
      '</div>' +
      '<div class="card-controls">' +
        '<span class="match-badge ' + matchClass + '">' + pct + '%</span>' +
        '<button class="notes-btn ' + notesIcon + '" data-job-id="' + job.id + '" data-job-title="' + escHtml(job.title) + '">' +
          '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20h9"/><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"/></svg>' +
        '</button>' +
        '<button class="notes-btn refresh-btn" data-job-id="' + job.id + '" data-job-title="' + escHtml(job.title) + '" title="Re-obtener detalles y keywords">' +
          '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="23 4 23 10 17 10"/><polyline points="1 20 1 14 7 14"/><path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"/></svg>' +
        '</button>' +
        '<button class="notes-btn cv-btn" data-job-id="' + job.id + '" data-job-title="' + escHtml(job.title) + '" title="Generar CV para esta oferta">' +
          '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/><polyline points="10 9 9 9 8 9"/></svg>' +
        '</button>' +
        '<button class="notes-btn delete-btn" data-job-id="' + job.id + '" data-job-title="' + escHtml(job.title) + '">' +
          '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>' +
        '</button>' +
        '<a class="oferta-link" href="' + escHtml(job.job_url) + '" target="_blank" rel="noopener">ver' +
          '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"/><polyline points="15 3 21 3 21 9"/><line x1="10" y1="14" x2="21" y2="3"/></svg>' +
        '</a>' +
      '</div>' +
      '<div class="oferta-accordion">' +
        '<div class="accordion-grid">' +
          '<div class="accordion-field"><div class="label">Ubicacion</div><div class="value">' + escHtml(job.location || '-') + '</div></div>' +
          '<div class="accordion-field"><div class="label">Fecha</div><div class="value">' + formatDate(job.posted_date) + '</div></div>' +
          '<div class="accordion-field"><div class="label">Salario</div><div class="value">' + escHtml(job.salary || '-') + '</div></div>' +
          '<div class="accordion-field"><div class="label">Beneficios</div><div class="value">' + escHtml(job.benefits || '-') + '</div></div>' +
        '</div>' +
        descHtml +
        '<div class="accordion-kw-list">' + keywordTags + '</div>' +
      '</div>' +
    '</div>';
  }).join('');

  container.querySelectorAll('.oferta-card').forEach(function (card) {
    card.addEventListener('click', function (e) {
      if (e.target.closest('.notes-btn') || e.target.closest('.oferta-link') || e.target.closest('.kw-tag')) return;
      card.classList.toggle('expanded');
    });
  });

  container.querySelectorAll('.notes-btn:not(.delete-btn):not(.refresh-btn):not(.cv-btn)').forEach(function (btn) {
    btn.addEventListener('click', function (e) { e.stopPropagation(); openNotesModal(Number(btn.dataset.jobId), btn.dataset.jobTitle); });
  });

  container.querySelectorAll('.delete-btn').forEach(function (btn) {
    btn.addEventListener('click', function (e) { e.stopPropagation(); handleDeleteJob(Number(btn.dataset.jobId), btn.dataset.jobTitle); });
  });

  container.querySelectorAll('.refresh-btn').forEach(function (btn) {
    btn.addEventListener('click', async function (e) {
      e.stopPropagation();
      btn.disabled = true;
      try {
        var res = await API.refreshJob(Number(btn.dataset.jobId));
        if (res.success) { await loadUserKeywords(); loadCategoryData(currentCategoryId); }
      } catch (err) {}
      btn.disabled = false;
    });
  });

  container.querySelectorAll('.cv-btn').forEach(function (btn) {
    btn.addEventListener('click', async function (e) {
      e.stopPropagation();
      btn.disabled = true;
      openCvPanel(Number(btn.dataset.jobId), btn.dataset.jobTitle);
      btn.disabled = false;
    });
  });
}

function renderKeywordTags(keywords) {
  return keywords.map(function (kw) {
    var uk = userKeywords[kw];
    var status = uk ? uk.status : 'not_learned';
    return '<span class="kw-tag ' + status + '" data-keyword="' + escHtml(kw) + '">' + escHtml(kw) + '</span>';
  }).join('');
}

function openNotesModal(jobId, title) {
  notesCurrentJobId = jobId;
  document.getElementById('notes-modal-title').textContent = title;
  var textarea = document.getElementById('notes-modal-textarea');
  var job = currentJobs.find(function (j) { return j.id === jobId; });
  textarea.value = job ? (job.notes || '') : '';
  document.getElementById('notes-modal').classList.remove('hidden');
  document.getElementById('notes-modal-saved').style.opacity = '0';
  setTimeout(function () { textarea.focus(); }, 100);
}

function setupNotesModal() {
  var modal = document.getElementById('notes-modal'), closeBtn = document.getElementById('notes-modal-close'), textarea = document.getElementById('notes-modal-textarea'), saved = document.getElementById('notes-modal-saved');
  closeBtn.addEventListener('click', function () { modal.classList.add('hidden'); });
  modal.addEventListener('click', function (e) { if (e.target === modal) modal.classList.add('hidden'); });
  textarea.addEventListener('input', function () {
    clearTimeout(notesTimer); saved.style.opacity = '0';
    notesTimer = setTimeout(function () {
      API.updateJobNotes(notesCurrentJobId, textarea.value).then(function (res) {
        if (res && res.success) {
          saved.style.opacity = '1';
          var job = currentJobs.find(function (j) { return j.id === notesCurrentJobId; });
          if (job) { job.notes = textarea.value; var btn = document.querySelector('.notes-btn[data-job-id="' + notesCurrentJobId + '"]'); if (btn) btn.classList.toggle('has-notes', !!textarea.value); }
          setTimeout(function () { saved.style.opacity = '0'; }, 2000);
        }
      });
    }, 800);
  });
}

function setupUpdateButton() {
  var btn = document.getElementById('update-btn');
  var status = document.getElementById('update-status');
  btn.addEventListener('click', async function () {
    var cat = categories.find(function (c) { return c.id === currentCategoryId; });
    if (!cat) return;
    btn.disabled = true;
    status.classList.remove('hidden');
    status.textContent = 'Actualizando...';
    try {
      var res = await API.updateCategory(cat.name);
      if (res.success) {
        status.textContent = 'Ok \u2014 ' + res.inserted + ' nuevas ofertas';
        await loadUserKeywords();
        await loadCategories();
        loadCategoryData(currentCategoryId);
        setTimeout(function () { status.classList.add('hidden'); }, 3000);
      } else {
        status.textContent = 'Error: ' + (res.error || 'desconocido');
      }
    } catch (err) { status.textContent = 'Error: ' + err.message; }
    btn.disabled = false;
  });
}

function renderChart() {
  if (chart) { chart.destroy(); chart = null; }
  document.getElementById('kw-count').textContent = currentKeywords.length + ' keywords';
  if (currentKeywords.length === 0) return;
  var data = currentKeywords.map(function (k) { return k.count; });
  var labels = currentKeywords.map(function (k) { return k.name; });
  var colors = labels.map(function (_, i) { return PALETTE[i % PALETTE.length]; });
  var ctx = document.getElementById('keywords-chart').getContext('2d');
  chart = new Chart(ctx, {
    type: 'doughnut',
    data: { labels: labels, datasets: [{ data: data, backgroundColor: colors, borderColor: '#21201d', borderWidth: 2, hoverBorderColor: '#ddd6c8', hoverBorderWidth: 2 }] },
    options: {
      responsive: true, cutout: '55%',
      plugins: {
        legend: { display: false },
        tooltip: { backgroundColor: '#21201d', titleColor: '#f5efe0', bodyColor: '#ddd6c8', borderColor: '#5a5240', borderWidth: 1, padding: 10, titleFont: { family: 'JetBrains Mono', size: 12 }, bodyFont: { family: 'JetBrains Mono', size: 11 }, callbacks: { label: function (c) { return ' ' + c.label + ': ' + c.parsed + ' ofertas'; } } }
      },
      onHover: function (event, elements) {
        if (elements.length > 0) { var idx = elements[0].index; highlightJobsByKeyword(chart.data.labels[idx]); updateHoverInfo(currentKeywords.find(function (k) { return k.name === chart.data.labels[idx]; })); }
        else { highlightJobsByKeyword(null); updateHoverInfo(null); }
      }
    }
  });
}

function highlightJobsByKeyword(kwName) {
  document.querySelectorAll('.oferta-card').forEach(function (card, i) {
    if (!kwName) { card.classList.remove('highlighted', 'dimmed'); }
    else { var job = currentJobs[i]; var hasKeyword = job.keywords.some(function (k) { return k.toLowerCase() === kwName.toLowerCase(); }); card.classList.toggle('highlighted', hasKeyword); card.classList.toggle('dimmed', !hasKeyword); }
  });
}

function updateHoverInfo(entry) {
  var el = document.getElementById('hover-info');
  if (!entry) { el.classList.remove('active'); el.innerHTML = 'Pasa el cursor sobre un segmento para ver las ofertas que contienen esa keyword.'; return; }
  el.classList.add('active');
  el.innerHTML = '<strong>' + escHtml(entry.name) + '</strong> \u2014 ' + entry.count + ' ofertas<div class="tag-cloud">' + currentJobs.filter(function (j) { return j.keywords.some(function (k) { return k.toLowerCase() === entry.name.toLowerCase(); }); }).map(function (j) { return '<span class="tag">' + escHtml(j.title) + '</span>'; }).join('') + '</div>';
}

function setupAddButton() {
  var addBtn = document.getElementById('tab-add'), dialog = document.getElementById('add-dialog'), input = document.getElementById('add-keyword'), cancelBtn = document.getElementById('add-cancel'), confirmBtn = document.getElementById('add-confirm'), status = document.getElementById('add-status');
  addBtn.addEventListener('click', function () { dialog.classList.remove('hidden'); input.value = ''; status.classList.add('hidden'); input.focus(); });
  cancelBtn.addEventListener('click', function () { dialog.classList.add('hidden'); });
  dialog.addEventListener('click', function (e) { if (e.target === dialog) dialog.classList.add('hidden'); });
  input.addEventListener('keydown', function (e) { if (e.key === 'Enter') confirmBtn.click(); if (e.key === 'Escape') cancelBtn.click(); });
  confirmBtn.addEventListener('click', async function () {
    var kw = input.value.trim(); if (!kw) return;
    confirmBtn.disabled = true; status.classList.remove('hidden'); status.textContent = 'Buscando ofertas en LinkedIn...';
    try {
      var res = await API.fetchLinkedinSearch(kw);
      if (res.success) { status.textContent = 'Ok \u2014 ' + res.jobsFound + ' encontradas, ' + res.inserted + ' nuevas'; await loadUserKeywords(); await loadCategories(); var nc = categories.find(function (c) { return c.name === kw; }); if (nc) selectTab(nc.id); setTimeout(function () { dialog.classList.add('hidden'); }, 1500); }
      else { status.textContent = 'Error: ' + (res.error || 'desconocido'); }
    } catch (err) { status.textContent = 'Error: ' + err.message; }
    confirmBtn.disabled = false;
  });
}

function setupKeywordModal() {
  var modal = document.getElementById('kw-modal'), title = document.getElementById('kw-modal-title'), closeBtn = document.getElementById('kw-modal-close'), curKw = null;
  closeBtn.addEventListener('click', function () { modal.classList.add('hidden'); });
  modal.addEventListener('click', function (e) { if (e.target === modal) modal.classList.add('hidden'); });
  modal.querySelectorAll('.kw-status-btn').forEach(function (btn) {
    btn.addEventListener('click', async function () {
      var uk = userKeywords[curKw]; if (!uk) return;
      var res = await API.updateKeywordStatus(uk.id, btn.dataset.status);
      if (res.success) {
        userKeywords[curKw].status = btn.dataset.status;
        var expandedIds = [];
        document.querySelectorAll('.oferta-card.expanded').forEach(function (card) {
          var job = currentJobs[Number(card.dataset.index)];
          if (job) expandedIds.push(job.id);
        });
        modal.classList.add('hidden');
        renderJobList();
        expandedIds.forEach(function (id) {
          var idx = currentJobs.findIndex(function (j) { return j.id === id; });
          if (idx >= 0) {
            var card = document.getElementById('oferta-' + idx);
            if (card) card.classList.add('expanded');
          }
        });
      }
    });
  });
  document.addEventListener('click', function (e) {
    var tag = e.target.closest('.kw-tag'); if (!tag) return; e.stopPropagation();
    curKw = tag.dataset.keyword; title.textContent = curKw;
    var uk = userKeywords[curKw];
    modal.querySelectorAll('.kw-status-btn').forEach(function (b) { b.style.borderColor = (uk && uk.status === b.dataset.status) ? 'var(--amber)' : 'var(--border)'; });
    modal.classList.remove('hidden');
  });
}

function renderEmpty() {
  document.getElementById('ofertas-list').innerHTML = '<div class="empty-state"><p>Bienvenido a 42jobs</p><p class="hint">Usa el boton + para buscar ofertas</p></div>';
  document.getElementById('offer-count').textContent = '0 ofertas';
  document.getElementById('kw-count').textContent = '0 keywords';
  document.getElementById('summary').textContent = 'Ninguna categoria \u00b7 Usa + para empezar';
}

function showLoading(show) { document.getElementById('loading-jobs').classList.toggle('hidden', !show); if (show) document.getElementById('ofertas-list').innerHTML = ''; }

function openCvPanel(jobId, jobTitle) {
  var panel = document.getElementById('cv-panel');
  var content = document.getElementById('cv-content');
  panel.classList.remove('hidden');
  content.innerHTML = '<div class="loading" style="padding:3rem">Generando CV con GPT-5.4...</div>';

  API.generateCV(jobId).then(function (res) {
    if (res.success) {
      renderCV(res.data.cv, jobTitle, res.data.profile);
    } else {
      content.innerHTML = '<div class="loading">Error: ' + (res.error || 'desconocido') + '</div>';
    }
  }).catch(function (err) {
    content.innerHTML = '<div class="loading">Error: ' + err.message + '</div>';
  });
}

function renderCV(data, jobTitle, profile) {
  var content = document.getElementById('cv-content');

  window._cvProfile = profile;
  window._cvData = data;
  window._cvJobTitle = jobTitle;

  var p = profile || {};

  var expHtml = (data.selected_experiences || []).map(function (e) {
    return '<div class="cv-item"><div class="cv-item-header"><strong>' + escHtml(e.role) + '</strong> — ' + escHtml(e.company) + ' <span class="cv-item-dates">' + escHtml(e.dates || '') + '</span></div>' +
      '<p>' + escHtml(e.adapted_description) + '</p></div>';
  }).join('');

  var projHtml = (data.selected_projects || []).map(function (p) {
    return '<div class="cv-item"><div class="cv-item-header"><strong>' + escHtml(p.name) + '</strong> <span class="cv-type-tag">' + (p.type === 'school' ? '42 School' : 'Personal') + '</span></div>' +
      '<p>' + escHtml(p.adapted_description) + '</p></div>';
  }).join('');

  var skillsHtml = (data.skill_categories || []).map(function (cat) {
    var tags = (cat.skills || []).map(function (s) { return '<span class="cv-skill-tag">' + escHtml(s) + '</span>'; }).join('');
    return '<div class="cv-skill-group"><div class="cv-skill-cat">' + escHtml(cat.category) + '</div><div class="cv-skills">' + tags + '</div></div>';
  }).join('');

  var langHtml = (data.languages || []).map(function (l) {
    return '<span class="cv-skill-tag">' + escHtml(l.name) + ' (' + escHtml(l.level) + ')</span>';
  }).join('');

  content.innerHTML =
    '<div class="cv-page" id="cv-page">' +
      '<div class="cv-header">' +
        '<h1>' + escHtml(p.name || '') + ' ' + escHtml(p.last_name || '') + '</h1>' +
        '<p class="cv-subtitle">' + escHtml(jobTitle) + '</p>' +
        '<div class="cv-contact">' +
          (p.email ? escHtml(p.email) + ' &nbsp;|&nbsp; ' : '') +
          (p.phone ? escHtml(p.phone) + ' &nbsp;|&nbsp; ' : '') +
          (p.address ? escHtml(p.address) + ' &nbsp;|&nbsp; ' : '') +
          (p.linkedin_url ? escHtml(p.linkedin_url) + ' &nbsp;|&nbsp; ' : '') +
          (p.github_url ? escHtml(p.github_url) : '') +
        '</div>' +
      '</div>' +
      '<div class="cv-section">' +
        '<h2>Perfil</h2>' +
        '<p>' + escHtml(data.introduction) + '</p>' +
      '</div>' +
      (data.selected_experiences && data.selected_experiences.length ? '<div class="cv-section"><h2>Experiencia</h2>' + expHtml + '</div>' : '') +
      (data.selected_projects && data.selected_projects.length ? '<div class="cv-section"><h2>Proyectos</h2>' + projHtml + '</div>' : '') +
      '<div class="cv-section">' +
        '<h2>Skills</h2>' +
        skillsHtml +
      '</div>' +
      '<div class="cv-section">' +
        '<h2>Idiomas</h2>' +
        '<div class="cv-skills">' + langHtml + '</div>' +
      '</div>' +
    '</div>';
}

document.addEventListener('DOMContentLoaded', function () {
  setupNotesModal(); setupKeywordModal(); setupAddButton(); setupUpdateButton();
  document.getElementById('cv-close').addEventListener('click', function () {
    document.getElementById('cv-panel').classList.add('hidden');
  });
  document.getElementById('cv-download').addEventListener('click', async function () {
    var p = (window._cvProfile || {});
    var data = (window._cvData || {});
    var jobTitle = (window._cvJobTitle || '');

    var photoB64 = '';
    try {
      var imgRes = await fetch('/resources/YoFinal.webp');
      var blob = await imgRes.blob();
      photoB64 = await new Promise(function (resolve) {
        var reader = new FileReader();
        reader.onloadend = function () { resolve(reader.result); };
        reader.readAsDataURL(blob);
      });
    } catch (e) {}

    var skillsHtml = (data.skill_categories || []).map(function (c) {
      return '<div class="cats"><b>' + escHtml(c.category) + '</b>: ' + (c.skills || []).map(escHtml).join(', ') + '</div>';
    }).join('');

    var expHtml = (data.selected_experiences || []).map(function (e) {
      return '<div class="item"><b>' + escHtml(e.role) + '</b> — ' + escHtml(e.company) + ' <span class="date">' + escHtml(e.dates) + '</span><p>' + escHtml(e.adapted_description) + '</p></div>';
    }).join('');

    var projHtml = (data.selected_projects || []).map(function (p) {
      return '<div class="item"><b>' + escHtml(p.name) + '</b> <span class="type">(' + (p.type === 'school' ? '42 School' : 'Personal') + ')</span><p>' + escHtml(p.adapted_description) + '</p></div>';
    }).join('');

    var langHtml = (data.languages || []).map(function (l) {
      return escHtml(l.name) + ' (' + escHtml(l.level) + ')';
    }).join(', ');

    var fullName = [p.name, p.last_name].filter(Boolean).join(' ');
    var contact = [p.email, p.phone, p.address, p.linkedin_url, p.github_url].filter(Boolean).join(' | ');

    var html = '<!DOCTYPE html>\n<html><head><meta charset="UTF-8"><title>CV ' + escHtml(fullName) + '</title>\n' +
      '<style>\n' +
      '*{margin:0;padding:0;box-sizing:border-box}\n' +
      'body{font-family:Helvetica,Arial,sans-serif;max-width:750px;margin:2rem auto;color:#222;line-height:1.5;font-size:11pt}\n' +
      '.header{display:flex;gap:1.5rem;margin-bottom:1.5rem;padding-bottom:1rem;border-bottom:2px solid #333;align-items:center}\n' +
      '.photo{width:100px;height:100px;border-radius:50%;object-fit:cover;flex-shrink:0}\n' +
      '.info h1{font-size:1.6rem;margin-bottom:.1rem}\n' +
      '.info .job{font-size:1.05rem;color:#555;margin-bottom:.3rem}\n' +
      '.info .contact{font-size:.75rem;color:#666}\n' +
      'h2{font-size:.85rem;text-transform:uppercase;letter-spacing:.1em;border-bottom:1px solid #ccc;padding-bottom:.2rem;margin:1rem 0 .5rem}\n' +
      '.item{margin-bottom:.5rem}\n' +
      '.item b{font-size:1rem}\n' +
      '.date{color:#888;font-size:.8rem}\n' +
      '.type{color:#2563eb;font-size:.8rem}\n' +
      '.item p{font-size:.85rem;color:#444;margin:.1rem 0 0}\n' +
      '.cats{margin-bottom:.2rem;font-size:.9rem}\n' +
      '.cats b{color:#333}\n' +
      '</style>\n</head>\n<body>\n' +
      '<div class="header">\n' +
      (photoB64 ? '<img class="photo" src="' + photoB64 + '" alt="photo">\n' : '') +
      '<div class="info">\n' +
      '<h1>' + escHtml(fullName) + '</h1>\n' +
      '<div class="job">' + escHtml(jobTitle) + '</div>\n' +
      '<div class="contact">' + escHtml(contact) + '</div>\n' +
      '</div></div>\n' +
      '<h2>Perfil</h2>\n<p>' + escHtml(data.introduction || '') + '</p>\n' +
      (expHtml ? '<h2>Experiencia</h2>\n' + expHtml + '\n' : '') +
      (projHtml ? '<h2>Proyectos</h2>\n' + projHtml + '\n' : '') +
      '<h2>Skills</h2>\n' + skillsHtml + '\n' +
      '<h2>Idiomas</h2>\n<p>' + langHtml + '</p>\n' +
      '</body>\n</html>';

    var blob = new Blob([html], { type: 'text/html' });
    var a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = 'cv_' + (fullName.toLowerCase().replace(/\s+/g, '_') || 'export') + '.html';
    a.click();
    URL.revokeObjectURL(a.href);
  });
  loadUserKeywords().then(function () { loadCategories(); });
});
