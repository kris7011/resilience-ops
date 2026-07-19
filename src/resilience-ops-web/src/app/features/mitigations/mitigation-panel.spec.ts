import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
} from 'vitest';

import { Mitigation } from '../../core/models/mitigation';
import { MitigationApiService } from '../../core/services/mitigation-api.service';
import { MitigationPanel } from './mitigation-panel';

describe('MitigationPanel', () => {
    const riskId =
        'f585152c-570b-4d2a-9f89-e2acd0011c3d';

    const getMitigationsMock =
        vi.fn();

    const createMitigationMock =
        vi.fn();

    const existingMitigation: Mitigation = {
        id:
            '17fcf344-36e7-4490-b0bd-b982554f4f1e',
        riskId,
        description:
            'Document and test the fallback operating procedure.',
        owner:
            'Platform Operations',
        dueDateUtc:
            '2030-08-02T13:30:00.000Z',
        status:
            'Planned',
        createdUtc:
            '2026-07-19T13:27:10.000Z',
    };

    beforeEach(async () => {
        getMitigationsMock.mockReset();
        createMitigationMock.mockReset();

        getMitigationsMock.mockReturnValue(
            of([
                existingMitigation,
            ]),
        );

        createMitigationMock.mockReturnValue(
            of({
                ...existingMitigation,
                id:
                    'c78ed375-2342-4d78-914e-7d203767683c',
                description:
                    'Test recovery with the alternate supplier.',
                owner:
                    'Business Continuity',
            }),
        );

        await TestBed.configureTestingModule({
            imports: [
                MitigationPanel,
            ],
            providers: [
                {
                    provide:
                        MitigationApiService,
                    useValue: {
                        getMitigations:
                            getMitigationsMock,
                        createMitigation:
                            createMitigationMock,
                    },
                },
            ],
        }).compileComponents();
    });

    it('should load mitigation actions', () => {
        const fixture =
            createFixture();

        const page =
            fixture.nativeElement as HTMLElement;

        expect(
            getMitigationsMock,
        ).toHaveBeenCalledWith(
            riskId,
        );

        expect(
            page.textContent,
        ).toContain(
            existingMitigation.description,
        );

        expect(
            page.textContent,
        ).toContain(
            'Platform Operations',
        );

        expect(
            page.textContent,
        ).toContain(
            'Planned',
        );
    });

    it('should create a mitigation action', () => {
        getMitigationsMock.mockReturnValue(
            of([]),
        );

        const fixture =
            createFixture();

        const page =
            fixture.nativeElement as HTMLElement;

        const description =
            page.querySelector<HTMLTextAreaElement>(
                '[data-testid="mitigation-description"]',
            );

        const owner =
            page.querySelector<HTMLInputElement>(
                '[data-testid="mitigation-owner"]',
            );

        const dueDate =
            page.querySelector<HTMLInputElement>(
                '[data-testid="mitigation-due-date"]',
            );

        const form =
            page.querySelector<HTMLFormElement>(
                '[data-testid="mitigation-form"]',
            );

        expect(description).not.toBeNull();
        expect(owner).not.toBeNull();
        expect(dueDate).not.toBeNull();
        expect(form).not.toBeNull();

        setControlValue(
            description!,
            'Test recovery with the alternate supplier.',
        );

        setControlValue(
            owner!,
            'Business Continuity',
        );

        const dueDateLocal =
            '2030-08-02T13:30';

        setControlValue(
            dueDate!,
            dueDateLocal,
        );

        form!.dispatchEvent(
            new Event(
                'submit',
                {
                    bubbles: true,
                    cancelable: true,
                },
            ),
        );

        fixture.detectChanges();

        expect(
            createMitigationMock,
        ).toHaveBeenCalledWith(
            riskId,
            {
                description:
                    'Test recovery with the alternate supplier.',
                owner:
                    'Business Continuity',
                dueDateUtc:
                    new Date(
                        dueDateLocal,
                    ).toISOString(),
            },
        );

        expect(
            page.textContent,
        ).toContain(
            'Test recovery with the alternate supplier.',
        );

        expect(
            page.textContent,
        ).toContain(
            'Business Continuity',
        );
    });

    function createFixture() {
        const fixture =
            TestBed.createComponent(
                MitigationPanel,
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

    function setControlValue(
        control:
            | HTMLInputElement
            | HTMLTextAreaElement,
        value: string,
    ): void {
        control.value = value;

        control.dispatchEvent(
            new Event(
                'input',
                {
                    bubbles: true,
                },
            ),
        );
    }
});