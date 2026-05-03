import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { ClientControlComponent } from './client-control.component';
import { ClientControlService } from '../client-control.service';
import { MediaDataService } from '../media-data.service';
import { SettingsService } from '../settings.service';

describe('ClientControlComponent', () => {
  let component: ClientControlComponent;
  let fixture: ComponentFixture<ClientControlComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ClientControlComponent],
      providers: [
        ClientControlService,
        MediaDataService,
        SettingsService,
        provideHttpClient(withInterceptorsFromDi()),
        provideRouter([])
      ],
      schemas: [NO_ERRORS_SCHEMA]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ClientControlComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
