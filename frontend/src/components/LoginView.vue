<script setup lang="ts">
defineProps<{
  loading: boolean;
  error: string;
}>();

const email = defineModel<string>('email', { required: true });
const password = defineModel<string>('password', { required: true });

defineEmits<{
  signIn: [];
}>();
</script>

<template>
  <main class="login-shell">
    <section class="login-panel">
      <div class="login-brand">
        <p class="eyebrow">PLC and PCB maintenance intelligence</p>
        <h1>Predictive Maintenance</h1>
        <p>Operational command center for reseller-managed rotating doors, sliding doors and gate systems.</p>
      </div>
      <form @submit.prevent="$emit('signIn')" class="login-form">
        <label>
          Email
          <input v-model="email" type="email" autocomplete="username" />
        </label>
        <label>
          Password
          <input v-model="password" type="password" autocomplete="current-password" />
        </label>
        <button :disabled="loading" type="submit">{{ loading ? 'Signing in' : 'Sign in' }}</button>
        <p v-if="error" class="error">{{ error }}</p>
        <div class="demo-users">
          <button type="button" @click="email = 'admin@showcase.local'; password = 'Admin123!'">Admin</button>
          <button type="button" @click="email = 'reseller@atlas-access.test'; password = 'Reseller123!'">Atlas</button>
          <button type="button" @click="email = 'reseller@vector-gate.test'; password = 'Reseller123!'">Vector</button>
        </div>
      </form>
    </section>
  </main>
</template>
