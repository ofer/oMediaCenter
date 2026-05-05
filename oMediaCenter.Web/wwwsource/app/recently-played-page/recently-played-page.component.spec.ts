import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { RecentlyPlayedPageComponent } from './recently-played-page.component';
import { MediaDataService } from '../media-data.service';

describe('RecentlyPlayedPageComponent', () => {
  let component: RecentlyPlayedPageComponent;
  let fixture: ComponentFixture<RecentlyPlayedPageComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [RecentlyPlayedPageComponent],
      providers: [
        MediaDataService,
        provideHttpClient(withInterceptorsFromDi()),
        provideRouter([])
      ],
      schemas: [NO_ERRORS_SCHEMA]
    });
    fixture = TestBed.createComponent(RecentlyPlayedPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
