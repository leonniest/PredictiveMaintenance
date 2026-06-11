<script setup lang="ts">
import { computed, nextTick, ref } from 'vue';
import type { AssistantSession, FaqPrompt, UserProfile } from '../types';

const props = defineProps<{
  user: UserProfile;
  sessions: AssistantSession[];
  activeSessionId: string | null;
  faq: FaqPrompt[];
  busy: boolean;
}>();

const emit = defineEmits<{
  newChat: [];
  selectSession: [sessionId: string];
  send: [content: string];
}>();

const open = ref(false);
const input = ref('');
const chatBody = ref<HTMLElement | null>(null);
const activeSession = computed(() => props.sessions.find((session) => session.id === props.activeSessionId) ?? props.sessions[0] ?? null);

async function send(content = input.value) {
  if (!content.trim()) return;
  emit('send', content.trim());
  input.value = '';
  await nextTick();
  if (chatBody.value) {
    chatBody.value.scrollTop = chatBody.value.scrollHeight;
  }
}
</script>

<template>
  <aside class="assistant-widget" :class="{ open }">
    <button class="assistant-toggle" @click="open = !open">
      <span>{{ open ? 'Close assistant' : 'Maintenance assistant' }}</span>
    </button>
    <section v-if="open" class="assistant-panel">
      <header>
        <div>
          <strong>Maintenance assistant</strong>
          <small>Persistent for {{ user.email }}</small>
        </div>
        <button class="secondary" @click="$emit('newChat')">New chat</button>
      </header>
      <div class="assistant-layout">
        <div class="session-list">
          <button v-for="session in sessions" :key="session.id" :class="{ active: session.id === activeSession?.id }" @click="$emit('selectSession', session.id)">
            {{ session.title }}
          </button>
        </div>
        <div class="chat-column">
          <div ref="chatBody" class="chat-body">
            <article v-for="message in activeSession?.messages ?? []" :key="message.id" :class="['chat-message', message.role]">
              {{ message.content }}
            </article>
          </div>
          <div class="faq-row">
            <button v-for="item in faq" :key="item.label" class="chip" @click="send(item.prompt)">
              {{ item.label }}
            </button>
          </div>
          <form class="chat-input" @submit.prevent="send()">
            <input v-model="input" placeholder="Ask about alerts, parts or dispatch..." />
            <button :disabled="busy">{{ busy ? 'Sending' : 'Send' }}</button>
          </form>
        </div>
      </div>
    </section>
  </aside>
</template>
