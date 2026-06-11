<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
  labels: string[];
  values: number[];
  color?: string;
  formatter?: (value: number) => string;
}>();

const max = computed(() => Math.max(...props.values, 1));
</script>

<template>
  <div class="bar-chart">
    <div v-for="(value, index) in values" :key="labels[index]" class="bar-row">
      <span>{{ labels[index] }}</span>
      <div class="bar-track">
        <div class="bar-fill" :style="{ width: `${Math.max(4, (value / max) * 100)}%`, background: color ?? '#287c6f' }"></div>
      </div>
      <strong>{{ formatter ? formatter(value) : value.toLocaleString() }}</strong>
    </div>
  </div>
</template>
