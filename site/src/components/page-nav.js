import React from 'react';
import { Link } from 'gatsby';

function PageNavigation({ previous, next }) {
  return (
    <nav className="content">
      <ul className="page-nav">
        {previous && (
          <li>
            <Link to={previous.fields.slug} rel="prev">
              ← {previous.frontmatter.title}
            </Link>
          </li>
        )}
        {next && (
          <li>
            <Link to={next.fields.slug} rel="next">
              {next.frontmatter.title} →
            </Link>
          </li>
        )}
      </ul>
    </nav>
  );
}

export default PageNavigation;
