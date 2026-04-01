import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import DataTable from '@/components/shared/DataTable.vue'
import type { DataTableColumn } from '@/types'

const columns: DataTableColumn[] = [
  { key: 'name', label: 'Name', sortable: true },
  { key: 'value', label: 'Value', sortable: true },
]

const data = [
  { name: 'Alpha', value: 10 },
  { name: 'Beta', value: 5 },
  { name: 'Gamma', value: 20 },
]

describe('DataTable', () => {
  it('renders rows correctly', () => {
    const wrapper = mount(DataTable, {
      props: { columns, data },
    })
    const rows = wrapper.findAll('tbody tr')
    expect(rows).toHaveLength(3)
    expect(rows[0].text()).toContain('Alpha')
    expect(rows[0].text()).toContain('10')
  })

  it('renders column headers', () => {
    const wrapper = mount(DataTable, {
      props: { columns, data },
    })
    const headers = wrapper.findAll('th')
    expect(headers).toHaveLength(2)
    expect(headers[0].text()).toContain('Name')
    expect(headers[1].text()).toContain('Value')
  })

  it('shows empty message when data is empty', () => {
    const wrapper = mount(DataTable, {
      props: { columns, data: [], emptyMessage: 'Nothing here' },
    })
    expect(wrapper.text()).toContain('Nothing here')
    expect(wrapper.find('table').exists()).toBe(false)
  })

  it('sorts data when clicking a sortable header', async () => {
    const wrapper = mount(DataTable, {
      props: { columns, data },
    })
    const nameHeader = wrapper.findAll('th')[0]
    await nameHeader.trigger('click')
    const rows = wrapper.findAll('tbody tr')
    expect(rows[0].text()).toContain('Alpha')
    expect(rows[1].text()).toContain('Beta')
    expect(rows[2].text()).toContain('Gamma')

    // Click again for descending
    await nameHeader.trigger('click')
    const rowsDesc = wrapper.findAll('tbody tr')
    expect(rowsDesc[0].text()).toContain('Gamma')
    expect(rowsDesc[1].text()).toContain('Beta')
    expect(rowsDesc[2].text()).toContain('Alpha')
  })

  it('shows default empty message', () => {
    const wrapper = mount(DataTable, {
      props: { columns, data: [] },
    })
    expect(wrapper.text()).toContain('No data available')
  })
})
