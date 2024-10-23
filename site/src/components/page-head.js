import React from 'react';

function PageHead({ title, children }) {
  return (
    <>
      <title>{title} | Morgan Stanley</title>
      {children}
    </>
  );
}

export default PageHead;
