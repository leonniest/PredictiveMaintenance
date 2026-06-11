<script setup lang="ts">
import type { Alert, Machine } from '../types';
import { formatDate, formatDateTime, formatNumber } from '../utils/format';
import { healthSeverity, machineAlertCount, severityClass, usageLabel } from '../utils/maintenance';

defineProps<{
  machines: Machine[];
  selectedMachine: Machine | null;
  selectedMachineAlerts: Alert[];
  alerts: Alert[];
}>();

defineEmits<{
  selectMachine: [machineId: string];
  dispatch: [machine: Machine, alert?: Alert];
}>();
</script>

<template>
  <section class="machine-page">
    <div class="panel machine-list-panel">
      <div class="panel-title">
        <h3>Machines</h3>
        <span>{{ machines.length }} in scope</span>
      </div>
      <div class="machine-list">
        <button v-for="machine in machines" :key="machine.id" :class="{ active: selectedMachine?.id === machine.id }" @click="$emit('selectMachine', machine.id)">
          <span>
            <strong>{{ machine.name }}</strong>
            <small>{{ machine.companyName }} / {{ machine.location }}</small>
          </span>
          <span class="machine-list-right">
            <b :class="['score', healthSeverity(machine.healthScore)]">{{ machine.healthScore }}</b>
            <em v-if="machineAlertCount(machine, alerts)">{{ machineAlertCount(machine, alerts) }}</em>
          </span>
        </button>
      </div>
    </div>

    <div class="panel machine-detail" v-if="selectedMachine">
      <div class="panel-title">
        <div>
          <h3>{{ selectedMachine.name }}</h3>
          <span>{{ selectedMachine.companyName }} / {{ selectedMachine.machineType }}</span>
        </div>
        <div class="machine-actions">
          <b :class="['score large', healthSeverity(selectedMachine.healthScore)]">{{ selectedMachine.healthScore }}</b>
          <button @click="$emit('dispatch', selectedMachine)">Mail support</button>
        </div>
      </div>

      <div class="info-grid">
        <article>
          <span>Location</span>
          <strong>{{ selectedMachine.location }}</strong>
        </article>
        <article>
          <span>Installed</span>
          <strong>{{ formatDate(selectedMachine.installedOn) }}</strong>
        </article>
        <article>
          <span>Controller</span>
          <strong>{{ selectedMachine.controller.kind }} {{ selectedMachine.controller.model }}</strong>
        </article>
        <article>
          <span>Firmware</span>
          <strong>{{ selectedMachine.controller.firmwareVersion }}</strong>
        </article>
      </div>

      <div v-if="selectedMachineAlerts.length" class="machine-alert-panel">
        <article v-for="alert in selectedMachineAlerts" :key="alert.id">
          <span :class="['pill', severityClass(alert.severity)]">{{ alert.severity }}</span>
          <div>
            <strong>{{ alert.partName }}</strong>
            <small>First alerted {{ formatDateTime(alert.createdAt) }} / due {{ formatDate(alert.predictedDueDate) }} / health {{ alert.healthScore }}</small>
          </div>
          <button
            :class="['primary-small', { dispatched: alert.status === 'Dispatched' }]"
            :disabled="alert.status === 'Dispatched'"
            @click="$emit('dispatch', selectedMachine, alert)"
          >
            {{ alert.status === 'Dispatched' ? 'Dispatched' : 'Dispatch' }}
          </button>
        </article>
      </div>

      <div class="part-table focused">
        <div class="part-row header">
          <span>Part</span>
          <span>Usage</span>
          <span>Expected</span>
          <span>Heat</span>
          <span>Prediction</span>
        </div>
        <div v-for="part in selectedMachine.controller.parts" :key="part.id" class="part-row">
          <span>
            <strong>{{ part.name }}</strong>
            <small>{{ part.type }} / {{ part.manufacturer }} {{ part.model }}</small>
          </span>
          <span>
            {{ formatNumber(part.totalMovements) }} cycles
            <small>{{ formatNumber(part.totalRotations) }} {{ usageLabel(part) }}</small>
          </span>
          <span>
            {{ formatNumber(part.prediction.expectedMovementsToReplacement) }} cycles
            <small>{{ part.prediction.expectedDaysToReplacement }} days</small>
          </span>
          <span>
            {{ part.averageTemperatureC }} C
            <small>expected {{ part.prediction.expectedTemperatureAtReplacementC }} C</small>
          </span>
          <span>
            <b :class="['score', severityClass(part.prediction.severity)]">{{ part.prediction.healthScore }}</b>
            <small>{{ formatDate(part.prediction.predictedDueDate) }}</small>
          </span>
          <p class="reason">{{ part.prediction.reasonCodes.join(' ') }}</p>
        </div>
      </div>
    </div>
  </section>
</template>
