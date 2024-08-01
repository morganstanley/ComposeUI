import React, { useCallback, useState } from 'react';
import { Link, graphql } from 'gatsby';
import Box from '@mui/material/Box';

import Layout from '../../components/layout';
import Seo from '../../components/seo';
import VersionSelect from '../../components/version-select';
import { getDocsVersion } from '../../utils/version-docs';

import HeroContent from '../../../content/hero.mdx';
import { Toolbar } from '@mui/material';

const DocumentationIndex = ({ data, location }) => {
  const allDocs = data.allMdx.nodes;
  const versions = data.allDirectory.nodes.map((node) => node.base);

  const [selectedVersion, setSelectedVersion] = useState(versions[0]);
  const docs = getDocsVersion(allDocs, selectedVersion);
  const handleVersionChange = useCallback((event) => {
    setSelectedVersion(event.target.value);
  }, []);

  return (
    <Layout data={data} location={location}>
      <div className="main docs-main">
        <HeroContent />

        <article className="content">
          <Toolbar disableGutters sx={{ mb: 3 }} variant="secondary">
            <Box sx={{ flexGrow: 1 }}>
              <h2>Documentation</h2>
            </Box>
            <Box sx={{ maxWidth: 120 }}>
              <VersionSelect
                versions={versions}
                selectedVersion={selectedVersion}
                onChange={handleVersionChange}
              />
            </Box>
          </Toolbar>
        </article>
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
    allDirectory(filter: { relativeDirectory: { eq: "documentation" } }) {
      nodes {
        base
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
