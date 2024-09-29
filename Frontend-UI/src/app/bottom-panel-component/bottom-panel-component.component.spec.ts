import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BottomPanelComponent } from './bottom-panel-component.component';

describe('BottomPanelComponentComponent', () => {
  let component: BottomPanelComponent;
  let fixture: ComponentFixture<BottomPanelComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BottomPanelComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BottomPanelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
