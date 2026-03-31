---
globs: src/ClientApp/src/**
---

## Vue Component Rules

- Always use <script setup lang="ts"> — never Options API
- Use Composition API patterns (ref, computed, watch, composables)
- TypeScript strict mode — no `any` types, use `unknown` and narrow
- Use Tailwind CSS for styling — no scoped CSS unless absolutely necessary
- Components must be accessible: proper ARIA labels, keyboard navigation
- Use Pinia stores for shared state, not provide/inject for cross-component state
- Emit events with defineEmits, props with defineProps — both fully typed
