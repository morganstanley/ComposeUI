import React from 'react';
import Section from './section';

export default function CardCollection({
  title,
  description,
  color = 'primary',
  children,
  cols = 1,
}) {
  const colorClassnames = `card-collection-container ${color}`;
  const classnames = `card-collection card-collection-cols-${cols}`;
  return (
    <div className={colorClassnames}>
      {description || title ? (
        <Section title={title}>{description}</Section>
      ) : null}
      <div className={classnames}>{children}</div>
    </div>
  );
}
