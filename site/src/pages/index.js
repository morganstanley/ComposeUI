import React from 'react';
import { graphql } from 'gatsby';

import Alternatives from '../../content/alternatives.mdx';
import Background from '../../content/background.mdx';
import GettingStarted from '../../content/getting-started.mdx';
import HeroContent from '../../content/hero.mdx';
import UseCases from '../../content/use-cases.mdx';

import Article from '../components/article';
import Layout from '../components/layout';

const SiteIndex = ({ data, location }) => {
  return (
    <Layout data={data} location={location}>
      <div className="main home-main">
        <HeroContent />
        <section className="content">
          <Article title="Background">
            <Background />
          </Article>
          <Article title="Use cases">
            <UseCases />
          </Article>
          <Article title="Alternatives">
            <Alternatives />
          </Article>
          <Article title="Getting Started">
            <GettingStarted />
          </Article>
        </section>
      </div>
    </Layout>
  );
};

export default SiteIndex;

export const pageQuery = graphql`
  query {
    site {
      siteMetadata {
        title
        documentationUrl
      }
    }
  }
`;
