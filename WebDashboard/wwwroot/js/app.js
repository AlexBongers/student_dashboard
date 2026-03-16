/* ── State ── */
let allStudents = [];
let archivedStudents = [];
let currentStudent = null;
let currentView = 'dashboard';
let activeFilter = null;
let editingStudentId = null;

const WORKFLOW_KEYS = ['Opstart', 'PvA', 'Concept 1', 'Concept 2', 'Definitief', 'Herkansing', 'Afgerond'];
const WORKFLOW_LABELS = { 'Opstart': 'Opstart', 'PvA': 'PvA / Stageplan', 'Concept 1': '1e Concept Verslag', 'Concept 2': '2e Concept Verslag', 'Definitief': 'Definitief Verslag', 'Herkansing': 'Herkansing', 'Afgerond': 'Afgerond' };
const STATUS_COLORS = { 'Opstart': '#3b82f6', 'PvA': '#f59e0b', 'Concept 1': '#4f46e5', 'Concept 2': '#7c3aed', 'Definitief': '#10b981', 'Herkansing': '#ef4444', 'Afgerond': '#6b7280' };

/* ── Init ── */
document.addEventListener('DOMContentLoaded', () => {
  // Set today's date as default for contact form
  document.getElementById('new-contact-date').value = today();
  // Set default deadline date (+7 days)
  const d = new Date(); d.setDate(d.getDate() + 7);
  document.getElementById('new-deadline-date').value = d.toISOString().split('T')[0];
  // Default form dates
  document.getElementById('f-startdate').value = today();
  const e = new Date(); e.setMonth(e.getMonth() + 5);
  document.getElementById('f-enddate').value = e.toISOString().split('T')[0];

  // Add toast container
  const tc = document.createElement('div');
  tc.id = 'toast-container';
  document.body.appendChild(tc);

  loadAll();
});

function today() {
  return new Date().toISOString().split('T')[0];
}

/* ── API helpers ── */
async function api(path, method = 'GET', body) {
  const opts = { method, headers: { 'Content-Type': 'application/json' } };
  if (body) opts.body = JSON.stringify(body);
  const r = await fetch(`/api/${path}`, opts);
  if (!r.ok) {
    const text = await r.text();
    throw new Error(text || r.statusText);
  }
  if (r.status === 204) return null;
  return r.json();
}

/* ── Data Loading ── */
async function loadAll() {
  try {
    const [active, archived, stats] = await Promise.all([
      api('students'),
      api('students?archived=true'),
      api('students/stats')
    ]);
    allStudents = active;
    archivedStudents = archived;
    renderStudentList();
    renderArchivedList();
    renderStats(stats);
  } catch (err) {
    toast('Fout bij laden van data: ' + err.message, 'error');
  }
}

/* ── Navigation ── */
function showView(view) {
  currentView = view;
  document.querySelectorAll('.view').forEach(v => v.classList.remove('active'));
  document.querySelectorAll('.nav-btn').forEach(b => b.classList.remove('active'));
  document.getElementById(`view-${view}`).classList.add('active');
  document.querySelector(`[data-view="${view}"]`).classList.add('active');
  const titles = { dashboard: 'Dashboard', students: 'Studenten', archived: 'Archief' };
  document.getElementById('page-title').textContent = titles[view] || view;
  if (view === 'students') filterStudents();
}

