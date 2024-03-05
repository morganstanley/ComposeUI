import React from 'react';

export default function Card({
  category,
  title,
  image,
  color,
  children,
  link,
  type = 'card-base',
}) {
  const cardClassNames = `card ${type}`;
  const eyebrowClassNames = `eyebrow ${color ? color : ''}`;
  return (
    <section className={cardClassNames}>
      <div className="card-inner-wrapper">
        {image ? (
          <div className="card-image">
            {link ? (
              <a href={link}>
                <img src={image} alt={title} />
              </a>
            ) : (
              <img src={image} alt={title} />
            )}
          </div>
        ) : (
          ''
        )}
        <div className="eyebrow-wrapper">
          <div className={eyebrowClassNames}>{category}</div>
        </div>
        <h3>{link ? <a href={link}>{title}</a> : title}</h3>
        {children}
      </div>
    </section>
  );
}
