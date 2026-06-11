<script setup lang="ts">
import type { Alert, Company, Machine } from '../types';
import { companyOpenAlerts, healthSeverity, machineAlertCount } from '../utils/maintenance';

defineProps<{
  companies: Company[];
  alerts: Alert[];
}>();

defineEmits<{
  openMachine: [machine: Machine];
}>();
</script>

<template>
  <section class="page-stack">
    <div class="reseller-grid">
      <article v-for="company in companies" :key="company.id" class="panel reseller-card">
        <div class="panel-title">
          <h3>{{ company.name }}</h3>
          <b :class="['score', healthSeverity(company.fleetHealthScore)]">{{ company.fleetHealthScore }}</b>
        </div>
        <p class="meta-line">{{ company.contactName }} / {{ company.contactEmail }}</p>
        <div class="reseller-stats">
          <span>{{ company.machineCount }} machines</span>
          <span>{{ company.industry }}</span>
          <b :class="{ urgent: companyOpenAlerts(company.id, alerts).length > 0 }">{{ companyOpenAlerts(company.id, alerts).length }} open alerts</b>
        </div>
        <div class="machine-mini-list">
          <button v-for="machine in company.machines" :key="machine.id" @click="$emit('openMachine', machine)">
            <span>
              <strong>{{ machine.name }}</strong>
              <small>{{ machine.machineType }} / {{ machine.location }}</small>
            </span>
            <span class="machine-list-right">
              <b :class="['score', healthSeverity(machine.healthScore)]">{{ machine.healthScore }}</b>
              <em v-if="machineAlertCount(machine, alerts)">{{ machineAlertCount(machine, alerts) }} alerts</em>
            </span>
          </button>
        </div>
      </article>
    </div>
  </section>
</template>
