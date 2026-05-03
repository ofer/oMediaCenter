import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { SettingsService } from './settings.service';

describe('SettingsService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        SettingsService,
        provideHttpClient(withInterceptorsFromDi())
      ]
    });
  });

  it('should be created', () => {
    const service = TestBed.inject(SettingsService);
    expect(service).toBeTruthy();
  });
});
