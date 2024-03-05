import React from 'react';
import { Link, graphql } from 'gatsby';

import Layout from '../../components/layout';
import Seo from '../../components/seo';

import HeroContent from '../../../content/hero.mdx';

const DocumentationIndex = ({ data, location }) => {
  const docs = data.allMdx.nodes;

  return (
    <Layout data={data} location={location}>
      <div className="main docs-main">
        <HeroContent />
        {docs.map((node) => {
          const title = node.frontmatter.title;
          const toc = node.tableOfContents.items;
          return (
            <article className="content" key={node.fields.slug}>
              <h3>
                <Link to={node.fields.slug}>{title}</Link>
              </h3>
              <ul>
                {toc &&
                  toc.map((item, i) => (
                    <li key={i}>
                      <Link to={`${node.fields.slug}${item.url}`}>
                        {item.title}
                      </Link>
                    </li>
                  ))}
              </ul>
            </article>
          );
        })}
        <Seo title="Documentation" />
      </div>
    </Layout>
  );
};

export default DocumentationIndex;

export const pageQuery = graphql`
  query {
    site {
      siteMetadata {
        title
        documentationUrl
      }
    }
    allMdx(
      filter: { internal: { contentFilePath: { regex: "/documentation//" } } }
      sort: [{ frontmatter: { order: ASC } }]
    ) {
      nodes {
        id
        tableOfContents
        frontmatter {
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
