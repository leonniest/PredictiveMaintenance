<script setup lang="ts">
import type { UserProfile } from '../types';

type Page = 'overview' | 'resellers' | 'machines' | 'analytics' | 'alerts' | 'settings' | 'architecture';

defineProps<{
  user: UserProfile;
  activePage: Page;
}>();

defineEmits<{
  navigate: [page: Page];
  logout: [];
}>();
</script>

<template>
  <aside class="sidebar">
    <div>
      <div class="brand-mark">PM</div>
      <strong>Predictive Maintenance</strong>
      <span>{{ user.role }} access</span>
    </div>
    <nav>
      <button :class="{ active: activePage === 'overview' }" @click="$emit('navigate', 'overview')">Overview</button>
      <button :class="{ active: activePage === 'resellers' }" @click="$emit('navigate', 'resellers')">
        {{ user.role === 'Admin' ? 'Resellers' : 'Production' }}
      </button>
      <button :class="{ active: activePage === 'machines' }" @click="$emit('navigate', 'machines')">Machines</button>
      <button :class="{ active: activePage === 'analytics' }" @click="$emit('navigate', 'analytics')">Analytics</button>
      <button :class="{ active: activePage === 'alerts' }" @click="$emit('navigate', 'alerts')">Alerts</button>
      <button :class="{ active: activePage === 'settings' }" @click="$emit('navigate', 'settings')">Settings</button>
      <button :class="{ active: activePage === 'architecture' }" @click="$emit('navigate', 'architecture')">Architecture</button>
    </nav>
    <button class="secondary dark" @click="$emit('logout')">Sign out</button>
  </aside>
</template>
