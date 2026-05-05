import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { MediaDataService } from './media-data.service';

describe('MediaDataService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        MediaDataService,
        provideHttpClient(withInterceptorsFromDi())
      ]
    });
  });

  it('should be created', () => {
    const service = TestBed.inject(MediaDataService);
    expect(service).toBeTruthy();
  });
});
