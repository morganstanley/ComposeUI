import React from 'react';
import { Link, graphql } from 'gatsby';

import Layout from '../components/layout';
import Seo from '../components/seo';

const DocumentationTemplate = ({ children, data, pageContext, location }) => {
  const docs = data.allMdx.nodes;
  const toc = data.mdx.tableOfContents.items;
  const pageTitle = pageContext.frontmatter.title;
  const siteTitle = data.site?.siteMetadata?.title;

  return (
    <Layout data={data} location={location}>
      <Seo title={pageTitle} description={pageContext.description} />
      <article className="page-main content">
        <h3>{siteTitle}</h3>
      </article>
      <article className="page-main content documentation-main">
        <nav className="nav documentation-nav">
          <h4>Documentation</h4>
          <ul>
            {docs.map((node, i) => {
              const current = location.pathname.includes(node.fields.slug);
              const title = node.frontmatter.title;
              return (
                <li key={i} className={current ? 'current' : ''}>
                  <Link to={node.fields.slug}>{title}</Link>
                  {current && (
                    <nav className="nav documentation-content-nav">
                      <ul>
                        {toc &&
                          toc.map((item, j) => (
                            <li key={`link-${j}`}>
                              <Link to={item.url}>{item.title}</Link>
                            </li>
                          ))}
                      </ul>
                    </nav>
                  )}
                </li>
              );
            })}
          </ul>
        </nav>
        <div className="documentation-content">
          <header>
            <h2>{pageTitle}</h2>
          </header>
          {children}
        </div>
      </article>
    </Layout>
  );
};

export default DocumentationTemplate;

export const pageQuery = graphql`
  query ($id: String!) {
    site {
      siteMetadata {
        title
      }
    }
    mdx(id: { eq: $id }) {
      frontmatter {
        title
      }
      tableOfContents
    }
    allMdx(
      filter: { internal: { contentFilePath: { regex: "/documentation//" } } }
      sort: [{ frontmatter: { order: ASC } }]
    ) {
      nodes {
        id
        excerpt
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
