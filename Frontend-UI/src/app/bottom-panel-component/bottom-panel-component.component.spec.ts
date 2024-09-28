import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BottomPanelComponentComponent } from './bottom-panel-component.component';

describe('BottomPanelComponentComponent', () => {
  let component: BottomPanelComponentComponent;
  let fixture: ComponentFixture<BottomPanelComponentComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BottomPanelComponentComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BottomPanelComponentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
