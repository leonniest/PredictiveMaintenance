<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue';
import { api, clearToken, login, setToken } from './api';
import AppSidebar from './components/AppSidebar.vue';
import AssistantWidget from './components/AssistantWidget.vue';
import LoginView from './components/LoginView.vue';
import TopAlertSummary from './components/TopAlertSummary.vue';
import AlertsPage from './views/AlertsPage.vue';
import AnalyticsPage from './views/AnalyticsPage.vue';
import MachinesPage from './views/MachinesPage.vue';
import OverviewPage from './views/OverviewPage.vue';
import ResellersPage from './views/ResellersPage.vue';
import SettingsPage from './views/SettingsPage.vue';
import type {
  Alert,
  AssistantSession,
  Company,
  DashboardSummary,
  FailureTrend,
  FaqPrompt,
  LoginResponse,
  Machine,
  PartLifetime,
  SupportSettings,
  UserProfile
} from './types';
import { formatDateTime } from './utils/format';
import { companyOpenAlerts, isOpenAlert } from './utils/maintenance';

type Page = 'overview' | 'resellers' | 'machines' | 'analytics' | 'alerts' | 'settings';

const email = ref('admin@showcase.local');
const password = ref('Admin123!');
const error = ref('');
const loading = ref(false);
const user = ref<UserProfile | null>(null);
const summary = ref<DashboardSummary | null>(null);
const companies = ref<Company[]>([]);
const alerts = ref<Alert[]>([]);
const lifetimes = ref<PartLifetime[]>([]);
const trends = ref<FailureTrend[]>([]);
const supportSettings = ref<SupportSettings[]>([]);
const activePage = ref<Page>('overview');
const selectedMachineId = ref<string | null>(null);
const dispatchStatus = ref('');
const pendingAlertId = ref('');
const assistantBusy = ref(false);
const assistantFaq = ref<FaqPrompt[]>([]);
const assistantSessions = ref<AssistantSession[]>([]);
const activeAssistantSessionId = ref<string | null>(null);

const allMachines = computed(() =>
  companies.value.flatMap((company) =>
    company.machines.map((machine) => ({
      ...machine,
      companyName: machine.companyName || company.name
    }))
  )
);
const selectedMachine = computed(() => allMachines.value.find((machine) => machine.id === selectedMachineId.value) ?? allMachines.value[0] ?? null);
const selectedMachineAlerts = computed(() => selectedMachine.value ? alerts.value.filter((alert) => alert.machineId === selectedMachine.value?.id && isOpenAlert(alert)) : []);
const openAlerts = computed(() => alerts.value.filter(isOpenAlert));
const criticalAlerts = computed(() => openAlerts.value.filter((alert) => alert.severity === 'Critical'));
// Counts shown anywhere in the UI all derive from the single /api/alerts fetch so
// overview, reseller cards and alert center always agree.
const summaryView = computed<DashboardSummary | null>(() => {
  if (!summary.value) return null;
  return {
    ...summary.value,
    openAlertCount: openAlerts.value.length,
    criticalAlertCount: criticalAlerts.value.length,
    resellersAtRisk: summary.value.resellersAtRisk.map((risk) => {
      const companyAlerts = companyOpenAlerts(risk.companyId, alerts.value);
      return {
        ...risk,
        openAlerts: companyAlerts.length,
        criticalAlerts: companyAlerts.filter((alert) => alert.severity === 'Critical').length
      };
    })
  };
});
const isReseller = computed(() => user.value?.role === 'Reseller');
const pageTitle = computed(() => ({
  overview: 'Overview',
  resellers: user.value?.role === 'Admin' ? 'Resellers' : 'Production Scope',
  machines: 'Machine Detail',
  analytics: 'Analytics',
  alerts: 'Alert Center',
  settings: 'Support Settings'
})[activePage.value]);

