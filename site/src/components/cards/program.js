import React from 'react';

import Card from './card';

export default function CardProgram({ image, children, ...rest }) {
  return (
    <Card type="card-program" {...rest}>
      <div className="card-program-child">{children}</div>
    </Card>
  );
}