/* ── Dashboard Stats ── */
function renderStats(stats) {
  document.getElementById('stat-needs-action').textContent = stats.needsActionCount;
  document.getElementById('stat-in-review').textContent = stats.inReviewCount;
  document.getElementById('stat-active').textContent = stats.activeCount;
  document.getElementById('stat-completed').textContent = stats.completedMonthCount;

  // Alerts
  const alertsList = document.getElementById('alerts-list');
  const badge = document.getElementById('alerts-badge');
  if (stats.alerts && stats.alerts.length > 0) {
    badge.textContent = stats.alerts.length;
    badge.style.display = '';
    alertsList.innerHTML = stats.alerts.map(a => `
      <div class="alert-item ${a.color}" onclick="openStudentById(${a.studentId})">
        <div class="alert-title">${esc(a.message)}</div>
        <div class="alert-desc">${esc(a.description)}</div>
      </div>
    `).join('');
  } else {
    badge.style.display = 'none';
    alertsList.innerHTML = '<p class="empty-state">Geen acties vereist. Je bent helemaal bij! 🎉</p>';
  }

  // Status chart
  const chart = document.getElementById('status-chart');
  if (stats.statusDistribution && stats.statusDistribution.length > 0) {
    chart.innerHTML = stats.statusDistribution.map(item => {
      const color = STATUS_COLORS[item.label] || '#6b7280';
      const pct = Math.round(item.percentage * 100);
      return `<div class="chart-row">
        <span class="chart-label">${esc(item.label)}</span>
        <div class="chart-bar-wrap"><div class="chart-bar" style="width:${pct}%;background:${color}"></div></div>
        <span class="chart-count">${item.count}</span>
      </div>`;
    }).join('');
  } else {
    chart.innerHTML = '<p class="empty-state">Nog geen data beschikbaar.</p>';
  }
}

function applyFilter(filter) {
  activeFilter = filter;
  showView('students');
  // Let filterStudents apply the stat filter
  if (filter === 'needsAction') {
    document.getElementById('search-input').value = '';
    // Filter by students needing action (no recent contact)
    renderFilteredStudents(allStudents.filter(s => {
      const lastContact = s.contacts.slice().sort((a, b) => new Date(b.date) - new Date(a.date))[0];
      if (!lastContact) return true;
      return (Date.now() - new Date(lastContact.date)) / 86400000 > 14;
    }));
  } else if (filter === 'inReview') {
    renderFilteredStudents(allStudents.filter(s => ['Concept 1', 'Concept 2', 'Eindversie', 'Definitief'].includes(s.status)));
  } else if (filter === 'active') {
    renderFilteredStudents(allStudents);
  }
}

/* ── Student List ── */
function filterStudents() {
  const q = document.getElementById('search-input').value.toLowerCase();
  const type = document.getElementById('filter-type').value;
  const status = document.getElementById('filter-status').value;

  let filtered = allStudents.filter(s => {
    const matchQ = !q || s.name.toLowerCase().includes(q) || (s.company || '').toLowerCase().includes(q) || (s.status || '').toLowerCase().includes(q) || (s.studentNumber || '').toLowerCase().includes(q);
    const matchType = !type || s.type === type;
    const matchStatus = !status || s.status === status;
    return matchQ && matchType && matchStatus;
  });
  renderFilteredStudents(filtered);
}

function renderStudentList() {
  filterStudents();
}

function renderArchivedList() {
  const container = document.getElementById('archived-list');
  if (archivedStudents.length === 0) {
    container.innerHTML = '<p class="empty-state">Geen gearchiveerde studenten.</p>';
    return;
  }
  container.innerHTML = archivedStudents.map(s => studentCard(s)).join('');
}

function renderFilteredStudents(list) {
  const container = document.getElementById('students-list');
  if (list.length === 0) {
    container.innerHTML = '<p class="empty-state">Geen studenten gevonden.</p>';
    return;
  }
  container.innerHTML = list.map(s => studentCard(s)).join('');
}

function studentCard(s) {
  const badgeClass = statusBadge(s.status);
  const initials = ((s.firstName || '')[0] || '') + ((s.lastName || '')[0] || '');
  const start = s.startDate ? new Date(s.startDate).toLocaleDateString('nl-NL') : '-';
  const end = s.endDate ? new Date(s.endDate).toLocaleDateString('nl-NL') : '-';
  return `<div class="student-card ${s.hasUrgentDeadline ? 'urgent' : ''}" onclick="openStudent(${s.id})">
    <div class="student-avatar">${esc(initials.toUpperCase())}</div>
    <div class="student-main">
      <div class="student-name">${esc(s.name)}</div>
      <div class="student-meta">${esc(s.company || '-')} · ${esc(s.type)} · ${esc(s.myRole)}</div>
    </div>
    <div class="student-right">
      <span class="badge ${badgeClass}">${esc(s.status)}</span>
      <span class="student-dates">${start} – ${end}</span>
    </div>
  </div>`;
}

