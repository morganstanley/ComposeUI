const gatsbyRemarkPlugins = [
  `gatsby-remark-autolink-headers`,
  {
    resolve: `gatsby-remark-images`,
    options: {
      maxWidth: 590,
    },
  },
  {
    resolve: `gatsby-remark-responsive-iframe`,
    options: {
      wrapperStyle: `margin-bottom: 1.0725rem`,
    },
  },
  {
    resolve: `gatsby-remark-prismjs`,
    options: {
      showLineNumbers: false,
    },
  },
  `gatsby-remark-copy-linked-files`,
  `gatsby-remark-smartypants`,
];

const gatsbyPluginMdx = {
  resolve: 'gatsby-plugin-mdx',
  options: {
    extensions: [`.md`, `.mdx`],
    gatsbyRemarkPlugins,
  },
};

const plugins = [
  'gatsby-plugin-image',
  'gatsby-plugin-sitemap',
  {
    resolve: 'gatsby-plugin-manifest',
    options: {
      icon: `./src/images/icon.png`,
    },
  },
  {
    resolve: 'gatsby-source-filesystem',
    options: {
      name: 'content',
      path: `./content`,
    },
    __key: 'content',
  },
  gatsbyPluginMdx,
  `gatsby-transformer-sharp`,
  {
    resolve: `gatsby-plugin-sharp`,
    options: {
      defaults: {
        formats: [`auto`, `webp`],
        placeholder: `none`,
        quality: 50,
        breakpoints: [750, 1080, 1366, 1920],
        backgroundColor: `transparent`,
        blurredOptions: {},
        jpgOptions: {},
        pngOptions: {},
        webpOptions: {},
        avifOptions: {},
      },
    },
  },
  `gatsby-plugin-react-helmet`,
  {
    resolve: `gatsby-plugin-webfonts`,
    options: {
      fonts: {
        google: [
          {
            family: 'Karla',
            variants: ['300', '400', '500'],
          },
        ],
      },
    },
  },
];

exports.plugins = plugins;
exports.gatsbyPluginMdx = gatsbyPluginMdx;
exports.gatsbyRemarkPlugins = gatsbyRemarkPlugins;
