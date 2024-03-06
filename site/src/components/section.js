import React from 'react';

function Section({ category, title, image, color = '', children }) {
  const eyebrowClassNames = `eyebrow ${color}`;
  return (
    <section className="section">
      {image ? <img src={image} alt={title} /> : ''}
      <div className={eyebrowClassNames}>{category}</div>
      <h3>{title}</h3>
      {children}
    </section>
  );
}

export default Section;
