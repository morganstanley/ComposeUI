import React from 'react';

import Card from './card';

export default function CardStory({ children, ...rest }) {
  return <Card type="card-story" {...rest} />;
}
