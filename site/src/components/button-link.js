import React from 'react';

export default function ButtonLink({ href, text, align, variant = '' }) {
  let classnames = align === 'right' ? `button align-right` : `button`;
  classnames = variant === 'outlined' ? `${classnames} outlined` : classnames;
  return (
    <a className={classnames} href={href}>
      {text}
    </a>
  );
}
