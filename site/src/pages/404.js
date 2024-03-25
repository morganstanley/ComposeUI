import React from 'react';
import { graphql } from 'gatsby';

import Layout from '../components/layout';
import Seo from '../components/seo';

const NotFoundPage = ({ data, location }) => {
  return (
    <Layout data={data} location={location}>
      <div className="home-main">
        <article className="hero">
          <div className="pane">
            <header className="content">
              <h2>404 Not Found</h2>
            </header>
          </div>
        </article>
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
