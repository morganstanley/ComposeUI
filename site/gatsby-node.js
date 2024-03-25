const path = require(`path`);
const { createFilePath } = require(`gatsby-source-filesystem`);

exports.createPages = async ({ graphql, actions }) => {
  const { createPage } = actions;

  const result = await graphql(
    `
      {
        allMdx {
          nodes {
            id
            tableOfContents
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
    `
  );

  if (result.errors) {
    throw result.errors;
  }

  // Create  pages.
  const pages = result.data.allMdx.nodes;
  const newsTemplate = path.resolve(`./src/templates/news.js`);
  const documentationTemplate = path.resolve(
    `./src/templates/documentation.js`
  );
  const pageTemplate = path.resolve(`./src/templates/page.js`);

  function getCategory(page) {
    const path = page.internal.contentFilePath;

    return path
      ? path.includes('news')
        ? 'news'
        : path.includes('documentation')
        ? 'documentation'
        : ''
      : '';
  }

  function getTemplate(category) {
    return category
      ? category.includes('news')
        ? newsTemplate
        : category.includes('documentation')
        ? documentationTemplate
        : pageTemplate
      : pageTemplate;
  }

  pages.forEach((page, index) => {
    const category = getCategory(page);
    createPage({
      path: page.fields.slug,
      component: `${getTemplate(category)}?__contentFilePath=${
        page.internal.contentFilePath
      }`,
      context: {
        id: page.id,
        category,
      },
    });
  });
};

exports.onCreateNode = ({ node, actions, getNode }) => {
  const { createNodeField } = actions;

  if (node.internal.type === `Mdx`) {
    const value = createFilePath({ node, getNode });
    createNodeField({
      name: `slug`,
      node,
      value,
    });
  }
};
