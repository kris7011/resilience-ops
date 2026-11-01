import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import {
    of,
    throwError,
} from 'rxjs';
import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
} from 'vitest';

import { Incident } from '../../core/models/incident';
import { Mitigation } from '../../core/models/mitigation';
import { IncidentApiService } from '../../core/services/incident-api.service';
import { MitigationApiService } from '../../core/services/mitigation-api.service';
import { IncidentPanel } from './incident-panel';

describe('IncidentPanel', () => {
    const riskId =
        'f585152c-570b-4d2a-9f89-e2acd0011c3d';

    const getIncidentsMock =
        vi.fn();

    const createIncidentMock =
        vi.fn();

    const updateIncidentStatusMock =
        vi.fn();

    const getIncidentMock =
        vi.fn();

    const getMitigationsMock =
        vi.fn();

    const mitigation: Mitigation = {
        id:
            '17fcf344-36e7-4490-b0bd-b982554f4f1e',
        riskId,
        description:
            'Fail over to the secondary identity provider.',
        owner:
            'Platform Operations',
        dueDateUtc:
            '2030-08-02T13:30:00.000Z',
        status:
            'InProgress',
        createdUtc:
            '2026-07-19T13:27:10.000Z',
    };

    const reportedIncident: Incident = {
        id:
            '5a0d6f0e-8d7c-4c41-9d2e-1b5f3c2a9e10',
        riskId,
        title:
            'Sign-in unavailable',
        description:
            'Users could not sign in for 20 minutes.',
        responsiblePerson:
            'Platform On-Call',
        status:
            'Reported',
        mitigationIds: [
            mitigation.id,
        ],
        reportedUtc:
            '2026-11-01T08:00:00.000Z',
        investigatingUtc: null,
        resolvedUtc: null,
        closedUtc: null,
        updatedUtc:
            '2026-11-01T08:00:00.000Z',
        history: [
            {
                id:
                    'a1',
                incidentId:
                    '5a0d6f0e-8d7c-4c41-9d2e-1b5f3c2a9e10',
                sequence: 1,
                previousStatus: null,
                newStatus:
                    'Reported',
                occurredUtc:
                    '2026-11-01T08:00:00.000Z',
            },
        ],
    };

    beforeEach(async () => {
        getIncidentsMock.mockReset();
        createIncidentMock.mockReset();
        updateIncidentStatusMock.mockReset();
        getIncidentMock.mockReset();
        getMitigationsMock.mockReset();

        getIncidentsMock.mockReturnValue(
            of([
                reportedIncident,
            ]),
        );

        getMitigationsMock.mockReturnValue(
            of([
                mitigation,
            ]),
        );

        await TestBed.configureTestingModule({
            imports: [
                IncidentPanel,
            ],
            providers: [
                {
                    provide:
                        IncidentApiService,
                    useValue: {
                        getIncidents:
                            getIncidentsMock,
                        getIncident:
                            getIncidentMock,
                        createIncident:
                            createIncidentMock,
                        updateIncidentStatus:
                            updateIncidentStatusMock,
                    },
                },
                {
                    provide:
                        MitigationApiService,
                    useValue: {
                        getMitigations:
                            getMitigationsMock,
                    },
                },
            ],
        }).compileComponents();
    });

    it('should show incidents with owner, linked actions and timeline', () => {
        const fixture =
            createFixture();

        const page =
            fixture.nativeElement as HTMLElement;

        expect(
            getIncidentsMock,
        ).toHaveBeenCalledWith(
            riskId,
        );

        expect(
            page.textContent,
        ).toContain(
            'Sign-in unavailable',
        );

        expect(
            page.textContent,
        ).toContain(
            'Platform On-Call',
        );

        expect(
            page.textContent,
        ).toContain(
            mitigation.description,
        );

        const timeline =
            page.querySelector(
                '[data-testid="incident-timeline"]',
            );

        expect(
            timeline?.querySelectorAll('li').length,
        ).toBe(1);

        expect(
            statusActionLabels(page),
        ).toEqual([
            'Start investigation',
        ]);
    });

    it('should offer close and reopen for a resolved incident', () => {
        getIncidentsMock.mockReturnValue(
            of([
                {
                    ...reportedIncident,
                    status: 'Resolved',
                },
            ]),
        );

        const fixture =
            createFixture();

        expect(
            statusActionLabels(
                fixture.nativeElement as HTMLElement,
            ),
        ).toEqual([
            'Close incident',
            'Reopen',
        ]);
    });

    it('should register an incident with the selected mitigation actions', () => {
        getIncidentsMock.mockReturnValue(
            of([]),
        );

        createIncidentMock.mockReturnValue(
            of(reportedIncident),
        );

        const fixture =
            createFixture();

        const page =
            fixture.nativeElement as HTMLElement;

        setControlValue(
            page,
            'incident-title',
            '  Sign-in unavailable ',
        );

        setControlValue(
            page,
            'incident-description',
            'Users could not sign in for 20 minutes.',
        );

        setControlValue(
            page,
            'incident-responsible',
            'Platform On-Call',
        );

        const option =
            page.querySelector<HTMLInputElement>(
                '[data-testid="incident-mitigation-option"]',
            );

        expect(option).not.toBeNull();

        option!.click();

        submitForm(page);
        fixture.detectChanges();

        expect(
            createIncidentMock,
        ).toHaveBeenCalledWith(
            riskId,
            {
                title:
                    'Sign-in unavailable',
                description:
                    'Users could not sign in for 20 minutes.',
                responsiblePerson:
                    'Platform On-Call',
                mitigationIds: [
                    mitigation.id,
                ],
            },
        );

        expect(
            page.querySelectorAll(
                '[data-testid="incident"]',
            ).length,
        ).toBe(1);
    });

    it('should not submit an incomplete incident', () => {
        const fixture =
            createFixture();

        const page =
            fixture.nativeElement as HTMLElement;

        submitForm(page);
        fixture.detectChanges();

        expect(
            createIncidentMock,
        ).not.toHaveBeenCalled();

        expect(
            page.textContent,
        ).toContain(
            'A responsible person between 1 and 120',
        );
    });

    it('should move an incident to the next status', () => {
        updateIncidentStatusMock.mockReturnValue(
            of({
                ...reportedIncident,
                status: 'Investigating',
            }),
        );

        const fixture =
            createFixture();

        const page =
            fixture.nativeElement as HTMLElement;

        statusActionButtons(page)[0].click();
        fixture.detectChanges();

        expect(
            updateIncidentStatusMock,
        ).toHaveBeenCalledWith(
            riskId,
            reportedIncident.id,
            'Investigating',
        );

        expect(
            page.querySelector(
                '[data-testid="incident-status"]',
            )?.textContent?.trim(),
        ).toBe('Investigating');

        expect(
            statusActionLabels(page),
        ).toEqual([
            'Mark resolved',
        ]);
    });

    it('should show the API reason and the stored state when a status change conflicts', () => {
        updateIncidentStatusMock.mockReturnValue(
            throwError(
                () =>
                    new HttpErrorResponse({
                        status: 409,
                        error: {
                            error:
                                'The incident was changed by another request. Reload it and try again.',
                        },
                    }),
            ),
        );

        getIncidentMock.mockReturnValue(
            of({
                ...reportedIncident,
                status: 'Closed',
            }),
        );

        const fixture =
            createFixture();

        const page =
            fixture.nativeElement as HTMLElement;

        statusActionButtons(page)[0].click();
        fixture.detectChanges();

        expect(
            page.querySelector(
                '[data-testid="incident-error"]',
            )?.textContent,
        ).toContain(
            'changed by another request',
        );

        expect(
            getIncidentMock,
        ).toHaveBeenCalledWith(
            riskId,
            reportedIncident.id,
        );

        expect(
            page.querySelector(
                '[data-testid="incident-status"]',
            )?.textContent?.trim(),
        ).toBe('Closed');
    });

    it('should report when incidents cannot be loaded', () => {
        getIncidentsMock.mockReturnValue(
            throwError(
                () => new Error('offline'),
            ),
        );

        const fixture =
            createFixture();

        expect(
            (fixture.nativeElement as HTMLElement)
                .textContent,
        ).toContain(
            'The incidents could not be loaded.',
        );
    });

    function createFixture() {
        const fixture =
            TestBed.createComponent(
                IncidentPanel,
            );

        fixture.componentRef.setInput(
            'riskId',
            riskId,
        );

        fixture.componentRef.setInput(
            'riskTitle',
            'Identity provider outage',
        );

        fixture.detectChanges();

        return fixture;
    }

    function statusActionButtons(
        page: HTMLElement,
    ): HTMLButtonElement[] {
        return Array.from(
            page.querySelectorAll<HTMLButtonElement>(
                '[data-testid="incident-status-action"]',
            ),
        );
    }

    function statusActionLabels(
        page: HTMLElement,
    ): string[] {
        return statusActionButtons(page)
            .map(
                (button) =>
                    button.textContent?.trim() ?? '',
            );
    }

    function setControlValue(
        page: HTMLElement,
        testId: string,
        value: string,
    ): void {
        const control =
            page.querySelector<
                | HTMLInputElement
                | HTMLTextAreaElement
            >(
                `[data-testid="${testId}"]`,
            );

        expect(control).not.toBeNull();

        control!.value = value;
        control!.dispatchEvent(
            new Event('input'),
        );
    }

    function submitForm(
        page: HTMLElement,
    ): void {
        page.querySelector<HTMLFormElement>(
            '[data-testid="incident-form"]',
        )!.dispatchEvent(
            new Event(
                'submit',
                {
                    bubbles: true,
                    cancelable: true,
                },
            ),
        );
    }
});
