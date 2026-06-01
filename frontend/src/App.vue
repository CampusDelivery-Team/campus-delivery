<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { getApi, type HealthResponse } from './api/http'

const isLoading = ref(false)
const health = ref<HealthResponse | null>(null)
const errorMessage = ref('')

const statusText = computed(() => {
  if (isLoading.value) {
    return 'Checking'
  }

  return health.value?.status ?? 'Disconnected'
})

async function loadHealth() {
  isLoading.value = true
  errorMessage.value = ''

  const result = await getApi<HealthResponse>('/api/health')

  if (result.ok) {
    health.value = result.data
  } else {
    health.value = null
    errorMessage.value = result.message
  }

  isLoading.value = false
}

onMounted(loadHealth)
</script>

<template>
  <main class="app-shell">
    <section class="workspace-panel">
      <div class="title-row">
        <div>
          <p class="eyebrow">Campus Delivery</p>
          <h1>开发通路检查</h1>
        </div>
        <button type="button" class="refresh-button" :disabled="isLoading" @click="loadHealth">
          刷新
        </button>
      </div>

      <div class="status-grid">
        <article class="status-card">
          <span>前端</span>
          <strong>Vue 3 / Vite</strong>
          <small>http://127.0.0.1:5173</small>
        </article>
        <article class="status-card">
          <span>后端</span>
          <strong>{{ statusText }}</strong>
          <small>代理目标 http://localhost:5227</small>
        </article>
      </div>

      <div v-if="health" class="result-box success">
        <span>{{ health.application }}</span>
        <strong>{{ health.environment }}</strong>
        <small>{{ new Date(health.serverTime).toLocaleString() }}</small>
      </div>

      <div v-else-if="errorMessage" class="result-box error">
        <span>接口请求失败</span>
        <strong>{{ errorMessage }}</strong>
        <small>请确认后端已通过 VS2022 或 dotnet run 启动。</small>
      </div>
    </section>
  </main>
</template>
