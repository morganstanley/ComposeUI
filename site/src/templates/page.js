import React from 'react';
import { graphql } from 'gatsby';

import Layout from '../components/layout';
import Seo from '../components/seo';

const PageTemplate = ({ title, data, location, pageContext, children }) => {
  return (
    <Layout data={data} location={location}>
      <Seo title={pageContext.title} description={pageContext.description} />
      <article className="page-main content">
        <header>
          <h2>{title}</h2>
        </header>
        {children}
      </article>
    </Layout>
  );
};

export default PageTemplate;

export const pageQuery = graphql`
  query ($id: String!) {
    site {
      siteMetadata {
        title
      }
    }
    mdx(id: { eq: $id }) {
      id
      frontmatter {
        date
        title
      }
    }
  }
`;
