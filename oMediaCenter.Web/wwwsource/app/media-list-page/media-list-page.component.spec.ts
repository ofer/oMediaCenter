import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { provideRouter } from '@angular/router';
import { MediaListPageComponent } from './media-list-page.component';

describe('MediaListPageComponent', () => {
  let component: MediaListPageComponent;
  let fixture: ComponentFixture<MediaListPageComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [MediaListPageComponent],
      providers: [provideRouter([])],
      schemas: [NO_ERRORS_SCHEMA]
    });
    fixture = TestBed.createComponent(MediaListPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
