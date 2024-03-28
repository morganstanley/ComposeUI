import React from 'react';
import { graphql } from 'gatsby';

import Hero from '../components/hero';
import Layout from '../components/layout';
import Seo from '../components/seo';

const NotFoundPage = ({ data, location }) => {
  return (
    <Layout data={data} location={location}>
      <div className="main home-main">
        <Hero title="404 Not Found">
          What you are looking for is either no longer here or has moved.
        </Hero>
      </div>
      <Seo title="404: Not Found" />
    </Layout>
  );
};

export default NotFoundPage;

export const pageQuery = graphql`
  query {
    site {
      siteMetadata {
        title
      }
    }
  }
`;
