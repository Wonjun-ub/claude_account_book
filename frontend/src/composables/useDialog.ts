import { ref } from 'vue'

const visible = ref(false)
const message = ref('')
const isConfirm = ref(false)

let resolveCallback: (value: boolean) => void = () => {}

export function useDialog() {
  function showConfirm(msg: string): Promise<boolean> {
    message.value = msg
    isConfirm.value = true
    visible.value = true
    return new Promise(resolve => {
      resolveCallback = resolve
    })
  }

  function showAlert(msg: string): Promise<void> {
    message.value = msg
    isConfirm.value = false
    visible.value = true
    return new Promise(resolve => {
      resolveCallback = () => resolve()
    })
  }

  function handleOk() {
    visible.value = false
    resolveCallback(true)
  }

  function handleCancel() {
    visible.value = false
    resolveCallback(false)
  }

  return { visible, message, isConfirm, showConfirm, showAlert, handleOk, handleCancel }
}
