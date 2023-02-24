import { executeScriptFile } from './executeScrtiptFile';

describe('CLI', () => {
  let testFileName: string;

  test('executeScriptFile() - No filename specified', () => {
    expect(() => {
      executeScriptFile(testFileName);
    }).toThrow("Specify filename.");
  });
});