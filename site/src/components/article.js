import React from 'react';

export default function Article({ title, children }) {
  return (
    <article className="article-wrapper">
      <h3>{title}</h3>
      {children}
    </article>
  );
}
