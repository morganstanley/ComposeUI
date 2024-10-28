const { plugins } = require('./src/config/base-gatsby-plugins');

module.exports = {
  siteMetadata: {
    title: `ComposeUI`,
    description: `ComposeUI is a .NET based general UI Container and Unified UI and App host which enables the hosting of Web and desktop content.`,
    siteUrl: 'https://morganstanley.github.io/ComposeUI',
    documentationUrl: false,
    //  documentationUrl: url-of.documentation.site,
  },
  pathPrefix: `/ComposeUI`, // put GitHub project url slug here e.g. github.com/morganstanley/<project url slug>
  plugins,
};