function statusBadge(status) {
  const map = { 'Opstart': 'info', 'PvA': 'warning', 'Concept 1': 'primary', 'Concept 2': 'primary', 'Definitief': 'success', 'Afgerond': 'gray', 'Herkansing': 'danger' };
  return map[status] || 'gray';
}

/* ── Detail Drawer ── */
function openStudentById(id) {
  const s = allStudents.find(x => x.id === id) || archivedStudents.find(x => x.id === id);
  if (s) openStudent(s.id);
}

async function openStudent(id) {
  try {
    const s = await api(`students/${id}`);
    currentStudent = s;
    renderDetail(s);
    document.getElementById('detail-overlay').classList.remove('hidden');
    document.getElementById('student-detail').classList.remove('hidden');
    switchTab('info');
  } catch (err) {
    toast('Fout bij laden van student: ' + err.message, 'error');
  }
}

function closeDetail() {
  document.getElementById('detail-overlay').classList.add('hidden');
  document.getElementById('student-detail').classList.add('hidden');
  currentStudent = null;
}

function renderDetail(s) {
  document.getElementById('detail-name').textContent = s.name;
  const typeEl = document.getElementById('detail-type-badge');
  typeEl.textContent = s.type;
  typeEl.className = 'badge ' + (s.type === 'stage' ? 'info' : 'primary');

  const archBtn = document.getElementById('archive-btn');
  archBtn.textContent = s.archived ? 'Herstellen' : 'Archiveren';

  // Info tab
  const infoGrid = document.getElementById('detail-info-grid');
  const fields = [
    ['Voornaam', s.firstName], ['Achternaam', s.lastName],
    ['Studentnummer', s.studentNumber], ['Email', s.email],
    ['Telefoon', s.phone], ['Type', s.type],
    ['Mijn Rol', s.myRole], ['Status', s.status],
    ['Bedrijf', s.company], ['Studie', s.studyProgram],
    ['Cohort', s.cohort], ['Locatie', s.location],
    ['Adres student', s.address, true],
    ['Bedrijfsadres', s.companyAddress, true],
    ['Bedrijfsbegeleider', s.companySupervisorName], ['Begeleider Email', s.companySupervisorEmail],
    ['Begeleider Tel.', s.companySupervisorPhone],
    ['Startdatum', s.startDate ? new Date(s.startDate).toLocaleDateString('nl-NL') : '-'],
    ['Einddatum', s.endDate ? new Date(s.endDate).toLocaleDateString('nl-NL') : '-'],
  ];
  infoGrid.innerHTML = fields.map(([label, val, full]) =>
    `<div class="info-item ${full ? 'full' : ''}"><label>${esc(label)}</label><span>${esc(val || '-')}</span></div>`
  ).join('');

  // Workflow tab
  renderWorkflow(s);

  // Contacts tab
  renderContacts(s.contacts || []);

  // Deadlines tab
  renderDeadlines(s.deadlines || []);

  // Notes tab
  document.getElementById('notes-textarea').value = s.notes || '';
}

function renderWorkflow(s) {
  const sel = document.getElementById('workflow-status-select');
  sel.innerHTML = WORKFLOW_KEYS.map(k => `<option value="${k}" ${s.status === k ? 'selected' : ''}>${k}</option>`).join('');

  const container = document.getElementById('workflow-steps');
  const steps = s.workflowSteps || [];
  let foundCurrent = false;
  container.innerHTML = WORKFLOW_KEYS.map(key => {
    const step = steps.find(w => w.stepKey === key);
    const completed = step?.completed ?? false;
    let isCurrent = false;
    if (!completed && !foundCurrent) { isCurrent = true; foundCurrent = true; }
    const dateStr = step?.completedDate ? new Date(step.completedDate).toLocaleDateString('nl-NL') : '';
    return `<div class="workflow-step">
      <div class="step-circle ${completed ? 'completed' : isCurrent ? 'current' : ''}">${completed ? '✓' : isCurrent ? '▶' : ''}</div>
      <div class="step-body">
        <div class="step-label ${isCurrent ? 'current' : ''}">${esc(WORKFLOW_LABELS[key] || key)}</div>
        ${dateStr ? `<div class="step-date">Voltooid: ${dateStr}</div>` : ''}
      </div>
    </div>`;
  }).join('');
}

