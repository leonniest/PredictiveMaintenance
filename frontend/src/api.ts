import type {
  Alert,
  AssistantSession,
  Company,
  DashboardSummary,
  DispatchTechnicianResult,
  FailureTrend,
  FaqPrompt,
  LoginResponse,
  PartLifetime,
  SupportSettings
} from './types';

const tokenKey = 'pm_token';

export function getToken() {
  return localStorage.getItem(tokenKey);
}

export function setToken(token: string) {
  localStorage.setItem(tokenKey, token);
}

export function clearToken() {
  localStorage.removeItem(tokenKey);
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers);
  headers.set('Content-Type', 'application/json');
  const token = getToken();
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(path, { ...options, headers });
  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export async function login(email: string, password: string) {
  return request<LoginResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password })
  });
}

export const api = {
  summary: () => request<DashboardSummary>('/api/dashboard/summary'),
  companies: () => request<Company[]>('/api/companies'),
  alerts: () => request<Alert[]>('/api/alerts'),
  partLifetimes: () => request<PartLifetime[]>('/api/analytics/part-lifetimes'),
  failureTrends: () => request<FailureTrend[]>('/api/analytics/failure-trends'),
  notifyAlert: (id: string) => request<void>(`/api/alerts/${id}/notify`, { method: 'POST' }),
  acknowledgeAlert: (id: string) => request<void>(`/api/alerts/${id}/acknowledge`, { method: 'POST' }),
  evaluateAlerts: () => request<{ created: number }>('/api/alerts/evaluate', { method: 'POST' }),
  supportSettings: () => request<SupportSettings[]>('/api/support/settings'),
  updateSupportSettings: (settings: SupportSettings) =>
    request<SupportSettings>('/api/support/settings', {
      method: 'PUT',
      body: JSON.stringify({
        companyId: settings.companyId,
        supportDepartmentEmail: settings.supportDepartmentEmail,
        technicianDispatchEmail: settings.technicianDispatchEmail,
        escalationPhone: settings.escalationPhone,
        defaultSlaHours: settings.defaultSlaHours
      })
    }),
  dispatchTechnician: (machineId: string, alertId?: string, note = '') =>
    request<DispatchTechnicianResult>('/api/support/dispatch', {
      method: 'POST',
      body: JSON.stringify({ machineId, alertId, note })
    }),
  assistantFaq: () => request<FaqPrompt[]>('/api/assistant/faq'),
  assistantSessions: () => request<AssistantSession[]>('/api/assistant/sessions'),
  createAssistantSession: (title?: string) =>
    request<AssistantSession>('/api/assistant/sessions', {
      method: 'POST',
      body: JSON.stringify({ title })
    }),
  sendAssistantMessage: (sessionId: string, content: string) =>
    request<AssistantSession>(`/api/assistant/sessions/${sessionId}/messages`, {
      method: 'POST',
      body: JSON.stringify({ content })
    })
};
