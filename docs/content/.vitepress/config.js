import { defineConfig } from 'vitepress'

// https://vitepress.dev/reference/site-config
export default defineConfig({
  title: "Hydro",
  description: "Stateful components for Razor Pages",
  themeConfig: {
    // https://vitepress.dev/reference/default-theme-config
    nav: [
      { text: 'Introduction', link: '/introduction/getting-started' },
      { text: 'Guide', link: '/features/components' }
    ],

    sidebar: [
      {
        text: 'Introduction',
        items: [
          { text: 'Overview', link: '/introduction/overview' },
          { text: 'Getting started', link: '/introduction/getting-started' }
        ]
      },
      {
        text: 'Features',
        items: [
          { text: 'Components', link: '/features/components' },
          { text: 'Parameters', link: '/features/parameters' },
          { text: 'Binding', link: '/features/binding' },
          { text: 'Actions', link: '/features/actions' },
          { text: 'Events', link: '/features/events' },
          { text: 'Navigation', link: '/features/navigation' },
          { text: 'Form validation', link: '/features/form-validation' },
          { text: 'Anti-forgery token', link: '/features/xsrf-token' },
          { text: 'User interface utilities', link: '/features/ui-utils' },
        ]
      },
      {
        text: 'Advanced concepts',
        items: [
          { text: 'Request queuing', link: '/advanced/request-queuing' },
        ]
      }
    ],

    socialLinks: [
      { icon: 'github', link: 'https://github.com/hydrostack/hydro/' }
    ]
  }
})
