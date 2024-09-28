import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PodItemComponent } from './pod-item.component';

describe('PodItemComponent', () => {
  let component: PodItemComponent;
  let fixture: ComponentFixture<PodItemComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PodItemComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PodItemComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
