<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
  points: { label: string; value: number }[];
  color?: string;
}>();

const width = 640;
const height = 190;
const padding = 28;
const max = computed(() => Math.max(...props.points.map((point) => point.value), 1));
const step = computed(() => props.points.length > 1 ? (width - padding * 2) / (props.points.length - 1) : 0);
const coordinates = computed(() => props.points.map((point, index) => ({
  x: padding + index * step.value,
  y: height - padding - (point.value / max.value) * (height - padding * 2)
})));
const path = computed(() => coordinates.value.map((point, index) => `${index === 0 ? 'M' : 'L'} ${point.x} ${point.y}`).join(' '));
</script>

<template>
  <svg class="line-chart" :viewBox="`0 0 ${width} ${height}`" role="img">
    <line :x1="padding" :y1="height - padding" :x2="width - padding" :y2="height - padding" stroke="#d8dedb" />
    <path :d="path" fill="none" :stroke="color ?? '#287c6f'" stroke-width="4" stroke-linecap="round" />
    <circle v-for="point in coordinates" :key="`${point.x}-${point.y}`" :cx="point.x" :cy="point.y" r="5" :fill="color ?? '#287c6f'" />
    <text v-for="(point, index) in points" :key="point.label" :x="padding + index * step" :y="height - 8" text-anchor="middle">
      {{ point.label.slice(5) }}
    </text>
  </svg>
</template>
