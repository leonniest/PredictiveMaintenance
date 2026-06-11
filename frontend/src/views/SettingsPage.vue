<script setup lang="ts">
import type { SupportSettings } from '../types';
import { formatDateTime } from '../utils/format';

defineProps<{
  supportSettings: SupportSettings[];
}>();

defineEmits<{
  save: [settings: SupportSettings];
}>();
</script>

<template>
  <section class="page-stack">
    <article v-for="settings in supportSettings" :key="settings.companyId" class="panel settings-panel">
      <div class="panel-title">
        <h3>{{ settings.companyName }}</h3>
        <span>Updated {{ formatDateTime(settings.updatedAt) }}</span>
      </div>
      <div class="settings-grid">
        <label>
          Support department email
          <input v-model="settings.supportDepartmentEmail" type="email" />
        </label>
        <label>
          Technician dispatch email
          <input v-model="settings.technicianDispatchEmail" type="email" />
        </label>
        <label>
          Escalation phone
          <input v-model="settings.escalationPhone" />
        </label>
        <label>
          SLA hours
          <input v-model.number="settings.defaultSlaHours" type="number" min="1" max="168" />
        </label>
      </div>
      <button @click="$emit('save', settings)">Save settings</button>
    </article>
  </section>
</template>
