import React from 'react';
import { MDXProvider } from '@mdx-js/react';

import Header from './header';
import Footer from './footer';
import Seo from './seo';

import Article from './article';
import ButtonLink from './button-link';
import CardCollection from './card-collection';
import * as Cards from './cards/index';
import Example from './example-box';
import Hero from './hero';
import Section from './section';

const shortcodes = {
  Article,
  ButtonLink,
  CardCollection,
  ...Cards,
  Example,
  Hero,
  Section,
};

const links = {
  Documentation: '/documentation',
  // News: '/news', // uncomment for news section
};

function Layout({ data, location, children }) {
  const { documentationUrl, title } = data.site.siteMetadata;

  if (documentationUrl) {
    links.Documentation = documentationUrl;
  }

  return (
    <>
      <Seo title={title} />
      <header className="header-main">
        <Header location={location} links={links} />
      </header>
      <main className="body-main">
        <MDXProvider components={shortcodes}>{children}</MDXProvider>
      </main>
      <Footer />
    </>
  );
}

export default Layout;
