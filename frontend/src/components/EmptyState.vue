<script setup lang="ts">
defineProps<{
  message: string
}>()
</script>

<template>
  <div class="py-12 text-center text-gray-400 text-sm">{{ message }}</div>
</template>
