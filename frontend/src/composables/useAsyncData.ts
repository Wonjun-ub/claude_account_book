import { ref } from 'vue'
import type { Ref } from 'vue'

export function useAsyncData<T>(fn: () => Promise<T>) {
  const data: Ref<T | null> = ref(null)
  const isLoading: Ref<boolean> = ref(false)
  const error: Ref<string | null> = ref(null)

  async function execute(): Promise<void> {
    isLoading.value = true
    error.value = null
    try {
      data.value = await fn()
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : '오류가 발생했습니다.'
    } finally {
      isLoading.value = false
    }
  }

  return { data, isLoading, error, execute }
}
