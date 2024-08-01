import { getCurrentVersion, getDocsVersion } from './version-docs';
import documentation from '../../__mocks__/documentation';

test('Gets documentation by version', () => {
  getDocsVersion(documentation, '1.0');

  expect(getDocsVersion(documentation, '1.0')).toEqual([
    {
      id: '41748935-af24-51c3-8d32-458375e4246c',
      excerpt:
        'This static site generator processes MDX  in addition to traditional Markdown  files.  allows for the use of JSX components within Markdown.…',
      frontmatter: {
        title: 'Components',
      },
      internal: {
        contentFilePath:
          '/Users/mimiflynn/Projects/MS/ms-gh-pages/site/content/documentation/1.0/components.mdx',
      },
      fields: {
        slug: '/documentation/1.0/components/',
      },
    },
  ]);
});

test('Gets version of current documentation', () => {
  expect(
    getCurrentVersion('/documentation/2.0/components/', ['1.0', '2.0'])
  ).toEqual('2.0');
});
