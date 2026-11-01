import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
    Component,
    computed,
    inject,
    input,
    OnInit,
    signal,
} from '@angular/core';
import {
    FormBuilder,
    ReactiveFormsModule,
    Validators,
} from '@angular/forms';
import {
    finalize,
    forkJoin,
} from 'rxjs';

import {
    CreateIncidentRequest,
    Incident,
    IncidentStatus,
} from '../../core/models/incident';
import { Mitigation } from '../../core/models/mitigation';
import { IncidentApiService } from '../../core/services/incident-api.service';
import { MitigationApiService } from '../../core/services/mitigation-api.service';

interface IncidentStatusAction {
    nextStatus: IncidentStatus;
    label: string;
    busyLabel: string;
}

// Lifecycle: Reported -> Investigating -> Resolved -> Closed. Resolved and
// Closed incidents can be reopened by moving them back to Investigating.
// The API enforces the same rules and answers 409 for any other move.
const incidentStatusActions: Record<
    IncidentStatus,
    IncidentStatusAction[]
> = {
    Reported: [
        {
            nextStatus: 'Investigating',
            label: 'Start investigation',
            busyLabel: 'Starting...',
        },
    ],
    Investigating: [
        {
            nextStatus: 'Resolved',
            label: 'Mark resolved',
            busyLabel: 'Resolving...',
        },
    ],
    Resolved: [
        {
            nextStatus: 'Closed',
            label: 'Close incident',
            busyLabel: 'Closing...',
        },
        {
            nextStatus: 'Investigating',
            label: 'Reopen',
            busyLabel: 'Reopening...',
        },
    ],
    Closed: [
        {
            nextStatus: 'Investigating',
            label: 'Reopen',
            busyLabel: 'Reopening...',
        },
    ],
};

const incidentStatusClasses: Record<
    IncidentStatus,
    string
> = {
    Reported:
        'bg-red-100 text-red-800',
    Investigating:
        'bg-amber-100 text-amber-800',
    Resolved:
        'bg-emerald-100 text-emerald-800',
    Closed:
        'bg-slate-200 text-slate-700',
};

@Component({
    selector: 'app-incident-panel',
    imports: [
        DatePipe,
        ReactiveFormsModule,
    ],
    templateUrl: './incident-panel.html',
})
export class IncidentPanel implements OnInit {
    public readonly riskId =
        input.required<string>();

    public readonly riskTitle =
        input.required<string>();

    private readonly formBuilder =
        inject(FormBuilder);

    private readonly incidentApiService =
        inject(IncidentApiService);

    private readonly mitigationApiService =
        inject(MitigationApiService);

    protected readonly incidents =
        signal<Incident[]>([]);

    protected readonly mitigations =
        signal<Mitigation[]>([]);

    protected readonly mitigationDescriptions =
        computed(
            () =>
                new Map(
                    this.mitigations().map(
                        (mitigation) => [
                            mitigation.id,
                            mitigation.description,
                        ],
                    ),
                ),
        );

    protected readonly selectedMitigationIds =
        signal<string[]>([]);

    protected readonly isLoading =
        signal(false);

    protected readonly isSaving =
        signal(false);

    protected readonly updatingIncidentId =
        signal<string | null>(null);

    protected readonly errorMessage =
        signal<string | null>(null);

    protected readonly statusActions =
        incidentStatusActions;

    protected readonly statusClasses =
        incidentStatusClasses;

    protected readonly incidentForm =
        this.formBuilder.nonNullable.group({
            title: [
                '',
                [
                    Validators.required,
                    Validators.maxLength(120),
                ],
            ],
            description: [
                '',
                [
                    Validators.required,
                    Validators.maxLength(1000),
                ],
            ],
            responsiblePerson: [
                '',
                [
                    Validators.required,
                    Validators.maxLength(120),
                ],
            ],
        });

    public ngOnInit(): void {
        this.loadIncidents();
    }

    protected loadIncidents(): void {
        this.isLoading.set(true);
        this.errorMessage.set(null);

        forkJoin({
            incidents:
                this.incidentApiService.getIncidents(
                    this.riskId(),
                ),
            mitigations:
                this.mitigationApiService.getMitigations(
                    this.riskId(),
                ),
        })
            .pipe(
                finalize(() => {
                    this.isLoading.set(false);
                }),
            )
            .subscribe({
                next: ({ incidents, mitigations }) => {
                    this.incidents.set(incidents);
                    this.mitigations.set(mitigations);

                    // Drop selections for actions that no longer exist.
                    this.selectedMitigationIds.update(
                        (selected) =>
                            selected.filter(
                                (id) =>
                                    mitigations.some(
                                        (mitigation) =>
                                            mitigation.id === id,
                                    ),
                            ),
                    );
                },
                error: () => {
                    this.errorMessage.set(
                        'The incidents could not be loaded.',
                    );
                },
            });
    }

