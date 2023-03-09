import { Launcher } from './launcher';
import { WindowConfig } from './WindowConfig';

describe('Launcher', () => {
  let testWindowConfig: WindowConfig;
  let testLauncher: Launcher;

  beforeAll( () => {
    testLauncher = new Launcher();
  });

  test('launch() - "Window config has no parameters', () => {
    testWindowConfig = {};

    expect(() => {
      testLauncher.launch(testWindowConfig);
    }).toThrow("At least the url must be specified!");
  });
});