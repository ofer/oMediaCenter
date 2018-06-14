import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ClientControlComponent } from './client-control.component';

describe('ClientControlComponent', () => {
  let component: ClientControlComponent;
  let fixture: ComponentFixture<ClientControlComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ClientControlComponent ]
    })
    .compileComponents();
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
