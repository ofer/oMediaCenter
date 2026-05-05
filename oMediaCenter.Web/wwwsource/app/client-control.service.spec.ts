import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { ClientControlService } from './client-control.service';
import { SettingsService } from './settings.service';

describe('ClientControlService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ClientControlService,
        SettingsService,
        provideHttpClient(withInterceptorsFromDi()),
        provideRouter([])
      ]
    });
  });

  it('should be created', () => {
    const service = TestBed.inject(ClientControlService);
    expect(service).toBeTruthy();
  });
});
