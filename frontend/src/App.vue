<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { getApi, type HealthResponse } from './api/http'

type ServiceCard = {
  title: string
  description: string
  tag: string
  tone: 'blue' | 'pink' | 'cyan'
}

type RoleEntry = {
  role: string
  title: string
  description: string
  actions: string[]
}

type FlowStep = {
  title: string
  detail: string
}

const isLoading = ref(false)
const health = ref<HealthResponse | null>(null)
const errorMessage = ref('')

const backendStatus = computed(() => {
  if (isLoading.value) {
    return '正在检测'
  }

  return health.value ? '服务正常' : '等待启动'
})

const serviceCards: ServiceCard[] = [
  {
    title: '外卖分发',
    description: '将校门、食堂、商圈外卖集中分发到宿舍楼或指定节点，减少等待和重复跑动。',
    tag: '高频场景',
    tone: 'blue',
  },
  {
    title: '快递代取',
    description: '支持填写快递公司、取件码和交付地址，由跑腿员从驿站代取并送达。',
    tag: '校园刚需',
    tone: 'pink',
  },
  {
    title: '私人跑腿',
    description: '覆盖临时送物、资料递送、物品转交等个性化需求，流程清晰可追踪。',
    tag: '灵活委托',
    tone: 'cyan',
  },
]

const roleEntries: RoleEntry[] = [
  {
    role: '普通用户',
    title: '发布任务与查看进度',
    description: '维护收货地址，发布外卖、快递或私人跑腿任务，完成支付、签收、评价和投诉。',
    actions: ['注册登录', '发布任务', '我的订单'],
  },
  {
    role: '跑腿员',
    title: '接单配送与状态更新',
    description: '提交跑腿员申请，通过审核后进入任务大厅接单，并按流程更新配送状态。',
    actions: ['申请认证', '任务大厅', '我的接单'],
  },
  {
    role: '管理员',
    title: '审核管理与运营统计',
    description: '维护节点和服务类型，审核跑腿员，处理退款投诉，查看结算、审计和报表。',
    actions: ['基础资料', '审核处理', '统计报表'],
  },
]