async function signIn() {
  loading.value = true;
  error.value = '';
  try {
    const response: LoginResponse = await login(email.value, password.value);
    setToken(response.token);
    user.value = response.user;
    await loadDashboard();
    startAutoRefresh();
    if (response.user.role === 'Reseller') {
      await loadAssistant();
    }
  } catch {
    error.value = 'Login failed. Check the demo credentials or backend status.';
  } finally {
    loading.value = false;
  }
}

const refreshIntervalMs = 20000;
let refreshTimer: ReturnType<typeof setInterval> | null = null;
let refreshInFlight = false;

async function loadDashboard(background = false) {
  if (refreshInFlight) return;
  refreshInFlight = true;
  if (!background) {
    loading.value = true;
    dispatchStatus.value = '';
  }
  try {
    const [summaryResult, companyResult, alertResult, lifetimeResult, trendResult, supportResult] = await Promise.all([
      api.summary(),
      api.companies(),
      api.alerts(),
      api.partLifetimes(),
      api.failureTrends(),
      api.supportSettings()
    ]);
    summary.value = summaryResult;
    companies.value = companyResult;
    alerts.value = alertResult;
    lifetimes.value = lifetimeResult;
    trends.value = trendResult;
    supportSettings.value = supportResult;
    selectedMachineId.value = selectedMachineId.value ?? companyResult[0]?.machines[0]?.id ?? null;
    error.value = '';
  } catch {
    if (!background) {
      error.value = 'Dashboard data could not be loaded. Start Docker and the .NET API, then retry.';
    }
  } finally {
    refreshInFlight = false;
    if (!background) {
      loading.value = false;
    }
  }
}

function startAutoRefresh() {
  stopAutoRefresh();
  refreshTimer = setInterval(() => {
    if (user.value) {
      void loadDashboard(true);
    }
  }, refreshIntervalMs);
}

function stopAutoRefresh() {
  if (refreshTimer) {
    clearInterval(refreshTimer);
    refreshTimer = null;
  }
}

async function loadAssistant() {
  try {
    const [faq, sessions] = await Promise.all([api.assistantFaq(), api.assistantSessions()]);
    assistantFaq.value = faq;
    assistantSessions.value = sessions.length ? sessions : [await api.createAssistantSession('Maintenance assistant')];
    activeAssistantSessionId.value = assistantSessions.value[0]?.id ?? null;
  } catch {
    assistantFaq.value = [];
  }
}

async function newAssistantChat() {
  const session = await api.createAssistantSession('Maintenance assistant');
  assistantSessions.value = [session, ...assistantSessions.value];
  activeAssistantSessionId.value = session.id;
}

async function sendAssistantMessage(content: string) {
  const session = assistantSessions.value.find((item) => item.id === activeAssistantSessionId.value) ?? assistantSessions.value[0];
  if (!session || !content.trim()) return;
  assistantBusy.value = true;
  try {
    const updated = await api.sendAssistantMessage(session.id, content);
    assistantSessions.value = assistantSessions.value.map((item) => (item.id === updated.id ? updated : item));
    activeAssistantSessionId.value = updated.id;
  } finally {
    assistantBusy.value = false;
  }
}

async function notify(alert: Alert) {
  pendingAlertId.value = alert.id;
  try {
    await api.notifyAlert(alert.id);
    alerts.value = alerts.value.map((item) => item.id === alert.id ? { ...item, status: 'Notified', notifiedAt: new Date().toISOString(), notificationChannel: 'smtp' } : item);
  } finally {
    pendingAlertId.value = '';
  }
}

async function acknowledge(alert: Alert) {
  pendingAlertId.value = alert.id;
  try {
    await api.acknowledgeAlert(alert.id);
    alerts.value = alerts.value.map((item) => item.id === alert.id ? { ...item, status: 'Acknowledged' } : item);
  } finally {
    pendingAlertId.value = '';
  }
}

