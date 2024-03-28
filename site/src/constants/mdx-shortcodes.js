import Article from '../components/article';
import ButtonLink from '../components/button-link';
import CardCollection from '../components/card-collection';
import * as Cards from '../components/cards/index';
import Example from '../components/example-box';
import Hero from '../components/hero';
import Section from '../components/section';

export const ShortCodes = {
  Article,
  ButtonLink,
  CardCollection,
  ...Cards,
  Example,
  Hero,
  Section,
};
