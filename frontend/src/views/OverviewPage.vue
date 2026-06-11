<script setup lang="ts">
import type { Alert, DashboardSummary, Machine } from '../types';
import { formatDate } from '../utils/format';
import { healthSeverity, severityClass } from '../utils/maintenance';
import EmptyState from '../components/EmptyState.vue';

defineProps<{
  summary: DashboardSummary;
  criticalAlerts: Alert[];
  allMachines: Machine[];
}>();

defineEmits<{
  navigate: [page: 'resellers' | 'machines'];
  openMachine: [machineId: string];
  dispatch: [machine: Machine, alert?: Alert];
}>();
</script>

<template>
  <section class="page-stack">
    <div class="metric-grid">
      <article>
        <span>Resellers</span>
        <strong>{{ summary.companyCount }}</strong>
      </article>
      <article>
        <span>Machines</span>
        <strong>{{ summary.machineCount }}</strong>
      </article>
      <article>
        <span>Parts monitored</span>
        <strong>{{ summary.partCount }}</strong>
      </article>
      <article class="warning">
        <span>Open alerts</span>
        <strong>{{ summary.openAlertCount }}</strong>
      </article>
      <article class="danger">
        <span>Critical</span>
        <strong>{{ summary.criticalAlertCount }}</strong>
      </article>
    </div>

    <div class="two-column stable-panels">
      <div class="panel">
        <div class="panel-title">
          <h3>Reseller fleet risk</h3>
          <span>Fleet health across all machines</span>
        </div>
        <div v-if="summary.resellersAtRisk.length" class="risk-list">
          <button v-for="risk in summary.resellersAtRisk" :key="risk.companyId" @click="$emit('navigate', 'resellers')">
            <span>
              <strong>{{ risk.companyName }}</strong>
              <small>{{ risk.openAlerts }} open, {{ risk.criticalAlerts }} critical, earliest {{ formatDate(risk.earliestDueDate) }}</small>
              <small>Alert-part avg health {{ risk.averageAlertHealthScore }}</small>
            </span>
            <b :class="['score', healthSeverity(risk.fleetHealthScore)]">{{ risk.fleetHealthScore }}</b>
          </button>
        </div>
        <EmptyState v-else title="No reseller risk" detail="No reseller has open maintenance alerts in the current scope." />
      </div>

      <div class="panel high-visibility">
        <div class="panel-title">
          <h3>Critical dispatch queue</h3>
          <span>{{ criticalAlerts.length }} immediate</span>
        </div>
        <div v-if="criticalAlerts.length" class="alert-list compact">
          <article v-for="alert in criticalAlerts.slice(0, 6)" :key="alert.id">
            <span :class="['pill', severityClass(alert.severity)]">{{ alert.severity }}</span>
            <button class="link-button" @click="$emit('openMachine', alert.machineId)">
              <strong>{{ alert.machineName }}</strong>
              <small>{{ alert.partName }} / first alerted {{ formatDate(alert.createdAt) }}</small>
            </button>
            <button
              :class="['primary-small', { dispatched: alert.status === 'Dispatched' }]"
              :disabled="alert.status === 'Dispatched'"
              @click="$emit('dispatch', allMachines.find((machine) => machine.id === alert.machineId)!, alert)"
            >
              {{ alert.status === 'Dispatched' ? 'Dispatched' : 'Dispatch' }}
            </button>
          </article>
        </div>
        <EmptyState v-else title="No critical dispatches" detail="Warning-level alerts are being monitored, but nothing requires immediate technician dispatch." />
      </div>
    </div>
  </section>
</template>