    protected isMitigationSelected(
        mitigationId: string,
    ): boolean {
        return this.selectedMitigationIds()
            .includes(mitigationId);
    }

    protected toggleMitigation(
        mitigationId: string,
        selected: boolean,
    ): void {
        this.selectedMitigationIds.update(
            (current) =>
                selected
                    ? [
                        ...current.filter(
                            (id) => id !== mitigationId,
                        ),
                        mitigationId,
                    ]
                    : current.filter(
                        (id) => id !== mitigationId,
                    ),
        );
    }

    protected createIncident(): void {
        if (this.incidentForm.invalid) {
            this.incidentForm.markAllAsTouched();
            return;
        }

        const formValue =
            this.incidentForm.getRawValue();

        const request: CreateIncidentRequest = {
            title:
                formValue.title.trim(),
            description:
                formValue.description.trim(),
            responsiblePerson:
                formValue.responsiblePerson.trim(),
            mitigationIds:
                this.selectedMitigationIds(),
        };

        this.isSaving.set(true);
        this.errorMessage.set(null);

        this.incidentApiService
            .createIncident(
                this.riskId(),
                request,
            )
            .pipe(
                finalize(() => {
                    this.isSaving.set(false);
                }),
            )
            .subscribe({
                next: (incident) => {
                    this.incidents.update(
                        (currentIncidents) => [
                            incident,
                            ...currentIncidents,
                        ],
                    );

                    this.incidentForm.reset({
                        title: '',
                        description: '',
                        responsiblePerson: '',
                    });

                    this.selectedMitigationIds.set([]);
                },
                error: (error: unknown) => {
                    this.errorMessage.set(
                        this.describeError(
                            error,
                            'The incident could not be registered.',
                        ),
                    );
                },
            });
    }

    protected changeIncidentStatus(
        incidentId: string,
        status: IncidentStatus,
    ): void {
        if (this.updatingIncidentId() !== null) {
            return;
        }

        this.updatingIncidentId.set(incidentId);
        this.errorMessage.set(null);

        this.incidentApiService
            .updateIncidentStatus(
                this.riskId(),
                incidentId,
                status,
            )
            .pipe(
                finalize(() => {
                    this.updatingIncidentId.set(null);
                }),
            )
            .subscribe({
                next: (updatedIncident) => {
                    this.replaceIncident(
                        updatedIncident,
                    );
                },
                error: (error: unknown) => {
                    this.errorMessage.set(
                        this.describeError(
                            error,
                            'The incident status could not be updated.',
                        ),
                    );

                    // A conflict means this view is stale, so show the
                    // incident as it is stored now.
                    if (
                        error instanceof HttpErrorResponse
                        &&
                        error.status === 409
                    ) {
                        this.refreshIncident(incidentId);
                    }
                },
            });
    }

    private refreshIncident(
        incidentId: string,
    ): void {
        this.incidentApiService
            .getIncident(
                this.riskId(),
                incidentId,
            )
            .subscribe({
                next: (incident) => {
                    this.replaceIncident(incident);
                },
            });
    }

    private replaceIncident(
        updatedIncident: Incident,
    ): void {
        this.incidents.update(
            (currentIncidents) =>
                currentIncidents.map(
                    (incident) =>
                        incident.id === updatedIncident.id
                            ? updatedIncident
                            : incident,
                ),
        );
    }

    // A 409 carries the API's explanation (an invalid transition or a
    // change made by someone else); a 400 carries field validation errors.
    private describeError(
        error: unknown,
        fallback: string,
    ): string {
        if (!(error instanceof HttpErrorResponse)) {
            return fallback;
        }

        const body =
            error.error as {
                error?: string;
                errors?: Record<string, string[]>;
            } | null;

        if (
            error.status === 409
            &&
            body?.error
        ) {
            return body.error;
        }

        if (
            error.status === 400
            &&
            body?.errors
        ) {
            return Object.values(body.errors)
                .flat()
                .join(' ');
        }

        return fallback;
    }
}
