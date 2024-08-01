import { getCurrentVersion, getDocsVersion } from './version-docs';
import { allDocs, slug, versions } from '../../__mocks__/documentation';

test('Gets documentation by version', () => {
  expect(getDocsVersion(allDocs, '1.0')).toEqual([
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
  expect(getCurrentVersion('/documentation/1.0/components/', versions)).toEqual(
    '1.0'
  );
  expect(getCurrentVersion('/documentation/2.0/components/', versions)).toEqual(
    '2.0'
  );
  expect(
    getCurrentVersion('/documentation/stuff/components/', [
      ...versions,
      'stuff',
    ])
  ).toEqual('stuff');
  expect(
    getCurrentVersion('/documentation/3.4.1-rc3/components/', [
      ...versions,
      '3.4.1-rc3',
    ])
  ).toEqual('3.4.1-rc3');
  expect(
    getCurrentVersion('/documentation/3.4.1-rc3/components/', versions)
  ).toEqual(undefined);
});
