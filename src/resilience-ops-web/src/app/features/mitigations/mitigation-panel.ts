import { DatePipe } from '@angular/common';
import {
    Component,
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
import { finalize } from 'rxjs';

import {
    CreateMitigationRequest,
    Mitigation,
} from '../../core/models/mitigation';
import { MitigationApiService } from '../../core/services/mitigation-api.service';

@Component({
    selector: 'app-mitigation-panel',
    imports: [
        DatePipe,
        ReactiveFormsModule,
    ],
    templateUrl: './mitigation-panel.html',
})
export class MitigationPanel implements OnInit {
    public readonly riskId =
        input.required<string>();

    public readonly riskTitle =
        input.required<string>();

    private readonly formBuilder =
        inject(FormBuilder);

    private readonly mitigationApiService =
        inject(MitigationApiService);

    protected readonly mitigations =
        signal<Mitigation[]>([]);

    protected readonly isLoading =
        signal(false);

    protected readonly isSaving =
        signal(false);

    protected readonly errorMessage =
        signal<string | null>(null);

    protected readonly minimumDueDateLocal =
        this.toDateTimeLocal(
            new Date(
                Date.now() + 5 * 60 * 1000,
            ),
        );

    protected readonly mitigationForm =
        this.formBuilder.nonNullable.group({
            description: [
                '',
                [
                    Validators.required,
                    Validators.maxLength(500),
                ],
            ],
            owner: [
                '',
                [
                    Validators.required,
                    Validators.maxLength(120),
                ],
            ],
            dueDateLocal: [
                '',
                Validators.required,
            ],
        });

    public ngOnInit(): void {
        this.loadMitigations();
    }

    protected loadMitigations(): void {
        this.isLoading.set(true);
        this.errorMessage.set(null);

        this.mitigationApiService
            .getMitigations(
                this.riskId(),
            )
            .pipe(
                finalize(() => {
                    this.isLoading.set(false);
                }),
            )
            .subscribe({
                next: (mitigations) => {
                    this.mitigations.set(
                        mitigations,
                    );
                },
                error: () => {
                    this.errorMessage.set(
                        'The mitigation actions could not be loaded.',
                    );
                },
            });
    }

    protected createMitigation(): void {
        if (this.mitigationForm.invalid) {
            this.mitigationForm.markAllAsTouched();
            return;
        }

        const formValue =
            this.mitigationForm.getRawValue();

        const dueDate =
            new Date(formValue.dueDateLocal);

        if (
            Number.isNaN(dueDate.getTime())
            ||
            dueDate <= new Date()
        ) {
            this.errorMessage.set(
                'Choose a valid future due date.',
            );

            return;
        }

        const request: CreateMitigationRequest = {
            description:
                formValue.description.trim(),
            owner:
                formValue.owner.trim(),
            dueDateUtc:
                dueDate.toISOString(),
        };

        this.isSaving.set(true);
        this.errorMessage.set(null);

        this.mitigationApiService
            .createMitigation(
                this.riskId(),
                request,
            )
            .pipe(
                finalize(() => {
                    this.isSaving.set(false);
                }),
            )
            .subscribe({
                next: (mitigation) => {
                    this.mitigations.update(
                        (currentMitigations) =>
                            [
                                ...currentMitigations,
                                mitigation,
                            ].sort(
                                (left, right) =>
                                    left.dueDateUtc.localeCompare(
                                        right.dueDateUtc,
                                    ),
                            ),
                    );

                    this.mitigationForm.reset({
                        description: '',
                        owner: '',
                        dueDateLocal: '',
                    });
                },
                error: () => {
                    this.errorMessage.set(
                        'The mitigation action could not be created.',
                    );
                },
            });
    }

    private toDateTimeLocal(
        date: Date,
    ): string {
        const localDate =
            new Date(
                date.getTime()
                -
                date.getTimezoneOffset()
                * 60
                * 1000,
            );

        return localDate
            .toISOString()
            .slice(0, 16);
    }
}