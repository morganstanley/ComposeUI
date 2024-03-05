import React from 'react';

import Card from './card';

export default function CardTheme({ children, ...rest }) {
  return <Card type="card-theme" {...rest} />;
}
