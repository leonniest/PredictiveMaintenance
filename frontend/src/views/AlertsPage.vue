<script setup lang="ts">
import type { Alert, Machine } from '../types';
import { formatDate, formatDateTime } from '../utils/format';
import { severityClass } from '../utils/maintenance';
import EmptyState from '../components/EmptyState.vue';

defineProps<{
  alerts: Alert[];
  allMachines: Machine[];
  pendingAlertId: string;
}>();

defineEmits<{
  notify: [alert: Alert];
  acknowledge: [alert: Alert];
  dispatch: [machine: Machine, alert?: Alert];
}>();
</script>

<template>
  <section class="panel">
    <div class="panel-title">
      <h3>Alert dashboard</h3>
      <span>{{ alerts.length }} records</span>
    </div>
    <div v-if="alerts.length" class="alert-list">
      <article v-for="alert in alerts" :key="alert.id" :class="['alert-row', severityClass(alert.severity)]">
        <span :class="['pill', severityClass(alert.severity)]">{{ alert.severity }}</span>
        <div>
          <strong>{{ alert.machineName }} / {{ alert.partName }}</strong>
          <small>{{ alert.message }}</small>
          <small>First alerted {{ formatDateTime(alert.createdAt) }} / due {{ formatDate(alert.predictedDueDate) }} / health {{ alert.healthScore }} / status {{ alert.status }}</small>
        </div>
        <div class="alert-actions">
          <button class="secondary" :disabled="pendingAlertId === alert.id" @click="$emit('notify', alert)">Notify reseller</button>
          <button :disabled="pendingAlertId === alert.id" @click="$emit('dispatch', allMachines.find((machine) => machine.id === alert.machineId)!, alert)">Mail support</button>
          <button class="secondary" :disabled="pendingAlertId === alert.id || alert.status === 'Acknowledged'" @click="$emit('acknowledge', alert)">
            {{ pendingAlertId === alert.id ? 'Saving' : 'Acknowledge' }}
          </button>
        </div>
      </article>
    </div>
    <EmptyState v-else title="No alerts in scope" detail="Alerts will appear here when a part reaches warning or critical replacement risk." />
  </section>
</template>
