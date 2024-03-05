import React from 'react';

function Hero({ title, children }) {
  return (
    <div className="hero-wrapper content">
      <h2>{title}</h2>
      <section className="hero">
        <article className="hero-content">{children}</article>
      </section>
    </div>
  );
}

export default Hero;
