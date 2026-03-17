import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { db } from '@/database/db'
import type { Category, TransactionType } from '@/database/db'

export const useCategoryStore = defineStore('category', () => {
  // isDeleted=false인 카테고리만 보관
  const categories = ref<(Category & { id: number })[]>([])

  async function loadCategories(): Promise<void> {
    const all = await db.categories.where('isDeleted').equals(0).toArray()
    categories.value = all.filter((c): c is Category & { id: number } => c.id !== undefined)
  }

  async function addCategory(name: string, type: TransactionType): Promise<void> {
    await db.categories.add({ name, type, isDefault: false, isDeleted: false })
    await loadCategories()
  }

  async function deleteCategory(id: number): Promise<void> {
    const cat = await db.categories.get(id)
    if (!cat) return

    // 연결된 거래 존재 시 soft delete
    const linked = await db.transactions.where('categoryId').equals(id).count()
    if (linked > 0) {
      await db.categories.update(id, { isDeleted: true })
    } else {
      await db.categories.delete(id)
    }
    await loadCategories()
  }

  const incomeCategories  = computed(() => categories.value.filter(c => c.type === 'Income'))
  const expenseCategories = computed(() => categories.value.filter(c => c.type === 'Expense'))
  const savingsCategories = computed(() => categories.value.filter(c => c.type === 'Savings'))

  return {
    categories,
    incomeCategories,
    expenseCategories,
    savingsCategories,
    loadCategories,
    addCategory,
    deleteCategory,
  }
})
