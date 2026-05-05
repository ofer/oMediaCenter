import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { MediaPlayerComponent } from './media-player.component';
import { MediaDataService } from '../media-data.service';
import { ClientControlService } from '../client-control.service';
import { SettingsService } from '../settings.service';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';

describe('MediaPlayerComponent', () => {
  let component: MediaPlayerComponent;
  let fixture: ComponentFixture<MediaPlayerComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [MediaPlayerComponent],
      providers: [
        MediaDataService,
        ClientControlService,
        SettingsService,
        provideHttpClient(withInterceptorsFromDi()),
        provideNoopAnimations(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { params: of({}) } }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MediaPlayerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