async function dispatch(machine: Machine, alert?: Alert) {
  dispatchStatus.value = '';
  if (alert) {
    pendingAlertId.value = alert.id;
  }
  const note = alert
    ? `Dispatch requested from alert center for ${alert.partName}.`
    : `Machine-level dispatch requested for ${machine.name}.`;
  try {
    const result = await api.dispatchTechnician(machine.id, alert?.id, note);
    if (alert) {
      alerts.value = alerts.value.map((item) => (item.id === alert.id ? { ...item, status: 'Dispatched' } : item));
    }
    dispatchStatus.value = `Dispatch email sent to ${result.recipients.join(', ')} at ${formatDateTime(result.sentAt)}.`;
  } finally {
    pendingAlertId.value = '';
  }
}

async function saveSupport(settings: SupportSettings) {
  const updated = await api.updateSupportSettings(settings);
  supportSettings.value = supportSettings.value.map((item) => (item.companyId === updated.companyId ? updated : item));
  dispatchStatus.value = `Support settings saved for ${updated.companyName}.`;
}

function openMachine(machine: Machine) {
  selectedMachineId.value = machine.id;
  activePage.value = 'machines';
}

function openMachineById(machineId: string) {
  selectedMachineId.value = machineId;
  activePage.value = 'machines';
}

function logout() {
  stopAutoRefresh();
  clearToken();
  user.value = null;
  summary.value = null;
  companies.value = [];
  alerts.value = [];
  supportSettings.value = [];
  assistantSessions.value = [];
  activeAssistantSessionId.value = null;
  activePage.value = 'overview';
}

onMounted(() => {
  clearToken();
});

onUnmounted(() => {
  stopAutoRefresh();
});
</script>

<template>
  <LoginView
    v-if="!user"
    v-model:email="email"
    v-model:password="password"
    :loading="loading"
    :error="error"
    @sign-in="signIn"
  />

  <main v-else class="dashboard-shell">
    <AppSidebar :user="user" :active-page="activePage" @navigate="activePage = $event" @logout="logout" />

    <section class="workspace">
      <header class="topbar">
        <div>
          <p class="eyebrow">{{ user.companyName ?? 'All resellers' }}</p>
          <h2>{{ pageTitle }}</h2>
        </div>
        <div class="topbar-actions">
          <span v-if="openAlerts.length" class="alert-counter">{{ openAlerts.length }} open alerts</span>
        </div>
      </header>

      <p v-if="error" class="error">{{ error }}</p>
      <p v-if="dispatchStatus" class="success">{{ dispatchStatus }}</p>
      <TopAlertSummary :alerts="openAlerts" @open-machine="openMachineById" />

      <OverviewPage
        v-if="activePage === 'overview' && summaryView"
        :summary="summaryView"
        :critical-alerts="criticalAlerts"
        :all-machines="allMachines"
        @navigate="activePage = $event"
        @open-machine="openMachineById"
        @dispatch="dispatch"
      />
      <ResellersPage v-else-if="activePage === 'resellers'" :companies="companies" :alerts="alerts" @open-machine="openMachine" />
      <MachinesPage
        v-else-if="activePage === 'machines'"
        :machines="allMachines"
        :selected-machine="selectedMachine"
        :selected-machine-alerts="selectedMachineAlerts"
        :alerts="alerts"
        @select-machine="selectedMachineId = $event"
        @dispatch="dispatch"
      />
      <AnalyticsPage v-else-if="activePage === 'analytics'" :lifetimes="lifetimes" :trends="trends" />
      <AlertsPage
        v-else-if="activePage === 'alerts'"
        :alerts="alerts"
        :all-machines="allMachines"
        :pending-alert-id="pendingAlertId"
        @notify="notify"
        @acknowledge="acknowledge"
        @dispatch="dispatch"
      />
      <SettingsPage v-else-if="activePage === 'settings'" :support-settings="supportSettings" @save="saveSupport" />
    </section>

    <AssistantWidget
      v-if="isReseller"
      :user="user"
      :sessions="assistantSessions"
      :active-session-id="activeAssistantSessionId"
      :faq="assistantFaq"
      :busy="assistantBusy"
      @new-chat="newAssistantChat"
      @select-session="activeAssistantSessionId = $event"
      @send="sendAssistantMessage"
    />
  </main>
</template>
