<script setup lang="ts">
withDefaults(defineProps<{
  message?: string
}>(), {
  message: '불러오는 중...',
})
</script>

<template>
  <div class="py-12 text-center text-gray-400 text-sm">{{ message }}</div>
</template>
