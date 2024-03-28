export function getDocsVersion(docs, version) {
  return docs.filter((doc) => doc.fields.slug.includes(`/${version}/`));
}

export function getCurrentVersion(slug, versions) {
  let version;
  const slugsAr = slug.split('/');
  versions.forEach((v) => {
    if (slugsAr.includes(v)) {
      version = v;
    }
  });
  return version;
}
