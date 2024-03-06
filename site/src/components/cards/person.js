import React from 'react';

import Card from './card';

export default function Person({ children, ...rest }) {
  return (
    <Card type="card-person" {...rest}>
      <div className="card-person-title">{children}</div>
    </Card>
  );
}
