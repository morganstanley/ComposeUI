# Morgan Stanley GitHub Pages Project Template

## Local Development

```shell
cd site
npm install
npm run start
```

Go to http://localhost:8000 for the development preview of the website and http://localhost:8000/\_\_\_graphql for the Graphql query tool.

Sample Query to see all Markdown files:

```graphql
{
  allMdx {
    edges {
      node {
        fields {
          slug
        }
        frontmatter {
          description
          title
        }
      }
    }
  }
}
```

## References

- [Documentation](https://www.gatsbyjs.com/docs/?utm_source=starter&utm_medium=readme&utm_campaign=minimal-starter-ts)
- [Tutorials](https://www.gatsbyjs.com/docs/tutorial/?utm_source=starter&utm_medium=readme&utm_campaign=minimal-starter-ts)
- [Guides](https://www.gatsbyjs.com/docs/how-to/?utm_source=starter&utm_medium=readme&utm_campaign=minimal-starter-ts)
- [API Reference](https://www.gatsbyjs.com/docs/api-reference/?utm_source=starter&utm_medium=readme&utm_campaign=minimal-starter-ts)
- [Plugin Library](https://www.gatsbyjs.com/plugins?utm_source=starter&utm_medium=readme&utm_campaign=minimal-starter-ts)
- [Cheat Sheet](https://www.gatsbyjs.com/docs/cheat-sheet/?utm_source=starter&utm_medium=readme&utm_campaign=minimal-starter-ts)
