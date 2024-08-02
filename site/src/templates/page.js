import React from 'react';
import { graphql } from 'gatsby';

import Layout from '../components/layout';

const PageTemplate = ({ title, data, location, children }) => {
  return (
    <Layout data={data} location={location}>
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

export const Head = ({ pageContext }) => (
  <>
    <title>{pageContext.title}</title>
    <meta name="description" content={pageContext.description} />
  </>
);

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
