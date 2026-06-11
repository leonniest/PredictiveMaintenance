<script setup lang="ts">
import type { Alert } from '../types';
import { formatDate } from '../utils/format';
import { severityClass } from '../utils/maintenance';
import EmptyState from './EmptyState.vue';

defineProps<{
  alerts: Alert[];
}>();

defineEmits<{
  openMachine: [machineId: string];
}>();
</script>

<template>
  <section class="alert-summary">
    <div class="alert-summary-header">
      <span>Priority alert queue</span>
      <strong>{{ alerts.length }} open</strong>
    </div>
    <div v-if="alerts.length" class="alert-summary-list">
      <button v-for="alert in alerts.slice(0, 5)" :key="alert.id" @click="$emit('openMachine', alert.machineId)">
        <span :class="['signal', severityClass(alert.severity)]"></span>
        <span>
          <strong>{{ alert.machineName }}</strong>
          <small>{{ alert.partName }} / first {{ formatDate(alert.createdAt) }} / due {{ formatDate(alert.predictedDueDate) }}</small>
        </span>
        <b>{{ alert.healthScore }}</b>
      </button>
    </div>
    <EmptyState v-else title="No active alerts" detail="The current production scope has no open or notified alerts." />
  </section>
</template>
