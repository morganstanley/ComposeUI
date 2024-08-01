import React, { useCallback, useMemo, useState } from 'react';
import { Link, navigate, graphql } from 'gatsby';
import { Box } from '@mui/material';

import Layout from '../components/layout';
import VersionSelect from '../components/version-select';
import { getCurrentVersion, getDocsVersion } from '../utils/version-docs';

const DocumentationTemplate = ({ children, data, pageContext, location }) => {
  const allDocs = data.allMdx.nodes;
  const toc = data.mdx.tableOfContents.items;
  const pageTitle = pageContext.frontmatter.title;
  const slug = data.mdx.fields.slug;
  const siteTitle = data.site?.siteMetadata?.title;
  const versions = useMemo(
    () => data.allDirectory.nodes.map((node) => node.base),
    [data.allDirectory.nodes]
  );

  const [selectedVersion, setSelectedVersion] = useState(
    getCurrentVersion(slug, versions)
  );
  const docs = getDocsVersion(allDocs, getCurrentVersion(slug, versions));

  const handleVersionChange = useCallback(
    (event) => {
      const newPath = slug.replace(selectedVersion, event.target.value);
      setSelectedVersion(event.target.value);
      navigate(newPath);
    },
    [selectedVersion, slug]
  );

  return (
    <Layout data={data} location={location}>
      <article className="page-main content">
        <h3>{siteTitle}</h3>
      </article>
      <article className="page-main content documentation-main">
        <nav className="nav documentation-nav">
          <h4>Documentation</h4>
          <Box sx={{ mt: 3 }}>
            <VersionSelect
              versions={versions}
              selectedVersion={selectedVersion}
              onChange={handleVersionChange}
            />
          </Box>
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

export const Head = ({ pageContext }) => (
  <>
    <title>{pageContext.frontmatter.title}</title>
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
    allDirectory(filter: { relativeDirectory: { eq: "documentation" } }) {
      nodes {
        base
      }
    }
    mdx(id: { eq: $id }) {
      frontmatter {
        title
      }
      fields {
        slug
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
