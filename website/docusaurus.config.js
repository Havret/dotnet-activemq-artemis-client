module.exports = {
  title: '.NET Client for ActiveMQ Artemis',
  tagline: 'Unofficial ActiveMQ Artemis .NET Client for .NET Core and .NET Framework',
  url: 'https://havret.github.io/dotnet-activemq-artemis-client',
  baseUrl: '/dotnet-activemq-artemis-client/',
  favicon: 'img/favicon.ico',
  organizationName: 'havret', // Usually your GitHub org/user name.
  projectName: 'dotnet-activemq-artemis-client', // Usually your repo name.
  themeConfig: {
    navbar: {
      title: '.NET Client for ActiveMQ Artemis',
      logo: {
        alt: 'My Site Logo',
        src: 'img/logo.svg',
      },
      links: [
        {
          to: 'docs/getting-started',
          activeBasePath: 'docs',
          label: 'Docs',
          position: 'left',
        }
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            {
              label: 'Get Started',
              to: 'docs/getting-started',
            }
          ],
        },

        {
          title: 'More',
          items: [            
            {
              label: 'GitHub',
              href: 'https://github.com/Havret/dotnet-activemq-artemis-client',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} Havret. Built with Docusaurus.`,
    },
    prism: {
      additionalLanguages: ['csharp']
    }
  },
  presets: [
    [
      '@docusaurus/preset-classic',
      {
        docs: {
          path: '../docs',
          sidebarPath: require.resolve('./sidebars.js'),
          // Please change this to your repo.
          editUrl:
            'https://github.com/Havret/dotnet-activemq-artemis-client/edit/master/website/',
        },
        blog: {
          showReadingTime: true,
          // Please change this to your repo.
          editUrl:
            'https://github.com/facebook/docusaurus/edit/master/website/blog/',
        },
        theme: {
          customCss: require.resolve('./src/css/custom.css'),
        },
      },
    ],
  ],
};
