import { TestBed } from '@angular/core/testing';

import { MockSubsystemsService } from './mock-subsystems.service';

describe('MockSubsystemsService', () => {
  let service: MockSubsystemsService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MockSubsystemsService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
