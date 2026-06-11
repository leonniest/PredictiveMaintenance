<script setup lang="ts">
import { computed, ref, watch } from 'vue';

interface ArchItem {
  name: string;
  detail: string;
}

interface ArchNode {
  id: string;
  title: string;
  subtitle: string;
  kind: 'client' | 'frontend' | 'api' | 'service' | 'infra' | 'external';
  x: number;
  y: number;
  w: number;
  h: number;
  summary: string;
  facts: string[];
  contains: ArchItem[];
}

interface ArchEdge {
  from: string;
  to: string;
  label?: string;
}

const nodes: ArchNode[] = [
  {
    id: 'browser',
    title: 'Browser',
    subtitle: 'Admin / Reseller',
    kind: 'client',
    x: 16, y: 290, w: 140, h: 72,
    summary: 'Users sign in with demo accounts. Admin sees every reseller, reseller users only their own fleet.',
    facts: ['Dashboard at http://localhost:5173', 'JWT kept in localStorage', 'Data auto-refreshes every 20 s'],
    contains: [
      { name: 'Admin role', detail: 'admin@showcase.local — full scope: all resellers, alert evaluation endpoint, all settings.' },
      { name: 'Reseller role', detail: 'reseller@atlas-access.test / vector-gate / orbit-entry — scoped to their own company, gets the assistant widget.' }
    ]
  },
  {
    id: 'spa',
    title: 'Vue 3 Dashboard',
    subtitle: 'Nginx container :5173',
    kind: 'frontend',
    x: 216, y: 282, w: 178, h: 88,
    summary: 'Single-page app built with Vite + TypeScript. Nginx serves the build and proxies /api to the API container.',
    facts: ['Vue 3 script-setup, no router/store library', 'All counts derived from one /api/alerts fetch', 'Silent background refresh every 20 s'],
    contains: [
      { name: 'Pages', detail: 'Overview, Resellers/Production, Machines, Analytics, Alerts, Settings, Architecture — switched by App.vue state.' },
      { name: 'Components', detail: 'AppSidebar, TopAlertSummary, LoginView, BarChart (inline SVG), EmptyState, AssistantWidget (reseller chat).' },
      { name: 'api.ts client', detail: 'Tiny fetch wrapper: attaches the JWT as Bearer header, all calls relative to /api.' },
      { name: 'Nginx proxy', detail: 'location /api/ → http://api:8080/api/ inside the compose network; everything else serves the SPA build.' }
    ]
  },
  {
    id: 'api',
    title: 'ASP.NET Core API',
    subtitle: '.NET 10 · :5088',
    kind: 'api',
    x: 458, y: 240, w: 184, h: 96,
    summary: 'Minimal-API host. Validates JWTs, exposes all endpoints, seeds the database on startup.',
    facts: ['Swagger UI at /swagger', 'JWT bearer auth, 8 h expiry, role + companyId claims', 'Seeds deterministic demo data into an empty DB'],
    contains: [
      { name: 'Auth endpoints', detail: 'POST /api/auth/login issues the JWT. GET /api/me echoes the caller identity.' },
      { name: 'Dashboard endpoints', detail: 'GET /api/dashboard/summary, /api/companies, /api/companies/{id}, /api/machines/{id}.' },
      { name: 'Alert endpoints', detail: 'GET /api/alerts, POST /api/alerts/evaluate (admin), POST /api/alerts/{id}/acknowledge, POST /api/alerts/{id}/notify.' },
      { name: 'Support endpoints', detail: 'GET/PUT /api/support/settings, POST /api/support/dispatch (sends technician mail, marks alert Dispatched).' },
      { name: 'Assistant endpoints', detail: 'GET /api/assistant/faq, GET/POST /api/assistant/sessions, POST /api/assistant/sessions/{id}/messages.' },
      { name: 'Analytics endpoints', detail: 'GET /api/analytics/part-lifetimes, GET /api/analytics/failure-trends.' }
    ]
  },
  {
    id: 'evaluator',
    title: 'Alert Evaluator',
    subtitle: 'Background service',
    kind: 'api',
    x: 458, y: 430, w: 184, h: 72,
    summary: 'Hosted service inside the API. Re-runs the prediction for every part 15 s after startup and every 5 minutes.',
    facts: ['Creates alerts for Warning/Critical parts', 'Updates severity, due date and health on existing alerts'],
    contains: [
      { name: 'Schedule', detail: 'First run 15 s after the API starts, then a fixed 5-minute loop.' },
      { name: 'Result', detail: 'One open alert per part at most; resolved alerts are left alone.' }
    ]
  },
  {
    id: 'auth',
    title: 'AuthService',
    subtitle: 'Application layer',
    kind: 'service',
    x: 712, y: 16, w: 190, h: 58,
    summary: 'Checks login credentials against PBKDF2-hashed passwords stored per user.',
    facts: ['PBKDF2 password hashing', 'Returns the profile used to build JWT claims'],
    contains: [
      { name: 'UserContext', detail: 'Every other service receives userId, role and companyId parsed from the JWT — this scopes all queries.' }
    ]
  },
  {
    id: 'dashboard',
    title: 'DashboardService',
    subtitle: 'Application layer',
    kind: 'service',
    x: 712, y: 96, w: 190, h: 58,
    summary: 'Builds the summary, company, machine and analytics views, scoped to what the caller may see.',
    facts: ['Fleet health = average of all part health scores', 'Part lifetimes and failure trends from technician records'],
    contains: [
      { name: 'Summary', detail: 'Company/machine/part counts, open + critical alerts, reseller risk list, upcoming alerts.' },
      { name: 'Analytics', detail: 'Replacement life per part type and per concrete part model; monthly work-order trend.' }
    ]
  },
  {
    id: 'prediction',
    title: 'PredictionService',
    subtitle: 'Application layer',
    kind: 'service',
    x: 712, y: 176, w: 190, h: 58,
    summary: 'Pure heuristic model that turns usage history into a health score, severity and predicted due date.',
    facts: ['Inputs: movement cycles, age, temperature', 'Baselines learned from seeded replacement records', 'Health score 0–100 with reason codes'],
    contains: [
      { name: 'Ratios', detail: 'Movement ratio, age ratio and temperature ratio against the per-part-type replacement baseline.' },
      { name: 'Output', detail: 'HealthScore, Severity (Info/Warning/Critical), PredictedDueDate, confidence and reason codes.' }
    ]
  },
  {
    id: 'alert',
    title: 'AlertService',
    subtitle: 'Application layer',
    kind: 'service',
    x: 712, y: 256, w: 190, h: 58,
    summary: 'Creates and updates maintenance alerts and pushes notifications over the configured channel.',
    facts: ['Statuses: Open → Notified / Dispatched → Acknowledged / Resolved', 'Notify channel chosen by Notifications:Mode'],
    contains: [
      { name: 'Evaluate', detail: 'Runs the prediction per part; Warning/Critical creates or refreshes the open alert.' },
      { name: 'Notify', detail: 'Sends the alert via SMTP (Mailpit) or publishes it to RabbitMQ, then stamps channel and time.' }
    ]
  },
  {
    id: 'support',
    title: 'SupportService',
    subtitle: 'Application layer',
    kind: 'service',
    x: 712, y: 336, w: 190, h: 58,
    summary: 'Per-reseller support settings and technician dispatch mails.',
    facts: ['Dispatch lists the machine’s open alerts in the mail body', 'Marks the dispatched alert status = Dispatched (persisted)'],
    contains: [
      { name: 'Settings', detail: 'Support mailbox, technician dispatch mailbox, escalation phone, SLA hours — one row per reseller.' },
      { name: 'Dispatch', detail: 'Mails support + technician addresses; falls back to the company contact when none are set.' }
    ]
  },
  {
    id: 'assistant',
    title: 'AssistantService',
    subtitle: 'Application layer',
    kind: 'service',
    x: 712, y: 416, w: 190, h: 58,
    summary: 'SQL-persisted chat sessions for reseller users, answered by the DeepSeek API.',
    facts: ['System prompt + last 16 messages per call', 'FAQ prompts served from the API', 'API errors are returned as readable chat replies'],
    contains: [
      { name: 'Sessions', detail: 'AssistantChatSession + AssistantChatMessage tables, one history per user, title from the first question.' },
      { name: 'Fallback', detail: 'Without a configured key the service answers from a local playbook instead of calling out.' }
    ]
  },
  {
    id: 'mssql',
    title: 'MSSQL 2022',
    subtitle: 'EF Core 10 · :14333',
    kind: 'infra',
    x: 990, y: 60, w: 204, h: 84,
    summary: 'Single database holding the whole domain model, created and seeded automatically on first start.',
    facts: ['EnsureCreated + deterministic seed (3 resellers, 9 machines)', 'Schema changes need a volume reset', 'sa / Your_strong_password123!'],
    contains: [
      { name: 'Fleet entities', detail: 'Company → Machine → ControllerUnit (PLC/PCB) → MachinePart (motor, belt, relay, sensor).' },
      { name: 'History entities', detail: 'TelemetryDailySummary (cycles, rotations, temperature) and TechnicianRecord (inspections, repairs, replacements, failures).' },
      { name: 'Operational entities', detail: 'MaintenanceAlert, ResellerSupportSettings, AppUser, assistant chat sessions + messages.' }
    ]
  },
  {
    id: 'mailpit',
    title: 'Mailpit',
    subtitle: 'SMTP :1025 · UI :8025',
    kind: 'infra',
    x: 990, y: 240, w: 204, h: 70,
    summary: 'Local SMTP sink. Every notification and dispatch mail lands here instead of a real mailbox.',
    facts: ['Inbox UI at http://localhost:8025', 'Used by alert notify and technician dispatch'],
    contains: [
      { name: 'Senders', detail: 'SmtpAlertNotifier (alert notifications) and SmtpEmailSender (technician dispatch / mail support).' }
    ]
  },
  {
    id: 'rabbitmq',
    title: 'RabbitMQ',
    subtitle: 'AMQP :5672 · UI :15672',
    kind: 'infra',
    x: 990, y: 350, w: 204, h: 70,
    summary: 'Optional alert channel: with Notifications:Mode=RabbitMQ alert payloads are published instead of mailed.',
    facts: ['Queue: predictive-maintenance.alerts', 'Management UI guest/guest'],
    contains: [
      { name: 'Payload', detail: 'JSON alert with machine, part, severity, due date and health score, published by RabbitMqAlertNotifier.' }
    ]
  },
  {
    id: 'deepseek',
    title: 'DeepSeek API',
    subtitle: 'External · api.deepseek.com',
    kind: 'external',
    x: 990, y: 460, w: 204, h: 70,
    summary: 'LLM behind the assistant. Called server-side only — the key never reaches the browser.',
    facts: ['Model: deepseek-v4-flash', 'Key from root .env (DEEPSEEK_API_KEY)', 'POST /chat/completions'],
    contains: [
      { name: 'Request', detail: 'System prompt + recent chat history, temperature 0.2; only the final answer content is stored and shown.' }
    ]
  }
];

