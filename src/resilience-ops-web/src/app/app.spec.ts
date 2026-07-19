import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import {
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest';

import { App } from './app';
import { HealthApiService } from './core/services/health-api.service';
import { RiskApiService } from './core/services/risk-api.service';

describe('App', () => {
  const getHealthMock = vi.fn();
  const getRisksMock = vi.fn();
  const createRiskMock = vi.fn();

  beforeEach(async () => {
    getHealthMock.mockReset();
    getRisksMock.mockReset();
    createRiskMock.mockReset();

    getHealthMock.mockReturnValue(
      of({
        status: 'Healthy',
        service: 'ResilienceOps.Api',
        environment: 'Development',
        timestampUtc: '2026-07-19T07:00:00+00:00',
      }),
    );

    getRisksMock.mockReturnValue(
      of([
        {
          id: '1e860e1e-4bcb-47df-b477-eef26e5f7d53',
          title: 'Identity provider outage',
          description: 'Users cannot access critical systems.',
          severity: 'Critical',
          status: 'Open',
          createdUtc: '2026-07-19T07:00:00+00:00',
        },
      ]),
    );

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        {
          provide: HealthApiService,
          useValue: {
            getHealth: getHealthMock,
          },
        },
        {
          provide: RiskApiService,
          useValue: {
            getRisks: getRisksMock,
            createRisk: createRiskMock,
          },
        },
      ],
    }).compileComponents();
  });

  it('should create the application', () => {
    const fixture = TestBed.createComponent(App);

    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should load health and risk data', () => {
    const fixture = TestBed.createComponent(App);

    fixture.detectChanges();

    const page = fixture.nativeElement as HTMLElement;

    expect(getHealthMock).toHaveBeenCalledOnce();
    expect(getRisksMock).toHaveBeenCalledOnce();
    expect(page.textContent).toContain('Healthy');
    expect(page.textContent).toContain(
      'Identity provider outage',
    );
  });

  it('should render the risk registration form', () => {
    const fixture = TestBed.createComponent(App);

    fixture.detectChanges();

    const page = fixture.nativeElement as HTMLElement;

    expect(
      page.querySelector('form'),
    ).not.toBeNull();

    expect(
      page.querySelector('#risk-title'),
    ).not.toBeNull();

    expect(
      page.querySelector('#risk-description'),
    ).not.toBeNull();
  });
});