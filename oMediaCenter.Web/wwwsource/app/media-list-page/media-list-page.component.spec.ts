import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MediaListPageComponent } from './media-list-page.component';

describe('MediaListPageComponent', () => {
  let component: MediaListPageComponent;
  let fixture: ComponentFixture<MediaListPageComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [MediaListPageComponent]
    });
    fixture = TestBed.createComponent(MediaListPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
