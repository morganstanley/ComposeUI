import React from 'react';
import { graphql } from 'gatsby';

import Layout from '../components/layout';
import PageHead from '../components/page-head';
import Section from '../components/section';

const NewsPostTemplate = ({ children, data, pageContext, location }) => {
  const date = new Date(pageContext?.frontmatter.date);

  return (
    <Layout data={data} location={location}>
      <article className="page-main content">
        <h3>News</h3>
        <Section
          category={date.toLocaleDateString()}
          title={pageContext?.frontmatter.title}
        >
          {children}
        </Section>
      </article>
    </Layout>
  );
};

export default NewsPostTemplate;

export const Head = ({ data, pageContext }) => {
  const title = `${pageContext.frontmatter.title} | ${data.site.siteMetadata.title}`;
  return (
    <PageHead title={title}>
      <meta name="description" content={pageContext.description} />
    </PageHead>
  );
};

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
