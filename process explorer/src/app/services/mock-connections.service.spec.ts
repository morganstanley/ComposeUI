import { TestBed } from '@angular/core/testing';

import { MockConnectionsService } from './mock-connections.service';

describe('MockConnectionsService', () => {
  let service: MockConnectionsService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MockConnectionsService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