async function updateWorkflowStatus(newStatus) {
  if (!currentStudent) return;
  try {
    const updated = await api(`students/${currentStudent.id}/workflow`, 'POST', { status: newStatus });
    currentStudent = updated;
    renderWorkflow(updated);
    // Update student in lists
    const idx = allStudents.findIndex(s => s.id === updated.id);
    if (idx >= 0) allStudents[idx] = updated;
    renderStudentList();
    toast('Status bijgewerkt', 'success');
    // Refresh stats
    const stats = await api('students/stats');
    renderStats(stats);
  } catch (err) {
    toast('Fout: ' + err.message, 'error');
  }
}

function renderContacts(contacts) {
  const container = document.getElementById('contacts-list');
  if (!contacts.length) { container.innerHTML = '<p class="empty-state">Geen contactmomenten geregistreerd.</p>'; return; }
  container.innerHTML = contacts.map(c => `
    <div class="contact-item">
      <div class="contact-header">
        <span class="contact-type">${esc(c.type)}</span>
        <div style="display:flex;align-items:center;gap:8px">
          <span class="contact-date">${new Date(c.date).toLocaleDateString('nl-NL')}</span>
          <button class="contact-delete" onclick="deleteContact(${c.id})" title="Verwijder">✕</button>
        </div>
      </div>
      <div class="contact-content">${esc(c.content)}</div>
    </div>
  `).join('');
}

async function addContact() {
  if (!currentStudent) return;
  const type = document.getElementById('new-contact-type').value;
  const content = document.getElementById('new-contact-content').value.trim();
  const date = document.getElementById('new-contact-date').value;
  if (!content) { toast('Voer een beschrijving in', 'error'); return; }
  try {
    const contact = await api(`students/${currentStudent.id}/contacts`, 'POST', { type, content, date: date || today() });
    currentStudent.contacts = [contact, ...(currentStudent.contacts || [])];
    renderContacts(currentStudent.contacts);
    document.getElementById('new-contact-content').value = '';
    document.getElementById('new-contact-date').value = today();
    toast('Contactmoment toegevoegd', 'success');
  } catch (err) {
    toast('Fout: ' + err.message, 'error');
  }
}

async function deleteContact(id) {
  if (!currentStudent) return;
  try {
    await api(`students/${currentStudent.id}/contacts/${id}`, 'DELETE');
    currentStudent.contacts = (currentStudent.contacts || []).filter(c => c.id !== id);
    renderContacts(currentStudent.contacts);
    toast('Contactmoment verwijderd', 'success');
  } catch (err) {
    toast('Fout: ' + err.message, 'error');
  }
}

function renderDeadlines(deadlines) {
  const container = document.getElementById('deadlines-list');
  if (!deadlines.length) { container.innerHTML = '<p class="empty-state">Geen deadlines.</p>'; return; }
  const sorted = [...deadlines].sort((a, b) => new Date(a.dueDate) - new Date(b.dueDate));
  container.innerHTML = sorted.map(d => {
    const due = new Date(d.dueDate);
    const daysLeft = Math.ceil((due - Date.now()) / 86400000);
    let dueClass = '';
    if (!d.isCompleted && daysLeft < 0) dueClass = 'overdue';
    else if (!d.isCompleted && daysLeft <= 7) dueClass = 'soon';
    const dueText = daysLeft < 0 ? `${Math.abs(daysLeft)} dagen te laat` : daysLeft === 0 ? 'Vandaag' : `Over ${daysLeft} dagen`;
    return `<div class="deadline-item ${dueClass}">
      <input type="checkbox" class="deadline-check" ${d.isCompleted ? 'checked' : ''} onchange="toggleDeadline(${d.id}, this.checked)" />
      <span class="deadline-title ${d.isCompleted ? 'done' : ''}">${esc(d.title)}</span>
      <span class="deadline-due ${dueClass}">${due.toLocaleDateString('nl-NL')} (${dueText})</span>
      <button class="deadline-delete" onclick="deleteDeadline(${d.id})" title="Verwijder">✕</button>
    </div>`;
  }).join('');
}

