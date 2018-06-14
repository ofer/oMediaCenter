import { TestBed, inject } from '@angular/core/testing';

import { ClientControlService } from './client-control.service';

describe('ClientControlService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ClientControlService]
    });
  });

  it('should be created', inject([ClientControlService], (service: ClientControlService) => {
    expect(service).toBeTruthy();
  }));
});