const flowSteps: FlowStep[] = [
  {
    title: '发布需求',
    detail: '用户选择服务类型，填写地址、节点、备注和价格信息。',
  },
  {
    title: '在线支付',
    detail: '任务支付后进入待接单状态，跑腿员可在任务大厅查看。',
  },
  {
    title: '接单配送',
    detail: '跑腿员抢单或管理员派单，配送过程写入状态日志。',
  },
  {
    title: '签收评价',
    detail: '用户确认签收后可评价或投诉，管理员可继续处理售后。',
  },
]

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
    <section class="hero">
      <nav class="topbar" aria-label="主导航">
        <a class="brand" href="#">
          <span class="brand-logo">跑</span>
          <span>
            <strong>校园跑腿</strong>
            <small>Campus Runner Service</small>
          </span>
        </a>

        <div class="nav-links">
          <a href="#services">服务类型</a>
          <a href="#roles">角色入口</a>
          <a href="#flow">服务流程</a>
        </div>

        <div class="nav-actions">
          <button type="button" class="text-button" :disabled="isLoading" @click="loadHealth">
            {{ isLoading ? '检测中' : '系统检测' }}
          </button>
          <a class="solid-button small" href="#roles">进入系统</a>
        </div>
      </nav>

      <div class="hero-grid">
        <div class="hero-copy">
          <p class="eyebrow">校园中转分发与跑腿服务管理系统</p>
          <h1>把校园里的每一次代取、配送和转交变得清楚可靠</h1>
          <p class="hero-text">
            面向普通用户、跑腿员和管理员的校园服务平台，覆盖外卖分发、快递代取、私人跑腿、支付签收、
            评价投诉、结算审计和统计报表。
          </p>

          <div class="hero-actions">
            <a class="solid-button" href="#services">立即发布任务</a>
            <a class="outline-button" href="#roles">我是跑腿员</a>
          </div>

          <div class="hero-metrics" aria-label="系统能力">
            <div>
              <strong>3类</strong>
              <span>跑腿服务</span>
            </div>
            <div>
              <strong>24张</strong>
              <span>业务数据表</span>
            </div>
            <div>
              <strong>全流程</strong>
              <span>状态追踪</span>
            </div>
          </div>
        </div>

        <aside class="task-preview" aria-label="任务预览">
          <div class="preview-header">
            <span>推荐任务</span>
            <strong>快递代取</strong>
          </div>
          <div class="route-card">
            <div class="route-point">
              <span></span>
              <div>
                <strong>北区快递驿站</strong>
                <small>取件码、快递公司、备注统一记录</small>
              </div>
            </div>
            <div class="route-line"></div>
            <div class="route-point finish">
              <span></span>
              <div>
                <strong>学生公寓 6 号楼</strong>
                <small>送达后用户确认签收</small>
              </div>
            </div>
          </div>
          <div class="preview-footer">
            <div>
              <span>任务状态</span>
              <strong>待接单</strong>
            </div>
            <div>
              <span>预估费用</span>
              <strong>¥6.00</strong>
            </div>
          </div>
        </aside>
      </div>
    </section>

    <section id="services" class="section-block">
      <div class="section-title">
        <p class="eyebrow">Services</p>
        <h2>常用校园跑腿服务</h2>
        <span>围绕校园生活中的高频委托场景设计，后续模块开发可以直接从这些入口继续扩展。</span>
      </div>

      <div class="service-grid">
        <article
          v-for="card in serviceCards"
          :key="card.title"
          class="service-card"
          :class="`tone-${card.tone}`"
        >
          <span>{{ card.tag }}</span>
          <h3>{{ card.title }}</h3>
          <p>{{ card.description }}</p>
          <a href="#roles">发布此类任务</a>
        </article>
      </div>
    </section>

    <section id="roles" class="section-block role-section">
      <div class="section-title">
        <p class="eyebrow">Portals</p>
        <h2>按身份进入系统</h2>
        <span>主界面提供真实项目入口，后续登录和权限完成后可接入对应页面。</span>
      </div>

      <div class="role-grid">
        <article v-for="entry in roleEntries" :key="entry.role" class="role-card">
          <span>{{ entry.role }}</span>
          <h3>{{ entry.title }}</h3>
          <p>{{ entry.description }}</p>
          <div>
            <a v-for="action in entry.actions" :key="action" href="#flow">{{ action }}</a>
          </div>
        </article>
      </div>
    </section>

    <section id="flow" class="section-block flow-section">
      <div class="section-title">
        <p class="eyebrow">Workflow</p>
        <h2>从发布到完成的服务闭环</h2>
        <span>优先跑通发布任务、支付、接单、状态流转、签收和评价这一条主流程。</span>
      </div>

      <div class="flow-grid">
        <article v-for="(step, index) in flowSteps" :key="step.title" class="flow-card">
          <span>{{ index + 1 }}</span>
          <h3>{{ step.title }}</h3>
          <p>{{ step.detail }}</p>
        </article>
      </div>
    </section>

    <section class="system-strip" aria-label="系统状态">
      <div>
        <span>后端状态</span>
        <strong>{{ backendStatus }}</strong>
      </div>
      <div>
        <span>应用服务</span>
        <strong>{{ health?.application ?? 'CampusDelivery.Api' }}</strong>
      </div>
      <div>
        <span>数据库</span>
        <strong>Oracle 19c</strong>
      </div>
      <p v-if="health">
        当前环境：{{ health.environment }}，检测时间：{{ new Date(health.serverTime).toLocaleString() }}
      </p>
      <p v-else-if="errorMessage">
        后端暂未连通：{{ errorMessage }}。请先启动后端服务。
      </p>
    </section>
  </main>
</template>
