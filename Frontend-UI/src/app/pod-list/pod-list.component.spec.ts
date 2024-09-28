import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PodListComponent } from './pod-list.component';

describe('PodListComponent', () => {
  let component: PodListComponent;
  let fixture: ComponentFixture<PodListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PodListComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PodListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
