import { mocked } from 'jest-mock'
import { jest } from '@jest/globals';

import { Launcher } from './launcher';
import { WindowConfig } from './windowConfig';


jest.mock('./launcher', () => {
  return {
    Launcher: jest.fn().mockImplementation(() => {
      return {
        launch: () => {throw new Error("At least the url must be specified!")},
      };
    })
  };
});

describe('Launcher', () => {
  const MockedLauncher = mocked(Launcher, { shallow:true });

  beforeEach(() => {
     MockedLauncher.mockClear();
  });

  it('constructor was called', () => {
    const testLauncher = new Launcher();
    expect(MockedLauncher).toHaveBeenCalledTimes(1);
  });

  it('throws if Window config has no parameters', () => {
    let testWindowConfig: WindowConfig = {};

    expect(() => {
      const testLauncher = new Launcher();
      testLauncher.launch(testWindowConfig);
    }).toThrow("At least the url must be specified!");
  });
});