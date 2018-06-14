import { TestBed, inject } from '@angular/core/testing';

import { MediaDataService } from './media-data.service';

describe('MediaDataService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [MediaDataService]
    });
  });

  it('should be created', inject([MediaDataService], (service: MediaDataService) => {
    expect(service).toBeTruthy();
  }));
});
