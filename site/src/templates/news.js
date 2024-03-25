import React from 'react';
import { graphql } from 'gatsby';

import Layout from '../components/layout';
import Section from '../components/section';
import Seo from '../components/seo';

const NewsPostTemplate = ({ children, data, pageContext, location }) => {
  const date = new Date(pageContext?.frontmatter.date);

  return (
    <Layout data={data} location={location}>
      <Seo title={pageContext.title} description={pageContext.description} />
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
