import React from 'react';
import InputLabel from '@mui/material/InputLabel';
import MenuItem from '@mui/material/MenuItem';
import FormControl from '@mui/material/FormControl';
import Select from '@mui/material/Select';

function VersionSelect({ versions, selectedVersion, onChange, showLabel }) {
  return (
    <FormControl data-testid="documentation-version-select">
      {showLabel && (
        <InputLabel id="documentation-version-label">Version</InputLabel>
      )}
      <Select
        labelId="documentation-version-label"
        id="documentation-version-select"
        value={selectedVersion}
        onChange={onChange}
      >
        {versions.reverse().map((version, index) => (
          <MenuItem
            selected={version === selectedVersion}
            value={version}
            key={`version-${index}`}
          >
            {version}
          </MenuItem>
        ))}
      </Select>
    </FormControl>
  );
}

export default VersionSelect;
