<script setup lang="ts">
import BarChart from '../components/BarChart.vue';
import LineChart from '../components/LineChart.vue';
import type { FailureTrend, PartLifetime } from '../types';
import { formatMonth, formatNumber } from '../utils/format';

defineProps<{
  lifetimes: PartLifetime[];
  trends: FailureTrend[];
}>();
</script>

<template>
  <section class="analytics-layout">
    <div class="panel chart-panel">
      <div class="panel-title">
        <h3>Replacement life by type</h3>
        <span>Baseline averages from technician replacement samples</span>
      </div>
      <BarChart :labels="lifetimes.map((item) => item.partType)" :values="lifetimes.map((item) => item.averageDaysToReplacement)" color="#245f73" />
    </div>

    <div class="panel chart-panel">
      <div class="panel-title">
        <h3>Technician work-order trend</h3>
        <span>Monthly inspections, repairs, replacements and failure reports</span>
      </div>
      <LineChart :points="trends.map((item) => ({ label: formatMonth(item.month), value: item.totalRecords }))" color="#8a5a22" />
      <div class="trend-table">
        <div class="trend-row header">
          <span>Month</span>
          <span>Inspect</span>
          <span>Repair</span>
          <span>Replace</span>
          <span>Failures</span>
        </div>
        <div v-for="item in trends" :key="item.month" class="trend-row">
          <span>{{ formatMonth(item.month) }}</span>
          <span>{{ item.inspections }}</span>
          <span>{{ item.repairs }}</span>
          <span>{{ item.replacements }}</span>
          <span>{{ item.failureReports }}</span>
        </div>
      </div>
    </div>

    <article v-for="item in lifetimes" :key="item.partType" class="panel lifetime-card">
      <div class="panel-title">
        <h3>{{ item.partType }} lifetime detail</h3>
        <span>{{ item.replacementSamples }} replacement samples</span>
      </div>
      <div class="info-grid compact">
        <article>
          <span>Avg life</span>
          <strong>{{ item.averageDaysToReplacement }} days</strong>
        </article>
        <article>
          <span>Avg cycles</span>
          <strong>{{ formatNumber(item.averageMovementsToReplacement) }}</strong>
        </article>
        <article>
          <span>Avg heat</span>
          <strong>{{ item.averageTemperatureAtReplacementC }} C</strong>
        </article>
        <article>
          <span>Longest part</span>
          <strong>{{ item.longestLastingPart }}</strong>
        </article>
      </div>
      <div class="model-table">
        <div class="model-row header">
          <span>Specific part model</span>
          <span>Samples</span>
          <span>Avg life</span>
          <span>Range</span>
          <span>Avg cycles</span>
          <span>Avg heat</span>
        </div>
        <div v-for="model in item.models" :key="`${item.partType}-${model.manufacturer}-${model.model}-${model.partName}`" class="model-row">
          <span>
            <strong>{{ model.manufacturer }} {{ model.model }}</strong>
            <small>{{ model.partName }}</small>
          </span>
          <span>{{ model.replacementSamples }}</span>
          <span>{{ model.averageDaysToReplacement }} days</span>
          <span>{{ model.lowestDaysToReplacement }}-{{ model.bestDaysToReplacement }} days</span>
          <span>{{ formatNumber(model.averageMovementsToReplacement) }}</span>
          <span>{{ model.averageTemperatureAtReplacementC }} C</span>
        </div>
      </div>
    </article>
  </section>
</template>
