import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LimsManagementComponent } from './lims-management.component';

describe('LimsManagementComponent', () => {
  let component: LimsManagementComponent;
  let fixture: ComponentFixture<LimsManagementComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LimsManagementComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(LimsManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
