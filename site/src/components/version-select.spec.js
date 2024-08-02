import React from 'react';
import { render } from '@testing-library/react';

import VersionSelect from './version-select';
import { versions } from '../../__mocks__/documentation';

const onChange = () => {
  return true;
};

test('Displays the documentation versions', () => {
  const { getByTestId } = render(
    <VersionSelect
      versions={versions}
      selectedVersion={versions[0]}
      showLabel={true}
      onChange={onChange}
    />
  );
  expect(getByTestId('documentation-version-select')).toBeInTheDocument();
});