async function addDeadline() {
  if (!currentStudent) return;
  const title = document.getElementById('new-deadline-title').value.trim();
  const dueDate = document.getElementById('new-deadline-date').value;
  if (!title) { toast('Voer een titel in', 'error'); return; }
  try {
    const deadline = await api(`students/${currentStudent.id}/deadlines`, 'POST', { title, dueDate: dueDate || today() });
    currentStudent.deadlines = [...(currentStudent.deadlines || []), deadline];
    renderDeadlines(currentStudent.deadlines);
    document.getElementById('new-deadline-title').value = '';
    toast('Deadline toegevoegd', 'success');
  } catch (err) {
    toast('Fout: ' + err.message, 'error');
  }
}

async function toggleDeadline(id, isCompleted) {
  if (!currentStudent) return;
  try {
    const updated = await api(`students/${currentStudent.id}/deadlines/${id}`, 'PUT', { isCompleted });
    const idx = currentStudent.deadlines.findIndex(d => d.id === id);
    if (idx >= 0) currentStudent.deadlines[idx] = updated;
    renderDeadlines(currentStudent.deadlines);
  } catch (err) {
    toast('Fout: ' + err.message, 'error');
  }
}

async function deleteDeadline(id) {
  if (!currentStudent) return;
  try {
    await api(`students/${currentStudent.id}/deadlines/${id}`, 'DELETE');
    currentStudent.deadlines = (currentStudent.deadlines || []).filter(d => d.id !== id);
    renderDeadlines(currentStudent.deadlines);
    toast('Deadline verwijderd', 'success');
  } catch (err) {
    toast('Fout: ' + err.message, 'error');
  }
}

async function saveNotes() {
  if (!currentStudent) return;
  const notes = document.getElementById('notes-textarea').value;
  try {
    const dto = buildStudentDto(currentStudent);
    dto.notes = notes;
    await api(`students/${currentStudent.id}`, 'PUT', dto);
    currentStudent.notes = notes;
    // Update in lists
    const idx = allStudents.findIndex(s => s.id === currentStudent.id);
    if (idx >= 0) allStudents[idx].notes = notes;
    toast('Notities opgeslagen', 'success');
  } catch (err) {
    toast('Fout: ' + err.message, 'error');
  }
}

function buildStudentDto(s) {
  return {
    firstName: s.firstName, lastName: s.lastName, email: s.email, phone: s.phone,
    studentNumber: s.studentNumber, type: s.type, myRole: s.myRole, company: s.company,
    location: s.location, address: s.address, studyProgram: s.studyProgram, cohort: s.cohort,
    companyAddress: s.companyAddress, companySupervisorName: s.companySupervisorName,
    companySupervisorEmail: s.companySupervisorEmail, companySupervisorPhone: s.companySupervisorPhone,
    startDate: s.startDate, endDate: s.endDate, status: s.status, notes: s.notes
  };
}

async function toggleArchive() {
  if (!currentStudent) return;
  try {
    const updated = await api(`students/${currentStudent.id}/archive`, 'POST');
    currentStudent = updated;

    // Move between lists
    if (updated.archived) {
      allStudents = allStudents.filter(s => s.id !== updated.id);
      archivedStudents = [updated, ...archivedStudents];
    } else {
      archivedStudents = archivedStudents.filter(s => s.id !== updated.id);
      allStudents = [updated, ...allStudents];
    }
    renderStudentList();
    renderArchivedList();

    document.getElementById('archive-btn').textContent = updated.archived ? 'Herstellen' : 'Archiveren';
    toast(updated.archived ? 'Student gearchiveerd' : 'Student hersteld', 'success');

    // Refresh stats
    const stats = await api('students/stats');
    renderStats(stats);
  } catch (err) {
    toast('Fout: ' + err.message, 'error');
  }
}

