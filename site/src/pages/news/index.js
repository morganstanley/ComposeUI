import React from 'react';
import { Link, graphql } from 'gatsby';

import Layout from '../../components/layout';

import HeroContent from '../../../content/hero.mdx';

const NewsIndex = ({ data, location }) => {
  const news = data.allMdx.nodes;

  return (
    <Layout data={data} location={location}>
      <div className="main news-main">
        <HeroContent />
        {news.map((node) => {
          const title = node.frontmatter.title;
          const date = new Date(node.frontmatter.date);
          return (
            <article className="content news-content" key={node.fields.slug}>
              <div className="eyebrow">{date.toLocaleDateString()}</div>
              <h3>
                <Link to={node.fields.slug}>{title}</Link>
              </h3>
              <section>{node.excerpt}</section>
            </article>
          );
        })}
      </div>
    </Layout>
  );
};

export default NewsIndex;

export const Head = () => <title>News</title>;

export const pageQuery = graphql`
  query {
    site {
      siteMetadata {
        title
        documentationUrl
      }
    }
    allMdx(
      filter: { internal: { contentFilePath: { regex: "/news//" } } }
      sort: [{ frontmatter: { date: DESC } }]
    ) {
      nodes {
        excerpt
        frontmatter {
          date
          title
        }
        internal {
          contentFilePath
        }
        fields {
          slug
        }
      }
    }
  }
`;