const edges: ArchEdge[] = [
  { from: 'browser', to: 'spa', label: 'HTTP :5173' },
  { from: 'spa', to: 'api', label: '/api + JWT' },
  { from: 'api', to: 'auth' },
  { from: 'api', to: 'dashboard' },
  { from: 'api', to: 'alert' },
  { from: 'api', to: 'support' },
  { from: 'api', to: 'assistant' },
  { from: 'evaluator', to: 'alert', label: 'every 5 min' },
  { from: 'dashboard', to: 'prediction' },
  { from: 'alert', to: 'prediction' },
  { from: 'auth', to: 'mssql' },
  { from: 'dashboard', to: 'mssql' },
  { from: 'alert', to: 'mssql' },
  { from: 'support', to: 'mssql' },
  { from: 'assistant', to: 'mssql' },
  { from: 'alert', to: 'mailpit', label: 'SMTP' },
  { from: 'alert', to: 'rabbitmq', label: 'AMQP (optional)' },
  { from: 'support', to: 'mailpit', label: 'SMTP' },
  { from: 'assistant', to: 'deepseek', label: 'HTTPS' }
];

const selectedId = ref<string | null>(null);
const expandedItem = ref<string | null>(null);

const selectedNode = computed(() => nodes.find((n) => n.id === selectedId.value) ?? null);
const connectedNodes = computed(() => {
  if (!selectedId.value) return [];
  const ids = new Set<string>();
  for (const edge of edges) {
    if (edge.from === selectedId.value) ids.add(edge.to);
    if (edge.to === selectedId.value) ids.add(edge.from);
  }
  return nodes.filter((n) => ids.has(n.id));
});