/* ── Tabs ── */
function switchTab(tab) {
  document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
  document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
  document.getElementById(`tab-${tab}`).classList.add('active');
  document.querySelectorAll('.drawer-tabs .tab-btn').forEach(b => {
    if (b.getAttribute('onclick')?.includes(`'${tab}'`)) b.classList.add('active');
  });
}

/* ── Add / Edit Student Modal ── */
function openAddStudentModal() {
  editingStudentId = null;
  document.getElementById('modal-title').textContent = 'Student Toevoegen';
  clearForm();
  document.getElementById('modal-overlay').classList.remove('hidden');
  document.getElementById('add-student-modal').classList.remove('hidden');
}

function closeModal() {
  document.getElementById('modal-overlay').classList.add('hidden');
  document.getElementById('add-student-modal').classList.add('hidden');
  editingStudentId = null;
}

function clearForm() {
  ['f-firstname','f-lastname','f-studentnumber','f-email','f-phone','f-company',
   'f-studyprogram','f-cohort','f-address','f-companyaddress','f-supervisorname',
   'f-supervisoremail','f-notes'].forEach(id => { const el = document.getElementById(id); if (el) el.value = ''; });
  document.getElementById('f-type').value = 'stage';
  document.getElementById('f-role').value = 'docentbegeleider';
  document.getElementById('f-status').value = 'Opstart';
  document.getElementById('f-startdate').value = today();
  const e = new Date(); e.setMonth(e.getMonth() + 5);
  document.getElementById('f-enddate').value = e.toISOString().split('T')[0];
}

async function saveStudent() {
  const firstName = document.getElementById('f-firstname').value.trim();
  const lastName = document.getElementById('f-lastname').value.trim();
  const company = document.getElementById('f-company').value.trim();
  const startDate = document.getElementById('f-startdate').value;
  const endDate = document.getElementById('f-enddate').value;

  if (!firstName || !lastName || !company || !startDate || !endDate) {
    toast('Vul alle verplichte velden in', 'error');
    return;
  }

  const dto = {
    firstName, lastName,
    email: document.getElementById('f-email').value.trim() || null,
    phone: document.getElementById('f-phone').value.trim() || null,
    studentNumber: document.getElementById('f-studentnumber').value.trim() || null,
    type: document.getElementById('f-type').value,
    myRole: document.getElementById('f-role').value,
    company,
    studyProgram: document.getElementById('f-studyprogram').value.trim() || null,
    cohort: document.getElementById('f-cohort').value.trim() || null,
    address: document.getElementById('f-address').value.trim() || null,
    companyAddress: document.getElementById('f-companyaddress').value.trim() || null,
    companySupervisorName: document.getElementById('f-supervisorname').value.trim() || null,
    companySupervisorEmail: document.getElementById('f-supervisoremail').value.trim() || null,
    companySupervisorPhone: null,
    location: null,
    startDate, endDate,
    status: document.getElementById('f-status').value,
    notes: document.getElementById('f-notes').value.trim() || null
  };

  try {
    if (editingStudentId) {
      const updated = await api(`students/${editingStudentId}`, 'PUT', dto);
      const idx = allStudents.findIndex(s => s.id === updated.id);
      if (idx >= 0) allStudents[idx] = updated;
    } else {
      const created = await api('students', 'POST', dto);
      allStudents = [created, ...allStudents];
    }
    renderStudentList();
    closeModal();
    toast(editingStudentId ? 'Student bijgewerkt' : 'Student toegevoegd', 'success');
    // Refresh stats
    const stats = await api('students/stats');
    renderStats(stats);
  } catch (err) {
    toast('Fout: ' + err.message, 'error');
  }
}

/* ── Export ── */
async function exportCsv() {
  window.location.href = '/api/students/export';
}

/* ── Utilities ── */
function esc(str) {
  if (!str) return '';
  return String(str).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

function toast(msg, type = 'info') {
  const container = document.getElementById('toast-container');
  const t = document.createElement('div');
  t.className = `toast ${type}`;
  t.textContent = msg;
  container.appendChild(t);
  setTimeout(() => t.remove(), 3500);
}
