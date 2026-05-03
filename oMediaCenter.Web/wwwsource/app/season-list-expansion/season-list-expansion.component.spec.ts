import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { SeasonListExpansionComponent } from './season-list-expansion.component';

describe('SeasonListExpansionComponent', () => {
  let component: SeasonListExpansionComponent;
  let fixture: ComponentFixture<SeasonListExpansionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [SeasonListExpansionComponent],
      schemas: [NO_ERRORS_SCHEMA]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(SeasonListExpansionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
