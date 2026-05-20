const API = {
  async getCategories() {
    const res = await fetch('/api/categories');
    return res.json();
  },

  async getCategoryJobs(categoryId) {
    const res = await fetch(`/api/categories/${categoryId}/jobs`);
    return res.json();
  },

  async getCategoryKeywords(categoryId) {
    const res = await fetch(`/api/categories/${categoryId}/keywords`);
    return res.json();
  },

  async fetchLinkedinSearch(keywords) {
    const params = new URLSearchParams({
      keywords,
      location: 'Barcelona',
      limit: '10',
      datePosted: 'past-week',
      sortBy: 'recent'
    });
    const res = await fetch(`/api/fetchLinkedinSimple?${params}`);
    return res.json();
  },

  async updateCategory(keywords) {
    const params = new URLSearchParams({
      keywords,
      location: 'Barcelona',
      limit: '10',
      datePosted: 'past-24h',
      sortBy: 'recent'
    });
    const res = await fetch(`/api/fetchLinkedinSimple?${params}`);
    return res.json();
  },

  async updateJobNotes(jobId, notes) {
    const res = await fetch(`/api/jobs/${jobId}/notes`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ notes })
    });
    return res.json();
  },

  async getUserKeywords() {
    const res = await fetch('/api/keywords');
    return res.json();
  },

  async updateKeywordStatus(keywordId, learningStatus) {
    const res = await fetch(`/api/keywords/${keywordId}`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ learning_status: learningStatus })
    });
    return res.json();
  },

  async deleteJob(jobId) {
    const res = await fetch(`/api/jobs/${jobId}`, { method: 'DELETE' });
    return res.json();
  },

  async refreshJob(jobId) {
    const res = await fetch(`/api/jobs/${jobId}/refresh`, { method: 'PATCH' });
    return res.json();
  },

  async getProfile() {
    const res = await fetch('/api/profile');
    return res.json();
  },

  async updateProfile(data) {
    const res = await fetch('/api/profile', { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) });
    return res.json();
  },

  async addLanguage(data) { return (await fetch('/api/languages', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) })).json(); },
  async updateLanguage(id, data) { return (await fetch('/api/languages/' + id, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) })).json(); },
  async deleteLanguage(id) { return (await fetch('/api/languages/' + id, { method: 'DELETE' })).json(); },
  async addCertification(data) { return (await fetch('/api/certifications', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) })).json(); },
  async updateCertification(id, data) { return (await fetch('/api/certifications/' + id, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) })).json(); },
  async deleteCertification(id) { return (await fetch('/api/certifications/' + id, { method: 'DELETE' })).json(); },
  async addEducation(data) { return (await fetch('/api/education', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) })).json(); },
  async updateEducation(id, data) { return (await fetch('/api/education/' + id, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) })).json(); },
  async deleteEducation(id) { return (await fetch('/api/education/' + id, { method: 'DELETE' })).json(); },
  async addProject(data) { return (await fetch('/api/projects', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) })).json(); },
  async updateProject(id, data) { return (await fetch('/api/projects/' + id, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) })).json(); },
  async deleteProject(id) { return (await fetch('/api/projects/' + id, { method: 'DELETE' })).json(); },
  async addExperience(data) { return (await fetch('/api/experiences', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) })).json(); },
  async updateExperience(id, data) { return (await fetch('/api/experiences/' + id, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) })).json(); },
  async deleteExperience(id) { return (await fetch('/api/experiences/' + id, { method: 'DELETE' })).json(); },

  async generateCV(jobId) {
    const res = await fetch('/api/cv/generate/' + jobId, { method: 'POST' });
    return res.json();
  },

  async addManualJob(data) {
    const res = await fetch('/api/jobs/manual', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    return res.json();
  }
};
