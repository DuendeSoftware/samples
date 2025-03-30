import { describe, it, expect } from 'vitest'

import { mount } from '@vue/test-utils'
import HomeView from '../../views/HomeView.vue'

describe('Home welcome', () => {
  it('renders properly', () => {
    const wrapper = mount(HomeView,{})
    expect(wrapper.text()).toContain('Welcome to the Vue BFF example')
  })
})
