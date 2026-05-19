var profileData = null;
var editingType = null;
var editingId = null;

function escHtml(str) {
  if (!str) return '';
  return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

function status(el, msg) {
  el.textContent = msg;
  setTimeout(function () { el.textContent = ''; }, 2000);
}

async function loadProfile() {
  var res = await API.getProfile();
  if (res.success) {
    profileData = res.data;
    fillInfo();
    renderLanguages();
    renderCertifications();
    renderEducation();
    renderProjects();
    renderExperiences();
  }
}

function fillInfo() {
  if (!profileData) return;
  document.getElementById('pf-name').value = profileData.name || '';
  document.getElementById('pf-last-name').value = profileData.last_name || '';
  document.getElementById('pf-phone').value = profileData.phone || '';
  document.getElementById('pf-email').value = profileData.email || '';
  document.getElementById('pf-address').value = profileData.address || '';
  document.getElementById('pf-linkedin').value = profileData.linkedin_url || '';
  document.getElementById('pf-website').value = profileData.website_url || '';
  document.getElementById('pf-github').value = profileData.github_url || '';
  document.getElementById('pf-junior').value = profileData.junior ? 'true' : 'false';
  document.getElementById('pf-presentation').value = profileData.presentation || '';
}

function collectInfo() {
  return {
    name: document.getElementById('pf-name').value,
    last_name: document.getElementById('pf-last-name').value,
    phone: document.getElementById('pf-phone').value,
    email: document.getElementById('pf-email').value,
    address: document.getElementById('pf-address').value,
    linkedin_url: document.getElementById('pf-linkedin').value,
    website_url: document.getElementById('pf-website').value,
    github_url: document.getElementById('pf-github').value,
    junior: document.getElementById('pf-junior').value === 'true',
    presentation: document.getElementById('pf-presentation').value
  };
}

function renderLanguages() {
  var list = document.getElementById('pf-languages-list');
  if (!profileData || !profileData.languages) return;
  list.innerHTML = profileData.languages.map(function (l) {
    return '<div class="pf-item"><span>' + escHtml(l.name) + ' - ' + escHtml(l.level) + '</span>' +
      '<div><button class="pf-edit-btn" data-edit="lang" data-id="' + l.id + '">editar</button>' +
      '<button class="pf-del-btn" data-del="lang" data-id="' + l.id + '">x</button></div></div>';
  }).join('');
  bindItemButtons(list);
}

function renderCertifications() {
  var list = document.getElementById('pf-certs-list');
  if (!profileData || !profileData.certifications) return;
  list.innerHTML = profileData.certifications.map(function (c) {
    return '<div class="pf-item"><span>' + escHtml(c.name) + (c.entity ? ' - ' + escHtml(c.entity) : '') + (c.date_obtained ? ' (' + c.date_obtained.split('T')[0] + ')' : '') + '</span>' +
      '<div><button class="pf-edit-btn" data-edit="cert" data-id="' + c.id + '">editar</button>' +
      '<button class="pf-del-btn" data-del="cert" data-id="' + c.id + '">x</button></div></div>';
  }).join('');
  bindItemButtons(list);
}

function renderEducation() {
  var list = document.getElementById('pf-edu-list');
  if (!profileData || !profileData.education) return;
  list.innerHTML = profileData.education.map(function (e) {
    return '<div class="pf-item"><span>' + escHtml(e.degree) + ' - ' + escHtml(e.institution || '') + ' (' + (e.start_year || '?') + '-' + (e.end_year || '?') + ')</span>' +
      '<div><button class="pf-edit-btn" data-edit="edu" data-id="' + e.id + '">editar</button>' +
      '<button class="pf-del-btn" data-del="edu" data-id="' + e.id + '">x</button></div></div>';
  }).join('');
  bindItemButtons(list);
}

function renderProjects() {
  var list = document.getElementById('pf-projects-list');
  if (!profileData || !profileData.projects) return;
  list.innerHTML = profileData.projects.map(function (p) {
    return '<div class="pf-item pf-item-block"><strong>' + escHtml(p.name) + ' <span class="pf-type">(' + p.type + ')</span></strong>' +
      (p.description ? '<p class="pf-desc">' + escHtml(p.description) + '</p>' : '') +
      '<div class="pf-kw-list">' + (p.keywords || []).map(function (k) { return '<span class="pf-kw">' + escHtml(k) + '</span>'; }).join('') + '</div>' +
      '<div><button class="pf-edit-btn" data-edit="proj" data-id="' + p.id + '">editar</button>' +
      '<button class="pf-del-btn" data-del="proj" data-id="' + p.id + '">x</button></div></div>';
  }).join('');
  bindItemButtons(list);
}

function renderExperiences() {
  var list = document.getElementById('pf-exp-list');
  if (!profileData || !profileData.experiences) return;
  list.innerHTML = profileData.experiences.map(function (e) {
    var dates = (e.start_date ? e.start_date.split('T')[0] : '?') + ' - ' + (e.end_date ? e.end_date.split('T')[0] : '?');
    return '<div class="pf-item pf-item-block"><strong>' + escHtml(e.position || '') + ' en ' + escHtml(e.company) + '</strong> <span class="pf-type">(' + dates + ')</span>' +
      (e.description ? '<p class="pf-desc">' + escHtml(e.description) + '</p>' : '') +
      '<div class="pf-kw-list">' + (e.keywords || []).map(function (k) { return '<span class="pf-kw">' + escHtml(k) + '</span>'; }).join('') + '</div>' +
      '<div><button class="pf-edit-btn" data-edit="exp" data-id="' + e.id + '">editar</button>' +
      '<button class="pf-del-btn" data-del="exp" data-id="' + e.id + '">x</button></div></div>';
  }).join('');
  bindItemButtons(list);
}

function bindItemButtons(list) {
  list.querySelectorAll('.pf-del-btn').forEach(function (b) {
    b.addEventListener('click', function () {
      handleDelete(b.dataset.del, Number(b.dataset.id));
    });
  });
  list.querySelectorAll('.pf-edit-btn').forEach(function (b) {
    b.addEventListener('click', function () {
      handleEdit(b.dataset.edit, Number(b.dataset.id));
    });
  });
}

function handleEdit(type, id) {
  editingType = type;
  editingId = id;
  var item;
  if (type === 'lang') {
    item = profileData.languages.find(function (l) { return l.id === id; });
    if (item) { document.getElementById('pf-lang-name').value = item.name; document.getElementById('pf-lang-level').value = item.level; }
  } else if (type === 'cert') {
    item = profileData.certifications.find(function (c) { return c.id === id; });
    if (item) { document.getElementById('pf-cert-name').value = item.name; document.getElementById('pf-cert-entity').value = item.entity || ''; document.getElementById('pf-cert-date').value = item.date_obtained ? item.date_obtained.split('T')[0] : ''; }
  } else if (type === 'edu') {
    item = profileData.education.find(function (e) { return e.id === id; });
    if (item) { document.getElementById('pf-edu-degree').value = item.degree; document.getElementById('pf-edu-inst').value = item.institution || ''; document.getElementById('pf-edu-start').value = item.start_year || ''; document.getElementById('pf-edu-end').value = item.end_year || ''; }
  } else if (type === 'proj') {
    item = profileData.projects.find(function (p) { return p.id === id; });
    if (item) {
      document.getElementById('pf-proj-name').value = item.name;
      document.getElementById('pf-proj-desc').value = item.description || '';
      document.getElementById('pf-proj-type').value = item.type;
    }
  } else if (type === 'exp') {
    item = profileData.experiences.find(function (e) { return e.id === id; });
    if (item) {
      document.getElementById('pf-exp-company').value = item.company;
      document.getElementById('pf-exp-position').value = item.position || '';
      document.getElementById('pf-exp-start').value = item.start_date ? item.start_date.split('T')[0] : '';
      document.getElementById('pf-exp-end').value = item.end_date ? item.end_date.split('T')[0] : '';
      document.getElementById('pf-exp-desc').value = item.description || '';
    }
  }
}

function resetEditState() {
  editingType = null;
  editingId = null;
}

async function handleDelete(type, id) {
  if (!confirm('Eliminar?')) return;
  var res;
  if (type === 'lang') res = await API.deleteLanguage(id);
  else if (type === 'cert') res = await API.deleteCertification(id);
  else if (type === 'edu') res = await API.deleteEducation(id);
  else if (type === 'proj') res = await API.deleteProject(id);
  else if (type === 'exp') res = await API.deleteExperience(id);
  if (res && res.success) loadProfile();
}

function setupProfilePanel() {
  document.getElementById('profile-btn').addEventListener('click', function () {
    document.getElementById('profile-panel').classList.remove('hidden');
    document.getElementById('tabs-bar').classList.add('hidden');
    document.querySelector('main').classList.add('hidden');
    document.querySelector('.trace-line').classList.add('hidden');
    loadProfile();
  });

  document.getElementById('profile-close').addEventListener('click', function () {
    document.getElementById('profile-panel').classList.add('hidden');
    document.getElementById('tabs-bar').classList.remove('hidden');
    document.querySelector('main').classList.remove('hidden');
    document.querySelector('.trace-line').classList.remove('hidden');
  });

  document.getElementById('pf-save-info').addEventListener('click', async function () {
    var st = document.getElementById('pf-info-status');
    var res = await API.updateProfile(collectInfo());
    if (res.success) status(st, 'Guardado');
    else status(st, 'Error: ' + (res.error || 'desconocido'));
  });

  document.getElementById('pf-add-lang').addEventListener('click', async function () {
    var name = document.getElementById('pf-lang-name').value.trim();
    var level = document.getElementById('pf-lang-level').value.trim();
    if (!name || !level) return;
    var res;
    if (editingType === 'lang' && editingId) {
      res = await API.updateLanguage(editingId, { name, level });
    } else {
      res = await API.addLanguage({ name, level });
    }
    if (res.success) { document.getElementById('pf-lang-name').value = ''; document.getElementById('pf-lang-level').value = ''; resetEditState(); loadProfile(); }
  });

  document.getElementById('pf-add-cert').addEventListener('click', async function () {
    var name = document.getElementById('pf-cert-name').value.trim();
    var entity = document.getElementById('pf-cert-entity').value.trim();
    var date = document.getElementById('pf-cert-date').value;
    if (!name) return;
    var data = { name, entity: entity || null, date_obtained: date || null };
    var res;
    if (editingType === 'cert' && editingId) {
      res = await API.updateCertification(editingId, data);
    } else {
      res = await API.addCertification(data);
    }
    if (res.success) { document.getElementById('pf-cert-name').value = ''; document.getElementById('pf-cert-entity').value = ''; document.getElementById('pf-cert-date').value = ''; resetEditState(); loadProfile(); }
  });

  document.getElementById('pf-add-edu').addEventListener('click', async function () {
    var degree = document.getElementById('pf-edu-degree').value.trim();
    var inst = document.getElementById('pf-edu-inst').value.trim();
    var start = document.getElementById('pf-edu-start').value;
    var end = document.getElementById('pf-edu-end').value;
    if (!degree) return;
    var data = { degree, institution: inst || null, start_year: start ? Number(start) : null, end_year: end ? Number(end) : null };
    var res;
    if (editingType === 'edu' && editingId) {
      res = await API.updateEducation(editingId, data);
    } else {
      res = await API.addEducation(data);
    }
    if (res.success) { document.getElementById('pf-edu-degree').value = ''; document.getElementById('pf-edu-inst').value = ''; document.getElementById('pf-edu-start').value = ''; document.getElementById('pf-edu-end').value = ''; resetEditState(); loadProfile(); }
  });

  document.getElementById('pf-add-proj').addEventListener('click', async function () {
    var name = document.getElementById('pf-proj-name').value.trim();
    var desc = document.getElementById('pf-proj-desc').value.trim();
    var type = document.getElementById('pf-proj-type').value;
    if (!name) return;
    var kwIds = getSelectedKwIds('pf-proj-kw');
    var data = { name, description: desc || null, type, keyword_ids: kwIds };
    var res;
    if (editingType === 'proj' && editingId) {
      res = await API.updateProject(editingId, data);
    } else {
      res = await API.addProject(data);
    }
    if (res.success) { document.getElementById('pf-proj-name').value = ''; document.getElementById('pf-proj-desc').value = ''; resetEditState(); loadProfile(); }
  });

  document.getElementById('pf-add-exp').addEventListener('click', async function () {
    var company = document.getElementById('pf-exp-company').value.trim();
    var position = document.getElementById('pf-exp-position').value.trim();
    var start = document.getElementById('pf-exp-start').value;
    var end = document.getElementById('pf-exp-end').value;
    var desc = document.getElementById('pf-exp-desc').value.trim();
    if (!company) return;
    var kwIds = getSelectedKwIds('pf-exp-kw');
    var data = { company, position: position || null, start_date: start || null, end_date: end || null, description: desc || null, keyword_ids: kwIds };
    var res;
    if (editingType === 'exp' && editingId) {
      res = await API.updateExperience(editingId, data);
    } else {
      res = await API.addExperience(data);
    }
    if (res.success) { document.getElementById('pf-exp-company').value = ''; document.getElementById('pf-exp-position').value = ''; document.getElementById('pf-exp-start').value = ''; document.getElementById('pf-exp-end').value = ''; document.getElementById('pf-exp-desc').value = ''; resetEditState(); loadProfile(); }
  });

  setupProfileTabs();
}

function getSelectedKwIds(containerId) {
  var ids = [];
  document.querySelectorAll('#' + containerId + ' .pf-kw-sel.selected').forEach(function (s) { ids.push(Number(s.dataset.id)); });
  return ids;
}

function setupProfileTabs() {
  document.querySelectorAll('.ptab-btn').forEach(function (btn) {
    btn.addEventListener('click', function () {
      document.querySelectorAll('.ptab-btn').forEach(function (b) { b.classList.remove('active'); });
      btn.classList.add('active');
      document.querySelectorAll('.ptab-content').forEach(function (c) { c.classList.add('hidden'); });
      document.getElementById('ptab-' + btn.dataset.ptab).classList.remove('hidden');
      if (btn.dataset.ptab === 'projects' || btn.dataset.ptab === 'experiences') loadKeywordSelector(btn.dataset.ptab);
    });
  });
}

async function loadKeywordSelector(type) {
  var container = document.getElementById(type === 'projects' ? 'pf-proj-kw' : 'pf-exp-kw');
  if (container.children.length > 0) return;
  var res = await API.getUserKeywords();
  if (!res.success) return;
  container.innerHTML = res.data.map(function (k) {
    return '<span class="pf-kw-sel" data-id="' + k.id + '">' + escHtml(k.name) + '</span>';
  }).join('');
  container.querySelectorAll('.pf-kw-sel').forEach(function (s) {
    s.addEventListener('click', function () { s.classList.toggle('selected'); });
  });
}

document.addEventListener('DOMContentLoaded', function () { setupProfilePanel(); });