watch(selectedId, () => {
  expandedItem.value = null;
});

function nodeById(id: string) {
  return nodes.find((n) => n.id === id)!;
}

function edgePath(edge: ArchEdge) {
  const a = nodeById(edge.from);
  const b = nodeById(edge.to);
  const x1 = a.x + a.w;
  const y1 = a.y + a.h / 2;
  const x2 = b.x;
  const y2 = b.y + b.h / 2;
  const dx = Math.max(36, (x2 - x1) / 2);
  return `M ${x1} ${y1} C ${x1 + dx} ${y1}, ${x2 - dx} ${y2}, ${x2} ${y2}`;
}

function edgeLabelPos(edge: ArchEdge) {
  const a = nodeById(edge.from);
  const b = nodeById(edge.to);
  return {
    x: (a.x + a.w + b.x) / 2,
    y: (a.y + a.h / 2 + b.y + b.h / 2) / 2 - 6
  };
}

function isEdgeActive(edge: ArchEdge) {
  return selectedId.value !== null && (edge.from === selectedId.value || edge.to === selectedId.value);
}

function select(id: string) {
  selectedId.value = selectedId.value === id ? null : id;
}
</script>

<template>
  <section class="arch-layout">
    <div class="panel arch-graph-panel">
      <div class="panel-title">
        <h3>System architecture</h3>
        <span>Click a block to scope into it</span>
      </div>
      <svg viewBox="0 0 1210 545" class="arch-svg" role="img" aria-label="Architecture diagram">
        <defs>
          <marker id="arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">
            <path d="M 0 0 L 10 5 L 0 10 z" fill="#9fb4ae" />
          </marker>
          <marker id="arrow-active" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">
            <path d="M 0 0 L 10 5 L 0 10 z" fill="#245f73" />
          </marker>
        </defs>

        <g v-for="edge in edges" :key="`${edge.from}-${edge.to}`">
          <path
            :d="edgePath(edge)"
            :class="['arch-edge', { active: isEdgeActive(edge), dimmed: selectedId && !isEdgeActive(edge) }]"
            :marker-end="isEdgeActive(edge) ? 'url(#arrow-active)' : 'url(#arrow)'"
          />
          <text
            v-if="edge.label && isEdgeActive(edge)"
            :x="edgeLabelPos(edge).x"
            :y="edgeLabelPos(edge).y"
            class="arch-edge-label"
            text-anchor="middle"
          >
            {{ edge.label }}
          </text>
        </g>

        <g
          v-for="node in nodes"
          :key="node.id"
          :class="['arch-node', node.kind, { selected: selectedId === node.id, dimmed: selectedId && selectedId !== node.id && !connectedNodes.some((n) => n.id === node.id) }]"
          @click="select(node.id)"
        >
          <rect :x="node.x" :y="node.y" :width="node.w" :height="node.h" rx="10" />
          <text :x="node.x + node.w / 2" :y="node.y + node.h / 2 - 6" text-anchor="middle" class="arch-node-title">{{ node.title }}</text>
          <text :x="node.x + node.w / 2" :y="node.y + node.h / 2 + 14" text-anchor="middle" class="arch-node-subtitle">{{ node.subtitle }}</text>
        </g>
      </svg>
      <div class="arch-legend">
        <span class="client">Client</span>
        <span class="frontend">Frontend</span>
        <span class="api">API host</span>
        <span class="service">Services</span>
        <span class="infra">Infrastructure</span>
        <span class="external">External</span>
      </div>
    </div>

    <div class="panel arch-detail-panel">
      <template v-if="selectedNode">
        <div class="panel-title">
          <div>
            <h3>{{ selectedNode.title }}</h3>
            <span>{{ selectedNode.subtitle }}</span>
          </div>
          <button class="secondary" @click="selectedId = null">Back</button>
        </div>
        <p class="arch-summary">{{ selectedNode.summary }}</p>
        <ul class="arch-facts">
          <li v-for="fact in selectedNode.facts" :key="fact">{{ fact }}</li>
        </ul>
        <div class="arch-contains">
          <h4>Contains</h4>
          <button
            v-for="item in selectedNode.contains"
            :key="item.name"
            :class="['arch-item', { open: expandedItem === item.name }]"
            @click="expandedItem = expandedItem === item.name ? null : item.name"
          >
            <strong>{{ item.name }}</strong>
            <small v-if="expandedItem === item.name">{{ item.detail }}</small>
          </button>
        </div>
        <div v-if="connectedNodes.length" class="arch-connections">
          <h4>Connected to</h4>
          <button v-for="node in connectedNodes" :key="node.id" class="arch-chip" @click="select(node.id)">
            {{ node.title }}
          </button>
        </div>
      </template>
      <template v-else>
        <div class="panel-title">
          <h3>Total picture</h3>
        </div>
        <p class="arch-summary">
          The browser loads the Vue dashboard from Nginx. Every data call goes through one ASP.NET Core API that talks to the
          application services. The services read and write MSSQL, mail through Mailpit, optionally publish to RabbitMQ, and the
          assistant calls the DeepSeek API. A background evaluator re-scores every part each 5 minutes.
        </p>
        <p class="arch-summary">Click any block in the graph to see what it contains and what it connects to.</p>
      </template>
    </div>
  </section>
</template>
