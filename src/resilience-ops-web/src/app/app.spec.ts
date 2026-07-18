import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { App } from './app';
import { HealthApiService } from './core/services/health-api.service';

describe('App', () => {
  const getHealthMock = vi.fn();

  beforeEach(async () => {
    getHealthMock.mockReset();

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        {
          provide: HealthApiService,
          useValue: {
            getHealth: getHealthMock,
          },
        },
      ],
    }).compileComponents();
  });

  it('should create the application', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    expect(app).toBeTruthy();
  });

  it('should render the ResilienceOps title', () => {
    const fixture = TestBed.createComponent(App);

    fixture.detectChanges();

    const page = fixture.nativeElement as HTMLElement;

    expect(page.querySelector('h1')?.textContent).toContain('ResilienceOps');
  });

  it('should load and display the backend health status', () => {
    getHealthMock.mockReturnValue(
      of({
        status: 'Healthy',
        service: 'ResilienceOps.Api',
        environment: 'Development',
        timestampUtc: '2026-07-18T12:47:20.9325583+00:00',
      }),
    );

    const fixture = TestBed.createComponent(App);

    fixture.detectChanges();

    const page = fixture.nativeElement as HTMLElement;
    const button = page.querySelector('button') as HTMLButtonElement;

    button.click();
    fixture.detectChanges();

    expect(getHealthMock).toHaveBeenCalledOnce();
    expect(page.textContent).toContain('Healthy');
    expect(page.textContent).toContain('ResilienceOps.Api');
    expect(page.textContent).toContain('Development');
  });
});