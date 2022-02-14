import { TestBed } from '@angular/core/testing';

import { MockProcessesService } from './mock-processes.service';

describe('MockMetricsService', () => {
  let service: MockProcessesService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MockProcessesService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
