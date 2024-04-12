import React from 'react';
import { render, screen } from '@testing-library/react';
// import userEvent from '@testing-library/user-event';
import VersionSelect from './version-select';

const onChange = () => {
  return true;
};

test('Displays the documentation versions', () => {
  const versions = ['1.0', '2.0'];
  const { getByTestId } = render(
    <VersionSelect
      versions={versions}
      selectedVersion={'2.0'}
      showLabel={true}
      onChange={onChange}
    />
  );
  expect(getByTestId('documentation-version-select')).toBeInTheDocument();
});
