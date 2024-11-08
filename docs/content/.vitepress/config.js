import { defineConfig } from 'vitepress'

// https://vitepress.dev/reference/site-config
export default defineConfig({
  title: "Hydro",
  description: "Stateful components for Razor Pages",
  themeConfig: {
    // https://vitepress.dev/reference/default-theme-config
    nav: [
      { text: 'Introduction', link: '/introduction/getting-started' },
      { text: 'Guide', link: '/features/components' },
      { text: 'Toolkit', link: 'https://toolkit.usehydro.dev' },
      { text: 'Sponsor', link: 'https://github.com/sponsors/kjeske' }
    ],
    sidebar: [
      {
        text: 'Introduction',
        items: [
          { text: 'Overview', link: '/introduction/overview' },
          { text: 'Motivation', link: '/introduction/motivation' },
          { text: 'Getting started', link: '/introduction/getting-started' },
          { text: 'Comparisons', link: '/introduction/comparisons' }
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
          { text: 'Authorization', link: '/features/authorization' },
          { text: 'Form validation', link: '/features/form-validation' },
          { text: 'Cookies', link: '/features/cookies' },
          { text: 'Long polling', link: '/features/long-polling' },
          { text: 'Errors handling', link: '/features/errors-handling' },
          { text: 'Anti-forgery token', link: '/features/xsrf-token' },
          { text: 'User interface utilities', link: '/features/ui-utils' },
          { text: 'Using JavaScript', link: '/features/js' },
        ]
      },
      {
        text: 'Advanced',
        items: [
          { text: 'Request queuing', link: '/advanced/request-queuing' },
          // { text: 'Load balancing', link: '/advanced/load-balancing' },
        ]
      },
      {
        text: 'Utilities',
        items: [
          { text: 'Hydro views', link: '/utilities/hydro-views' },
        ]
      },
      {
        text: 'Examples',
        items: [
          { text: 'Apps', link: '/examples/apps' },
        ]
      }
    ],

    socialLinks: [
      { icon: 'x', link: 'https://x.com/usehydro' },
      { icon: 'github', link: 'https://github.com/hydrostack/hydro/' },
    ]
  }
})
